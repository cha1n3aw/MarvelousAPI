using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MarvelousAPI
{
    class Network
    {
        #region Public
        public delegate void UdpDataReceivedHandler(object sender, DataReceivedEventArgs e);
        public event UdpDataReceivedHandler OnDataReceived;
        public ConcurrentDictionary<IPEndPoint, Pipe> EndpointsPipes;
        public bool AllowDebug = false;
        #endregion
        #region Private
        private bool Run = false;
        private UdpClient sender;
        #endregion

        #region Private
        private async Task StartUdpListener(int port)
        {
            EndpointsPipes = new();                                                     //Use a Dictionary to match packets from given connections to give Pipes
            sender = new(new IPEndPoint(IPAddress.Any, port));
            while (Run)
            {
                UdpReceiveResult result = await sender.ReceiveAsync();                  //Wait for some data to arrive
                if (EndpointsPipes.ContainsKey(result.RemoteEndPoint))                  //If we have seen this IPEndpoint before send the traffic to the pipe
                {
                    EndpointsPipes.TryGetValue(result.RemoteEndPoint, out Pipe pipe);      
                    await pipe.Writer.WriteAsync(result.Buffer);                        //the task associated with that Pipe will pick the traffic up
                }
                else                                                                    //If we have not seen it, make the pipe, stick the data in the pipe
                {
                    Pipe pipe = new();
                    EndpointsPipes.TryAdd(result.RemoteEndPoint, pipe);                    
                    await pipe.Writer.WriteAsync(result.Buffer);                    
                    _ = Task.Run(() => ProcessUdpPipes(result.RemoteEndPoint, pipe));   // and spin up a task to Read/Process the data
                }
            }
        }

        private async Task ProcessUdpPipes(IPEndPoint endPoint, Pipe pipe)
        {
            while (Run)
            {
                ReadResult readResult = await pipe.Reader.ReadAsync();
                string data = Encoding.ASCII.GetString(readResult.Buffer.FirstSpan.ToArray());
                OnDataReceived?.Invoke(this, new DataReceivedEventArgs(data, endPoint));
                pipe.Reader.AdvanceTo(readResult.Buffer.End);
            }
        }
        #endregion
        #region Public
        public void Send(string data, IPEndPoint endPoint)
        {
            if (Run)
            {
                try
                {
                    byte[] send_buffer = Encoding.ASCII.GetBytes(data);
                    sender.Send(send_buffer, data.Length, endPoint);
                }
                catch (Exception ex)
                {
                    DebugWriteLine(ex.Message);
                }
            }
        }

        public void DebugWriteLine(string data)
        {
            if (AllowDebug) Console.WriteLine(data);
        }
        public void DebugWriteLine()
        {
            if (AllowDebug) Console.WriteLine();
        }

        public void DebugWrite(string data)
        {
            if (AllowDebug) Console.Write(data);
        }

        public class DataReceivedEventArgs : EventArgs
        {
            public string Message { get; private set; }
            public IPEndPoint RemoteIP { get; private set; }
            public DataReceivedEventArgs(string message, IPEndPoint remoteIp)
            {
                Message = message;
                RemoteIP = remoteIp;
            }
        }

        public void Start(int remotePort)
        {
            Run = true;
            _ = Task.Run(() => StartUdpListener(remotePort));
        }

        public void Stop()
        {
            Run = false;
            sender.Close();
            sender.Dispose();
            GC.Collect();
        }

        public Network()
        {

        }
        #endregion
    }
}