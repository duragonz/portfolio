using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gplat
{
	public partial class TcpSocket
		: TcpSocketType, IDisposable
	{
		public void notifySocketConnected()
		{
			var notify = new msg_gen_network.notify_socket_connected();
			try
			{
				GenPacket packet = new GenPacket();
				packet.fromNetMsg(notify.MsgInfo.MsgId, notify);
				packet.m_socket = this;

				var notified = m_message_notifier.notify(packet);
				if (!notified)
				{
					m_log.ErrorFormat($"notify_socket_connected not notified !! ID:[{packet.messageId()}]");
				}
			}
			catch (System.Exception ex)
			{
				m_log.ErrorFormat("notify {0} failed. {1}", Message.toDetail(notify.MsgInfo.MsgId), ex.ToString());
			}
		}

		public void notifySocketConnectFail(gplat.Result in_result)
		{
			var notify = new msg_gen_network.notify_socket_connect_fail();
			try
			{
				notify.MsgInfo.MsgResult = in_result.toMsgResult();
				GenPacket gen_packet = new GenPacket();
				gen_packet.fromNetMsg(notify.MsgInfo.MsgId, notify);
				gen_packet.m_socket = this;

				var notified = m_message_notifier.notify(gen_packet);
				if (!notified)
				{
					m_log.ErrorFormat("notify_socket_connect_fail not notified !!");
				}
			}
			catch (System.Exception ex)
			{
				m_log.ErrorFormat("notify {0} failed. {1}", Message.toDetail(notify.MsgInfo.MsgId), ex.ToString());
			}
		}

		public void notifyInvalidPacket(gplat.Result gen_result)
		{
			if (null == m_raw_socket)
			{
				return;
			}
			var notify = new msg_gen_network.notify_invalid_packet();
			try
			{
				notify.MsgInfo.MsgResult = gen_result.toMsgResult();

				GenPacket packet = new GenPacket();
				packet.fromNetMsg(notify.MsgInfo.MsgId, notify);
				packet.m_socket = this;

				var notified = m_message_notifier.notify(packet);
				if (!notified)
				{
					m_log.ErrorFormat("notify_socket_closed not notified !!");
				}
			}
			catch (System.Exception ex)
			{
				m_log.Error($"notify {Message.toDetail(notify.MsgInfo.MsgId)} failed. {ex.ToString()}");
			}
		}

		public void notifySocketClosed()
		{
			if (null == m_raw_socket)
			{
				return;
			}

			try
			{
				var notify = makeNotifySocketClosed();

				GenPacket packet = new GenPacket();
				packet.fromNetMsg(notify.MsgInfo.MsgId, notify);
				packet.m_socket = this;

				var notified = m_message_notifier.notify(packet);
				if (!notified)
				{
					m_log.Error($"notify_socket_closed not notified !!");
				}
			}
			catch (System.Exception ex)
			{
				m_log.Error($"Exception:{ex}");
			}
		}

		//상속받은 TcpSocket에 맞는 정보 추가 
		virtual public msg_gen_network.notify_socket_closed makeNotifySocketClosed()
		{
			var notify = new msg_gen_network.notify_socket_closed();
			notify.MsgInfo.MsgResult = m_raw_socket.m_disconnect_result.toMsgResult();

			return notify;
		}


		public void notifySendFail(GenPacket in_failed_packet, gplat.Result in_gplat_result)
		{
			var notify = new msg_gen_network.notify_send_fail();
			try
			{

				if (null != in_failed_packet)
				{
					m_send_failed_packets.add(in_failed_packet); //전송 실패 패킷을 추가함.
					notify.MessageId = (Int32)in_failed_packet.messageId();
					notify.ManageGuid = in_failed_packet.manageGuid();
				}

				notify.MsgInfo.MsgResult = in_gplat_result.toMsgResult();

				GenPacket notify_packet = new GenPacket();
				notify_packet.fromNetMsg(notify.MsgInfo.MsgId, notify);
				notify_packet.m_socket = this;

				var notified = m_message_notifier.notify(notify_packet);
				if (!notified)
				{
					m_log.FatalFormat("notify_send_fail not notified !!");
				}
			}
			catch (System.Exception ex)
			{
				// 전송오류자체는 의미가 없으므로 오류 로그를 남기지 않는다
				m_log.Info($"notify {Message.toDetail(notify.MsgInfo.MsgId)} failed. {ex.Message}");
			}
		}
	}
}
