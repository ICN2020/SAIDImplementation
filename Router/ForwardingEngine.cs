using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using Common;

namespace Router
{
	public class FaceCounter
	{
		public int Count { set; get; }
	}

	public class PITEntry
	{
		private Dictionary<IPEndPoint, FaceCounter> Faces { set; get; }

		public int Max { private set; get; }

		public PITEntry ()
		{
			Faces = new Dictionary<IPEndPoint, FaceCounter> ();
			Max = 0;
		}

		private int CalculateNewMax ()
		{
			int max = 0;
			foreach (FaceCounter counter in Faces.Values) {
				if (counter.Count > max)
					max = counter.Count;
			}

			return max;
		}

		/// <summary>
		/// Called when a subscription packet comes, update the count of a face.
		/// 
		/// if count == Int.MaxValue, it means inf.
		/// </summary>
		/// <param name="face">The specified face</param>
		/// <param name="count">The count of that face, Int32.MaxValue for infinitive</param>
		/// <param name="CountInQueue">A count function that will calculate the packets with the same CD in the queue</param>
		/// <returns>-1 if no change on that CD, </returns>
		public int UpdateFace (IPEndPoint face, int count, Func<int> CountInQueue, out int realFaceCount)
		{
			// To obsolate count = -1
			Trace.Assert (count >= 0);

			FaceCounter counter;
			if (!Faces.TryGetValue (face, out counter))
				Faces [face] = counter = new FaceCounter ();

			if (counter.Count == Int32.MaxValue) {
				if (count != Int32.MaxValue) {
					// Modified here, if the subscription changed from inf. to a certain value, use count - N as new count
					counter.Count = count - CountInQueue ();
				}
				//else do nothing
			} else {
				if (count == Int32.MaxValue)
					counter.Count = Int32.MaxValue;
				else
					counter.Count += count;
			}
			realFaceCount = counter.Count;

			int newMax = CalculateNewMax ();
			if (newMax == Int32.MaxValue) {
				if (Max == Int32.MaxValue)
					return -1;
				else
					return Max = Int32.MaxValue;
			} else {
				if (Max == Int32.MaxValue)
					return Max = newMax;
				else {
					int diff = newMax - Max;
					Max = newMax;
					return diff;
				}
			}
		}

		/// <summary>
		/// Called when a publication packet comes, add the face to the face list and reduce the count if needed.
		/// </summary>
		/// <param name="faces"></param>
		/// <returns>The face count that added into the face list</returns>
		public int GetFaces (List<IPEndPoint> faces)
		{
			int count = 0;
			foreach (KeyValuePair<IPEndPoint, FaceCounter> f in Faces) {
				if (f.Value.Count > 0) {
					count++;
					faces.Add (f.Key);
					if (f.Value.Count != Int32.MaxValue)
						f.Value.Count--;
				}
			}
			Max = CalculateNewMax ();
			return count;
		}

		public IEnumerable<Tuple<IPEndPoint, int>> UpdateLegalFaces ()
		{
			foreach (KeyValuePair<IPEndPoint, FaceCounter> f in Faces) {
				if (f.Value.Count > 0) {
					if (f.Value.Count != Int32.MaxValue)
						f.Value.Count--;
					yield return new Tuple<IPEndPoint, int> (f.Key, f.Value.Count);
				}
			}
			Max = CalculateNewMax ();
		}

		public void WriteFaces (ContentName name, TextWriter writer)
		{
			foreach (KeyValuePair<IPEndPoint, FaceCounter> pair in Faces) {
				writer.WriteLine ("{0} -> {1}:{2}", name, pair.Key, pair.Value.Count);
			}
		}
	}

	public class ContentStore
	{
		private Dictionary<ContentName, ISerializable> container = new Dictionary<ContentName, ISerializable> ();
		private LinkedList<ContentName> LRU = new LinkedList<ContentName> ();

		private int Capacity { set; get; }

		public ContentStore (int capacity = 100)
		{
			Capacity = capacity;
		}

		public void AddContent (ContentName name, ISerializable content)
		{
			LinkedListNode<ContentName> node = LRU.Find (name);
			if (node == null) {
				container.Add (name, content);
				LRU.AddLast (name);
			} else {
				container [name] = content;
				LRU.Remove (node);
				LRU.AddLast (node);
			}
			while (LRU.Count > Capacity) {
				node = LRU.First;
				container.Remove (node.Value);
				LRU.RemoveFirst ();
			}
		}

		public bool TryGetContent (ContentName name, out ISerializable content)
		{
			return container.TryGetValue (name, out content);
		}
	}

	public class FIBEntry
	{
		public IPEndPoint Node;
		public int Hops;

		public override string ToString ()
		{
			return string.Format ("{0}:{1}", Node, Hops);
		}
	}

	public class ForwardingEngine : Node
	{
		private InterestTable<FIBEntry> FIB = new InterestTable<FIBEntry> ();
		private InterestTable<PITEntry> PIT = new InterestTable<PITEntry> ();
		//private ContentStore ContentStore = new ContentStore ();
		public ForwardingEngine (string name, IPEndPoint localEP) : base(name, localEP)
		{
			this.PacketReceived += HandlePacket;
		}

		private void HandlePacket (IPEndPoint remoteEP, byte[] packet)
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
					HandleInterest (remoteEP, interest);
					break;
				}
			case Packet.PACKET_DATA:
				{
					Data data;
					using (MemoryStream ms = new MemoryStream(packet)) {
						data = new Data (ms);
					}
					HandleData (remoteEP, data);
					break;
				}
			}
		}

		public void AddFIB (ContentName prefix, IPEndPoint target, int hops = 1)
		{
			Trace.Assert (IsLinkedTo (target));
			bool exist = false;
			FIB.ForEachLongestPrefixMatch (prefix, (name, entry) =>
			{
				if (!name.Equals (prefix) || entry.Node != target)
					return;
				exist = true;
				entry.Hops = hops;
			});
			if (!exist) {
				bool nameAdded, valueAdded;
				FIB.Add (prefix, new FIBEntry { Node = target, Hops = hops }, out nameAdded, out valueAdded);
			}
		}

		public void WritePIT (TextWriter writer)
		{
			PIT.ForEachPair ((name,entry) => {
				entry.WriteFaces (name, writer);
			});
		}

		private void HandleInterest (IPEndPoint remoteEP, Interest interest)
		{
			switch (interest.InterestType) {
			case Interest.InterestACK:
				HandleACK (remoteEP, interest);
				break;
			case Interest.InterestRequest:
				break;
			default:
				HandleSubscription (remoteEP, interest);
				break;
			}
		}

		private void HandleACK (IPEndPoint remoteEP, Interest interest)
		{
			Dictionary<IPEndPoint, bool> candidates = new Dictionary<IPEndPoint, bool> ();
			FIB.ForEachLongestPrefixMatchingValue (interest.Name, f => candidates [f.Node] = true);
			foreach (var ep in candidates.Keys)
				SendPacket (ep, interest);
		}

		private void HandleSubscription (IPEndPoint remoteEP, Interest interest)
		{
			Dictionary<IPEndPoint, int> forwards = new Dictionary<IPEndPoint, int> ();
			PITEntry entry = PIT.LookupExactOrAddNew (interest.Name, () => new PITEntry ());
			int realFaceCount;
			int ret = entry.UpdateFace (remoteEP, interest.InterestType == Interest.InterestACKer ? Int32.MaxValue : interest.InterestType, () =>
			{
				throw new Exception ("Not Implemented");
//				int count = 0;
//				foreach (ISerializable s in QueueContents(from)) {
//					CCNPacket tmp = s as CCNPacket;
//					if (tmp == null || tmp.Type != CCNPacket.PACKET_PUBLICATION)
//						continue;
//					if (Array.Exists (tmp.CDs, x => cd.Key.IsPrefixOf (x.Key)))
//						count++;
//				}
//				return count;
				//return 0;
			}, out realFaceCount);

			if (ret == Int32.MaxValue)
				ret = Interest.InterestACKer;
			if (ret != -1) {
				FIB.ForEachLongestPrefixMatchingValue (interest.Name, e => {
					forwards [e.Node] = ret;
				});
			}
			foreach (var p in forwards)
				SendPacket (p.Key, new Interest (interest.Name, (byte)p.Value));
		}

		private void HandleData (IPEndPoint remoteEP, Data data)
		{
			Dictionary<IPEndPoint, int> toSend = new Dictionary<IPEndPoint, int> ();
			PIT.ForEachSatisfiedValue (data.Name, e => {
				foreach (var t in e.UpdateLegalFaces()) {
					toSend[t.Item1]=t.Item2;
				}
			});
			UInt16 origMPR = data.MPR;
			foreach (var ep in toSend) {
				data.MPR = origMPR < ep.Value ? origMPR : (UInt16)ep.Value;
				SendPacket (ep.Key, data);
			}
		}
	}
}

