using System;
using System.Collections.Generic;

namespace game
{
	// Meta에 설정되지 않은 Item 보상 및 소비 정보
	public class RewardItem
	{
		public List<h_info.RewardItemInfo> m_rewarditem_infos = new List<h_info.RewardItemInfo>();


		public List<h_info.RewardItemInfo> GetRewardItemInfos()
		{
			var h_rewarditem_infos = new List<h_info.RewardItemInfo>();
			h_rewarditem_infos.AddRange(m_rewarditem_infos);
			return h_rewarditem_infos;
		}


		public gplat.Result Setup(List<h_info.RewardItemInfo> in_infos)
		{
			var gen_result = new gplat.Result();

			m_rewarditem_infos.Clear();
			foreach (var in_reward_item in in_infos)
			{
				if (GameMeta.MetaRewardItem.ContainRewardItems(in_reward_item.RewardItemTid))
				{
					m_rewarditem_infos.Add(in_reward_item);
				}
				else
				{
					return gen_result.setFail($"h_reward_item_meta no data : {in_reward_item}");
				}
			}

			return gen_result;
		}


		// 보상을 받았는지 체크
		public bool IsRewardReceived(Int64 in_rewaditem_tid)
		{
			if (GameMeta.MetaRewardItem.ContainRewardItems(in_rewaditem_tid))
			{
				foreach (var each_item in m_rewarditem_infos)
				{
					if (each_item.RewardItemTid == in_rewaditem_tid && each_item.RewardState != 0)
					{
						return true;
					}
				}
			}
			return false;
		}
		public bool IsRewardReceived(string in_hash_tag)
		{
			if (GameMeta.MetaRewardItem.GetRewardItems(in_hash_tag, out var out_reward_meta_info))
			{
				return IsRewardReceived(out_reward_meta_info.Tid);
			}
			return false;
		}


		// 보상 아이템 정보
		public List<h_info.MetaItem> GetRewardItemRewards(Int64 in_rewaditem_tid)
		{
			if (GameMeta.MetaRewardItem.GetRewardItems(in_rewaditem_tid, out var out_reward_meta_info))
			{
				return out_reward_meta_info;
			}
			return null;
		}
		public List<h_info.MetaItem> GetRewardItemRewards(string in_hash_tag)
		{
			if (GameMeta.MetaRewardItem.GetRewardItems(in_hash_tag, out var out_reward_meta_info))
			{
				return out_reward_meta_info.RewardInfos;
			}
			return null;
		}

		// MetaTable 정보 찾기: for Tid
		public h_reward_item_meta.data GetRewardItemMetaInfo(string in_hash_tag)
		{
			if (GameMeta.MetaRewardItem.GetRewardItems(in_hash_tag, out var out_reward_meta_info))
			{
				return out_reward_meta_info;
			}
			return null;
		}
		public h_reward_item_meta.data GetRewardItemMetaInfo(Int64 in_rewaditem_tid)
		{
			return gplat.MetaData.byMetaId<h_reward_item_meta.data>(in_rewaditem_tid);
		}

		// 보상 저장 정보 확인
		public bool GetRewardItemInfo(Int64 in_tid, out h_info.RewardItemInfo out_user_rewarditem)
		{
			out_user_rewarditem = null;
			foreach (var each_item in m_rewarditem_infos)
			{
				if (each_item.RewardItemTid == in_tid)
				{
					out_user_rewarditem = each_item;
					return true;
				}
			}
			return false;
		}

		public bool GetRewardItemInfo(string in_hash_tag, out h_info.RewardItemInfo out_user_rewarditem)
		{
			out_user_rewarditem = null;
			if (GameMeta.MetaRewardItem.GetRewardItems(in_hash_tag, out var out_reward_meta_info))
			{
				return GetRewardItemInfo(out_reward_meta_info.Tid, out out_user_rewarditem);
			}
			return false;
		}


		// 받은 보상을 저장후 Logic에 반영
		public void CommitRewardItem(Int64 in_tid)
		{
			var commit_reward = new h_info.RewardItemInfo()
			{
				Dbid = 0,
				RewardItemTid = in_tid,
				RewardState = 1,
				InsertAt = gplat.TimeUtil.it.UtcNowString(),
				CompleteAt = gplat.TimeUtil.it.UtcNowString()
			};
			CommitRewardItem(commit_reward);
		}
		public void CommitRewardItem(string in_hash_tag)
		{
			if (GameMeta.MetaRewardItem.GetRewardItems(in_hash_tag, out var out_reward_meta_info))
			{
				CommitRewardItem(out_reward_meta_info.Tid);
			}
		}
		public void CommitRewardItem(h_info.RewardItemInfo in_rewarditem)
		{
			foreach (var each_item in m_rewarditem_infos)
			{
				if (each_item.RewardItemTid == in_rewarditem.RewardItemTid)
				{
					each_item.Dbid = in_rewarditem.Dbid;
					each_item.RewardItemTid = in_rewarditem.RewardItemTid;
					each_item.RewardState = in_rewarditem.RewardState;
					each_item.InsertAt = in_rewarditem.InsertAt;
					each_item.CompleteAt = in_rewarditem.CompleteAt;
					return;
				}
			}
			m_rewarditem_infos.Add(in_rewarditem);
		}
	}

}
