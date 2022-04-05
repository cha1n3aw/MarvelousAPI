using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MarvelousAPI
{
    class Network
    {
        #region Public
        public delegate void UdpDataReceivedHandler(object sender, DataReceivedEventArgs e);
        public event UdpDataReceivedHandler OnDataReceived;
        public bool AllowDebug = false;
        #endregion
        #region Private
        private string RemoteAddress = string.Empty;
        private int RemotePort = 0;
        private int LocalPort = 0;
        private bool Run = false;
        private Thread ReceiveThread;
        private UdpClient sender;
        #endregion

        #region Private
        private void ReceiveData()
        {
            UdpClient receiver = new(LocalPort);
            IPEndPoint remoteIp = null;
            while (Run)
            {
                try
                {
                    byte[] data = receiver.Receive(ref remoteIp);
                    string message = Encoding.Default.GetString(data);
                    if (OnDataReceived != null)
                    {
                        DataReceivedEventArgs args = new(message, remoteIp);
                        OnDataReceived(this, args);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            receiver.Close();
            GC.Collect();
        }

        private void HiddenStart(string remoteAddress, int remotePort, int localPort)
        {
            RemoteAddress = remoteAddress;
            RemotePort = remotePort;
            LocalPort = localPort;
            sender = new UdpClient();
            Run = true;
            ReceiveThread = new Thread(new ThreadStart(ReceiveData));
            ReceiveThread.Start();
            while (ReceiveThread.ThreadState != ThreadState.Running) ;
        }
        #endregion
        #region Public
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

        public void Send(string data)
        {
            try
            {
                byte[] send_buffer = Encoding.ASCII.GetBytes(data);
                sender.Send(send_buffer, data.Length, RemoteAddress, RemotePort);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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

        public void Start(string remoteAddress, int remotePort)
        {
            HiddenStart(remoteAddress, remotePort, remotePort);
        }

        public void Start(string remoteAddress, int remotePort, int localPort)
        {
            HiddenStart(remoteAddress, remotePort, localPort);
        }

        public void Stop()
        {
            Run = false;
            while (ReceiveThread.ThreadState == ThreadState.Running) ;
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