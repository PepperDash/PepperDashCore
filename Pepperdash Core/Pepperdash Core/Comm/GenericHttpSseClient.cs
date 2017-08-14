using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Net;
using Crestron.SimplSharp.Net.Http;

namespace PepperDash.Core
{
    public class GenericHttpSseClient : ICommunicationReceiver 
    {
        public event EventHandler<GenericCommMethodReceiveBytesArgs> BytesReceived;
        public event EventHandler<GenericCommMethodReceiveTextArgs> TextReceived;

        public bool IsConnected
        {
            get;
            private set;
        }

        public string Key
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public string Url { get; set; }

        HttpClient Client;
        HttpClientRequest Request;
        Connection SseConnection;
        

        public GenericHttpSseClient(string key, string name)
        {
            Key = key;
            Name = name;
        }

        public void Connect()
        {
            InitiateConnection(Url);
        }

        public void Disconnect()
        {
            CloseConnection(null);
        }

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
                    //CrestronConsole.PrintLine("Request made!");
                }
                catch (Exception e)
                {
                    ErrorLog.Notice("Exception occured in InitiateConnection(): " + e);
                }
            });
        }

        public void CloseConnection(string s)
        {
            try
            {
                if (Client != null)
                {
                    if (SseConnection != null)
                    {
                        // Gracefully dispose of any in use resources??

                        SseConnection.Disconnect();
                        SseConnection.Close(true);

                        Client.Abort();
                    }
                    IsConnected = false;

                    Debug.Console(1, this, "Client Disconnected");
                }
            }
            catch (Exception e)
            {
                Debug.Console(1, this, "Error Closing SSE Connection: {0}", e);
            }
        }

        private void GetRequestStreamCallback(HttpClientRequest request, HTTP_CALLBACK_ERROR error, object status)
        {
#warning Explore simplifying this later....

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
        /// <param name="asynchronousResult"></param>
        /// <param name="error"></param>
        /// <param name="status"></param>
        private void GetResponseStreamCallback(HttpClientRequest request, HTTP_CALLBACK_ERROR error, object status)
        {
            try
            {
                // This closes up the GetResponseStream async
                var response = request.ThisClient.EndGetResponseStream(request);

                SseConnection = response.DataConnection;                

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="asyncResult"></param>
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

                try
                {
                    Crestron.SimplSharp.CrestronIO.IAsyncResult asynchronousResult;
                    do
                    {
                            asynchronousResult = responseStream.BeginRead(requestState.BufferRead, 0, RequestState.BUFFER_SIZE,
                                new Crestron.SimplSharp.CrestronIO.AsyncCallback(ReadCallBack), requestState);                      
                    }
                    while (asynchronousResult.CompletedSynchronously && !requestState.Done);
                }
                catch (Exception e)
                {
                    Debug.Console(1, this, "Exception: {0}", e);
                }
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
        public const int BUFFER_SIZE = 10000;
        public byte[] BufferRead;
        public HttpClient HttpClient;
        public HttpClientRequest Request;
        public HttpClientResponse Response;
        public Stream StreamResponse;
        public bool Done;

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
        public CEvent wait_for_response = new CEvent(true, false);
    }
}