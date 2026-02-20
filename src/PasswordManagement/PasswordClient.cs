using System;
using Crestron.SimplSharp;

namespace PepperDash.Core.PasswordManagement
{
    /// <summary>
    /// A class to allow user interaction with the PasswordManager
    /// </summary>
	public class PasswordClient
	{
		/// <summary>
		/// Password selected key
		/// </summary>
		public string Username { get; set; }
		
		/// <summary>
		/// Password selected
		/// </summary>
		public string Password { get; set; }
		
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
			PasswordManager.Initialized += (sender, args) => Initialize();
			PasswordManager.PasswordChange += PasswordManager_PasswordChange;

			if (PasswordManager.IsInitialized)
				Initialize();
		}		

		/// <summary>
		/// Initialize method
		/// </summary>
		public void Initialize()
		{
			OnBoolChange(false, 0, PasswordConstants.Initialized);

			Username = string.Empty;
			Password = string.Empty;
			PasswordToValidate = string.Empty;

			OnUshrtChange((ushort)PasswordManager.Passwords.Count, 0, PasswordConstants.Count);
			OnUshrtChange(0, 0, PasswordConstants.PasswordLength);
			OnStringChange(string.Empty, 0, PasswordConstants.UsernameValidated);
			OnStringChange("Password client initilaized", 0, PasswordConstants.Message);
			OnBoolChange(true, 0, PasswordConstants.Initialized);
		}

		/// <summary>
		/// Sends clear/0 values to S+ wrapper
		/// </summary>
		public void ClearOutputs()
		{
			//OnBoolChange(false, 0, PasswordConstants.UsernameValidated);
			OnUshrtChange(0, 0, PasswordConstants.PasswordLength);
			OnStringChange(string.Empty, 0, PasswordConstants.UsernameValidated);
			OnStringChange(string.Empty, 0, PasswordConstants.Message);
		}

	    /// <summary>
	    /// Sends clear/0 values to S+ wrapper
	    /// </summary>
	    /// <param name="result"></param>
	    public void UpdateOutputs(PasswordValidationResult result)
	    {
			OnBoolChange(result.IsValid, 0, PasswordConstants.UsernameValidated);
		    OnUshrtChange((ushort)(string.IsNullOrEmpty(result.Password) ? 0 : result.Password.Length), 0, PasswordConstants.PasswordLength);
			OnStringChange(string.Empty, 0, PasswordConstants.UsernameValidated);
			OnStringChange(result.Message, 0, PasswordConstants.Message);
	    }

		/// <summary>
		/// Validate username
		/// </summary>
		/// <param name="username"></param>
	    public void ValidateUsername(string username)
	    {
			if (string.IsNullOrEmpty(username))
			{
				OnStringChange("Username is null or empty", 0, PasswordConstants.Message);
				return;
			}

			var result = PasswordManager.ValidateUsername(username);
			if (!result.IsValid)
			{
				UpdateOutputs(result);
				return;
			}

			Username = username;
			Password = result.Password;
			PasswordToValidate = string.Empty;
			
			OnStringChange(Username, 0, PasswordConstants.UsernameValidated); 
			OnBoolChange(result.IsValid, 0, PasswordConstants.UsernameValidated);						
			OnUshrtChange((ushort)result.Password.Length, 0, PasswordConstants.PasswordLength);
			OnStringChange(result.Message, 0, PasswordConstants.Message);
	    }

		/// <summary>
		/// Validate username and passowrd
		/// </summary>
		/// <param name="username"></param>
		/// <param name="password"></param>
		public void ValidateUsernameAndPassword(string username, string password)
		{
			if (string.IsNullOrEmpty(username))
			{
				OnStringChange("Username is null or empty", 0, PasswordConstants.Message);
				return;
			}

			if (string.IsNullOrEmpty(password))
			{
				OnStringChange("Password is null or empty", 0, PasswordConstants.Message);
				return;
			}

			var result = PasswordManager.ValidateUsernameAndPassword(username, password);
			if (!result.IsValid)
			{
				UpdateOutputs(result);
				return;
			}

			OnBoolChange(result.IsValid, 0, PasswordConstants.PasswordValidated);
			OnStringChange(result.Message, 0, PasswordConstants.Message);

			// Clear entered password and reset outputs after a delay (make configurable as needed)
			const long clearDelayMs = 5000;

			new CTimer(_ =>
			{
				ClearPassword();
				ClearOutputs();
			}, clearDelayMs);
		}

		/// <summary>
		/// Builds the user entered passwrod string, will attempt to validate the user entered
		/// password against the selected password when the length of the 2 are equal
		/// </summary>
		/// <param name="data"></param>
		public void BuildPassword(string data)
		{
			PasswordToValidate = String.Concat(PasswordToValidate, data);
			OnBoolChange(true, (ushort)PasswordToValidate.Length, PasswordConstants.LedFeedback);

			if (string.IsNullOrEmpty(Password))
			{
				OnStringChange("Cannot validate password, password is null or empty", 0, PasswordConstants.Message);
				return;
			}
			if (PasswordToValidate.Length != Password.Length) return;

			if (string.IsNullOrEmpty(Username))
			{
				OnStringChange("Cannot validate password, username is null or empty", 0, PasswordConstants.Message);
				return;
			}

			ValidateUsernameAndPassword(Username, PasswordToValidate);
		}

		/// <summary>
		/// Clears the user entered password and resets the LEDs
		/// </summary>
		public void ClearPassword()
		{
			PasswordToValidate = string.Empty;			
			OnBoolChange(true, (ushort)PasswordToValidate.Length, PasswordConstants.LedFeedback);
		}

		/// <summary>
		/// Deletes the last character in the currently entered password field
		/// </summary>
		public void DeletePasswordCharacter()
		{
			if (string.IsNullOrEmpty(PasswordToValidate))
				return;

			var previousLength = (ushort)PasswordToValidate.Length;

			// Remove last entered character
			PasswordToValidate = PasswordToValidate.Substring(0, PasswordToValidate.Length - 1);

			// Turn off the last LED that was on (old length index)
			OnBoolChange(false, previousLength, PasswordConstants.LedFeedback);

			// Send updated length back to S+
			OnUshrtChange((ushort)PasswordToValidate.Length, 0, PasswordConstants.PasswordLength);
		}

		#region event handlers

		/// <summary>
		/// Protected boolean change event handler
		/// </summary>
		/// <param name="state"></param>
		/// <param name="index"></param>
		/// <param name="type"></param>
		protected void OnBoolChange(bool state, ushort index, ushort type)
		{
			var handler = BoolChange;
			if (handler == null) return;

			var args = new BoolChangeEventArgs(state, type) {Index = index};
			BoolChange(this, args);
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
			if (handler == null) return;

			var args = new UshrtChangeEventArgs(value, type) {Index = index};
			UshrtChange(this, args);
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
			if (handler == null) return;

			var args = new StringChangeEventArgs(value, type) { Index = index };
			StringChange(this, args);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected void PasswordManager_Initialized(object sender, BoolChangeEventArgs args)
		{
			Initialize();
		}

		/// <summary>
		/// If password changes while selected change event will be notifed and update the client
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		protected void PasswordManager_PasswordChange(object sender, StringChangeEventArgs args)
		{
			if (Username == args.StringValue && args.Type == PasswordConstants.PasswordUpdated)
			{				
				// TODO - If the current username password changes, do something
			}
		}

		#endregion
	}
}