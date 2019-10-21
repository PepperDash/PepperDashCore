using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core.PasswordManagement
{
	public class PasswordClient
	{
		/// <summary>
		/// Password Client
		/// </summary>
		public PasswordConfig Client { get; set; }
		/// <summary>
		/// Used to build the password entered by the user
		/// </summary>
		public string PasswordToValidate { get; set; }

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
		public PasswordClient()
		{

		}

		/// <summary>
		/// Initialize method
		/// </summary>
		/// <param name="key"></param>
		public void Initialize(string key)
		{
			OnBoolChange(false, 0, PasswordManagementConstants.BoolEvaluatedChange);

			Client = new PasswordConfig();
			PasswordToValidate = "";

			// there has to be a better way to get the index of the current index of password
			ushort i = 0;
			foreach (var password in PasswordManager.Passwords)
			{
				i++;
				OnUshrtChange((ushort)password.Key, (ushort)password.Key, PasswordManagementConstants.PasswordKey);
			}

			OnBoolChange(true, 0, PasswordManagementConstants.BoolEvaluatedChange);
		}

		/// <summary>
		/// Retrieves password by key
		/// </summary>
		/// <param name="key"></param>
		//public void GetPasswordByKey(string key)
		//{
		//    if (string.IsNullOrEmpty(key))
		//    {
		//        Debug.Console(1, "PassowrdClient.GetPasswordByKey failed:\rKey {0} is null or empty", key);
		//        return;
		//    }

		//    PasswordConfig password = PasswordManager.Passwords.FirstOrDefault(p => p.key.Equals(key));
		//    if (password == null)
		//    {
		//        OnUshrtChange(0, 0, PasswordManagementConstants.SelectedPasswordLength);
		//        return;
		//    }

		//    Client = password;
		//    OnUshrtChange((ushort)Client.password.Length, 0, PasswordManagementConstants.SelectedPasswordLength);
		//    OnStringChange(Client.key, 0, PasswordManagementConstants.PasswordKeySelected);
		//}

		/// <summary>
		/// Retrieve password by index
		/// </summary>
		/// <param name="index"></param>
		public void GetPasswordByIndex(ushort key)
		{
			PasswordConfig pw = PasswordManager.Passwords[key];
			if (pw == null)
			{
				OnUshrtChange(0, 0, PasswordManagementConstants.SelectedPasswordLength);
				return;
			}

			Client = pw;
			OnUshrtChange((ushort)Client.password.Length, 0, PasswordManagementConstants.SelectedPasswordLength);
			OnUshrtChange(key, 0, PasswordManagementConstants.PasswordKeySelected);
		}

		/// <summary>
		/// Password validation method
		/// </summary>
		/// <param name="password"></param>
		public void ValidatePassword(string password)
		{
			if (string.IsNullOrEmpty(password))
				return;

			if (string.Equals(Client.password, password))
			{
				OnBoolChange(true, 0, PasswordManagementConstants.PasswordIsValid);
			}
			else
			{
				OnBoolChange(true, 0, PasswordManagementConstants.PasswordIsInvalid);
			}


			OnBoolChange(false, 0, PasswordManagementConstants.PasswordIsValid);
			OnBoolChange(false, 0, PasswordManagementConstants.PasswordIsInvalid);

			ClearPassword();
		}

		/// <summary>
		/// Builds the user entered passwrod string, will attempt to validate the user entered
		/// password against the selected password when the length of the 2 are equal
		/// </summary>
		/// <param name="data"></param>
		public void BuildPassword(string data)
		{
			PasswordToValidate = String.Concat(PasswordToValidate, data);
			OnBoolChange(true, (ushort)PasswordToValidate.Length, PasswordManagementConstants.PasswordLedChange);

			if (PasswordToValidate.Length == Client.password.Length)
				ValidatePassword(PasswordToValidate);
		}

		/// <summary>
		/// Clears the user entered password and resets the LEDs
		/// </summary>
		public void ClearPassword()
		{
			PasswordToValidate = "";
			OnBoolChange(true, (ushort)PasswordToValidate.Length, PasswordManagementConstants.PasswordLedChange);

			for(var i = 1; i <= Client.password.Length; i++)
				OnBoolChange(false, (ushort)i, PasswordManagementConstants.PasswordLedChange);
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
		/// <param name="value"></param>
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