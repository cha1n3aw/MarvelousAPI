using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;

namespace MarvelousAPI
{
    class SerialPortConnection
    {
        #region Public
        public SerialPort Port = null;

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
            catch(Exception e) { return false; }
        }
        #endregion
    }
}
