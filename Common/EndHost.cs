using System;
using System.Net;

namespace Common
{
	public class EndHost : Node
	{
		public IPEndPoint FirstHop{ private set; get; }

		public EndHost (string name, IPEndPoint localEP, IPEndPoint firstHop, IQueue<Packet> queue, int bandwidthInBitsPerSecond, int linkBandwidthInBitsPerSecond, long delayInMS) : base(name, localEP)
		{
			Link (firstHop, queue, bandwidthInBitsPerSecond, linkBandwidthInBitsPerSecond, delayInMS);
			FirstHop = firstHop;
		}

		public void SendPacket (Packet packet)
		{
			SendPacket (FirstHop, packet);
		}
	}
}

