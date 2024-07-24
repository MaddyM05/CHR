using System.Collections.Generic;

namespace Chorus.WindowsReader.Common
{
    public class AppSettings
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
        /// Determines Is Log Required or not.
        /// </summary>
        public bool IsLogRequired { get; set; }


        /// <summary>
        /// This is the EnableFileLog.
        /// </summary>
        public bool EnableFileLog { get; set; }

        /// <summary>
        /// This is the FileSizeLimitBytes.
        /// </summary>
        public long FileSizeLimitBytes { get; set; }

        /// <summary>
        /// This is the MaxRollingFiles.
        /// </summary>
        public int MaxRollingFiles { get; set; }

        /// <summary>
        /// This is the MaxRollingFiles.
        /// </summary>
        public int ProcessTaskTimeout { get; set; }

        /// <summary>
        /// This is the ApiEndpointForProto.
        /// </summary>
        public string ApiEndpointForProto { get; set; }

        /// <summary>
        /// This is the ApiEndpointForProto.
        /// </summary>
        public string ApiEndpointForJson { get; set; }

        /// <summary>
        /// This is the UploadThroughJson.
        /// </summary>
        public bool UploadThroughJson { get; set; }

        /// <summary>
        /// This will log payloads as well with exceptions into log files.
        /// </summary>
        public bool LogPayloads { get; set; }

        /// <summary>
        /// This is the PayloadHexString.
        /// </summary>
        public bool PayloadHexString { get; set; }

        /// <summary>
        /// This is the ScanTime in seconds.
        /// </summary>
        public int ScanTime { get; set; }

        /// <summary>
        /// This is the AppKillTime in seconds.
        /// </summary>
        public int AppKillTime { get; set; }

        /// <summary>
        /// This is IsAutoKillRequired determines if auto kill function required or not.
        /// </summary>
        public bool IsAutoKillRequired { get; set; }

        /// <summary>
        /// Gets or sets the dictionary of identifiers associated with an object.
        /// </summary>
        public Dictionary<string, string> Identifiers { get; set; }
    }
}
