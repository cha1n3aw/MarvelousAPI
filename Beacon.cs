using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MarvelousAPI
{
    #region Public
    public class Submap
    {
        public int ID { get; set; }
        [JsonIgnore]
        public string Name { get { return $"Submap { ID }"; } }
    }

    public class BeaconSettings
    {
        public byte ID { get; set; }
        public bool isHedge { get; set; }
        public bool isAwake { get; set; }
        public string FirmwareVersion { get; set; }
        public int UartBaud { get; set; }
        public int RadioProfile { get; set; }
        public int RadioBand { get; set; }
        public int Submap { get; set; }
        public bool Exists { get; set; }
    }
    #endregion

    [Serializable]
    public class Beacon
    {
        #region Public
        public CoordinatesStructure_mm Coordinates_mm { get; set; }
        public BeaconSettings Settings { get; set; }
        [JsonIgnore]
        public string Name { get { return $"Beacon { Settings.ID }"; } }

        public async Task GetFirmwareVersion(SerialPortConnection connection)
        {
            Request_03_FE00 packet_03_fe00 = new()
            {
                DeviceAddress = Settings.ID
            };
            connection.Write(packet_03_fe00.ToBuffer());
            await Task.Delay(1000);
        }

        public async Task WakeUp(SerialPortConnection connection)
        {
            Request_10_B006 packet_10_B006 = new Request_10_B006()
            {
                AccessMode = 0x0002, //0x0001 to sleep, 0x0002 to wake
                Command = 0x02, //0x00 sleep, 0x01 deep sleep, 0x02 wake
                DeviceAddress = Settings.ID
            };
            connection.Write(packet_10_B006.ToBuffer());
            await Task.Delay(1000);
        }

        public async Task Sleep(SerialPortConnection connection)
        {
            Request_10_B006 packet_10_B006 = new Request_10_B006()
            {
                AccessMode = 0x0001, //0x0001 to sleep, 0x0002 to wake
                Command = 0x00, //0x00 sleep, 0x01 deep sleep, 0x02 wake
                DeviceAddress = Settings.ID
            };
            connection.Write(packet_10_B006.ToBuffer());
            await Task.Delay(1000);
        }

        public Beacon()
        {

        }
        #endregion
    }
}
