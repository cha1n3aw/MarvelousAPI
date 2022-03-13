using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarvelousAPI
{
    public struct Data_03_4110_Structure
    {
        public byte BeaconAddress;
        public CoordinatesStructure_mm Coordinates_mm;
        public bool no_relevant_coordinates;
        public bool temporary_mobile_beacon;
        public bool beacon_is_used_for_positioning;
    }

    public struct Data_47_0002_Structure
    {
        public byte BeaconAddress;
        public CoordinatesStructure_cm Coordinates_cm;
    }

    public struct Data_47_0012_Structure
    {
        public byte BeaconAddress;
        public CoordinatesStructure_mm Coordinates_mm;
        public bool LocationApplicable;
    }

    public struct CoordinatesStructure_cm
    {
        public Int16 X;
        public Int16 Y;
        public Int16 Z;
    }

    public struct CoordinatesStructure_mm
    {
        public Int32 X;
        public Int32 Y;
        public Int32 Z;
    }

    class IncomingPacket
    {
        #region Public
        public byte Address { get { return PickBytes(0, 1); } }
        public byte PacketType { get { return PickBytes(1, 1); } }
        public UInt16 PacketCode { get { return PickBytes(2, 2); } }
        public byte DataLength { get { return PickBytes(4, 1); } }
        public UInt16 CRC { get { return PickBytes(buffer.Length - 2, 2); } }
        public bool ValidCRC { get { return ValidateCRC(CRC); } }

        public bool ValidateCRC(UInt16 CRC)
        {
            UInt16 crc = 0xFFFF;
            for (int pos = 0; pos < buffer.Length - 2; pos++)
            {
                crc ^= (UInt16)buffer[pos];
                for (int i = 8; i != 0; i--)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else crc >>= 1;
                }
            }
            if (crc == CRC) return true;
            else return false;
        }

        public T CastPacket<T>() where T : IncomingPacket
        {
            return Activator.CreateInstance(typeof(T), new object[] { buffer }) as T;
        }

        public IncomingPacket(byte[] buffer)
        {
            this.buffer = buffer;
        }
        #endregion
        #region Protected
        protected byte[] buffer;

        protected dynamic PickBytes(int offset, int length)
        {
            if (buffer != null && buffer.Length >= 4)
            {
                switch (length)
                {
                    case 1: { return buffer[offset]; }
                    case 2: { return (UInt16)((int)buffer[offset + 1] << 8 | (int)buffer[offset]); } //CAST IS NOT REDUNDANT, DO NOT REMOVE!
                    case 4: { return (UInt32)((int)buffer[offset + 3] << 24 | (int)buffer[offset + 2] << 16 | (int)buffer[offset + 1] << 8 | (int)buffer[offset]); } //there are no bitwise operations on bytes, only on ints
                    case -2: { return (Int16)((int)buffer[offset + 1] << 8 | (int)buffer[offset]); } //CAST IS NOT REDUNDANT, DO NOT REMOVE!
                    case -4: { return (Int32)((int)buffer[offset + 3] << 24 | (int)buffer[offset + 2] << 16 | (int)buffer[offset + 1] << 8 | (int)buffer[offset]); } //there are no bitwise operations on bytes, only on ints
                    default: return 0x00;
                }
            }
            else return 0x00;
        }
        protected dynamic PickBytes(byte[] buffer, int offset, int length)
        {
            switch(length)
            {
                case 1: { return buffer[offset]; }
                case 2: { return (UInt16)((int)buffer[offset + 1] << 8 | (int)buffer[offset]); } //CAST IS NOT REDUNDANT, DO NOT REMOVE!
                case 4: { return (UInt32)((int)buffer[offset + 3] << 24 | (int)buffer[offset + 2] << 16 | (int)buffer[offset + 1] << 8 | (int)buffer[offset]); } //there are no bitwise operations on bytes, only on ints
                case -2: { return (Int16)((int)buffer[offset + 1] << 8 | (int)buffer[offset]); } //CAST IS NOT REDUNDANT, DO NOT REMOVE!
                case -4: { return (Int32)((int)buffer[offset + 3] << 24 | (int)buffer[offset + 2] << 16 | (int)buffer[offset + 1] << 8 | (int)buffer[offset]); } //there are no bitwise operations on bytes, only on ints
            }
            return 0x00;
        }
        #endregion
    }

    //receive hedge coordinates in cm
    class Incoming_47_0001 : IncomingPacket
    {
        #region Public
        public UInt32 Timestamp { get { return PickBytes(5, 4); } }
        public CoordinatesStructure_cm Coordinates_cm { get { return new CoordinatesStructure_cm { X = PickBytes(9, -2), Y = PickBytes(11, -2), Z = PickBytes(13, -2) }; } }
        public bool CoordinatesAvailable { get { return (buffer[15] & 0b00000001) == 0; } }
        public bool FirstUserButtonIsPushed { get { return (buffer[15] & 0b00000100) == 1; } }
        public bool DataAvailableForUpload { get { return (buffer[15] & 0b00001000) == 1; } }
        public bool DataDownloadRequested { get { return (buffer[15] & 0b00010000) == 1; } }
        public bool SecondUserButtonIsPushed { get { return (buffer[15] & 0b00100000) == 1; } }
        public bool DataForAnotherHedge { get { return (buffer[15] & 0b01000000) == 1; } }
        public byte HedgeAddress { get { return PickBytes(16, 1); } }
        public UInt16 Orientation { get { return PickBytes(17, 2); } }
        public UInt16 SeTimePassed { get { return PickBytes(19, 2); } }

        public Incoming_47_0001(byte[] buffer) : base(buffer)
        {

        }
        #endregion
    }

    //receive hedge coordinates in mm
    class Incoming_47_0011 : IncomingPacket
    {
        #region Public
        public UInt32 Timestamp { get { return PickBytes(5, 4); } }
        public CoordinatesStructure_mm Coordinates_mm { get { return new CoordinatesStructure_mm { X = PickBytes(9, -4), Y = PickBytes(13, -4), Z = PickBytes(17, -4) }; } }
        public bool CoordinatesAvailable { get { return (buffer[21] & 0b00000001) == 0; } }
        public bool FirstUserButtonIsPushed { get { return (buffer[21] & 0b00000100) == 1; } }
        public bool DataAvailableForUpload { get { return (buffer[21] & 0b00001000) == 1; } }
        public bool DataDownloadRequested { get { return (buffer[21] & 0b00010000) == 1; } }
        public bool SecondUserButtonIsPushed { get { return (buffer[21] & 0b00100000) == 1; } }
        public bool DataForAnotherHedge { get { return (buffer[21] & 0b01000000) == 1; } }
        public byte HedgeAddress { get { return PickBytes(22, 1); } }
        public UInt16 Orientation { get { return PickBytes(23, 2); } }
        public UInt16 SeTimePassed { get { return PickBytes(25, 2); } }

        public Incoming_47_0011(byte[] buffer) : base(buffer)
        {
            
        }
        #endregion
    }

    //all beacon's coordinates in cm, received every 10 sec
    class Incoming_47_0002 : IncomingPacket
    {
        #region Public
        public byte NumberOfBeacons { get { return PickBytes(5, 1); } }
        public Data_47_0002_Structure[] Data_Structure_cm { get { return ParseDataStructure(); } } 

        public Incoming_47_0002(byte[] buffer) : base(buffer)
        {

        }
        #endregion
        #region Private
        private Data_47_0002_Structure[] ParseDataStructure()
        {
            Data_47_0002_Structure [] coordinates = new Data_47_0002_Structure[NumberOfBeacons];
            for (int i = 0; i < NumberOfBeacons; i++)
            {
                coordinates[i].BeaconAddress = buffer[6 + i * 8];
                coordinates[i].Coordinates_cm.X = PickBytes(buffer, 7 + i * 8, -2);
                coordinates[i].Coordinates_cm.Y = PickBytes(buffer, 7 + i * 8 + 2, -2);
                coordinates[i].Coordinates_cm.Z = PickBytes(buffer, 7 + i * 8 + 4, -2);
            }
            return coordinates;
        }
        #endregion
    }

    //all beacon's coordinates in mm, received every 10 sec
    class Incoming_47_0012 : IncomingPacket
    {
        #region Public
        public byte NumberOfBeacons { get { return PickBytes(5, 1); } }
        public Data_47_0012_Structure[] Data_Structure_mm { get { return ParseDataStructure(); } }

        public Incoming_47_0012(byte[] buffer) : base(buffer)
        {

        }
        #endregion
        #region Private
        private Data_47_0012_Structure[] ParseDataStructure()
        {
            Data_47_0012_Structure[] coordinates = new Data_47_0012_Structure[NumberOfBeacons];
            for (int i = 0; i < NumberOfBeacons; i++)
            {
                coordinates[i].BeaconAddress = buffer[6 + i * 14];
                coordinates[i].Coordinates_mm.X = PickBytes(buffer, 7 + i * 14, -4);
                coordinates[i].Coordinates_mm.Y = PickBytes(buffer, 7 + i * 14 + 4, -4);
                coordinates[i].Coordinates_mm.Z = PickBytes(buffer, 7 + i * 14 + 8, -4);
                coordinates[i].LocationApplicable = (buffer[6 + i * 14 + 9] & 0b00000001) == 0;
            }
            return coordinates;
        }
        #endregion
    }

    #region TODO
    class Reply_47_03 : IncomingPacket //Raw IMU data
    {
        public Reply_47_03(byte[] buffer) : base(buffer)
        {

        }
    }

    class Reply_47_04 : IncomingPacket //Raw distances (need to be enabled in settings => interfaces)
    {
        public Reply_47_04(byte[] buffer) : base(buffer)
        {

        }
    }

    class Reply_47_05 : IncomingPacket //IMU data
    {
        public Reply_47_05(byte[] buffer) : base(buffer)
        {

        }
    }

    class Reply_47_06 : IncomingPacket //Telemetry data (need to be enabled via option Telemetry stream)
    {
        public Reply_47_06(byte[] buffer) : base(buffer)
        {

        }
    }

    class Reply_47_07 : IncomingPacket //Positioning quality and extended location data (need to be enabled via option Quality and extended location data)
    {
        public Reply_47_07(byte[] buffer) : base(buffer)
        {

        }
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //answer on wake request, derived class is empty because there no unique values are received
    class Answer_10_b006 : IncomingPacket
    {
        #region Public
        public Answer_10_b006(byte[] buffer) : base(buffer)
        {

        }
        #endregion
    }

    //answer on sleep request
    class Answer_7f_b006 : IncomingPacket
    {
        #region BAD_DOC
        /*
        documentation is WRONG, accessing this will result in "index was outside of bounds"

            public byte DeviceAddress { get { return PickBytes(1, 8); } } 
            public byte PacketTypeDevice { get { return PickBytes(1, 9); } } 
            public new UInt16 PacketCode { get { return PickBytes(2, 10); } }

        maybe some fixes will be applied to the documentation in further updates
        */
        #endregion
        #region Public
        public new bool ValidCRC { get { return ValidateCRC(CRC); } } //and its intended to override crc validation

        public Answer_7f_b006(byte[] buffer) : base(buffer)
        {

        }
        #endregion
        #region Private
        private new UInt16 CRC { get { return PickBytes(6, 2); } } //CRC bytes are not the last ones (at least thats said in documentation)

        private new bool ValidateCRC(UInt16 CRC)
        {
            UInt16 crc = 0xFFFF;
            for (int pos = 0; pos < 6; pos++)
            {
                crc ^= (UInt16)buffer[pos];
                for (int i = 8; i != 0; i--)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else crc >>= 1;
                }
            }
            if (crc == CRC) return true;
            else return false;
        }
        #endregion
    }

    //answer on coordinates pack request
    class Answer_03_4110 : IncomingPacket
    {
        #region Public
        public new byte DataLength { get { return PickBytes(2, 1); } }
        public bool UserDataAvailaible { get { return (PickBytes(96, 1) & 0b00000100) == 1; } } //pick 3rd lsb, others are reserved
        public Data_03_4110_Structure[] DataStructure { get { return ParseDataStructure(); } }
        public Answer_03_4110(byte[] buffer) : base(buffer)
        {

        }
        #endregion
        #region Private
        private Data_03_4110_Structure[] ParseDataStructure()
        {
            Data_03_4110_Structure[] coordinates = new Data_03_4110_Structure[6];
            byte[] data = buffer.Skip(3).Take(100).ToArray();
            for (int i = 0; i < 6; i++)
            {
                coordinates[i].BeaconAddress = data[i * 16];
                coordinates[i].Coordinates_mm.X = PickBytes(data.Skip(i * 16).Take(4).ToArray(), 0, 4);
                coordinates[i].Coordinates_mm.Y = PickBytes(data.Skip(i * 16 + 4).Take(4).ToArray(), 0, 4);
                coordinates[i].Coordinates_mm.Z = PickBytes(data.Skip(i * 16 + 8).Take(4).ToArray(), 0, 4);
                coordinates[i].no_relevant_coordinates = (data[i * 16 + 13] & 0b00000001) == 1;
                coordinates[i].temporary_mobile_beacon = (data[i * 16 + 13] & 0b00000010) == 1;
                coordinates[i].beacon_is_used_for_positioning = (data[i * 16 + 13] & 0b00000100) == 1;
            }
            return coordinates;
        }
        #endregion
    }

    //answer with error code
    class Answer_Error : IncomingPacket
    {
        #region Public
        public byte ErrorCode { get { return PickBytes(2, 1); } }
        public new byte PacketCode { get { return (byte)((int)PickBytes(1, 1) & 0b01111111); } }

        public string GetErrorContents()
        {
            string errorContents = string.Empty;
            switch (ErrorCode)
            {
                case 0x01:
                    errorContents = "Unknown type of packet in request";
                    break;
                case 0x02:
                    errorContents = "Unknown code of data in request";
                    break;
                case 0x03:
                    errorContents = "Error in data field of request";
                    break;
                case 0x06:
                    errorContents = "Device is busy";
                    break;
                case 0x0A:
                    errorContents = "Error message from remote device";
                    break;
                case 0x0B:
                    errorContents = "Timeout of reply from remote device";
                    break;
            }
            return errorContents;
        }

        public Answer_Error(byte[] buffer) : base(buffer)
        {

        }
        #endregion
    }

    /// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    abstract class RequestPacket
    {
        #region Public
        public byte DeviceAddress { get; set; }
        public UInt16 AccessMode { get; set; }
        public abstract byte[] ToBuffer();

        public RequestPacket()
        {
            
        }
        #endregion
        #region Protected
        protected UInt16 CRC { get; set; }
        protected byte PacketType { get; set; }
        protected UInt16 PacketCode { get; set; }
        protected byte DataLength { get; set; }

        protected UInt16 CalculateCRC(byte[] _buffer)
        {
            UInt16 crc = 0xFFFF;
            for (int pos = 0; pos < _buffer.Length - 2; pos++)
            {
                crc ^= (UInt16)_buffer[pos];
                for (int i = 8; i != 0; i--)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else crc >>= 1;
                }
            }
            return crc;
        }
        #endregion
    }

    //wake up beacon or put it into sleep mode
    class Request_10_B006 : RequestPacket
    {
        //access modes: 0x0002 to wake, 0x0001 to sleep
        //commands: 0 to sleep, 1 to deep sleep, 2 to wake
        #region Private                                 
        private byte[] PasswordBytes { get; set; }
        private byte[] Reserved { get; set; }
        #endregion
        #region Public
        public byte Command { get; set; }
        public override byte[] ToBuffer()
        {
            byte[] Buffer = new byte[]
            {
                DeviceAddress,
                PacketType,
                (byte)PacketCode,
                (byte)(PacketCode >> 8),
                (byte)AccessMode,
                (byte)(AccessMode >> 8),
                DataLength,
                PasswordBytes[0],
                PasswordBytes[1],
                PasswordBytes[2],
                PasswordBytes[3],
                Command,
                Reserved[0],
                Reserved[1],
                Reserved[2],
                0xFF, //crc_b1
                0xFF //crc_b2
            };
            CRC = CalculateCRC(Buffer);
            Buffer[^2] = (byte)CRC;
            Buffer[^1] = (byte)(CRC >> 8);
            return Buffer;
        }
        public Request_10_B006() : base()
        {
            PacketType = 0x10;
            PacketCode = 0xb006;
            DataLength = 0x08;
            PasswordBytes = new byte[] { 0x2d, 0x94, 0x5e, 0x81 };
            Reserved = new byte[] { 0x00, 0x00, 0x00 };
        }
        #endregion
    }

    //get last coordinates pack, method has no input args
    class Request_03_4110 : RequestPacket
    {
        #region Public
        public override byte[] ToBuffer()
        {
            byte[] Buffer = new byte[]
            {
                DeviceAddress,
                PacketType,
                (byte)PacketCode,
                (byte)(PacketCode >> 8),
                (byte)AccessMode,
                (byte)(AccessMode >> 8),
                0xFF, //crc_b1
                0xFF //crc_b2
            };
            CRC = CalculateCRC(Buffer);
            Buffer[^2] = (byte)CRC;
            Buffer[^1] = (byte)(CRC >> 8);
            return Buffer;
        }
        public Request_03_4110() : base()
        {
            DeviceAddress = 0xff;
            PacketType = 0x03;
            PacketCode = 0x4110;
            AccessMode = 0x0000;
        }
        #endregion
    }

    //get firmware version of the device (beacon/modem), device address is the only arg
    class Request_03_FE00 : RequestPacket
    {
        #region Public
        public override byte[] ToBuffer()
        {
            byte[] Buffer = new byte[]
            {
                DeviceAddress,
                PacketType,
                (byte)PacketCode,
                (byte)(PacketCode >> 8),
                (byte)AccessMode,
                (byte)(AccessMode >> 8),
                0xFF, //crc_b1
                0xFF //crc_b2
            };
            CRC = CalculateCRC(Buffer);
            Buffer[^2] = (byte)CRC;
            Buffer[^1] = (byte)(CRC >> 8);
            return Buffer;
        }
        public Request_03_FE00() : base()
        {
            PacketType = 0x03;
            PacketCode = 0xfe00;
            AccessMode = 0x0000;
        }
        #endregion
    }
}
