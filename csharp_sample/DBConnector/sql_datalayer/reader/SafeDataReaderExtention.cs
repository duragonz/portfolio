using System;
using System.Collections.Generic;
using System.Linq;

namespace gplat.datalayer
{
	public static class SafeDataReaderExtention
	{
		public static T MapToObject<T>(this ISafeDataReader in_safe_data_reader, bool in_use_map_column_attribute = false)
		{
			if (in_safe_data_reader.Read())
			{
				return in_safe_data_reader.GetObject<T>(in_use_map_column_attribute);
			}
			else
			{
				return default(T);
			}
		}

		public static List<T> MapToList<T>(this ISafeDataReader in_safe_data_reader, bool in_use_map_column_attribute = false)
		{
			var result_list = new List<T>();
			if (in_safe_data_reader != null)
			{
				while (in_safe_data_reader.Read())
				{
					result_list.Add(in_safe_data_reader.GetObject<T>(in_use_map_column_attribute));
				}
			}
			return result_list;
		}

		private static T GetObject<T>(this ISafeDataReader in_safe_data_reader, bool in_use_map_column_attribute = false)
		{
			var instance = Activator.CreateInstance<T>();
			foreach (var prop in instance.GetType().GetProperties())
			{
				var member_name = string.Empty;
				if (in_use_map_column_attribute)
				{
					// if attribute of type MapColumnAttribute found use it, else use property name itself
					var attribute = prop.GetCustomAttributes(false).FirstOrDefault(x => x.GetType() == typeof(MapColumnAttribute));
					if (attribute != null)
					{
						var mapTo = attribute as MapColumnAttribute;
						if (mapTo != null)
						{
							member_name = mapTo.Name;
						}
					}
					else
					{
						member_name = prop.Name;
					}
				}
				else
				{
					member_name = prop.Name;
				}

				var type_code = Type.GetTypeCode(prop.PropertyType);
				if (in_safe_data_reader.HasColumn(member_name) && !Equals(in_safe_data_reader[member_name], DBNull.Value))
				{
					switch (type_code)
					{
						case TypeCode.String:
							prop.SetValue(instance, in_safe_data_reader.GetString(member_name), null);
							break;
						case TypeCode.Int16:
							prop.SetValue(instance, in_safe_data_reader.GetInt16(member_name), null);
							break;
						case TypeCode.Int32:
							prop.SetValue(instance, in_safe_data_reader.GetInt32(member_name), null);
							break;
						case TypeCode.Int64:
							prop.SetValue(instance, in_safe_data_reader.GetInt64(member_name), null);
							break;
						case TypeCode.Decimal:
							prop.SetValue(instance, in_safe_data_reader.GetDecimal(member_name), null);
							break;
						case TypeCode.DateTime:
							prop.SetValue(instance, in_safe_data_reader.GetDateTime(member_name), null);
							break;
						case TypeCode.Double:
							prop.SetValue(instance, in_safe_data_reader.GetDouble(member_name), null);
							break;
						case TypeCode.Single:
							prop.SetValue(instance, in_safe_data_reader.GetFloat(member_name), null);
							break;
						case TypeCode.Boolean:
							prop.SetValue(instance, in_safe_data_reader.GetBoolean(member_name), null);
							break;
						case TypeCode.Char:
							prop.SetValue(instance, in_safe_data_reader.GetChar(member_name), null);
							break;
						case TypeCode.Byte:
							prop.SetValue(instance, in_safe_data_reader.GetChar(member_name), null);
							break;
						case TypeCode.Object:
							if (prop.PropertyType == typeof(Guid))
								prop.SetValue(instance, in_safe_data_reader.GetGuid(member_name), null);
							break;
						default:
							throw new Exception(string.Format("not support type:{0}", type_code));
					}
				}
				else
				{
					switch (type_code)
					{
						case TypeCode.String:
							prop.SetValue(instance, string.Empty, null);
							break;
					}
				}
			}
			return instance;
		}

	}
}
