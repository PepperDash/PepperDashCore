using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core
{
	/// <summary>
	/// A wrapper class that creates a TCP client and gather for use within S+
	/// </summary>
	public class SplusGenericTcpIpClientWithGather
	{
		public GenericTcpIpClient Client { get; private set; }
		public CommunicationGather Gather { get; private set; }

		public SplusGenericTcpIpClientWithGather()
		{			
		}
		
		/// <summary>
		/// In place of the useless contstructor, for S+ compatability
		/// </summary>
		/// <param name="key"></param>
		/// <param name="host"></param>
		/// <param name="port"></param>
		/// <param name="bufferSize"></param>
		/// <param name="delimiter"></param>
		public void Initialize(string key, string host, int port, int bufferSize, char delimiter)
		{
			Client = new GenericTcpIpClient(key, host, port, bufferSize);
			Gather = new CommunicationGather(Client, delimiter);
		}
	}
}