using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;

namespace MarvelousAPI
{
    public class SerialPortConnection
    {
        #region Public
        public SerialPort Port = null;
        public bool AllowDebug = false;
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

        public bool Write(byte[] buffer)
        {
            if (Port.IsOpen) Port.Write(buffer, 0, buffer.Length);
            else return false;
            Port.DiscardOutBuffer();
            return true;
        }

        public List<string> Scan()
        {
            List<string> portsList = SerialPort.GetPortNames().ToList();
            return portsList;
        }

        public bool Close()
        {
            if (Port != null && Port.IsOpen) Port.Close();
            else return false;
            return true;
        }

        public bool Open(string portName)
        {
            try
            {
                Port = new(portName)
                {
                    BaudRate = 500000,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    DataBits = 8,
                    Handshake = Handshake.None
                };
                Port.Open();
                return true;
            }
            catch (Exception) { return false; }
        }
        #endregion
    }
}
