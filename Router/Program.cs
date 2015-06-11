using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Common;
using System.IO;

namespace Router
{
	class MainClass
	{
		public static void Main (string[] args)
		{
//			args = new string[] {
//				"192.168.50.51",
//				"9696",
//				"R0"
//			};

			IPAddress local = IPAddress.Parse (args [0]);
			short port = Convert.ToInt16 (args [1]);
			new System.Threading.Thread (REMQueue<Packet>.WriteThread).Start ();

			List<StreamWriter> writers = new List<StreamWriter> ();
			ForwardingEngine fe = new ForwardingEngine (args [2], new IPEndPoint (local, port));
			using (StreamReader reader = new StreamReader("links.txt")) {
				while (!reader.EndOfStream) {
					string[] parts = reader.ReadLine ().Split ('\t');
					string name = parts [0];
					IPAddress ip = IPAddress.Parse (parts [1]);
					int rPort = Convert.ToInt32 (parts [2]);
					int bandwidthInBitsPerSecond = Convert.ToInt32 (parts [3]) * Link.KB;
					long delay = Convert.ToInt64 (parts [4]);
					StreamWriter writer = new StreamWriter (args[2] + name + ".txt");
					writers.Add (writer);
					Console.WriteLine ("PTC:{0}", (int)(bandwidthInBitsPerSecond * 250.0 / 3 / Link.MB));
					fe.Link (new IPEndPoint (ip, rPort), new REMQueue<Packet> ((int)(bandwidthInBitsPerSecond * 250.0 / 3 / Link.MB), writer), bandwidthInBitsPerSecond, 1000000000, delay);
					Console.WriteLine ("Link: {0} {1}:{2} BW:{3} D:{4}", name, ip, rPort, bandwidthInBitsPerSecond, delay);
				}
			}
			using (StreamReader reader = new StreamReader("FIB.txt")) {
				while (!reader.EndOfStream) {
					string[] parts = reader.ReadLine ().Split ('\t');
					string name = parts [0];
					IPAddress ip = IPAddress.Parse (parts [1]);
					int rPort = Convert.ToInt32 (parts [2]);
					fe.AddFIB (name, new IPEndPoint (ip, rPort));
					Console.WriteLine ("FIB: {0}->{1}:{2}", name, ip, rPort);
				}
			}
			Console.CancelKeyPress += (s,a) => {
				Console.WriteLine ("exiting...");
				foreach (var w in writers) {
					w.Flush ();
					w.Close ();
				}
				Console.WriteLine ("exit");
			};
			while (true)
				System.Threading.Thread.Sleep (1000);
		}

		static void TestForwardingEngine (string[] args)
		{
			StreamWriter l1Writer = new StreamWriter ("l1.txt");
			StreamWriter l2Writer = new StreamWriter ("l2.txt");

			args = new string[] {
				"192.168.50.51",
				"9696"
			};

			IPAddress local = IPAddress.Parse (args [0]);
			short port = Convert.ToInt16 (args [1]);

			ForwardingEngine fe = new ForwardingEngine ("R0", new IPEndPoint (local, port));

			IPEndPoint n1Addr = new IPEndPoint (IPAddress.Parse (args [0]), 9697);
			IPEndPoint n2Addr = new IPEndPoint (IPAddress.Parse (args [0]), 9698);


			fe.Link (n1Addr, new REMQueue<Packet> (1000000 * 250 / 3 / Link.MB, l1Writer), 1000000, 1000000000, 2000);
			fe.Link (n2Addr, new REMQueue<Packet> (2000000 * 250 / 3 / Link.MB, l2Writer), 2000000, 1000000000, 2000);
			fe.AddFIB ("test", n2Addr, 1);
			Console.CancelKeyPress += (s,a) => {
				Console.WriteLine ("exiting...");
				l1Writer.Flush ();
				l2Writer.Flush ();
				Console.WriteLine ("exit");
			};

//			Endhost n1 = new Endhost ("N1", n1Addr, fe.LocalEP, 1000000, 1000000000, 2000);
//			Endhost n2 = new Endhost ("N2", n2Addr, fe.LocalEP, 1000000, 1000000000, 2000);

//			n1.SendInterest ("/test", 10);
//			System.Threading.Thread.Sleep (1000);
//			for (int i =0; i < 15; i++) {
//				n2.SendData ("/test/" + i, new RandomContent (1500 - 36));
//			}
//			fe.WritePIT (Console.Out);

		}

		private class Endhost : Node
		{
			public IPEndPoint FirstHop{ private set; get; }

			public Endhost (string name, IPEndPoint localEndpoint, IPEndPoint firstHop, int bandwidthInBitsPerSecond, int linkBandwidthInBitsPerSecond, long delayInMS) : base(name, localEndpoint)
			{
				Link (firstHop, new REMQueue<Packet> (bandwidthInBitsPerSecond * 250 / 3 / Common.Link.MB, TextWriter.Null), bandwidthInBitsPerSecond, linkBandwidthInBitsPerSecond, delayInMS);
				FirstHop = firstHop;
				this.PacketReceived += HandlePacket;
			}

			public void HandlePacket (IPEndPoint remote, byte[] packet)
			{
				byte type;
				bool marked;
				Packet.GetPacketTypeAndMark (packet, out type, out marked);
				switch (type) {
				case Packet.PACKET_INTEREST:
					{
						Interest interest;
						using (MemoryStream ms = new MemoryStream(packet)) {
							interest = new Interest (ms);
						}
						Console.WriteLine ("Receive in {0}: {1} {2}", Name, remote, interest);
						break;
					}
				case Packet.PACKET_DATA:
					{
						Data data;
						using (MemoryStream ms = new MemoryStream(packet)) {
							data = new Data (ms);
						}
						Console.WriteLine ("Receive in {0}: {1} {2}", Name, remote, data);
						break;
					}
				}
			}

			public void SendInterest (ContentName name, byte interestType)
			{
				SendPacket (FirstHop, new Interest (name, interestType));
			}

			public void SendData (ContentName name, ISerializable content)
			{
				SendPacket (FirstHop, new Data (name, content));
			}
		}
	}
}
