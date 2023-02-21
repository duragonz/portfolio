using System;
using System.Collections.Generic;
using System.Text;

namespace game
{
	//속성에 따라 카드를 장착한다.
	//장착한 카드를 마지막 퀘스트 보상으로 부여한다. 
	//카드덱을 패킷으로 변환 
	public class ItemCardDeck
	{
		Int32 DECK_CARD_COUNT = 4;
		const int DECK_RANGE_MIN = 0;
		const int DECK_RANGE_MAX = 3;

		public string m_name; //덱이름 

		List<ItemCard> m_deck = new List<ItemCard>() { null, null, null, null }; //슬롯만 확보
		h_define.item_type_e m_item_type;



		public int MaxDeckCount => DECK_CARD_COUNT;

		public List<ItemCard> PlayerCards => m_deck;



		public ItemCardDeck(h_define.item_type_e in_type)
		{
			m_item_type = in_type;
			m_deck.Clear();
			for (int i = 0; i < DECK_CARD_COUNT; ++i)
			{
				var item_card = new ItemCard();
				m_deck.Add(item_card);
			}
		}

		//수정하면 곤란해질 수 있으므로 깊은 복사
		public List<h_info.InstantItem> MakeInstantItems()
		{
			var instantitems = new List<h_info.InstantItem>();
			foreach (var player_card in m_deck)
			{
				if (player_card == null)
				{
					continue;
				}
				instantitems.Add((h_info.InstantItem)player_card.m_instant_item.DeepClone());
			}
			return instantitems;
		}

		// 일반 카드는 슬롯을 설정해서 장착한다.
		public bool SetCard(Int32 in_slot_index, ItemCard in_player_card)
		{
			var gen_result = new gplat.Result();
			if (in_slot_index < DECK_RANGE_MIN || in_slot_index > DECK_RANGE_MAX)
			{
				gplat.Log.logger("app").Error($"BattleDeck out of range :{in_slot_index}");
				return false;
			}
			m_deck[in_slot_index] = in_player_card;
			return true;
		}
		// 아이템 리스트는 순서대로 장착
		public void SetCard(List<h_info.InstantItem> in_card_items)
		{
			for (int i = 0; i < in_card_items.Count; i++)
			{
				SetCard(i, in_card_items[i]);
			}
		}
		public bool SetCard(Int32 in_slot_index, h_info.InstantItem in_card_item)
		{
			ItemCard set_card = null;
			if (in_card_item.ItemType == h_define.item_type_e.PlayerCard)
			{
				set_card = new ItemCard();
			}
			else
			{
				set_card = new PetCard();
			}

			var result = set_card.Setup(in_card_item);
			if (result.fail())
			{
				gplat.Log.logger("app").Error($"ItemCard setup error:{result.descString()}");
				return false;
			}
			if (in_slot_index < DECK_RANGE_MIN || in_slot_index > DECK_RANGE_MAX)
			{
				gplat.Log.logger("app").Error($"BattleDeck out of range :{in_slot_index}");
				return false;
			}
			m_deck[in_slot_index] = set_card;
			return true;
		}
		public ItemCard GetCard(Int32 in_slot_index)
		{
			if (in_slot_index < DECK_RANGE_MIN || in_slot_index > DECK_RANGE_MAX)
			{
				gplat.Log.logger("app").Error($"GetCard out of range :{in_slot_index}");
				return null;
			}
			return m_deck[in_slot_index];
		}

		// 펫은 주 속성을 보고 장착한다.
		public void SetCardByAttribute(ItemCard in_player_card)
		{
			int slot_index = DeckSlot(in_player_card.MainAttribute);
			if (slot_index >= DECK_RANGE_MIN && slot_index <= DECK_RANGE_MAX)
			{
				m_deck[slot_index] = in_player_card;
			}
		}
		public Int32 DeckSlot(h_define.battle_attribute_e in_attribute)
		{
			if (h_define.battle_attribute_e._NONE == in_attribute ||
				h_define.battle_attribute_e._END == in_attribute)
			{
				return 0;
			}
			return (Int32)in_attribute - 1;
		}

		// 아이템이 덱에 포함되어 있는지 확인
		public bool IsContain(Int64 in_item_dbid)
		{
			foreach (var each_card in m_deck)
			{
				if (each_card.DbId == in_item_dbid)
				{
					return true;
				}
			}
			return false;
		}
		public bool IsCardValid()
		{
			foreach (var each_card in m_deck)
			{
				if (null == each_card || false == each_card.IsValid)
				{
					return false;
				}
			}
			return true;
		}


		//PVP_Deploy 정보를 통해서 Deck을 설정한다.
		// - PVP Default Bot
		public gplat.Result SetupCardFromDeploy(Int64 in_char_uid, Int32 in_deploy_no)
		{
			var gen_result = new gplat.Result();
			if (false == game.MetaPvpDeployInfo.it.GetDeployInfo(in_deploy_no, out var deploy_bot_data))
			{
				return gen_result.setFail(result.h_code_e.PVP_ENERMY_DEPLOY_NO_DATA, $"no pvp deploy in_deploy_no:{in_deploy_no}");
			}

			// 카드 Index
			List<Int64> card_tids = CardTids();
			Int32 start_card_idx = gplat.Random.Range(0, card_tids.Count) % (card_tids.Count - 1);

			//덱 슬롯 4개
			for (Int32 i = 0; i < DECK_CARD_COUNT; ++i)
			{
				if (MakeCardFromDeploy(in_char_uid, deploy_bot_data, start_card_idx + i, out h_info.InstantItem out_item))
				{
					m_deck[i].Setup(out_item);
				}
				else
				{
					// 없는 Tid인 경우 생성되지 않는다.
				}

			}
			return gen_result;
		}
		//PVP_Deploy 정보를 통해서 Deck을 설정한다.
		//- 펫은 순서대로 만들어 준다.
		public gplat.Result SetupPetFromDeploy(Int64 in_char_uid, Int32 in_deploy_no)
		{
			var gen_result = new gplat.Result();
			if (false == game.MetaPvpDeployInfo.it.GetDeployInfo(in_deploy_no, out var deploy_bot_data))
			{
				return gen_result.setFail(result.h_code_e.PVP_ENERMY_DEPLOY_NO_DATA, $"no pvp deploy in_deploy_no:{in_deploy_no}");
			}
			for (Int32 i = 0; i < DECK_CARD_COUNT; ++i)
			{
				if (MakeCardFromDeploy(in_char_uid, deploy_bot_data, i, out h_info.InstantItem out_item))
				{
					m_deck[i].Setup(out_item);
				}
			}
			return gen_result;
		}
		// Index 생성
		bool MakeCardFromDeploy(Int64 in_char_uid, h_pvp_deploy_meta.MatchBotItem in_deploy_bot, Int32 in_seq_no, out h_info.InstantItem out_item)
		{
			List<Int64> card_tids = CardTids();
			var tid_idx = in_seq_no % (card_tids.Count - 1);
			out_item = gplat.RewardCreater.MakeH2DeckItem(in_char_uid, card_tids[tid_idx], in_deploy_bot.CardTier, in_deploy_bot.CardTierLevel, gplat.Random.Range(in_deploy_bot.CardlevelRange.MinValue, in_deploy_bot.CardlevelRange.MaxValue));
			return true;
		}
		// 랜덤 생성
		public bool MakeCardFromDeploy(Int64 in_char_uid, h_pvp_deploy_meta.MatchBotItem in_deploy_bot, out h_info.InstantItem out_item)
		{
			List<Int64> card_tids = CardTids();
			var tid_idx = gplat.Random.Range(0, card_tids.Count);

			out_item = gplat.RewardCreater.MakeH2DeckItem(in_char_uid, card_tids[tid_idx], in_deploy_bot.CardTier, in_deploy_bot.CardTierLevel, gplat.Random.Range(in_deploy_bot.CardlevelRange.MinValue, in_deploy_bot.CardlevelRange.MaxValue));
			return true;
		}



		List<Int64> CardTids()
		{
			List<Int64> card_tids;

			if (h_define.item_type_e.PlayerCard == m_item_type)
			{
				card_tids = GameMeta.Item.ShuffledPlayerCardTids(); //캐릭터단위로 골고루 섞어서 주도록 반영 
			}
			else
			{
				card_tids = GameMeta.Item.PetCardTids();
			}
			return card_tids;
		}



		public Int32 TotalCombatPower()
		{
			Int32 total_combat_power = 0;

			for (Int32 i = 0; i < PlayerCards.Count; i++)
			{
				ItemCard card = PlayerCards[i];
				if (card == null || card.IsValid == false)
				{
					continue;
				}

				total_combat_power += card.CombatPower();
			}

			return total_combat_power;
		}


		public override string ToString()
		{
			StringBuilder str_log = new StringBuilder();
			for (int i = 0; i < m_deck.Count; i++)
			{
				if (m_deck[i] == null)
				{
					str_log.AppendLine($"	[{i}] is null");
				}
				else
				{
					str_log.AppendLine($"{m_deck[i]}");
				}
			}
			return str_log.ToString();

		}

		// ================================================================================================
		//#삭제예정: Deprecated

		//속성덱에만 꼽을 수 있다.  
		public gplat.Result SetCard(ItemCard in_player_card)
		{
			var gen_result = new gplat.Result();

			if (in_player_card.DeckSlotNo < 1) //슬롯 계산이 안되는 경우는 메타데이터가 없는 경우이다
			{
				return gen_result.setFail(result.code_e.NO_META_DATA, $"Invalid deck slot:[{in_player_card.DeckSlotNo}], dbid:[{in_player_card.DbId}], Tier:[{in_player_card.ItemTier}:{in_player_card.TierLevel}]");
			}

			m_deck[in_player_card.DeckSlotNo - 1] = in_player_card;
			return gen_result;
		}


		// 전투 가능 조건 체크
		// - 4개의 카드가 모두 설정되어 있어야 한다.
		public gplat.Result BattleValid()
		{
			gplat.Result gen_result = new gplat.Result();
			foreach (var each_card in m_deck)
			{
				if (each_card.CardTid < 0)
				{
					return gen_result.setFail(result.h_code_e.ITEM_CARD_DECK_NEED, "PVP DECK Empty");
				}
			}
			return gen_result.setOk();
		}



		public h_define.battle_attribute_e DeckSlotAttribute(Int32 in_deck_slot)
		{
			if (in_deck_slot <= 0 || in_deck_slot >= (Int32)h_define.battle_attribute_e._END)
			{
				return h_define.battle_attribute_e._NONE;
			}

			return (h_define.battle_attribute_e)(in_deck_slot);
		}


		public bool HaveCards()
		{
			foreach (var item_card in m_deck)
			{
				if (item_card != null && item_card.IsValid)
				{
					return true;
				}
			}
			return false;
		}

	}
}
