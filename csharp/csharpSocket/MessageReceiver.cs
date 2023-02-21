using log4net;

using System;
using System.Collections.Generic;

namespace gplat
{
	public class MessageReceiver
	{
		public SuspendHandlerManager m_suspend_handler_mgr = SuspendHandlerManager.it; //#suspendhandler, #virtualserver 기본 suspend handler manager 지정

		public delegate void MessageHandleFunction(GenPacket in_packet);
		public static Int32 MAX_PROCESS_PACKET_PER_FRAME = 4;

		ILog m_log;
		protected bool m_handle_direct = false;
		protected Dictionary<UInt32, Delegate> m_handlers = new Dictionary<UInt32, Delegate>(); //메시지 처리를 위한 핸들러
		protected List<PacketQueue> m_message_queues = new List<PacketQueue>();

		public UInt32 m_total_consume = 0;
		public Int32 m_last_consume = 0;

		public bool m_use_max_process_packet_per_frame = true;

		public string loggerName => m_log.Logger.Name;

		public MessageReceiver(string in_logger_name = "receiver.noname", SuspendHandlerManager in_suspend_handler_mgr = null, bool in_use_max_process_packet_per_frame = true)
		{
			m_log = gplat.Log.logger(in_logger_name);
			m_use_max_process_packet_per_frame = in_use_max_process_packet_per_frame;

			if (null != in_suspend_handler_mgr) //#suspendhandler 메시지 리시버 suspend handler 지정
			{
				m_suspend_handler_mgr = in_suspend_handler_mgr;
			}

			//우선순위 만큼 생성 
			for (Int16 priority = 0; priority < (Int16)gplat_define.packet_priority_e._END; ++priority)
			{
				var packet_queue = new PacketQueue();
				packet_queue.m_priority = (gplat_define.packet_priority_e)priority;
				m_message_queues.Add(packet_queue);
			}

			m_log.Debug($"[{loggerName}] suspendhandler:{m_suspend_handler_mgr.loggerName}");
		}

		//메시지큐를 모두 정리함.
		public void clearMessageQueues()
		{
			foreach (var queue in m_message_queues)
			{
				queue.clear();
			}
		}

		~MessageReceiver()
		{
			m_handlers.Clear();
		}

		public void setHandleDirect(bool in_yes)
		{
			m_handle_direct = in_yes;
		}

		public bool isDirectHandler()
		{
			return m_handle_direct;
		}

		public void processUnhandledMessage(GenPacket in_gen_packet)
		{
			m_log.Error($"processUnhandledMessage: not handled message_id:{in_gen_packet.detailMessageIdStr}. please use suspend handler or registerMessageHandler");
		}

		//모든 숫자형 타입 받아들이기
		public void registerHandler<T>(T in_msg_id, MessageHandleFunction in_handle_func = null)
		{
			UInt32 msg_id = (UInt32)Convert.ChangeType(in_msg_id, typeof(UInt32));
			registerHandler(msg_id, in_handle_func);
		}

		public void registerHandler(UInt32 in_msg_id, MessageHandleFunction in_handler_func = null)
		{
			MessageHandleFunction handler_func = in_handler_func;
			if (null == in_handler_func)
			{
				// c#특성상 message_processor객체는 살아있음 
				var message_processor = new MessageProcessor<handler_unhandled>();
				handler_func = message_processor.process;
			}
			setHandler(in_msg_id, handler_func);
		}
		// register handler
		public void registerMessageHandler<HANDLER_TYPE>(bool in_req_loggable = false, bool in_ack_loggable = false) where HANDLER_TYPE : new()
		{
			// c#특성상 message_processor객체는 살아있음 (체크필요)
			var message_processor = new MessageProcessor<HANDLER_TYPE>();
			var msg_id = message_processor.messageHandlerId();

			MessageHandlerLoggingPolicy.it.setLoggable(msg_id, in_req_loggable, in_ack_loggable); //메시지핸들러 로깅정책

			message_processor.m_suspend_handler_mgr = m_suspend_handler_mgr; //#suspendhandler singleton에서 개별지정으로 변경
			if (!setHandler(msg_id, message_processor.process))
			{
				var handler_type = typeof(HANDLER_TYPE);
				gplat.Log.logger("sys").Warn($"register handler failed for {handler_type.FullName}");
			}
		}

		//requester가 사라졌을 때 처리를 위해 전용 함수 추가
		public void registerRequestHandler<MSG_ID_TYPE>(MSG_ID_TYPE in_msg_id)
		{
			var message_processor = new MessageProcessor<handler_suspend_unhandled>();
			var msg_id = (UInt32)Convert.ChangeType(in_msg_id, typeof(UInt32));

			message_processor.m_suspend_handler_mgr = m_suspend_handler_mgr; //#suspendhandler singleton에서 개별지정으로 변경
			registerHandler(msg_id, message_processor.process);
		}


		private bool setHandler(UInt32 in_msg_id, MessageHandleFunction in_handler_func)
		{
			if (m_handlers.ContainsKey(in_msg_id))
			{
				m_log.Fatal($"msg_id[{in_msg_id}] {in_handler_func.GetType()}'s handler already registered. please check this !!!");
				return false;
			}
			m_handlers[in_msg_id] = in_handler_func;
			return true;
		}

		public void removeHandler(UInt32 in_msg_id)
		{
			if (false == m_handlers.ContainsKey(in_msg_id))
			{
				m_log.Error($"no handler for {in_msg_id}");
				return;
			}

			m_handlers.Remove(in_msg_id);
		}

		public bool hasHandler(UInt32 in_msg_id)
		{
			if (m_handlers.ContainsKey(in_msg_id))
			{
				return true;
			}
			return false;
		}

		public bool processable(GenPacket in_packet)
		{
			return hasHandler(in_packet.messageId());
		}

		public bool handleNotify(GenPacket in_packet, bool in_notify_direct = false)
		{
			if (in_notify_direct || isDirectHandler())
			{
				return this.handlePacket(in_packet);
			}
			else
			{
				this.pushMessage(in_packet);
				return true;
			}
		}

		public void pushMessage(GenPacket in_packet)
		{
			//우선순위에 따라서 패킷 넣어주기 
			var priority = in_packet.m_packet_header.m_protocol_type;
			var queue = m_message_queues[priority];
			queue.push(in_packet);
		}

		public Int32 totalQueueCount()
		{
			Int32 total = 0;
			foreach (var q in m_message_queues)
			{
				total += q.count();
			}
			return total;
		}

		public Int32 queueCount(Int16 in_priority)
		{
			return m_message_queues[in_priority].count();
		}

		public List<GenPacket> popMessages(Int32 in_count, Int16 in_priority)
		{
			var queue = m_message_queues[in_priority];
			return queue.pop(in_count);
		}


		public bool handlePacket(GenPacket in_packet) //해당 프로토콜에 대한 Response처리
		{
			bool executed = false;
			if (null == in_packet)
			{
				return executed;
			}

			// guid를 가지고 있는가?
			var message_id = in_packet.messageId();
			if (m_handlers.ContainsKey(message_id))
			{
				m_log.Debug($"handle packet:{in_packet.packetGuid()} {in_packet.detailMessageIdStr}");
				MessageHandleFunction func = (MessageHandleFunction)m_handlers[message_id];
				func(in_packet);
				executed = true;
			}
			else if (m_handlers.ContainsKey(0)) // has global message handler
			{
				MessageHandleFunction func = (MessageHandleFunction)m_handlers[0];
				func(in_packet);
				executed = true;
			}
			return executed;
		}


		//public QueueConsumeInfo 


		//매프레임마다 반드시 처리되도록 호출해주어야 함.
		public Int32 processPackets()
		{
			Int32 processed_count = 0;

			Int32 processing_q_count = 0;

			foreach (var queue in m_message_queues) //우선 순위로 도는가?
			{
				processing_q_count = MAX_PROCESS_PACKET_PER_FRAME < queue.count() ? MAX_PROCESS_PACKET_PER_FRAME : queue.count(); // 한프레임에 처리할수 있는 최대 패킷만큼 가져옴

				var packets = queue.pop(processing_q_count);
				m_total_consume += (UInt32)packets.Count;

				foreach (var packet in packets)
				{
					handlePacket(packet);
					processed_count++;
				}
			}

			m_last_consume = processed_count;
			return processed_count;
		}
	}

}
