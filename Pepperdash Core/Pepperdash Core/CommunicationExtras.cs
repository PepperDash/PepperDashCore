using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PepperDash.Core
{
    /// <summary>
    /// An incoming communication stream
    /// </summary>
    public interface ICommunicationReceiver : IKeyed
    {
        event EventHandler<GenericCommMethodReceiveBytesArgs> BytesReceived;
        event EventHandler<GenericCommMethodReceiveTextArgs> TextReceived;

        bool IsConnected { get; }
        void Connect();
        void Disconnect();
    }

	/// <summary>
	/// Represents a device that uses basic connection
	/// </summary>
    public interface IBasicCommunication : ICommunicationReceiver
	{
		void SendText(string text);
		void SendBytes(byte[] bytes);
	}

    /// <summary>
    /// Represents a device that implements IBasicCommunication and IStreamDebugging
    /// </summary>
    public interface IBasicCommunicationWithStreamDebugging : IBasicCommunication, IStreamDebugging
    {

    }

    /// <summary>
    /// Represents a device with stream debugging capablities
    /// </summary>
    public interface IStreamDebugging
    {
        CommunicationStreamDebugging StreamDebugging { get; }
    }

	/// <summary>
	/// For IBasicCommunication classes that have SocketStatus. GenericSshClient,
	/// GenericTcpIpClient
	/// </summary>
	public interface ISocketStatus : IBasicCommunication
	{
		event EventHandler<GenericSocketStatusChageEventArgs> ConnectionChange;
		SocketStatus ClientStatus { get; }
	}

    /// <summary>
    /// Represents a device that implements ISocketStatus and IStreamDebugging
    /// </summary>
    public interface ISocketStatusWithStreamDebugging : ISocketStatus, IStreamDebugging
    {

    }

	public interface IAutoReconnect
	{
		bool AutoReconnect { get; set; }
		int AutoReconnectIntervalMs { get; set; }
	}

	/// <summary>
	/// 
	/// </summary>
	public enum eGenericCommMethodStatusChangeType
	{
		Connected, Disconnected
	}

	/// <summary>
	/// This delegate defines handler for IBasicCommunication status changes
	/// </summary>
	/// <param name="comm">Device firing the status change</param>
	/// <param name="status"></param>
	public delegate void GenericCommMethodStatusHandler(IBasicCommunication comm, eGenericCommMethodStatusChangeType status);

	/// <summary>
	/// 
	/// </summary>
	public class GenericCommMethodReceiveBytesArgs : EventArgs
	{
		public byte[] Bytes { get; private set; }
		public GenericCommMethodReceiveBytesArgs(byte[] bytes)
		{
			Bytes = bytes;
		}

		/// <summary>
		/// Stupid S+ Constructor
		/// </summary>
		public GenericCommMethodReceiveBytesArgs() { }
	}

	/// <summary>
	/// 
	/// </summary>
	public class GenericCommMethodReceiveTextArgs : EventArgs
	{
		public string Text { get; private set; }
        public string Delimiter { get; private set; }
		public GenericCommMethodReceiveTextArgs(string text)
		{
			Text = text;
		}

        public GenericCommMethodReceiveTextArgs(string text, string delimiter)
            :this(text)
        {
            Delimiter = delimiter;
        }

		/// <summary>
		/// Stupid S+ Constructor
		/// </summary>
		public GenericCommMethodReceiveTextArgs() { }
	}



	/// <summary>
	/// 
	/// </summary>
	public class ComTextHelper
	{
		public static string GetEscapedText(byte[] bytes)
		{
			return String.Concat(bytes.Select(b => string.Format(@"[{0:X2}]", (int)b)).ToArray());
		}

		public static string GetEscapedText(string text)
		{
			var bytes = Encoding.GetEncoding(28591).GetBytes(text);
			return String.Concat(bytes.Select(b => string.Format(@"[{0:X2}]", (int)b)).ToArray());
		}

        public static string GetDebugText(string text)
        {
            return Regex.Replace(text, @"[^\u0020-\u007E]", a => GetEscapedText(a.Value));
        }
	}
}