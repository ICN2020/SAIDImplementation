using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Common;

namespace Subscriber
{
	public class Subscriber : EndHost
	{
		public ContentName FlowName{ private set; get; }

		public Subscriber (string name, IPEndPoint localEP, ContentName flowName, IPEndPoint firstHop, IQueue<Packet> queue, int bandwidthInBitsPerSecond, int linkBandwidthInBitsPerSecond, long delayInMS) : base(name, localEP, firstHop, queue, bandwidthInBitsPerSecond, linkBandwidthInBitsPerSecond,delayInMS)
		{
			this.PacketReceived += HandlePacket;
			FlowName = flowName;
			SendInterest (1);
		}

		private void HandlePacket (IPEndPoint remote, byte[] buf)
		{
			byte type;
			bool mark;
			Packet.GetPacketTypeAndMark (buf, out type, out mark);
			if (type == Packet.PACKET_DATA) {
				Data data;
				using (MemoryStream ms = new MemoryStream(buf)) {
					data = new Data (ms);
				}
				if (FlowName.IsPrefixOf (data.Name))
					HandleData (data);
			}
		}

		private int MinorityWindowSize = 1;
		private int MinorityOutstandingSubscriptions = 1;
		private int MinorityAccumulate = 0;
		public const int WINDOW_COUNT_THRESHOLD = 5;
		public const int WINDOW_SIZE_THRESHOLD = 5;
		private int MinPRInWindow = Int32.MaxValue;
		private LinkedList<int> MinPRs = new LinkedList<int> ();
		private int State = 0;

		private void HandleData (Data data)
		{
			MinorityOutstandingSubscriptions--;
			if (data.MPR < MinPRInWindow)
				MinPRInWindow = data.MPR;
			if (data.Mark) {
				MinorityAccumulate = 0;
				if (MinorityWindowSize > 1)
					MinorityWindowSize /= 2;
				MinPRs.Clear ();
				MinPRInWindow = Int32.MaxValue;
				State = 0;
			} else {
				MinorityAccumulate++;
				while (MinorityAccumulate>=MinorityWindowSize) {
					MinorityAccumulate -= MinorityWindowSize;
					switch (State) {
					case 0:
						{
							if (MinPRInWindow >= WINDOW_SIZE_THRESHOLD)
								State = 1;
							else {
								MinorityWindowSize++;
								if (MinPRInWindow > 0)
									State = 2;
							}
							break;
						}
					case 1:
						{
							if (MinPRInWindow == 0) {
								MinorityWindowSize++;
								State = 0;
							} else {
								if (MinPRs.Count >= WINDOW_COUNT_THRESHOLD && MinPRs.All (x => x > 0))
									State = 3;
								else if (MinPRInWindow < WINDOW_SIZE_THRESHOLD) {
									MinorityWindowSize++;
									State = 2;
								}
							}
							break;
						}
					case 2:
						{
							if (MinPRInWindow >= WINDOW_SIZE_THRESHOLD)
								State = 1;
							else if (MinPRInWindow == 0) {
								MinorityWindowSize++;
								State = 0;
							} else if (MinPRs.Count >= WINDOW_COUNT_THRESHOLD && MinPRs.All (x => x > 0))
								State = 3;
							else
								MinorityWindowSize++;
							break;
						}
					case 3:
						{
							if (MinPRInWindow == 0)
								State = 4;
							break;
						}
					case 4:
						{
							if (MinPRInWindow > 0)
								State = 3;
							else if (MinPRs.Count >= WINDOW_COUNT_THRESHOLD && MinPRs.All (x => x == 0)) {
								MinorityWindowSize++;
								State = 0;
							}
							break;
						}
					}
					MinPRs.AddLast (MinPRInWindow);
					while (MinPRs.Count>WINDOW_COUNT_THRESHOLD)
						MinPRs.RemoveFirst ();
					MinPRInWindow = Int32.MaxValue;
				}
			}
			int count = MinorityWindowSize - MinorityOutstandingSubscriptions;
			if (count > 0) {
				SendInterest (count);
				MinorityOutstandingSubscriptions = MinorityWindowSize;
			}
			Console.WriteLine ("{0},{1},{2},{3}", MinorityWindowSize, State, MinPRInWindow, data.Mark);
		}

		private void SendInterest (int count)
		{
			SendPacket (new Interest (FlowName, (byte)count));
		}
	}
}

