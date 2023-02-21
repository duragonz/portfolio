using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace gplat
{
	public class RawSocket : IDisposable
	{
		protected log4net.ILog m_log = gplat.Log.logger("tcp.socket.raw");
		private readonly object m_mutex = new object(); // mutex

		public string m_hostname;
		IPAddress m_ip_address; // IPAddress방식으로 교체
		public UInt16 m_port = 0;

		public DateTime m_createtime;

		protected bool m_disposed = false;
		public System.Net.Sockets.Socket m_net_socket = null;

		public gplat.Result m_disconnect_result = new gplat.Result();

		private PacketBuffer m_received_packet_buffer = new PacketBuffer();


		public RawSocket(IPAddress in_ip_address, UInt16 in_port)
		{
			m_ip_address = in_ip_address;
			m_port = in_port;
			gplat.Log.logger("socket").Info($"create socket for hostname:{in_ip_address}, post:{in_port}");

			m_net_socket = new System.Net.Sockets.Socket(m_ip_address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			setupSocket();
		}

		public RawSocket(Socket in_socket)
		{
			gplat.Log.logger("socket").Info($"create socket for Net.Socket");
			m_net_socket = in_socket;

			setupSocket();
		}
		void setupSocket()
		{
			//m_net_socket.DualMode = true;
			// set socket option
			m_net_socket.NoDelay = true;
			m_net_socket.SendBufferSize = (Int32)gplat_define.socket_e.TCP_SEND_BUFFER_SIZE;
			m_net_socket.ReceiveBufferSize = (Int32)gplat_define.socket_e.TCP_RECV_BUFFER_SIZE;
			m_net_socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
			m_net_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

			LingerOption linger_opt = new LingerOption(true, 0);
			m_net_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, linger_opt);

			//#if !LINUX_BUILD
			//			m_net_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
			//#else
			//			m_net_socket.LingerState = new LingerOption(enable: true, seconds: 0);
			//#endif

			//setLoopBackFastPath(); // try set tcp fastpath

			m_disconnect_result = new gplat.Result();

			m_createtime = DateTime.UtcNow; //소켓 생성시간

		}

		public string remoteIp()
		{
			if (null == m_net_socket)
			{
				return "";
			}
			return ((IPEndPoint)(m_net_socket.RemoteEndPoint)).Address.ToString();
		}

		public string ipAddress()
		{
			return m_ip_address.ToString();
		}

		public Int32 port()
		{
			return m_port;
		}

		private void setLoopBackFastPath()
		{
#if !NET35
			unchecked
			{
				try
				{
					Byte[] Yes = BitConverter.GetBytes(1);
					const int SIO_LOOPBACK_FAST_PATH = (int)0x98000010;
					m_net_socket.IOControl(SIO_LOOPBACK_FAST_PATH, Yes, null);
				}
				catch (Exception e)
				{
					m_log.InfoFormat("loopback_fast_path not supported: {0}", e.ToString());
				}
			}
#endif //NET35
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			lock (m_mutex) // managed object
			{
				if (m_disposed)
				{
					return;
				}
				m_disposed = true;

				// managed object
				m_received_packet_buffer.Dispose();
				//m_received_packet_buffer = null;
			}
			// unmanaged object
		}

		public EndPoint remoteEndPoint()
		{
			return m_net_socket.RemoteEndPoint;
		}
		public Socket netSocket()
		{
			return m_net_socket;
		}


		public gplat.Result beginConnect(AsyncCallback callback)
		{
			var gen_result = new gplat.Result();
			try
			{
				//m_net_socket.BeginConnect(m_ip, m_port, callback, this);
				m_net_socket.BeginConnect(m_ip_address, m_port, callback, this);
				return gen_result.setOk();
			}
			catch (ObjectDisposedException)
			{
				return gen_result.setExceptionOccurred(result.code_e.GPLAT_SOCKET_CONNECT_EXCEPTION_OCCURRED, "socket disposed already.");
			}
			catch (System.Net.Sockets.SocketException ex)
			{
				if (ex.SocketErrorCode == SocketError.IsConnected)
				{
					return gen_result.setFail(result.code_e.GPLAT_SOCKET_CONNECTED_ALREADY, $"CONNECTED ALREADY. error:{ex.ErrorCode}, native:{ex.NativeErrorCode}, socket:{ex.SocketErrorCode}");
				}
				else
				{
					return gen_result.setExceptionOccurred(result.code_e.GPLAT_SOCKET_CONNECT_SOCKET_EXCEPTION_OCCURRED, $"error:{ex.ErrorCode}, native:{ex.NativeErrorCode}, socket:{ex.SocketErrorCode}");
				}
			}
			catch (Exception e)
			{
				return gen_result.setFail(result.code_e.GPLAT_SOCKET_CONNECT_EXCEPTION_OCCURRED, e.ToString());
			}
		}

		public gplat.Result endConnect(IAsyncResult async_result)
		{
			var gen_result = new gplat.Result();
			try
			{
				var raw_socket = (RawSocket)async_result.AsyncState;
				m_net_socket.EndConnect(async_result);
				return gen_result.setOk();
			}
			catch (SocketException se)
			{
				if (se.SocketErrorCode == SocketError.ConnectionRefused)
				{
					return gen_result.setFail(result.code_e.GPLAT_SOCKET_CONNECTION_REFUSED, $"connect error:[{se.ErrorCode}:{se.SocketErrorCode}]");
				}
				else
				{
					return gen_result.setFail(result.code_e.GPLAT_CONNECT_FAIL, $"connect error:[{se.ErrorCode}:{se.SocketErrorCode}]");
				}
			}
			catch (Exception ex)
			{
				return gen_result.setExceptionOccurred(result.code_e.GPLAT_SOCKET_CONNECT_EXCEPTION_OCCURRED, ex.ToString());
			}
		}

		// begin / end는 동기식으로 동작함.
		public gplat.Result beginReceive(AsyncCallback in_callback) // try catch
		{
			var gen_result = new gplat.Result();
			// receiving state로 중복요청 방지
			lock (m_mutex)
			{
				if (m_disposed || false == m_net_socket.Connected)
				{
					m_log.Warn("disposed already");
					return gen_result.setFail($"already disposed");
				}

				try
				{
					m_net_socket.BeginReceive(m_received_packet_buffer.m_recv_buffer, 0, m_received_packet_buffer.receiveBufferSize(), SocketFlags.None, in_callback, this);
				}
				catch (System.Exception ex)
				{
					return gen_result.setExceptionOccurred(result.code_e.GPLAT_SOCKET_RECV_EXCEPTION_OCCURRED, $"socket receive error:[{ex.Message}");
				}
				return gen_result.setOk();
			}
		}
		public gplat.Result endReceive(IAsyncResult in_async_result, out Int32 in_received_size) //try catch
		{
			var gplat_result = new gplat.Result();
			lock (m_mutex)
			{
				if (m_disposed || !m_net_socket.Connected)
				{
					in_received_size = 0;
					return gplat_result.setFail($"already disposed");
				}

				in_received_size = 0;
				try
				{
					in_received_size = m_net_socket.EndReceive(in_async_result);
					if (0 == in_received_size)
					{
						return gplat_result.setFail(result.code_e.GPLAT_SOCKET_CONNECTION_RESET, "received size is zero");
					}
				}
				catch (Exception ex)
				{
					in_received_size = 0;
					return gplat_result.setExceptionOccurred(result.code_e.GPLAT_SOCKET_RECV_EXCEPTION_OCCURRED, ex.ToString());
				}

				var copied_size = m_received_packet_buffer.appendRecvBuffer(in_received_size);
				if (copied_size != in_received_size)
				{
					in_received_size = 0;
					return gplat_result.setFail($"buffer copy failed. received_size:{in_received_size}, copied_size:{copied_size}");
				}

				return gplat_result.setOk();
			}
		}


		public GenPacket takePacket(ref gplat.Result out_gen_result)
		{
			GenPacket packet = null;
			lock (m_mutex)
			{
				if (m_disposed)
				{
					return null;
				}

				packet = m_received_packet_buffer.takePacket(ref out_gen_result);
				if (null == packet)
				{
					return packet;
				}
			}

			out_gen_result = packet.decrypt();
			if (out_gen_result.fail())
			{
				return null;
			}

			return packet;

		}
		public gplat.Result sendAsync(GenPacket sending_packet, AsyncCallback in_callback)
		{
			var gplat_result = new gplat.Result();
			lock (m_mutex)
			{
				if (m_disposed)
				{
					m_log.Warn("disposed, cannot send");
					return gplat_result.setOk();
				}
			}

			try
			{
				var buffer = sending_packet.toByteBuffer();
				m_net_socket.BeginSend(buffer, 0, buffer.Length, System.Net.Sockets.SocketFlags.None, in_callback, this);
				return gplat_result.setOk();
			}
			catch (SocketException sock_ex)
			{
				gplat_result.setExceptionOccurred(result.code_e.GPLAT_SOCKET_SEND_EXCEPTION_OCCURRED, $"send socket error:[{sock_ex.ErrorCode}:{sock_ex.SocketErrorCode}]{sock_ex.Message}");
			}
			catch (Exception ex)
			{
				gplat_result.setExceptionOccurred(result.code_e.GPLAT_SOCKET_SEND_EXCEPTION_OCCURRED, $"send socket error:[{ex.ToString()}]");
			}
			return gplat_result;
		}
		public gplat.Result endSend(IAsyncResult async_result, out Int32 send_bytes)
		{
			send_bytes = 0;

			var gen_result = new gplat.Result();

			lock (m_mutex)
			{
				if (m_disposed)
				{
					m_log.Warn("disposed, cannot send");
					return gen_result.setOk();
				}
			}

			var raw_socket = (RawSocket)async_result.AsyncState;
			try
			{
				send_bytes = m_net_socket.EndSend(async_result);
				return gen_result.setOk();
			}
			catch (SocketException sock_ex)
			{
				gen_result.setExceptionOccurred(result.code_e.GPLAT_SOCKET_SEND_EXCEPTION_OCCURRED, $"send socket error:[{sock_ex.ErrorCode}:{sock_ex.SocketErrorCode}]{sock_ex.Message}");
			}
			catch (Exception ex)
			{
				gen_result.setExceptionOccurred(result.code_e.GPLAT_SOCKET_SEND_EXCEPTION_OCCURRED, $"send socket error:[{ex.ToString()}]");
			}
			return gen_result;
		}
		public gplat.Result send(GenPacket sending_packet)
		{
			var gen_result = new gplat.Result();
			lock (m_mutex)
			{
				if (m_disposed)
				{
					m_log.Warn("disposed, cannot send");
					return gen_result.setOk();
				}
			}

			try
			{
				var buffer = sending_packet.toByteBuffer();
				m_net_socket.Send(buffer, 0, buffer.Length, System.Net.Sockets.SocketFlags.None);
				return gen_result.setOk();
			}
			catch (SocketException sock_ex)
			{
				gen_result.setExceptionOccurred(result.code_e.GPLAT_SOCKET_SEND_EXCEPTION_OCCURRED, $"send socket error:[{sock_ex.ErrorCode}:{sock_ex.SocketErrorCode}]{sock_ex.Message}");
			}
			catch (Exception ex)
			{
				gen_result.setExceptionOccurred(result.code_e.GPLAT_SOCKET_SEND_EXCEPTION_OCCURRED, $"send socket error:[{ex.ToString()}]");
			}
			return gen_result;
		}

		public void close()
		{
			lock (m_mutex)
			{
				if (m_disposed)
				{
					m_log.Info("RawSocket ALREADY DISPOSED.");
					return;
				}
				try
				{
					if (m_net_socket.Connected)
					{
						m_net_socket.Shutdown(SocketShutdown.Both);
						m_net_socket.Close();
					}
				}
				catch (Exception ex)
				{
					m_log.Debug(ex.Message);
				}
				Dispose();
			}
		}
	}
}
