
namespace PepperDash.Core.PasswordManagement
{
	/// <summary>
	/// Constants
	/// </summary>
	public class PasswordConstants
	{
		/// <summary>
		/// Generic boolean value change constant
		/// </summary>
		public const ushort BoolValueChange = 1;
		
		/// <summary>
		/// Evaluated boolean change constant
		/// </summary>
		public const ushort Initialized = 2;
		
		/// <summary>
		/// Update busy change const
		/// </summary>
		public const ushort UpdateBusy = 3;

		/// <summary>
		/// Username is valid change constant
		/// </summary>
		public const ushort UsernameValidated = 4;

		/// <summary>
		/// Password is valid change constant
		/// </summary>
		public const ushort PasswordValidated = 5;
		
		/// <summary>
		/// Password LED change constant
		/// </summary>
		public const ushort LedFeedback = 6;



		/// <summary>
		/// Generic ushort value change constant
		/// </summary>
		public const ushort UshrtValueChange = 101;
		
		/// <summary>
		/// Password count
		/// </summary>
		public const ushort Count = 102;
		
		/// <summary>
		/// Password length
		/// </summary>
		public const ushort PasswordLength = 104;



		/// <summary>
		/// Generic string value change constant
		/// </summary>
		public const ushort StringValueChange = 201;

		/// <summary>
		/// Password message constant, used to send the password string to clients when a password is updated
		/// </summary>
		public const ushort Message = 202;

		/// <summary>
		/// Password updated 
		/// </summary>
		public const ushort PasswordUpdated = 203;
	}
}