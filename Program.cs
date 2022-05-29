using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security;
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
        private static Network ModemUdpStream { get; set; }
        private static Network ClientUdpStream { get; set; }
        private static SerializerAPI Serializer { get; set; }
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
                                            Supermodem.Beacons[casted_packet.Address].Settings.isAwake = true;
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
                                    Connection.DebugWriteLine($"all beacons: num={ casted_packet.NumberOfDevices }");
                                    foreach (Data_03_31XX_Structure structure in casted_packet.DataStructure)
                                    {
                                        if (structure.BeaconAddress < 255)
                                        {
                                            Connection.DebugWriteLine($"beacon_addr={ structure.BeaconAddress }, dev_type={ structure.DeviceType }, maj_ver={ structure.MajorVersion }, min_ver={ structure.MinorVersion }, sec_min_ver={ structure.SecondMinorVersion }, slp_mode={ structure.SleepingMode }, dupl_addr={ structure.DuplicatedAddress }");
                                            Supermodem.Beacons[structure.BeaconAddress].Settings.Exists = true;
                                            Supermodem.Beacons[structure.BeaconAddress].Settings.isAwake = !structure.SleepingMode;
                                            Supermodem.Beacons[structure.BeaconAddress].Settings.isHedge = 
                                                (byte)(structure.DeviceType & (byte)0b00111111) == 43 ||
                                                (byte)(structure.DeviceType & (byte)0b00111111) == 23 ||
                                                (byte)(structure.DeviceType & (byte)0b00111111) == 31 ||
                                                (byte)(structure.DeviceType & (byte)0b00111111) == 45;
                                            Supermodem.Beacons[structure.BeaconAddress].Settings.FirmwareVersion = $"{ structure.MajorVersion }.{ structure.MinorVersion }.{ structure.SecondMinorVersion }";
                                        }
                                    }
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
                                            Supermodem.Beacons[casted_packet.Address].Settings.isAwake = false;
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
                                if (ReceiveBuffer[2] == 0x72) //Number of bytes = Get availaible beacons
                                {
                                    /*
                                    Connection.DebugWrite("0x0003: ");
                                    var casted_packet = packet.CastPacket<Answer_03_31XX>();
                                    foreach (Data_03_31XX_Structure structure in casted_packet.DataStructure)
                                    {
                                        Supermodem.Beacons[structure.BeaconAddress].Exists = true;
                                        Supermodem.Beacons[structure.BeaconAddress].isAwake = !structure.SleepingMode;
                                        Supermodem.Beacons[structure.BeaconAddress].isHedge = 
                                            (byte)(structure.DeviceType & (byte)0b00111111) == 0x43 || 
                                            (byte)(structure.DeviceType & (byte)0b00111111) == 0x23 || 
                                            (byte)(structure.DeviceType & (byte)0b00111111) == 0x31 || 
                                            (byte)(structure.DeviceType & (byte)0b00111111) == 0x45;
                                        Supermodem.Beacons[structure.BeaconAddress].FirmwareVersion = $"{ structure.MajorVersion }.{ structure.MinorVersion }.{ structure.SecondMinorVersion }";
                                    }
                                    */
                                }
                                else
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

        public static void DrawMessage(string message, ref int choice)
        {
            Console.Clear();
            Console.WriteLine(message);
            choice = Convert.ToInt32(Console.ReadKey().KeyChar - '0');
        }

        public static void ClientUdpDataReceived(object sender, Network.DataReceivedEventArgs e)
        {
            ClientUdpStream.DebugWriteLine($"{e.RemoteIP.Address} : {e.Message}");
            dynamic data = Serializer.Deserialize(e.Message);
            if (data?.GetType().ToString() == "MarvelousAPI.Beacon")
            {
                foreach (PropertyInfo propertyInfo in Type.GetType("MarvelousAPI.BeaconSettings").GetProperties())
                {
                    try
                    {
                        object value = propertyInfo.GetValue(data.Settings);
                        if (value != null) propertyInfo.SetValue(Supermodem.Beacons[Supermodem.Beacons.FindIndex(x => x.Settings.ID == ((Beacon)data).Settings.ID)].Settings, value);
                    }
                    catch (Exception) { }
                }
            }
        }

        public static void ModemUdpDataReceived(object sender, Network.DataReceivedEventArgs e)
        {
            ModemUdpStream.DebugWriteLine($"Data received from: {e.RemoteIP.Address}");
            int packgeType = e.Message[3];
            packgeType = packgeType << 8;
            packgeType += e.Message[2];
            if (packgeType == 0x0011)
			{
                int id = (byte)e.Message[0];

                int x = (byte)e.Message[12];
                x = x << 8;
                x += (byte)e.Message[11];
                x = x << 8;
                x += (byte)e.Message[10];
                x = x << 8;
                x += (byte)e.Message[9];

                int y = (byte)e.Message[16];
                y = y << 8;
                y += (byte)e.Message[15];
                y = y << 8;
                y += (byte)e.Message[14];
                y = y << 8;
                y += (byte)e.Message[13];

                int z = (byte)e.Message[12];
                z = z << 8;
                z += (byte)e.Message[11];
                z = z << 8;
                z += (byte)e.Message[10];
                z = z << 8;
                z += (byte)e.Message[9];

                Supermodem.Beacons[id].Settings.Exists = true;
                Supermodem.Beacons[id].Settings.isAwake = true;
                Supermodem.Beacons[id].Settings.isHedge = true;
                Supermodem.Beacons[id].Coordinates_mm = new CoordinatesStructure_mm { X = x, Y = y, Z = z};
                foreach (KeyValuePair<IPEndPoint, Pipe> endPoint in ClientUdpStream.EndpointsPipes)
                    ClientUdpStream.Send(Serializer.Serialize(Supermodem.Beacons[(byte)e.Message[0]]), endPoint.Key);
                ModemUdpStream.DebugWriteLine(Serializer.Serialize(Supermodem.Beacons[(byte)e.Message[0]]));
            }
            else if (packgeType == 0x0012)
			{
                for (int i = 0; i < (byte)e.Message[5]; i++)
				{
                    int id = (byte)e.Message[6 + i * 14 + 0];

                    int x = (byte)e.Message[6 + i * 14 + 4];
                    x = x << 8;
                    x += (byte)e.Message[6 + i * 14 + 3];
                    x = x << 8;
                    x += (byte)e.Message[6 + i * 14 + 2];
                    x = x << 8;
                    x += (byte)e.Message[6 + i * 14 + 1];

                    int y = (byte)e.Message[6 + i * 14 + 8];
                    y = y << 8;
                    y += (byte)e.Message[6 + i * 14 + 7];
                    y = y << 8;
                    y += (byte)e.Message[6 + i * 14 + 6];
                    y = y << 8;
                    y += (byte)e.Message[6 + i * 14 + 5];

                    int z = (byte)e.Message[6 + i * 14 + 12];
                    z = z << 8;
                    z += (byte)e.Message[6 + i * 14 + 11];
                    z = z << 8;
                    z += (byte)e.Message[6 + i * 14 + 10];
                    z = z << 8;
                    z += (byte)e.Message[6 + i * 14 + 9];

                    Supermodem.Beacons[id].Settings.Exists = true;
                    Supermodem.Beacons[id].Settings.isAwake = true;
                    Supermodem.Beacons[id].Settings.isHedge = false;
                    Supermodem.Beacons[id].Coordinates_mm = new CoordinatesStructure_mm { X = x, Y = y, Z = z };
                    foreach (KeyValuePair<IPEndPoint, Pipe> endPoint in ClientUdpStream.EndpointsPipes)
                        ClientUdpStream.Send(Serializer.Serialize(Supermodem.Beacons[id]), endPoint.Key);
                    ModemUdpStream.DebugWriteLine(Serializer.Serialize(Supermodem.Beacons[id]));
                }
			}
        }

        public static void Main(string[] args)
        {
            Console.WriteLine($"=========================================================================");
            Console.Write($"== Login: ");
            string login = Console.ReadLine();
            Console.Write("== Password: ");
            SecureString password = GetPassword();
            int choice = -1;
            ClientUdpStream = new();
            Serializer = new();
            Supermodem = new();
            ClientUdpStream.OnDataReceived += new Network.UdpDataReceivedHandler(ClientUdpDataReceived);
            ClientUdpStream.Start(40000);
        select_connection_type:
            DrawMessage("Select connection type:\n1) COM\n2) UDP", ref choice);
            if (choice == 1)
            {
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
                for (int i = 0; i < comportnames.Count; i++) Console.WriteLine($"{ i + 1 }: { comportnames[i] }");
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
            main_menu_serial:
                DrawMessage($"Main serial menu:\n1) List all beacons\n2) Get modem firmware\n3) Get last coordinates\n4) Serial debugging\n5) UDP debugging\n6) Wake up beacons\n7) Sleep beacons\n8) Re-scan for available beacons\n9) Reselect COM port\n0) Change connection method", ref choice);
                if (choice < 0 || choice > 9) goto main_menu_serial;
                switch (choice)
                {
                    case 1:
                        {
                            Console.Clear();
                            List<Beacon> beacons = Supermodem.Beacons.FindAll(x => x.Settings.Exists == true);
                            if (beacons.Count == 0)
                            {
                                DrawMessage("No available beacons! Re-scan manually\nPress any key...", ref choice);
                                goto main_menu_serial;
                            }
                            else
                            {
                                for (int i = 0; i < beacons.Count; i++)
                                    Console.WriteLine($"{ beacons[i].Settings.ID }: Awaken = { beacons[i].Settings.isAwake }, IsHedge = { beacons[i].Settings.isHedge }, Firmware = { beacons[i].Settings.FirmwareVersion }");
                                Console.WriteLine("Press any key...");
                                Console.ReadKey();
                                goto main_menu_serial;
                            }
                        }
                    case 2:
                        {
                            Task newTask = Task.Run(async () => await Supermodem.GetFirmwareVersion(Connection));
                            while (!newTask.IsCompleted) ;
                            DrawMessage($"Modem version: { Supermodem.Settings.MajorVersion }.{ Supermodem.Settings.MinorVersion }", ref choice);
                            goto main_menu_serial;
                        }
                    case 3:
                        {
                            Task newTask = Task.Run(async () => await Supermodem.GetLastCoordinates(Connection));
                            while (!newTask.IsCompleted) ;
                            goto main_menu_serial;
                        }
                    case 4:
                        {
                            Connection.AllowDebug = true;
                            DrawMessage("Press any key...", ref choice);
                            Connection.AllowDebug = false;
                            goto main_menu_serial;
                        }
                    case 5:
                        {
                            ClientUdpStream.AllowDebug = true;
                            DrawMessage("Press any key...", ref choice);
                            ClientUdpStream.AllowDebug = false;
                            goto main_menu_serial;
                        }
                    case 6:
                        {
                        wakeup_beacons:
                            List<Beacon> sleepyBeacons = Supermodem.Beacons.FindAll(x => x.Settings.isAwake == false && x.Settings.Exists == true);
                            if (sleepyBeacons.Count == 0)
                            {
                                DrawMessage("No beacons in sleep mode! Re-scan manually\nPress any key...", ref choice);
                                goto main_menu_serial;
                            }
                            else
                            {
                                Console.Clear();
                                for (int i = 0; i < sleepyBeacons.Count; i++) Console.WriteLine($"{ sleepyBeacons[i].Settings.ID } : Hedge = { sleepyBeacons[i].Settings.isHedge }");
                                Console.WriteLine("Select beacon address: ");
                                choice = Convert.ToInt32(Console.ReadKey().KeyChar - '0');
                                if (choice != 255 && sleepyBeacons.Find(x => x.Settings.ID == choice) == null) goto wakeup_beacons;
                                else
                                {
                                    Task newTask;
                                    if (choice == 255)
                                    {
                                        foreach (Beacon beacon in sleepyBeacons)
                                        {
                                            newTask = Task.Run(async () => await sleepyBeacons.Find(x => x.Settings.ID == choice).WakeUp(Connection));
                                            while (!newTask.IsCompleted) ;
                                        }
                                    }
                                    else newTask = Task.Run(async () => await sleepyBeacons.Find(x => x.Settings.ID == choice).WakeUp(Connection));
                                    goto main_menu_serial;
                                }
                            }
                        }
                    case 7:
                        {
                        sleep_beacons:
                            List<Beacon> awakenBeacons = Supermodem.Beacons.FindAll(x => x.Settings.isAwake == true && x.Settings.Exists == true);
                            if (awakenBeacons.Count == 0)
                            {
                                DrawMessage("No awaken beacons! Re-scan manually\nPress any key...", ref choice);
                                goto main_menu_serial;
                            }
                            else
                            {
                                Console.Clear();
                                for (int i = 0; i < awakenBeacons.Count; i++) Console.WriteLine($"{ awakenBeacons[i].Settings.ID } : Hedge = { awakenBeacons[i].Settings.isHedge }");
                                Console.WriteLine("Select beacon address: ");
                                choice = Convert.ToInt32(Console.ReadKey().KeyChar - '0');
                                if (awakenBeacons.Find(x => x.Settings.ID == choice) == null) goto sleep_beacons;
                                else
                                {
                                    Task newTask;
                                    if (choice == 255)
                                    {
                                        foreach (Beacon beacon in awakenBeacons)
                                        {
                                            newTask = Task.Run(async () => await awakenBeacons.Find(x => x.Settings.ID == choice).Sleep(Connection));
                                            while (!newTask.IsCompleted) ;
                                        }
                                    }
                                    else newTask = Task.Run(async () => await awakenBeacons.Find(x => x.Settings.ID == choice).Sleep(Connection));
                                    goto main_menu_serial;
                                }
                            }
                        }
                    case 8:
                        {
                            Task newTask = Task.Run(async () => await Supermodem.GetAvailableBeacons(Connection, (byte)1));
                            while (!newTask.IsCompleted) ;
                            goto main_menu_serial;
                        }
                    case 9:
                        {
                            Connection.Close();
                            goto rescan_comports;
                        }
                    case 0:
                        {
                            Connection.Close();
                            ClientUdpStream.Stop();
                            GC.Collect();
                            goto select_connection_type;
                        }
                    default:
                        {
                            DrawMessage("Unimplemented function\nPress any key...", ref choice);
                            goto main_menu_serial;
                        }
                }
            }
            else if (choice == 2)
            {
                ModemUdpStream = new();
                ModemUdpStream.Start(49100);
                ModemUdpStream.OnDataReceived += new Network.UdpDataReceivedHandler(ModemUdpDataReceived);
            main_menu_udp:
                DrawMessage($"Main UDP menu:\n1) List all beacons\n2) Client UDP debug\n3) Modem UDP debug\n4) Full UDP debug\n5) List connected clients\n0) Change connection method", ref choice);
                if (choice < 0 || choice > 9) goto main_menu_udp;
                switch (choice)
                {
                    case 1:
                        {
                            Console.Clear();
                            List<Beacon> beacons = Supermodem.Beacons.FindAll(x => x.Settings.Exists == true);
                            if (beacons.Count == 0)
                            {
                                DrawMessage("No available beacons! Re-scan manually\nPress any key...", ref choice);
                                goto main_menu_udp;
                            }
                            else
                            {
                                for (int i = 0; i < beacons.Count; i++)
                                    Console.WriteLine($"{ beacons[i].Settings.ID }: Awaken = { beacons[i].Settings.isAwake }, IsHedge = { beacons[i].Settings.isHedge }, Firmware = { beacons[i].Settings.FirmwareVersion }");
                                Console.WriteLine("Press any key...");
                                Console.ReadKey();
                                goto main_menu_udp;
                            }
                        }
                    case 2:
                        {
                            Console.Clear();
                            ClientUdpStream.AllowDebug = true;
                            DrawMessage("Client UDP connection debug, press any key to exit...", ref choice);
                            ClientUdpStream.AllowDebug = false;
                            goto main_menu_udp;
                        }
                    case 3:
                        {
                            Console.Clear();
                            ModemUdpStream.AllowDebug = true;
                            DrawMessage("Modem UDP connection debug, press any key to exit...", ref choice);
                            ModemUdpStream.AllowDebug = false;
                            goto main_menu_udp;
                        }
                    case 4:
                        {
                            Console.Clear();
                            ClientUdpStream.AllowDebug = true;
                            ModemUdpStream.AllowDebug = true;
                            DrawMessage("Client UDP connection debug, press any key to exit...", ref choice);
                            ClientUdpStream.AllowDebug = false;
                            ModemUdpStream.AllowDebug = false;
                            goto main_menu_udp;
                        }
                    case 5:
                        {
                            Console.Clear();
                            foreach (KeyValuePair<IPEndPoint, Pipe> keyValuePair in ClientUdpStream.EndpointsPipes)
                                Console.WriteLine($"IP Address: { keyValuePair.Key.Address }, Port: { keyValuePair.Key.Port }");
                            DrawMessage("Press any key to exit...", ref choice);
                            goto main_menu_udp;
                        }
                    case 0:
                        {
                            goto select_connection_type;
                        }
                    default:
                        {
                            DrawMessage("Unimplemented function\nPress any key...", ref choice);
                            goto main_menu_udp;
                        }
                }
            }
            else
            {
                DrawMessage("Wrong choice! Press any key to retry...", ref choice);
                goto select_connection_type;
            }
                    
        }
        #endregion
        #region Private
        private static SecureString GetPassword()
        {
            SecureString password = new();
            while (true)
            {
                ConsoleKeyInfo keyPressed = Console.ReadKey(true);
                if (keyPressed.Key == ConsoleKey.Enter) break;
                else if (keyPressed.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password.RemoveAt(password.Length - 1);
                        Console.Write("\b \b");
                    }
                }
                else if (!char.IsControl(keyPressed.KeyChar))
                {
                    password.AppendChar(keyPressed.KeyChar);
                    Console.Write("*");
                }
            }
            return password;
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