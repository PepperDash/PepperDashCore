using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PepperDash.Core
{
    /// <summary>
    /// An incoming communication stream
    /// </summary>
    public interface ICommunicationReceiver : IKeyed
    {
		/// <summary>
		/// Event handler for bytes received
		/// </summary>
        event EventHandler<GenericCommMethodReceiveBytesArgs> BytesReceived;
		/// <summary>
		/// Event hanlder for text recieved
		/// </summary>
        event EventHandler<GenericCommMethodReceiveTextArgs> TextReceived;
		/// <summary>
		/// Is connected property.
		/// </summary>
        bool IsConnected { get; }
		/// <summary>
		/// Connect method
		/// </summary>
        void Connect();
		/// <summary>
		/// Disconnect method
		/// </summary>
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

	/// <summary>
	/// Represents a device that can automatically reconnect when a connection drops.  Typically
	/// used with IP based communications.
	/// </summary>
	public interface IAutoReconnect
	{
		bool AutoReconnect { get; set; }
		int AutoReconnectIntervalMs { get; set; }
	}

	/// <summary>
	/// Generic communication method status change type Enum
	/// </summary>
	public enum eGenericCommMethodStatusChangeType
	{
		Connected, Disconnected
	}

	/// <summary>
	/// This delegate defines handler for IBasicCommunication status changes
	/// </summary>
	/// <param name="comm">Device firing the status change</param>
	/// <param name="status">A eGenericCommMethodStatusChangeType enum</param>
	public delegate void GenericCommMethodStatusHandler(IBasicCommunication comm, eGenericCommMethodStatusChangeType status);

	/// <summary>
	/// Generic communication method receive bytes args class
	/// </summary>
	public class GenericCommMethodReceiveBytesArgs : EventArgs
	{
		/// <summary>
		/// Bytes array property
		/// </summary>
		public byte[] Bytes { get; private set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bytes">An array of bytes</param>
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
	/// Generic communication method receive text args class
	/// </summary>
	/// <remarks>
	/// Inherits from <seealso cref="System.EventArgs"/>
	/// </remarks>
	public class GenericCommMethodReceiveTextArgs : EventArgs
	{
		/// <summary>
		/// Text string property
		/// </summary>
		public string Text { get; private set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="text">A string</param>
		public GenericCommMethodReceiveTextArgs(string text)
		{
			Text = text;
		}

		/// <summary>
		/// Stupid S+ Constructor
		/// </summary>
		public GenericCommMethodReceiveTextArgs() { }
	}



	/// <summary>
	/// Communication text helper class
	/// </summary>
	public class ComTextHelper
	{
		/// <summary>
		/// Method to convert escaped bytes to an array of escaped bytes
		/// </summary>
		/// <param name="bytes">An array of bytes</param>
		/// <returns>Array of escaped characters</returns>
		public static string GetEscapedText(byte[] bytes)
		{
			return String.Concat(bytes.Select(b => string.Format(@"[{0:X2}]", (int)b)).ToArray());
		}
		/// <summary>
		/// Method to coonvert a string to an array of escaped bytes
		/// </summary>
		/// <param name="text">A string</param>
		/// <returns>Array of escaped characters</returns>
		public static string GetEscapedText(string text)
		{
			var bytes = Encoding.GetEncoding(28591).GetBytes(text);
			return String.Concat(bytes.Select(b => string.Format(@"[{0:X2}]", (int)b)).ToArray());
		}
	}
}