#if MYSQL
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Data.Common;

using MySql.Data.MySqlClient;


namespace gplat.datalayer
{
	public abstract class MySqlBaseDataProvider //: IDisposable
	{
		private MySqlConnection m_sql_conn;
		private string m_conn_str;


		protected MySqlBaseDataProvider()
		{
			//SetConnectionStringByConnectionName();
		}

		protected MySqlBaseDataProvider(string in_conn_str)
		{
			SetConnectionStringByConnectionName(in_conn_str);
		}


		private void SetConnectionStringByConnectionName(string in_conn_name = "DefaultConnection")
		{
			m_conn_str = ConfigurationManager.ConnectionStrings[in_conn_name].ConnectionString;
		}

		public void SetConnectionString(string connection_string)
		{
			m_conn_str = connection_string;
		}

		public MySqlConnection sqlConnection()
		{
			if (m_sql_conn == null)
			{
				CreateConnection();
			}
			else if (m_sql_conn.State == System.Data.ConnectionState.Closed)
			{
				m_sql_conn.Open();
				//CreateConnection();
			}
			else if (m_sql_conn.State == System.Data.ConnectionState.Broken)
			{
				m_sql_conn.Close();
				m_sql_conn.Open();
			}
			return m_sql_conn;
		}

		protected virtual MySqlConnection CreateConnection()
		{
			Ensure.Argument.NotNullOrEmpty(m_conn_str);

			m_sql_conn = new MySqlConnection(m_conn_str);
			//m_sql_conn.Open();
			return m_sql_conn;
		}

		//mysql에서도 풀링옵션을 지원하는지 확인 필요
		public void CloseConnection()
		{
			if (null != m_sql_conn)
			{
				m_sql_conn.Close(); //풀링 옵션을 준 경우 연결을 풀에 돌려준다.  
									//m_sql_conn = null;
			}
		}
	}
}
#endif
