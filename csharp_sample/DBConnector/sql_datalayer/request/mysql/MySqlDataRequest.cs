#if MYSQL
using System.Collections.Generic;
using System.Data;

namespace gplat.datalayer
{
	public class MySqlDataRequest : DbDataRequest
	{
		private int m_timeout = -1;
		private List<MySqlDataParameter> m_data_params = new List<MySqlDataParameter>();

		//use sql parameter 
		public MySqlDataParameter m_return_param = new MySqlDataParameter("return_value", 0, ParameterDirection.ReturnValue);

		public MySqlDataRequest(string in_command)
		{
			Command = in_command;
		}

		public MySqlDataRequest(string in_cmd, CommandType in_cmd_type)
			: this(in_cmd)
		{
			m_command_type = in_cmd_type;
		}

		public MySqlDataRequest(string in_cmd, CommandType in_cmd_type, bool in_prepare)
			: this(in_cmd, in_cmd_type)
		{
			Prepare = in_prepare;
		}

		public List<MySqlDataParameter> Params
		{
			get { return m_data_params; }
			set { m_data_params = value; }
		}
		public string paramsToString()
		{
			string out_str = "";
			foreach (var data_param in m_data_params)
			{
				out_str += data_param.ToString();
			}
			return out_str;
		}
		public MySqlDataParameter GetOutParam(string in_param_name)
		{
			foreach (var param_object in Params)
			{
				if (param_object.ParamName == "@" + in_param_name)
				{
					return param_object;
				}
			}
			return null;
		}
	}
}

#endif
