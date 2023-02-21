using System;
using System.Collections.Generic;
using System.Text;
using game;
using gplat;
using log4net;

namespace logic
{
	//=======================================================================
	// 전투 및 아이템 관련 동작에 대해 DB 저장 및 결과 정보 생성
	// ** h_info.MetaItem, h_info.ItemAction 타입의 입력 처리
	// 갱신대상
	//	- 증감 : Hp, Exp, Count, 
	//	- Set : Lv
	// 사용법
	//	Step1 생성: 
	//				stage_reward = new ItemDbUpdater();
	//	Step2 ItemAction이나 MetaItem 저장 정보 추가: 
	//				stage_reward.AppendAction(m_user, heart_consume_action);
	//				+ AppendActions...
	//	Step3 DB저장용 Merge(DBID기반으로 머징): 
	//				stage_reward.MergeAction();
	//	Step4 DB저장 use m_db_merge_items : 
	//				var db_req = new server_msg_db.req_h2_item_upsert_json_gameuser();
	//				db_req.In_json_item_infos.AddRange(m_db_merge_items);
	//	Step5 DB저장 내용 Commit: 
	//				m_inventory.UpsertItems(m_reward_items.ResultItem);
	//=======================================================================
	public class ItemDbUpdater
	{
		public delegate Int64 ReplaceTidProcessor();

		//[2021.10 TimeLimitItem]
		// Action 정보
		public List<h_info.ItemAction> m_in_action_infos = new List<h_info.ItemAction>();
		// MergeAction 전까지 누적된
		public List<h_info.InstantItem> m_action_to_items = new List<h_info.InstantItem>();
		// 전체 Action 정보를 해석해서 생성한 Item 정보
		public List<h_info.InstantItem> m_all_items = new List<h_info.InstantItem>();
		// DB 저장 정보
		public List<h_info.UpdaterSaveInfo> m_db_save_items = new List<h_info.UpdaterSaveInfo>();

		private HWeightDbRandomItemUpdater m_weight_db_random_item_updater = null;

		private gplat.Result m_gen_result = new gplat.Result();
		protected ILog m_log = gplat.Log.logger("user.updater");

		#region getter__setter

		// 저장할 정보가 있는지 여부
		public bool hasSavedData
		{
			get
			{
				return m_all_items.Count > 0;
			}
		}

		// Merge 하기전까지 쌓인 AppendAction 아이템 목록
		public List<h_info.InstantItem> AppendedItems => m_action_to_items;

		#endregion //getter__setter

		//=======================================================================
		// MetaItem 타입으로 증가, 감소
		// 갱신대상
		//	- 증감 : Hp, Exp, Count, 
		//	- Set : Lv
		//=======================================================================
		public gplat.Result AppendAction(LobbyUser in_lobby_user, List<h_info.ItemAction> in_h_item_actions)
		{
			foreach (var h_item_action in in_h_item_actions)
			{
				var gen_result = AppendAction(in_lobby_user, h_item_action);
				if (gen_result.fail())
				{
					return gen_result;
				}
			}
			return m_gen_result.setOk();
		}
		public gplat.Result AppendAction(LobbyUser in_lobby_user, h_info.ItemAction in_h_item_action, ReplaceTidProcessor in_replace_tid_processor = null)
		{
			bool regist_action = false;
			h_info.InstantItem action_item = null;

			// 저장 갯수가 없는 경우는 저장하지 않는다.
			if (in_h_item_action.Count == 0)
			{
				return m_gen_result.setOk();
			}

			switch (in_h_item_action.ActionType)
			{
				case h_define.item_action_e.Item:
					{
						action_item = gplat.RewardCreater.MakeH2ActionItem(in_h_item_action);
						if (action_item == null)
						{
							return m_gen_result.setFail(result.h_code_e.ITEM_INVALID_DATA, $"Cant find item: {in_h_item_action}");
						}
						// DB 저장 정보 생성 (업데이트 또는 새로운 아이템 생성)
						if (MakeOrUpdateItemDbid(in_lobby_user, ref in_h_item_action, ref action_item))
						{
							regist_action = RegistDbSaveItemWithPrority(in_lobby_user, in_h_item_action, action_item);
						}
					}
					break;

				case h_define.item_action_e.CardExp:
					{
						var have_item = in_lobby_user.m_h_inventory.FindExpItem();
						if (have_item != null)
						{
							// 경험치 증가
							have_item.GainExpToLevel(in_h_item_action.Count, out int out_level);
							in_h_item_action.TargetDbid = have_item.DbId;
							action_item = gplat.RewardCreater.MakeH2ActionItem(in_h_item_action);
							if (action_item == null)
							{
								return m_gen_result.setFail(result.h_code_e.ITEM_INVALID_DATA, $"Cant find item: {in_h_item_action}");
							}
							action_item.Lv = out_level;
							regist_action = RegistDbSaveItem(in_h_item_action, action_item);
						}
					}
					break;
				case h_define.item_action_e.Replace:
					{
						if (in_replace_tid_processor != null)
						{
							in_h_item_action.ItemTid = in_replace_tid_processor.Invoke();

							action_item = gplat.RewardCreater.MakeH2ActionItem(in_h_item_action);
							if (action_item == null)
							{
								return m_gen_result.setFail(result.h_code_e.ITEM_INVALID_DATA, $"Cant find item: {in_h_item_action}");
							}
							// DB 저장 정보 생성 (업데이트 또는 새로운 아이템 생성)
							if (MakeOrUpdateItemDbid(in_lobby_user, ref in_h_item_action, ref action_item))
							{
								regist_action = RegistDbSaveItemWithPrority(in_lobby_user, in_h_item_action, action_item);
							}
						}
						else
						{
							return m_gen_result.setFail(result.h_code_e.ITEM_INVALID_DATA, $"[Replace]must have ReplaceTidProcessor: {in_h_item_action}");
						}
					}
					break;

				case h_define.item_action_e.PackageItem:
					break;

				case h_define.item_action_e.RandomItem:
					{
						// 다시 뽑기를 위해서 정보를 저장해야함
						// 이미 DB 저장이 완료되었는데 다시 뽑기를 할 수 있어야 함.
						regist_action = true;
						HRandomItemBase h_random_item_base = null;
						m_gen_result = in_lobby_user.m_h_random_item.FindRandomItemByItemTid(in_h_item_action.ItemTid, out h_random_item_base);
						if (m_gen_result.fail())
						{
							return m_gen_result;
						}

						List<h_info.MetaItem> reward_items = null;
						h_random_item_base.Draw(out reward_items);

						// 랜덤아이템 구입 개수만큼 수량을 증가해서 준다.
						if( reward_items.Count > 0)
						{
							foreach(var each_reward in reward_items)
							{
								each_reward.Count *= in_h_item_action.Count;
							}
						}

						m_gen_result = AppendAction(in_lobby_user, reward_items);
						if (m_gen_result.fail())
						{
							return m_gen_result;
						}
					}
					break;
				case h_define.item_action_e.WeightDbRandomItem:
					{
						regist_action = true;
						if (m_weight_db_random_item_updater == null)
						{
							m_weight_db_random_item_updater = in_lobby_user.m_h_random_item.HWeightDbRandomItemUpdater;
						}

						h_info.MetaItem reward_item = null;
						m_gen_result = m_weight_db_random_item_updater.Draw(in_lobby_user, this, in_h_item_action.ItemTid, out reward_item);
						if (m_gen_result.fail())
						{
							return m_gen_result;
						}
					}
					break;
				case h_define.item_action_e.CallStone:
					// 전화석은 외부에서 따로 처리할 것.
					regist_action = true;
					break;
			}

			if (regist_action == false)
			{
				return m_gen_result.setFail(result.h_code_e.ITEM_UPDATE_INVALID_ACTION_TYPE, $"Cant work ActionType: {in_h_item_action}");
			}

			return m_gen_result.setOk();
		}

		//=======================================================================
		// Meta에서 주는 보상 또는 Email 첨부의 경우 MetaItem으로 처리
		// * ItemAction 으로 변환해서 동일 로직 처리
		//=======================================================================
		public gplat.Result AppendAction(LobbyUser in_lobby_user, List<h_info.MetaItem> in_h_meta_items, ReplaceTidProcessor in_replace_tid_processor = null)
		{
			foreach (var h_meta_item in in_h_meta_items)
			{
				var gen_result = AppendAction(in_lobby_user, h_meta_item, in_replace_tid_processor);
				if (gen_result.fail())
				{
					return gen_result;
				}
			}
			return m_gen_result.setOk();
		}
		public gplat.Result AppendAction(LobbyUser in_lobby_user, h_info.MetaItem in_h_meta_item, ReplaceTidProcessor in_replace_tid_processor = null)
		{
			return AppendAction(in_lobby_user, new h_info.ItemAction()
			{
				ActionType = in_h_meta_item.ActionType,
				ItemTid = in_h_meta_item.ItemTid,
				ItemTier = in_h_meta_item.ItemTier,
				TierLevel = in_h_meta_item.TierLevel,
				CreateLevel = in_h_meta_item.CreateLevel,
				TimeLimitDay = in_h_meta_item.TimeLimitDay,
				Count = in_h_meta_item.Count
			}, in_replace_tid_processor);
		}
		public gplat.Result AppendAction(LobbyUser in_lobby_user, h_info.MetaCountItem in_h_meta_item, ReplaceTidProcessor in_replace_tid_processor = null)
		{
			return AppendAction(in_lobby_user, new h_info.ItemAction()
			{
				ActionType = h_define.item_action_e.Item,
				ItemTid = in_h_meta_item.ItemTid,
				ItemTier = h_define.item_tier_e._NONE,
				TierLevel = 0,
				CreateLevel = 0,
				TimeLimitDay = 0,
				Count = in_h_meta_item.Count
			}, in_replace_tid_processor);
		}
		public gplat.Result AppendAction(LobbyUser in_lobby_user, Int64 in_item_tid, Int32 in_item_count, ReplaceTidProcessor in_replace_tid_processor = null)
		{
			return AppendAction(in_lobby_user, new h_info.ItemAction()
			{
				ActionType = h_define.item_action_e.Item,
				ItemTid = in_item_tid,
				ItemTier = h_define.item_tier_e._NONE,
				TierLevel = 0,
				CreateLevel = 0,
				TimeLimitDay = 0,
				Count = in_item_count
			}, in_replace_tid_processor);
		}
		public gplat.Result AppendConsumeAction(LobbyUser in_lobby_user, List<h_info.MetaItem> in_h_meta_items)
		{
			foreach (var h_meta_item in in_h_meta_items)
			{
				var gen_result = AppendConsumeAction(in_lobby_user, h_meta_item);
				if (gen_result.fail())
				{
					return gen_result;
				}
			}
			return m_gen_result.setOk();
		}
		public gplat.Result AppendConsumeAction(LobbyUser in_lobby_user, h_info.MetaItem in_h_meta_item)
		{
			int op_minus = (in_h_meta_item.Count > 0) ? -1 : 1;     // 음수화
			return AppendAction(in_lobby_user, new h_info.ItemAction()
			{
				ActionType = in_h_meta_item.ActionType,
				ItemTid = in_h_meta_item.ItemTid,
				ItemTier = in_h_meta_item.ItemTier,
				TierLevel = in_h_meta_item.TierLevel,
				Count = in_h_meta_item.Count * op_minus
			});
		}
		public gplat.Result AppendConsumeAction(LobbyUser in_lobby_user, h_info.ItemAction in_action_item)
		{
			int op_minus = (in_action_item.Count > 0) ? -1 : 1;     // 음수화
			in_action_item.Count = in_action_item.Count * op_minus;
			return AppendAction(in_lobby_user, in_action_item);
		}
		public gplat.Result AppendConsumeAction(LobbyUser in_lobby_user, List<h_info.ItemAction> in_h_actions)
		{
			foreach (var item_action in in_h_actions)
			{
				if (item_action.Count > 0)
				{
					item_action.Count *= -1;     // 음수화
				}
				var gen_result = AppendAction(in_lobby_user, item_action);
				if (gen_result.fail())
				{
					return gen_result;
				}
			}
			return m_gen_result.setOk();
		}

		public gplat.Result AppendConsumeAction(LobbyUser in_lobby_user, h_define.item_action_e in_action, Int64 in_tid, Int64 in_dbid, int in_count, h_define.item_tier_e in_tier = h_define.item_tier_e._NONE, int in_tier_level = 0)
		{
			int op_minus = (in_count > 0) ? -1 : 1;     // 음수화
			return AppendAction(in_lobby_user, new h_info.ItemAction()
			{
				ActionType = in_action,
				ItemTid = in_tid,
				TargetDbid = in_dbid,
				ItemTier = in_tier,
				TierLevel = in_tier_level,
				Count = in_count * op_minus
			});
		}



		//=======================================================================
		// 연결재화가 있는 경우 소비 우선순위에 따라 소비
		//[2021.10 TimeLimitItem]
		//=======================================================================
		private bool RegistDbSaveItemWithPrority(LobbyUser in_user, h_info.ItemAction in_h_item_action, h_info.InstantItem db_save_item)
		{
			var have_item = in_user.LogicInventory.FindItemByDbId(in_h_item_action.TargetDbid);
			if (have_item != null)
			{
				if (have_item.HasConsumeGroups && in_h_item_action.Count < 0)
				{
					// 소비 순서에 따른 정렬
					var have_comsume_groups = have_item.ConsumeGroupItems();
					List<game.ItemBase> ordered_consume = new List<game.ItemBase>();

					var meta_consume_orders = have_item.MetaComsumeGroupInfo;
					foreach (var each_order in meta_consume_orders)
					{
						if (each_order.ConsumeLinkTid == have_item.ItemTid)
						{
							ordered_consume.Add(have_item);
						}
						else
						{
							foreach (var each_item in have_comsume_groups)
							{
								if (each_order.ConsumeLinkTid == each_item.ItemTid)
								{
									ordered_consume.Add(each_item);
								}
							}
						}
					}

					// 소비 진행
					int consume_amount = in_h_item_action.Count * (-1);
					List<h_info.InstantItem> consume_items = new List<h_info.InstantItem>();
					foreach (var each_consume_item in ordered_consume)
					{
						if (each_consume_item.Count > consume_amount)
						{
							//소비완료
							var consume_item = each_consume_item.DeepClone();
							consume_item.Count = -consume_amount;
							consume_items.Add(consume_item);
							break;
						}
						else
						{
							var consume_item = each_consume_item.DeepClone();
							consume_item.Count = -each_consume_item.Count;
							consume_items.Add(consume_item);
							consume_amount -= each_consume_item.Count;
						}
					}
					RegistDbSaveItem(in_h_item_action, consume_items);
				}
				else
				{
					RegistDbSaveItem(in_h_item_action, db_save_item);
				}
			}
			else
			{
				RegistDbSaveItem(in_h_item_action, db_save_item);
			}

			return true;
		}

		//=======================================================================
		// 동일한 Dbid를 갖는 아이템의 경우 SP 최적화를 위해서 Merge를 해주도록 한다.
		//	* Item Action을 Merge 해서 h_info.InstantItem(db_save_info)를 생성
		//	* 모든 Item을 Append한 이후 처리해주면 좋을 것 같다.
		//=======================================================================
		public gplat.Result MergeAction(LobbyUser in_user)
		{
			foreach (var h_item_info in m_action_to_items)
			{
				h_info.InstantItem merge_item_info = null;
				h_info.InstantItem action_item_info = h_item_info;
				h_info.UpdaterSaveInfo db_update_info = m_db_save_items.Find(t => t.DbUpdateInfo.Db_id == h_item_info.Db_id);


				if (db_update_info != null)
				{
					db_update_info.ActionType = GetActionType(h_item_info.Db_id);
					merge_item_info = db_update_info.DbUpdateInfo;
					merge_item_info.Exp += h_item_info.Exp;
					merge_item_info.Count += h_item_info.Count;

					// 최종 저장 데이터의 개수가 LimitCount 이내로 보정
					var inventory_item = in_user.LogicInventory.FindItemByDbId(merge_item_info.Db_id);
					if (inventory_item != null)
					{
						merge_item_info.Count = inventory_item.GainCountWithLimit(merge_item_info.Count);
					}
				}
				else
				{
					db_update_info = new h_info.UpdaterSaveInfo();
					db_update_info.DbUpdateInfo = h_item_info.DeepClone() as h_info.InstantItem;
					m_db_save_items.Add(db_update_info);
					merge_item_info = db_update_info.DbUpdateInfo;
					db_update_info.ActionType = GetActionType(h_item_info.Db_id);
				}

				// need refill time update
				if (action_item_info.ItemType == h_define.item_type_e.RelaxReward)
				{
					db_update_info.NeedRefillUpdate = true;
				}
				else if (action_item_info.ItemType == h_define.item_type_e.UserMoney)
				{
					db_update_info.NeedRefillUpdate = (NeedRefillTimeUpdate(in_user, ref merge_item_info) > 0);
				}
			}

			// RollingNotice 처리
			HRollingNoticeManager.it.AppendUserNotice(in_user, m_in_action_infos, m_db_save_items);
			m_action_to_items.Clear();

			return m_gen_result.setOk();
		}


		// 경험치 아이템의 경우 중첩되서 들어오면 최종 레벨 계산을 해줘야 한다.
		// - 자동 진행시 필요
		public void ComputeLevelFromExp(LobbyUser in_user)
		{
			var char_exp_item = in_user.LogicInventory.ExpItem;
			if (char_exp_item != null)
			{
				h_info.UpdaterSaveInfo db_update_info = m_db_save_items.Find(t => t.DbUpdateInfo.Db_id == char_exp_item.DbId);
				if (db_update_info != null)
				{
					var char_level_meta = GameMeta.CardLevel.H2LevelInfoWithExp(char_exp_item.Exp + db_update_info.DbUpdateInfo.Exp);
					if (char_level_meta != null)
					{
						db_update_info.DbUpdateInfo.Lv = char_level_meta.Level;
					}
				}
			}
		}


		private h_define.item_action_e GetActionType(long item_dbid)
		{
			var find_action = m_in_action_infos.Find(t => t.TargetDbid == item_dbid);
			if (find_action != null)
			{
				return find_action.ActionType;
			}
			// Action을 못찾는것은 다른 소비가 되어서임(유료/ 기간제)
			return h_define.item_action_e.Item;
		}

		// 유지보수를 위해, 기존 리퀘스트에서 따로 db update 함수를 각각 만들어 사용했던 것을
		// ItemDbUpdater 로 옮겨 하나로 관리하도록 개선
		public gplat.Result DbProcess(Int64 char_uid, TransactionInfo tran = null)
		{
			var db_req = new server_msg_db.req_h2_item_upsert_json_gameuser();
			db_req.In_char_uid = char_uid;

			var db_req_refill = new server_msg_db.req_h2_item_upsert_with_refilltime_json_gameuser();
			db_req_refill.In_char_uid = char_uid;

			foreach (var each_item in m_db_save_items)
			{
				if (each_item.NeedRefillUpdate)
				{
					db_req_refill.In_json_item_infos.Add(each_item.DbUpdateInfo);
				}
				else
				{
					db_req.In_json_item_infos.Add(each_item.DbUpdateInfo);
				}
			}

			if (db_req.In_json_item_infos.Count > 0)
			{
				var db_exec = new exec_h2_item_upsert_json_gameuser();
				if (tran == null)
				{
					m_gen_result = db_exec.process(db_req);
				}
				else
				{
					m_gen_result = db_exec.processTransaction(db_req, tran);
				}

				if (m_gen_result.fail())
				{
					return m_gen_result;
				}
			}
			if (db_req_refill.In_json_item_infos.Count > 0)
			{
				var db_exec = new exec_h2_item_upsert_with_refilltime_json_gameuser();
				if (tran == null)
				{
					m_gen_result = db_exec.process(db_req_refill);
				}
				else
				{
					m_gen_result = db_exec.processTransaction(db_req_refill, tran);
				}
				if (m_gen_result.fail())
				{
					return m_gen_result;
				}
			}

			if (m_weight_db_random_item_updater != null)
			{
				m_gen_result = m_weight_db_random_item_updater.DbProcess(char_uid, tran);
			}

			return m_gen_result;
		}



		#region private_function
		private bool MakeOrUpdateItemDbid(LobbyUser user, ref h_info.ItemAction in_h_item_action, ref h_info.InstantItem in_db_save_h_instant_item)
		{
			game.ItemBase have_item = null;

			// 찾아서 있으면 증감처리
			if (in_h_item_action.TargetDbid > 0)
			{
				in_db_save_h_instant_item.Db_id = in_h_item_action.TargetDbid;   // 카운팅
				have_item = user.m_h_inventory.FindItem(in_db_save_h_instant_item.Db_id);
				if (have_item != null)
				{
					in_db_save_h_instant_item.Lv = have_item.ItemInfo.Lv;
					in_db_save_h_instant_item.Count = have_item.GainCountWithLimit(in_h_item_action.Count);
				}
			}
			else
			{
				if (user.LogicInventory.IsCountingItem(in_db_save_h_instant_item.ItemType))
				{
					have_item = user.m_h_inventory.FindItemByInfo(in_db_save_h_instant_item);
					if (have_item != null)
					{
						in_h_item_action.TargetDbid = have_item.DbId;
						in_db_save_h_instant_item.Db_id = have_item.DbId;
						in_db_save_h_instant_item.Lv = have_item.ItemInfo.Lv;
						in_db_save_h_instant_item.ItemTier = have_item.ItemInfo.ItemTier;
						in_db_save_h_instant_item.TierLevel = have_item.ItemInfo.TierLevel;
						in_db_save_h_instant_item.Count = have_item.GainCountWithLimit(in_h_item_action.Count);
					}
					else
					{
						Int64 target_tid = in_h_item_action.ItemTid;
						var db_update_info = m_db_save_items.Find(t => t.DbUpdateInfo.Item_tid == target_tid);
						if (db_update_info != null)
						{
							in_db_save_h_instant_item.Db_id = db_update_info.DbUpdateInfo.Db_id;
							in_db_save_h_instant_item.Lv = db_update_info.DbUpdateInfo.Lv;
							in_db_save_h_instant_item.ItemTier = db_update_info.DbUpdateInfo.ItemTier;
							in_db_save_h_instant_item.TierLevel = db_update_info.DbUpdateInfo.TierLevel;
						}
					}
				}
			}

			// 없으면 생성
			if (have_item == null)
			{
				in_db_save_h_instant_item.Db_id = GameMeta.Item.NextItemDbId();   // 생성
				in_h_item_action.TargetDbid = in_db_save_h_instant_item.Db_id;

				// 카드인 경우
				var item_meta_data = MetaData.byMetaId<h_item_meta.data>(in_db_save_h_instant_item.Item_tid);
				if (item_meta_data.ItemType == h_define.item_type_e.PlayerCard || item_meta_data.ItemType == h_define.item_type_e.PetCard)
				{
					// 티어가 없으면 Normal
					if (in_h_item_action.ItemTier == h_define.item_tier_e._NONE)
					{
						in_h_item_action.ItemTier = h_define.item_tier_e.Normal;
						in_db_save_h_instant_item.ItemTier = in_h_item_action.ItemTier;
					}
				}
				else if (item_meta_data.ItemType == h_define.item_type_e.RelaxReward)
				{
					in_db_save_h_instant_item.NextResetTime = gplat.TimeUtil.it.DateTimeToString(DateTime.UtcNow);
				}
				else if (item_meta_data.ItemType == h_define.item_type_e.UserMoney)
				{
					if (item_meta_data.RefillLinkTid > 0)
					{
						var meta_h_item_refill_data = MetaData.byMetaId<h_item_refill_meta.data>(item_meta_data.RefillLinkTid);
						if (null != meta_h_item_refill_data)
						{
							if (meta_h_item_refill_data.ComRefill.IsUse)
							{
								in_db_save_h_instant_item.NextRefillTime = gplat.TimeUtil.it.DateTimeToString(DateTime.UtcNow.AddMinutes(meta_h_item_refill_data.ComRefill.IntervalMinutes));
							}

							if (meta_h_item_refill_data.ComReset.IsUse)
							{
								in_db_save_h_instant_item.NextResetTime = gplat.TimeUtil.it.DateTimeToString(DateTime.UtcNow);
							}
						}
					}
				}
			}

			return true;
		}

		// ====================================================================
		// 최대갯수 보다 많이 보유했다가, 최대 갯수보다 적게 보유하는 시점이 refill 갱신 시점이다.
		//	> 사용시점에 Refill 시간의 시작을 업데이트 해야 한다.
		// ====================================================================
		private int NeedRefillTimeUpdate(LobbyUser user, ref h_info.InstantItem in_db_save_h_instant_item)
		{
			if (in_db_save_h_instant_item.ItemType == h_define.item_type_e.UserMoney && in_db_save_h_instant_item.Count < 0)
			{
				var have_item = user.m_h_inventory.FindItemByInfo(in_db_save_h_instant_item) as game.ItemRefill;
				if (have_item != null)
				{
					int use_amount = in_db_save_h_instant_item.Count * -1;
					if (have_item.PrepareNextRefillUpdate(use_amount, out string out_next_refill_time))
					{
						in_db_save_h_instant_item.NextRefillTime = out_next_refill_time;
						return 1;
					}
				}
			}
			return 0;
		}

		private bool RegistDbSaveItem(h_info.ItemAction in_h_item_action, h_info.InstantItem in_h_instant_item)
		{
			m_in_action_infos.Add(in_h_item_action);
			m_action_to_items.Add(in_h_instant_item);
			m_all_items.Add(in_h_instant_item);
			return true;
		}
		private bool RegistDbSaveItem(h_info.ItemAction in_h_item_action, List<h_info.InstantItem> in_action_to_items)
		{
			m_in_action_infos.Add(in_h_item_action);
			m_action_to_items.AddRange(in_action_to_items);
			m_all_items.AddRange(in_action_to_items);
			return true;
		}


		public bool HaveNewItemCard()
		{
			foreach (var each_item in m_all_items)
			{
				if (each_item.ItemType == h_define.item_type_e.PlayerCard && each_item.Count > 0)
				{
					return true;
				}
			}
			return false;
		}

		public override string ToString()
		{
			StringBuilder str_log = new StringBuilder();
			str_log.AppendLine($"=== Actions ===");
			foreach (var each_action in m_in_action_infos)
			{
				str_log.AppendLine($"	[{each_action.TargetDbid}:{each_action.ItemTid}] Type:{each_action.ActionType}, Count:{each_action.Count}");
			}
			str_log.AppendLine($"=== Reward ===");
			foreach (var each_item in m_db_save_items)
			{
				str_log.Append($"	[{each_item.DbUpdateInfo.Db_id}:{each_item.DbUpdateInfo.Item_tid}: {each_item.DbUpdateInfo.ItemType}]");
				if (each_item.ResultItem != null && each_item.BeforeClone != null)
				{
					//str_log.Append(each_item.DbUpdateInfo.Hp != 0 ? $", HP:{each_item.BeforeClone.Hp}+{each_item.DbUpdateInfo.Hp}={each_item.ResultItem.Hp}" : $", HP:{each_item.BeforeClone.Hp}");
					str_log.Append(each_item.DbUpdateInfo.Exp != 0 ? $", Exp:{each_item.BeforeClone.Exp}+{each_item.DbUpdateInfo.Exp}={each_item.ResultItem.Exp}" : $", Exp:{each_item.BeforeClone.Exp}");
					str_log.Append(each_item.DbUpdateInfo.Count != 0 ? $", Count:{each_item.BeforeClone.Count}+{each_item.DbUpdateInfo.Count}={each_item.ResultItem.Count}" : $", Count:{each_item.BeforeClone.Count}");
					str_log.AppendLine($", NeedRefill:{each_item.NeedRefillUpdate}:{each_item.DbUpdateInfo.NextRefillTime}, Reset:{each_item.DbUpdateInfo.NextResetTime}, Expire:{each_item.DbUpdateInfo.ExpireDate}");
				}
			}

			return str_log.ToString();
		}
		#endregion //private_function
	}


	public class LevelupRewardInfo
	{
		public long m_dbid;
		public int m_prev_level;
		public int m_next_level;
		public int m_reward_hp = 0;

		public override string ToString()
		{
			return string.Format($"[{m_dbid}] Level {m_prev_level} >> {m_next_level}, HP:+{m_reward_hp}");
		}
	}
}
