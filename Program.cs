using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;

namespace MarvelousAPI
{
    class Program
    {
        public static SerialPortConnection Connection { get; set; }
        public static Modem Supermodem { get; set; }
        public static byte[] ReceiveBuffer { get; set; }
        public static Task task;

        public static void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            ReceiveBuffer = new byte[sp.BytesToRead];
            sp.Read(ReceiveBuffer, 0, ReceiveBuffer.Length);
            //Console.WriteLine();
            //foreach (byte b in ReceiveBuffer) Console.Write($"{b:X} ");
            //Console.WriteLine();
            if (ReceiveBuffer.Length < 5)
            {
                Console.Write("Received too short packet: ");
                foreach (byte b in ReceiveBuffer) Console.Write($"{b:x} ");
                Console.WriteLine();
            }
            else
            {
                IncomingPacket packet = new(ReceiveBuffer);
                byte PacketType = ReceiveBuffer[1];
                if (((int)PacketType & 0b10000000) > 0x00) //mask is applied in order to separate and parse all error messages (msb is 1)
                {
                    var casted_packet = packet.CastPacket<Answer_Error>();
                    Console.Write($"ERROR RECEIVED: {casted_packet.GetErrorContents()}, ");
                    if (casted_packet.ValidateCRC(casted_packet.CRC)) Console.WriteLine("CRC VALID");
                    else Console.WriteLine("CRC INVALID");
                }
                else
                {
                    switch (PacketType)
                    {
                        case 0x10: //modem configuration type, further distinction needed
                            {
                                Console.Write("RECEIVED 0X10: ");
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
                                            Console.Write($"data_code={CodeOfData:X}, addr={casted_packet.Address}, ");
                                            if (casted_packet.ValidateCRC(casted_packet.CRC)) Console.WriteLine("CRC VALID");
                                            else Console.WriteLine("CRC INVALID");
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
                                            Console.WriteLine("data code unknown");
                                        }
                                        break;
                                }
                            }
                            break;
                        case 0x7f: //sleeping answer OR reading/writing device control settings
                        {
                            Console.Write("RECEIVED 0x7F: ");
                            if (ReceiveBuffer.Length == 119) //all beacons answer
                            {
                                var casted_packet = packet.CastPacket<Answer_03_31XX>();
                                if (casted_packet.ValidateCRC(casted_packet.CRC))
                                {
                                    Console.WriteLine("CRC VALID");
                                    Console.WriteLine($"all beacons: num={casted_packet.NumberOfDevices}");
                                    foreach (Data_03_31XX_Structure structure in casted_packet.DataStructure)
                                        Console.WriteLine($"beacon_addr={ structure.BeaconAddress }, dev_type={ structure.DeviceType }, maj_ver={ structure.MajorVersion }, min_ver={ structure.MinorVersion }, sec_min_ver={ structure.SecondMinorVersion }, slp_mode={ structure.SleepingMode }, dupl_addr={ structure.DuplicatedAddress }");
                                }
                                else Console.WriteLine("CRC INVALID");
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
                                            Console.Write($"addr={casted_packet.Address}, ");
                                            if (casted_packet.ValidateCRC(casted_packet.CRC)) Console.WriteLine("CRC VALID");
                                            else Console.WriteLine("CRC INVALID");
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
                                Console.Write("RECEIVED 0x47, ");
                                UInt16 CodeOfData = (UInt16)((int)ReceiveBuffer[3] << 8 | (int)ReceiveBuffer[2]); //CAST IS NOT REDUNDANT, DO NOT REMOVE
                                switch (CodeOfData)
                                {
                                    case 0x0001:
                                        {
                                            Console.Write("0x0001: ");
                                            var casted_packet = packet.CastPacket<Incoming_47_0001>();
                                            if (ReceiveBuffer.Length != 23)
                                                if (ReceiveBuffer.Length > 23) ReceiveBuffer = ReceiveBuffer.Take(23).ToArray();
                                                else Console.WriteLine("damaged packet");
                                            else
                                            {
                                                Console.Write($"hedge_addr={ casted_packet.HedgeAddress }, coord_avl={ casted_packet.CoordinatesAvailable }, time_stamp={ casted_packet.Timestamp }, X={ casted_packet.Coordinates_cm.X }, Y={ casted_packet.Coordinates_cm.Y }, Z={ casted_packet.Coordinates_cm.Z }, ");
                                                if (casted_packet.ValidateCRC(casted_packet.CRC)) Console.WriteLine("CRC VALID");
                                                else Console.WriteLine("CRC INVALID");
                                            }
                                        }
                                        break;
                                    case 0x0002:
                                        {
                                            Console.Write("0x0002: ");
                                            var casted_packet = packet.CastPacket<Incoming_47_0002>();
                                            if (ReceiveBuffer.Length != (8 + casted_packet.NumberOfBeacons * 8))
                                                if (ReceiveBuffer.Length > (8 + casted_packet.NumberOfBeacons * 8)) ReceiveBuffer = ReceiveBuffer.Take(8 + casted_packet.NumberOfBeacons * 8).ToArray();
                                                else Console.WriteLine("damaged packet");
                                            else
                                            {
                                                foreach (Data_47_0002_Structure structure in casted_packet.Data_Structure_cm)
                                                    Console.Write($"beacon_addr={ structure.BeaconAddress }, X={ structure.Coordinates_cm.X }, Y={ structure.Coordinates_cm.Y }, { structure.Coordinates_cm.Z }, ");
                                                if (casted_packet.ValidateCRC(casted_packet.CRC)) Console.WriteLine("CRC VALID");
                                                else Console.WriteLine("CRC INVALID");
                                            }
                                        }
                                        break;
                                    case 0x0011:
                                        {
                                            Console.Write("0x0011: ");
                                            var casted_packet = packet.CastPacket<Incoming_47_0011>();
                                            if (ReceiveBuffer.Length != 29)
                                                if (ReceiveBuffer.Length > 29) ReceiveBuffer = ReceiveBuffer.Take(29).ToArray();
                                                else Console.WriteLine("damaged packet");
                                            else
                                            {
                                                Console.Write($"hedge_addr={ casted_packet.HedgeAddress }, coord_avl={ casted_packet.CoordinatesAvailable }, time_stamp={ casted_packet.Timestamp }, X={ casted_packet.Coordinates_mm.X }, Y={ casted_packet.Coordinates_mm.Y }, Z={ casted_packet.Coordinates_mm.Z }, ");
                                                if (casted_packet.ValidateCRC(casted_packet.CRC)) Console.WriteLine("CRC VALID");
                                                else Console.WriteLine("CRC INVALID");
                                            }
                                        }
                                        break;
                                    case 0x0012:
                                        {
                                            Console.Write("0x0012: ");
                                            var casted_packet = packet.CastPacket<Incoming_47_0012>();
                                            if (ReceiveBuffer.Length != (8 + casted_packet.NumberOfBeacons * 14))
                                                if (ReceiveBuffer.Length > (8 + casted_packet.NumberOfBeacons * 14)) ReceiveBuffer = ReceiveBuffer.Take(8 + casted_packet.NumberOfBeacons * 14).ToArray();
                                                else Console.WriteLine("damaged packet");
                                            else
                                            {
                                                foreach (Data_47_0012_Structure structure in casted_packet.Data_Structure_mm)
                                                    Console.Write($"beacon_addr={ structure.BeaconAddress }, X={ structure.Coordinates_mm.X }, Y={ structure.Coordinates_mm.Y }, { structure.Coordinates_mm.Z }, loc_appl={ structure.LocationApplicable }, ");
                                                if (casted_packet.ValidateCRC(casted_packet.CRC)) Console.WriteLine("CRC VALID");
                                                else Console.WriteLine("CRC INVALID");
                                            }
                                        }
                                        break;
                                    default:   //unknown data code
                                        {
                                            Console.Write("data code unknown: ");
                                            foreach (byte b in ReceiveBuffer) Console.Write($"{b:x} ");
                                            Console.WriteLine();
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
                                        Console.Write("0x0001: ");
                                        var casted_packet = packet.CastPacket<Incoming_47_0001>();
                                        if (ReceiveBuffer.Length != 23)
                                            if (ReceiveBuffer.Length > 23) ReceiveBuffer = ReceiveBuffer.Take(23).ToArray();
                                            else Console.WriteLine("damaged packet");
                                        else
                                        {
                                            Console.Write($"hedge_addr={ casted_packet.HedgeAddress }, coord_avl={ casted_packet.CoordinatesAvailable }, time_stamp={ casted_packet.Timestamp }, X={ casted_packet.Coordinates_cm.X }, Y={ casted_packet.Coordinates_cm.Y }, Z={ casted_packet.Coordinates_cm.Z }, ");
                                            if (casted_packet.ValidateCRC(casted_packet.CRC)) Console.WriteLine("CRC VALID");
                                            else Console.WriteLine("CRC INVALID");
                                        }
                                    }
                                    break;
                                case 0x0002:
                                    {
                                        Console.Write("0x0002: ");
                                        var casted_packet = packet.CastPacket<Incoming_47_0002>();
                                        if (ReceiveBuffer.Length != (8 + casted_packet.NumberOfBeacons * 8))
                                            if (ReceiveBuffer.Length > (8 + casted_packet.NumberOfBeacons * 8)) ReceiveBuffer = ReceiveBuffer.Take(8 + casted_packet.NumberOfBeacons * 8).ToArray();
                                            else Console.WriteLine("damaged packet");
                                        else
                                        {
                                            foreach (Data_47_0002_Structure structure in casted_packet.Data_Structure_cm)
                                                Console.Write($"beacon_addr={ structure.BeaconAddress }, X={ structure.Coordinates_cm.X }, Y={ structure.Coordinates_cm.Y }, { structure.Coordinates_cm.Z }, ");
                                            if (casted_packet.ValidateCRC(casted_packet.CRC)) Console.WriteLine("CRC VALID");
                                            else Console.WriteLine("CRC INVALID");
                                        }
                                    }
                                    break;
                                case 0x0011:
                                    {
                                        Console.Write("0x0011: ");
                                        var casted_packet = packet.CastPacket<Incoming_47_0011>();
                                        if (ReceiveBuffer.Length != 29)
                                            if (ReceiveBuffer.Length > 29) ReceiveBuffer = ReceiveBuffer.Take(29).ToArray();
                                            else Console.WriteLine("damaged packet");
                                        else
                                        {
                                            Console.Write($"hedge_addr={ casted_packet.HedgeAddress }, coord_avl={ casted_packet.CoordinatesAvailable }, time_stamp={ casted_packet.Timestamp }, X={ casted_packet.Coordinates_mm.X }, Y={ casted_packet.Coordinates_mm.Y }, Z={ casted_packet.Coordinates_mm.Z }, ");
                                            if (casted_packet.ValidateCRC(casted_packet.CRC)) Console.WriteLine("CRC VALID");
                                            else Console.WriteLine("CRC INVALID");
                                        }
                                    }
                                    break;
                                case 0x0012:
                                    {
                                        Console.Write("0x0012: ");
                                        var casted_packet = packet.CastPacket<Incoming_47_0012>();
                                        if (ReceiveBuffer.Length != (8 + casted_packet.NumberOfBeacons * 14))
                                            if (ReceiveBuffer.Length > (8 + casted_packet.NumberOfBeacons * 14)) ReceiveBuffer = ReceiveBuffer.Take(8 + casted_packet.NumberOfBeacons * 14).ToArray();
                                            else Console.WriteLine("damaged packet");
                                        else
                                        {
                                            foreach (Data_47_0012_Structure structure in casted_packet.Data_Structure_mm)
                                                Console.Write($"beacon_addr={ structure.BeaconAddress }, X={ structure.Coordinates_mm.X }, Y={ structure.Coordinates_mm.Y }, { structure.Coordinates_mm.Z }, loc_appl={ structure.LocationApplicable }, ");
                                            if (casted_packet.ValidateCRC(casted_packet.CRC)) Console.WriteLine("CRC VALID");
                                            else Console.WriteLine("CRC INVALID");
                                        }
                                    }
                                    break;
                                default: //unknown data code
                                    {
                                        Console.Write("data code unknown: ");
                                        foreach (byte b in ReceiveBuffer) Console.Write($"{b:x} ");
                                        Console.WriteLine();
                                    }
                                    break;
                            }
                        }
                            break;
                        default:
                            {
                                Console.Write("Damaged packet: ");
                                foreach (byte b in ReceiveBuffer) Console.Write($"{b:X} ");
                                Console.WriteLine();
                            }
                            break;
                    }
                }
                Connection.Port.DiscardInBuffer();
            }
        }

        public static void DrawMenu()
        {
            Console.Clear();
            Console.WriteLine($"Main menu:\n1) List all beacons\n2) Get modem firmware\n3) Get last coordinates\n0) Reselect COM port");
        }

        public static void DataReceived(object sender, Network.DataReceivedEventArgs e)
        {
            Console.WriteLine($"{e.RemoteIP.Address} : {e.Message}");
        }

        public static void Main(string[] args)
        {
            int choice = -1;
            Network network = new();
            network.OnDataReceived += new Network.DataReceivedHandler(DataReceived);
            network.Start("127.0.0.1", 8001);
            //network.Start("127.0.0.1", 8001, 8002); //it allows to listen on a different port

        rescan_comports:
            if (Connection != null) Connection = null;
            Connection = new();
            Console.Clear();
            Console.WriteLine("Select serial port:");
            List<string> comportnames = Connection.Scan();
            if (comportnames.Count == 0)
            {
                Console.WriteLine("No COM ports availaible! Press any key to retry...");
                Console.ReadKey();
                goto rescan_comports;
            }
            for(int i = 0; i < comportnames.Count; i++) Console.WriteLine($"{ i + 1 }: { comportnames[i] }");
            choice = Convert.ToInt32(Console.ReadKey().KeyChar - '0');
            if (choice < 0 || choice > comportnames.Count) goto rescan_comports;
            if (choice == 0) return;
            else
            {
                Connection.Open(comportnames[choice - 1]);
                Connection.Port.DataReceived += new SerialDataReceivedEventHandler(DataReceived);
                Connection.Port.DiscardInBuffer();
                Connection.Port.DiscardOutBuffer();
            }
            if (Supermodem == null) Supermodem = new();  
        main_menu:
            DrawMenu();
            choice = Convert.ToInt32(Console.ReadKey().KeyChar - '0');
            if (choice < 0 || choice > 3) goto main_menu;
            switch (choice)
            {
                case 0:
                    {
                        Connection.Close();
                        goto rescan_comports;
                    }
                case 1:
                    {
                        task = Task.Run(async () => await Supermodem.GetAvailableBeacons(Connection, (byte)1));
                        //while (!task.IsCompleted) ;
                        goto main_menu;
                    }
                case 2:
                    {
                        task = Task.Run(async () => await Supermodem.GetFirmwareVersion(Connection));
                        //while (!task.IsCompleted) ;
                        goto main_menu;
                    }
                case 3:
                    {
                        task = Task.Run(async () => await Supermodem.GetLastCoordinates(Connection));
                        //while (!task.IsCompleted) ;
                        goto main_menu;
                    }
                default:
                    {

                    }
                    break;
            }            
            
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
            Console.ReadLine();
            network.Stop();
        }
    }
}

/*
task = Task.Run(async () => await Supermodem.GetFirmwareVersion(Connection));
task = Task.Run(async () => await Supermodem.GetLastCoordinates(Connection));
task = Task.Run(async () => await Supermodem.GetAvailableBeacons(Connection, (byte)0));

task = Task.Run(async () => await Supermodem.Beacons[2].GetFirmwareVersion(Connection));
task = Task.Run(async () => await Supermodem.Beacons[Supermodem.Beacons.Find(x => x.Number == 2).Number].WakeUp(Connection));
task = Task.Run(async () => await Supermodem.Beacons[Supermodem.Beacons.Find(x => x.Number == 2).Number].Sleep(Connection));
*/
