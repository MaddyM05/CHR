using Chorus.WindowsReader.Common;
using Chorus.WindowsReader.Common.Helpers;
using Chorus.WindowsReader.Common.Logger;
using Chorus.WindowsReader.Common.Models;
using Chorus.WindowsReader.Common.Services;
using Chorus.WindowsReader.Ingestion;
using Chorus.WindowsReaderApp.Dialogs.Views;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using static Chorus.WindowsReader.Common.DerivedLocation.Types;

namespace Chorus.WindowsReaderApp.ViewModels
{
    public class HomePageViewModel : ViewModelBase
    {
        #region Properties

        private int countdown;
        private bool isTimerRunning;
        private readonly IChorusLogger<HomePageViewModel> _logger;
        private Dictionary<string, DateTime> beaconUploadInfo = new Dictionary<string, DateTime>();
        private LocationData LocationData;
        private LocationService LocationServices;
        private ObservableCollection<Beacon> _filteredBeacons;
        private BluetoothLEAdvertisementWatcher watcher;
        private CoreDispatcher _dispatcher;
        private DispatcherTimer countTimer;
        private DispatcherTimer locationTimer;
        private DispatcherTimer uploadPayloadTimer;
        TagDetailsDialog dialogPopup;

        private ObservableCollection<(Payload, string)> _payloadResponses = new ObservableCollection<(Payload, string)>();
        public ObservableCollection<(Payload, string)> PayloadResponses
        {
            get { return _payloadResponses; }
            set => SetProperty(ref _payloadResponses, value);
        }

        public ObservableCollection<Beacon> FilteredBeacons
        {
            get { return _filteredBeacons; }
            set => SetProperty(ref _filteredBeacons, value);
        }

        private (Payload, string) _selectedPayload;
        public (Payload, string) SelectedPayload
        {
            get { return _selectedPayload; }
            set => SetProperty(ref _selectedPayload, value);
        }

        private Beacon _selectedBeacon;
        public Beacon SelectedBeacon
        {
            get { return _selectedBeacon; }
            set => SetProperty(ref _selectedBeacon, value);
        }

        private ObservableCollection<Beacon> _beacons;
        public ObservableCollection<Beacon> Beacons
        {
            get { return _beacons; }
            set => SetProperty(ref _beacons, value);
        }

        private List<Beacon> _beaconsToUpload;
        public List<Beacon> BeaconsToUpload
        {
            get { return _beaconsToUpload; }
            set => SetProperty(ref _beaconsToUpload, value);
        }

        private string _macFilterText;
        public string MacFilterText
        {
            get { return _macFilterText; }
            set => SetProperty(ref _macFilterText, value);
        }

        private string _companyFilterText;
        public string CompanyFilterText
        {
            get { return _companyFilterText; }
            set => SetProperty(ref _companyFilterText, value);
        }

        private string _coordinatesText = "Latitude is loading...";
        public string CoordinatesText
        {
            get { return _coordinatesText; }
            set => SetProperty(ref _coordinatesText, value);
        }

        private string _bleTimerTextBlock = "00:00";
        public string BleTimerTextBlock
        {
            get { return _bleTimerTextBlock; }
            set => SetProperty(ref _bleTimerTextBlock, value);
        }

        private string _ScanBlock = "";
        public string ScanBlock
        {
            get { return _ScanBlock; }
            set => SetProperty(ref _ScanBlock, value);
        }

        private string _locationSourceText = "Location Fetching from ...";
        public string LocationSourceText
        {
            get { return _locationSourceText; }
            set => SetProperty(ref _locationSourceText, value);
        }

        private bool _isDeviceAvailable;
        public bool IsDeviceAvailable
        {
            get { return _isDeviceAvailable; }
            set => SetProperty(ref _isDeviceAvailable, value);
        }

        private bool _isButtonEnabled = true; // Initial state: enabled
        public bool IsButtonEnabled
        {
            get { return _isButtonEnabled; }
            set { SetProperty(ref _isButtonEnabled, value); }
        }

        public AsyncRelayCommand StartScanCommand => new AsyncRelayCommand(StartScan);
        public RelayCommand StopScanCommand => new RelayCommand(StopScan);
        public AsyncRelayCommand ShowLogsCommand => new AsyncRelayCommand(ShowLogs);

        #endregion

        /// <summary>
        /// Starts scanning process.
        /// </summary>
        public async Task StartScan()
        {
            try
            {
                await GlobalHelper.CheckBluetoothStatus();
                LocationData = await LocationServices.GetCoordinatesAsync();

                Beacons.Clear();
                FilteredBeacons.Clear();
                PayloadResponses.Clear();
                watcher.Start();
                locationTimer.Start();
                IsButtonEnabled = false;
                ScanBlock = "Scanning...";

                countdown = GlobalHelper.AppSettings.ScanTime;
                BleTimerTextBlock = $"00:{countdown:D2}";

                countTimer.Start();
                _logger.LogInfo($"Start Scan");

                await Task.Delay(TimeSpan.FromSeconds(countdown + 1));

                StopScan();
            }
            catch (Exception Ex)
            {
                _logger.LogException(ChorusEventType.ERROR, new ChorusCustomException("Error while starting scan", Ex));
                await MessageDialogService.ShowMessageAsync(Ex.Message, "Issue Occurred");
            }
        }

        /// <summary>
        /// Stops Scanning.
        /// </summary>
        private void StopScan()
        {
            countTimer.Stop();
            _logger.LogInfo($"Stop Scan");

            if (dialogPopup != null)
            {
                dialogPopup.Hide();
                dialogPopup = null;
            }

            Beacons.Clear();
            FilteredBeacons.Clear();
            PayloadResponses.Clear();
            watcher.Stop();
            locationTimer.Stop();
            IsButtonEnabled = true;
            ScanBlock = "Scanning stopped.";
        }

        /// <summary>
        /// Opens up Logs folder.
        /// </summary>
        private async Task ShowLogs()
        {
            if (!string.IsNullOrWhiteSpace(GlobalHelper.LoggerFilesPath))
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(GlobalHelper.LoggerFilesPath);
                if (folder != null)
                {
                    await Launcher.LaunchFolderAsync(folder);
                }
            }
        }

        #region Constructor
        public HomePageViewModel(IChorusLogger<HomePageViewModel> logger, CoreDispatcher dispatcher)
        {
            _logger = logger;
            LocationServices = new LocationService(App.ServiceProvider.GetRequiredService<IChorusLogger<LocationService>>());
            Beacons = new ObservableCollection<Beacon>();
            FilteredBeacons = new ObservableCollection<Beacon>();
            watcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };
            watcher.Received += OnAdvertisementReceived;
            _dispatcher = dispatcher;
            locationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            locationTimer.Tick += LocationTimer_Tick;
            uploadPayloadTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            uploadPayloadTimer.Tick += UploadPayloadTimer_Tick;
            uploadPayloadTimer.Start();

            //count timer 
            countTimer = new DispatcherTimer();
            countTimer.Interval = TimeSpan.FromSeconds(1); // Set the timer interval to 1 second
            countTimer.Tick += DispatcherTimer_Tick;

            _ = StartScan();
        }

        #endregion

        /// <summary>
        /// This is the DispatcherTimer_Tick Handles the tick event of a dispatcher timer used for countdown functionality.
        /// </summary>
        /// <returns></returns>
        private void DispatcherTimer_Tick(object sender, object e)
        {
            if (countdown > 0)
            {
                countdown--; // Decrement the countdown value
                BleTimerTextBlock = $"00:{countdown:D2}";
            }
            else
            {
                countTimer.Stop(); // Stop the timer when countdown reaches 0
            }
        }

        /// <summary>
        /// This is the TagDetailsOpeanAsync Opens a dialog to display Raw Data of Selected Beacon.
        /// </summary>
        /// <returns></returns>
        public async void TagDetailsOpenAsync()
        {
            dialogPopup = new TagDetailsDialog(new List<string>() { SelectedBeacon?.RawData }, "Advertising Data");
            await dialogPopup.ShowAsync();
        }

        /// <summary>
        /// This is UploadPayloadTimer_Tick Handles the tick event of a timer to upload payloads.
        /// </summary>
        /// <returns> </returns>
        private async void UploadPayloadTimer_Tick(object sender, object e)
        {
            try
            {
                if (FilteredBeacons != null && FilteredBeacons.Count > 0)
                {
                    await UploadPayloadAsync();
                }
            }
            catch (Exception Ex)
            {
                uploadPayloadTimer.Stop();
                await MessageDialogService.ShowMessageAsync(Ex.Message, "Issue Occurred");
                _logger.LogException(ChorusEventType.WARNING, new ChorusCustomException("Error while starting upload process", Ex));
                uploadPayloadTimer.Start();
            }
        }

        /// <summary>
        /// This is the Timer_Tick Handles the tick event of a timer to update location and device availability.
        /// </summary>
        /// <returns> </returns>
        private async void LocationTimer_Tick(object sender, object e)
        {
            try
            {
                LocationData = await LocationServices.GetCoordinatesAsync();
                CoordinatesText = LocationData.ToString();
                LocationSourceText = "Source: " + LocationData.Source;
                IsDeviceAvailable = FilteredBeacons.Count > 0;
            }
            catch (Exception Ex)
            {
                _logger.LogException(ChorusEventType.WARNING, new ChorusCustomException("Error while fetching coordinates", Ex));
            }
        }

        /// <summary>
        /// This is the OnAdvertisementReceived.
        /// </summary>
        /// <returns>Returns OnAdvertisementReceived data.</returns>
        private async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {

            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    var manuData = args.Advertisement.GetManufacturerDataByCompanyId(GlobalHelper.ChorusCompanyId1);
                    if (args.Advertisement.ManufacturerData.Count > 0 && manuData.Count > 0)
                    {
                        // Extract the flags
                        var flagsData = args.Advertisement.DataSections
                        .Where(ds => ds.DataType == BluetoothLEAdvertisementDataTypes.Flags).SelectMany(v => v.Data.ToArray());
                        string flags = flagsData != null ? BitConverter.ToString(flagsData.ToArray()).Replace("-", "") : "";

                        // Extract manufacturer data
                        var manufacturerData = args.Advertisement.ManufacturerData.First()?.Data.ToArray();
                        string manufacturerDataStr = manufacturerData != null ? BitConverter.ToString(manufacturerData).Replace("-", "") : "";
                        string address = args.BluetoothAddress.ToString("X");

                        //TODO:Remove after testing
                        if (address.StartsWith("E7666-", StringComparison.OrdinalIgnoreCase) || address.StartsWith("F7666", StringComparison.OrdinalIgnoreCase)
                             || address.StartsWith("51", StringComparison.OrdinalIgnoreCase))
                        {

                        }
                        var isFlagAbsent = flagsData == null || flagsData?.Count() == 0;
                        var companyName = GlobalHelper.GetCompanyName(flagsData == null ? manufacturerData : flagsData.Concat(manufacturerData).ToArray(), args.Advertisement.ManufacturerData.First(), isFlagAbsent);

                        if (GlobalHelper.CompanyNames.Contains(companyName))
                        {
                            var beacon = new Beacon
                            {
                                CompanyName = companyName,
                                Address = address,
                                Rssi = args.RawSignalStrengthInDBm,
                                AdvertisementMessage = "0x" + flags + manufacturerDataStr,
                                RawData = "0x" + manufacturerDataStr,
                                Location = LocationData.ToString(),
                                LocalTime = DateTime.Now,
                                UTCTime = DateTime.UtcNow
                            };
                            Beacons.Add(beacon);
                            ApplyFilters();
                        }
                    }
                }
                catch (Exception Ex)
                {
                    _logger.LogException(ChorusEventType.ERROR, new ChorusCustomException("Error while receiving advertisement", Ex));
                }
            });
        }

        /// <summary>
        /// This is the ApplyFilters Applies filters to the list of beacons based on MAC address and company name filters.
        /// </summary>
        /// <returns></returns>
        public void ApplyFilters()
        {
            string macFilter = string.Empty;
            string companyFilter = string.Empty;
            if (!string.IsNullOrWhiteSpace(MacFilterText))
            {
                macFilter = MacFilterText.ToLower();
            }
            if (!string.IsNullOrWhiteSpace(CompanyFilterText))
            {
                companyFilter = CompanyFilterText.ToLower();
            }
            FilteredBeacons.Clear();
            foreach (var beacon in Beacons)
            {

                if ((string.IsNullOrEmpty(macFilter) || beacon.Address.ToLower().Contains(macFilter)) &&
                    (string.IsNullOrEmpty(companyFilter) || beacon.CompanyName.ToLower().Contains(companyFilter)))
                {
                    FilteredBeacons.Add(beacon);
                }
            }
        }

        /// <summary>
        /// This is the GetLocationAccessPoint Determines the location access point based on the source of location data.
        /// </summary>
        /// <returns></returns>
        private LocationSource GetLocationAccessPoint()
        {
            if (LocationData?.Source != null)
            {
                return LocationData?.Source == LocationSourceConstant.IPBasedService ? DerivedLocation.Types.LocationSource.WifiAccessPointsAndCellTowers : DerivedLocation.Types.LocationSource.Gnss;
            }
            else
            {
                return LocationSource.Unspecified;
            }
        }

        /// <summary>
        /// This is the UploadPayloadAsync.
        /// Uploads filtered beacon data to a server asynchronously, handling errors and response tracking.
        /// </summary>
        /// <returns></returns>
        private async Task UploadPayloadAsync()
        {
            try
            {
                BeaconsToUpload = FilteredBeacons.Where(beacon =>
                {
                    if (beaconUploadInfo.TryGetValue(beacon.Address, out var lastUploadTime))
                    {
                        return beacon.LocalTime > lastUploadTime;
                    }
                    return true; // New beacon address
                }).ToList();

                if (BeaconsToUpload.Count == 0)
                {
                    return; // No new beacons to upload
                }

                var tasks = BeaconsToUpload.Select(async beacon =>
                {
                    var beaconPayload = new Payload.Types.BeaconPayload
                    {
                        ReceiveTime = Timestamp.FromDateTime(DateTime.SpecifyKind(beacon.LocalTime.LocalDateTime, DateTimeKind.Utc)),
                        RssiDbm = beacon.Rssi
                    };
                    if (GlobalHelper.AppSettings.PayloadHexString)
                    {
                        beaconPayload.BleManufacturerData = ByteString.CopyFromUtf8(GlobalHelper.HexToUtf8String(beacon.RawData));
                    }
                    else
                    {
                        beaconPayload.BleManufacturerData = ByteString.CopyFrom(GlobalHelper.HexStringToByteArray(beacon.RawData));
                    }

                    string[] parts = beacon.Location.Split(new[] { "Latitude: ", ", Longitude: " }, StringSplitOptions.RemoveEmptyEntries);
                    double latitude = double.Parse(parts[0], CultureInfo.InvariantCulture);
                    double longitude = double.Parse(parts[1], CultureInfo.InvariantCulture);
                    GeoPoint point = new GeoPoint
                    {
                        LatitudeMicro = (int)(latitude * 1_000_000),
                        LongitudeMicro = (int)(longitude * 1_000_000)
                    };

                    beaconUploadInfo[beacon.Address] = DateTime.UtcNow;

                    var payload = new Payload
                    {
                        DeviceId = GlobalHelper.DeviceId,
                        OrgId = GlobalHelper.AppSettings.OrganizationId.ToString(),
                        DeviceLocation = new DerivedLocation
                        {
                            Point = point,
                            AccuracyCm = 1000,
                            Source = GetLocationAccessPoint()
                        },
                        DeviceLocationTime = Timestamp.FromDateTime(DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)),
                        Metadata = new Payload.Types.PayloadMetadata
                        {
                            Type = Payload.Types.PayloadMetadata.Types.DeviceType.Windows,
                            Version = GlobalHelper.VersionInfo
                        }
                    };

                    payload.BeaconPayloads.Add(beaconPayload);
                    var client = new BaseApiClient(App.ServiceProvider.GetRequiredService<IChorusLogger<BaseApiClient>>());
                    if (GlobalHelper.AppSettings.LogPayloads)
                        _logger.LogInfo(GlobalHelper.RawPayload(payload).FirstOrDefault());
                    var response = await client.SendPayloadAsync(payload);
                    PayloadResponses.Add((payload, response));

                    return response;
                }).ToList();

                var responses = await Task.WhenAll(tasks);
                foreach (var response in responses)
                {
                    //Responses.Add(response);
                    if (PayloadResponses != null && PayloadResponses.Count > 10)
                    {
                        PayloadResponses.RemoveAt(0);
                    }
                }
            }
            catch (Exception Ex)
            {
                //Responses.Add($"Error: {ex.Message}");
                _logger.LogException(ChorusEventType.ERROR, new ChorusCustomException("Error while uploading payload", Ex));
                if (PayloadResponses.Count > 10)
                {
                    PayloadResponses.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// This is the OpenPayloadDetailsAsync.
        /// Opens a dialog displaying details of the selected payload item, if it is not null.
        /// </summary>
        /// </summary>
        /// <returns></returns>
        public async Task OpenPayloadDetailsAsync()
        {
            if (SelectedPayload.Item1 != null)
            {
                dialogPopup = new TagDetailsDialog(GlobalHelper.RawPayload(SelectedPayload.Item1), "Payload");
                await dialogPopup.ShowAsync();
            }
        }
    }
}
