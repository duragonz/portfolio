using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Timers;

namespace gplat
{
	// todo: socket state 
	public class TcpConnection
		: TcpSocket
		, IConnection
	{
		//-- 멤버 --//
		public gplat_define.connection_type_e m_conn_type = gplat_define.connection_type_e._BEGIN;
		public Int32 m_connect_count = 0; // 몇번 접속 했는가?
		gplat.Result m_connect_result = new gplat.Result();


		//- virtual method --//
		public virtual void onConnectSuccess() { }
		public virtual void onConnectFail() { } // 멀티쓰레드 전용 thread safe하게 작성해야함. 

		public TcpConnection()
		{
			//기본정책 반영
			m_enable_heartbeat = m_global_enable_heartbeat;
			m_heartbeat_disconnect = m_global_heartbeat_disconnect;
		}

		public gplat.Result connect(string in_hostname, UInt16 in_port, bool in_connect_local = false)
		{
			if (in_connect_local)
			{
				m_log.Debug("connect to local");
				return connectLocal();
			}

			m_log.Debug($"connect toserver {in_hostname}:{in_port}");
			return connectAsync(in_hostname, in_port);
		}

		public override void onCreateTimers()
		{
			//m_log.Warn("set connect timer");
			m_connect_timer.setTimer(5000); // 5초
		}
		public override void onCancelTimers()
		{
			cancelConnectTimer();
		}
		public override void onProcessTimer()
		{
			if (m_connect_timer.m_active)
			{
				if (m_connect_timer.expired())
				{
					handleConnectTimer();
					m_connect_timer.reset();
				}
			}
		}
		public void setConnectTimer()
		{
			m_log.Debug("[SET CONNECT TIMER]");
			m_connect_timer.activate();
		}
		public void cancelConnectTimer()
		{
			m_connect_timer.m_active = false;
		}
		public void handleConnectTimer()
		{
			if (false == m_timer_thread_active)
			{
				return;
			}

			m_log.InfoFormat("CONNECT TIMEOUT: {0}", connectionString());
			cancelConnectTimer();
			if (false == m_sock_state.isState(sock_state_e.connected))
			{
				var result = new gplat.Result();
				handleConnectFail(result.setFail(global::result.code_e.GPLAT_SOCKET_CONNECTION_TIMEOUT, $"{connectionString()}: connect time out. no response peer."));
			}
		}

		public void handleConnectSuccess()
		{
			try
			{
				m_sock_state.setState(sock_state_e.connected);
				if (isSockType(sock_type_e.standard))
				{
					cancelConnectTimer();
					setHeartbeatTimer();
					m_connect_count++;
				}

				m_log.InfoFormat("connect success.!!");
				onConnectSuccess();

				notifySocketConnected();
				startSocket();
			}
			catch (Exception ex)
			{
				m_log.FatalFormat("Exception occurred: {0}", ex.ToString());
			}
		}

		public void handleConnectFail(gplat.Result in_result)
		{
			m_sock_state.setState(sock_state_e.disconnected);

			m_log.Debug(in_result);

			notifySocketConnectFail(in_result);
			setHeartbeatTimer(); //하트비트 전송으로 자동 재연결을 시도하도록 한다.

			onConnectFail(); //worker thread에서만 사용가능(유니티 클라이언트에서 사용하지 않음) 
		}

		protected gplat.Result connectAsync(string in_hostname, UInt16 in_port)
		{
			//standard가 아니면 로컬 연결을 수행
			switch (m_sock_type)
			{
				case sock_type_e.local_ipc:
					return connectLocalIpc();
				case sock_type_e.local_logic:
					return connectLocal();
			}

			m_log.Info($"#### connectAsync:{in_hostname}:{in_port}");
			if (m_sock_state.isState(sock_state_e.connecting))
			{
				m_log.Info("#### already connecting... just skip");
				return gplat.Result.alloc().setOk();
			}
			var gen_result = createRawSocket(in_hostname, in_port);
			if (gen_result.fail())
			{
				return gen_result;
			}
			return connectAyncImpl();
		}
		private gplat.Result connectAyncImpl()
		{
			m_log.Debug("try connect async.");
			sock_state_e old_state;
			if (false == m_sock_state.exchangeNotEqualExcept(sock_state_e.connecting, sock_state_e.connected, out old_state))
			{
				m_log.WarnFormat("connected already. just skip");
				return gplat.Result.alloc().setOk();
			}

			startTimerThread();
			cancelHeartbeatTimer();
			setConnectTimer();
			var result = m_raw_socket.beginConnect(new AsyncCallback(callbackBeginConnect));
			if (result.fail())
			{
				result.appendDesc(connectionString());
				handleConnectFail(result);
				return result;
			}
			return result.setOk();
		}

		public gplat.Result connectLocal()
		{
			m_log.Info("TRY CONNECT TO LOCAL");

			m_sock_type = sock_type_e.local_logic;
			m_local_connected = true;

			m_enable_heartbeat = false;
			m_sock_state.setState(sock_state_e.connected);
			handleConnectSuccess();

			return gplat.Result.alloc().setOk();
		}

		//버추얼서버인경우 내부서버용으로 처리 
		public gplat.Result connectLocalIpc()
		{
			m_log.InfoFormat("TRY CONNECT TO LOCAL IPC");
			m_sock_type = sock_type_e.local_ipc;
			m_local_connected = true;

			m_enable_heartbeat = false;
			m_sock_state.setState(sock_state_e.connected);
			handleConnectSuccess();

			return gplat.Result.alloc().setOk();
		}

		public override void prepareOfflineSend()
		{
			m_log.Debug("offline send occurred. try connect async");
			m_offline_sending = true;
			connectAsync(m_hostname, m_port);
		}

		private void callbackBeginConnect(IAsyncResult async_result)
		{
			cancelConnectTimer();

			var raw_socket = (RawSocket)async_result.AsyncState;
			m_log.DebugFormat("handling async connect.");
			var result = raw_socket.endConnect(async_result);
			if (result.ok())
			{
				handleConnectSuccess();
			}
			else
			{
				//if (gen_result.m_result_code != (Int32)result.code_e.GPLAT_SOCKET_CONNECT_EXCEPTION_OCCURRED)
				{
					result.appendDesc(connectionString());
					handleConnectFail(result);
				}
			}
			m_offline_sending = false;
		}
	}
}
