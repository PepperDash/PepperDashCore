using System;
using System.Collections.Generic;
using Crestron.SimplSharp;

namespace PepperDash.Core.PasswordManagement
{
    /// <summary>
    /// Allows passwords to be stored and managed
    /// </summary>
	public class PasswordManager
	{
		/// <summary>
		/// Public dictionary of known passwords
		/// </summary>
		public static Dictionary<string, string> Passwords = new Dictionary<string, string>();

		/// <summary>
		/// Indicates whether the manager has been initialized
		/// </summary>
		public static bool IsInitialized { get; private set; }

		/// <summary>
		/// Tracks keys changed during the debounce window for notification
		/// </summary>
		private readonly List<string> _pendingChanges = new List<string>();

		/// <summary>
		/// Timer used to wait until password changes have stopped before updating the dictionary
		/// </summary>
		CTimer _passwordTimer;
		/// <summary>
		/// Timer length
		/// </summary>
		public long PasswordTimerElapsedMs = 5000;

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
		/// Event to notify clients of an updated password at the specified index (uint)
		/// </summary>
		public static event EventHandler<StringChangeEventArgs> PasswordChange;

		/// <summary>
		/// Event to notify clients when the manager has been initialized
		/// </summary>
		public static event EventHandler<BoolChangeEventArgs> Initialized;

		/// <summary>
		/// Constructor (empty constructor required by S+)
		/// </summary>
		public PasswordManager()
		{
		}

		/// <summary>
		/// Initialize password manager
		/// </summary>
		public void Initialize()
		{
			if (Passwords == null)
			{
				Passwords = new Dictionary<string, string>();
			}

			OnBoolChange(true, 0, PasswordConstants.Initialized);

			IsInitialized = true;

			var handler = Initialized;
			if (handler != null)
				handler(this, new BoolChangeEventArgs(true, PasswordConstants.Initialized));
		}

		/// <summary>
		/// Updates password stored in the dictonary
		/// </summary>
		/// <param name="key"></param>
		/// <param name="password"></param>
		public void UpdatePassword(string key, string password)
		{
			// validate the parameters
			if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(password))
			{
				Debug.Console(1, string.Format("PasswordManager.UpdatePassword: key [{0}] or password are not valid", key, password));
				return;
			}

			try
			{
				Passwords[key] = password;
				if (!_pendingChanges.Contains(key))
					_pendingChanges.Add(key);

				Debug.Console(1, string.Format("PasswordManager.UpdatePassword: Passwords[{0}] = {1}", key, Passwords[key]));

				if (_passwordTimer == null)
				{
					_passwordTimer = new CTimer((o) => PasswordTimerElapsed(), PasswordTimerElapsedMs);
					Debug.Console(1, string.Format("PasswordManager.UpdatePassword: CTimer Started"));
					OnBoolChange(true, 0, PasswordConstants.UpdateBusy);
				}
				else
				{
					_passwordTimer.Reset(PasswordTimerElapsedMs);
					Debug.Console(1, string.Format("PasswordManager.UpdatePassword: CTimer Reset"));
				}
			}
			catch (Exception e)
			{
				var msg = string.Format("PasswordManager.UpdatePassword key-value[{0}, {1}] failed:\r{2}", key, password, e);
				Debug.Console(1, msg);
			}
		}

		/// <summary>
		/// Helper method for debugging to see what passwords are in the lists
		/// </summary>
		public void ListEntries()
		{
			Debug.Console(0, "PasswordManager.ListEntries:\r");
			foreach (var pw in Passwords)
			{
				var maskedValue = string.IsNullOrEmpty(pw.Value) ? string.Empty : new string('*', pw.Value.Length);
				Debug.Console(0, "Passwords[{0}]: {1}\r", pw.Key, maskedValue);
			}
		}

		/// <summary>
		/// Validates the username against the known entries
		/// </summary>
		/// <param name="username"></param>
		public static PasswordValidationResult ValidateUsername(string username)
		{
			if (Passwords == null)
				return PasswordValidationResult.Failure("No entires defined, verify list is initilaized");

			if (string.IsNullOrEmpty(username))
				return PasswordValidationResult.Failure("Username or password is empty");

			string storedPassword;
			return !Passwords.TryGetValue(username, out storedPassword) 
				? PasswordValidationResult.Failure(string.Format("Username '{0}' not found", username)) 
				: PasswordValidationResult.Success("Valid username", storedPassword);
		}

		/// <summary>
		/// Validates the username password against the known entries
		/// </summary>
		/// <param name="username"></param>
		/// <param name="password"></param>
		public static PasswordValidationResult ValidateUsernameAndPassword(string username, string password)
		{
			if (Passwords == null)
				return PasswordValidationResult.Failure("No entires defined, verify list is initilaized");

			if (string.IsNullOrEmpty(username))
				return PasswordValidationResult.Failure("Username is empty or null");

			if(string.IsNullOrEmpty(password))
				return PasswordValidationResult.Failure("Password is empty or null");				

			string storedPassword;
			if (!Passwords.TryGetValue(username, out storedPassword))
				return PasswordValidationResult.Failure(string.Format("Username '{0}' not found", username));

			return !string.Equals(storedPassword, password, StringComparison.Ordinal) 
				? PasswordValidationResult.Failure("Invalid password, verify password and try again") 
				: PasswordValidationResult.Success("Valid credentials, login was successfull", storedPassword);
		}

		/// <summary>
		/// CTimer callback function
		/// </summary>
		private void PasswordTimerElapsed()
		{
			try
			{
				_passwordTimer.Stop();
				Debug.Console(1, string.Format("PasswordManager.PasswordTimerElapsed: CTimer Stopped"));
				OnBoolChange(false, 0, PasswordConstants.UpdateBusy);

				foreach (var key in _pendingChanges)
				{
					string value;
					if (!Passwords.TryGetValue(key, out value)) continue;

					Debug.Console(1, string.Format("PasswordManager.PasswordTimerElapsed: Notifying change for [{0}]", key));
					OnPasswordChange(key, 0, PasswordConstants.PasswordUpdated);
				}

				_pendingChanges.Clear();
				OnUshrtChange((ushort)Passwords.Count, 0, PasswordConstants.Count);
			}
			catch (Exception e)
			{
				var msg = string.Format("PasswordManager.PasswordTimerElapsed failed:\r{0}", e);
				Debug.Console(1, msg);
			}
		}

		/// <summary>
		/// Method to change the default timer value, (default 5000ms/5s)
		/// </summary>
		/// <param name="time"></param>
		public void PasswordTimerMs(ushort time)
		{
			PasswordTimerElapsedMs = Convert.ToInt64(time);
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

			var args = new StringChangeEventArgs(value, type) {Index = index};
			StringChange(this, args);
		}

		/// <summary>
		/// Protected password change event handler
		/// </summary>
		/// <param name="value"></param>
		/// <param name="index"></param>
		/// <param name="type"></param>
		protected void OnPasswordChange(string value, ushort index, ushort type)
		{
			var handler = PasswordChange;
			if (handler == null) return;

			var args = new StringChangeEventArgs(value, type) {Index = index};
			PasswordChange(this, args);
		}

		#endregion
	}
}