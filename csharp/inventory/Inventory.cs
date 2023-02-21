using System;
using System.Collections.Generic;
using System.Text;

namespace game
{
	//instant item list를 가지고 인벤토리를 구성한다. 
	//각 속성에 맞는 카드 타입 및 재화 정보 주기 
	//부분 갱신 및 전체 갱신 기능 
	public class Inventory
	{
		//전체 : 원시 디비정보
		List<h_info.InstantItem> m_instant_items = new List<h_info.InstantItem>();
		//전체 : 아이템 클래스 
		Dictionary<Int64/* db_id*/, ItemBase> m_items = new Dictionary<Int64, ItemBase>();

		// 인벤토리 변경(Reddot)
		InventoryChange m_inventory_changed = new InventoryChange();

		// BuffManager
		BuffManager m_pass_buff = new BuffManager();
		BuffManager m_pet_buff = new BuffManager();

		//================
		//필터 그룹 
		//================
		//카드 그룹 
		List<ItemCard> m_player_cards = new List<ItemCard>();
		List<PetCard> m_pet_cards = new List<PetCard>();
		List<ItemCard> m_fusion_material_card = new List<ItemCard>();

		//카드덱의 정의 
		// PVP에서만 사용: PVP설정된 덱을 융합 가능할지는 체크해봐야함.
		ItemCardDeck m_card_deck = new ItemCardDeck(h_define.item_type_e.PlayerCard);
		ItemCardDeck m_pvp_deck = new ItemCardDeck(h_define.item_type_e.PlayerCard);
		ItemCardDeck m_pet_deck = new ItemCardDeck(h_define.item_type_e.PetCard);

		// 유료재화
		List<ItemNormal> m_payed_items = new List<ItemNormal>();
		// 기간제 아이템
		List<ItemTimeLimit> m_timelimit_items = new List<ItemTimeLimit>();

		//Money: Refill Process가 필요한 아이템 그룹
		List<IRefillProcess> m_refill_items = new List<IRefillProcess>();
		List<ItemRefill> m_money_items = new List<ItemRefill>();
		// Costume
		List<ItemCostume> m_costume_items = new List<ItemCostume>();
		List<ItemCostume> m_ria_character_items = new List<ItemCostume>();
		// 펫 버프
		List<ItemPetBuff> m_petbuff_items = new List<ItemPetBuff>();
		// 레벨업 재료
		List<ItemNormal> m_levelup_material_items = new List<ItemNormal>();

		//================
		// 숏컷 
		//================
		ItemRefill m_heart_item = new ItemRefill();             // 하트(AP)
		ItemRefill m_gold_item = new ItemRefill();              // 골드
		ItemRefill m_dia_item = new ItemRefill();               // 다이아
		ItemRefill m_dia_key_item = new ItemRefill();           // 다이아 키
		ItemRefill m_gold_key_item = new ItemRefill();          // 골드 키
		ItemRefill m_dia_free_key_item = new ItemRefill();      // 무료 다이아 키
		ItemRefill m_gold_free_key_item = new ItemRefill();     // 무료 골드 키
		ItemRelaxReward m_relax_item = new ItemRelaxReward();             // 휴식 보상
		ItemRefill m_autoplay_item = new ItemRefill();          // 소탕
		ItemRefill m_hardap_item = new ItemRefill();            // 하드모드 AP
		ItemNormal m_exp_item = new ItemNormal();               // 사용자 EXP
		ItemRefill m_story_ap_item = new ItemRefill();          // 스토리AP


		private const short DEFAULT_DECK_GROUP_NO = 1;

		#region getter__setter

		public List<ItemBase> AllItems => new List<ItemBase>(m_items.Values);
		public ItemRefill HeartItem => m_heart_item;
		public ItemRefill StoryApItem => m_story_ap_item;
		public ItemRefill GoldItem => m_gold_item;
		public ItemRefill DiaItem => m_dia_item;
		public ItemRefill DiaKeyItem => m_dia_key_item;
		public ItemRefill GoldKeyItem => m_gold_key_item;
		public ItemRefill DiaFreeKeyItem => m_dia_free_key_item;
		public ItemRefill GoldFreeKeyItem => m_gold_free_key_item;
		public ItemRelaxReward RelaxRewardItem => m_relax_item;
		public ItemRefill AutoPlayCountItem => m_autoplay_item;
		public ItemRefill HardApItem => m_hardap_item;

		public ItemNormal ExpItem => m_exp_item;

		// 사용자 레벨 정보
		public Int32 UserLevel => m_exp_item.Level;
		public Int32 UserExp => m_exp_item.Exp;

		//전체 원시 디비정보 
		public List<h_info.InstantItem> InstantItems => m_instant_items;
		public BuffManager InventoryBuff => m_pass_buff;
		public BuffManager PetBuff => m_pet_buff;

		public bool IsCardChanged => m_inventory_changed.Card;
		#endregion //getter__setter





		// Commit된 결과를 처리한다.
		// ** InstantItem 정보에 DB저장된 결과를 반영해서 적용해야 한다 : [Result] 사용. **
		// 원시디비정보, 아이템클래스, 덱정보의 InstantItem 정보를 갱신해야 한다.
		public gplat.Result UpsertItems(List<h_info.InstantItem> in_instant_items)
		{
			var gen_result = new gplat.Result();
			List<h_info.InstantItem> add_new_items = new List<h_info.InstantItem>();

			foreach (var h_upsert_item in in_instant_items)
			{
				// -------------------------------------------------------------------
				// 원시 디비정보 갱신
				// -------------------------------------------------------------------
				bool found = false;
				bool is_delete = IsDeletableItem(h_upsert_item);
				for (int i = 0; i < m_instant_items.Count; i++)
				{
					h_info.InstantItem h_instant_item = m_instant_items[i];
					if (h_instant_item.Db_id == h_upsert_item.Db_id)
					{
						found = true;
						if (true == is_delete)
						{
							gplat.Log.logger("logic").Warn($"Inventory Remove:{h_upsert_item.Db_id}:{h_upsert_item.Item_tid}, Count:{h_upsert_item.Count}");
							m_instant_items.RemoveAt(i);
						}
						else
						{
							m_instant_items[i] = h_upsert_item;     // 데이터 주소가 바뀜
						}
						break;
					}
				}

				if (found == false)
				{
					m_instant_items.Add(h_upsert_item);
				}

				// -------------------------------------------------------------------
				// 아이템 클래스, 덱 갱신
				// -------------------------------------------------------------------
				if (m_items.TryGetValue(h_upsert_item.Db_id, out var find_item))
				{
					if (true == is_delete)
					{
						// 삭제: 아이템목록과 카드목록에서 제거
						if (find_item.IsItemType(h_define.item_type_e.PlayerCard))
						{
							m_inventory_changed.Card = true;
							RemoveCardItem(find_item);
						}
						else if (find_item.IsItemType(h_define.item_type_e.FusionMaterialCard))
						{
							RemoveFusionMaterialCardItem(find_item);
						}
						else if (find_item.IsItemType(h_define.item_type_e.TimeLimitItem))
						{
							RemoveTimeLimitItem(find_item);
						}
					}
					else
					{
						// 갱신: 아이템목록 갱신, 덱 정보 갱신
						find_item.Setup(h_upsert_item);
						RefreshItemCount(find_item);
						//RefreshDeckInfo(find_item);
					}
				}
				else
				{
					// 추가:
					add_new_items.Add(h_upsert_item);
				}
			}

			UpdateShotcut(add_new_items);
			UpdateConsumeGroups();
			UpdateRefillMax();
			ComputePetBuff();

			return gen_result.setOk();
		}

		bool IsDeletableItem(h_info.InstantItem in_item)
		{
			return (in_item.Count == 0 && IsCountingItem(in_item.ItemType) == false)
				|| (in_item.ItemType == h_define.item_type_e.TimeLimitItem && in_item.Count == 0);
		}


		public ItemRefill TakeUserMoney(Int64 in_item_tid)
		{
			foreach (var money_item in m_money_items)
			{
				if (money_item.ItemTid == in_item_tid)
				{
					return money_item;
				}
			}
			return null;
		}

		private void ClearShortcut()
		{
			m_fusion_material_card.Clear();
			m_player_cards.Clear();
			m_pet_cards.Clear();
			m_refill_items.Clear();
			m_levelup_material_items.Clear();

			m_heart_item = new ItemRefill();
			m_gold_item = new ItemRefill();
			m_dia_item = new ItemRefill();
			m_dia_key_item = new ItemRefill();
			m_gold_key_item = new ItemRefill();
			m_relax_item = new ItemRelaxReward();
			m_story_ap_item = new ItemRefill();
			// 삭제예정 ===
			m_autoplay_item = new ItemRefill();
			m_hardap_item = new ItemRefill();
			// ============

			m_money_items.Clear();
			m_costume_items.Clear();
			m_ria_character_items.Clear();

			m_payed_items.Clear();
			m_timelimit_items.Clear();
			m_petbuff_items.Clear();


			m_items.Clear();
		}
		private void UpdateShotcut(List<h_info.InstantItem> in_instant_items)
		{
			gplat.Result gen_result = null;
			foreach (var h_instantitem in in_instant_items)
			{
				//펫
				if (h_define.item_type_e.PetCard == h_instantitem.ItemType)
				{
					var item_card = new PetCard();
					h_instantitem.DeckNo = 1;       // 모든 펫
					gen_result = item_card.Setup(h_instantitem);
					if (gen_result.fail())
					{
						continue;
					}

					m_pet_cards.Add(item_card);
					m_items.Add(item_card.DbId, item_card);
					// 펫 덱
					m_pet_deck.SetCardByAttribute(item_card);
				}
				//카드 
				else if (h_define.item_type_e.PlayerCard == h_instantitem.ItemType)
				{
					var item_card = new ItemCard();
					gen_result = item_card.Setup(h_instantitem);
					if (gen_result.fail())
					{
						continue;
					}
					m_player_cards.Add(item_card);
					m_items.Add(item_card.DbId, item_card);
					m_inventory_changed.Card = true;
				}
				//융합재료카드 (리아카드)
				else if (h_define.item_type_e.FusionMaterialCard == h_instantitem.ItemType)
				{
					var item_card = new ItemCard();
					gen_result = item_card.Setup(h_instantitem);
					if (gen_result.fail())
					{
						continue;
					}
					m_fusion_material_card.Add(item_card);
					m_items.Add(item_card.DbId, item_card);
					m_inventory_changed.Card = true;
				}
				//레벨업 재료카드 (영혼석)
				else if (h_define.item_type_e.CharSoulMaterial == h_instantitem.ItemType)
				{
					m_levelup_material_items.Add(CreateNormalItem(h_instantitem));
					m_inventory_changed.LevelupMaterial = true;
				}
				//재화 
				else if (h_define.item_type_e.UserMoney == h_instantitem.ItemType)
				{
					var item_money = new ItemRefill();
					gen_result = item_money.Setup(h_instantitem, InventoryBuff);
					if (gen_result.fail())
					{
						continue;
					}
					CommodityShortcut(item_money);

					m_money_items.Add(item_money);
					m_items.Add(item_money.DbId, item_money);
					m_refill_items.Add(item_money);
				}
				//Relax Reward
				else if (h_define.item_type_e.RelaxReward == h_instantitem.ItemType)
				{
					var item_relax = new ItemRelaxReward();
					gen_result = item_relax.Setup(h_instantitem, InventoryBuff);
					if (gen_result.fail())
					{
						continue;
					}
					m_relax_item = item_relax;
					m_items.Add(item_relax.DbId, item_relax);
				}
				else if (h_define.item_type_e.TimeLimitItem == h_instantitem.ItemType)
				{
					var item_timelimit = new ItemTimeLimit();
					gen_result = item_timelimit.Setup(h_instantitem);
					if (gen_result.fail())
					{
						continue;
					}
					m_timelimit_items.Add(item_timelimit);
					m_items.Add(item_timelimit.DbId, item_timelimit);
				}
				else if (IsCostumeItem(h_instantitem.ItemType))
				{
					var item_costume = new ItemCostume();
					gen_result = item_costume.Setup(h_instantitem);
					if (gen_result.fail())
					{
						continue;
					}
					m_costume_items.Add(item_costume);
					m_items.Add(item_costume.DbId, item_costume);
				}
				else if(h_define.item_type_e.RiaCharacter == h_instantitem.ItemType)
				{
					var item_costume = new ItemCostume();
					gen_result = item_costume.Setup(h_instantitem);
					if (gen_result.fail())
					{
						continue;
					}
					m_ria_character_items.Add(item_costume);
					m_items.Add(item_costume.DbId, item_costume);
				}
				else if (h_define.item_type_e.PetBuff == h_instantitem.ItemType)
				{
					var item_pet_buff = new ItemPetBuff();
					gen_result = item_pet_buff.Setup(h_instantitem);
					if (gen_result.fail())
					{
						continue;
					}
					m_petbuff_items.Add(item_pet_buff);
					m_items.Add(item_pet_buff.DbId, item_pet_buff);
					m_inventory_changed.PetBuff = true;
				}
				else if (h_define.item_type_e.PayedItem == h_instantitem.ItemType)
				{
					m_payed_items.Add(CreateNormalItem(h_instantitem));
				}
				else    //일반아이템 
				{
					var item_normal = CreateNormalItem(h_instantitem);
					NormalItemShortcut(item_normal);
				}
			}
		}


		private void UpdateConsumeGroups()
		{
			foreach (var payed_item in m_payed_items)
			{
				if (payed_item.ParentLinkTid > 0)
				{
					var parent_item = m_money_items.Find(t => t.ItemTid == payed_item.ParentLinkTid);
					if (parent_item != null)
					{
						parent_item.SetupConsumeGroup(payed_item);
					}
				}
			}
			foreach (var timelimit_item in m_timelimit_items)
			{
				var parent_item = m_money_items.Find(t => t.ItemTid == timelimit_item.ParentLinkTid);
				if (parent_item != null)
				{
					parent_item.SetupConsumeGroup(timelimit_item);
				}
			}
		}

		// 레벨에 따른 RefillMax의 변경으로 후처리를 진행한다.
		private void UpdateRefillMax()
		{
			if (HeartItem != null)
			{
				HeartItem.SetRefillMax(UserLevel);
			}
		}

		public void RemoveCardItem(game.ItemBase in_item)
		{
			if (in_item.IsItemType(h_define.item_type_e.PlayerCard))
			{
				if (in_item is ItemCard)
				{
					m_player_cards.Remove((ItemCard)in_item);
				}
			}
			m_items.Remove(in_item.DbId);
		}
		public void RemoveFusionMaterialCardItem(game.ItemBase in_item)
		{
			if (in_item.IsItemType(h_define.item_type_e.FusionMaterialCard))
			{
				if (in_item is ItemCard)
				{
					m_fusion_material_card.Remove((ItemCard)in_item);
				}
			}
			m_items.Remove(in_item.DbId);
		}
		public void RemoveTimeLimitItem(game.ItemBase in_item)
		{
			if (in_item.IsItemType(h_define.item_type_e.TimeLimitItem))
			{
				// remove from parent.consumegroups
				RemoveConsumeGroups(in_item.ParentLinkTid, in_item.DbId);
				// remove timelimit
				m_timelimit_items.Remove((ItemTimeLimit)in_item);
			}
			m_items.Remove(in_item.DbId);
		}

		private void RemoveConsumeGroups(Int64 in_parent_tid, Int64 in_timelimit_dbid)
		{
			var parent_item = m_money_items.Find(t => t.ItemTid == in_parent_tid);
			if (parent_item != null)
			{
				parent_item.RemoveConsumeGroup(in_timelimit_dbid);
			}
		}

		private ItemNormal CreateNormalItem(h_info.InstantItem in_item)
		{
			var item_normal = new ItemNormal();
			item_normal.Setup(in_item);
			m_items.Add(item_normal.DbId, item_normal);
			return item_normal;
		}

		private void CommodityShortcut(ItemRefill in_item_money)
		{
			//재화 직접 접근 숏컷생성 
			if (in_item_money.ItemHashtag == "gold")
			{
				m_gold_item = in_item_money;
			}
			else if (in_item_money.ItemHashtag == "dia")
			{
				m_dia_item = in_item_money;
			}
			else if (in_item_money.ItemHashtag == "ap")
			{
				m_heart_item = in_item_money;
			}
			else if (in_item_money.ItemHashtag == "diakey")
			{
				m_dia_key_item = in_item_money;
			}
			else if (in_item_money.ItemHashtag == "goldkey")
			{
				m_gold_key_item = in_item_money;
			}
			else if (in_item_money.ItemHashtag == "diafree")
			{
				m_dia_free_key_item = in_item_money;
			}
			else if (in_item_money.ItemHashtag == "goldfree")
			{
				m_gold_free_key_item = in_item_money;
			}
			
			else if (in_item_money.ItemHashtag == "storyap")
			{
				m_story_ap_item = in_item_money;
			}
			//else if (in_item_money.ItemHashtag == "relax")
			//{
			//	m_relax_item = in_item_money;
			//	if (m_relax_item.ItemInfo.NextResetTime.IsNullOrWhiteSpace())
			//	{
			//		m_relax_item.ItemInfo.NextResetTime = gplat.TimeUtil.it.DateTimeToString(DateTime.UtcNow);
			//	}
			//}
			//else if (in_item_money.ItemHashtag == "hardap")
			//{
			//	m_hardap_item = in_item_money;
			//}
		}

		private void NormalItemShortcut(ItemNormal in_item)
		{
			if (in_item.ItemHashtag == "userexp")
			{
				m_exp_item = in_item;
			}
		}


		//전체 데이터 로드 : 디비 / 패킷 
		public gplat.Result Init(List<h_info.InstantItem> in_instant_items)
		{
			var gen_result = new gplat.Result();
			InitSetupItems(in_instant_items);

			ClearShortcut();
			UpdateShotcut(m_instant_items);
			UpdateConsumeGroups();
			UpdateRefillMax();
			ComputePetBuff();

			return gen_result.setOk();
		}

		// ItemTid가 정상적이지 않은경우 예외처리 및 로그 출력
		//- Meta Tid Invalid
		//- Origine, Tier, TierLevel Invalid
		void InitSetupItems(List<h_info.InstantItem> in_instant_items)
		{
			m_instant_items.Clear();

			StringBuilder str_log = new StringBuilder();
			foreach (var each_item in in_instant_items)
			{
				var item_meta_data = gplat.MetaData.byMetaId<h_item_meta.data>(each_item.Item_tid);
				if (item_meta_data == null)
				{
					str_log.AppendLine($"\tDbid:{each_item.Db_id}, Tid{each_item.Item_tid}, Count:{each_item.Count}");
					continue;
				}
				else if (each_item.ItemType == h_define.item_type_e.PlayerCard)
				{
					if (false == VerifyItemTierLevel(item_meta_data.CardAttributes.OrginAttribute, each_item.ItemTier, each_item.TierLevel))
					{
						str_log.AppendLine($"\tDbid:{each_item.Db_id}, Tid{each_item.Item_tid}, Origine:{item_meta_data.CardAttributes.OrginAttribute}, Tier:{each_item.ItemTier}, TierLevel:{each_item.TierLevel}");
						continue;
					}
				}
				m_instant_items.Add(each_item);
			}
			if (str_log.Length > 0)
			{
				gplat.Log.logger("app").Error($"== UserInstantItem Tid Errors ==\n{str_log}");
			}
		}

		// 아이템의 티에에 맞는 레벨인지 확인
		bool VerifyItemTierLevel(h_define.item_origin_e in_origine, h_define.item_tier_e in_tier, Int32 in_tier_level)
		{
			if (false == GameMeta.CardGrade.GetMaxTierLevel(in_origine, in_tier, out int max_tier_level))
			{
				// Invalid origine Tier: 태생과 티어가 Meta에 없음.
				return false;
			}
			if (in_tier_level > max_tier_level)
			{
				// Invalid TierLevel : 해당 태생 티어의 MaxLevel보다 큰 값
				return false;
			}
			return true;
		}


		public ItemBase FindItemByDbId(Int64 in_db_id)
		{
			ItemBase out_item = null;
			m_items.TryGetValue(in_db_id, out out_item);
			return out_item;
		}


		// 일반 아이템 보상은 TID로 찾아서 DBID를 설정해 줘야 한다.
		public ItemBase FindItemByTId(Int64 in_tid)
		{
			foreach (var item_base in m_items.Values)
			{
				if (item_base.ItemTid == in_tid)
				{
					return item_base;
				}
			}
			return null;
		}
		// 일반 아이템 보상은 TID로 찾아서 DBID를 설정해 줘야 한다.
		public List<ItemBase> FindItemsByTId(Int64 in_tid)
		{
			List<ItemBase> find_items = new List<ItemBase>();
			foreach (var item_base in m_items.Values)
			{
				if (item_base.ItemTid == in_tid)
				{
					find_items.Add(item_base);
				}
			}
			return find_items;
		}
		// 재료 아이템은 속성과 등급으로 찾아서 DBID를 설정
		// 강화재료: 타입, 등급, 속성
		// 진화재료: ?
		public ItemBase FindItemByInfo(h_info.InstantItem in_item)
		{
			ItemBase out_item = null;
			foreach (var each_item in m_items.Values)
			{
				if (each_item.ItemTid == in_item.Item_tid && each_item.ItemTier == in_item.ItemTier && each_item.TierLevel == in_item.TierLevel)
				{
					return each_item;
				}
			}
			return out_item;
		}

		// 유효기간이 있는 아이템은 같은 유효기간을 갖는 아이템을 찾는다.
		public ItemBase FindItemByTimeLimit(h_info.InstantItem in_item)
		{
			int in_item_day_key = gplat.TimeUtil.it.GetResetKey(in_item.ExpireDate);
			ItemBase out_item = null;
			foreach (var each_item in m_timelimit_items)
			{
				if (each_item.ItemTid == in_item.Item_tid && each_item.SameExpiredKey(in_item_day_key))
				{
					return each_item;
				}
			}
			return out_item;
		}



		// RefillItem의 경우 HashTag를 주로 사용하므로 찾아서 쓸수 있도록 한다.
		public ItemBase FindHashTagMoney(string in_tag)
		{
			foreach (var each_money in m_money_items)
			{
				if (each_money.IsHashTag(in_tag))
				{
					return each_money;
				}
			}
			return null;
		}

		// 해당 캐릭터 보유 코스튬 전체
		public List<ItemCostume> FindEquipCostumes(Int64 in_char_tid)
		{
			List<ItemCostume> costume_items = new List<ItemCostume>();

			foreach (var each_costume in m_costume_items)
			{
				if (each_costume.CharTid == in_char_tid && each_costume.DeckNo > 0)
				{
					costume_items.Add(each_costume);
				}
			}

			return costume_items;
		}
		// 장착된 코스튬 파츠
		public ItemCostume FindEquipCostume(Int64 in_char_tid, h_define.item_type_e in_item_type)
		{
			if (!IsCostumeItem(in_item_type))
			{
				return null;
			}

			foreach (var each_costume in m_costume_items)
			{
				if (each_costume.CharTid == in_char_tid &&
					each_costume.ItemType == in_item_type &&
					each_costume.DeckNo > 0)
				{
					return each_costume;
				}
			}
			return null;
		}
		// 리아 캐릭터 전체
		//> 장착은 DeckNo > 0으로 찾을수 잇음.
		public List<ItemCostume> FindEquipRiaCharacters()
		{
			return m_ria_character_items;
		}


		#region get_infos

		//보유 카드 리스트 
		public List<ItemCard> PlayerCards()
		{
			return m_player_cards;
		}
		public List<ItemCard> FusionMaterialCards()
		{
			return m_fusion_material_card;
		}
		// 레벨업 재료들(영혼석)
		public List<ItemNormal> LevelupMaterials()
		{
			return m_levelup_material_items;
		}

		//보유 펫 리스트 
		public List<PetCard> PetCards()
		{
			return m_pet_cards;
		}


		//캐릭터 레벨 = ExpItem
		public int CharacterLevel()
		{
			if (m_exp_item != null)
			{
				return m_exp_item.Level;
			}
			return 1;
		}
		public int CharacterExp()
		{
			if (m_exp_item != null)
			{
				return m_exp_item.Exp;
			}
			return 0;
		}

		//전체 공격력 (덱 공격력 + 펫 공격력)
		public int TotalCombatPower(short in_deck_index)
		{
			return CardDeck(in_deck_index).TotalCombatPower() + m_pet_deck.TotalCombatPower();
		}


		//기간제 아이템
		public List<ItemTimeLimit> TimeLimitItems()
		{
			return m_timelimit_items;
		}
		//기간제 아이템
		public List<ItemBase> TimeLimitItems(long in_parent_tid)
		{
			//최초 제작시에는 Parent는 Money만 있음.
			var parent_item = TakeUserMoney(in_parent_tid);
			if (parent_item != null)
			{
				return parent_item.ConsumeGroupItems();
			}
			return null;
		}
		// 모든 코스튬 아이템
		public List<ItemCostume> CostumeItems()
		{
			return m_costume_items;
		}

		#endregion //get_infos



		// 카운트 아이템이 변경되었을때 동작
		private void RefreshItemCount(ItemBase in_refresh_item)
		{
			if (in_refresh_item.ItemType == h_define.item_type_e.PetBuff)
			{
				m_inventory_changed.PetBuff = true;
			}
		}

		// 펫 덱
		public ItemCardDeck PetDeck()
		{
			return m_pet_deck;
		}

		// ===================================================================
		// 아이템의 개수 체크
		//	- 양수로 비교해야 한다.
		// ===================================================================
		public bool IsEnough(Int64 in_dbid, Int32 in_need_count)
		{
			var find_item = FindItemByDbId(in_dbid);
			if (find_item != null)
			{
				return find_item.IsEnough(in_need_count);
			}
			return false;
		}
		public bool IsEnoughByTid(Int64 in_tid, Int32 in_need_count)
		{
			var find_item = FindItemByTId(in_tid);
			if (find_item != null && IsCountingItem(find_item.ItemType) == true)
			{
				return find_item.IsEnough(in_need_count);
			}
			return false;
		}

		// ===================================================================
		// 카운팅 타입 아이템
		// NormalItem, UserMoney, ReinforceItem, EvolutionItem
		// ===================================================================
		public bool IsCountingItem(h_define.item_type_e in_type)
		{
			return (in_type != h_define.item_type_e.PlayerCard && in_type != h_define.item_type_e.PetCard && in_type != h_define.item_type_e.FusionMaterialCard);
		}
		public bool IsCostumeItem(h_define.item_type_e in_type)
		{
			return  (in_type == h_define.item_type_e.CostumeHair) ||
					(in_type == h_define.item_type_e.CostumeHairAcc) ||
					(in_type == h_define.item_type_e.CostumeEyeAcc) ||
					(in_type == h_define.item_type_e.CostumeBabyAcc) ||
					(in_type == h_define.item_type_e.CostumeBabyCloth) ||
					(in_type == h_define.item_type_e.CostumeGirlAcc) ||
					(in_type == h_define.item_type_e.CostumeGirlCloth) ||
					(in_type == h_define.item_type_e.CostumeWomanAcc) ||
					(in_type == h_define.item_type_e.CostumeWomanCloth) ||
					(in_type == h_define.item_type_e.CostumeBackground);
		}
		
		// 카드타입 아이템 : PlayerCard, PetCard, FusionMaterialCard
		public bool IsCardItem(h_define.item_type_e in_type)
		{
			return (in_type == h_define.item_type_e.PlayerCard) || (in_type == h_define.item_type_e.PetCard) || (in_type == h_define.item_type_e.FusionMaterialCard);
		}

		public gplat.Result IsAbleConsumed(Int64 in_item_tid, Int32 in_consume_money)
		{
			var gen_result = new gplat.Result();
			var item = TakeUserMoney(in_item_tid);
			if (item == null)
			{
				return gen_result.setFail(result.h_code_e.ITEM_CONSUME_INVALID, $"User [{in_item_tid}] Item is invalid");
			}
			if (false == IsEnough(item.DbId, in_consume_money))
			{
				return gen_result.setFail(result.code_e.LACK_ITEM, $"Have Item({in_item_tid}) is {item.Count}, Need :{in_consume_money}");
			}

			return gen_result.setOk();
		}
		public gplat.Result IsAbleConsumeCount(Int64 in_item_tid, Int32 in_consume_count)
		{
			var gen_result = new gplat.Result();
			var item = FindItemByTId(in_item_tid);
			if (item == null)
			{
				return gen_result.setFail(result.h_code_e.ITEM_CONSUME_INVALID, $"User [{in_item_tid}] Item is invalid");
			}
			if (item.TotalCount() < in_consume_count)
			{
				return gen_result.setFail(result.code_e.LACK_ITEM, $"Have Item({in_item_tid}) is {item.Count}, Need :{in_consume_count}");
			}

			return gen_result.setOk();
		}


		// ===================================================================
		// 인벤토리 최대 카드 보유개수
		// ConfigMeta.m_h_limit_card_count
		// ===================================================================
		public bool IsInventoryCountOver(int predict_count = 0)
		{
			if (gplat.ConfigMeta.m_h_limit_card_count < m_player_cards.Count + predict_count)
			{
				return true;
			}
			return false;
		}

		// ===================================================================
		// 카운팅 아이템에 대해 최대 보유개수를 체크한다.
		// ===================================================================
		public bool IsItemCountOver(h_info.MetaItem in_item)
		{
			var item_meta_data = gplat.MetaData.byMetaId<h_item_meta.data>(in_item.ItemTid);
			if (item_meta_data == null || IsCountingItem(item_meta_data.ItemType) == false)
			{
				return false;
			}

			var have_item = FindItemByTId(in_item.ItemTid);
			if (have_item == null)
			{
				if (item_meta_data.HaveLimit < in_item.Count)
				{
					return true;
				}
				return false;
			}
			else
			{
				if (have_item.HaveLimitCount < have_item.Count + in_item.Count)
				{
					return true;
				}
			}
			return false;
		}

		// 가장 높은 등급의 카드의 티어를 찿는다.
		public h_define.item_tier_e HighestCardTier()
		{
			h_define.item_tier_e highest_tier = h_define.item_tier_e._NONE;
			foreach (var each_card in PlayerCards())
			{
				if (highest_tier < each_card.ItemTier)
				{
					highest_tier = each_card.ItemTier;
				}
			}
			return highest_tier;
		}


		//필요한 재화 추가하기 
		//필요한 재화 정보 가지고 오기
		public List<ItemRefill> MoneyItems()
		{
			return m_money_items;
		}

		// ===================================================================
		// 재화 리필 Process
		// - nextRefillTime, nextResetTime때문에 ImteDbUpdater를 사용할 수 없다.
		// - Logic에서 저장할 수 있는 정보를 생성해서 보내준다.
		// ===================================================================
		public bool ProcessRefill(out List<RefillResult> out_update_result)
		{
			out_update_result = new List<RefillResult>();
			foreach (var each_refill in m_refill_items)
			{
				if (true == each_refill.ProcessUpdate(out var out_update))
				{
					out_update_result.Add(out_update);
				}
			}
			return (out_update_result.Count > 0);
		}

		// ===================================================================
		// 기간제 아이템 만료 Process
		// ===================================================================
		public bool ProcessTimeLimit(out h_info.ExpireSaveInfo out_update_result)
		{
			out_update_result = new h_info.ExpireSaveInfo();
			foreach (var each_item in m_timelimit_items)
			{
				if (true == each_item.ProcessTimeLimit())
				{
					out_update_result.ExpireInfos.Add(each_item.DeepClone());
					each_item.ItemInfo.Count = 0;
					out_update_result.DbUpdateInfos.Add(each_item.DeepClone());
				}
			}
			return (out_update_result.ExpireInfos.Count > 0);
		}


		public List<h_info.InstantItem> DeepClone()
		{
			var h_item_infos = new List<h_info.InstantItem>();
			foreach (var item_info in m_items.Values)
			{
				h_item_infos.Add(item_info.DeepClone());
			}
			return h_item_infos;
		}


		// Deck정보의 갱신여부를 체크하기 위해서 전체 InstantItem을 출력한다.
		public string DeckLogInfo()
		{
			StringBuilder str_log = new StringBuilder();
			// m_instant_items
			str_log.AppendLine($"[instant_items]");
			foreach (var each_item in m_instant_items)
			{
				if (each_item.DeckNo > 0)
				{
					str_log.AppendLine($"	[{each_item.Db_id}:{each_item.Item_tid}] Exp:{each_item.Exp}, Deck:{each_item.DeckNo}");
				}
			}

			// m_items
			str_log.AppendLine($"[m_items]");
			foreach (var each_item in m_items.Values)
			{
				if (each_item.ItemInfo.DeckNo > 0)
				{
					str_log.AppendLine($"	[{each_item.ItemInfo.Db_id}:{each_item.ItemInfo.Item_tid}], Exp:{each_item.ItemInfo.Exp}, Deck:{each_item.ItemInfo.DeckNo}");
				}
			}

			// m_player_cards
			str_log.AppendLine($"[cards]");
			foreach (var each_item in m_player_cards)
			{
				if (each_item.ItemInfo.DeckNo > 0)
				{
					str_log.AppendLine($"	[{each_item.ItemInfo.Db_id}:{each_item.ItemInfo.Item_tid}], Exp:{each_item.ItemInfo.Exp}, Deck:{each_item.ItemInfo.DeckNo}");
				}
			}

			return str_log.ToString();
		}



		//=======================================================================
		// Buff 적용
		//=======================================================================
		public void RefreshBuff(BuffManager in_buffmanager)
		{
			m_pass_buff = in_buffmanager;
			foreach (var each_money in m_money_items)
			{
				each_money.SetBuff(in_buffmanager);
			}
		}
		// PremiumPass
		public void RefreshBuff(HPremiumPassInfo in_passinfo)
		{
			m_pass_buff.Clear();
			if (in_passinfo != null && in_passinfo.IsPaidPassValid)
			{
				m_pass_buff.Setup(in_passinfo.PassBuffs);
			}

			foreach (var each_money in m_money_items)
			{
				each_money.SetBuff(m_pass_buff);
			}
		}

		// ItemCard를 획득하는 경우 변경사항 추적
		//> Reddot 업데이트를 필요한 경우에만 진행하도록 설정함
		public bool GetChangedClear()
		{
			bool return_value = m_inventory_changed.Card;
			m_inventory_changed.Card = false;
			return return_value;
		}
		public void SetupReddotParam(ReddotParam in_param)
		{
			in_param.m_card_changed = m_inventory_changed.Card;
			in_param.m_levelup_material = m_inventory_changed.LevelupMaterial;
			in_param.m_petbuff_change = m_inventory_changed.PetBuff;
			m_inventory_changed.Clear();
		}

		// 펫 버프를 계산한다.
		//- 펫 정보를 가져오기 전에 호출되어야 한다.
		public void ComputePetBuff()
		{
			if (m_inventory_changed.PetBuff)
			{
				m_pet_buff.Clear();
				m_inventory_changed.PetBuff = false;
				foreach (var each_item in m_petbuff_items)
				{
					m_pet_buff.Attach(each_item.GetBuffInfo());
				}
				foreach (var each_pet_item in PetCards())
				{
					if (GetPetBuff(each_pet_item.CardTid, out h_info.BuffInfo out_value_buff, out h_info.BuffInfo out_rate_buff))
					{
						each_pet_item.SetBuff(out_value_buff, out_rate_buff);
					}
				}
			}
		}

		// 펫에 적용되어 있는 버프를 가져온다.
		// 해당 아이템에 적용 가능한 버프 목록을 가져와서 계산한다.
		public bool GetPetBuff(Int64 in_item_tid, out h_info.BuffInfo out_value_buff, out h_info.BuffInfo out_rate_buff)
		{
			out_value_buff = null;
			out_rate_buff = null;
			if (MetaItemBuff.it.FindTargetBuffList(in_item_tid, out var target_buff_list))
			{
				foreach (var each_buff in target_buff_list)
				{
					if (each_buff.BuffType > h_define.buff_type_e._RATE_BUFF_BEGINE)
					{
						out_rate_buff = new h_info.BuffInfo()
						{
							BuffType = each_buff.BuffType,
							Value = TotalPetBuffValue(each_buff.BuffType)
						};
					}
					else if (each_buff.BuffType > h_define.buff_type_e._VALUE_BUFF_BEGINE)
					{
						out_value_buff = new h_info.BuffInfo()
						{
							BuffType = each_buff.BuffType,
							Value = TotalPetBuffValue(each_buff.BuffType)
						};
					}
				}
				return true;
			}
			return false;
		}

		// 펫 버프를 가져온다(+전체버프).
		//- 전체를 더해준 값이므로 전체를 가져오면 2번 더해진다.
		public int TotalPetBuffValue(h_define.buff_type_e in_buff_type)
		{
			if (in_buff_type > h_define.buff_type_e._RATE_BUFF_BEGINE)
			{
				return m_pet_buff.GetBuffValue(h_define.buff_type_e.PET_ALL_RATE) + m_pet_buff.GetBuffValue(in_buff_type);
			}
			else if (in_buff_type > h_define.buff_type_e._VALUE_BUFF_BEGINE)
			{
				return m_pet_buff.GetBuffValue(h_define.buff_type_e.PET_ALL_VALUE) + m_pet_buff.GetBuffValue(in_buff_type);
			}
			return 0;
		}
		// 전체가 아닌 단일 값을 가져온다.
		public int PetBuffValue(h_define.buff_type_e in_buff_type)
		{
			if (in_buff_type > h_define.buff_type_e._VALUE_BUFF_BEGINE)
			{
				return m_pet_buff.GetBuffValue(in_buff_type);
			}
			return 0;
		}

		public string PetBuffString()
		{
			StringBuilder str_log = new StringBuilder();
			foreach (h_define.buff_type_e each_buff_type in Enum.GetValues(typeof(h_define.buff_type_e)))
			{
				if (each_buff_type > h_define.buff_type_e._VALUE_BUFF_BEGINE)
				{
					int value = PetBuffValue(each_buff_type);
					if (value > 0)
					{
						str_log.Append($"{each_buff_type}:{value}, ");
					}
				}
			}
			return str_log.ToString();
		}

	}



	// 인벤토리 아이템에 변경이 있을때 체크
	//- 변경시점에 매번 갱신이 아니라, 배치 처리가 필요한 경우 설정해서 사용한다.
	public class InventoryChange
	{
		bool m_card_change = false;
		bool m_levelup_material = false;
		bool m_petbuff_change = false;


		public bool Card { get { return m_card_change; } set { m_card_change = value; } }
		public bool LevelupMaterial { get { return m_levelup_material; } set { m_levelup_material = value; } }
		public bool PetBuff { get { return m_petbuff_change; } set { m_petbuff_change = value; } }

		public void Clear()
		{
			m_card_change = false;
			m_levelup_material = false;
			m_petbuff_change = false;
		}
	}

	// reddot 갱신시 전달되는 Param
	public class ReddotParam
	{
		// 인벤토리의 카드 변경
		public bool m_card_changed = false;
		public bool m_levelup_material = false;
		public bool m_petbuff_change = false;
	}
}
