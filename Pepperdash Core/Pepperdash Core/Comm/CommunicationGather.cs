using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;

using PepperDash.Core;


namespace PepperDash.Core
{
	/// <summary>
	/// Defines the string event handler for line events on the gather
	/// </summary>
	/// <param name="text"></param>
	public delegate void LineReceivedHandler(string text);

	/// <summary>
	/// Attaches to IBasicCommunication as a text gather
	/// </summary>
	public class CommunicationGather
	{
		/// <summary>
		/// Event that fires when a line is received from the IBasicCommunication source.
		/// The event merely contains the text, not an EventArgs type class.
		/// </summary>
		public event EventHandler<GenericCommMethodReceiveTextArgs> LineReceived;

		/// <summary>
		/// The communication port that this gathers on
		/// </summary>
		public IBasicCommunication Port { get; private set; }

		/// <summary>
		/// Default false. If true, the delimiter will be included in the line output
		/// events
		/// </summary>
		public bool IncludeDelimiter { get; set; }

		/// <summary>
		///	For receive buffer
		/// </summary>
		StringBuilder ReceiveBuffer = new StringBuilder();

		/// <summary>
		/// Delimiter, like it says!
		/// </summary>
		char Delimiter;

		string StringDelimiter;

		/// <summary>
		/// Fires up a gather, given a IBasicCommunicaion port and char for de
		/// </summary>
		/// <param name="port"></param>
		/// <param name="delimiter"></param>
		public CommunicationGather(IBasicCommunication port, char delimiter)
		{
			Port = port;
			Delimiter = delimiter;
			port.TextReceived += new EventHandler<GenericCommMethodReceiveTextArgs>(Port_TextReceived);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="port"></param>
		/// <param name="delimiter"></param>
		public CommunicationGather(IBasicCommunication port, string delimiter)
		{
			Port = port;
			StringDelimiter = delimiter;
			port.TextReceived += Port_TextReceivedStringDelimiter;
		}

		/// <summary>
		/// Disconnects this gather from the Port's TextReceived event. This will not fire LineReceived
		/// after the this call.
		/// </summary>
		public void Stop()
		{
			Port.TextReceived -= Port_TextReceived;
			Port.TextReceived -= Port_TextReceivedStringDelimiter;
		}

		/// <summary>
		/// Handler for raw data coming from port 
		/// </summary>
		void Port_TextReceived(object sender, GenericCommMethodReceiveTextArgs args)
		{
			var handler = LineReceived;
			if (handler != null)
			{
                if (Debug.Level >= 2)
                {
                    var output = Regex.Replace(args.Text,
                          @"\p{Cc}",
                          a => string.Format("[{0:X2}]", (byte)a.Value[0]));
                    Debug.Console(2, Port, "RX: '{0}", output);
                }
				ReceiveBuffer.Append(args.Text);
				var str = ReceiveBuffer.ToString();
				var lines = str.Split(Delimiter);
				if (lines.Length > 0)
				{
					for (int i = 0; i < lines.Length - 1; i++)
					{
						string strToSend = null;
						if (IncludeDelimiter)
							strToSend = lines[i] + Delimiter;
						else
							strToSend = lines[i];
						handler(this, new GenericCommMethodReceiveTextArgs(strToSend));
					}
					ReceiveBuffer = new StringBuilder(lines[lines.Length - 1]);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		void Port_TextReceivedStringDelimiter(object sender, GenericCommMethodReceiveTextArgs args)
		{
			var handler = LineReceived;
			if (handler != null)
			{
				// Receive buffer should either be empty or not contain the delimiter
				// If the line does not have a delimiter, append the 
                if (Debug.Level >= 2)
                {
                    var output = Regex.Replace(args.Text,
                          @"\p{Cc}",
                          a => string.Format("[{0:X2}]", (byte)a.Value[0]));
                    Debug.Console(2, Port, "RX: '{0}'", output);
                }
				ReceiveBuffer.Append(args.Text);
				var str = ReceiveBuffer.ToString();

                // Case: Receiving DEVICE get version\x0d\0x0a+OK "value":"1234"\x0d\x0a

                // RX: DEV
                //  Split: (1) "DEV"
                // RX: I
                //  Split: (1) "DEVI"
                // RX: CE get version
                //  Split: (1) "DEVICE get version"
                // RX: \x0d\x0a+OK "value":"1234"\x0d\x0a
                //  Split: (2) DEVICE get version, +OK "value":"1234"

				var lines = Regex.Split(str, StringDelimiter);
				if (lines.Length > 1)
				{
					for (int i = 0; i < lines.Length - 1; i++)
					{
						string strToSend = null;
						if (IncludeDelimiter)
							strToSend = lines[i] + StringDelimiter;
						else
							strToSend = lines[i];
						handler(this, new GenericCommMethodReceiveTextArgs(strToSend));
					}
					ReceiveBuffer = new StringBuilder(lines[lines.Length - 1]);
				}
			}
		}

		/// <summary>
		/// Deconstructor.  Disconnects from port TextReceived events.
		/// </summary>
		~CommunicationGather()
		{
			Stop();
		}
	}
}