using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarvelousAPI
{
    class Modem
    {
        #region Private
        private const byte ModemAddress = 0xff;
        #endregion
        #region Public
        public List<Beacon> Beacons { get; set; }
        public byte MajorVersion { get; set; }
        public byte MinorVersion { get; set; }

        public async Task GetFirmwareVersion(SerialPortConnection connection)
        {
            Request_03_FE00 packet_03_fe00 = new()
            {
                DeviceAddress = ModemAddress
            };
            connection.Write(packet_03_fe00.ToBuffer());
            await Task.Delay(15000);
        }

        public async Task GetLastCoordinates(SerialPortConnection connection)
        {
            Request_03_4110 packet_03_4110 = new();
            connection.Write(packet_03_4110.ToBuffer());
            await Task.Delay(15000);
        }

        public async Task GetAvailableBeacons(SerialPortConnection connection, byte groupnumber)
        {
            Request_03_31XX packet_03_31xx = new() { GroupNumber = groupnumber };
            connection.Write(packet_03_31xx.ToBuffer());
            await Task.Delay(15000);
        }

        public Modem()
        {
            Beacons = new List<Beacon>();
            for (byte i = 0x00; i < ModemAddress; i++) Beacons.Add(new Beacon()
            {
                Coordinates_cm = new CoordinatesStructure_cm { X = 0, Y = 0, Z = 0 },
                Coordinates_mm = new CoordinatesStructure_mm { X = 0, Y = 0, Z = 0 },
                isAwake = false,
                isHedge = false,
                Number = i,
                Exists = false
            });
        }
        #endregion
    }
}
