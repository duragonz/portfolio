using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Timers;
using System.Threading;

namespace gplat
{
	public enum sock_type_e
	{
		_BEGIN,
		standard,   //일반 네트워크 소켓 
		local_logic, //클라이언트 로컬로직 통신용 
		local_ipc,   //프로세스간 내부통신용
		_END
	}

	public class TcpSocketType
	{
		public bool m_local_connected = false;

		protected sock_type_e m_sock_type = sock_type_e._BEGIN;

		public sock_type_e sockType
		{
			get
			{
				return m_sock_type;
			}

			set
			{
				m_sock_type = value;
				if (sock_type_e.local_logic == m_sock_type)
				{
					m_local_connected = true;
				}
			}
		}
		public bool isSockType(sock_type_e in_sock_type)
		{
			return (m_sock_type == in_sock_type);
		}


		public virtual bool sendPacket(GenPacket gen_packet, bool sync_send = false)
		{
			return false;
		}

		public bool isInternal
		{
			get { return (true == m_local_connected || sock_type_e.local_logic == m_sock_type); }
		}
	}

}
