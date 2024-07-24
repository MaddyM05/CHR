// Ignore Spelling: Org

using Google.Protobuf.WellKnownTypes;
using System.Collections.Generic;

namespace Chorus.WindowsReader.Common.Models
{
    public enum DeviceType
    {
        UNKNOWN_DEVICE_TYPE = 0,
        ANDROID = 1,
        IOS = 2,
        WINDOWS = 3
    }

    /// <summary>
    /// this is the PayloadToDisplay.
    /// </summary>
    public class PayloadToDisplay
    {
        public string DeviceId { get; set; }
        public string OrgId { get; set; }
        public DerivedLocation DeviceLocation { get; set; }
        public Timestamp DeviceLocationTime { get; set; }
        public List<BeaconPayload> BeaconPayloads { get; set; }
        public PayloadMetaData MetaData { get; set; }

        public PayloadToDisplay()
        {
            BeaconPayloads = new List<BeaconPayload>();
            MetaData = new PayloadMetaData();
        }
    }

    /// <summary>
    /// This is the BeaconPayload.
    /// </summary>
    public class BeaconPayload
    {
        public string BleManufacturerData { get; set; }
        public Timestamp ReceiveTime { get; set; }
        public int RssiDbm { get; set; }
        // Reserved field 4 is omitted as it's not explicitly defined
    }

    /// <summary>
    /// This is the PayloadMetadata. 
    /// </summary>
    public class PayloadMetaData
    {
        public DeviceType Type { get; set; }
        public string Version { get; set; }
    }
}
