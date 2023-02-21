using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using gplat;

namespace gplat
{
	//db타입별 프로바이더 브리지(bridge)와 정보 공유 
	public class DbProviderBridge
	{
		public static DbProviderBridge it = new DbProviderBridge();

		//이부분도 좀 더 유연하게 동작하도록 변경 필요.
		Dictionary<string, DbInfo> m_dbinfos = new Dictionary<string, DbInfo>();

		//DbInfo 설정정보는 define로 올리도록 
		public Result addProvider(DbInfo in_db_info, Int32 in_pool_count)
		{
			var gplat_result = new Result();
			bool added = false;
			if (in_db_info.m_db_type.Equals("mysql")) // mysql provider추가 
			{
				added = DbMySqlProviderManager.it.addProvider(in_db_info.m_alias, in_db_info.m_db_name, in_db_info.m_server_ip, in_db_info.m_server_port, in_db_info.authString(), in_pool_count);
			}
			else
			{
				added = DbMsSqlProviderManager.it.addProvider(in_db_info.m_alias, in_db_info.m_db_name, in_db_info.serverAddress(), in_db_info.authString(), in_pool_count);
			}

			m_dbinfos.Add(in_db_info.m_alias, in_db_info);//


			//추가한 디비의 타입과 정보를 넣어주도록 하자. 
			if (!added)
			{
				return gplat_result.setFail($"{in_db_info.m_alias}, {in_db_info.m_db_name} {in_db_info.serverAddress()} not added");
			}
			return gplat_result.setOk();
		}

		public string getDbType(meta_define.db_provider_type_e in_provider_type)
		{
			return getDbType(in_provider_type.ToString());
		}

		public string getDbType(string in_dbalias)
		{
			if (m_dbinfos.TryGetValue(in_dbalias, out var out_dbinfo))
			{
				return out_dbinfo.m_db_type;
			}
			return "none";
		}

	}
}
