namespace PepperDash.Core.PasswordManagement
{
	/// <summary>
	/// Represents the result of a password validation attempt
	/// </summary>
	public class PasswordValidationResult
	{
		/// <summary>
		/// Indicates whether the validation was successful
		/// </summary>
		public bool IsValid { get; private set; }

		/// <summary>
		/// Password
		/// </summary>
		public string Password { get; private set; }

		/// <summary>
		/// Message describing the validation result
		/// </summary>
		public string Message { get; private set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="isValid">Whether the validation was successful</param>
		/// <param name="message">Message describing the result</param>
		/// <param name="password"></param>
		public PasswordValidationResult(bool isValid, string message, string password)
		{
			IsValid = isValid;
			Message = message;
			Password = password;
		}

		/// <summary>
		/// Creates a successful validation result
		/// </summary>
		/// <param name="message">The success message</param>
		public static PasswordValidationResult Success(string message, string password)
		{
			return new PasswordValidationResult(true, message, password);
		}

		/// <summary>
		/// Creates a failed validation result with the specified message
		/// </summary>
		/// <param name="message">The failure reason</param>
		public static PasswordValidationResult Failure(string message)
		{
			return new PasswordValidationResult(false, message, null);
		}
	}
}
