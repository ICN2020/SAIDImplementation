using System;
using System.Collections.Generic;

namespace Common
{
	public static class Helper
	{
		public static bool All<T> (this IEnumerable<T> list, Func<T, bool> predicate)
		{
			foreach (var t in list) 
				if (!predicate (t))
					return false;
			return true;
		}
	}
}

