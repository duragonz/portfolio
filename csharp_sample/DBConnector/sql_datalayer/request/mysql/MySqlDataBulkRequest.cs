using System.Data;

namespace gplat.datalayer
{
	public class MySqlDataBulkRequest
	{
		private readonly string m_dest_table_name;
		private readonly DataTable m_src_datatable;
		private readonly int m_timeout = 500;

		public MySqlDataBulkRequest(DataTable in_src_datatable, string destinationTable)
		{
			m_src_datatable = in_src_datatable;
			m_dest_table_name = destinationTable;
		}

		public MySqlDataBulkRequest(DataTable source, string destinationTable, int timeOut)
			: this(source, destinationTable)
		{
			m_timeout = timeOut;
		}

		public MySqlDataBulkRequest(DataTable source, string destinationTable, int timeOut, int batchSize)
			: this(source, destinationTable, timeOut)
		{
			BatchSize = batchSize;
		}

		public DataTable SourceTable
		{
			get { return m_src_datatable; }
		}

		public string DestinaitionTable
		{
			get { return m_dest_table_name; }
		}

		public int TimeOut
		{
			get { return m_timeout; }
		}

		public int BatchSize { get; private set; }
	}
}
