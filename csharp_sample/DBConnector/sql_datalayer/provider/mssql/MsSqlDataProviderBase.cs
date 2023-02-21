using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Data.Common;

namespace gplat.datalayer
{
	public abstract class MsSqlDataProviderBase
	{
		protected SqlConnection m_sql_conn = null;
		private string m_conn_string;

		protected MsSqlDataProviderBase()
		{
			//SetConnectionStringByConnectionName();
		}
		protected MsSqlDataProviderBase(string in_conn_name)
		{
			SetConnectionStringByConnectionName(in_conn_name);
		}

		private void SetConnectionStringByConnectionName(string connectionStringName = "DefaultConnection")
		{
			m_conn_string = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
		}

		public void SetConnectionString(string in_conn_string)
		{
			m_conn_string = in_conn_string;
		}

		protected virtual SqlConnection CreateConnection()
		{
			Ensure.Argument.NotNullOrEmpty(m_conn_string);

			m_sql_conn = new SqlConnection(m_conn_string);
			m_sql_conn.Open();
			return m_sql_conn;
		}

		public SqlConnection sqlConnection()
		{
			if (m_sql_conn == null)
			{
				CreateConnection();
			}
			else if (m_sql_conn.State == System.Data.ConnectionState.Closed)
			{
				m_sql_conn.Open();
			}
			else if (m_sql_conn.State == System.Data.ConnectionState.Broken)
			{
				m_sql_conn.Close();
				m_sql_conn.Open();
			}
			return m_sql_conn;
		}


		public void CloseConnection()
		{
			if (null != m_sql_conn)
			{
				m_sql_conn.Close(); //풀링 옵션을 준 경우 연결을 풀에 돌려준다.  
				m_sql_conn = null;
			}
		}
	}
}
