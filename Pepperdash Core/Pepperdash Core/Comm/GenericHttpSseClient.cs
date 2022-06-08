using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Net.Http;

namespace PepperDash.Core
{
    /// <summary>
    /// Client for communicating with an HTTP Server Side Event pattern
    /// </summary>
    public class GenericHttpSseClient : ICommunicationReceiver 
    {
        /// <summary>
        /// Notifies when bytes have been received
        /// </summary>
        public event EventHandler<GenericCommMethodReceiveBytesArgs> BytesReceived;
        /// <summary>
        /// Notifies when text has been received
        /// </summary>
        public event EventHandler<GenericCommMethodReceiveTextArgs> TextReceived;

        /// <summary>
        /// Indicates connection status
        /// </summary>
        public bool IsConnected
        {
            get;
            private set;
        }

        /// <summary>
        /// Unique identifier for the instance
        /// </summary>
        public string Key
        {
            get;
            private set;
        }

        /// <summary>
        /// Name for the instance
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// URL of the server
        /// </summary>
        public string Url { get; set; }

        HttpClient Client;
        HttpClientRequest Request;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="name"></param>
        public GenericHttpSseClient(string key, string name)
        {
            Key = key;
            Name = name;
        }

        /// <summary>
        /// Connects to the server.  Requires Url to be set first.
        /// </summary>
        public void Connect()
        {
            InitiateConnection(Url);
        }

        /// <summary>
        /// Disconnects from the server
        /// </summary>
        public void Disconnect()
        {
            CloseConnection(null);
        }

        /// <summary>
        /// Initiates connection to the server
        /// </summary>
        /// <param name="url"></param>
        public void InitiateConnection(string url)
        {
            CrestronInvoke.BeginInvoke(o => 
            {
                try
                {
                    if(string.IsNullOrEmpty(url))
                    {
                        Debug.Console(0, this, "Error connecting to Server.  No URL specified");
                        return;
                    }

                    Client = new HttpClient();
                    Request = new HttpClientRequest();
                    Client.Verbose = true;
                    Client.KeepAlive = true;
                    Request.Url.Parse(url);
                    Request.RequestType = RequestType.Get;
                    Request.Header.SetHeaderValue("Accept", "text/event-stream");

                    // In order to get a handle on the response stream, we have to get
                    // the request stream first.  Boo
                    Client.BeginGetRequestStream(GetRequestStreamCallback, Request, null);
                    CrestronConsole.PrintLine("Request made!");
                }
                catch (Exception e)
                {
                    ErrorLog.Notice("Exception occured in AsyncWebPostHttps(): " + e.ToString());
                }
            });
        }

        /// <summary>
        /// Closes the connection to the server
        /// </summary>
        /// <param name="s"></param>
        public void CloseConnection(string s)
        {
            if (Client != null)
            {
                Client.Abort();
                IsConnected = false;

                Debug.Console(1, this, "Client Disconnected");
            }
        }

        private void GetRequestStreamCallback(HttpClientRequest request, HTTP_CALLBACK_ERROR error, object status)
        {

            try
            {
                // End the the async request operation and return the data stream
                Stream requestStream = request.ThisClient.EndGetRequestStream(request, null);
                // If this were something other than a GET we could write to the stream here

                // Closing makes the request happen
                requestStream.Close();

                // Get a handle on the response stream.
                request.ThisClient.BeginGetResponseStream(GetResponseStreamCallback, request, status);
            }
            catch (Exception e)
            {
                ErrorLog.Notice("Exception occured in GetSecureRequestStreamCallback(): " + e.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="error"></param>
        /// <param name="status"></param>
        private void GetResponseStreamCallback(HttpClientRequest request, HTTP_CALLBACK_ERROR error, object status)
        {
            try
            {
                // This closes up the GetResponseStream async
                var response = request.ThisClient.EndGetResponseStream(request);

                response.DataConnection.OnBytesReceived += new EventHandler(DataConnection_OnBytesReceived);

                IsConnected = true;

                Debug.Console(1, this, "Client Disconnected");

                Stream streamResponse = response.ContentStream;
                // Object containing various states to be passed back to async callback below
                RequestState asyncState = new RequestState();
                asyncState.Request = request;
                asyncState.Response = response;
                asyncState.StreamResponse = streamResponse;
                asyncState.HttpClient = request.ThisClient;

                // This processes the ongoing data stream
                Crestron.SimplSharp.CrestronIO.IAsyncResult asyncResult = null;
                do
                {
                    asyncResult = streamResponse.BeginRead(asyncState.BufferRead, 0, RequestState.BUFFER_SIZE, 
                        new Crestron.SimplSharp.CrestronIO.AsyncCallback(ReadCallBack), asyncState);
                }
                while (asyncResult.CompletedSynchronously && !asyncState.Done);

                //Console.WriteLine("\r\nExit Response Callback\r\n");
            }
            catch (Exception e)
            {
                ErrorLog.Notice("Exception occured in GetSecureRequestStreamCallback(): " + e.ToString());
            }
        }

        void DataConnection_OnBytesReceived(object sender, EventArgs e)
        {
            Debug.Console(1, this, "DataConnection OnBytesReceived Fired");
        }

        private void ReadCallBack(Crestron.SimplSharp.CrestronIO.IAsyncResult asyncResult)
        {
            //we are getting back everything here, so cast the state from the call
            RequestState requestState = asyncResult.AsyncState as RequestState;
            Stream responseStream = requestState.StreamResponse;

            int read = responseStream.EndRead(asyncResult);
            // Read the HTML page and then print it to the console. 
            if (read > 0)
            {
                var bytes = requestState.BufferRead;

                var bytesHandler = BytesReceived;
                if (bytesHandler != null)
                    bytesHandler(this, new GenericCommMethodReceiveBytesArgs(bytes));
                var textHandler = TextReceived;
                if (textHandler != null)
                {
                    var str = Encoding.GetEncoding(28591).GetString(bytes, 0, bytes.Length);
                    textHandler(this, new GenericCommMethodReceiveTextArgs(str));
                }
                           
                //requestState.RequestData.Append(Encoding.ASCII.GetString(requestState.BufferRead, 0, read));
                //CrestronConsole.PrintLine(requestState.RequestData.ToString());

                //clear the byte array buffer used.
                Array.Clear(requestState.BufferRead, 0, requestState.BufferRead.Length);

                if (asyncResult.CompletedSynchronously)
                {
                    return;
                }

                Crestron.SimplSharp.CrestronIO.IAsyncResult asynchronousResult;
                do
                {
                    asynchronousResult = responseStream.BeginRead(requestState.BufferRead, 0, RequestState.BUFFER_SIZE, 
                        new Crestron.SimplSharp.CrestronIO.AsyncCallback(ReadCallBack), requestState);
                }
                while (asynchronousResult.CompletedSynchronously && !requestState.Done);
            }
            else
            {
                requestState.Done = true;
            }
        }
    }

    /// <summary>
    /// Stores the state of the request
    /// </summary>
    public class RequestState
    {
        /// <summary>
        /// 
        /// </summary>
        public const int BUFFER_SIZE = 10000;
        /// <summary>
        /// 
        /// </summary>
        public byte[] BufferRead;
        /// <summary>
        /// 
        /// </summary>
        public HttpClient HttpClient;
        /// <summary>
        /// 
        /// </summary>
        public HttpClientRequest Request;
        /// <summary>
        /// 
        /// </summary>
        public HttpClientResponse Response;
        /// <summary>
        /// 
        /// </summary>
        public Stream StreamResponse;
        /// <summary>
        /// 
        /// </summary>
        public bool Done;

        /// <summary>
        /// Constructor
        /// </summary>
        public RequestState()
        {
            BufferRead = new byte[BUFFER_SIZE];
            HttpClient = null;
            Request = null;
            Response = null;
            StreamResponse = null;
            Done = false;
        }
    }

    /// <summary>
    /// Waithandle for main thread.
    /// </summary>
    public class StreamAsyncTest
    {
        /// <summary>
        /// 
        /// </summary>
        public CEvent wait_for_response = new CEvent(true, false);
    }
}