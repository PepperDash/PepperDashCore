using Crestron.SimplSharp.WebScripting;

namespace PepperDash.Core.Web.RequestHandlers
{
	/// <summary>
	/// Web API default request handler
	/// </summary>
	public class DefaultRequestRequestHandler : WebApiBaseRequestHandler
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public DefaultRequestRequestHandler()
			: base(true)
		{ }
	}
}