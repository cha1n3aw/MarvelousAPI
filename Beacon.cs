using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarvelousAPI
{
    class Beacon
    {
        #region Public
        public byte Number { get; set; }
        public CoordinatesStructure_cm Coordinates_cm { get; set; }
        public CoordinatesStructure_mm Coordinates_mm { get; set; }
        public bool isHedge { get; set; }
        public bool isAwake { get; set; }

        public async Task GetFirmwareVersion(SerialPortConnection connection)
        {
            Request_03_FE00 packet_03_fe00 = new()
            {
                DeviceAddress = Number
            };
            connection.Write(packet_03_fe00.ToBuffer());
            await Task.Delay(15000);
        }

        public async Task WakeUp(SerialPortConnection connection)
        {
            Request_10_B006 packet_10_B006 = new Request_10_B006()
            {
                AccessMode = 0x0002, //0x0001 to sleep, 0x0002 to wake
                Command = 0x02, //0x00 sleep, 0x01 deep sleep, 0x02 wake
                DeviceAddress = Number
            };
            connection.Write(packet_10_B006.ToBuffer());
            await Task.Delay(15000);
        }

        public async Task Sleep(SerialPortConnection connection)
        {
            Request_10_B006 packet_10_B006 = new Request_10_B006()
            {
                AccessMode = 0x0001, //0x0001 to sleep, 0x0002 to wake
                Command = 0x00, //0x00 sleep, 0x01 deep sleep, 0x02 wake
                DeviceAddress = Number
            };
            connection.Write(packet_10_B006.ToBuffer());
            await Task.Delay(15000);
        }

        public Beacon()
        {

        }
        #endregion
    }
}
