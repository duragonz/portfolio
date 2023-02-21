using game;

using gplat;

using System;
using System.Collections.Generic;

public static class ItemBaseExt
{
	public static bool IsNullOrEmpty(this ItemBase in_item_base)
	{
		return (in_item_base == null || in_item_base.ItemTid == 0);
	}
	public static bool NotEmpty(this ItemBase in_item_base)
	{
		return (false == in_item_base.IsNullOrEmpty());
	}

}

namespace game
{
	public static class Stringify
	{
		public static string LogString(this h_info.ItemAction in_action)
		{
			var name = "";
			if (h_define.item_action_e.Item == in_action.ActionType)
			{
				var meta_item_data = MetaData.byMetaId<h_item_meta.data>(in_action.ItemTid);
				if (null != meta_item_data)
				{
					name = $":{meta_item_data.Meta_name}";
				}
			}
			return $"{in_action.ActionType} [{in_action.TargetDbid}:{in_action.ItemTid}{name}] count[{in_action.Count}]  tier[{in_action.ItemTier}] tierlevel[{in_action.TierLevel}]\n";
		}
	}

	public abstract class ItemBase
	{
		//- static data 
		protected h_item_meta.data m_meta_item_data = new h_item_meta.data();

		//- dynamic data : h_info.instantItem : 덱, 메타테이블 정보 등 세팅  
		public h_info.InstantItem m_instant_item = new h_info.InstantItem();

		// ConsumeGroupInfo
		protected int m_expired_key = 0;
		public Dictionary<long, ItemBase> m_consume_groups = new Dictionary<long, ItemBase>();

		// Buff 관련 : 많아지면 dictionary화
		protected int m_buff_relax_reward = 0;
		protected int m_buff_autoplay_count = 0;

		#region getter__setter

		public h_info.InstantItem ItemInfo => m_instant_item;
		public Int64 DbId => m_instant_item.Db_id;
		public Int64 ItemTid => m_instant_item.Item_tid;
		public string ItemName => m_meta_item_data.LSTR("Meta_name");
		public string ItemHashtag => m_meta_item_data.HashTag;
		public string SortTag
		{
			get
			{
				if(string.IsNullOrEmpty(m_meta_item_data.SortTag) == false)
				{
					return m_meta_item_data.SortTag;
				}
				else
				{
					return m_meta_item_data.Meta_id.ToString();
				}
			}
		}
		public Int32 HaveLimitCount => m_meta_item_data.HaveLimit;
		public virtual string Image => m_meta_item_data.Meta_id.ToString();

		public Int64 ItemLinkTid => m_meta_item_data.ItemLinkTid;
		public Int64 ParentLinkTid => m_meta_item_data.ParentLinkTid;
		public bool HasConsumeGroups => (m_consume_groups.Count > 0);
		public List<h_item_meta.ConsumeGroupInfo> MetaComsumeGroupInfo => m_meta_item_data.LinkGroups;
		public bool HasRefill => (m_meta_item_data.RefillLinkTid > 0);


		public Int64 CharTid => m_meta_item_data.CharTid;

		public Int32 Count => TotalCount();
		public string ExpireDateStr => m_instant_item.ExpireDate;

		public string CountString => Count.ToString("N0");


		// Level, Exp는 CharExp 아이템에서만 사용된다.
		public Int32 Level => m_instant_item.Lv;
		public Int32 Exp => m_instant_item.Exp;

		public bool IsValid => m_instant_item.Item_tid > 0;


		public h_define.item_type_e ItemType => m_instant_item.ItemType;

		// 등급 관련
		public h_define.item_tier_e ItemTier
		{
			get
			{
				if (ItemType == h_define.item_type_e.PetCard || ItemType == h_define.item_type_e.PlayerCard || ItemType == h_define.item_type_e.FusionMaterialCard)
				{
					if (m_instant_item.ItemTier == h_define.item_tier_e._NONE)
					{
						return h_define.item_tier_e.Normal;
					}
					return m_instant_item.ItemTier;
				}
				return h_define.item_tier_e._NONE;
			}
		}
		public Int32 TierLevel => m_instant_item.TierLevel;

		public Int32 DeckNo => m_instant_item.DeckNo;

		public string MetaName
		{
			get
			{
				if (m_meta_item_data == null)
				{
					return "";
				}
				return m_meta_item_data.LSTR("Meta_name");
			}
		}
		public string MetaNote
		{
			get
			{
				if (m_meta_item_data == null)
				{
					return "";
				}
				return m_meta_item_data.LSTR("Meta_note");
			}
		}

		#endregion //getter__setter

		public bool IsItemType(h_define.item_type_e in_item_type)
		{
			return (ItemType == in_item_type);
		}
		public bool IsHashTag(string in_tag)
		{
			if (false == m_meta_item_data.HashTag.IsNullOrWhiteSpace())
			{
				return m_meta_item_data.HashTag.Equals(in_tag);
			}
			return false;
		}

		//자동변환 
		public static implicit operator h_info.InstantItem(ItemBase in_item_base)
		{
			return in_item_base.m_instant_item;
		}
		public h_info.InstantItem DeepClone()
		{
			return m_instant_item.DeepClone() as h_info.InstantItem;
		}

		public game.ItemCard DeepCloneCard()
		{
			game.ItemCard card = new ItemCard();
			card.Setup(DeepClone());
			return card;
		}

		public game.ItemNormal DeepCloneNormal()
		{
			game.ItemNormal normal = new ItemNormal();
			normal.Setup(DeepClone());
			return normal;
		}



		//플레이어 카드 정보 세팅 
		public virtual gplat.Result Setup(h_info.InstantItem in_instant_item)
		{
			m_instant_item = in_instant_item;
			return SetupMetaData();
		}
		public virtual gplat.Result Setup(h_info.InstantItem in_instant_item, BuffManager in_buffmanager)
		{
			m_instant_item = in_instant_item;
			return SetupMetaData();
		}

		private gplat.Result SetupMetaData()
		{
			var gen_result = new gplat.Result();

			//아이템 메타 세팅 
			m_meta_item_data = MetaData.byMetaId<h_item_meta.data>(ItemTid);
			if (null == m_meta_item_data)
			{
				return gen_result.setFail(result.code_e.NO_META_DATA, $"no meta data item Tid:{ItemTid}");
			}
			return gen_result.setOk();
		}

		//=======================================================================
		// 유료 재화가 설정되어 있는 경우 유료재화 연결
		//=======================================================================
		public bool SetupConsumeGroup(ItemBase in_item)
		{
			//if (m_consume_groups.ContainsKey(in_item.DbId))
			ItemBase already_have = null;
			if (m_consume_groups.TryGetValue(in_item.DbId, out already_have))
			{
				return false;
			}
			m_consume_groups.Add(in_item.DbId, in_item);
			return true;
		}
		public bool RemoveConsumeGroup(long in_item_dbid)
		{
			return m_consume_groups.Remove(in_item_dbid);
		}


		public virtual void CommitUpdater(h_info.InstantItem in_db_update_info, bool in_update_refill = false)
		{
			// 증감 처리
			//ItemInfo.Hp += in_db_update_info.Hp;
			ItemInfo.Exp += in_db_update_info.Exp;
			ItemInfo.Count += in_db_update_info.Count;
			// 셋 처리
			ItemInfo.Lv = in_db_update_info.Lv;
			ItemInfo.ItemTier = in_db_update_info.ItemTier;
			ItemInfo.TierLevel = in_db_update_info.TierLevel;

			if (in_update_refill)
			{
				ItemInfo.NextRefillTime = in_db_update_info.NextRefillTime;
			}
		}

		public virtual void CommitNextReset(h_info.InstantItem in_db_update_info)
		{
			ItemInfo.NextResetTime = in_db_update_info.NextResetTime;
		}
		public virtual void CommitItemTier(h_info.InstantItem in_db_update_info)
		{
			ItemInfo.ItemTier = in_db_update_info.ItemTier;
			ItemInfo.TierLevel = in_db_update_info.TierLevel;
		}

		public virtual int TotalCount()
		{
			return (FreeCount() + ConsumeGroupCount());
		}
		public virtual int FreeCount()
		{
			return ItemInfo.Count;
		}

		public virtual int PaidCount()
		{
			//[2021.10 TimeLimitItem]
			foreach (var each_item in m_consume_groups.Values)
			{
				if (each_item.ItemType == h_define.item_type_e.PayedItem)
				{
					return each_item.Count;
				}
			}
			return 0;

		}

		public virtual int ConsumeGroupCount()
		{
			int count_sum = 0;
			foreach (var each_item in m_consume_groups.Values)
			{
				count_sum += each_item.Count;
			}
			return count_sum;
		}

		public virtual List<ItemBase> ConsumeGroupItems()
		{
			List<ItemBase> out_result = new List<ItemBase>();
			foreach (var each_item in m_consume_groups.Values)
			{
				if (each_item.TotalCount() > 0)
				{
					out_result.Add(each_item);
				}
			}
			if (out_result.Count > 0)
			{
				out_result.Sort((item_a, item_b) => item_a.ExpiredKey().CompareTo(item_b.ExpiredKey()));
			}
			return out_result;
		}
		public virtual bool IsConsumeGroupTid(Int64 in_tid)
		{
			foreach (var each_item in m_consume_groups.Values)
			{
				if (each_item.ItemTid == in_tid)
				{
					return true;
				}
			}
			return false;
		}

		// 경험치 획득에 따른 레벨 계산
		public void GainExpToLevel(Int32 in_gain_exp, out Int32 out_level)
		{
			int calc_exp = Math.Max(Exp + in_gain_exp, 0);
			var calc_item_level_meta = GameMeta.CardLevel.H2LevelInfoWithExp(calc_exp);
			out_level = calc_item_level_meta.Level;
		}

		// 누적 경험치 레벨업
		public void CommitExpLevel(h_info.CharacterLevelChange in_level_change)
		{
			// Exp가 모두 Commit되고 나서 계산되는것이어서 현재 Exp와 AfterLevel 체크
			var calc_item_level_meta = GameMeta.CardLevel.H2LevelInfoWithExp(Exp);
			if (calc_item_level_meta.Level != in_level_change.AfterLevel)
			{
				// 이상한 경우
			}
			m_instant_item.Lv = in_level_change.AfterLevel;

		}


		public virtual int ExpiredKey()
		{
			return m_expired_key;
		}

		public virtual int TodayExpiredCount()
		{
			int today_key = TimeUtil.it.GetResetKey(0, gplat.ConfigMeta.m_h_timelimit_reset_hour, gplat.ConfigMeta.m_h_timelimit_reset_min);
			int today_expired_sum = 0;
			foreach (var each_item in m_consume_groups.Values)
			{
				if (today_key == each_item.ExpiredKey())
				{
					today_expired_sum += each_item.Count;
				}
			}
			return today_expired_sum;
		}
		public virtual List<ItemTimeLimit> TodayExpiredList()
		{
			int today_key = TimeUtil.it.GetResetKey(0, gplat.ConfigMeta.m_h_timelimit_reset_hour, gplat.ConfigMeta.m_h_timelimit_reset_min);
			List<ItemTimeLimit> today_expired_list = new List<ItemTimeLimit>();
			foreach (var each_item in m_consume_groups.Values)
			{
				if (today_key == each_item.ExpiredKey())
				{
					today_expired_list.Add(each_item as ItemTimeLimit);
				}
			}
			return today_expired_list;
		}

		public virtual bool IsEnough(Int32 in_need_count)
		{
			return (TotalCount() >= in_need_count);
		}

		public virtual Int32 GainCountWithLimit(Int32 in_add_count)
		{
			if(HaveLimitCount < Count + in_add_count)
			{
				// 최대 보유 카운트에서 현재 보유카운트를 뺀 개수
				return HaveLimitCount - Count;
			}
			return in_add_count;
		}


		public virtual int WorthValue(h_define.enchant_ingredient_e in_type)
		{
			return 0;
		}



		public virtual void SetBuff(BuffManager in_buffmanager)
		{
		}

		// 동일 등급 아이템인지 체크
		//> PVP 덱 저장에서 체크
		public virtual bool IsSameTierItem(h_info.InstantItem in_item)
		{
			if (in_item.Item_tid == m_instant_item.Item_tid &&
				in_item.ItemTier == m_instant_item.ItemTier &&
				in_item.TierLevel == m_instant_item.TierLevel &&
				in_item.Lv == m_instant_item.Lv)
			{
				return true;
			}
			return false;
		}

	}

	// refill이 필요한 아이템 Interface
	interface IRefillProcess
	{
		bool ProcessUpdate(out RefillResult out_update_result);

	}

}
