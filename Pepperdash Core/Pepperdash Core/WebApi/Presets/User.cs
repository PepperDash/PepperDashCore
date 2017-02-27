using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core.WebApi.Presets
{
	/// <summary>
	/// 
	/// </summary>
	public class User
	{
		public int Id { get; set; }
		
		public string ExternalId { get; set; }

		public string FirstName { get; set; }

		public string LastName { get; set; }
	}


	/// <summary>
	/// 
	/// </summary>
	public class UserReceivedEventArgs : EventArgs
	{
        /// <summary>
        /// True when user is found
        /// </summary>
        public bool LookupSuccess { get; private set; }

        /// <summary>
        /// For stupid S+
        /// </summary>
        public ushort ULookupSuccess { get { return (ushort)(LookupSuccess ? 1 : 0); } }

		public User User { get; private set; }

		/// <summary>
		/// For Simpl+
		/// </summary>
		public UserReceivedEventArgs() { }

		public UserReceivedEventArgs(User user, bool success)
		{
            LookupSuccess = success;
			User = user;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class UserAndRoomMessage
	{
		public int UserId { get; set; }

		public int RoomTypeId { get; set; }

		public int PresetNumber { get; set; }
	}
}