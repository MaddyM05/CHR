using System;

namespace Chorus.WindowsReader.Common
{
    public class BeaconAppSettings
    {
        /// <summary>
        /// This is the OrganizationName.
        /// </summary>
        public string OrganizationName { get; set; }

        /// <summary>
        /// This is the OrganizationId.
        /// </summary>
        public string OrganizationId { get; set; }

        /// <summary>
        /// This is the DeviceId.
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// This is the BeaconUid.
        /// </summary>
        public int BeaconUid { get; set; }

        /// <summary>
        /// This is the Power.
        /// </summary>
        public int Power { get; set; }

        /// <summary>
        /// This is the Rsi.
        /// </summary>
        public int Rsi { get; set; }

        //if required
        /// <summary>
        /// This is the Uuid.
        /// </summary>
        public String Uuid { get; set; }

        /// <summary>
        /// This is the Major.
        /// </summary>
        public int Major { get; set; }

        /// <summary>
        /// This is the Minor.
        /// </summary>
        public int Minor { get; set; }

        /// <summary>
        /// This is the AdvertisementPerSecond.
        /// </summary>
        public int AdvertisementPerSecond { get; set; }

        /// <summary>
        /// This is the TransmitterPower.
        /// </summary>
        public int TransmitterPower { get; set; }

    }
}
