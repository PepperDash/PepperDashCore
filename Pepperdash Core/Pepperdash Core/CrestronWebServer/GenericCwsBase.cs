using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.WebScripting;

namespace PepperDash.Core
{
	public class GenericCwsBase : Device
	{
		private const string SplusKey = "Uninitialized CWS Server";
		private const string DefaultBasePath = "/api";

		private const uint DebugTrace = 0;
		private const uint DebugInfo = 1;
		private const uint DebugVerbose = 2;

		private HttpCwsServer _server;
		private readonly CCriticalSection _serverLock = new CCriticalSection();

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
		public GenericCwsBase()
			: base(SplusKey)
		{
			CrestronEnvironment.ProgramStatusEventHandler += CrestronEnvironment_ProgramStatusEventHandler;
			CrestronEnvironment.EthernetEventHandler += CrestronEnvironment_EthernetEventHandler;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="key"></param>
		/// <param name="basePath"></param>
		public GenericCwsBase(string key, string basePath)
			: base(key)
		{

			BasePath = string.IsNullOrEmpty(basePath) ? DefaultBasePath : basePath;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="key"></param>
		/// <param name="name"></param>
		/// <param name="basePath"></param>
		public GenericCwsBase(string key, string name, string basePath)
			: base(key, name)
		{

			BasePath = string.IsNullOrEmpty(basePath) ? DefaultBasePath : basePath;
		}

		/// <summary>
		/// Program status event handler
		/// </summary>
		/// <param name="programEventType"></param>
		void CrestronEnvironment_ProgramStatusEventHandler(eProgramStatusEventType programEventType)
		{
			if (programEventType != eProgramStatusEventType.Stopping)
				return;

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
			if (ethernetEventArgs.EthernetEventType == eEthernetEventType.LinkUp
				&& IsRegistered)
			{
				Debug.Console(DebugInfo, this, "Ethernet link up. Starting server");

				Start();
			}
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
					HttpRequestHandler = new RequestHandlerUnknown()
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
					Debug.Console(DebugInfo, this, "Servier has already been stopped");
					return;
				}

				if (_server.Unregister())
				{
					IsRegistered = false;
				}

				Dispose(true);				
			}
			catch (Exception ex)
			{
				Debug.Console(DebugInfo, this, "ServerStop Exception Message: {0}", ex.Message);
				Debug.Console(DebugVerbose, this, "ServerStop Exception StackTrace: {0}", ex.StackTrace);
				if (ex.InnerException != null)
					Debug.Console(DebugVerbose, this, "ServerStop Exception InnerException: {0}", ex.InnerException);
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
				// TODO [ ] Add logic for received requests
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

		/// <summary>
		/// Tracks if CWS is disposed
		/// </summary>
		public bool Disposed
		{
			get
			{
				return (_server == null); 
			}
		}

		/// <summary>
		/// Disposes CWS instance
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			CrestronEnvironment.GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposes CWS instance
		/// </summary>
		/// <param name="disposing"></param>
		protected void Dispose(bool disposing)
		{
			if (Disposed)
			{
				Debug.Console(DebugInfo, this, "Server has already been disposed");
				return;
			}

			if (!disposing) return;

			if (_server != null)
			{
				_server.Dispose();
				_server = null;
			}
		}

		~GenericCwsBase()
		{
			Dispose(true);
		}
	}
}