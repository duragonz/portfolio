using System;

using gplat;

namespace gplat
{
	public class DbBroker
		: WorkerThread
	{
		public UInt16 m_broker_id = 0;
		public Int32 m_allocated_count = 0;
		public type_e m_broker_type;

		MessageReceiver m_req_message_receiver = null;
		public DateTime m_lastupdate = DateTime.UtcNow;

		public enum type_e
		{
			_NONE,
			UserDbBroker,
			SystemDbBroker,
			LogDbBroker,
			RestBroker,
			_END
		}

		public DbBroker(UInt16 in_broker_id, type_e in_broker_type)
			: base("server.broker")
		{
			m_broker_id = in_broker_id;
			m_broker_type = in_broker_type;
			setLoopSleepTime(10);
		}

		public Result init<MESSAGE_RECEIVER_TYPE>() where MESSAGE_RECEIVER_TYPE : new()
		{
			object message_receiver = new MESSAGE_RECEIVER_TYPE();
			m_req_message_receiver = (MessageReceiver)message_receiver;

			var gen_result = new Result();
			if (m_initialized)
			{
				return gen_result.setFail("already initialized.");
			}
			m_initialized = true;
			return gen_result.setOk();
		}

		public override bool onStart()
		{
			m_log.Info($"BROKER[{m_broker_type}_{m_broker_id}] START");
			return true;
		}

		public override void onStop()
		{
			m_log.Info($"BROKER[{m_broker_type}_{m_broker_id}] STOPPED..");
		}
		void setDbReqMessageReceiver(MessageReceiver in_db_message_receiver)
		{
			m_req_message_receiver = in_db_message_receiver;
		}

		public void dealloc()
		{
			if (m_allocated_count <= 0)
			{
				return;
			}
			m_allocated_count--;
		}

		public Int32 queueCount()
		{
			return m_req_message_receiver.totalQueueCount();
		}

		public void pushMessage(GenPacket gen_packet)
		{
			m_req_message_receiver.pushMessage(gen_packet);
		}

		private void processMessageReceivers()
		{
			m_req_message_receiver.processPackets();
		}

		public override void onUpdate(int elapsed_time)
		{
			if (!m_initialized)
			{
				return;
			}
			m_lastupdate = DateTime.UtcNow;
			processMessageReceivers();
		}
	}
}
