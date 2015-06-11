using System;
using System.Collections.Generic;

namespace Common
{
	public class FIFOQueue<T> : IQueue<T>
	{
		private Queue<T> innerQueue = new Queue<T> ();

		public int Capacity{ private set; get; }

		public FIFOQueue (int capacity)
		{
			Capacity = capacity;
		}
		#region IQueue implementation
		public void Enqueue (T item)
		{
			if (innerQueue.Count >= Capacity)
				return;
			innerQueue.Enqueue (item);
		}

		public T Dequeue ()
		{
			return innerQueue.Dequeue ();
		}

		public double RecalculateAverage (int modification)
		{
			throw new NotImplementedException ();
		}

		public string Name { set; get; }

		public int Size { get { return innerQueue.Count; } }

		public System.Collections.Generic.IEnumerable<T> Contents {
			get {
				foreach (var q in innerQueue)
					yield return q;
			}
		}
		#endregion
	}
}

