using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.WebScripting;
using PepperDash.Core.Web.RequestHandlers;

namespace PepperDash.Core.Web
{
	/// <summary>
	/// Web API server
	/// </summary>
	public class WebApiServer : IKeyName
	{
		private const string SplusKey = "Uninitialized Web API Server";
		private const string DefaultName = "Web API Server";
		private const string DefaultBasePath = "/api";

		private const uint DebugTrace = 0;
		private const uint DebugInfo = 1;
		private const uint DebugVerbose = 2;

		private HttpCwsServer _server;
		private readonly CCriticalSection _serverLock = new CCriticalSection();

		/// <summary>
		/// Web API server key
		/// </summary>
		public string Key { get; private set; }

		/// <summary>
		/// Web API server name
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// CWS base path, will default to "/api" if not set via initialize method
		/// </summary>
		public string BasePath { get; private set; }

		/// <summary>
		/// Indicates CWS is registered with base path
		/// </summary>
		public bool IsRegistered { get; private set; }

		/// <summary>
		/// Constructor for S+.  Make sure to set necessary properties using init method
		/// </summary>
		public WebApiServer()
			: this(SplusKey, DefaultName, null)
		{			
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="key"></param>
		/// <param name="basePath"></param>
		public WebApiServer(string key, string basePath)
			: this(key, DefaultName, basePath)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="key"></param>
		/// <param name="name"></param>
		/// <param name="basePath"></param>
		public WebApiServer(string key, string name, string basePath)
		{
			Key = key;
			Name = string.IsNullOrEmpty(name) ? DefaultName : name;
			BasePath = string.IsNullOrEmpty(basePath) ? DefaultBasePath : basePath;

			CrestronEnvironment.ProgramStatusEventHandler += CrestronEnvironment_ProgramStatusEventHandler;
			CrestronEnvironment.EthernetEventHandler += CrestronEnvironment_EthernetEventHandler;
		}

		/// <summary>
		/// Program status event handler
		/// </summary>
		/// <param name="programEventType"></param>
		void CrestronEnvironment_ProgramStatusEventHandler(eProgramStatusEventType programEventType)
		{
			if (programEventType != eProgramStatusEventType.Stopping) return;

			Debug.Console(DebugInfo, this, "Program stopping. Disabling Server");

			Stop();
		}

		/// <summary>
		/// Ethernet event handler
		/// </summary>
		/// <param name="ethernetEventArgs"></param>
		void CrestronEnvironment_EthernetEventHandler(EthernetEventArgs ethernetEventArgs)
		{
			// Re-enable the server if the link comes back up and the status should be connected
			if (ethernetEventArgs.EthernetEventType == eEthernetEventType.LinkUp && IsRegistered)
			{
				Debug.Console(DebugInfo, this, "Ethernet link up. Server is alreedy registered.");
				return;
			}

			Debug.Console(DebugInfo, this, "Ethernet link up. Starting server");

			Start();
		}

		/// <summary>
		/// Initializes CWS class
		/// </summary>
		public void Initialize(string key, string basePath)
		{
			Key = key;
			BasePath = string.IsNullOrEmpty(basePath) ? DefaultBasePath : basePath;			
		}

		/// <summary>
		/// Adds a route to CWS
		/// </summary>
		public void AddRoute(HttpCwsRoute route)
		{
			if (route == null)
			{
				Debug.Console(DebugInfo, this, "Failed to add route, route parameter is null");
				return;
			}

			_server.Routes.Add(route);
			
		}

		/// <summary>
		/// Removes a route from CWS
		/// </summary>
		/// <param name="route"></param>
		public void RemoveRoute(HttpCwsRoute route)
		{
			if (route == null)
			{
				Debug.Console(DebugInfo, this, "Failed to remote route, orute parameter is null");
				return;
			}

			_server.Routes.Remove(route);
		}

		/// <summary>
		/// Starts CWS instance
		/// </summary>
		public void Start()
		{
			try
			{
				_serverLock.Enter();

				if (_server != null)
				{
					Debug.Console(DebugInfo, this, "Server has already been started");
					return;
				}

				Debug.Console(DebugInfo, this, "Starting server");

				_server = new HttpCwsServer(BasePath)
				{
					HttpRequestHandler = new DefaultRequestRequestHandler()
				};

				IsRegistered = _server.Register();
			}
			catch (Exception ex)
			{
				Debug.Console(DebugInfo, this, "Start Exception Message: {0}", ex.Message);
				Debug.Console(DebugVerbose, this, "Start Exception StackTrace: {0}", ex.StackTrace);
				if (ex.InnerException != null)
					Debug.Console(DebugVerbose, this, "Start Exception InnerException: {0}", ex.InnerException);
			}
			finally
			{
				_serverLock.Leave();
			}
		}

		/// <summary>
		/// Stop CWS instance
		/// </summary>
		public void Stop()
		{
			try
			{
				_serverLock.Enter();

				if (_server == null)
				{
					Debug.Console(DebugInfo, this, "Server has already been stopped");
					return;
				}

				IsRegistered = _server.Unregister() == false;
				_server.Dispose();
				_server = null;				
			}
			catch (Exception ex)
			{
				Debug.Console(DebugInfo, this, "Server Stop Exception Message: {0}", ex.Message);
				Debug.Console(DebugVerbose, this, "Server Stop Exception StackTrace: {0}", ex.StackTrace);
				if (ex.InnerException != null)
					Debug.Console(DebugVerbose, this, "Server Stop Exception InnerException: {0}", ex.InnerException);
			}
			finally
			{
				_serverLock.Leave();
			}
		}		

		/// <summary>
		/// Received request handler
		/// </summary>
		/// <remarks>
		/// This is here for development and testing
		/// </remarks>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public void ReceivedRequestEventHandler(object sender, HttpCwsRequestEventArgs args)
		{
			try
			{
				Debug.Console(DebugInfo, this, @"RecieveRequestEventHandler 
Method: {0}
Path: {1}
PathInfo: {2}
PhysicalPath: {3}
ContentType: {4}
RawUrl: {5}
Url: {6}
UserAgent: {7}
UserHostAddress: {8}
UserHostName: {9}",
	args.Context.Request.HttpMethod,
	args.Context.Request.Path,
	args.Context.Request.PathInfo,
	args.Context.Request.PhysicalPath,
	args.Context.Request.ContentType,
	args.Context.Request.RawUrl,
	args.Context.Request.Url,
	args.Context.Request.UserAgent,
	args.Context.Request.UserHostAddress,
	args.Context.Request.UserHostName);
			}
			catch (Exception ex)
			{
				Debug.Console(DebugInfo, this, "ReceivedRequestEventHandler Exception Message: {0}", ex.Message);
				Debug.Console(DebugVerbose, this, "ReceivedRequestEventHandler Exception StackTrace: {0}", ex.StackTrace);
				if (ex.InnerException != null)
					Debug.Console(DebugVerbose, this, "ReceivedRequestEventHandler Exception InnerException: {0}", ex.InnerException);
			}
		}		
	}
}