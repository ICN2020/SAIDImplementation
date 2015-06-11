using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Common
{
	public class Node
	{
		public string Name{ private set; get; }

		private UdpClient udpClient;

		public IPEndPoint LocalEP{ private set; get; }

		private Dictionary<IPEndPoint, Link> Links = new Dictionary<IPEndPoint, Link> ();

		public long SentData {
			get {
				long ret = 0;
				foreach (var l in Links.Values)
					ret += l.DataSize;
				return ret;
			}
		}

		public Node (string name, IPEndPoint localEP)
		{
			Name = name;
			LocalEP = localEP;
			udpClient = new UdpClient (localEP);
			new Thread (ReceiveThread).Start ();
		}

		private void ReceiveThread ()
		{
			while (true) {
				IPEndPoint remote = new IPEndPoint (0, 0);
				byte[] buf = udpClient.Receive (ref remote);
//				Console.WriteLine ("Received in Node {0}: {1}, {2}", Name, remote, buf.Length);
				if (!Links.ContainsKey (remote))
					continue;
				if (PacketReceived != null)
					PacketReceived (remote, buf);
			}
		}

		public override string ToString ()
		{
			return Name;
		}

		public void Link (IPEndPoint destination, IQueue<Packet> queue, int bandwidthInBitsPerSecond, int linkBandwidthInBitsPerSecond, long delayInMS)
		{
			Links.Add (destination, new Common.Link (udpClient, destination, queue, bandwidthInBitsPerSecond, linkBandwidthInBitsPerSecond, delayInMS));
		}

		public bool IsLinkedTo (IPEndPoint destination)
		{
			return Links.ContainsKey (destination);
		}

		public void SendPacket (IPEndPoint destination, Packet packet)
		{
			Link link;
			Links.TryGetValue (destination, out link);
			link.SendPacket (packet);
		}

		public event Action<IPEndPoint,byte[]> PacketReceived;

		public static void TestNode ()
		{
			IPAddress myAddress = new IPAddress (new byte[] { 192, 168, 50, 51 });
			DateTime startTime = DateTime.Now;
			Node n1 = new Node ("Test1", new IPEndPoint (myAddress, 9696));
			n1.PacketReceived += (addr,pkt) => {
				Console.WriteLine ("[{2}]Test1: From {0} length {1}", addr, pkt.Length, DateTime.Now.Subtract (startTime).TotalMilliseconds);
			};
			n1.Link (new IPEndPoint (myAddress, 9697), new REMQueue<Packet> (2000000 * 250 / 3 / Common.Link.MB, System.IO.TextWriter.Null), 2000000, 1000000000, 2000);
			Node n2 = new Node ("Test2", new IPEndPoint (myAddress, 9697));
			n2.PacketReceived += (addr,pkt) => {
				Console.WriteLine ("[{2}]Test2: From {0} length {1}", addr, pkt.Length, DateTime.Now.Subtract (startTime).TotalMilliseconds);
			};
			n2.Link (new IPEndPoint (myAddress, 9696), new REMQueue<Packet> (2000000 * 250 / 3 / Common.Link.MB, System.IO.TextWriter.Null), 2000000, 1000000000, 2000);
			for (int i =0; i < 1000; i++) {
				n2.SendPacket (n1.LocalEP, new Data ("Test", new RandomContent (1500 - 28)));
			}
			Console.WriteLine ("SendFinished");

//			UdpClient u2 = new UdpClient (9697);
//			u2.Send (new byte[300], 300, new IPEndPoint(myAddress, 9696));
		}

		public static void TestNodeN1 ()
		{
			IPAddress myAddress = new IPAddress (new byte[] { 192, 168, 50, 51 });
			IPAddress remoteAddress = new IPAddress (new byte[] { 192, 168, 50, 52 });
			DateTime startTime = DateTime.Now;
			Node n1 = new Node ("Test1", new IPEndPoint (myAddress, 9696));
			n1.PacketReceived += (addr,pkt) => {
				Console.WriteLine ("[{2}]Test1: From {0} length {1}", addr, pkt.Length, DateTime.Now.Subtract (startTime).TotalMilliseconds);
			};
			n1.Link (new IPEndPoint (remoteAddress, 9696), new REMQueue<Packet> (1000000 * 250 / 3 / Common.Link.MB, System.IO.TextWriter.Null), 1000000, 1000000000, 2000);
		}

		public static void TestNodeN2 ()
		{
			DateTime startTime = DateTime.Now;
			IPAddress myAddress = new IPAddress (new byte[] { 192, 168, 50, 52 });
			IPAddress remoteAddress1 = new IPAddress (new byte[] { 192, 168, 50, 51 });
			IPAddress remoteAddress2 = new IPAddress (new byte[] { 192, 168, 50, 53 });
			Node n2 = new Node ("Test2", new IPEndPoint (myAddress, 9696));
			n2.PacketReceived += (addr,pkt) => {
				Console.WriteLine ("[{2}]Test2: From {0} length {1}", addr, pkt.Length, DateTime.Now.Subtract (startTime).TotalMilliseconds);
			};
			n2.Link (new IPEndPoint (remoteAddress1, 9696), new REMQueue<Packet> (1000000 * 250 / 3 / Common.Link.MB, System.IO.TextWriter.Null), 1000000, 1000000000, 2000);
			n2.Link (new IPEndPoint (remoteAddress2, 9696), new REMQueue<Packet> (1000000 * 250 / 3 / Common.Link.MB, System.IO.TextWriter.Null), 1000000, 1000000000, 2000);
			for (int i =0; i < 10000; i++) {
				n2.SendPacket (new IPEndPoint (remoteAddress1, 9696), new Data ("Test", new RandomContent (1500 - 28)));
				n2.SendPacket (new IPEndPoint (remoteAddress2, 9696), new Data ("Test", new RandomContent (1500 - 28)));
			}
			Console.WriteLine ("SendFinished");
		}

		public static void TestNodeN3 ()
		{
			IPAddress myAddress = new IPAddress (new byte[] { 192, 168, 50, 53 });
			IPAddress remoteAddress = new IPAddress (new byte[] { 192, 168, 50, 52 });
			DateTime startTime = DateTime.Now;
			Node n1 = new Node ("Test1", new IPEndPoint (myAddress, 9696));
			n1.PacketReceived += (addr,pkt) => {
				Console.WriteLine ("[{2}]Test3: From {0} length {1}", addr, pkt.Length, DateTime.Now.Subtract (startTime).TotalMilliseconds);
			};
			n1.Link (new IPEndPoint (remoteAddress, 9696), new REMQueue<Packet> (1000000 * 250 / 3 / Common.Link.MB, System.IO.TextWriter.Null), 1000000, 1000000000, 2000);
		}
	}
}

