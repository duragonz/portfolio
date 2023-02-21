using System;
using System.Collections.Generic;
using gplat.datalayer;
using Newtonsoft.Json.Linq;
using log4net;
using System.Collections.Concurrent;
using gplat;

namespace gplat
{
	public class DbMySqlProvider : MySqlDataProvider
	{
		MySqlDataProvider m_provider = new MySqlDataProvider();
		public string m_db_alias;
		public string m_conn_string;

		public void setConnectionString(string in_conn_string)
		{
			m_conn_string = in_conn_string;
			SetConnectionString(m_conn_string);
		}

		public void createConnection(string in_conn_string)
		{
			setConnectionString(in_conn_string);
			CreateConnection();
		}
	}

	// provider connection list
	public class DbMySqlProviderGroup
	{
		public string m_db_alias;
		public string m_conn_string;
		ConcurrentQueue<DbMySqlProvider> m_providers = new ConcurrentQueue<DbMySqlProvider>();

		//리턴 등을 사용하지 않고 내부에서 sql connection을 new 
		public void createProvider(Int32 in_provider_count, string in_conn_string)
		{
			m_conn_string = in_conn_string;
			for (int i = 0; i < in_provider_count; ++i)
			{
				var db_provider = new DbMySqlProvider();
				db_provider.m_db_alias = m_db_alias;
				db_provider.createConnection(in_conn_string);
				m_providers.Enqueue(db_provider);
			}

			gplat.Log.logger("db.provider").Info($"{m_db_alias} {m_providers.Count} created");
		}

		public Int32 count()
		{
			return m_providers.Count;
		}

		public DbMySqlProvider takeProvider()
		{
			DbMySqlProvider db_provider = null;
			bool result = m_providers.TryDequeue(out db_provider);
			if (false == result)
			{
				gplat.Log.logger("db.provider").FatalFormat("provider for {0}. no available. {1}", m_db_alias, m_providers.Count);
				return null; //자동 추가하지 않고 오류 리턴 
			}
			return db_provider;
		}

		public void returnProvider(DbMySqlProvider in_db_provider)
		{
			in_db_provider.CloseConnection(); //풀링 옵션을 주고 풀에 연결을 되돌려 주도록 한다. 
			m_providers.Enqueue(in_db_provider);
		}
	}

	public class DbMySqlProviderManager
	{
		ILog m_log = Log.logger("db.provider");

		Dictionary<string, DbMySqlProviderGroup> m_provider_groups = new Dictionary<string, DbMySqlProviderGroup>();

		private static DbMySqlProviderManager m_instance = new DbMySqlProviderManager();

		public static DbMySqlProviderManager it => m_instance;
		public static DbMySqlProviderManager instance()
		{
			return m_instance;
		}

		// 연결 스트링에 풀옵션 추가
		public bool addProvider(string in_db_alias, string in_real_dbname, string in_server_addr, UInt16 in_server_port, string in_auth_string, Int32 in_pool_count = 10)
		{
			Int32 connect_retry_count = 255;
			Int32 connect_retry_interval = 5;

			Int32 min_pool_size = in_pool_count;// in_pool_count = (db_worker_count * 3)  
			Int32 max_pool_size = in_pool_count * 2; //in_pool_count * 2 

			var provider_group = new DbMySqlProviderGroup();
			provider_group.m_db_alias = in_db_alias;

			//var conn_str = $"Server={in_server_addr};Port={in_server_port};{in_auth_string};DataBase={in_real_dbname};Pooling=true;MinimumPoolSize={min_pool_size};maximumpoolsize={max_pool_size};";
			var conn_str = $"Server={in_server_addr};Port={in_server_port};{in_auth_string};DataBase={in_real_dbname};Pooling='true';Min Pool Size={min_pool_size};Max Pool Size={max_pool_size};";
			//var conn_str = $"Server={in_server_addr};Port={in_server_port};{in_auth_string};DataBase={in_real_dbname};Pooling='true';";
			m_log.WarnFormat($"MySQL DBProvider: {conn_str}");
			provider_group.createProvider(in_pool_count, conn_str);

			return addProviderGroup(ref provider_group);
		}

		public bool addProviderGroup(ref DbMySqlProviderGroup in_provider_group)
		{
			if (m_provider_groups.ContainsKey(in_provider_group.m_db_alias))
			{
				m_log.WarnFormat("ALREADY EXIST, base_dbname:{0} conn_string:{1}", in_provider_group.m_db_alias, in_provider_group.m_conn_string);
				return false;
			}
			m_provider_groups.Add(in_provider_group.m_db_alias, in_provider_group);
			//m_log.Debug($"REGISTER db_alias:{in_provider_group.m_db_alias} conn_string:{in_provider_group.m_conn_string} count {in_provider_group.count()}");
			gplat.Log.logger("sys").Info($"DB Provider Group REGISTERED {in_provider_group.m_db_alias} {in_provider_group.count()}");
			return true;
		}

		public DbMySqlProvider getProvider(string in_db_alias)
		{
			if (m_provider_groups.ContainsKey(in_db_alias))
			{
				return m_provider_groups[in_db_alias].takeProvider();
			}
			return null;
		}

		public void returnProvider(DbMySqlProvider in_db_provider)
		{
			if (null == in_db_provider)
			{
				m_log.Warn("db_provider is null");
				return;
			}
			if (m_provider_groups.ContainsKey(in_db_provider.m_db_alias))
			{
				m_provider_groups[in_db_provider.m_db_alias].returnProvider(in_db_provider);
			}
			else
			{
				m_log.ErrorFormat("NOT FOUND PROVIDER:{0}", in_db_provider.m_db_alias);
			}
		}
	}
}
