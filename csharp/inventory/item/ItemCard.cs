using gplat;

using System;
using System.Collections.Generic;

namespace game
{

	//플레이어의 카드 아이템 
	public class ItemCard : ItemBase
	{
		//- static data 
		protected h2_item_fusion_meta.data m_meta_item_tier;
		protected h2_item_fusion_meta.TierInfo m_meta_item_tier_info;
		protected h2_item_level_meta.LevelInfo m_meta_item_level_info;

		protected Int32 m_card_word_idx = 0; //카드 한마디 결정방식 변경
		protected h_info.BuffInfo m_value_buff = new h_info.BuffInfo();
		protected h_info.BuffInfo m_rate_buff = new h_info.BuffInfo();

		public override string ToString()
		{
			if (ItemType == h_define.item_type_e.PlayerCard)
			{
				return $"   [{DbId}:{ItemTid}:{ItemName}] tier[{ItemTier}:{TierLevel}] level[{Level}] cattribute[{MainAttribute},{SubAttribute}] Char:{CharTid} >> combat:{CombatPower()}";
			}
			else
			{
				return $"   [{DbId}:{ItemTid}:{ItemName}] tier[{ItemTier}:{TierLevel}] level[{Level}] attribute[{PetV2Attribute}] combat[{CombatPower()} : {BuffedPower()}]";
			}
		}


		#region getter_setter
		//- shortcut
		public h2_item_fusion_meta.TierInfo MetaTierInfo => m_meta_item_tier_info;
		public h2_item_level_meta.LevelInfo MetaLevelInfo => m_meta_item_level_info;

		public h2_item_fusion_meta.TierInfo MetaNextTierInfo => GameMeta.CardGrade.NextFusionTierInfo(ItemOrigin, ItemTier, TierLevel);
		public h2_item_level_meta.LevelInfo MetaNextLevelInfo => GameMeta.CardLevel.H2NextLevelInfo(Level);

		public bool IsLock => m_instant_item.ItemLock;

		public Int64 CardTid
		{
			get
			{
				return m_instant_item.Item_tid;
			}
			set
			{
				m_instant_item.Item_tid = value;
			}
		}

		public Int32 CardExp
		{
			get
			{
				return m_instant_item.Exp;
			}
			set
			{
				m_instant_item.Exp = value;
			}
		}

		public Int32 CardNeedExpV2
		{
			get
			{
				if (null != MetaNextLevelInfo)
				{
					return MetaNextLevelInfo.BeginExp;
				}
				return 0;
			}
		}

		public Int32 CardBeginExpV2
		{
			get
			{
				if (null != m_meta_item_level_info)
				{
					return m_meta_item_level_info.BeginExp;
				}
				return 0;
			}
		}

		/// <summary>
		/// 카드/펫 레벨업 필요 영혼석
		/// </summary>
		public Int32 CardV2LevelUpMaterialCount
		{
			get
			{
				if (null != m_meta_item_level_info)
				{
					return m_meta_item_level_info.LevelupConsumes.MaterialCount;
				}
				return 0;
			}
		}

		/// <summary>
		/// 카드/펫 V2 레벨업 골드 필요량
		/// </summary>
		public h_info.MetaCountItem CardV2LevelUpConsumeMoney
		{
			get
			{
				if (null != m_meta_item_level_info)
				{
					return m_meta_item_level_info.LevelupConsumes.ConsumeMoney;
				}
				return null;
			}
		}

		/// <summary>
		/// 펫 V2 진화 진화석 필요량
		/// </summary>
		public Int32 PetV2EvolutionMaterialCount
		{
			get
			{
				if (m_meta_item_data.ItemType == h_define.item_type_e.PetCard)
				{
					return m_meta_item_tier_info.PetEvolutionMaterialCount;
				}
				return 0;
			}
		}

		public h_define.battle_attribute_e PetV2Attribute
		{
			get
			{
				if (m_meta_item_data.ItemType == h_define.item_type_e.PetCard)
				{
					return m_meta_item_data.CardAttributes.MainAttribute;
				}
				else
				{
					return h_define.battle_attribute_e._NONE;
				}
			}
		}

		/// <summary>
		/// 펫 V2 다음 레벨 파워
		/// </summary>
		public Int32 PetV2NextLevelPower
		{
			get
			{
				if (m_meta_item_data.ItemType == h_define.item_type_e.PetCard && IsMaxLevelV2() == false)
				{
					Int32 power = (Int32)((m_meta_item_tier_info.Power + (Level) * m_meta_item_tier_info.PowerPerLevel) * ContentPolicy.it.PetBaseWeight);
					return power;
				}
				return 0;
			}
		}

		/// <summary>
		/// 펫 V2 다음 등급 파워
		/// </summary>
		public Int32 PetV2NextGradePower
		{
			get
			{
				if (m_meta_item_data.ItemType == h_define.item_type_e.PetCard && IsMaxEvolutionV2() == false)
				{
					var next_tier_info = GameMeta.CardGrade.NextFusionTierInfo(ItemOrigin, ItemTier, TierLevel);
					Int32 power = (Int32)((next_tier_info.Power + (Level - 1) * next_tier_info.PowerPerLevel) * ContentPolicy.it.PetBaseWeight);

					return power;
				}
				return 0;
			}
		}

		public Int32 PetV2MaxLevel
		{
			get
			{
				if (m_meta_item_data.ItemType == h_define.item_type_e.PetCard)
				{
					int max_level = m_meta_item_tier_info.MaxLevel;

					return max_level;
				}
				return 0;
			}
		}

		public Int32 PetV2NextGradeMaxLevel
		{
			get
			{
				if (m_meta_item_data.ItemType == h_define.item_type_e.PetCard && IsMaxEvolutionV2() == false)
				{
					var next_tier_info = GameMeta.CardGrade.NextFusionTierInfo(ItemOrigin, ItemTier, TierLevel);
					return next_tier_info.MaxLevel;
				}
				return 0;
			}
		}

		public h_define.item_origin_e ItemOrigin
		{
			get
			{
				if (ItemType == h_define.item_type_e.PlayerCard || ItemType == h_define.item_type_e.PetCard || ItemType == h_define.item_type_e.FusionMaterialCard)
				{
					if (m_meta_item_data.CardAttributes.OrginAttribute != h_define.item_origin_e.Special)
					{
						return h_define.item_origin_e.Common;
					}
					return h_define.item_origin_e.Special;
				}
				return h_define.item_origin_e._NONE;

			}
		}

		public h_define.battle_attribute_e MainAttribute => m_meta_item_data.CardAttributes.MainAttribute;
		public h_define.battle_attribute_e SubAttribute => m_meta_item_data.CardAttributes.SubAttribute;

		public h_define.item_fusion_material_e FusionMaterialType
		{
			get
			{
				if (m_meta_item_tier_info == null)
				{
					return h_define.item_fusion_material_e._NONE;
				}
				return m_meta_item_tier_info.FusionMaterialType;
			}
		}

		public Int32 FusionMaterialCount => m_meta_item_tier_info.FusionMaterialCount;

		public Int32 BasicPower
		{
			get
			{
				return m_meta_item_tier_info.Power + (Level - 1) * m_meta_item_tier_info.PowerPerLevel;
			}
		}


		// 메시지
		public string WinMessage => m_meta_item_data.Word_win;
		public string LoseMessage => m_meta_item_data.Word_lose;

		public string WinMessageLstr => m_meta_item_data.LSTR("Word_win");
		public string LoseMessageLstr => m_meta_item_data.LSTR("Word_lose");

		public string CardWordText => CardWord.LSTR("Word");
		public string CardWordResource => CardWord.Resource;

		public h_item_meta.CardWord CardWord
		{
			get
			{
				if (m_meta_item_data.Card_word_list.Count > CardWordIdxV2)
				{
					return m_meta_item_data.Card_word_list[CardWordIdxV2];
				}
				else
				{
					Int32 last_index = m_meta_item_data.Card_word_list.Count - 1;
					return m_meta_item_data.Card_word_list[last_index];
				}
			}
		}

		public string Appearance
		{
			get
			{
				string out_string = "pet1";

				if (m_meta_item_tier_info.IsNull())
				{
					return out_string;
				}

				out_string = m_meta_item_tier_info.Appearance;
				return out_string;
			}
		}

		public Int32 CardWordIdxV2
		{
			get
			{
				Int32 card_word_idx = 0;
				card_word_idx = gplat.Random.Range(0, m_meta_item_data.Card_word_list.Count);

				return card_word_idx;
			}
		}

		public Int32 DeckSlotNo
		{
			get
			{
				// PVP에서만 사용될것으로 판단됨
				//- 단일덱이므로 DeckNo를 DeckSlotNo으로 사용
				//- Base 1
				if (m_meta_item_data.ItemType == h_define.item_type_e.PlayerCard)
				{
					return (Int32)m_instant_item.DeckNo;
				}
				else if (m_meta_item_data.ItemType == h_define.item_type_e.PetCard)
				{
					return (Int32)m_meta_item_data.CardAttributes.MainAttribute;
				}
				return 0;
			}
		}

		#endregion getter_setter




		//플레이어 카드 정보 세팅 
		public override gplat.Result Setup(h_info.InstantItem in_h_instant_item)
		{
			var gen_result = new gplat.Result();
			gen_result = base.Setup(in_h_instant_item);
			if (gen_result.fail())
			{
				return gen_result;
			}

			return SetupMetaData();
		}


		// 저장 후 커밋
		public gplat.Result Commit(h_info.InstantItem in_h_instant_item)
		{
			return Setup(in_h_instant_item);
		}

		protected gplat.Result SetupMetaData()
		{
			var gen_result = new gplat.Result();
			// 아이템 메타 세팅 
			m_meta_item_data = MetaData.byMetaId<h_item_meta.data>(ItemTid);
			if (null == m_meta_item_data)
			{
				return gen_result.setFail(result.code_e.NO_META_DATA, $"no meta data item Tid:{ItemTid}");
			}

			//#TODO 추후 루틴 개선 필요
			// 스페셜 아이템은 Epic 등급이 최소 이므로 보정한다.
			if (ItemOrigin == h_define.item_origin_e.Special)
			{
				if (ItemTier < h_define.item_tier_e.Epic)
				{
					m_instant_item.ItemTier = h_define.item_tier_e.Epic;
				}
			}

			// 등급 메타 세팅 
			if (SetupFusionTier(ItemOrigin, ItemTier, TierLevel) == false)
			{
				return gen_result.setFail(result.code_e.NO_META_DATA, $"no meta data item_fusion_meta Tid:{ItemTid}");
			}

			// 레벨 메타 세팅
			if (SetupLevelInfo(Level) == false)
			{
				return gen_result.setFail(result.code_e.NO_META_DATA, $"no meta data item_level_meta Tid:{ItemTid} Level:{Level}");
			}

			return gen_result.setOk();

		}

		protected bool SetupFusionTier(h_define.item_origin_e in_origin, h_define.item_tier_e in_tier, Int32 in_tier_level)
		{
			m_meta_item_tier = GameMeta.CardGrade.FusionInfos(in_origin);
			if (m_meta_item_tier == null)
			{
				m_meta_item_tier = GameMeta.CardGrade.FusionInfos(h_define.item_origin_e.Common);
			}

			m_meta_item_tier_info = GameMeta.CardGrade.FusionTierInfo(in_origin, in_tier, in_tier_level);
			if (m_meta_item_tier_info == null)
			{
				m_meta_item_tier_info = GameMeta.CardGrade.FusionTierInfo(h_define.item_origin_e.Common, h_define.item_tier_e.Normal, 1);
				if (m_meta_item_tier_info == null)
				{
					return false;
				}
			}
			return true;
		}

		protected bool SetupLevelInfo(Int32 in_level)
		{
			m_meta_item_level_info = GameMeta.CardLevel.H2LevelInfo(in_level);
			if (m_meta_item_level_info == null)
			{
				return false;
			}
			return true;
		}

		public void SetBuff(h_info.BuffInfo in_value_buff, h_info.BuffInfo in_rate_buff)
		{
			m_value_buff.BuffType = in_value_buff.BuffType;
			m_value_buff.Value = in_value_buff.Value;
			m_rate_buff.BuffType = in_rate_buff.BuffType;
			m_rate_buff.Value = in_rate_buff.Value;
		}


		//공격력 : 등급파워 + 레벨파워 호출할 때마다 달라짐 
		public virtual Int32 OffencePower()
		{
			Double offence_power = 0;
			if (IsItemType(h_define.item_type_e.PlayerCard))
			{
				offence_power = BasicPower;  // 카드
			}
			else
			{
				offence_power = (BasicPower * ContentPolicy.it.PetBaseWeight) * (1 + m_rate_buff.Value * 0.01) + m_value_buff.Value;     // 펫
			}

			return (Int32)offence_power;
		}


		// Quest OffencePower
		public virtual Int32 OffencePower(game.collection.CollectionManager in_collection, BattleParam in_battle_param)
		{
			Double offence_power = 0;
			if (IsItemType(h_define.item_type_e.PlayerCard))
			{
				double buff_value = 0;
				double char_buff_value = 0;
				if (in_battle_param.ContainMainAttribute(MainAttribute))
				{
					buff_value = ContentPolicy.it.QuestMainAttributeWeight(in_battle_param.m_stage_level);
				}
				if (in_battle_param.ContainSubAttribute(SubAttribute))
				{
					buff_value += ContentPolicy.it.QuestSubAttributeWeight(in_battle_param.m_stage_level);
				}
				if (in_battle_param.ContainChar(CharTid))
				{
					char_buff_value = ContentPolicy.it.CharTidWeight;
				}
				//gplat.Log.logger("battle").Error($"Power:{CollectionPower(in_collection)} * {buff_value} * {char_buff_value}");
				offence_power = (CollectionPower(in_collection) * (1 + buff_value)) * (1 + char_buff_value);  // 카드

			}
			else
			{
				double buff_value = 0;
				if (in_battle_param.ContainMainAttribute(MainAttribute))
				{
					buff_value = ContentPolicy.it.QuestPetAttributeWeight(in_battle_param.m_stage_level);
				}
				offence_power = BasicPower * ContentPolicy.it.PetBaseWeight * (1 + buff_value) * (1 + m_rate_buff.Value * 0.01) + m_value_buff.Value;  // 펫
			}
			return (Int32)offence_power;
		}
		// Quest Conflict OffencePower
		public virtual Int32 OffencePower(Double in_conflict_weight, BattleParam in_battle_param)
		{
			Double offence_power = 0;
			if (IsItemType(h_define.item_type_e.PlayerCard))
			{
				offence_power = 0;  // 카드
			}
			else
			{
				double buff_value = 0;
				if (in_battle_param.ContainMainAttribute(MainAttribute))
				{
					buff_value = ContentPolicy.it.QuestPetAttributeWeight(in_battle_param.m_stage_level);
				}
				offence_power = (BasicPower * ContentPolicy.it.PetBaseWeight * (1 + buff_value) * (1 + m_rate_buff.Value * 0.01) + m_value_buff.Value) * in_conflict_weight;  // 펫
			}
			return (Int32)offence_power;
		}

		//H2 Pvp OffencePower
		public virtual Int32 OffencePower(Int16 in_step, h_define.attack_type_e in_attack_type, game.collection.CollectionManager in_collection, BattleParam in_battle_param, int in_addition_power)
		{
			Double offence_power = 0;
			if (IsItemType(h_define.item_type_e.PlayerCard))
			{
				double buff_value = 0;
				double char_buff_value = 0;
				if (in_battle_param.ContainMainAttribute(MainAttribute))
				{
					buff_value = ContentPolicy.it.ArenaMainWeight;
				}
				if (in_battle_param.ContainSubAttribute(SubAttribute))
				{
					buff_value += ContentPolicy.it.ArenaSubWeight;
				}
				if (in_battle_param.ContainChar(CharTid))
				{
					char_buff_value = ContentPolicy.it.ArenaCharTidWeight;
				}
				offence_power = (CollectionPower(in_collection) * (1 + buff_value)) * (1 + char_buff_value) + in_addition_power;  // 카드

			}
			else
			{
				double buff_value = 0;
				if (in_battle_param.ContainMainAttribute(MainAttribute))
				{
					buff_value = ContentPolicy.it.ArenaPetWeight;
				}
				offence_power = BasicPower * ContentPolicy.it.PetBaseWeight * (1 + buff_value) * (1 + m_rate_buff.Value * 0.01) + m_value_buff.Value + in_addition_power;  // 펫
			}
			return (Int32)offence_power;
		}

		//RelationDungeon OffencePower
		public virtual Int32 DungeonOffencePower(game.collection.CollectionManager in_collection, BattleParam in_battle_param)
		{
			Double offence_power = 0;
			if (IsItemType(h_define.item_type_e.PlayerCard))
			{
				double buff_value = 0;
				double char_buff_value = 0;
				if (in_battle_param.ContainMainAttribute(MainAttribute))
				{
					buff_value = ContentPolicy.it.DungeonMainAttributeWeight(in_battle_param.m_stage_level);
				}
				if (in_battle_param.ContainSubAttribute(SubAttribute))
				{
					buff_value += ContentPolicy.it.DungeonSubAttributeWeight(in_battle_param.m_stage_level);
				}
				//gplat.Log.logger("battle").Error($"Power:{CollectionPower(in_collection)} * {buff_value} * {char_buff_value}");
				offence_power = (CollectionPower(in_collection) * (1 + buff_value)) * (1 + char_buff_value);  // 카드
			}
			else
			{
				double buff_value = 0;
				if (in_battle_param.ContainMainAttribute(MainAttribute))
				{
					buff_value = ContentPolicy.it.DungeonPetAttributeWeight(in_battle_param.m_stage_level);
				}
				offence_power = BasicPower * ContentPolicy.it.PetBaseWeight * (1 + buff_value) * (1 + m_rate_buff.Value * 0.01) + m_value_buff.Value;  // 펫
			}
			return (Int32)offence_power;
		}
		// RelationDungeon Conflict OffencePower
		public virtual Int32 DungeonOffencePower(Double in_conflict_weight, BattleParam in_battle_param)
		{
			Double offence_power = 0;
			if (IsItemType(h_define.item_type_e.PlayerCard))
			{
				offence_power = 0;  // 카드
			}
			else
			{
				double buff_value = 0;
				if (in_battle_param.ContainMainAttribute(MainAttribute))
				{
					buff_value = ContentPolicy.it.DungeonPetAttributeWeight(in_battle_param.m_stage_level);
				}
				offence_power = (BasicPower * ContentPolicy.it.PetBaseWeight * (1 + buff_value) * (1 + m_rate_buff.Value * 0.01) + m_value_buff.Value) * in_conflict_weight;  // 펫
			}
			return (Int32)offence_power;
		}


		// 기본 파워(UI표시용)
		public virtual Int32 CombatPower()
		{
			if (IsValid && IsItemType(h_define.item_type_e.PlayerCard)) // 카드
			{
				return (Int32)BasicPower;
			}
			else if (IsItemType(h_define.item_type_e.PetCard))      // 펫
			{
				return (Int32)(BasicPower * ContentPolicy.it.PetBaseWeight * (1 + m_rate_buff.Value * 0.01) + m_value_buff.Value);
			}

			return 0;
		}

		// 버프로 증가된 공격력
		//- 펫만 사용된다.
		public virtual Int32 BuffedPower()
		{
			if (IsItemType(h_define.item_type_e.PetCard))
			{
				return (Int32)BasicPower;
			}
			else if (IsItemType(h_define.item_type_e.PetCard))
			{
				// 펫
				return (Int32)(BasicPower * ContentPolicy.it.PetBaseWeight * m_rate_buff.Value * 0.01) + m_value_buff.Value;
			}
			return 0;
		}


		public virtual Int32 CollectionPower(game.collection.CollectionManager collection_manager)
		{
			if (collection_manager != null)
			{
				return collection_manager.CalcPower(this, BasicPower);
			}
			else
			{
				return BasicPower;
			}
		}

		// Quest 공격
		public AttackInfo Attack(collection.CollectionManager in_collection, BattleParam in_battle_param, h_define.attack_type_e in_attack_type)
		{
			var attack_info = new AttackInfo();
			attack_info.m_attack_by = in_attack_type;
			attack_info.m_player_card = this;
			var attack_power = OffencePower(in_collection, in_battle_param);
			attack_info.m_offence_power = attack_power;
			attack_info.m_attack_log = String.Format($"{this}, StageBuff:{in_battle_param}, Collection:{CollectionPower(in_collection)} > {attack_power}");
			attack_info.SetBuffInfo(in_battle_param);
			return attack_info;
		}
		// Quest Conflict 공격
		public AttackInfo Attack(Double in_conflict_weight, BattleParam in_battle_param, h_define.attack_type_e in_attack_type)
		{
			var attack_info = new AttackInfo();
			attack_info.m_attack_by = in_attack_type;
			attack_info.m_player_card = this;
			var attack_power = OffencePower(in_conflict_weight, in_battle_param);
			attack_info.m_offence_power = attack_power;
			attack_info.m_attack_log = String.Format($"{this}, Conflict:{in_battle_param}, Weight:{in_conflict_weight} > Attack:{attack_power}");
			attack_info.SetBuffInfo(in_battle_param);
			return attack_info;
		}

		// PVP 공격
		public AttackInfo Attack(Int16 in_step, collection.CollectionManager in_collection, BattleParam in_pvp_param, h_define.attack_type_e in_attack_type, int in_addition_power)
		{
			var attack_info = new AttackInfo();
			attack_info.m_attack_by = in_attack_type;
			attack_info.m_player_card = this;
			var attack_power = OffencePower(in_step, in_attack_type, in_collection, in_pvp_param, in_addition_power);
			attack_info.m_offence_power = attack_power;
			attack_info.m_attack_log = String.Format($"{this}, BattleArg:{in_pvp_param}, Collection:{CollectionPower(in_collection)}, Addition:{in_addition_power} > {attack_power}");

			return attack_info;
		}


		// 인연던전 공격
		public AttackInfo DungeonAttack(collection.CollectionManager in_collection, BattleParam in_battle_param, h_define.attack_type_e in_attack_type)
		{
			var attack_info = new AttackInfo();
			attack_info.m_attack_by = in_attack_type;
			attack_info.m_player_card = this;
			var attack_power = DungeonOffencePower(in_collection, in_battle_param);
			attack_info.m_offence_power = attack_power;
			attack_info.m_attack_log = String.Format($"{this}, StageBuff:{in_battle_param}, Collection:{CollectionPower(in_collection)} > {attack_power}");
			attack_info.SetBuffInfo(in_battle_param);
			return attack_info;
		}
		// 인연던전 Conflict 공격
		public AttackInfo DungeonAttack(Double in_conflict_weight, BattleParam in_battle_param, h_define.attack_type_e in_attack_type)
		{
			var attack_info = new AttackInfo();
			attack_info.m_attack_by = in_attack_type;
			attack_info.m_player_card = this;
			var attack_power = DungeonOffencePower(in_conflict_weight, in_battle_param);
			attack_info.m_offence_power = attack_power;
			attack_info.m_attack_log = String.Format($"{this}, Conflict:{in_battle_param}, Weight:{in_conflict_weight} > Attack:{attack_power}");
			attack_info.SetBuffInfo(in_battle_param);
			return attack_info;
		}



		// 최대 레벨인가
		//> ExtraCard의 경우 강화, 진화 불가
		public bool IsMaxLevel()
		{
			return (Level >= m_meta_item_tier_info.MaxLevel);
		}
		public bool IsMaxLevel(out Int32 out_max_level)
		{
			out_max_level = m_meta_item_tier_info.MaxLevel;
			return IsMaxLevel();
		}

		public bool IsMaxFusion()
		{
			var last_fusion_data = GameMeta.CardGrade.LastFusionTierInfo(ItemOrigin);
			if (last_fusion_data != null)
			{
				return (last_fusion_data.TierType == ItemTier && last_fusion_data.TierLevel == TierLevel);
			}
			return false;
		}

		public bool IsMaxEvolutionV2()
		{
			var last_tier_info = GameMeta.CardGrade.LastFusionTierInfo(ItemOrigin);

			if (ItemTier == last_tier_info.TierType && TierLevel == last_tier_info.TierLevel)
			{
				return true;
			}
			return false;
		}

		// 레벨업 체크 (Inventory의 soul_item에서 찾아서 체크)
		public bool LevelUpgradable(List<ItemNormal> in_soul_items)
		{
			if( IsMaxLevel())
			{
				return false;
			}

			if( in_soul_items.Count < 1)
			{
				return false;
			}

			// 소비 아이템 체크
			ItemNormal consume_material_item = in_soul_items.Find(t => t.ItemTid == ItemLinkTid);
			if(consume_material_item == null)
			{
				return false;
			}	
			if (consume_material_item.Count < CardV2LevelUpMaterialCount)
			{
				return false;
			}

			// 소비 재화 체크 => 외부에서 체크한다.
			//var h_meta_consume_money_item = CardV2LevelUpConsumeMoney;

			return true;
		}



		public gplat.Result Fusionable(ItemBase in_consume_card)
		{
			var gplat_result = new gplat.Result();

			// 최대레벨 체크
			if (IsMaxFusion())
			{
				return gplat_result.setFail(result.h_code_e.CARD_EVOLUTION_LIMITED, $"Already Max Level :{DbId}:{CardTid}");
			}

			var consume_card = in_consume_card as ItemCard;

			// #24245 재료아이템과 융합아이템의 태생이 동일해야한다.
			if (consume_card.ItemOrigin != ItemOrigin)
			{
				return gplat_result.setFail(result.h_code_e.CARD_FUSION_NO_SAME_ORIGIN, $"Not Same Origine [ Consume Card ({consume_card.ItemOrigin}) != Fusion Card ( {ItemOrigin} ) ]");
			}

			// #24245 재료아이템과 융합아이템의 티어가 동일해야한다.
			if (consume_card.ItemTier != ItemTier)
			{
				return gplat_result.setFail(result.h_code_e.CARD_INVALID_FUSION_ITEM, $"{in_consume_card.CharTid} == {CharTid} && {in_consume_card.ItemTier} == {ItemTier}");
			}

			// ItemType 이 융합재료(FusionMaterialCard) 라면 그대로 완료
			if (in_consume_card.ItemType == h_define.item_type_e.FusionMaterialCard)
			{
				return gplat_result.setOk();
			}

			// #24245 재료아이템과 융합아이템의 티어레벨이 동일해야한다.
			if (consume_card.TierLevel != TierLevel)
			{
				return gplat_result.setFail(result.h_code_e.CARD_FUSION_NO_SAME_TIER_LEVEL, $"Not Same TierLevel [ Consume Card ({consume_card.TierLevel}) != Fusion Card ( {TierLevel} ) ]");
			}

			// #24245 재료아이템과 융합아이템의 캐릭터가 동일해야한다.
			if (in_consume_card.CharTid != CharTid)
			{
				return gplat_result.setFail(result.h_code_e.CARD_INVALID_FUSION_ITEM, $"{in_consume_card.CharTid} == {CharTid} && {in_consume_card.ItemTier} == {ItemTier}");
			}

			// #24245 FusionMaterialType 이 SameItem 일경우, ItemTid(아이템) 가 동일한지 체크한다.
			if (FusionMaterialType == h_define.item_fusion_material_e.SameItem)
			{
				if (in_consume_card.ItemTid != ItemTid)
				{
					return gplat_result.setFail(result.h_code_e.CARD_INVALID_FUSION_ITEM, $"[FusionMaterialType : {FusionMaterialType}]Item Tid is not same ! {in_consume_card.ItemTid} == {ItemTid}");
				}
			}

			return gplat_result.setOk();
		}
		public virtual bool IsFusionable(ItemBase in_consume_card)
		{
			// 최대레벨 체크
			if (IsMaxFusion())
			{
				return false;
			}

			var consume_card = in_consume_card as ItemCard;

			// #24245 재료아이템과 융합아이템의 태생이 동일해야한다.
			if (consume_card.ItemOrigin != ItemOrigin)
			{
				return false;
			}

			// #24245 재료아이템과 융합아이템의 티어가 동일해야한다.
			if (consume_card.ItemTier != ItemTier)
			{
				return false;
			}

			// ItemType 이 융합재료(FusionMaterialCard) 라면 그대로 완료
			if (in_consume_card.ItemType == h_define.item_type_e.FusionMaterialCard)
			{
				return true;
			}

			// #24245 재료아이템과 융합아이템의 티어레벨이 동일해야한다.
			if (consume_card.TierLevel != TierLevel)
			{
				return false;
			}

			// #24245 재료아이템과 융합아이템의 캐릭터가 동일해야한다.
			if (in_consume_card.CharTid != CharTid)
			{
				return false;
			}

			// #24245 FusionMaterialType 이 SameItem 일경우, ItemTid(아이템) 가 동일한지 체크한다.
			if (FusionMaterialType == h_define.item_fusion_material_e.SameItem)
			{
				if (in_consume_card.ItemTid != ItemTid)
				{
					return false;
				}
			}

			return true;
		}

		public bool IsFusionable(List<ItemCard> in_consume_card_list, List<ItemCard> in_exclude_card_list = null)
		{
			if (in_exclude_card_list != null)
			{
				if( in_exclude_card_list.Find(t => t.DbId == DbId) != null)
				{
					return false;
				}
			}

			foreach (var consume_card in in_consume_card_list)
			{
				if (DbId == consume_card.DbId)
				{
					continue;
				}
				if (m_meta_item_data == null)
				{
					continue;
				}
				if (in_exclude_card_list != null)
				{
					if (in_exclude_card_list.Find(t => t.DbId == consume_card.DbId) != null)
					{
						continue;
					}
				}

				if (IsFusionable(consume_card) == false)
				{
					continue;
				}

				return true;
			}
			return false;
		}

		public gplat.Result Fusionable(List<ItemCard> in_consume_card_list)
		{
			var gplat_result = new gplat.Result();

			for (int i = 0; i < in_consume_card_list.Count; i++)
			{
				ItemCard consume_card = in_consume_card_list[i];
				if (DbId == consume_card.DbId)
				{
					continue;
				}
				if (m_meta_item_data == null)
				{
					continue;
				}

				if (IsFusionable(consume_card) == false)
				{
					continue;
				}

				return gplat_result.setOk();
			}

			return gplat_result.setFail(result.h_code_e.CARD_INVALID_FUSION_ITEM, $"[FusionMaterialType : {FusionMaterialType}]Item Tid is not same !");
		}

		public gplat.Result CalcV2CardResetRecoveryMaterial(out h_info.MetaItem out_count_material, out h_info.MetaItem out_count_money)
		{
			var gplat_result = new gplat.Result();
			Int32 target_count_material = 0;
			Int32 target_count_money = 0;

			out_count_material = null;
			out_count_money = null;

			var meta_h2_item_level_info = GameMeta.CardLevel.H2LevelInfo(Level);
			if (meta_h2_item_level_info == null)
			{
				return gplat_result.setFail(result.h_code_e.CARD_ITEM_LEVEL_NO_DATA, $"Tid: {ItemTid} Level : {Level}");
			}

			//영혼석 개수 계산
			for (Int32 i = 1; i < Level; i++)
			{
				var each_meta_h2_item_level_info = GameMeta.CardLevel.H2LevelInfo(i);
				if (each_meta_h2_item_level_info == null)
				{
					return gplat_result.setFail(result.h_code_e.CARD_ITEM_LEVEL_NO_DATA, $"Tid: {ItemTid} Level : {i}");
				}

				target_count_material = target_count_material + each_meta_h2_item_level_info.LevelupConsumes.MaterialCount;
				target_count_money = target_count_money + each_meta_h2_item_level_info.LevelupConsumes.ConsumeMoney.Count;
			}

			target_count_material = (Int32)Math.Truncate(meta_h2_item_level_info.CardResetRefundInfo.MaterialRefundValue * target_count_material); //내림처리
			target_count_money = (Int32)Math.Truncate(meta_h2_item_level_info.CardResetRefundInfo.MoneyRefundValue * target_count_money); //내림처리

			out_count_material = new h_info.MetaItem()
			{
				ActionType = h_define.item_action_e.Item,
				ItemTid = ItemLinkTid,
				Count = target_count_material
			};

			out_count_money = new h_info.MetaItem()
			{
				ActionType = h_define.item_action_e.Item,
				ItemTid = CardV2LevelUpConsumeMoney.ItemTid,
				Count = target_count_money
			};

			return gplat_result.setOk();
		}

		public gplat.Result CalcV2PetResetRecoveryMaterial(out Int32 out_count_levelup_material, out Int32 out_count_money, out Int32 out_count_evolution_material)
		{
			var gplat_result = new gplat.Result();
			out_count_levelup_material = 0;
			out_count_money = 0;
			out_count_evolution_material = 0;

			Int32 target_pet_level = Level;
			Int32 target_count_levelup_material = 0;
			Int32 target_count_money = 0;
			Int32 target_count_evolution_material = 0;

			// 영혼석 개수 계산 (레벨)
			for (Int32 i = 1; i < target_pet_level; i++)
			{
				var meta_h2_item_level_info = GameMeta.CardLevel.H2LevelInfo(i);
				if (meta_h2_item_level_info == null)
				{
					return gplat_result.setFail(result.h_code_e.PET_ITEM_LEVEL_NO_DATA, $"Tid: {ItemTid} Level : {i}");
				}

				target_count_levelup_material += meta_h2_item_level_info.LevelupConsumes.MaterialCount;
				target_count_money += meta_h2_item_level_info.LevelupConsumes.ConsumeMoney.Count;
			}

			// 현재 tier info
			h2_item_fusion_meta.TierInfo fusion_tier_info = m_meta_item_tier_info;
			while (true)
			{
				var previous_fusion_tier_info = GameMeta.CardGrade.PreviousFusionTierInfo(ItemOrigin, fusion_tier_info.TierType, fusion_tier_info.TierLevel);
				if (previous_fusion_tier_info == null)
				{
					break;
				}
				target_count_evolution_material += previous_fusion_tier_info.PetEvolutionMaterialCount;
				fusion_tier_info = previous_fusion_tier_info;
			}

			out_count_levelup_material = target_count_levelup_material;
			out_count_money = target_count_money;
			out_count_evolution_material = target_count_evolution_material;

			return gplat_result.setOk();
		}

		public h2_item_fusion_meta.TierInfo PetV2NextUpgradeEvolutionInfo()
		{
			if (m_meta_item_data.ItemType == h_define.item_type_e.PetCard && IsMaxEvolutionV2() == false)
			{
				var next_tier_info = GameMeta.CardGrade.NextFusionTierInfo(ItemOrigin, ItemTier, TierLevel);
				return next_tier_info;
			}
			return null;
		}

		public bool ShouldEnableAnimation()
		{
			return (ItemOrigin == h_define.item_origin_e.Special && h_define.item_tier_e.Epic <= ItemTier) || h_define.item_tier_e.Epic <= ItemTier;
		}

		/// <summary>
		/// 현재 진화단계에서의 최대 레벨인지 검사
		/// </summary>
		/// <returns></returns>
		public bool IsMaxLevelV2()
		{
			if (Level >= m_meta_item_tier_info.MaxLevel)
			{
				return true;
			}
			return false;
		}


		//#삭제예정
		// 덱슬롯 : [카드 속성-1] = DeckSlot[0]:Thinking[1] / DeckSlot[1]:Concentration[2] / DeckSlot[2]:Judgment[3] / DeckSlot[3]:Creative[4]
		// 펫 전용. 펫에는 메인 속성만 존재.
		public Int32 DeckSlot
		{
			get
			{
				if (m_meta_item_data == null)
				{
					//gplat.Log.logger("item.card").Error($"meta_item_data is null: dbId:{DbId}");
					return -1;
				}
				return (Int32)m_meta_item_data.CardAttributes.MainAttribute - 1;
			}
		}
		//#삭제예정: 설정 Deck No
		public Int16 DeckGroupNo => (m_instant_item.DeckNo);




	}


	public static class BattleExtention
	{
		public static bool Contains(this h_info.BattleParamAttribute base_buff, h_define.battle_attribute_e in_attribute)
		{
			if (in_attribute != h_define.battle_attribute_e._NONE &&
				(base_buff.Attribute01 == in_attribute || base_buff.Attribute02 == in_attribute))
			{
				return true;
			}
			return false;
		}
	}

}
