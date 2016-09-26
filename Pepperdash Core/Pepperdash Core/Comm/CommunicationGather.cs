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
				ReceiveBuffer.Append(args.Text);
				var str = ReceiveBuffer.ToString();
				var lines = str.Split(Delimiter);
				if (lines.Length > 0)
				{
					for (int i = 0; i < lines.Length - 1; i++)
						handler(this, new GenericCommMethodReceiveTextArgs(lines[i]));
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

				ReceiveBuffer.Append(args.Text);
				var str = ReceiveBuffer.ToString();
				var lines = Regex.Split(str, StringDelimiter);
				if (lines.Length > 1)
				{
					for (int i = 0; i < lines.Length - 1; i++)
						handler(this, new GenericCommMethodReceiveTextArgs(lines[i]));
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