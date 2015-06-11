using System;
using Common;
using System.Net;
using System.Net.Sockets;

namespace Test
{
	class MainClass
	{
		public static void Main (string[] args)
		{
//			Interest.TestInterest ();	
//			Data.TestData ();
//			Node.TestNode ();
//			TestNodes (args);
		}

		public static void TestNodes (string[] args)
		{
			if (args.Length == 0)
				return;
			if (args [0].Equals ("n1"))
				Node.TestNodeN1 ();
			else if (args [0].Equals ("n2"))
				Node.TestNodeN2 ();
			else
				Node.TestNodeN3 ();
		}
	}
}
