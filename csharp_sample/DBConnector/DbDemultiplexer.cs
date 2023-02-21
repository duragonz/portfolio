using System;
using System.Collections.Generic;
using System.Linq;

using gplat;

namespace gplat
{
	public class DbDemultiplexer
		: WorkerThread
	{
		public static DbDemultiplexer it = new DbDemultiplexer();

		MessageReceiver m_logic_message_receiver = null; //전체 수집용 
		public DateTime m_db_demux_lastupdate = DateTime.UtcNow;

		//통합관리용 : 전체 디비 브로커를 가지고 있음.
		List<DbBroker> m_total_brokers = new List<DbBroker>(); //통합관리용 

		// 카테고리별 디비 브로커 : 시스템, 유져, 로그
		Int32 m_last_sys_broker_idx = 0;
		List<DbBroker> m_sys_brokers = new List<DbBroker>(); //시스템 요청 처리용 

		//무작위로 사용 
		Int32 m_last_log_broker_idx = 0;
		List<DbBroker> m_log_brokers = new List<DbBroker>(); //로그 

		//무작위로 사용 
		Int32 m_last_rest_broker_idx = 0;
		List<DbBroker> m_rest_brokers = new List<DbBroker>(); //로그 

		//사용자는 접속 시 할당 : 순서보장 
		Dictionary<UInt16, DbBroker> m_user_db_brokers = new Dictionary<UInt16, DbBroker>(); //유져 

		public DbDemultiplexer()
			: base("logic.dbmultiplexer")
		{
		}

		//[db_conn]런쳐인 경우에는 workercount를 직접지정하도록 하자.
		void setupDbprovider(Int32 in_override_db_worker_count = 0)
		{
			Int32 db_worker_count = Server.Config.dbWorkerCount();
			if (in_override_db_worker_count != 0)
			{
				db_worker_count = in_override_db_worker_count;
			}
			//var pool_count = (db_worker_count * 2) + 1; // db_worker * ( contents + log ) + system db broker, 컨넥션 풀 없을 때 오류 처리 부분 체크 
			var pool_count = (db_worker_count * 3); //추후 브로커별 설정 추가 
			foreach (var db_info in Server.Config.m_db_infos.Values)
			{
				DbProviderBridge.it.addProvider(db_info, pool_count);
			}
		}

		public override bool onStart()
		{
			InternalNotifier.it.registerMessageReceiver(m_logic_message_receiver); // 내부 통신 용 

			startDbBrokers();
			return true;
		}

		public override void onStop()
		{
			stopDbBrokers();
		}

		//generic dbbroker message receiver 
		public Result init<MESSAGE_RECEIVER_TYPE>(Int32 override_db_worker_count = 0) where MESSAGE_RECEIVER_TYPE : new()
		{
			var gen_result = new Result();
			if (m_initialized)
			{
				return gen_result.setFail("already initialized.");
			}

			object message_receiver = new MESSAGE_RECEIVER_TYPE();
			m_logic_message_receiver = (MessageReceiver)message_receiver;

			setupDbprovider(override_db_worker_count);

			makeDbBrokers<MESSAGE_RECEIVER_TYPE>();

			m_initialized = true;
			return gen_result.setOk();
		}

		// 새로 생성하고 total brokers에 넣어준다. 
		DbBroker createNewDbBroker(UInt16 in_broker_id, DbBroker.type_e in_broker_type)
		{
			var dbbroker = new DbBroker(in_broker_id, in_broker_type);
			m_total_brokers.Add(dbbroker);
			return dbbroker;
		}

		//message receiver
		//시스템 디비 브로커는 UInt16.Max에서부터 하나씩 감소
		//사용자 디비 브로커는 1부터 시작해서 지정한 카운트 만큼 
		void makeDbBrokers<MESSAGE_RECEIVER_TYPE>() where MESSAGE_RECEIVER_TYPE : new()
		{
			UInt16 last_broker_id = UInt16.MaxValue;

			// log broker: 끝자락부터 
			for (UInt16 i = 0; i < Server.Config.dbWorkerCount(); ++i)
			{
				var dbbroker = createNewDbBroker(last_broker_id, DbBroker.type_e.LogDbBroker);
				dbbroker.init<MESSAGE_RECEIVER_TYPE>();
				m_log_brokers.Add(dbbroker);
				last_broker_id--;
			}

			//시스템 브로커 구간 
			for (UInt16 i = 0; i < Server.Config.dbWorkerCount(); ++i)
			{
				var dbbroker = createNewDbBroker(last_broker_id, DbBroker.type_e.SystemDbBroker);
				dbbroker.init<MESSAGE_RECEIVER_TYPE>();
				m_sys_brokers.Add(dbbroker);
				last_broker_id--;
			}

			//Rest API호출 등 별도 처리용 
			for (UInt16 i = 0; i < Server.Config.dbWorkerCount(); ++i)
			{
				var dbbroker = createNewDbBroker(last_broker_id, DbBroker.type_e.RestBroker);
				dbbroker.init<MESSAGE_RECEIVER_TYPE>();
				m_rest_brokers.Add(dbbroker);
				last_broker_id--;
			}

			// user broker 1부터 카운트 만큼 
			for (int i = 0; i < Server.Config.dbWorkerCount(); ++i)
			{
				var dbbroker = createNewDbBroker((UInt16)(i + 1), DbBroker.type_e.UserDbBroker);
				dbbroker.init<MESSAGE_RECEIVER_TYPE>();

				m_user_db_brokers.Add(dbbroker.m_broker_id, dbbroker);
			}
		}

		public List<DbBroker> totalBrokers()
		{
			return m_total_brokers;
		}

		void startDbBrokers()
		{
			foreach (var dbbroker in m_total_brokers)
			{
				dbbroker.start();
			}
		}
		// 모든 큐 처리후 종료 
		public void stopDbBrokers()
		{
			m_log.Warn("stop dbbrokers");
			foreach (var dbbroker in m_total_brokers)
			{
				dbbroker.stop();
			}
		}

		public DbBroker allocUserDbBroker()
		{
			var broker_list = new List<DbBroker>(m_user_db_brokers.Values);

			// 적게 할당한 녀석 
			broker_list = broker_list.OrderBy(info => info.m_allocated_count).ThenBy(info => info.queueCount()).ThenBy(info => info.m_broker_id).ToList();

			// 최상단에 있는 녀석을 할당 
			var db_broker = broker_list[0];
			db_broker.m_allocated_count++;
			return db_broker;
		}

		public void deallocUserDbBroker(UInt16 db_broker_id)
		{
			if (false == m_user_db_brokers.ContainsKey(db_broker_id))
			{
				m_log.WarnFormat("dbbroker:{0} is not exist. cannot dealloc", db_broker_id);
				return;
			}

			DbBroker db_broker;
			m_user_db_brokers.TryGetValue(db_broker_id, out db_broker);
			db_broker.dealloc();
		}



		public Int32 queueCount()
		{
			return m_logic_message_receiver.totalQueueCount();
		}

		// 브로커 갯수는 최대 20개 이상 넘지 않으므로 id충돌은 일어나지 않는다.  
		// system : 0
		// user : 1 + (broker_count) 
		// log : 65535 - (broker_count)
		public DbBroker.type_e dbBrokerType(UInt16 in_dbbroker_id)
		{
			// min_log_broker_id = UInt16.MaxValue - Server.Config.dbWorkerCount();
			if (UInt16.MaxValue == in_dbbroker_id) // 최대값이 키인 경우 log
			{
				return DbBroker.type_e.LogDbBroker;
			}
			else if (true == m_user_db_brokers.ContainsKey(in_dbbroker_id))
			{
				return DbBroker.type_e.UserDbBroker;
			}
			return DbBroker.type_e.SystemDbBroker; //소유자가 없는 경우 system
		}
		bool pushLogBroker(GenPacket in_packet)
		{
			if (gplat_define.db_broker_e.LogDbBroker != in_packet.m_db_broker_type)
			{
				m_log.Error($"db_broker_id{in_packet.m_db_broker_type} is not logDbBroker's");
				return false;
			}
			m_last_log_broker_idx = (m_last_log_broker_idx + 1) % Server.Config.dbWorkerCount(); //wrap arround 
			m_log_brokers[m_last_log_broker_idx].pushMessage(in_packet);
			return true;
		}
		bool pushSystemBroker(GenPacket in_packet)
		{
			if (gplat_define.db_broker_e.SystemDbBroker != in_packet.m_db_broker_type)
			{
				m_log.Error($"db_broker_id{in_packet.m_db_broker_type} is not SysDbBroker's");
				return false;
			}
			m_last_sys_broker_idx = (m_last_sys_broker_idx + 1) % Server.Config.dbWorkerCount(); //wrap arround 
			m_sys_brokers[m_last_sys_broker_idx].pushMessage(in_packet);
			return true;
		}
		bool pushRestBroker(GenPacket in_packet)
		{
			if (gplat_define.db_broker_e.RestBroker != in_packet.m_db_broker_type)
			{
				m_log.Error($"db_broker_id{in_packet.m_db_broker_type} is not RestBroker's");
				return false;
			}
			m_last_rest_broker_idx = (m_last_rest_broker_idx + 1) % Server.Config.dbWorkerCount(); //wrap arround 
			m_sys_brokers[m_last_rest_broker_idx].pushMessage(in_packet);
			return true;
		}

		bool pushUserBroker(GenPacket in_packet)
		{
			var db_broker_id = in_packet.m_packet_header.m_db_broker_id;
			if (false == m_user_db_brokers.ContainsKey(db_broker_id))
			{
				m_log.Error($"db_broker_id: {db_broker_id} is not userDbBroker's");
				return false;
			}
			m_user_db_brokers[db_broker_id].pushMessage(in_packet);
			return true;
		}
		// demux : db_req -> db_brokers_q  
		private void processMessageReceivers()
		{
			for (Int16 priority = (Int16)gplat_define.packet_priority_e.High; priority < (Int16)gplat_define.packet_priority_e._END; ++priority)
			{
				var q_size = m_logic_message_receiver.queueCount(priority);
				var packets = m_logic_message_receiver.popMessages(q_size, 0);

				foreach (var packet in packets)
				{
					if (null != packet.m_net_msg)
					{
						m_log.Debug($"{packet.m_net_msg.GetType().Name}, {packet.m_db_broker_type}");
					}

					switch (packet.m_db_broker_type)
					{
						case gplat_define.db_broker_e.UserDbBroker:
							pushUserBroker(packet);
							break;

						case gplat_define.db_broker_e.LogDbBroker:
							pushLogBroker(packet);
							break;

						case gplat_define.db_broker_e.SystemDbBroker:
							pushSystemBroker(packet);
							break;

						case gplat_define.db_broker_e.RestBroker:
							pushRestBroker(packet);
							break;

						default:
							m_log.Error($"invalid dbbroker_type:{packet.m_db_broker_type}");
							break;
					}
				}
			}
		}
		//private void processMessageReceivers()
		//{
		//	for (Int16 priority = (Int16)gplat_define.packet_priority_e.High; priority < (Int16)gplat_define.packet_priority_e._END; ++priority)
		//	{
		//		var q_size = m_logic_message_receiver.queueCount(priority);
		//		var packets = m_logic_message_receiver.popMessages(q_size, 0);
		//		foreach (var gen_packet in packets)
		//		{
		//			var db_broker_id = gen_packet.m_packet_header.m_db_broker_id;
		//			switch (dbBrokerType(db_broker_id))
		//			{
		//				case DbBroker.type_e.LogDbBroker:
		//					pushLogBroker(gen_packet);
		//					break;
		//				case DbBroker.type_e.UserDbBroker:
		//					pushUserBroker(gen_packet);
		//					break;
		//				case DbBroker.type_e.SystemDbBroker:
		//					pushSystemBroker(gen_packet);
		//					break;
		//				default:
		//					m_log.ErrorFormat("invalid dbbroker_id:{0}", db_broker_id);
		//					break;
		//			}
		//		}
		//	}
		//}

		public override void onUpdate(Int32 in_elapsed_time)
		{
			if (!m_initialized)
			{
				return;
			}
			m_db_demux_lastupdate = DateTime.UtcNow;
			processMessageReceivers();
		}
	}
}
