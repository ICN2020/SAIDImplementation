using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Common
{
	public class ContentName
	{
		private readonly string[] NameParts;

		public string this[int idx] { get { return idx >= NameParts.Length ? null : NameParts[idx]; } }
		public int ComponentCount { get { return NameParts.Length; } }
		public int Length { private set; get; }

		private string nameString = null;
		public string NameString
		{
			get
			{
				if (nameString == null)
				{
					if (NameParts.Length == 0) nameString = "/";
					else
					{
						StringBuilder builder = new StringBuilder();
						foreach (string part in NameParts)
						{
							builder.Append('/');
							builder.Append(part);
						}
						nameString = builder.ToString();
					}
				}
				return nameString;
			}
		}

		private ContentName parent = null;
		public ContentName Parent
		{
			get
			{
				if (parent == null)
				{
					if (NameParts.Length == 0) parent = this;
					else
						parent = new ContentName(this, ComponentCount - 1);
				}
				return parent;
			}
		}

		public ContentName(string rawName) : this(GetNameParts(rawName)) { }

		public ContentName(ContentName name, int partCount) : this(SubArray(name.NameParts, partCount)) { }

		public ContentName(ICollection<string> nameParts) : this(ListToArray(nameParts)) { }

		private ContentName(string[] nameParts)
		{
			Trace.Assert(nameParts != null);
			NameParts = nameParts;
			Length = 0;
			foreach (string part in NameParts)
				Length += part.Length + 1;
			Length = Length == 0 ? 1 : Length;
		}

		public ContentName GetChild(params string[] suffixParts)
		{
			string[] nameParts = new string[NameParts.Length + suffixParts.Length];
			Array.Copy(NameParts, nameParts, NameParts.Length);
			Array.Copy(suffixParts, 0, nameParts, NameParts.Length, suffixParts.Length);
			return new ContentName(nameParts);
		}

		public ContentName GetChild(ContentName suffix)
		{
			return GetChild(suffix.NameParts);
		}

		public string[] ToArray()
		{
			string[] ret = new string[NameParts.Length];
			Array.Copy(NameParts, ret, NameParts.Length);
			return ret;
		}

		public string[] GetSuffix(ContentName prefix)
		{
			if (!prefix.IsPrefixOf(this)) return null;

			string[] ret = new string[NameParts.Length - prefix.NameParts.Length];
			for (int i = prefix.ComponentCount, j = 0; i < ComponentCount; i++, j++)
				ret[j] = NameParts[i];
			return ret;
		}

		public bool IsPrefixOf(ContentName another)
		{
			if (NameParts.Length > another.NameParts.Length) return false;
			for (int i = 0; i < NameParts.Length; i++)
				if (!NameParts[i].Equals(another.NameParts[i]))
					return false;
			return true;
		}

		public override string ToString() { return NameString; }

		public override bool Equals(object obj)
		{
			ContentName another = obj as ContentName;
			if (another == null || NameParts.Length != another.NameParts.Length) return false;
			for (int i = 0; i < NameParts.Length; i++)
				if (!NameParts[i].Equals(another.NameParts[i]))
					return false;
			return true;
		}

		public override int GetHashCode()
		{
			int ret = 53;
			foreach (string part in NameParts)
			{
				ret = ret * 47 + part.GetHashCode();
			}
			return ret;
		}

		//public virtual int CompareTo(ContentName other)
		//{
		//    int lengthDiff = NameParts.Length - other.NameParts.Length;
		//    int length = lengthDiff < 0 ? NameParts.Length : other.NameParts.Length;

		//    for (int i = 0; i < length; i++)
		//    {
		//        int tmp = NameParts[i].CompareTo(other.NameParts[i]);
		//        if (tmp != 0) return tmp;
		//    }
		//    return lengthDiff;
		//}

		private static T[] SubArray<T>(T[] orig, int count)
		{
			if (orig.Length < count) count = orig.Length;
			T[] ret = new T[count];
			Array.Copy(orig, 0, ret, 0, count);
			return ret;
		}

		private static string[] GetNameParts(string name)
		{
			List<string> nameParts = new List<string>();
			char[] tmpPart = new char[name.Length + 1];
			int pointer = name[0] == '/' ? 1 : 0;
			int k = 0;
			for (; pointer < name.Length; pointer++)
			{
				char c = name[pointer];
				if (name[pointer] == '/')
				{
					nameParts.Add(new string(tmpPart, 0, k));
					k = 0;
				}
				else
				{
					tmpPart[k++] = c;
				}
			}
			if (k != 0)
				nameParts.Add(new string(tmpPart, 0, k));

			return nameParts.ToArray();
		}

		private static T[] ListToArray<T>(ICollection<T> list)
		{
			T[] ret = new T[list.Count];
			list.CopyTo(ret, 0);
			return ret;
		}


		public static implicit operator ContentName(String str)
		{
			return new ContentName(str);
		}

		//private static implicit operator ContentName(String[] strs)
		//{
		//    return new ContentName(strs);
		//}

		public static implicit operator string(ContentName name)
		{
			return name.ToString();
		}

		public void WriteTo(Stream stream)
		{
			byte[] buf1, buf2;
			buf1 = BitConverter.GetBytes(NameParts.Length);
			stream.Write(buf1, 0, buf1.Length);
			foreach (string str in NameParts)
			{
				buf2 = Encoding.UTF8.GetBytes(str);
				buf1 = BitConverter.GetBytes(buf2.Length);
				stream.Write(buf1, 0, buf1.Length);
				stream.Write(buf2, 0, buf2.Length);
			}
		}

		public static ContentName ReadFrom(Stream stream)
		{
			byte[] buf1, buf2;
			buf1 = new byte[sizeof(int)];
			stream.Read(buf1, 0, buf1.Length);
			string[] nameParts = new string[BitConverter.ToInt32(buf1, 0)];
			for (int i = 0; i < nameParts.Length; i++)
			{
				stream.Read(buf1, 0, buf1.Length);
				buf2 = new byte[BitConverter.ToInt32(buf1, 0)];
				stream.Read(buf2, 0, buf2.Length);
				nameParts[i] = new String(Encoding.UTF8.GetChars(buf2));
			}
			return new ContentName(nameParts);
		}
	}
}

