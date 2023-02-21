
using System;

namespace game
{
	// 기간제 아이템
	// 동일 시간에 만료
	// parentLinkTid에 연동되어서 소비된다.
	public class ItemTimeLimit : ItemBase
	{
		// 만료 날짜 Key (YYYYMMDD)
		public DateTime m_expired_date = DateTime.UtcNow.AddDays(-1);

		#region getter__setter

		public DateTime ExpiredDate => m_expired_date;
		public bool IsExpired => (ExpiredDate < DateTime.UtcNow);
		#endregion

		public override gplat.Result Setup(h_info.InstantItem in_sc_instant_item)
		{
			var gen_result = base.Setup(in_sc_instant_item);
			if (gen_result.fail())
			{
				return gen_result;
			}

			if (m_meta_item_data.TimeLimitInfo.DefaultLimitDay < 1)
			{
				return gen_result.setFail($"[TimeLimitItem] not setted TimeLimitInfo:{in_sc_instant_item.Item_tid}");
			}

			m_expired_date = gplat.TimeUtil.it.StringToDateTime(in_sc_instant_item.ExpireDate);
			m_expired_key = m_expired_date.Year * 10000 + m_expired_date.Month * 100 + m_expired_date.Day;

			return gen_result;
		}


		public bool SameExpiredKey(int in_expired_key)
		{
			return (in_expired_key == m_expired_key);
		}

		//=======================================================================
		// TimeLimit 업데이트
		//=======================================================================
		public bool ProcessTimeLimit()
		{
			if (ExpiredDate < DateTime.UtcNow)
			{
				return true;
			}

			return false;
		}

	}
}
