using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PepperDash.Core
{
	public class GenericTcpIpClient : Device, ISocketStatus, IAutoReconnect
	{
		/// <summary>
		/// 
		/// </summary>
		public event EventHandler<GenericCommMethodReceiveBytesArgs> BytesReceived;

		/// <summary>
		/// 
		/// </summary>
		public event EventHandler<GenericCommMethodReceiveTextArgs> TextReceived;

		/// <summary>
		/// 
		/// </summary>
		public event TCPClientSocketStatusChangeEventHandler SocketStatusChange;

		/// <summary>
		/// 
		/// </summary>
		public TCPClient Client { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public bool IsConnected { get { return Client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED; } }

		/// <summary>
		/// 
		/// </summary>
		public SocketStatus ClientStatus { get { return Client.ClientStatus; } }
		
		/// <summary>
		/// 
		/// </summary>
		public string ClientStatusText { get { return Client.ClientStatus.ToString(); } }

		/// <summary>
		/// 
		/// </summary>
		public ushort UClientStatus { get { return (ushort)Client.ClientStatus; } }

		/// <summary>
		/// 
		/// </summary>
		public string ConnectionFailure { get { return Client.ClientStatus.ToString(); } }

		/// <summary>
		/// 
		/// </summary>
		public bool AutoReconnect { get; set; }
		
		/// <summary>
		/// 
		/// </summary>
		public int AutoReconnectIntervalMs { get; set; }

		/// <summary>
		/// Set only when the disconnect method is called.
		/// </summary>
		bool DisconnectCalledByUser;

		/// <summary>
		/// 
		/// </summary>
		public bool Connected
		{
			get { return Client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED; }
		}

		CTimer RetryTimer;

		public GenericTcpIpClient(string key, string address, int port, int bufferSize)
			: base(key)
		{
			Client = new TCPClient(address, port, bufferSize);
			Client.SocketStatusChange += Client_SocketStatusChange;
		}

		//public override bool CustomActivate()
		//{
		//    return true;
		//}

		public override bool Deactivate()
		{
			Client.SocketStatusChange -= this.Client_SocketStatusChange;
			return true;
		}

		public void Connect()
		{
			Client.ConnectToServerAsync(null);
			DisconnectCalledByUser = false;
		}

		public void Disconnnect()
		{
			DisconnectCalledByUser = true;
			Client.DisconnectFromServer();
		}

		void ConnectToServerCallback(TCPClient c)
		{
			if (c.ClientStatus != SocketStatus.SOCKET_STATUS_CONNECTED)
				WaitAndTryReconnect();
		}

		void WaitAndTryReconnect()
		{
			Client.DisconnectFromServer();
			Debug.Console(2, "Attempting reconnect, status={0}", Client.ClientStatus);
			RetryTimer = new CTimer(o => { Client.ConnectToServerAsync(ConnectToServerCallback); }, 1000);
		}

		void Receive(TCPClient client, int numBytes)
		{
			if (numBytes > 0)
			{
				var bytes = client.IncomingDataBuffer.Take(numBytes).ToArray();
 				var bytesHandler = BytesReceived;
				if (bytesHandler != null)
					bytesHandler(this, new GenericCommMethodReceiveBytesArgs(bytes));
				var textHandler = TextReceived;
				if (textHandler != null)
				{
					var str = Encoding.GetEncoding(28591).GetString(bytes, 0, bytes.Length);
					textHandler(this, new GenericCommMethodReceiveTextArgs(str));
				}
			}
			Client.ReceiveDataAsync(Receive);
		}

		/// <summary>
		/// General send method
		/// </summary>
		public void SendText(string text)
		{
			var bytes = Encoding.GetEncoding(28591).GetBytes(text);
			// Check debug level before processing byte array
			if (Debug.Level == 2)
				Debug.Console(2, this, "Sending {0} bytes: '{1}'", bytes.Length, ComTextHelper.GetEscapedText(bytes));
			Client.SendData(bytes, bytes.Length);
		}

		/// <summary>
		/// This is useful from console and...?
		/// </summary>
		public void SendEscapedText(string text)
		{
			var unescapedText = Regex.Replace(text, @"\\x([0-9a-fA-F][0-9a-fA-F])", s =>
				{
					var hex = s.Groups[1].Value;
					return ((char)Convert.ToByte(hex, 16)).ToString();
				});
			SendText(unescapedText);

		}

		public void SendBytes(byte[] bytes)
		{
			if (Debug.Level == 2)
				Debug.Console(2, this, "Sending {0} bytes: '{1}'", bytes.Length, ComTextHelper.GetEscapedText(bytes));
			Client.SendData(bytes, bytes.Length);
		}


		void Client_SocketStatusChange(TCPClient client, SocketStatus clientSocketStatus)
		{
			Debug.Console(2, this, "Socket status change {0} ({1})", clientSocketStatus, UClientStatus);
			if (client.ClientStatus != SocketStatus.SOCKET_STATUS_CONNECTED && !DisconnectCalledByUser)
				WaitAndTryReconnect();

			// Probably doesn't need to be a switch since all other cases were eliminated
			switch (clientSocketStatus)
			{
				case SocketStatus.SOCKET_STATUS_CONNECTED:
					Client.ReceiveDataAsync(Receive);
					DisconnectCalledByUser = false;
					break;
			}

			// Relay the event
			var handler = SocketStatusChange;
			if (handler != null)
				SocketStatusChange(client, clientSocketStatus);
		}
	}


	public class TcpSshPropertiesConfig
	{
		[JsonProperty(Required = Required.Always)]
		public string Address { get; set; }
		
		[JsonProperty(Required = Required.Always)]
		public int Port { get; set; }
		
		public string Username { get; set; }
		public string Password { get; set; }

		/// <summary>
		/// Defaults to 32768
		/// </summary>
		public int BufferSize { get; set; }

		/// <summary>
		/// Defaults to true
		/// </summary>
		public bool AutoReconnect { get; set; }

		/// <summary>
		/// Defaults to 5000ms
		/// </summary>
		public int AutoReconnectIntervalMs { get; set; }

		public TcpSshPropertiesConfig()
		{
			BufferSize = 32768;
			AutoReconnect = true;
			AutoReconnectIntervalMs = 5000;
		}

	}

	//public class TcpIpConfig
	//{
	//    [JsonProperty(Required = Required.Always)]
	//    public string Address { get; set; }

	//    [JsonProperty(Required = Required.Always)]
	//    public int Port { get; set; }

	//    /// <summary>
	//    /// Defaults to 32768
	//    /// </summary>
	//    public int BufferSize { get; set; }

	//    /// <summary>
	//    /// Defaults to true
	//    /// </summary>
	//    public bool AutoReconnect { get; set; }

	//    /// <summary>
	//    /// Defaults to 5000ms
	//    /// </summary>
	//    public int AutoReconnectIntervalMs { get; set; }

	//    public TcpIpConfig()
	//    {
	//        BufferSize = 32768;
	//        AutoReconnect = true;
	//        AutoReconnectIntervalMs = 5000;
	//    }
	//}

}