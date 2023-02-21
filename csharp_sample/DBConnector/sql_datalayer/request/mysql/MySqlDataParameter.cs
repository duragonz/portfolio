#if MYSQL
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

using MySql.Data.MySqlClient;


namespace gplat.datalayer
{
	[Serializable]
	public class MySqlDataParameter
	{
		public MySqlParameter m_sql_parameter = new MySqlParameter();

		public MySqlParameter DbParameter => m_sql_parameter;

		public string ParamName
		{
			// use sql parameter
			get { return m_sql_parameter.ParameterName; }
			set { m_sql_parameter.ParameterName = value; }
		}

		public object ParamValue
		{
			// use sql parameter
			get { return m_sql_parameter.Value; }
			set { m_sql_parameter.Value = value; }
		}

		public ParameterDirection ParamDirection
		{
			// use sql parameter
			get { return m_sql_parameter.Direction; }
			set { m_sql_parameter.Direction = value; }
		}

		public int Size
		{
			// use sql parameter
			get { return m_sql_parameter.Size; }
			set { m_sql_parameter.Size = value; }
		}
		public MySqlDbType DataType
		{
			// use sql parameter
			get { return m_sql_parameter.MySqlDbType; }
			set
			{
				gplat.Log.logger("mysql").Info($" value={value}");
				m_sql_parameter.MySqlDbType = value;
			}
		}

		public override string ToString()
		{
			return $"{DataType} {ParamName},";
		}

		// use sql parameter
		public MySqlDataParameter(object in_param_value, ParameterDirection in_param_direction)
		{
			m_sql_parameter.Value = in_param_value;
			m_sql_parameter.Direction = in_param_direction;
		}

		public MySqlDataParameter(string in_param_name, object in_param_value)
		{
			// use sql parameter
			m_sql_parameter.ParameterName = "@" + in_param_name;
			m_sql_parameter.Value = in_param_value;
			m_sql_parameter.Direction = ParameterDirection.Input;
		}

		public MySqlDataParameter(string in_param_name, object in_param_value, ParameterDirection in_param_direction)
			: this(in_param_name, in_param_value)
		{
			// use sql parameter
			m_sql_parameter.Direction = in_param_direction;
			//if (ParameterDirection.ReturnValue == m_sql_parameter.Direction)
			//{
			//    m_sql_parameter.SqlDbType = SqlDbType.Int;
			//}
		}

		public MySqlDataParameter(int in_size, MySqlDbType in_data_type, string in_param_name, object in_param_value)
			: this(in_param_name, in_param_value)
		{
			// use sql parameter
			m_sql_parameter.Size = in_size;
			m_sql_parameter.MySqlDbType = in_data_type;
		}

		public MySqlDataParameter(int in_size, MySqlDbType in_data_type, string in_param_name, object in_param_value, ParameterDirection in_param_direction)
			: this(in_param_name, in_param_value, in_param_direction)
		{
			// use sql parameter
			m_sql_parameter.Size = in_size;
			m_sql_parameter.MySqlDbType = in_data_type;
		}
	}
}
#endif
