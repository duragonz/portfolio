using System;

namespace gplat.datalayer
{
	[AttributeUsage(AttributeTargets.Property)]
	public class MapColumnAttribute : Attribute
	{
		public MapColumnAttribute(string name)
		{
			Name = name;
		}
		public string Name { get; private set; }

	}
}
