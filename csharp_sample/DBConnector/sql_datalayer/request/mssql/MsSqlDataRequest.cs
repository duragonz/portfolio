using System.Collections.Generic;
using System.Data;

namespace gplat.datalayer
{
	public class DbDataRequest
	{
		protected CommandType m_command_type = CommandType.Text;
		private int m_timeout = -1;

		public string Command { get; set; }
		public CommandType CommandType
		{
			get { return m_command_type; }
			set { m_command_type = value; }
		}


		public int Timeout
		{
			get { return m_timeout; }
			set { m_timeout = value; }
		}

		public bool Prepare { get; set; }
	}

	public class MsSqlDataRequest : DbDataRequest
	{
		private List<MsSqlDataParameter> m_data_params = new List<MsSqlDataParameter>();

		//use sql parameter
		public MsSqlDataParameter m_return_param = new MsSqlDataParameter("return_value", 0, ParameterDirection.ReturnValue);

		public MsSqlDataRequest(string command)
		{
			Command = command;
		}

		public MsSqlDataRequest(string in_command, CommandType in_cmd_type)
			: this(in_command)
		{
			m_command_type = in_cmd_type;
		}

		public MsSqlDataRequest(string in_command, CommandType in_cmd_type, bool in_prepare)
			: this(in_command, in_cmd_type)
		{
			Prepare = in_prepare;
		}


		public List<MsSqlDataParameter> Params
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

		public MsSqlDataParameter GetOutParam(string in_param_name)
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
