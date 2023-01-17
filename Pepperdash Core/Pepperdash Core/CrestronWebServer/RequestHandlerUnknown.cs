using Crestron.SimplSharp.WebScripting;

namespace PepperDash.Core
{
	public class RequestHandlerUnknown : IHttpCwsHandler
	{
		public void ProcessRequest(HttpCwsContext context)
		{
			// TODO [ ] Modify unknown request handler 
			context.Response.StatusCode = 418;
			context.Response.StatusDescription = "I'm a teapot";			
			context.Response.ContentType = "application/json";			
			context.Response.Write(string.Format("{0} {1}", context.Request.HttpMethod, context.Request.RawUrl), true);
		}
	}
}