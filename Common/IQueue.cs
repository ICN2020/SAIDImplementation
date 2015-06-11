using System;
using System.Collections.Generic;

namespace Common
{
	public interface IQueue<T>
	{
		string Name{ set; get; }

		int Capacity{ get; }

		int Size{ get; }

		void Enqueue (T item);

		T Dequeue ();

		IEnumerable<T> Contents{ get; }

		double RecalculateAverage (int modification);
	}
}

