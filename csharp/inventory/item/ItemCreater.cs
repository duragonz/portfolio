using System;
using System.Collections.Generic;
namespace gplat
{
	//for project H 
	public partial class RewardCreater
	{
		// ================================================================
		// Project H2 Reward
		// ================================================================
		// Make MetaItem
		public static h_info.MetaItem MakeH2MetaReward(h_define.item_action_e in_action_type, Int64 in_tid, Int32 in_count, h_define.item_tier_e in_tier = h_define.item_tier_e._NONE, int in_tier_level = 0, int in_level = 1, int in_timelimit_day = 0)
		{
			return new h_info.MetaItem()
			{
				ActionType = in_action_type,
				ItemTid = in_tid,
				Count = in_count,
				ItemTier = in_tier,
				TierLevel = in_tier_level,
				CreateLevel = in_level,
				TimeLimitDay = in_timelimit_day
			};
		}
		public static h_info.MetaItem MakeH2MetaReward(h_define.item_action_e in_action_type, Int64 in_tid, Int32 in_count, int limit_day)
		{
			return new h_info.MetaItem()
			{
				ActionType = in_action_type,
				ItemTid = in_tid,
				Count = in_count,
				ItemTier = h_define.item_tier_e._NONE,
				TierLevel = 0,
				CreateLevel = 1,
				TimeLimitDay = limit_day,
			};
		}
		public static h_info.MetaItem MakeH2MetaReward(h_info.RangeItem in_range_item)
		{
			return new h_info.MetaItem()
			{
				ActionType = in_range_item.ActionType,
				ItemTid = in_range_item.ItemTid,
				Count = gplat.Random.Range(in_range_item.MinCount, in_range_item.MaxCount),
				ItemTier = h_define.item_tier_e._NONE,
				TierLevel = 0,
				CreateLevel = in_range_item.CreateLevel,
				TimeLimitDay = in_range_item.TimeLimitDay,
			};
		}
		public static h_info.MetaItem MakeH2MetaReward(h_info.MetaCountItem in_item)
		{
			return new h_info.MetaItem()
			{
				ActionType = h_define.item_action_e.Item,
				ItemTid = in_item.ItemTid,
				Count = in_item.Count,
				ItemTier = h_define.item_tier_e._NONE,
				TierLevel = 0,
				CreateLevel = 0,
				TimeLimitDay = 0,
			};
		}

		// Make Action
		public static h_info.ItemAction MakeH2ItemAction(h_define.item_action_e in_action_type, Int64 in_db_id, Int64 in_tid, Int32 in_count, h_define.item_tier_e in_tier = h_define.item_tier_e._NONE, int in_tier_level = 0, int in_level = 1)
		{
			return new h_info.ItemAction()
			{
				ActionType = in_action_type,
				TargetDbid = in_db_id,
				ItemTid = in_tid,
				Count = in_count,
				ItemTier = in_tier,
				TierLevel = in_tier_level,
				CreateLevel = in_level,
			};
		}

		// 치트에서 아이템 생성시 사용
		public static h_info.InstantItem CheatMakeH2Item(Int64 in_item_tid, h_define.item_tier_e in_tier = h_define.item_tier_e._NONE, int in_tier_level = 0, bool in_use_dbid = true)
		{
			var meta_item_h_data = MetaData.byMetaId<h_item_meta.data>(in_item_tid);
			if (null == meta_item_h_data)
			{
				gplat.Log.logger("item").Error($"no item meta tid:{in_item_tid}");
				return null;
			}

			Int64 db_id = 0;
			if (in_use_dbid)
			{
				db_id = GameMeta.Item.NextItemDbId();
			}

			var new_item = new h_info.InstantItem()
			{
				Db_id = db_id,
				Item_tid = in_item_tid,
				ItemType = meta_item_h_data.ItemType,
				ItemTier = in_tier,
				TierLevel = in_tier_level,
				Count = 1,
				Lv = 0,
				Exp = 0,
			};

			return new_item;
		}




		// ItemDbUpdater에서 아이템 생성시 사용
		//> ActionItem 정보를 통해서 DB저장용 InstantItem Action 생성
		public static h_info.InstantItem MakeH2ActionItem(h_info.ItemAction in_item_action)
		{
			var item_meta_data = MetaData.byMetaId<h_item_meta.data>(in_item_action.ItemTid);
			if (item_meta_data == null)
			{
				gplat.Log.logger("app").Error($"[Item] Cant find h_item_meta:{in_item_action.ItemTid}");
				return null;
			}
			h_info.InstantItem new_item = null;
			switch (in_item_action.ActionType)
			{
				case h_define.item_action_e.Replace:
				case h_define.item_action_e.Item:
					new_item = new h_info.InstantItem()
					{
						Db_id = in_item_action.TargetDbid,
						Item_tid = in_item_action.ItemTid,
						ItemType = item_meta_data.ItemType,
						ItemTier = in_item_action.ItemTier,
						TierLevel = in_item_action.TierLevel,
						Lv = in_item_action.CreateLevel,
						Count = in_item_action.Count,
					};
					SetLimitTime(ref new_item, in_item_action.TimeLimitDay, item_meta_data.TimeLimitInfo);
					break;
				case h_define.item_action_e.CardExp:
					new_item = new h_info.InstantItem()
					{
						Db_id = in_item_action.TargetDbid,
						Item_tid = in_item_action.ItemTid,
						ItemType = item_meta_data.ItemType,
						Exp = in_item_action.Count,
					};
					break;
				case h_define.item_action_e.RandomItem:
				case h_define.item_action_e.PackageItem:
					break;
			}

			return new_item;
		}

		// MetaItem 정보를 통해서 DB저장용 InstantItem Action 생성
		// 최초 생성 또는 Meta 설정을 통한 아이템 정보
		public static h_info.InstantItem MakeH2CreateItem(h_info.MetaItem in_item_action, bool use_dbid = true)
		{
			h_info.InstantItem new_item = null;

			// ActionType이 Item 일때만 생성한다.
			if (in_item_action.ActionType == h_define.item_action_e.Item)
			{
				var item_meta_data = MetaData.byMetaId<h_item_meta.data>(in_item_action.ItemTid);
				// Client에서 DBID없이 MetaItem으로 생성 사용을 위해 추가
				Int64 db_id = use_dbid ? GameMeta.Item.NextItemDbId() : 0;
				new_item = new h_info.InstantItem()
				{
					Db_id = db_id,
					Item_tid = in_item_action.ItemTid,
					ItemType = item_meta_data.ItemType,
					ItemTier = in_item_action.ItemTier,
					TierLevel = in_item_action.TierLevel,
					Lv = in_item_action.CreateLevel,
					Count = in_item_action.Count,
					//BaseGrade = h_define.item_grade_e._NONE,
					//BattleAttribute = h_define.battle_attribute_e._NONE,
				};
				// 아이템 타입에 따른 추가정보
				SetLimitTime(ref new_item, in_item_action.TimeLimitDay, item_meta_data.TimeLimitInfo);
				SetRefillReset(ref new_item, item_meta_data);
			}
			return new_item;
		}

		// Pvp deploy에서 BotItem 생성
		// DeckNo = 1
		public static h_info.InstantItem MakeH2DeckItem(Int64 in_char_uid, Int64 in_item_tid, h_define.item_tier_e in_tier, Int32 in_tier_level, Int32 in_item_level)
		{
			var meta_item_h_data = MetaData.byMetaId<h_item_meta.data>(in_item_tid);
			if (null == meta_item_h_data)
			{
				gplat.Log.logger("item").Error($"no item meta tid:{in_item_tid}");
				return null;
			}

			var new_item = new h_info.InstantItem()
			{
				Db_id = GameMeta.Item.NextItemDbId(),
				Char_uid = in_char_uid,
				Item_tid = in_item_tid,
				ItemType = meta_item_h_data.ItemType,
				ItemTier = in_tier,
				TierLevel = in_tier_level,
				Count = 1,
				Lv = in_item_level,
				Exp = 0,
				DeckNo = 1,
			};

			return new_item;
		}


		// ================================================================
		// Project H & H2 공용 로직
		// ================================================================

		// Tid: 3407024, 3407025, 3407026, 3407027
		// MidNight : UTD 15:00 at Korea
		public static void SetLimitTime(ref h_info.InstantItem in_item, int in_day, h_item_meta.TimeLimitInfo in_timelimit_info)
		{
			if (in_item.ItemType != h_define.item_type_e.TimeLimitItem || in_timelimit_info == null)
			{
				return;
			}
			if (in_day == 0)
			{
				in_day = in_timelimit_info.DefaultLimitDay;
			}
			DateTime limit_date = TimeUtil.it.GetResetUtcTime(in_day, gplat.ConfigMeta.m_h_timelimit_reset_hour, gplat.ConfigMeta.m_h_timelimit_reset_min);
			// Set
			in_item.ExpireDate = TimeUtil.it.DateTimeToString(limit_date);
		}
		// 치트에서는 오늘만료 아이템을 설정할 수 있도록 한다.
		public static void SetLimitTimeForCheat(ref h_info.InstantItem in_item, int in_day)
		{
			if (in_item.ItemType != h_define.item_type_e.TimeLimitItem)
			{
				return;
			}
			DateTime limit_date = TimeUtil.it.GetResetUtcTime(in_day, gplat.ConfigMeta.m_h_timelimit_reset_hour, gplat.ConfigMeta.m_h_timelimit_reset_min);
			// Set
			in_item.ExpireDate = TimeUtil.it.DateTimeToString(limit_date);
		}

		public static void SetRefillReset(ref h_info.InstantItem in_item, h_item_meta.data in_item_meta_data)
		{
			if (in_item_meta_data.ItemType == h_define.item_type_e.RelaxReward)
			{
				in_item.NextResetTime = gplat.TimeUtil.it.DateTimeToString(DateTime.UtcNow);
			}
			else if (in_item_meta_data.ItemType == h_define.item_type_e.UserMoney)
			{
				if (in_item_meta_data.RefillLinkTid > 0)
				{
					var meta_h_item_refill_data = MetaData.byMetaId<h_item_refill_meta.data>(in_item_meta_data.RefillLinkTid);
					if (null != meta_h_item_refill_data)
					{
						if (meta_h_item_refill_data.ComRefill.IsUse)
						{
							in_item.NextRefillTime = gplat.TimeUtil.it.DateTimeToString(DateTime.UtcNow.AddMinutes(meta_h_item_refill_data.ComRefill.IntervalMinutes));
						}

						if (meta_h_item_refill_data.ComReset.IsUse)
						{
							in_item.NextResetTime = gplat.TimeUtil.it.DateTimeToString(DateTime.UtcNow);
						}
					}
				}
			}
		}



		// 통합 보상 정보
		public static h_info.MetaItem MakeHMetaReward(h_info.MetaItem in_h_meta_item_info, Int32 in_count)
		{
			h_info.MetaItem h_meta_reward_info = in_h_meta_item_info.DeepClone() as h_info.MetaItem;
			h_meta_reward_info.Count = in_count;
			return h_meta_reward_info;
		}


		// 패키지
		// h_shop_package_meta에서 이미 적용되어 있다.
		public static gplat.Result MakePackageReward(Int64 in_tid, out List<h_info.MetaItem> out_item_infos)
		{
			out_item_infos = new List<h_info.MetaItem>();
			var gen_result = new gplat.Result();

			var item_package_meta_data = MetaData.byMetaId<h_shop_package_meta.data>(in_tid);
			if (item_package_meta_data == null)
			{
				return gen_result.setFail(result.code_e.NO_META_DATA, $"package_item_tid:{in_tid}");
			}
			foreach (var h_meta_item_info in item_package_meta_data.PackItemList)
			{
				out_item_infos.Add(h_meta_item_info);
			}

			return gen_result.setOk();
		}


		//#TODO: origin 설정 및 등급의 기준이 레벨이 아니어서 새로 구성해야함.
		// bot_card의 pool 부터 전체적으로 재구성 필요
		// 일단은 기존 로직을 사용하도록 함.
		public static void SetCardDetailH2ByLevel(ref h_info.InstantItem in_item, int in_level)
		{
			if (in_item.ItemType == h_define.item_type_e.PlayerCard || in_item.ItemType == h_define.item_type_e.PetCard)
			{
				if (in_level < 1)
				{
					in_level = 1;
				}
				var level_meta = GameMeta.CardLevel.H2LevelInfo(in_level);
				if (level_meta != null)
				{
					in_item.Exp = level_meta.BeginExp;
					in_item.Lv = in_level;
				}
			}
		}
	}

}


public static class ItemExtention
{
	public static void SetUpdate(this h_info.InstantItem in_item_info, h_info.InstantItem set_item_info)
	{
		in_item_info.Db_id = set_item_info.Db_id;
		in_item_info.Char_uid = set_item_info.Char_uid;
		in_item_info.Item_tid = set_item_info.Item_tid;
		in_item_info.ItemType = set_item_info.ItemType;
		in_item_info.Count = set_item_info.Count;
		in_item_info.Exp = set_item_info.Exp;
		in_item_info.Lv = set_item_info.Lv;
		in_item_info.DeckNo = set_item_info.DeckNo;
		in_item_info.ItemLock = set_item_info.ItemLock;
		in_item_info.NextRefillTime = set_item_info.NextRefillTime;
		in_item_info.NextResetTime = set_item_info.NextResetTime;
		in_item_info.ExpireDate = set_item_info.ExpireDate;
	}
}
