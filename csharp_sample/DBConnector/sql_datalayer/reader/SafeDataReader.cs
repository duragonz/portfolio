using System;
using System.Data;

namespace gplat.datalayer
{
	public class SafeDataReader : ISafeDataReader
	{
		private readonly IDataReader m_data_reader;
		protected string m_command;
		public SafeDataReader(IDataReader in_data_reader, string in_cmd = "")
		{
			m_data_reader = in_data_reader;
			m_command = in_cmd;
		}

		public string Command()
		{
			return m_command;
		}

		public IDataReader DataReader
		{
			get { return m_data_reader; }
		}

		public virtual string GetString(int in_ordinal)
		{
			return m_data_reader.IsDBNull(in_ordinal) ? "" : m_data_reader.GetString(in_ordinal);
		}

		public virtual string GetString(string in_name)
		{
			var index = GetOrdinal(in_name);
			return GetString(index);
		}

		public virtual object GetValue(int in_ordinal)
		{
			if (m_data_reader.IsDBNull(in_ordinal))
			{
				return null;
			}
			return m_data_reader.GetValue(in_ordinal);
		}

		public virtual object GetValue(string in_name)
		{
			var index = GetOrdinal(in_name);
			return GetValue(index);
		}

		public virtual int GetInt32(int in_ordinal)
		{
			if (m_data_reader.IsDBNull(in_ordinal))
			{
				return 0;
			}
			return m_data_reader.GetInt32(in_ordinal);
		}

		public virtual int GetInt32(string in_name)
		{
			var index = GetOrdinal(in_name);
			return GetInt32(index);
		}

		public virtual double GetDouble(int in_ordinal)
		{
			if (m_data_reader.IsDBNull(in_ordinal))
			{
				return 0;
			}
			return m_data_reader.GetDouble(in_ordinal);
		}

		public virtual double GetDouble(string in_name)
		{
			var index = GetOrdinal(in_name);
			return GetDouble(index);
		}

		public virtual Guid GetGuid(int in_ordinal)
		{
			if (m_data_reader.IsDBNull(in_ordinal))
			{
				return Guid.Empty;
			}
			return m_data_reader.GetGuid(in_ordinal);
		}

		public virtual Guid GetGuid(string in_name)
		{
			var index = GetOrdinal(in_name);
			return GetGuid(index);
		}

		public bool HasColumn(string in_name)
		{
			for (var x = 0; x <= m_data_reader.FieldCount - 1; x++)
			{
				if (m_data_reader.GetName(x).Equals(in_name, StringComparison.InvariantCultureIgnoreCase))
					return true;
			}
			return false;
		}

		public virtual bool Read()
		{
			return m_data_reader.Read();
		}

		public virtual bool NextResult()
		{
			return m_data_reader.NextResult();
		}

		public virtual void Close()
		{
			m_data_reader.Close();
		}

		public virtual int Depth
		{
			get { return m_data_reader.Depth; }
		}

		public virtual int FieldCount
		{
			get { return m_data_reader.FieldCount; }
		}

		public virtual bool GetBoolean(int in_ordinal)
		{
			if (m_data_reader.IsDBNull(in_ordinal))
			{
				return false;
			}
			return m_data_reader.GetBoolean(in_ordinal);
		}

		public virtual bool GetBoolean(string in_name)
		{
			var index = GetOrdinal(in_name);
			return GetBoolean(index);
		}


		public virtual byte GetByte(int i)
		{
			if (m_data_reader.IsDBNull(i))
			{
				return 0;
			}
			return m_data_reader.GetByte(i);
		}

		public virtual byte GetByte(string name)
		{
			var index = GetOrdinal(name);
			return GetByte(index);
		}

		public virtual long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferOffset, int length)
		{
			if (m_data_reader.IsDBNull(i))
			{
				return 0;
			}
			return m_data_reader.GetBytes(i, fieldOffset, buffer, bufferOffset, length);
		}

		public virtual long GetBytes(string name, long fieldOffset, byte[] buffer, int bufferOffset, int length)
		{
			var index = GetOrdinal(name);
			return GetBytes(index, fieldOffset, buffer, bufferOffset, length);
		}

		public virtual char GetChar(int i)
		{
			if (m_data_reader.IsDBNull(i))
			{
				return char.MinValue;
			}
			var myChar = new char[1];
			m_data_reader.GetChars(i, 0, myChar, 0, 1);
			return myChar[0];
		}

		public virtual char GetChar(string in_name)
		{
			var index = GetOrdinal(in_name);
			return GetChar(index);
		}

		public virtual long GetChars(int i, long fieldOffset, char[] buffer, int bufferOffset, int length)
		{
			if (m_data_reader.IsDBNull(i))
			{
				return 0;
			}
			return m_data_reader.GetChars(i, fieldOffset, buffer, bufferOffset, length);
		}

		public virtual long GetChars(string in_name, long in_field_offset, char[] in_buffer, int bufferOffset, int length)
		{
			var index = GetOrdinal(in_name);
			return GetChars(index, in_field_offset, in_buffer, bufferOffset, length);
		}

		public virtual IDataReader GetData(int in_ordinal)
		{
			return m_data_reader.GetData(in_ordinal);
		}

		public virtual IDataReader GetData(string in_name)
		{
			var index = GetOrdinal(in_name);
			return GetData(index);
		}

		public virtual string GetDataTypeName(int in_ordinal)
		{
			return m_data_reader.GetDataTypeName(in_ordinal);
		}

		public virtual string GetDataTypeName(string in_name)
		{
			var index = GetOrdinal(in_name);
			return GetDataTypeName(index);
		}

		public virtual DateTime GetDateTime(int i)
		{
			if (m_data_reader.IsDBNull(i))
			{
				return DateTime.MinValue;
			}
			return m_data_reader.GetDateTime(i);
		}
		public virtual DateTime GetDateTime(string name)
		{
			var index = GetOrdinal(name);
			return GetDateTime(index);
		}

		public virtual decimal GetDecimal(int i)
		{
			if (m_data_reader.IsDBNull(i))
			{
				return 0;
			}
			return m_data_reader.GetDecimal(i);
		}

		public virtual decimal GetDecimal(string name)
		{
			var index = GetOrdinal(name);
			return GetDecimal(index);
		}

		public virtual Type GetFieldType(int i)
		{
			return m_data_reader.GetFieldType(i);
		}

		public virtual Type GetFieldType(string name)
		{
			var index = GetOrdinal(name);
			return GetFieldType(index);
		}

		public virtual float GetFloat(int i)
		{
			if (m_data_reader.IsDBNull(i))
			{
				return 0;
			}
			return m_data_reader.GetFloat(i);
		}

		public virtual float GetFloat(string name)
		{
			var index = GetOrdinal(name);
			return GetFloat(index);
		}

		public virtual short GetInt16(int i)
		{
			if (m_data_reader.IsDBNull(i))
			{
				return 0;
			}
			return m_data_reader.GetInt16(i);
		}

		public virtual short GetInt16(string name)
		{
			var index = GetOrdinal(name);
			return GetInt16(index);
		}

		public virtual long GetInt64(int i)
		{
			if (m_data_reader.IsDBNull(i))
			{
				return 0;
			}
			return m_data_reader.GetInt64(i);
		}

		public virtual long GetInt64(string name)
		{
			var index = GetOrdinal(name);
			return GetInt64(index);
		}

		public virtual string GetName(int i)
		{
			return m_data_reader.GetName(i);
		}

		public virtual int GetOrdinal(string name)
		{
			return m_data_reader.GetOrdinal(name);
		}

		public virtual DataTable GetSchemaTable()
		{
			return m_data_reader.GetSchemaTable();
		}

		public virtual int GetValues(object[] values)
		{
			return m_data_reader.GetValues(values);
		}

		public bool IsClosed
		{
			get { return m_data_reader.IsClosed; }
		}

		public virtual bool IsDBNull(int i)
		{
			return m_data_reader.IsDBNull(i);
		}

		public virtual bool IsDBNull(string name)
		{
			var index = GetOrdinal(name);
			return IsDBNull(index);
		}
		public virtual object this[string name]
		{
			get
			{
				var value = m_data_reader[name];
				if (DBNull.Value.Equals(value))
				{
					return null;
				}
				return value;
			}
		}
		public virtual object this[int i]
		{
			get
			{
				if (m_data_reader.IsDBNull(i))
				{
					return null;
				}
				return m_data_reader[i];
			}
		}

		public int RecordsAffected
		{
			get { return m_data_reader.RecordsAffected; }
		}


		// To detect redundant calls
		private bool m_disposed;

		protected virtual void Dispose(bool in_disposing)
		{
			if (!m_disposed)
			{
				if (in_disposing)
				{
					// free unmanaged resources when explicitly called
					m_data_reader.Dispose();
				}
				// free shared unmanaged resources
			}
			m_disposed = true;
		}

		public void Dispose()
		{
			// Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
			Dispose(true);
			GC.SuppressFinalize(this);
		}

	}
}
