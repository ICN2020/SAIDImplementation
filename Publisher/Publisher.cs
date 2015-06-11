using System;
using System.Net;
using Common;

namespace Publisher
{
	public class Publisher : EndHost
	{
		public ContentName FlowName{ private set; get; }

		public int PacketCount{ private set; get; }

		public Publisher (string name, IPEndPoint localEP, ContentName flowName, int packetCount, IPEndPoint firstHop, IQueue<Packet> queue, int bandwidthInBitsPerSecond, int linkBandwidthInBitsPerSecond, long delayInMS)
			:base(name, localEP,firstHop,queue,bandwidthInBitsPerSecond,linkBandwidthInBitsPerSecond,delayInMS)
		{
			FlowName = flowName;
			PacketCount = packetCount;
			for (int i = 0; i < PacketCount; i++)
				SendPacket (new Data (FlowName.GetChild (new string[] { i.ToString() }), new RandomContent (1500 - 36)));
		}
	}
}

