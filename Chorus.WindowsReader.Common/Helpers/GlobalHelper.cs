using Chorus.WindowsReader.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Radios;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Chorus.WindowsReader.Common
{
    //Use appSettings.json
    public static class GlobalHelper
    {
        public static BeaconAppSettings BeaconAppSettings { get; set; }
        public static AppSettings AppSettings { get; set; }
        public static string LoggerFilesPath { get; set; }
        public static Dictionary<int, List<ushort>> Identifiers { get; set; }
        public static DateTimeOffset DeviceLocationTime_UTC { get; private set; } = DateTimeOffset.UtcNow;
        public static DateTimeOffset DeviceLocationTime_Now { get; private set; } = DateTimeOffset.Now;
        public const ushort ChorusCompanyId1 = 0x00E0;//0xE0 0x00  5-6 bytes offset
        private const ushort ChorusCompanyId2 = 0x0000;
        private const ushort ChorusForwarder17 = 0x0017;// Chorous forwarder 0x19, 0x17 or, 0x1A // 8th byte
        private const ushort ChorusAggregator18 = 0x0018;
        private const ushort ChorusExplorer19 = 0x0019;
        private const ushort ChorusSeeker1A = 0x001A;
        public static List<string> CompanyNames = new List<string>()
        {
            "Ruuvi Innovations Ltd.",
            "Chorus Beacon 1",
            "Chorus Beacon 2",
            "Chorus Forwarder 17",
            "Chorus Aggregator 18",
            "Chorus Explorer 19",
            "Chorus Seeker 1A"
        };
        private static string deviceId = null;

        private static string versionInfo = null;

        /// <summary>
        /// This version information will be fetch from app packages.
        /// </summary>
        public static string VersionInfo
        {
            get
            {
                if (string.IsNullOrWhiteSpace(versionInfo))
                {
                    PackageVersion version = Package.Current.Id.Version;
                    versionInfo = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
                    return versionInfo;
                }
                return versionInfo;
            }
        }
        /// <summary>
        /// This is the DeviceId  Gets or generates a unique identifier (GUID) for the device, stored locally in application settings.
        /// </summary>
        /// <returns></returns>
        public static string DeviceId
        {
            get
            {
                if (deviceId == null)
                {
                    ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                    if (localSettings.Values.ContainsKey("DeviceId"))
                    {
                        deviceId = localSettings.Values["DeviceId"].ToString(); // Retrieve Guid locally
                    }
                    else
                    {
                        deviceId = Guid.NewGuid().ToString();
                        localSettings.Values["DeviceId"] = deviceId; // Save Guid locally
                    }
                }
                return deviceId;
            }
        }

        /// <summary>
        /// Checks whether bluetooth is on or off.
        /// </summary>
        public static async Task CheckBluetoothStatus()
        {
            try
            {
                var radios = await Radio.GetRadiosAsync();
                var bluetoothRadio = radios.FirstOrDefault(radio => radio.Kind == RadioKind.Bluetooth);
                if (bluetoothRadio != null)
                {
                    bool isBluetoothOn = bluetoothRadio.State == RadioState.On;
                    if (!isBluetoothOn)
                    {
                        throw new Exception("Bluetooth is not enabled.");
                    }
                }
                else
                {
                    throw new Exception("Bluetooth not found.");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        /// <summary>
        /// Checks the OS is Windows 10 or 11.
        /// </summary>
        /// <returns>True if Os is Windows 10 or 11</returns>
        public static bool IsWindows10Or11()
        {
            try
            {
                string deviceFamilyVersion = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
                ulong version = ulong.Parse(deviceFamilyVersion);
                // Extract major and minor version numbers
                ulong major = (version & 0xFFFF000000000000L) >> 48;
                ulong minor = (version & 0x0000FFFF00000000L) >> 32;
                if (major == 10 && minor == 0 || major == 11)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
            }
            return false;
        }

        /// <summary>
        /// This is the setEnvironmentVarible
        /// </summary>
        /// <returns></returns>
        private static void SetEnvironmentVarible()
        {
            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            var testPath = "c:\\";
            cmd.StandardInput.WriteLine("setx testName \"" + testPath + "\"");
        }

        /// <summary>
        /// This is the ShowEnvironmentVariablePopup Shows a popup dialog informing the user that a required environment variable is not set,
        /// and exits the application.
        /// </summary>
        /// <returns></returns>
        public static async void ShowEnvironmentVariablePopup(Application app)
        {
            var dialog = new Windows.UI.Popups.MessageDialog("Environment variable 'OrganizationId' not set. Please set it and restart the application.");
            await dialog.ShowAsync();
            app.Exit();
        }

        /// <summary>
        ///  This is the SetBeaconIdentifiers Sets beacon identifiers from the application settings.
        /// </summary>
        /// <returns></returns>
        public static void SetBeaconIdentifiers()
        {
            if (AppSettings.Identifiers != null && AppSettings.Identifiers.Count > 0)
            {
                Identifiers = new Dictionary<int, List<ushort>>();
                foreach (var identifier in AppSettings.Identifiers.OrderBy(o => int.Parse(o.Key)))
                {
                    if (int.TryParse(identifier.Key, out _))
                    {
                        var values = new List<ushort>();
                        foreach (var strVal in identifier.Value.Split(","))
                        {
                            var hexaByte = Convert.ToByte(strVal.Substring(0, 2), 16);
                            if (ushort.TryParse(hexaByte.ToString(), out _))
                            {
                                values.Add(ushort.Parse(hexaByte.ToString()));
                            }
                        }
                        Identifiers.Add(int.Parse(identifier.Key), values);
                    }
                }
            }
        }

        /// <summary>
        /// Therefore, it is recommended to scan for only beacons that match 0xE0 0x00 (in the 5-6 byte offset) 
        /// and either 0x19, 0x17 or, 0x1A (in the 8th byte offset)
        /// </summary>
        /// <param name="manufacturerData"></param> 
        /// <returns></returns>
        public static string GetCompanyName(byte[] bytesData, BluetoothLEManufacturerData data, bool isFlagAbsent)
        {
            int isValidIdentifier = 0;
            int incrementalForOption2 = isFlagAbsent ? 0 : 1;

            foreach (var identifier in Identifiers)
            {
                if (bytesData.Length > (Convert.ToInt32(identifier.Key) + incrementalForOption2))
                {
                    bool isValid = identifier.Value.Any(v => bytesData[(Convert.ToInt32(identifier.Key) + incrementalForOption2)].ToString() == v.ToString());
                    if (isValid)
                        isValidIdentifier++;
                }
            }
            if (isValidIdentifier == Identifiers.Count)
            {
                return CompanyNames[1];// "Chorus Beacon";
            }
            return "Unknown";
        }

        /// <summary>
        /// This is the IncrementVersion Increments the build number in a version string formatted as "Major.Minor.Build".
        /// </summary>
        /// <param name="version">The version string to increment.</param>
        /// <returns>
        /// If the version string is in the format "Major.Minor.Build", increments the build number
        /// and returns the updated version string. Otherwise, returns the original version string unchanged.
        /// </returns>

        public static string IncrementVersion(string version)
        {
            var versionParts = version.Split('.');
            if (versionParts.Length == 3)
            {
                if (int.TryParse(versionParts[2], out int buildNumber))
                {
                    buildNumber++;
                    return $"{versionParts[0]}.{versionParts[1]}.{buildNumber}";
                }
            }
            return version;
        }

        /// <summary>
        ///  This is the HexStringToByteArray Converts a hexadecimal string representation to a byte array.
        /// </summary>
        /// <param name="hex">The hexadecimal string to convert.</param>
        /// <returns>A byte array converted from the hexadecimal input.</returns>
        /// <exception cref="ArgumentException"></exception>

        public static byte[] HexStringToByteArray(string hex)
        {
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                hex = hex.Substring(2);
            }
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have an even length");
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        /// <summary>
        /// This is the HexStringToFormattedByteString Converts a hexadecimal string representation to a formatted byte string.
        /// </summary>
        /// <param name="hex"></param>
        /// <returns>A formatted byte string in the format 'b'\xHH\xHH...''.</returns>
        /// <exception cref="ArgumentException">T</exception>

        public static string HexStringToFormattedByteString(string hex)
        {
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                hex = hex.Substring(2);
            }
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have an even length");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("b'");
            foreach (byte b in bytes)
            {
                sb.AppendFormat("\\x{0:X2}", b);
            }
            sb.Append("'");

            return sb.ToString();
        }

        /// <summary>
        /// This is the HexToUtf8String Converts a hexadecimal string representation to a UTF-8 encoded string.
        /// </summary>
        /// <param name="hex">The hexadecimal string to convert.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">.</exception>

        public static string HexToUtf8String(string hex)
        {
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                hex = hex.Substring(2);
            }

            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("Hex string must have an even length");
            }

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                string hexPair = hex.Substring(i, 2);
                bytes[i / 2] = Convert.ToByte(hexPair, 16);
            }

            return Encoding.UTF8.GetString(bytes);
        }
        /// <summary>
        /// This is the HexStringToByteArrayWithoutConversion Converts a byte array to a hexadecimal string representation.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns>A string containing hexadecimal representation of the byte array.</returns>

        public static byte[] HexStringToByteArrayWithoutConversion(string hex)
        {
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                hex = hex.Substring(2);
            }

            byte[] bytes = new byte[hex.Length];
            for (int i = 0; i < hex.Length; i++)
            {
                bytes[i] = (byte)hex[i];
            }

            return bytes;
        }
        /// <summary>
        /// This is the ByteArrayToHexString Converts a byte array to a hexadecimal string representation.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns>A hexadecimal string representation of the byte array.</returns>

        public static string ByteArrayToHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty);
        }

        /// <summary>
        /// This method will convert raw payload into readable format
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public static List<string> RawPayload(Payload payload)
        {
            byte[] byteArray = payload.BeaconPayloads[0].BleManufacturerData.ToByteArray();
            string hexString = GlobalHelper.ByteArrayToHexString(byteArray);
            var displayrawdata = GlobalHelper.HexStringToFormattedByteString(hexString);
            var displaypayload = new PayloadToDisplay
            {
                DeviceId = payload.DeviceId,
                OrgId = payload.OrgId,
                DeviceLocationTime = payload.DeviceLocationTime,
            };
            displaypayload.BeaconPayloads = new List<WindowsReader.Common.Models.BeaconPayload>()
                    {
                        new WindowsReader.Common.Models.BeaconPayload
                        {
                            BleManufacturerData = displayrawdata.ToLower(),
                            ReceiveTime = payload.BeaconPayloads[0].ReceiveTime,
                            RssiDbm =payload.BeaconPayloads[0].RssiDbm
                        }
                    };
            displaypayload.DeviceLocation = new DerivedLocation
            {
                AccuracyCm = payload.DeviceLocation.AccuracyCm,
                Point = payload.DeviceLocation.Point,
                Source = payload.DeviceLocation.Source,
            };
            displaypayload.MetaData = new WindowsReader.Common.Models.PayloadMetaData
            {
                Type = WindowsReader.Common.Models.DeviceType.WINDOWS,
                Version = GlobalHelper.VersionInfo
            };
            return new List<string>() { JsonConvert.SerializeObject(displaypayload).Replace("\\\\", "\\") };
            new List<string>();
        }
    }
}
