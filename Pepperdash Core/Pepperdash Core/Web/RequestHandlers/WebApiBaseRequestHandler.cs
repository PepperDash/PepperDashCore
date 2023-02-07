using System;
using System.Collections.Generic;
using Crestron.SimplSharp.WebScripting;

namespace PepperDash.Core.Web.RequestHandlers
{
	/// <summary>
	/// CWS Base Handler, implements IHttpCwsHandler
	/// </summary>
	public abstract class WebApiBaseRequestHandler : IHttpCwsHandler
	{
		private readonly Dictionary<string, Action<HttpCwsContext>> _handlers;

		/// <summary>
		/// Constructor
		/// </summary>
		protected WebApiBaseRequestHandler()
		{
			_handlers = new Dictionary<string, Action<HttpCwsContext>>
			{
				{"CONNECT", HandleConnect},
				{"DELETE", HandleDelete},
				{"GET", HandleGet},
				{"HEAD", HandleHead},
				{"OPTIONS", HandleOptions},
				{"PATCH", HandlePatch},
				{"POST", HandlePost},
				{"PUT", HandlePut},
				{"TRACE", HandleTrace}
			};
		}

		/// <summary>
		/// Handles CONNECT method requests
		/// </summary>
		/// <param name="context"></param>
		protected virtual void HandleConnect(HttpCwsContext context)
		{
			context.Response.StatusCode = 501;
			context.Response.StatusDescription = "Not Implemented";			
			context.Response.End();
		}

		/// <summary>
		/// Handles DELETE method requests
		/// </summary>
		/// <param name="context"></param>
		protected virtual void HandleDelete(HttpCwsContext context)
		{
			context.Response.StatusCode = 501;
			context.Response.StatusDescription = "Not Implemented";			
			context.Response.End();
		}

		/// <summary>
		/// Handles GET method requests
		/// </summary>
		/// <param name="context"></param>
		protected virtual void HandleGet(HttpCwsContext context)
		{
			context.Response.StatusCode = 501;
			context.Response.StatusDescription = "Not Implemented";
			context.Response.End();
		}

		/// <summary>
		/// Handles HEAD method requests
		/// </summary>
		/// <param name="context"></param>
		protected virtual void HandleHead(HttpCwsContext context)
		{
			context.Response.StatusCode = 501;
			context.Response.StatusDescription = "Not Implemented";
			context.Response.End();
		}

		/// <summary>
		/// Handles OPTIONS method requests
		/// </summary>
		/// <param name="context"></param>
		protected virtual void HandleOptions(HttpCwsContext context)
		{
			context.Response.StatusCode = 501;
			context.Response.StatusDescription = "Not Implemented";
			context.Response.End();
		}

		/// <summary>
		/// Handles PATCH method requests
		/// </summary>
		/// <param name="context"></param>
		protected virtual void HandlePatch(HttpCwsContext context)
		{
			context.Response.StatusCode = 501;
			context.Response.StatusDescription = "Not Implemented";
			context.Response.End();
		}

		/// <summary>
		/// Handles POST method requests
		/// </summary>
		/// <param name="context"></param>
		protected virtual void HandlePost(HttpCwsContext context)
		{
			context.Response.StatusCode = 501;
			context.Response.StatusDescription = "Not Implemented";
			context.Response.End();
		}

		/// <summary>
		/// Handles PUT method requests
		/// </summary>
		/// <param name="context"></param>
		protected virtual void HandlePut(HttpCwsContext context)
		{
			context.Response.StatusCode = 501;
			context.Response.StatusDescription = "Not Implemented";
			context.Response.End();
		}

		/// <summary>
		/// Handles TRACE method requests
		/// </summary>
		/// <param name="context"></param>
		protected virtual void HandleTrace(HttpCwsContext context)
		{
			context.Response.StatusCode = 501;
			context.Response.StatusDescription = "Not Implemented";
			context.Response.End();
		}

		/// <summary>
		/// Process request
		/// </summary>
		/// <param name="context"></param>
		public void ProcessRequest(HttpCwsContext context)
		{
			Action<HttpCwsContext> handler;

			if (!_handlers.TryGetValue(context.Request.HttpMethod, out handler))
			{
				return;
			}

			handler(context);
		}		
	}
}