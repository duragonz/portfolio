using gplat;

namespace game
{
	// 코스튬 장착 아이템
	public class ItemCostume : ItemBase
	{
		protected story_char_meta.data m_meta_char_data;
		public string Prefab => m_meta_item_data.Image;

		public override gplat.Result Setup(h_info.InstantItem in_sc_instant_item)
		{
			var gen_result = base.Setup(in_sc_instant_item);
			if (gen_result.fail())
			{
				return gen_result;
			}

			return SetupMetaData();
		}

		protected gplat.Result SetupMetaData()
		{
			var gen_result = new gplat.Result();
			m_meta_char_data = MetaData.byMetaId<story_char_meta.data>(m_meta_item_data.CharTid);
			if (null == m_meta_char_data)
			{
				return gen_result.setFail(result.h_code_e.ITEM_INVALID_DATA, $"no meta data by char tid:{m_meta_item_data.CharTid}");
			}
			return gen_result.setOk();
		}


		public string CostumeColor()
		{
			if (string.IsNullOrEmpty(Prefab))
			{
				return "default";
			}

			string[] resource_infos = Prefab.Split('_');

			if (resource_infos.Length < 3)
			{
				return "default";
			}

			string color = resource_infos[2];

			return color;
		}

		public override string ToString()
		{
			return $"[{ItemName}({ItemTid})]";
		}
	}
}
