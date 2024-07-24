namespace Chorus.WindowsReader.Common.Models
{
    /// <summary>
    /// This class is responsible to hold Location related data of Latitude, Longitude and Source.
    /// </summary>
    public class LocationData
    {
        /// <summary>
        /// This is the Latitude.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// This is the Longitude.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// This is the Source.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// This is the ToString.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Latitude: {Latitude}, Longitude: {Longitude}";
        }
    }
}
