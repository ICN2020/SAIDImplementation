using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;

namespace Common
{
	public class Link
	{
		public const int BITS_IN_BYTE = 8;
		public const int KB = 1000;
		public const int MB = 1000 * KB;
		public const long MS = 1000;
		public const long SECOND = 1000 * MS;

		public IPEndPoint To{ private set; get; }

		public long DataSize{ private set; get; }

		public int BandwidthInBitsPerSecond{ private set; get; }

		public int LinkBandwidthInBitsPerSecond{ private set; get; }

		public long DelayInMS{ private set; get; }

		public bool Busy { private set; get; }

		private IQueue<Packet> Queue;
		private UdpClient udpClient;

		/// <summary>
		/// Initializes a new instance of the <see cref="Common.Link"/> class.
		/// The link would sleep delay + packetlength / bandwidth - packet length / linkBandwidth so that it can arrive the other end at the right time (hopefully)
		/// LinkBandwidthInBitsPerSeond is used to reduce the latency in bandwidth.
		/// </summary>
		/// <param name="from">The source address of the link</param>
		/// <param name="to">The destination of the link</param>
		/// <param name="queue">The queue that is used in th elink</param>
		/// <param name="bandwidthInBitsPerSecond">Bandwidth in bits per second.</param>
		/// <param name="linkBandwidthInBitsPerSecond">Real link bandwidth in bits per second.</param>
		/// <param name="delayInMS">Delay in Milliseconds</param>
		public Link (UdpClient localClient, IPEndPoint to, IQueue<Packet> queue, int bandwidthInBitsPerSecond, int linkBandwidthInBitsPerSecond, long delayInMS)
		{
			To = to;
			Queue = queue;
			BandwidthInBitsPerSecond = bandwidthInBitsPerSecond;
			LinkBandwidthInBitsPerSecond = linkBandwidthInBitsPerSecond;
			DelayInMS = delayInMS;

			udpClient = localClient;
			Busy = false;
			Queue.Name = string.Format ("->{0}", to);
		}

		public IEnumerable<Packet> ContentsInQueue{ get { return Queue.Contents; } }

		public void SendPacket (Packet packet)
		{
			Queue.Enqueue (packet);
			if (!Busy) {
				Busy = true;
				new Thread (SendPacket).Start ();
			}
		}

		private void SendPacket ()
		{
			while (Queue.Size>0) {
				ISerializable obj = Queue.Dequeue ();
				byte[] buf;
				using (MemoryStream ms = new MemoryStream()) {
					obj.WriteTo (ms);
					buf = ms.ToArray ();
				}
				// 8B MAC header, 20B IP header, 8B UDP header
				Thread.Sleep (GetSendDelayInMS ((buf.Length + 36) * BITS_IN_BYTE));

				udpClient.BeginSend (buf, buf.Length, To, (ar) => {}, null);
			}
//			Console.WriteLine ("Link quque empty");
			Busy = false;
		}

		private int GetSendDelayInMS (int lengthInBits)
		{
			return lengthInBits * 1000 / BandwidthInBitsPerSecond - lengthInBits * 1000 / LinkBandwidthInBitsPerSecond;
		}
	}
}

