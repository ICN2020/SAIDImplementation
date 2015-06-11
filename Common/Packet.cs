using System;
using System.IO;
using System.Diagnostics;

namespace Common
{
	public abstract class Packet : ISerializable
	{
		public const byte PACKET_INTEREST = 1;
		public const byte PACKET_DATA = 2;
		public const byte MARK_BIT = 128;

		public byte Type{ private set; get; }

		private bool _mark = false;

		public bool Mark { 
			get { return _mark; } 
			set { 
				if (value)
					_mark = true;
			} 
		}

//		private UInt16 _mpr = UInt16.MaxValue;

		public UInt16 MPR { set; get; }

		public Packet (byte type, bool mark = false, UInt16 mpr = UInt16.MaxValue)
		{
			Type = type;
			Mark = mark;
			MPR = mpr;
		}

		public Packet (byte type, Stream stream)
		{
			byte type2 = (byte)stream.ReadByte ();
			Mark = (type2 & MARK_BIT) == MARK_BIT;
			type2 = (byte)(type & (MARK_BIT - 1));
			Trace.Assert (type == type2);
			Type = type;

			byte[] buf = new byte[sizeof(UInt16)];
			stream.Read (buf, 0, buf.Length);
			MPR = BitConverter.ToUInt16 (buf, 0);

		}

		public virtual void WriteTo (Stream stream)
		{
			byte type = Type;
			if (Mark)
				type |= MARK_BIT;
			stream.WriteByte (type);
			byte[] mpr = BitConverter.GetBytes (MPR);
			stream.Write (mpr, 0, mpr.Length);
		}

		public override string ToString ()
		{
			return string.Format ("[Packet: Type={0}, Mark={1}, MPR={2}]", Type, Mark, MPR);
		}

		public static void GetPacketTypeAndMark (byte[] buf, out byte type, out bool marked)
		{
			type = buf [0];
			marked = (type & MARK_BIT) == MARK_BIT;
			type = (byte)(type & (MARK_BIT - 1));
		}
	}
}

