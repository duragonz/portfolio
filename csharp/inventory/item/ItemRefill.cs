using System;
using System.Collections.Generic;
using gplat;

namespace game
{
	// 일반 아이템
	public class ItemRefill : ItemBase, IRefillProcess
	{
		// refill 정보: nullable
		protected h_item_refill_meta.data m_meta_refill_data = null;
		// refill, reset 및 결과 데이터
		public gplat.TimeRefill m_refill = new gplat.TimeRefill();
		public gplat.TimeReset m_reset = new gplat.TimeReset();

		// 특정 패킷을 받은 이후 업데이트가 진행되어야 하는 경우 설정
		public bool IsProcessUpdate { get; set; } = true;

		// 리필 갯수 제한 //#item_meta or refill_meta에 설정되어야 한다.
		public int m_refill_max = 60;
		// 레벨에 따른 리필증가로, 설정할때 레벨정보를 갱신한다.
		private Int32 m_user_level = 1;



		#region getter__setter
		public bool Refillable
		{
			get
			{
				return (m_meta_refill_data != null && m_meta_refill_data.ComRefill.IsUse == true);
			}
		}
		public bool Resetable
		{
			get
			{
				return (m_meta_refill_data != null && m_meta_refill_data.ComReset.IsUse == true);
			}
		}

		// 리필 최대 갯수
		// UserLevel에 다른 refill 갯수 조정이 필요하면 Logic에서 구현
		//[202110] RefillMax가 무료 아이템의 최대 갯수로 적용
		public int RefillMax
		{
			get
			{
				if (m_meta_refill_data != null)
				{
					m_refill_max = Math.Max(0, m_meta_refill_data.SystemMaximumAmount) + AdditionalRefill();
				}
				return m_refill_max;
			}
			set
			{
				m_refill_max = value;
			}
		}
		public int SystemMax
		{
			get
			{
				if (m_meta_refill_data != null)
				{
					return m_meta_refill_data.SystemMaximumAmount;
				}
				return int.MaxValue;
			}
		}

		#endregion //getter__setter


		public override gplat.Result Setup(h_info.InstantItem in_h_instant_item)
		{
			var gen_result = new gplat.Result();
			gen_result = base.Setup(in_h_instant_item);
			if (gen_result.fail())
			{
				return gen_result;
			}

			SetupMetaData();
			if (Refillable)
			{
				if (in_h_instant_item.NextRefillTime.IsNullOrWhiteSpace())
				{
					DateTime next_refill_time = DateTime.UtcNow.AddMinutes(m_meta_refill_data.ComRefill.IntervalMinutes);
					in_h_instant_item.NextRefillTime = gplat.TimeUtil.it.DateTimeToString(next_refill_time);
				}
				m_refill.Initialize(in_h_instant_item.Count, in_h_instant_item.NextRefillTime, m_meta_refill_data.ComRefill.IntervalMinutes, m_meta_refill_data.ComRefill.Amount, RefillMax);
			}
			if (Resetable)
			{
				if (in_h_instant_item.NextResetTime.IsNullOrWhiteSpace())
				{
					//in_h_instant_item.NextResetTime = gplat.TimeUtil.it.NextDayResetTimeToString();
					in_h_instant_item.NextResetTime = TimeUtil.it.DateTimeToString(TimeUtil.it.TodayResetUtcTime());
				}
				m_reset.Initialize(in_h_instant_item.NextResetTime);
			}

			return gen_result.setOk();
		}
		public override gplat.Result Setup(h_info.InstantItem in_h_instant_item, BuffManager in_buffmanager)
		{
			var gen_result = new gplat.Result();
			gen_result = base.Setup(in_h_instant_item);
			if (gen_result.fail())
			{
				return gen_result;
			}

			SetupMetaData();
			SetBuff(in_buffmanager);
			if (Refillable)
			{
				if (in_h_instant_item.NextRefillTime.IsNullOrWhiteSpace())
				{
					DateTime next_refill_time = DateTime.UtcNow.AddMinutes(m_meta_refill_data.ComRefill.IntervalMinutes);
					in_h_instant_item.NextRefillTime = gplat.TimeUtil.it.DateTimeToString(next_refill_time);
				}
				m_refill.Initialize(in_h_instant_item.Count, in_h_instant_item.NextRefillTime, m_meta_refill_data.ComRefill.IntervalMinutes, m_meta_refill_data.ComRefill.Amount, RefillMax);
			}
			if (Resetable)
			{
				if (in_h_instant_item.NextResetTime.IsNullOrWhiteSpace())
				{
					//in_h_instant_item.NextResetTime = gplat.TimeUtil.it.NextDayResetTimeToString();
					in_h_instant_item.NextResetTime = TimeUtil.it.DateTimeToString(TimeUtil.it.TodayResetUtcTime());
				}
				m_reset.Initialize(in_h_instant_item.NextResetTime);
			}

			return gen_result.setOk();
		}


		protected gplat.Result SetupMetaData()
		{
			var gen_result = new gplat.Result();
			if (m_meta_item_data.RefillLinkTid > 0)
			{
				m_meta_refill_data = MetaData.byMetaId<h_item_refill_meta.data>(m_meta_item_data.RefillLinkTid);
			}

			return gen_result.setOk();
		}

		// 레벨에 따른 RefillMax의 변경 적용을 위해서 레벨설정과 Setup을 후처리 진행
		public void SetRefillMax(Int32 in_user_level)
		{
			m_user_level = in_user_level;
			Setup(m_instant_item);
		}



		//=======================================================================
		// reset, refill 업데이트
		//=======================================================================
		public bool ProcessUpdate(out RefillResult out_update_result)
		{
			out_update_result = null;
			RefillResult refill_result = null;
			bool need_update = false;

			if (false == IsProcessUpdate)
			{
				//m_log.Warn($"UserMoney[{Tid}]: UpdateMoney is not yet updated");
				return false;
			}

			// Update process
			if (Refillable && m_refill.ProcessUpdate())
			{
				var refill_point = m_refill.CalcRefillPoint(Count);
				refill_result = new RefillResult(DbId, refill_point.Point, refill_point.NextDate);
				//gplat.Log.logger("item").Debug($"Refill[{ItemTid}] Point:{refill_point.Point}, Next:{refill_point.NextDate}");
				need_update = true;
			}

			// 동시 발생과 Reset 발생에 대해서 처리하고
			// Reset이 없으면 Refill을 결과로 처리한다.
			if (Resetable && m_reset.ProcessUpdate())
			{
				var reset_point = m_reset.CalcNextReset(Count, m_meta_refill_data.ComReset.Amount + m_buff_autoplay_count, m_meta_refill_data.ComReset.SetHour, m_meta_refill_data.ComReset.SetMinute);
				if (refill_result != null)  // Refill & Reset Both
				{
					out_update_result = new RefillResult(h_define.item_refill_type_e.Both, DbId, reset_point.Point, refill_result.m_next_refill, reset_point.NextDate);
					//gplat.Log.logger("item").Debug($"Reset & Refill [{ItemTid}] Point:{reset_point.Point}, Next:{reset_point.NextDate}");
				}
				else    // only Reset
				{
					out_update_result = new RefillResult(h_define.item_refill_type_e.Reset, DbId, reset_point.Point, reset_point.NextDate);
					//gplat.Log.logger("item").Debug($"Reset[{ItemTid}] Point:{reset_point.Point}, Next:{reset_point.NextDate}");
				}
				need_update = true;
			}
			else
			{
				out_update_result = refill_result;
			}

			return need_update;
		}

		// 강제로 다음 리필시간을 현재 시간으로 부터 계산해준다.
		//> 쿨타임 초기화 아이템 사용
		public void ForceResetNextRefill(out h_info.InstantItem out_reset_result)
		{
			out_reset_result = DeepClone();
			out_reset_result.NextRefillTime = gplat.TimeUtil.it.DateTimeToString(m_refill.RefillTimeFromNow());
		}


		public Int32 AdditionalRefill()
		{
			if(m_meta_item_data.HashTag != null && m_meta_item_data.HashTag.Equals("ap"))
			{
				// 레벨별 하트 리필 개수 증가
				return GameMeta.CardLevel.H2LevelHeartIncrease(m_user_level);
			}
			return 0;
		}



		public override void SetBuff(BuffManager in_buffmanager)
		{
			m_buff_autoplay_count = 0;
			m_buff_relax_reward = 0;

			if (ItemHashtag == "autoplay" && in_buffmanager != null)
			{
				m_buff_autoplay_count = in_buffmanager.GetBuffValue(h_define.buff_type_e.AUTO_PLAY_COUNT);

			}
		}

		// ====================================================================
		// 최대갯수 보다 많이 보유했다가, 최대 갯수보다 적게 보유하는 시점이 refill 갱신 시점이다.
		//	> 사용시점에 Refill 시간의 시작을 업데이트 해야 한다.
		// ====================================================================
		public bool PrepareNextRefillUpdate(int in_consume_amount, out string out_next_refill_time)
		{
			out_next_refill_time = string.Empty;
			if (true == Refillable)
			{
				int current_count = Count;
				int refill_max_count = RefillMax;
				if (current_count >= refill_max_count)
				{
					if ((current_count - in_consume_amount) < refill_max_count)
					{
						out_next_refill_time = gplat.TimeUtil.it.DateTimeToString(m_refill.RefillTimeFromNow());
						return true;
					}
				}
			}
			return false;
		}

		// ====================================================================
		// 최대 갯수까지 남은 시간
		//> for 로컬푸시 (대략적인 시간과, 정확한 시간 구분)
		// ====================================================================
		public bool GetMaxRefillWaitingTime(out int out_wait_seconds)
		{
			out_wait_seconds = 0;
			if (true == Refillable)
			{
				int total_count = FreeCount();
				int max_count = RefillMax;
				if (total_count < max_count)
				{
					int need_refill_count = max_count - total_count;
					// 대략적인 시간
					out_wait_seconds = need_refill_count * m_refill.Interval * 60;
				}
				return true;
			}
			return false;
		}

		public bool GetMaxRefillWaitingTimeEx(out int out_wait_seconds)
		{
			out_wait_seconds = 0;
			if (true == Refillable)
			{
				int total_count = FreeCount();
				int max_count = RefillMax;
				if (total_count < max_count)
				{
					int need_refill_count = max_count - total_count;
					// 정확한 시간
					out_wait_seconds = (int)(m_refill.NextRefillDate - DateTime.UtcNow).TotalSeconds + (need_refill_count - 1) * m_refill.Interval * 60;
				}
				return true;
			}
			return false;
		}


		public override string ToString()
		{
			return string.Format($"[{DbId}:{ItemTid}] refill:{Refillable}, reset:{Resetable} free:{FreeCount()} totalcount:{Count}, nextRefill:{ItemInfo.NextRefillTime}, nextReset:{ItemInfo.NextResetTime}, Buff:{m_buff_relax_reward}");
		}
	}



	// ===================================================================
	// DB 저장을 위한 Refill, Reset 정보를 저장한다.
	// ===================================================================
	public class RefillResult
	{
		public h_define.item_refill_type_e m_type = h_define.item_refill_type_e.Refill;
		public long m_db_id;
		public int m_add_count;
		public string m_next_refill;
		public string m_next_reset;

		public RefillResult(long in_db_id, int add_count, DateTime next_update)
		{
			m_db_id = in_db_id;
			m_add_count = add_count;
			m_next_refill = gplat.TimeUtil.it.DateTimeToString(next_update);
		}
		public RefillResult(h_define.item_refill_type_e in_type, long in_db_id, int add_count, DateTime next_reset)
		{
			m_type = in_type;
			m_db_id = in_db_id;
			m_add_count = add_count;
			m_next_reset = gplat.TimeUtil.it.DateTimeToString(next_reset);
		}
		public RefillResult(h_define.item_refill_type_e in_type, long in_db_id, int add_count, string next_refill, DateTime next_reset)
		{
			m_type = in_type;
			m_db_id = in_db_id;
			m_add_count = add_count;
			m_next_refill = next_refill;
			m_next_reset = gplat.TimeUtil.it.DateTimeToString(next_reset);
		}
	}

}
