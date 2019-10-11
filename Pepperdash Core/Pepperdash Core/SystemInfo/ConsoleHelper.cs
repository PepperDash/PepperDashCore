using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core.SystemInfo
{
	/// <summary>
	/// Console helper class
	/// </summary>
	public class ConsoleHelper
	{
		/// <summary>
		/// Cosntructor
		/// </summary>
		public ConsoleHelper()
		{

		}		

		/// <summary>
		/// Parse console respopnse method
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="line"></param>
		/// <param name="startString"></param>
		/// <param name="endString"></param>
		/// <returns>console response</returns>
		public string ParseConsoleResponse(string response, string line, string startString, string endString)
		{
			if (string.IsNullOrEmpty(response) || string.IsNullOrEmpty(line) || string.IsNullOrEmpty(startString) || string.IsNullOrEmpty(endString))
				return "";
			
			try
			{
				var linePos = response.IndexOf(line);
				var startPos = response.IndexOf(startString, linePos) + startString.Length;
				var endPos = response.IndexOf(endString, startPos);
				response = response.Substring(startPos, endPos - startPos).Trim();
			}
			catch (Exception e)
			{
				var msg = string.Format("ConsoleHelper.ParseConsoleResponse failed:\r{0}", e);
				CrestronConsole.PrintLine(msg);
				ErrorLog.Error(msg);
			}

			return response;
		}
	}
}