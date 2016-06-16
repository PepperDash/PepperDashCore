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
	public class GenericTcpIpClient : Device, IBasicCommunication
	{
		public event EventHandler<GenericCommMethodReceiveBytesArgs> BytesReceived;
		public event EventHandler<GenericCommMethodReceiveTextArgs> TextReceived;

		public bool IsConnected { get { return Client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED; } }
		public string Status { get { return Client.ClientStatus.ToString(); } }
		public string ConnectionFailure { get { return Client.ClientStatus.ToString(); } }

		public bool Connected
		{
			get { return Client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED; }
		}

		public TCPClient Client { get; private set; }
		CTimer RetryTimer;

		public GenericTcpIpClient(string key, string address, int port, int bufferSize)
			: base(key)
		{
			Client = new TCPClient(address, port, bufferSize);
		}

		public override bool CustomActivate()
		{
			Client.SocketStatusChange += new TCPClientSocketStatusChangeEventHandler(Client_SocketStatusChange);
			return true;
		}

		public override bool Deactivate()
		{
			Client.SocketStatusChange -= this.Client_SocketStatusChange;
			return true;
		}

		public void Connect()
		{
			Client.ConnectToServerAsync(null);
		}

		public void Disconnnect()
		{
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
				//if (Debug.Level == 2)
				//    Debug.Console(2, this, "Received: {0} bytes: '{1}'", bytes.Length, ComTextHelper.GetEscapedText(bytes));
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
			//if (Client.ClientStatus != SocketStatus.SOCKET_STATUS_CONNECTED)
			//    Connect();
			var unescapedText = Regex.Replace(text, @"\\x([0-9a-fA-F][0-9a-fA-F])", s =>
				{
					var hex = s.Groups[1].Value;
					return ((char)Convert.ToByte(hex, 16)).ToString();
				});
			SendText(unescapedText);

			//var bytes = Encoding.GetEncoding(28591).GetBytes(unescapedText);
			//Debug.Console(2, this, "Sending {0} bytes: '{1}'", bytes.Length, text);
			//Client.SendData(bytes, bytes.Length);
		}

		public void SendBytes(byte[] bytes)
		{
			if (Debug.Level == 2)
				Debug.Console(2, this, "Sending {0} bytes: '{1}'", bytes.Length, ComTextHelper.GetEscapedText(bytes));
			Client.SendData(bytes, bytes.Length);
		}


		void Client_SocketStatusChange(TCPClient client, SocketStatus clientSocketStatus)
		{
			if (client.ClientStatus != SocketStatus.SOCKET_STATUS_CONNECTED &&
				client.ClientStatus != SocketStatus.SOCKET_STATUS_BROKEN_LOCALLY)
				WaitAndTryReconnect();


			Debug.Console(2, this, "Socket status change {0}", clientSocketStatus);
			switch (clientSocketStatus)
			{
				case SocketStatus.SOCKET_STATUS_BROKEN_LOCALLY:
					break;
				case SocketStatus.SOCKET_STATUS_BROKEN_REMOTELY:
					break;
				case SocketStatus.SOCKET_STATUS_CONNECTED:
					Client.ReceiveDataAsync(Receive);
					break;
				case SocketStatus.SOCKET_STATUS_CONNECT_FAILED:
					break;
				case SocketStatus.SOCKET_STATUS_DNS_FAILED:
					break;
				case SocketStatus.SOCKET_STATUS_DNS_LOOKUP:
					break;
				case SocketStatus.SOCKET_STATUS_DNS_RESOLVED:
					break;
				case SocketStatus.SOCKET_STATUS_LINK_LOST:
					break;
				case SocketStatus.SOCKET_STATUS_NO_CONNECT:
					break;
				case SocketStatus.SOCKET_STATUS_SOCKET_NOT_EXIST:
					break;
				case SocketStatus.SOCKET_STATUS_WAITING:
					break;
				default:
					break;
			}
		}
	}

	public class TcpIpConfig
	{
		[JsonProperty(Required = Required.Always)]
		public string Address { get; set; }

		[JsonProperty(Required = Required.Always)]
		public int Port { get; set; }

		/// <summary>
		/// Defaults to 32768
		/// </summary>
		public int BufferSize { get; set; }

		public TcpIpConfig()
		{
			BufferSize = 32768;
		}
	}

}