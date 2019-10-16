using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Crestron.SimplSharp;
using PepperDash.Core.JsonToSimpl;

namespace PepperDash.Core.PasswordManager
{
	public class PasswordManager : IKeyed
	{
		public string Key { get; set; }

		public List<PasswordConfig> Passwords { get; set; }

		/// <summary>
		/// Boolean event 
		/// </summary>
		public event EventHandler<BoolChangeEventArgs> BoolChange;
		/// <summary>
		/// Ushort event
		/// </summary>
		public event EventHandler<UshrtChangeEventArgs> UshrtChange;
		/// <summary>
		/// String event
		/// </summary>
		public event EventHandler<StringChangeEventArgs> StringChange;

		/// <summary>
		/// Constructor
		/// </summary>
		public PasswordManager()
		{

		}

		/// <summary>
		/// Initialize method
		/// </summary>
		/// <param name="key"></param>
		/// <param name="uniqueId"></param>
		public void Initialize(string uniqueId, string key)
		{
			OnBoolChange(false, 0, PasswordManagerConstants.BoolEvaluatedChange);

			try
			{
				if(string.IsNullOrEmpty(uniqueId) || string.IsNullOrEmpty(key))
				{
					Debug.Console(1, "PasswordManager.Initialize({0}, {1}) null or empty parameters", uniqueId, key);
					return;
				}

				Key = key;

				JsonToSimplMaster master = J2SGlobal.GetMasterByFile(uniqueId);
				if(master == null)
				{
					Debug.Console(1, "PassowrdManager.Initialize failed:\rCould not find JSON file with uniqueID {0}", uniqueId);
					return;
				}

				var passwords = master.JsonObject.ToObject<RootObject>().global.passwords;
				if(passwords == null)
				{
					Debug.Console(1, "PasswordManager.Initialize failed:\rCould not find password object");
					return;
				}

				foreach(var password in passwords)
				{
					AddPassword(password);
				}
			}
			catch(Exception e)
			{
				var msg = string.Format("PasswordManager.Initialize({0}, {1}) failed:\r{2}", uniqueId, Key, e.Message);
				CrestronConsole.PrintLine(msg);
				ErrorLog.Error(msg);
			}
			finally
			{
				OnBoolChange(true, 0, PasswordManagerConstants.BoolEvaluatedChange);
			}
		}

		/// <summary>
		///	Adds password to the list 
		/// </summary>
		/// <param name="password"></param>
		private void AddPassword(PasswordConfig password)
		{
			if (password == null)
				return;

			RemovePassword(password);
			Passwords.Add(password);
		}

		/// <summary>
		/// Removes password from the list
		/// </summary>
		/// <param name="password"></param>
		private void RemovePassword(PasswordConfig password)
		{
			if (password == null)
				return;

			var item = Passwords.FirstOrDefault(p => p.key.Equals(password.key));
			if (item != null)
				Passwords.Remove(item);
		}

		/// <summary>
		/// Protected boolean change event handler
		/// </summary>
		/// <param name="state"></param>
		/// <param name="index"></param>
		/// <param name="type"></param>
		protected void OnBoolChange(bool state, ushort index, ushort type)
		{
			var handler = BoolChange;
			if (handler != null)
			{
				var args = new BoolChangeEventArgs(state, type);
				args.Index = index;
				BoolChange(this, args);
			}
		}

		/// <summary>
		/// Protected ushort change event handler
		/// </summary>
		/// <param name="state"></param>
		/// <param name="index"></param>
		/// <param name="type"></param>
		protected void OnUshrtChange(ushort value, ushort index, ushort type)
		{
			var handler = UshrtChange;
			if (handler != null)
			{
				var args = new UshrtChangeEventArgs(value, type);
				args.Index = index;
				UshrtChange(this, args);
			}
		}

		/// <summary>
		/// Protected string change event handler
		/// </summary>
		/// <param name="value"></param>
		/// <param name="index"></param>
		/// <param name="type"></param>
		protected void OnStringChange(string value, ushort index, ushort type)
		{
			var handler = StringChange;
			if (handler != null)
			{
				var args = new StringChangeEventArgs(value, type);
				args.Index = index;
				StringChange(this, args);
			}
		}
	}
}