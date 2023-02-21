using System.Data;

namespace gplat.datalayer
{
	public class MsSqlDataBulkRequest
	{
		private readonly string m_dest_tablename;
		private readonly DataTable m_src_datatable;
		private readonly int m_timeout = 500;

		public MsSqlDataBulkRequest(DataTable in_src_datatable, string in_dest_tablename)
		{
			m_src_datatable = in_src_datatable;
			m_dest_tablename = in_dest_tablename;
		}

		public MsSqlDataBulkRequest(DataTable in_src_datatable, string in_dest_tablename, int in_timeout)
			: this(in_src_datatable, in_dest_tablename)
		{
			m_timeout = in_timeout;
		}

		public MsSqlDataBulkRequest(DataTable in_src_datatable, string in_dest_tablename, int in_timeout, int in_batch_size)
			: this(in_src_datatable, in_dest_tablename, in_timeout)
		{
			BatchSize = in_batch_size;
		}

		public DataTable SourceTable
		{
			get { return m_src_datatable; }
		}

		public string DestinaitionTable
		{
			get { return m_dest_tablename; }
		}

		public int TimeOut
		{
			get { return m_timeout; }
		}

		public int BatchSize { get; private set; }
	}
}
