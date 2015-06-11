using System;
using System.IO;

namespace Common
{
	public interface ISerializable
	{
		void WriteTo(Stream stream);
	}
}

