using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarvelousAPI
{
    class Network
    {
        public delegate void DataReceivedHandler(object sender, DataReceivedEventArgs e);
        public event DataReceivedHandler OnDataReceived;
        private string RemoteAddress = string.Empty;
        private int RemotePort = 0;
        private int LocalPort = 0;
        private bool Run = false;
        Thread ReceiveThread;
        UdpClient sender;

        private void ReceiveData()
        {
            UdpClient receiver = new UdpClient(LocalPort);
            IPEndPoint remoteIp = null;
            try
            {
                while (Run)
                {
                    byte[] data = receiver.Receive(ref remoteIp);
                    string message = Encoding.Default.GetString(data);
                    if (OnDataReceived != null)
                    {
                        DataReceivedEventArgs args = new DataReceivedEventArgs(message, remoteIp);
                        OnDataReceived(this, args);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                receiver.Close();
            }
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
            finally
            {
                
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

        private void HiddenStart(string remoteAddress, int remotePort, int localPort)
        {
            RemoteAddress = remoteAddress;
            RemotePort = remotePort;
            LocalPort = localPort;
            sender = new UdpClient();
            Run = true;
            ReceiveThread = new Thread(new ThreadStart(ReceiveData));
            ReceiveThread.Start();
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
        }

        public Network()
        {

        }
    }
}
