using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;

namespace MarvelousAPI
{
    class Program
    {
        #region Public
        public static byte[] ReceiveBuffer { get; set; }
        #endregion
        #region Private
        private static SerialPortConnection Connection { get; set; }
        private static Modem Supermodem { get; set; }
        private static Network Network { get; set; }
        #endregion

        #region Public
        public static void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            ReceiveBuffer = new byte[sp.BytesToRead];
            sp.Read(ReceiveBuffer, 0, ReceiveBuffer.Length);
            //Connection.DebugWriteLine();
            //foreach (byte b in ReceiveBuffer) Connection.DebugWrite($"{b:X} ");
            //Connection.DebugWriteLine();
            if (ReceiveBuffer.Length < 5)
            {
                Connection.DebugWrite("Received too short packet: ");
                foreach (byte b in ReceiveBuffer) Connection.DebugWrite($"{b:x} ");
                Connection.DebugWriteLine();
            }
            else
            {
                IncomingPacket packet = new(ReceiveBuffer);
                byte PacketType = ReceiveBuffer[1];
                if (((int)PacketType & 0b10000000) > 0x00) //mask is applied in order to separate and parse all error messages (msb is 1)
                {
                    var casted_packet = packet.CastPacket<Answer_Error>();
                    Connection.DebugWrite($"ERROR RECEIVED: {casted_packet.GetErrorContents()}, ");
                    if (casted_packet.ValidateCRC(casted_packet.CRC)) Connection.DebugWriteLine("CRC VALID");
                    else Connection.DebugWriteLine("CRC INVALID");
                }
                else
                {
                    switch (PacketType)
                    {
                        case 0x10: //modem configuration type, further distinction needed
                            {
                                Connection.DebugWrite("RECEIVED 0X10: ");
                                UInt16 CodeOfData = (UInt16)((int)ReceiveBuffer[3] << 8 | (int)ReceiveBuffer[2]); //CAST IS NOT REDUNDANT, DO NOT REMOVE
                                switch (CodeOfData)
                                {
                                    case 0x5000: //modem configuration
                                        {

                                        }
                                        break;
                                    case 0xb006: //wake up
                                        {
                                            var casted_packet = packet.CastPacket<Answer_10_b006>();
                                            Supermodem.Beacons[casted_packet.Address].isAwake = true;
                                            Connection.DebugWrite($"data_code={CodeOfData:X}, addr={casted_packet.Address}, ");
                                            if (casted_packet.ValidateCRC(casted_packet.CRC)) Connection.DebugWriteLine("CRC VALID");
                                            else Connection.DebugWriteLine("CRC INVALID");
                                        }
                                        break;
                                    case 0x0101: //setting address
                                        {

                                        }
                                        break;
                                    case 0x1201: //device control settings
                                        {

                                        }
                                        break;
                                    case 0x5003: //manual device location
                                        {

                                        }
                                        break;
                                    case 0x4003: //manual distances
                                        {

                                        }
                                        break;
                                    default: //unknown data code
                                        {
                                            Connection.DebugWriteLine("data code unknown");
                                        }
                                        break;
                                }
                            }
                            break;
                        case 0x7f: //sleeping answer OR reading/writing device control settings
                        {
                            Connection.DebugWrite("RECEIVED 0x7F: ");
                            if (ReceiveBuffer.Length == 119) //all beacons answer
                            {
                                var casted_packet = packet.CastPacket<Answer_03_31XX>();
                                if (casted_packet.ValidateCRC(casted_packet.CRC))
                                {
                                    Connection.DebugWriteLine("CRC VALID");
                                    Connection.DebugWriteLine($"all beacons: num={casted_packet.NumberOfDevices}");
                                    foreach (Data_03_31XX_Structure structure in casted_packet.DataStructure)
                                        Connection.DebugWriteLine($"beacon_addr={ structure.BeaconAddress }, dev_type={ structure.DeviceType }, maj_ver={ structure.MajorVersion }, min_ver={ structure.MinorVersion }, sec_min_ver={ structure.SecondMinorVersion }, slp_mode={ structure.SleepingMode }, dupl_addr={ structure.DuplicatedAddress }");
                                }
                                else Connection.DebugWriteLine("CRC INVALID");
                            }
                            else
                            {
                                UInt16 CodeOfData = (UInt16)((int)ReceiveBuffer[3] << 8 | (int)ReceiveBuffer[2]); //CAST IS NOT REDUNDANT, DO NOT REMOVE
                                switch (CodeOfData)
                                {
                                    case 0xb006: //sleep answer
                                        {
                                            var casted_packet = packet.CastPacket<Answer_7f_b006>();
                                            Supermodem.Beacons[casted_packet.Address].isAwake = false;
                                            Connection.DebugWrite($"addr={casted_packet.Address}, ");
                                            if (casted_packet.ValidateCRC(casted_packet.CRC)) Connection.DebugWriteLine("CRC VALID");
                                            else Connection.DebugWriteLine("CRC INVALID");
                                        }
                                        break;
                                    case 0x1000:
                                        {

                                        }
                                        break;
                                    case 0x0403:
                                        {

                                        }
                                        break;
                                    case 0x1201:
                                        {

                                        }
                                        break;
                                    default: //shitty packet without data code, 26 bytes long if (buffer.Length == 26)
                                        {

                                        }
                                        break;
                                }
                            }
                        }
                            break;
                        case 0x47:
                            {
                                Connection.DebugWrite("RECEIVED 0x47, ");
                                UInt16 CodeOfData = (UInt16)((int)ReceiveBuffer[3] << 8 | (int)ReceiveBuffer[2]); //CAST IS NOT REDUNDANT, DO NOT REMOVE
                                switch (CodeOfData)
                                {
                                    case 0x0001:
                                        {
                                            Connection.DebugWrite("0x0001: ");
                                            var casted_packet = packet.CastPacket<Incoming_47_0001>();
                                            if (ReceiveBuffer.Length != 23)
                                                if (ReceiveBuffer.Length > 23) ReceiveBuffer = ReceiveBuffer.Take(23).ToArray();
                                                else Connection.DebugWriteLine("damaged packet");
                                            else
                                            {
                                                Connection.DebugWrite($"hedge_addr={ casted_packet.HedgeAddress }, coord_avl={ casted_packet.CoordinatesAvailable }, time_stamp={ casted_packet.Timestamp }, X={ casted_packet.Coordinates_cm.X }, Y={ casted_packet.Coordinates_cm.Y }, Z={ casted_packet.Coordinates_cm.Z }, ");
                                                if (casted_packet.ValidateCRC(casted_packet.CRC)) Connection.DebugWriteLine("CRC VALID");
                                                else Connection.DebugWriteLine("CRC INVALID");
                                            }
                                        }
                                        break;
                                    case 0x0002:
                                        {
                                            Connection.DebugWrite("0x0002: ");
                                            var casted_packet = packet.CastPacket<Incoming_47_0002>();
                                            if (ReceiveBuffer.Length != (8 + casted_packet.NumberOfBeacons * 8))
                                                if (ReceiveBuffer.Length > (8 + casted_packet.NumberOfBeacons * 8)) ReceiveBuffer = ReceiveBuffer.Take(8 + casted_packet.NumberOfBeacons * 8).ToArray();
                                                else Connection.DebugWriteLine("damaged packet");
                                            else
                                            {
                                                foreach (Data_47_0002_Structure structure in casted_packet.Data_Structure_cm)
                                                    Connection.DebugWrite($"beacon_addr={ structure.BeaconAddress }, X={ structure.Coordinates_cm.X }, Y={ structure.Coordinates_cm.Y }, { structure.Coordinates_cm.Z }, ");
                                                if (casted_packet.ValidateCRC(casted_packet.CRC)) Connection.DebugWriteLine("CRC VALID");
                                                else Connection.DebugWriteLine("CRC INVALID");
                                            }
                                        }
                                        break;
                                    case 0x0011:
                                        {
                                            Connection.DebugWrite("0x0011: ");
                                            var casted_packet = packet.CastPacket<Incoming_47_0011>();
                                            if (ReceiveBuffer.Length != 29)
                                                if (ReceiveBuffer.Length > 29) ReceiveBuffer = ReceiveBuffer.Take(29).ToArray();
                                                else Connection.DebugWriteLine("damaged packet");
                                            else
                                            {
                                                Connection.DebugWrite($"hedge_addr={ casted_packet.HedgeAddress }, coord_avl={ casted_packet.CoordinatesAvailable }, time_stamp={ casted_packet.Timestamp }, X={ casted_packet.Coordinates_mm.X }, Y={ casted_packet.Coordinates_mm.Y }, Z={ casted_packet.Coordinates_mm.Z }, ");
                                                if (casted_packet.ValidateCRC(casted_packet.CRC)) Connection.DebugWriteLine("CRC VALID");
                                                else Connection.DebugWriteLine("CRC INVALID");
                                            }
                                        }
                                        break;
                                    case 0x0012:
                                        {
                                            Connection.DebugWrite("0x0012: ");
                                            var casted_packet = packet.CastPacket<Incoming_47_0012>();
                                            if (ReceiveBuffer.Length != (8 + casted_packet.NumberOfBeacons * 14))
                                                if (ReceiveBuffer.Length > (8 + casted_packet.NumberOfBeacons * 14)) ReceiveBuffer = ReceiveBuffer.Take(8 + casted_packet.NumberOfBeacons * 14).ToArray();
                                                else Connection.DebugWriteLine("damaged packet");
                                            else
                                            {
                                                foreach (Data_47_0012_Structure structure in casted_packet.Data_Structure_mm)
                                                    Connection.DebugWrite($"beacon_addr={ structure.BeaconAddress }, X={ structure.Coordinates_mm.X }, Y={ structure.Coordinates_mm.Y }, { structure.Coordinates_mm.Z }, loc_appl={ structure.LocationApplicable }, ");
                                                if (casted_packet.ValidateCRC(casted_packet.CRC)) Connection.DebugWriteLine("CRC VALID");
                                                else Connection.DebugWriteLine("CRC INVALID");
                                            }
                                        }
                                        break;
                                    default:   //unknown data code
                                        {
                                            Connection.DebugWrite("data code unknown: ");
                                            foreach (byte b in ReceiveBuffer) Connection.DebugWrite($"{b:x} ");
                                            Connection.DebugWriteLine();
                                        }
                                        break;
                                }
                            }
                            break;
                        case 0x03:
                        {
                            UInt16 CodeOfData = (UInt16)((int)ReceiveBuffer[3] << 8 | (int)ReceiveBuffer[2]); //CAST IS NOT REDUNDANT, DO NOT REMOVE
                            switch (CodeOfData)
                            {
                                case 0x0001:
                                    {
                                        Connection.DebugWrite("0x0001: ");
                                        var casted_packet = packet.CastPacket<Incoming_47_0001>();
                                        if (ReceiveBuffer.Length != 23)
                                            if (ReceiveBuffer.Length > 23) ReceiveBuffer = ReceiveBuffer.Take(23).ToArray();
                                            else Connection.DebugWriteLine("damaged packet");
                                        else
                                        {
                                            Connection.DebugWrite($"hedge_addr={ casted_packet.HedgeAddress }, coord_avl={ casted_packet.CoordinatesAvailable }, time_stamp={ casted_packet.Timestamp }, X={ casted_packet.Coordinates_cm.X }, Y={ casted_packet.Coordinates_cm.Y }, Z={ casted_packet.Coordinates_cm.Z }, ");
                                            if (casted_packet.ValidateCRC(casted_packet.CRC)) Connection.DebugWriteLine("CRC VALID");
                                            else Connection.DebugWriteLine("CRC INVALID");
                                        }
                                    }
                                    break;
                                case 0x0002:
                                    {
                                        Connection.DebugWrite("0x0002: ");
                                        var casted_packet = packet.CastPacket<Incoming_47_0002>();
                                        if (ReceiveBuffer.Length != (8 + casted_packet.NumberOfBeacons * 8))
                                            if (ReceiveBuffer.Length > (8 + casted_packet.NumberOfBeacons * 8)) ReceiveBuffer = ReceiveBuffer.Take(8 + casted_packet.NumberOfBeacons * 8).ToArray();
                                            else Connection.DebugWriteLine("damaged packet");
                                        else
                                        {
                                            foreach (Data_47_0002_Structure structure in casted_packet.Data_Structure_cm)
                                                Connection.DebugWrite($"beacon_addr={ structure.BeaconAddress }, X={ structure.Coordinates_cm.X }, Y={ structure.Coordinates_cm.Y }, { structure.Coordinates_cm.Z }, ");
                                            if (casted_packet.ValidateCRC(casted_packet.CRC)) Connection.DebugWriteLine("CRC VALID");
                                            else Connection.DebugWriteLine("CRC INVALID");
                                        }
                                    }
                                    break;
                                case 0x0011:
                                    {
                                        Connection.DebugWrite("0x0011: ");
                                        var casted_packet = packet.CastPacket<Incoming_47_0011>();
                                        if (ReceiveBuffer.Length != 29)
                                            if (ReceiveBuffer.Length > 29) ReceiveBuffer = ReceiveBuffer.Take(29).ToArray();
                                            else Connection.DebugWriteLine("damaged packet");
                                        else
                                        {
                                            Connection.DebugWrite($"hedge_addr={ casted_packet.HedgeAddress }, coord_avl={ casted_packet.CoordinatesAvailable }, time_stamp={ casted_packet.Timestamp }, X={ casted_packet.Coordinates_mm.X }, Y={ casted_packet.Coordinates_mm.Y }, Z={ casted_packet.Coordinates_mm.Z }, ");
                                            if (casted_packet.ValidateCRC(casted_packet.CRC)) Connection.DebugWriteLine("CRC VALID");
                                            else Connection.DebugWriteLine("CRC INVALID");
                                        }
                                    }
                                    break;
                                case 0x0012:
                                    {
                                        Connection.DebugWrite("0x0012: ");
                                        var casted_packet = packet.CastPacket<Incoming_47_0012>();
                                        if (ReceiveBuffer.Length != (8 + casted_packet.NumberOfBeacons * 14))
                                            if (ReceiveBuffer.Length > (8 + casted_packet.NumberOfBeacons * 14)) ReceiveBuffer = ReceiveBuffer.Take(8 + casted_packet.NumberOfBeacons * 14).ToArray();
                                            else Connection.DebugWriteLine("damaged packet");
                                        else
                                        {
                                            foreach (Data_47_0012_Structure structure in casted_packet.Data_Structure_mm)
                                                Connection.DebugWrite($"beacon_addr={ structure.BeaconAddress }, X={ structure.Coordinates_mm.X }, Y={ structure.Coordinates_mm.Y }, { structure.Coordinates_mm.Z }, loc_appl={ structure.LocationApplicable }, ");
                                            if (casted_packet.ValidateCRC(casted_packet.CRC)) Connection.DebugWriteLine("CRC VALID");
                                            else Connection.DebugWriteLine("CRC INVALID");
                                        }
                                    }
                                    break;
                                default: //unknown data code
                                    {
                                        Connection.DebugWrite("data code unknown: ");
                                        foreach (byte b in ReceiveBuffer) Connection.DebugWrite($"{b:x} ");
                                        Connection.DebugWriteLine();
                                    }
                                    break;
                            }
                        }
                            break;
                        default:
                            {
                                Connection.DebugWrite("Damaged packet: ");
                                foreach (byte b in ReceiveBuffer) Connection.DebugWrite($"{b:X} ");
                                Connection.DebugWriteLine();
                            }
                            break;
                    }
                }
                Connection.Port.DiscardInBuffer();
            }
        }

        public static void UdpDataReceived(object sender, Network.DataReceivedEventArgs e)
        {
            Network.DebugWriteLine($"{e.RemoteIP.Address} : {e.Message}");
        }

        public static void DrawMessage(string message, ref int choice)
        {
            Console.Clear();
            Console.WriteLine(message);
            choice = Convert.ToInt32(Console.ReadKey().KeyChar - '0');
        }

        public static void Main(string[] args)
        {
            int choice = -1;
            Network = new();
            Network.OnDataReceived += new Network.UdpDataReceivedHandler(UdpDataReceived);
            Network.Start("127.0.0.1", 8001);
            //network.Start("127.0.0.1", 8001, 8002); //it allows to listen on a different port
            Supermodem = new();
        rescan_comports:
            if (Connection != null) Connection = null;
            Connection = new();
            Console.Clear();
            Console.WriteLine("Select serial port:");
            List<string> comportnames = Connection.Scan();
            if (comportnames.Count == 0)
            {
                DrawMessage("No COM ports availaible! Press any key to retry...", ref choice);
                goto rescan_comports;
            }
            for(int i = 0; i < comportnames.Count; i++) Console.WriteLine($"{ i + 1 }: { comportnames[i] }");
            choice = Convert.ToInt32(Console.ReadKey().KeyChar - '0');
            if (choice < 0 || choice > comportnames.Count) goto rescan_comports;
            if (choice == 0) return;
            else
            {
                Connection.Open(comportnames[choice - 1]);
                Connection.Port.DataReceived += new SerialDataReceivedEventHandler(SerialDataReceived);
                Connection.Port.DiscardInBuffer();
                Connection.Port.DiscardOutBuffer();
            }
        main_menu:
            DrawMessage($"Main menu:\n1) List all beacons\n2) Get modem firmware\n3) Get last coordinates\n4) Serial debugging\n5) UDP debugging\n6) Wake up beacons\n7) Sleep beacons\n8) Re-scan for available beacons\n9) Reselect COM port\n0) Exit", ref choice);
            if (choice < 0 || choice > 9) goto main_menu;
            switch (choice)
            {
                case 1:
                    {
                        Console.Clear();
                        List<Beacon> beacons = Supermodem.Beacons.FindAll(x => x.Exists == true);
                        if (beacons.Count == 0)
                        {
                            DrawMessage("No available beacons! Re-scan manually\nPress any key...", ref choice);
                            goto main_menu;
                        }
                        else
                        {
                            for (int i = 0; i < beacons.Count; i++)
                                Console.WriteLine($"{ beacons[i].Number }: Awaken = { beacons[i].isAwake }, IsHedge = { beacons[i].isHedge }");
                            Console.ReadKey();
                            goto main_menu;
                        }
                    }
                case 2:
                    {
                        Task newTask = Task.Run(async () => await Supermodem.GetFirmwareVersion(Connection));
                        while (!newTask.IsCompleted) ;
                        DrawMessage($"Modem version: { Supermodem.MajorVersion }.{ Supermodem.MinorVersion }", ref choice);
                        goto main_menu;
                    }
                case 3:
                    {
                        Task newTask = Task.Run(async () => await Supermodem.GetLastCoordinates(Connection));
                        while (!newTask.IsCompleted) ;
                        goto main_menu;
                    }
                case 4:
                    {
                        Connection.AllowDebug = true;
                        DrawMessage("Press any key...", ref choice);
                        Connection.AllowDebug = false;
                        goto main_menu;
                    }
                case 5:
                    {
                        Network.AllowDebug = true;
                        DrawMessage("Press any key...", ref choice);
                        Network.AllowDebug = false;
                        goto main_menu;
                    }
                case 6:
                    {
                wakeup_beacons:
                        List<Beacon> sleepyBeacons = Supermodem.Beacons.FindAll(x => x.isAwake == false && x.Exists == true);
                        if (sleepyBeacons.Count == 0)
                        {
                            DrawMessage("No available beacons! Re-scan manually\nPress any key...", ref choice);
                            goto main_menu;
                        }
                        else
                        {
                            for (int i = 0; i < sleepyBeacons.Count; i++) Console.WriteLine($"{ sleepyBeacons[i].Number } : Hedge = { sleepyBeacons[i].isHedge }");
                            choice = Convert.ToInt32(Console.ReadKey().KeyChar - '0');
                            if (sleepyBeacons.Find(x => x.Number == choice) == null) goto wakeup_beacons;
                            else
                            {
                                Task newTask = Task.Run(async () => await sleepyBeacons.Find(x => x.Number == choice).WakeUp(Connection));
                                goto main_menu;
                            }
                        }
                    }
                case 7:
                    {
                    sleep_beacons:
                        List<Beacon> awakenBeacons = Supermodem.Beacons.FindAll(x => x.isAwake == true && x.Exists == true);
                        if (awakenBeacons.Count == 0)
                        {
                            DrawMessage("No available beacons! Re-scan manually\nPress any key...", ref choice);
                            goto main_menu;
                        }
                        else
                        {
                            for (int i = 0; i < awakenBeacons.Count; i++) Console.WriteLine($"{ awakenBeacons[i].Number } : Hedge = { awakenBeacons[i].isHedge }");
                            choice = Convert.ToInt32(Console.ReadKey().KeyChar - '0');
                            if (awakenBeacons.Find(x => x.Number == choice) == null) goto sleep_beacons;
                            else
                            {
                                Task newTask = Task.Run(async () => await awakenBeacons.Find(x => x.Number == choice).Sleep(Connection));
                                goto main_menu;
                            }
                        }
                    }
                case 8:
                    {
                        Task newTask = Task.Run(async () => await Supermodem.GetAvailableBeacons(Connection, (byte)1));
                        while (!newTask.IsCompleted) ;
                        goto main_menu;
                    }
                case 9:
                    {
                        Connection.Close();
                        goto rescan_comports;
                    }
                case 0:
                    {
                        Connection.Close();
                        Network.Stop();
                        GC.Collect();
                        return;
                    }
                default:
                    {
                        DrawMessage("Unimplemented function\nPress any key...", ref choice);
                        goto main_menu;
                    }
            }            
        }
        #endregion
    }
}

/*
task = Task.Run(async () => await Supermodem.GetFirmwareVersion(Connection));
task = Task.Run(async () => await Supermodem.GetLastCoordinates(Connection));
task = Task.Run(async () => await Supermodem.GetAvailableBeacons(Connection, (byte)0));

task = Task.Run(async () => await Supermodem.Beacons[2].GetFirmwareVersion(Connection));
task = Task.Run(async () => await Supermodem.Beacons[Supermodem.Beacons.Find(x => x.Number == 2).Number].WakeUp(Connection));
task = Task.Run(async () => await Supermodem.Beacons[Supermodem.Beacons.Find(x => x.Number == 2).Number].Sleep(Connection));
//task = Task.Run(async () => await Supermodem.GetAvailableBeacons(Connection, (byte)0));
//task = Task.Run(async () => await Supermodem.Beacons[Supermodem.Beacons.Find(x => x.Number == 2).Number].Sleep(Connection));
//while (!task.IsCompleted)
//{

//}
//task = Task.Run(async () => await Supermodem.Beacons[Supermodem.Beacons.Find(x => x.Number == 3).Number].Sleep(Connection));
//while (!task.IsCompleted)
//{

//}
//task = Task.Run(async () => await Supermodem.Beacons[Supermodem.Beacons.Find(x => x.Number == 4).Number].Sleep(Connection));
//while (!task.IsCompleted)
//{

//}
//task = Task.Run(async () => await Supermodem.Beacons[Supermodem.Beacons.Find(x => x.Number == 5).Number].Sleep(Connection));
//while (!task.IsCompleted)
//{

//}
//Console.WriteLine("Awaken all");
*/