using System;
using System.IO;

namespace Common
{
	public class Interest : Packet
	{
		public const byte InterestACK = 0;
//		public const byte InterestSubscription = -1;
		public const byte InterestACKer = 254;
		public const byte InterestRequest = 255;

		public byte InterestType{ private set; get; }

		public ContentName Name{ private set; get; }

		public Interest (ContentName name, byte interestType) : base(PACKET_INTEREST)
		{
			Name = name;
			InterestType = interestType;
		}

		public Interest (Stream stream) : base(PACKET_INTEREST, stream)
		{
			InterestType = (byte)stream.ReadByte ();
			Name = ContentName.ReadFrom (stream);
		}

		public override void WriteTo (Stream stream)
		{
			base.WriteTo (stream);
			stream.WriteByte (InterestType);
			Name.WriteTo (stream);
		}

		public override string ToString ()
		{
			return string.Format ("[Interest: [{2},{3}] InterestType={0}, Name={1}]", InterestType, Name, Mark ? "M" : "", MPR);
		}

		public static void TestInterest ()
		{
			Interest interest = new Interest ("/asdf/egha/asdf", 2);
			interest.Mark = true;
			interest.MPR = 300;
			interest.MPR = 400;

			Console.WriteLine (interest);

			using (MemoryStream ms = new MemoryStream()) {
				interest.WriteTo (ms);
				ms.Seek (0, SeekOrigin.Begin);
				Console.WriteLine (ms.Length);
				Interest i2 = new Interest (ms);
				Console.WriteLine (i2);
			}

		}
	}
}

