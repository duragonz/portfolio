#if MYSQL
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;

using MySql.Data.MySqlClient;
using gplat;

namespace gplat.datalayer
{
	public class MySqlDataProvider : MySqlBaseDataProvider//, IMySqlDataProvider
	{

		private static MySqlCommand CreateSqlCommand(MySqlDataRequest in_request, MySqlConnection in_sql_conn)
		{
			var command = new MySqlCommand(in_request.Command, in_sql_conn) { CommandType = in_request.CommandType };

			foreach (var item in in_request.Params)
			{
				// use sql parameter
				command.Parameters.Add(item.m_sql_parameter);
			}

			// mysql doest not support return value default.
			// add return value, sql sql parameter
			//command.Parameters.Add(request.m_return_param.m_sql_parameter);

			if (in_request.Prepare)
			{
				command.Prepare();
			}

			return command;
		}

		private static MySqlCommand CreateTransactionSqlCommand(MySqlDataRequest in_request, MySqlConnection in_sql_conn, DbTransaction in_sql_tran)
		{
			var command = new MySqlCommand(in_request.Command, in_sql_conn, (MySqlTransaction)in_sql_tran) { CommandType = in_request.CommandType };
			foreach (var item in in_request.Params)
			{
				// use sql parameter
				command.Parameters.Add(item.m_sql_parameter);
			}

			// add return value, sql sql parameter
			// mysql has no return value 
			//command.Parameters.Add(in_request.m_return_param.m_sql_parameter);

			if (in_request.Prepare)
			{
				command.Prepare();
			}

			return command;
		}

		public MySqlDataProvider()
		{
		}

		public MySqlDataProvider(string in_conn_str)
			: base(in_conn_str)
		{
		}

		public virtual ISafeDataReader ExecuteDataReader(MySqlDataRequest in_request)
		{
			return ExecuteDataReader(in_request, CommandBehavior.CloseConnection);
		}

		public virtual ISafeDataReader ExecuteDataReader(MySqlDataRequest in_request, CommandBehavior in_behavior)
		{
			Ensure.Argument.NotNull(in_request, "MySqlDataRequest");

			SafeDataReader safe_datareader = null;
			try
			{
				var sql_conn = sqlConnection(); //var sql_conn = CreateConnection();
				if ((sql_conn != null))
				{
					var sql_cmd = CreateSqlCommand(in_request, sql_conn);
					var sql_datareader = sql_cmd.ExecuteReader(in_behavior);
					safe_datareader = new SafeDataReader(sql_datareader, sql_cmd.CommandText);
				}
			}
			catch (ArgumentException arg_ex) //for mysql
			{
				throw new GplatException(new Result().setFail(result.code_e.GPLAT_SQL_SP_EXECUTION_ERROR, $"SP PARAMETER NOT MATCH.\n {in_request.Command}({in_request.paramsToString()})\n {arg_ex.Message}"));
			}
			catch (MySqlException my_ex)
			{
				throw new GplatException(new Result().setFail(result.code_e.GPLAT_SQL_SP_EXECUTION_ERROR, $"MYSQL EXCEPTION.\n {in_request.Command}({in_request.paramsToString()})\n {my_ex.Message}"));
			}
			return safe_datareader;
		}

		public virtual ISafeDataReader ExecuteTransactionDataReader(MySqlDataRequest in_request, MySqlTransaction in_sql_tran)
		{
			return ExecuteTransactionDataReader(in_request, in_sql_tran, CommandBehavior.Default);
		}

		public virtual ISafeDataReader ExecuteTransactionDataReader(MySqlDataRequest in_request, MySqlTransaction in_sql_tran, CommandBehavior in_behavior)
		{
			Ensure.Argument.NotNull(in_request, "DataRequest");
			SafeDataReader safe_datareader = null;

			try
			{
				var sql_conn = sqlConnection();
				if ((sql_conn != null))
				{
					var sql_cmd = CreateTransactionSqlCommand(in_request, sql_conn, in_sql_tran);
					var sql_datareader = sql_cmd.ExecuteReader(in_behavior);
					safe_datareader = new SafeDataReader(sql_datareader, sql_cmd.CommandText);
				}
			}
			catch (ArgumentException arg_ex)
			{
				throw new GplatException(new Result().setFail(result.code_e.GPLAT_SQL_SP_EXECUTION_ERROR, $"{arg_ex.Message} params:{in_request.paramsToString()}"));
			}
			return safe_datareader;
		}

		public virtual object ExecuteScalar(MySqlDataRequest request)
		{
			Ensure.Argument.NotNull(request, "DataRequest");
			object result;

			using (var sql_conn = CreateConnection())
			{
				Ensure.Argument.NotNull(sql_conn, "SQL Connection");
				using (var cm = CreateSqlCommand(request, sql_conn))
				{
					result = cm.ExecuteScalar();
				}
			}

			return result;
		}

		public virtual int ExecuteNonQuery(MySqlDataRequest request)
		{
			Ensure.Argument.NotNull(request, "DataRequest");
			int result;
			//using (var sql_conn = CreateConnection())
			using (var sql_conn = sqlConnection())
			{
				Ensure.Argument.NotNull(sql_conn, "SQL Connection");
				using (var cm = CreateSqlCommand(request, sql_conn))
				{
					result = cm.ExecuteNonQuery();
				}
			}

			return result;
		}

		public virtual T ExecuteNonQueryForOutParameter<T>(MySqlDataRequest request, string parameterName)
		{
			Ensure.Argument.NotNull(request, "DataRequest");
			var result = default(T);

			//using (var sql_conn = CreateConnection())
			using (var sql_conn = sqlConnection())
			{
				Ensure.Argument.NotNull(sql_conn, "SQL Connection");
				using (var cm = CreateSqlCommand(request, sql_conn))
				{
					cm.ExecuteNonQuery();
					if (!String.IsNullOrWhiteSpace(parameterName))
					{
						result = (T)Convert.ChangeType(cm.Parameters["@" + parameterName].Value, typeof(T), CultureInfo.InvariantCulture);
					}
				}
			}
			return result;
		}

		public virtual DataSet ExecuteDataSet(MySqlDataRequest request)
		{
			Ensure.Argument.NotNull(request, "DataRequest");
			var dataset = new DataSet();

			//using (var sql_conn = CreateConnection())
			using (var sql_conn = sqlConnection())
			{
				Ensure.Argument.NotNull(sql_conn, "SQL Connection");
				using (var cm = CreateSqlCommand(request, sql_conn))
				{
					var ds = new MySqlDataAdapter(cm);
					ds.Fill(dataset);
				}
			}
			return dataset;
		}

		public virtual DataTable ExecuteDataTable(MySqlDataRequest request)
		{
			Ensure.Argument.NotNull(request, "DataRequest");
			var dataTable = new DataTable();

			//using (var sql_conn = CreateConnection())
			using (var sql_conn = sqlConnection())
			{
				Ensure.Argument.NotNull(sql_conn, "SQL Connection");
				using (var sql_cmd = CreateSqlCommand(request, sql_conn))
				{
					var ds = new MySqlDataAdapter(sql_cmd);
					ds.Fill(dataTable);
				}
			}
			return dataTable;
		}

		//public virtual void ExecuteBulkCopy(DataBulkRequest request)
		//{
		//    Ensure.Argument.NotNull(request, "AdhocBulkRequest");

		//    using (var bulkCopy = new SqlBulkCopy(CreateConnection()))
		//    {
		//        bulkCopy.DestinationTableName = request.DestinaitionTable;
		//        bulkCopy.BulkCopyTimeout = request.TimeOut;
		//        bulkCopy.BatchSize = request.BatchSize;
		//        bulkCopy.WriteToServer(request.SourceTable);
		//    }
		//}
	}
}

#endif
