using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;

namespace gplat.datalayer
{
	public class MsSqlDataProvider : MsSqlDataProviderBase
	{
		private static SqlCommand CreateSqlCommand(MsSqlDataRequest in_request, SqlConnection in_sql_conn)
		{
			var command = new SqlCommand(in_request.Command, in_sql_conn) { CommandType = in_request.CommandType };
			foreach (var item in in_request.Params)
			{
				// use sql parameter
				command.Parameters.Add(item.m_sql_parameter);
			}

			// add return value, sql sql parameter 
			command.Parameters.Add(in_request.m_return_param.m_sql_parameter);

			if (in_request.Prepare)
			{
				command.Prepare();
			}

			return command;
		}

		private static SqlCommand CreateTransactionSqlCommand(MsSqlDataRequest in_request, SqlConnection in_sql_conn, DbTransaction in_sql_tran)
		{
			var command = new SqlCommand(in_request.Command, in_sql_conn, (SqlTransaction)in_sql_tran) { CommandType = in_request.CommandType };
			foreach (var item in in_request.Params)
			{
				// use sql parameter
				command.Parameters.Add(item.m_sql_parameter);
			}

			// add return value, sql sql parameter
			command.Parameters.Add(in_request.m_return_param.m_sql_parameter);

			if (in_request.Prepare)
			{
				command.Prepare();
			}

			return command;
		}

		public MsSqlDataProvider()
		{
		}

		public MsSqlDataProvider(string in_conn_string)
			: base(in_conn_string)
		{
		}

		public virtual ISafeDataReader ExecuteDataReader(MsSqlDataRequest in_request)
		{
			//return ExecuteDataReader(request, CommandBehavior.CloseConnection);
			return ExecuteDataReader(in_request, CommandBehavior.Default);
		}

		public virtual ISafeDataReader ExecuteDataReader(MsSqlDataRequest in_request, CommandBehavior in_behavior)
		{
			Ensure.Argument.NotNull(in_request, "DataRequest");

			SafeDataReader safe_datareader = null;

			var sql_conn = sqlConnection();
			if ((sql_conn != null))
			{
				var sql_cmd = CreateSqlCommand(in_request, sql_conn);
				var sql_datareader = sql_cmd.ExecuteReader(in_behavior);

				safe_datareader = new SafeDataReader(sql_datareader, sql_cmd.CommandText);
			}

			return safe_datareader;
		}

		public virtual ISafeDataReader ExecuteTransactionDataReader(MsSqlDataRequest in_request, SqlTransaction in_sql_tran)
		{
			return ExecuteTransactionDataReader(in_request, in_sql_tran, CommandBehavior.Default);
		}

		public virtual ISafeDataReader ExecuteTransactionDataReader(MsSqlDataRequest in_request, SqlTransaction in_sql_tran, CommandBehavior in_behavior)
		{
			Ensure.Argument.NotNull(in_request, "DataRequest");

			SafeDataReader safe_datareader = null;

			var sql_conn = sqlConnection();
			if ((sql_conn != null))
			{
				var sql_cmd = CreateTransactionSqlCommand(in_request, sql_conn, in_sql_tran);
				var sql_datareader = sql_cmd.ExecuteReader(in_behavior);
				safe_datareader = new SafeDataReader(sql_datareader, sql_cmd.CommandText);
			}

			return safe_datareader;
		}

		public virtual object ExecuteScalar(MsSqlDataRequest in_request)
		{
			Ensure.Argument.NotNull(in_request, "DataRequest");
			object result;

			var sqlCon = sqlConnection(); //reuse connection for pool
			Ensure.Argument.NotNull(sqlCon, "SQL Connection");
			using (var cm = CreateSqlCommand(in_request, sqlCon))
			{
				result = cm.ExecuteScalar();
			}

			return result;
		}

		public virtual int ExecuteNonQuery(MsSqlDataRequest in_request)
		{
			Ensure.Argument.NotNull(in_request, "DataRequest");
			int result;

			var sqlCon = sqlConnection(); //reuse connection for pool
			Ensure.Argument.NotNull(sqlCon, "SQL Connection");
			using (var cm = CreateSqlCommand(in_request, sqlCon))
			{
				result = cm.ExecuteNonQuery();
			}
			return result;
		}

		public virtual T ExecuteNonQueryForOutParameter<T>(MsSqlDataRequest in_request, string in_param_name)
		{
			Ensure.Argument.NotNull(in_request, "DataRequest");
			var result = default(T);

			var sqlCon = sqlConnection(); //resue connection
			Ensure.Argument.NotNull(sqlCon, "SQL Connection");
			using (var cm = CreateSqlCommand(in_request, sqlCon))
			{
				cm.ExecuteNonQuery();
				if (!String.IsNullOrWhiteSpace(in_param_name))
					result = (T)Convert.ChangeType(cm.Parameters["@" + in_param_name].Value, typeof(T), CultureInfo.InvariantCulture);
			}
			return result;
		}

		public virtual DataSet ExecuteDataSet(MsSqlDataRequest request)
		{
			Ensure.Argument.NotNull(request, "DataRequest");
			var dataset = new DataSet();

			var sqlCon = sqlConnection(); //reuse connection
			Ensure.Argument.NotNull(sqlCon, "SQL Connection");
			using (var cm = CreateSqlCommand(request, sqlCon))
			{
				var ds = new SqlDataAdapter(cm);
				ds.Fill(dataset);
			}
			return dataset;
		}

		public virtual DataTable ExecuteDataTable(MsSqlDataRequest request)
		{
			Ensure.Argument.NotNull(request, "DataRequest");
			var dataTable = new DataTable();

			// 연결설정에서 파라메터를 넣어주어서 해결하여야 한다. 
			//using (var sqlCon = CreateConnection())
			var sqlCon = sqlConnection();
			Ensure.Argument.NotNull(sqlCon, "SQL Connection");
			using (var cm = CreateSqlCommand(request, sqlCon))
			{
				var ds = new SqlDataAdapter(cm);
				ds.Fill(dataTable);
			}
			return dataTable;
		}

		public virtual void ExecuteBulkCopy(MsSqlDataBulkRequest request)
		{
			Ensure.Argument.NotNull(request, "AdhocBulkRequest");

			var sqlConn = sqlConnection(); //reuse connection
			var bulkCopy = new SqlBulkCopy(sqlConn);
			bulkCopy.DestinationTableName = request.DestinaitionTable;
			bulkCopy.BulkCopyTimeout = request.TimeOut;
			bulkCopy.BatchSize = request.BatchSize;
			bulkCopy.WriteToServer(request.SourceTable);
		}
	}
}
