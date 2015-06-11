using System;
using System.Collections.Generic;
using System.IO;

namespace Common
{
	public class REMQueue<T> : IQueue<T>
		where T : Packet
	{
		private static Queue<Tuple<TextWriter, string>> ToWrites = new Queue<Tuple<TextWriter, string>> ();

		public static void WriteThread ()
		{
			while (true) {
				Tuple<TextWriter, string> toWrite = null;
				lock (ToWrites) {
					if (ToWrites.Count > 0) {
						toWrite = ToWrites.Dequeue ();
					}
				}
				if (toWrite == null) {
					System.Threading.Thread.Sleep (200);
				} else {
					toWrite.Item1.WriteLine (toWrite.Item2);
				}
			}
		}

		private static void AddWrite (TextWriter writer, string toWrite)
		{
			lock (ToWrites) {
				ToWrites.Enqueue (new Tuple<TextWriter, string> (writer, toWrite));
			}
		}

		public const double WQ = 0.002;
		public const int MINTH = 5;
		public const int MAXTH = MINTH * 3;
		public const double MAXP = 0.1;
		private TextWriter Writer;

		public static double f (DateTime time, DateTime q_time, int ptc)
		{

			return ptc * time.Subtract (q_time).TotalMilliseconds / 1000.0;
			//			return ptc * (time - q_time) / 1000000.0;
		}

		public static Random RAND;

		static REMQueue ()
		{
			int seed = 68413;
			Console.WriteLine ("Random Seed: {0}", seed);
			RAND = new Random (seed);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Common.REMQueue`1"/> class.
		/// </summary>
		/// <param name="ptc">Bandwidth in Mbps * 250 / 3</param>
		public REMQueue (int ptc, TextWriter writer)
		{
			innerQueue = new Queue<T> ();
			Capacity = 0;
			avg = 0;
			count = -1;
			PTC = ptc;
			Writer = writer;
		}

		public string Name { set; get; }

		public int Capacity { private set; get; }

		private Queue<T> innerQueue;

		public int Size { get { return innerQueue.Count; } }

		public int PTC { private set; get; }

		double avg;
		int count;
		DateTime q_time;

		public IEnumerable<T> Contents {
			get {
				foreach (var i in innerQueue)
					yield return i;
			}
		}

		public double RecalculateAverage (int modification)
		{
			// calculate new avg. queue size avg:
			if (innerQueue.Count != 1)
				avg = (1 - WQ) * avg + WQ * (innerQueue.Count + modification);
			else
				avg = Math.Pow ((1 - WQ), f (DateTime.Now, q_time, PTC)) * avg;

			AddWrite (Writer, string.Format ("Q\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}", Name, DateTime.Now.Subtract (DateTime.Today).TotalMilliseconds, innerQueue.Count, avg, 0, modification));

			return avg;
		}

		public void Enqueue (T item)
		{
			innerQueue.Enqueue (item);

			RecalculateAverage (0);

			if (avg < MINTH) {
				AddWrite (Writer, string.Format ("Q\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}", Name, DateTime.Now.Subtract (DateTime.Today).TotalMilliseconds, innerQueue.Count, avg, 0, item));
				count = -1;
			} else if (avg < 2 * MAXTH) {
				count++;
				double pb;
				if (avg < MAXTH)
					pb = MAXP * (avg - MINTH) / (MAXTH - MINTH);
				else
					pb = (1 - MAXP) / MAXTH * avg + 2 * MAXP - 1;
				double pa = (count * pb > 1) ? 1 : pb / (1 - count * pb);
				AddWrite (Writer, string.Format ("Q\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}", Name, DateTime.Now.Subtract (DateTime.Today).TotalMilliseconds, innerQueue.Count, avg, pa, item));
				if (RAND.NextDouble () < pa) {
					AddWrite (Writer, string.Format ("M\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}", Name, DateTime.Now.Subtract (DateTime.Today).TotalMilliseconds, innerQueue.Count, avg, pa, item));
					item.Mark = true;
					count = 0;
				}
			} else {
				AddWrite (Writer, string.Format ("Q\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}", Name, DateTime.Now.Subtract (DateTime.Today).TotalMilliseconds, innerQueue.Count, avg, 1, item));
				AddWrite (Writer, string.Format ("M\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}", Name, DateTime.Now.Subtract (DateTime.Today).TotalMilliseconds, innerQueue.Count, avg, 1, item));
				item.Mark = true;
				count = 0;
			}
		}

		public T Dequeue ()
		{
			T item = innerQueue.Dequeue ();
			AddWrite (Writer, string.Format ("Q\t{0}\t{1}\t{2}\t\t\t{3}", Name, DateTime.Now.Subtract (DateTime.Today).TotalMilliseconds, innerQueue.Count, item));
			if (innerQueue.Count == 0)
				q_time = DateTime.Now;
			return item;
		}
	}
}

