using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Common
{
	public class InterestTable<T>
	{
		private TableTreeNode Root;

		public InterestTable ()
		{
			Root = new TableTreeNode ();
		}

		public void ForEachName (Action<ContentName> f)
		{
			Root.ForEachName (f, new List<string> ());
		}

		public void ForEachValue (Action<T> f)
		{
			Root.ForEachValue (f);
		}

		public void ForEachPair (Action<ContentName, T> f)
		{
			Root.ForEachPair (f, new LinkedList<string> ());
		}

		/// <summary>
		/// Iterate through all the values the target can satisfy
		/// More light-weight compared to ForEachLongestPrefixMatch. Use this when you don't really need the names.
		/// </summary>
		/// <param name="target">name to match</param>
		/// <param name="f">action on each value</param>
		public void ForEachLongestPrefixMatchingValue (ContentName target, Action<T> f)
		{
			Root.ForEachLongestPrefixMatchingValue (target, 0, f);
		}

		/// <summary>
		/// Iterate through all the pairs the target can satisfy
		/// </summary>
		/// <param name="target">name to match</param>
		/// <param name="f">action on each match</param>
		public void ForEachLongestPrefixMatch (ContentName target, Action<ContentName, T> f)
		{
			Root.ForEachLongestPrefixMatch (target, 0, f);
		}

		/// <summary>
		/// Iterate through all the values that the target can satisfy
		/// More light-weight compared to ForEachSatisfiedPair. Use this when you don't really need the names.
		/// </summary>
		/// <param name="target">name to satisfy</param>
		/// <param name="f">action on each value</param>
		public void ForEachSatisfiedValue (ContentName target, Action<T> f)
		{
			Root.ForEachSatisfiedValue (target, 0, f);
		}

		/// <summary>
		/// Iterate through all the pairs that the target can satisfy
		/// </summary>
		/// <param name="target">name to satisfy</param>
		/// <param name="f">action on each pair</param>
		public void ForEachSatisfiedPair (ContentName target, Action<ContentName, T> f)
		{
			Root.ForEachSatisfiedPair (target, 0, f);
		}

		/// <summary>
		/// Add a value associated with content name to the table
		/// </summary>
		/// <param name="name">name of the content</param>
		/// <param name="value">associated object</param>
		/// <param name="exclude">if exclude add</param>
		/// <param name="nameAdded">if a new name has been added</param>
		/// <returns>if content is added</returns>
		public void Add (ContentName name, T value, out bool nameAdded, out bool valueAdded, bool exclude = false)
		{
			Trace.Assert (name != null && value != null);
			Root.AddValue (name, 0, value, out valueAdded, out nameAdded, exclude);
		}

		public T LookupExactOrAddNew (ContentName target, Func<T> generator)
		{
			return Root.LookupExactOrAddNew (target, 0, generator);
		}

		/// <summary>
		/// Remove all the values satisfying the target name
		/// 
		/// </summary>
		/// <param name="target">name to remove</param>
		/// <param name="f">action on each value (to-remove)</param>
		public void RemoveSatisfiedValues (ContentName target, Action<T> f)
		{
			Root.RemoveSatisfiedValues (target, 0, f);
		}

		/// <summary>
		/// Remove all the values matching the target name (the whole sub-tree)
		/// </summary>
		/// <param name="target">name to remove</param>
		/// <param name="f">action on each match (to-remove)</param>
		public void RemoveMatches (ContentName target, Action<ContentName, T> f)
		{
			Root.RemoveSatisfiedMatches (target, 0, f);
		}

		private class TableTreeNode
		{
			private Dictionary<string, TableTreeNode> children = new Dictionary<string, TableTreeNode> ();
			private LinkedList<T> contents = new LinkedList<T> ();

			public void ForEachName (Action<ContentName> f, List<string> parts)
			{
				if (contents.Count > 0)
					f (new ContentName (parts));
				foreach (var child in children) {
					parts.Add (child.Key);
					child.Value.ForEachName (f, parts);
					parts.RemoveAt (parts.Count - 1);
				}
			}

			public void ForEachValue (Action<T> f)
			{
				if (contents.Count > 0)
					foreach (T t in contents)
						f (t);
				foreach (var child in children.Values)
					child.ForEachValue (f);
			}

			public void ForEachPair (Action<ContentName, T> f, LinkedList<string> parts)
			{
				if (contents.Count > 0) {
					ContentName name = new ContentName (parts);
					foreach (T t in contents)
						f (name, t);
				}
				foreach (var child in children) {
					parts.AddLast (child.Key);
					child.Value.ForEachPair (f, parts);
					parts.RemoveLast ();
				}
			}

			public bool ForEachLongestPrefixMatchingValue (ContentName name, int part, Action<T> f)
			{
				TableTreeNode child;
				if (part < name.ComponentCount && children.TryGetValue (name [part], out child) && child.ForEachLongestPrefixMatchingValue (name, part + 1, f))
					return true;
				if (contents.Count > 0) {
					foreach (T content in contents)
						f (content);
					return true;
				}
				return false;
			}

			public bool ForEachLongestPrefixMatch (ContentName name, int part, Action<ContentName, T> f)
			{
				TableTreeNode child;
				if (part < name.ComponentCount && children.TryGetValue (name [part], out child) && child.ForEachLongestPrefixMatch (name, part + 1, f))
					return true;
				if (contents.Count > 0) {
					name = new ContentName (name, part);
					foreach (T content in contents)
						f (name, content);
					return true;
				}
				return false;
			}

			public void ForEachSatisfiedValue (ContentName name, int part, Action<T> f)
			{
				TableTreeNode child;
				if (part < name.ComponentCount && children.TryGetValue (name [part], out child))
					child.ForEachSatisfiedValue (name, part + 1, f);
				foreach (T content in contents)
					f (content);
			}

			public void ForEachSatisfiedPair (ContentName name, int part, Action<ContentName, T> f)
			{
				TableTreeNode child;
				if (part < name.ComponentCount && children.TryGetValue (name [part], out child))
					child.ForEachSatisfiedPair (name, part + 1, f);
				if (contents.Count > 0) {
					ContentName n = new ContentName (name, part);
					foreach (T content in contents)
						f (n, content);
				}
			}

			public void AddValue (ContentName name, int part, T value, out bool valueAdded, out bool nameAdded, bool exclude)
			{
				if (part == name.ComponentCount) {
					if (exclude && contents.Contains (value)) {
						nameAdded = false;
						valueAdded = false;
					}
					contents.AddLast (value);
					nameAdded = contents.Count == 1;
					valueAdded = true;
				} else {
					string s = name [part];
					TableTreeNode child;
					if (!children.TryGetValue (s, out child))
						children.Add (s, child = new TableTreeNode ());
					child.AddValue (name, part + 1, value, out valueAdded, out nameAdded, exclude);
				}
			}

			public bool RemoveSatisfiedValues (ContentName name, int part, Action<T> f)
			{
				TableTreeNode child;
				if (part < name.ComponentCount && children.TryGetValue (name [part], out child))
				if (child.RemoveSatisfiedValues (name, part + 1, f))
					children.Remove (name [part]);
				if (contents.Count > 0) {
					foreach (T content in contents)
						f (content);
					contents.Clear ();
				}
				return children.Count == 0;
			}

			public bool RemoveSatisfiedMatches (ContentName name, int part, Action<ContentName, T> f)
			{
				TableTreeNode child;
				if (part < name.ComponentCount && children.TryGetValue (name [part], out child))
				if (child.RemoveSatisfiedMatches (name, part + 1, f))
					children.Remove (name [part]);
				if (contents.Count > 0) {
					name = new ContentName (name, part);
					foreach (T content in contents)
						f (name, content);
					contents.Clear ();
				}
				return children.Count == 0;
			}

			public T LookupExactOrAddNew (ContentName name, int part, Func<T> f)
			{
				if (part == name.ComponentCount) {
					if (contents.Count > 0)
						return contents.First.Value;
					else
						return contents.AddLast (f ()).Value;
				} else {
					string s = name [part];
					TableTreeNode child;
					if (!children.TryGetValue (s, out child))
						children.Add (s, child = new TableTreeNode ());
					return child.LookupExactOrAddNew (name, part + 1, f);
				}
			}
		}
	}
}

