using System;

namespace Chorus.WindowsReader.Common.Models
{
    public class Beacon
    {
        /// <summary>
        /// This is the CompanyName.
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// This is the Address.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// This is the Rssi.
        /// </summary>
        public short Rssi { get; set; }

        /// <summary>
        /// This is the AdvertisementMessage.
        /// </summary>
        public string AdvertisementMessage { get; set; }

        /// <summary>
        /// This is the Temperature.
        /// </summary>
        public string Temperature { get; set; }

        /// <summary>
        /// This is the Humidity.
        /// </summary>
        public string Humidity { get; set; }

        /// <summary>
        /// This is the Pressure.
        /// </summary>
        public string Pressure { get; set; }

        /// <summary>
        /// This is the Location.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// This is the RawData.
        /// </summary>
        public string RawData { get; set; }

        /// <summary>
        /// This is the UTCTime.
        /// </summary>
        public DateTimeOffset UTCTime { get; set; }

        /// <summary>
        /// This is the LocalTime.
        /// </summary>
        public DateTimeOffset LocalTime { get; set; }

    }
}
