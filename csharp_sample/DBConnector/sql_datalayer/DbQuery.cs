using gplat.datalayer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using log4net;
using gplat;
using System.Data.Common;

namespace gplat
{
	public class DbQuery
	{
		protected ILog m_log = Log.logger("db");
		protected Result m_gplat_result = new Result();
		protected Profiler m_profiler;

		string m_dbtype;
		string m_dbalias;
		//for mssql
		DbMsSqlProvider m_mssql_provider = null;
		MsSqlDataRequest m_mssql_request = null;

		//for mysql
		DbMySqlProvider m_mysql_provider = null;
		MySqlDataRequest m_mysql_request = null;

		protected SqlException m_exception = null;

		//transaction info 날리기 ? 
		protected TransactionInfo m_transaction_info = null;

		public SqlException sqlException => m_exception;

		public DbQuery(string in_db_alias, string in_cmd)
		{
			m_dbalias = in_db_alias;
			//디비 알리아스의 디비타입을 알아낸다. 
			m_dbtype = DbProviderBridge.it.getDbType(in_db_alias);
			m_profiler = new Profiler(in_cmd, ServerConfig.it.m_db_profiler_log_miliseconds/1000);

			if (m_dbtype.Equals("mysql"))
			{
				m_mysql_request = new MySqlDataRequest(in_cmd, CommandType.StoredProcedure);
			}
			else
			{
				m_mssql_request = new MsSqlDataRequest(in_cmd, CommandType.StoredProcedure);
			}
		}

		public bool haveProvider()
		{
			if (m_dbtype.Equals("mysql"))
			{
				return (m_mysql_provider != null);
			}
			else
			{
				return (m_mssql_provider != null);
			}
		}


		//트랜잭션은 트랜잭션 설정시 프로바이더를 설정 
		public void setTransactionInfo(TransactionInfo in_transaction_info)
		{
			m_transaction_info = in_transaction_info;
			m_mssql_provider = m_transaction_info.MsSqlProvider;
			m_mysql_provider = m_transaction_info.MySqlProvider;
		}
		//트랜잭션 실행 후 환경 정리 
		public ISafeDataReader ExecuteTransactionDataReader()
		{
			ISafeDataReader data_reader = null;
			if (m_dbtype.Equals("mysql"))
			{
				data_reader = m_mysql_provider.ExecuteTransactionDataReader(m_mysql_request, m_transaction_info.mysqlTransaction);
			}
			else
			{
				data_reader = m_mssql_provider.ExecuteTransactionDataReader(m_mssql_request, m_transaction_info.mssqlTransaction);
			}
			//링크 정리 
			m_transaction_info = null;
			m_mssql_provider = null;
			m_mysql_provider = null;
			return data_reader;
		}

		//단일 처리시 사용 
		public Result SetProvider()
		{
			if (m_dbtype.Equals("mysql"))
			{
				m_mysql_provider = DbMySqlProviderManager.it.getProvider(m_dbalias);
				if (m_mysql_provider == null)
				{
					return m_gplat_result.setFail(result.code_e.GPLAT_DB_PROVIDER_NULL, $"{m_dbalias} provider is null").DeepClone();
				}
			}
			else
			{
				m_mssql_provider = DbMsSqlProviderManager.it.getProvider(m_dbalias);
				if (m_mssql_provider == null)
				{
					return m_gplat_result.setFail(result.code_e.GPLAT_DB_PROVIDER_NULL, $"{m_dbalias} provider is null").DeepClone();
				}
			}
			return m_gplat_result.setOk();
		}
		public ISafeDataReader ExecuteDataReader()
		{
			if (m_dbtype.Equals("mysql"))
			{
				return m_mysql_provider.ExecuteDataReader(m_mysql_request);
			}
			else
			{
				return m_mssql_provider.ExecuteDataReader(m_mssql_request);
			}
		}
		//단일 처리 후 끝나는 경우에만 사용
		public void ReturnProvider()
		{
			if (m_dbtype.Equals("mysql"))
			{
				DbMySqlProviderManager.it.returnProvider(m_mysql_provider);
				m_mysql_provider = null;
			}
			else
			{
				DbMsSqlProviderManager.it.returnProvider(m_mssql_provider);
				m_mssql_provider = null;
			}

		}

		// stored procedure parameters : 현재 사용빈도가 있는 것의 인터페이스만 추가 
		public void AddParameter(string in_param_name, object in_param_value)
		{
			if (m_dbtype.Equals("mysql"))
			{
				m_mysql_request.Params.Add(new MySqlDataParameter(in_param_name, in_param_value));
			}
			else
			{
				m_mssql_request.Params.Add(new MsSqlDataParameter(in_param_name, in_param_value));
			}
		}
		public void AddParameter(string in_param_name, object in_param_value, ParameterDirection in_param_direction)
		{
			if (m_dbtype.Equals("mysql"))
			{
				m_mysql_request.Params.Add(new MySqlDataParameter(in_param_name, in_param_value, in_param_direction));
			}
			else
			{
				m_mssql_request.Params.Add(new MsSqlDataParameter(in_param_name, in_param_value, in_param_direction));
			}
		}
		//실행후에 TransactionInfo 링크 끊기 처리할것 


		public Int32 SqlReturnValue()
		{
			if (m_dbtype.Equals("mysql"))
			{
				return (Int32)m_mysql_request.m_return_param.ParamValue;
			}
			else
			{
				return (Int32)m_mssql_request.m_return_param.ParamValue;
			}
		}

		public Result OutParamValue(string in_param_name, out object out_param_value)
		{
			var result = new Result();
			out_param_value = null;
			if (m_dbtype.Equals("mysql"))
			{
				var param = m_mysql_request.GetOutParam(in_param_name);
				if (null == param)
				{
					return result.setFail(@"no out param by {in_param_name}");
				}
				out_param_value = param.ParamValue;
			}
			else
			{
				var param = m_mssql_request.GetOutParam(in_param_name);
				if (null == param)
				{
					return result.setFail($"no out param by {in_param_name}");
				}
				out_param_value = param.ParamValue;
			}
			return result.setOk();
		}
	}
}
