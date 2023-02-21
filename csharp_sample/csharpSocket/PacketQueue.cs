#if !NET35

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;


public class PacketQueue
{
	public gplat_define.packet_priority_e m_priority = gplat_define.packet_priority_e._END;

	ConcurrentQueue<GenPacket> m_queue = new ConcurrentQueue<GenPacket>();

	//큐를 모두 비움 by jogyram 2022/11/01
	public void clear()
	{
		//호환성 이슈로 dummy pop
		var cnt = m_queue.Count;
		for (Int32 i = 0; i < cnt; ++i)
		{
			pop();
		}
	}

	public void push(GenPacket message)
	{
		m_queue.Enqueue(message);
	}

	public GenPacket peek()
	{
		GenPacket out_packet = null;
		bool result = m_queue.TryPeek(out out_packet);
		if (false == result)
		{
			return null;
		}
		return out_packet;
	}

	public GenPacket pop()
	{
		GenPacket out_packet = null;
		bool result = m_queue.TryDequeue(out out_packet);
		if (false == result)
		{
			return null;
		}
		return out_packet;
	}

	public List<GenPacket> pop(Int32 count)
	{
		List<GenPacket> packets = new List<GenPacket>();
		for (int i = 0; i < count; ++i)
		{
			var gen_packet = pop();
			if (null == gen_packet)
			{
				break;
			}
			packets.Add(gen_packet);
		}
		return packets;
	}

	public Int32 count() // no lock
	{
		return m_queue.Count;
	}

	public bool isEmpty()
	{
		if (count() <= 0)
		{
			return true;
		}
		return false;
	}
}

#endif //NET462
