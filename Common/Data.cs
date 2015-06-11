using System;
using System.IO;

namespace Common
{
	public class Data : Packet
	{
		public ContentName Name{ private set; get; }

		public ISerializable Content { private set; get; }

		public Data (ContentName name, ISerializable content) : base(PACKET_DATA)
		{
			Name = name;
			Content = content;
		}

		public Data (Stream stream) : base(PACKET_DATA, stream)
		{
			Name = ContentName.ReadFrom (stream);
			Content = new RandomContent (stream);
		}

		public override void WriteTo (Stream stream)
		{
			base.WriteTo (stream);
			Name.WriteTo (stream);
			Content.WriteTo (stream);
		}

		public override string ToString ()
		{
			return string.Format ("[Data: [{2},{3}] Name={0}, DataLength={1}]", Name, Content, Mark ? "M" : "", MPR);
		}

		public static void TestData ()
		{
			Data d = new Data ("/test/asdf/123.pdf", new RandomContent(1500));
			Console.WriteLine (d);

			using (MemoryStream ms = new MemoryStream()) {
				d.WriteTo (ms);
				ms.Seek (0, SeekOrigin.Begin);
				Console.WriteLine (ms.Length);
				Data d2 = new Data (ms);
				Console.WriteLine (d2);
			}

		}
	}

	public class RandomContent : ISerializable
	{
		public int TotalSizeInContent{ private set; get; }

		public RandomContent (int totalSizeInContent)
		{
			TotalSizeInContent = totalSizeInContent;
		}

		public override string ToString ()
		{
			return string.Format ("[RandomContent: TotalSizeInContent={0}]", TotalSizeInContent);
		}

		public RandomContent (Stream stream)
		{
//			int currentPosition = (int)stream.Position;
			byte[] buf = new byte[sizeof(int)];
			stream.Read (buf, 0, buf.Length);
			int length = BitConverter.ToInt32 (buf, 0);
			buf = new byte[length];
			stream.Read (buf, 0, buf.Length);
			int newPosition = (int)stream.Position;
			TotalSizeInContent = newPosition;
		}

		public void WriteTo (Stream stream)
		{
			int length = TotalSizeInContent - (int)stream.Length - sizeof(int);
			byte[] buf = BitConverter.GetBytes (length);
			stream.Write (buf, 0, buf.Length);
			buf = new byte[length];
			stream.Write (buf, 0, buf.Length);
		}
	}
}

