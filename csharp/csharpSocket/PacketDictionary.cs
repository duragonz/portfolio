#if !NET35
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace gplat
{
	public class PacketDictionary
	{
		protected ConcurrentDictionary<string, GenPacket> m_dict = new ConcurrentDictionary<string, GenPacket>();

		public bool add(GenPacket gen_packet)
		{
			try
			{
				return m_dict.TryAdd(gen_packet.manageGuid(), gen_packet);
			}
			catch (System.Exception)
			{

			}
			return false;
		}

		public void clear()
		{
			m_dict.Clear();
		}

		public GenPacket pop(string manage_guid)
		{
			GenPacket gen_packet;
			remove(manage_guid, out gen_packet);
			return gen_packet;
		}

		public bool remove(string manage_guid, out GenPacket removed_gen_packet)
		{
			try
			{
				return m_dict.TryRemove(manage_guid, out removed_gen_packet);
			}
			catch (System.Exception)
			{

			}
			removed_gen_packet = null;
			return false;
		}

		public bool find(string manage_guid, out GenPacket gen_packet)
		{
			try
			{
				return m_dict.TryGetValue(manage_guid, out gen_packet);
			}
			catch (Exception)
			{

			}
			gen_packet = null;
			return false;
		}

	}
}

#endif //NET46