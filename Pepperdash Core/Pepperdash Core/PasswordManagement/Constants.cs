using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core.PasswordManagement
{
	/// <summary>
	/// Constants
	/// </summary>
	public class PasswordManagementConstants
	{
		/// <summary>
		/// Generic boolean value change constant
		/// </summary>
		public const ushort BoolValueChange = 1;
		/// <summary>
		/// Evaluated boolean change constant
		/// </summary>
		public const ushort BoolEvaluatedChange = 2;
		/// <summary>
		/// Password is valid change constant
		/// </summary>
		public const ushort PasswordIsValid = 3;
		/// <summary>
		/// Password is invalid change constant
		/// </summary>
		public const ushort PasswordIsInvalid = 4;
		/// <summary>
		/// Password LED change constant
		/// </summary>
		public const ushort PasswordLedChange = 5;

		/// <summary>
		/// Generic ushort value change constant
		/// </summary>
		public const ushort UshrtValueChange = 101;
		/// <summary>
		/// Password count
		/// </summary>
		public const ushort PasswordListCount = 102;
		/// <summary>
		/// Password length
		/// </summary>
		public const ushort SelectedPasswordLength = 103;
		/// <summary>
		/// Password to validate length
		/// </summary>
		public const ushort UserEnteredPasswordLength = 104;

		/// <summary>
		/// Generic string value change constant
		/// </summary>
		public const ushort StringValueChange = 201;
		/// <summary>
		/// Password key change constant
		/// </summary>
		public const ushort PasswordKey = 202;
		/// <summary>
		/// Password selected key change constant
		/// </summary>
		public const ushort PasswordKeySelected = 203;
	}
}