﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Cimbalino.Phone.Toolkit.Services;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using MediaBrowser.Model;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Net;
using MediaBrowser.WindowsPhone.Model;
using MediaBrowser.WindowsPhone.Resources;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Notification;
using Newtonsoft.Json;
using ScottIsAFool.WindowsPhone.ViewModel;
using INavigationService = MediaBrowser.WindowsPhone.Model.INavigationService;

using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using LockScreenService = MediaBrowser.WindowsPhone.Services.LockScreenService;

namespace MediaBrowser.WindowsPhone.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IExtendedApiClient _apiClient;
        private readonly INavigationService _navigationService;
        private readonly IApplicationSettingsService _applicationSettings;

        private const string PushServiceName = "MediaBrowser.WindowsPhone.PushService";

        public bool LoadingFromSettings;

        /// <summary>
        /// Initializes a new instance of the PushViewModel class.
        /// </summary>
        public SettingsViewModel(IExtendedApiClient apiClient, INavigationService navigationService, IApplicationSettingsService applicationSettings)
        {
            _apiClient = apiClient;
            _navigationService = navigationService;
            _applicationSettings = applicationSettings;

            if (IsInDesignMode)
            {
                IsRegistered = UseNotifications = true;
                RegisteredText = "Device registered";
                ServerPluginInstalled = false;

                FoundServers = new ObservableCollection<UdpResponse>
                {
                    new UdpResponse{Id = Guid.NewGuid().ToString(), Name = "Home", ServerAddress = "http://192.168.0.2"}
                };
            }
            else
            {
                LoadingFromSettings = true;
                SendTileUpdates = SendToastUpdates = true;
                RegisteredText = AppResources.DeviceNotRegistered;
                LoadingFromSettings = false;
                ServerPluginInstalled = false;
            }
        }

        public string RegisteredText { get; set; }
        public bool SendToastUpdates { get; set; }
        public bool SendTileUpdates { get; set; }

        public bool IsRegistered { get; set; }
        public bool ServerPluginInstalled { get; set; }
        public bool UseNotifications { get; set; }
        public HttpNotificationChannel HttpNotificationChannel { get; set; }

        public bool IsLockScreenProvider
        {
            get
            {
                return LockScreenService.Current.IsProvidedByCurrentApplication;
            }
        }

        public string LockScreenText
        {
            get
            {
                return IsLockScreenProvider ? AppResources.LabelIsLockScreenProvider : AppResources.LabelIsNotLockScreenProvider;
            }
        }

        public List<Stream> Posters { get; set; }
        public ObservableCollection<BaseItemDto> Folders { get; set; }
        public BaseItemDto SelectedCollection { get; set; }

        public RelayCommand MakeLockScreenProviderCommand
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    var result = await LockScreenService.Current.RequestAccessAsync();

                    if (result == LockScreenServiceRequestResult.Granted)
                    {
                        RaisePropertyChanged(() => IsLockScreenProvider);
                        RaisePropertyChanged(() => LockScreenText);
                    }
                });
            }
        }

        public RelayCommand<SelectionChangedEventArgs> CollectionChangedCommand
        {
            get
            {
                return new RelayCommand<SelectionChangedEventArgs>(async args =>
                {
                    if (args == null)
                    {
                        return;
                    }

                    var collection = (BaseItemDto) args.AddedItems[0];
                    App.SpecificSettings.LockScreenCollectionId = collection.Id;
                    SelectedCollection = Folders.FirstOrDefault(x => x.Id == App.SpecificSettings.LockScreenCollectionId);
                    await LockScreenService.Current.SetLockScreen(App.SpecificSettings.LockScreenType);
                });
            }
        } 

        private void LoadPosterStreams()
        {
            var list = new List<Stream>();

            for (var i = 1; i <= 5; i++)
            {
                var file = string.Format("Images/folder{0}.jpg", i);
                var sri = Application.GetResourceStream(new Uri(file, UriKind.Relative));
                if (sri != null)
                {
                    list.Add(sri.Stream);
                }
            }

            Posters = list;
        }

        private void GetFolders()
        {
            var main = SimpleIoc.Default.GetInstance<MainViewModel>();
            var folders = main.Folders;
            if (folders.IsNullOrEmpty())
            {
                return;
            }

            Folders = folders;
            SelectedCollection = Folders.FirstOrDefault(x => x.Id == App.SpecificSettings.LockScreenCollectionId);
        }

        public RelayCommand SettingsPageLoaded
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    //if (_navigationService.IsNetworkAvailable && !string.IsNullOrEmpty(_apiClient.ServerHostName))
                    //{
                    //    try
                    //    {
                    //        //var result = await ApiClient.CheckForPushServer();
                    //        //ServerPluginInstalled = true;
                    //    }
                    //    catch
                    //    {
                    //        ServerPluginInstalled = false;
                    //    }
                    //}
                    LoadPosterStreams();
                    GetFolders();
                });
            }
        }

        public RelayCommand<LiveTile> DeleteLiveTile
        {
            get
            {
                return new RelayCommand<LiveTile>(async liveTile =>
                {

                });
            }
        }

        public RelayCommand TestConnectionCommand
        {
            get
            {
                return new RelayCommand(async () => await TestConnection());
            }
        }

        private async Task TestConnection()
        {
            SetProgressBar(AppResources.SysTrayAuthenticating);

            if (_navigationService.IsNetworkAvailable)
            {
                Log.Info("Testing connection");


                var hostnameType = Uri.CheckHostName(App.Settings.ConnectionDetails.HostName);
                if (hostnameType == UriHostNameType.Unknown)
                {
                    MessageBox.Show(AppResources.ErrorInvalidHostname, AppResources.ErrorTitle, MessageBoxButton.OK);
                    return;
                }

                if (await Utils.GetServerConfiguration(_apiClient, Log))
                {
                    if (!IsInDesignMode)
                    {
                        _applicationSettings.Reset(Constants.Settings.SelectedUserSetting);
                        SettingsSet(Constants.Settings.ConnectionSettings, App.Settings.ConnectionDetails);
                    }

                    SetProgressBar(AppResources.SysTrayAuthenticating);
                    Utils.CheckProfiles(_navigationService);
                }
                else
                {
                    Log.Info("Invalid connection details");
                    App.ShowMessage(AppResources.ErrorConnectionDetailsInvalid);
                }
            }

            SetProgressBar();
        }


        #region Server Broadcast code WP8 only

        public ObservableCollection<UdpResponse> FoundServers { get; set; }

        public RelayCommand FindServerLoaded
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    // If we're not connected to wifi or ethernet then we don't want to attempt this
                    if (!_navigationService.IsNetworkAvailable ||
                        (NetworkInterface.NetworkInterfaceType != NetworkInterfaceType.Ethernet
                         && NetworkInterface.NetworkInterfaceType != NetworkInterfaceType.Wireless80211))
                    {
                        return;
                    }

                    SetProgressBar(AppResources.SysTrayFindingServer);

                    Log.Info("Sending UDP broadcast");
                    await SendMessage("who is MediaBrowserServer_v2?", 7359);

                    SetProgressBar();
                });
            }
        }

        public RelayCommand<Server> ServerTappedCommand
        {
            get
            {
                return new RelayCommand<Server>(async server =>
                {
                    App.Settings.ConnectionDetails.HostName = server.IpAddress;
                    App.Settings.ConnectionDetails.PortNo = int.Parse(server.PortNo);
                    //NavigationService.GoBack();

                    SetProgressBar(AppResources.SysTrayAuthenticating);

                    await TestConnection();

                    SetProgressBar();
                });
            }
        }

        private async Task SendMessage(string message, int port)
        {
            FoundServers = new ObservableCollection<UdpResponse>();
            var socket = new DatagramSocket();

            socket.MessageReceived += SocketOnMessageReceived;

            using (var stream = await socket.GetOutputStreamAsync(new HostName("255.255.255.255"), port.ToString(CultureInfo.InvariantCulture)))
            {
                using (var writer = new DataWriter(stream))
                {
                    var data = Encoding.UTF8.GetBytes(message);

                    writer.WriteBytes(data);
                    writer.StoreAsync();
                }
            }
        }

        private async void SocketOnMessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            var result = args.GetDataStream();
            var resultStream = result.AsStreamForRead(1024);

            using (var reader = new StreamReader(resultStream))
            {
                var text = await reader.ReadToEndAsync();
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    Log.Info("UDP response received");

                    var response = JsonConvert.DeserializeObject<UdpResponse>(text);

                    FoundServers.Add(response);
                });
            }
        }

        #endregion

        internal string DeviceId
        {
            get
            {
                var id = SimpleIoc.Default.GetInstance<IUserExtendedPropertiesService>().AnonymousUserID;
                return string.IsNullOrEmpty(id) ? "emulator" : id;
            }
        }

        #region Push Notification methods

        [UsedImplicitly]
        private void OnServerPluginInstalledChanged()
        {
            SettingsSet("ServerPluginInstalled", ServerPluginInstalled);
        }

        [UsedImplicitly]
        public async void OnUseNotificationsChanged()
        {
            if (!ServerPluginInstalled || LoadingFromSettings)
            {
                return;
            }

            await RegisterService();
        }

        public async Task RegisterService()
        {
            if (_navigationService.IsNetworkAvailable)
            {
                if (UseNotifications)
                {
                    SetProgressBar(AppResources.SysTrayRegisteringDevice);
                    HttpNotificationChannel = HttpNotificationChannel.Find(PushServiceName);

                    if (HttpNotificationChannel != null)
                    {
                        await SubscribeToService();
                    }
                    else
                    {
                        HttpNotificationChannel = new HttpNotificationChannel(PushServiceName);

                        HttpNotificationChannel.Open();
                    }
                }
                else
                {
                    SetProgressBar(AppResources.SysTrayUnregisteringDevice);

                    await _apiClient.DeleteDeviceAsync(DeviceId);

                    SendToastUpdates = SendTileUpdates = true;

                    if (HttpNotificationChannel.IsShellTileBound) HttpNotificationChannel.UnbindToShellTile();
                    if (HttpNotificationChannel.IsShellToastBound) HttpNotificationChannel.UnbindToShellToast();

                    IsRegistered = false;
                }

                SettingsSet("UseNotifications", UseNotifications);
                
                SetProgressBar();
            }
        }

        [UsedImplicitly]
        private void OnHttpNotificationChannelChanged()
        {
            SubscribeToChannelEvents();
        }

        [UsedImplicitly]
        private void OnIsRegisteredChanged()
        {
            RegisteredText = IsRegistered ? AppResources.DeviceRegistered : AppResources.DeviceNotRegistered;

            SettingsSet("IsRegistered", IsRegistered);
        }

        [UsedImplicitly]
        private async void OnSendToastUpdatesChanged()
        {
            if (!LoadingFromSettings)
            {
                try
                {
                    await _apiClient.UpdateDeviceAsync(DeviceId, SendToastUpdates);
                }
                catch (Exception ex)
                {
                    Log.ErrorException("OnSendToastUpdatesChanged()", ex);
                }
            }

            SettingsSet("SendToastUpdates", SendToastUpdates);
        }

        [UsedImplicitly]
        private async void OnSendTileUpdatesChanged()
        {
            if (!LoadingFromSettings)
            {
                try
                {
                    await _apiClient.UpdateDeviceAsync(DeviceId, sendTileUpdate: SendTileUpdates);
                }
                catch (Exception ex)
                {
                    Log.ErrorException("OnSendToastUpdatesChanged()", ex);
                }
            }

            SettingsSet("SendTileUpdates", SendTileUpdates);
        }

        private async Task SubscribeToService()
        {
            var isRegistered = false;
            try
            {
                await _apiClient.RegisterDeviceAsync(DeviceId, HttpNotificationChannel.ChannelUri.ToString(), SendTileUpdates, SendToastUpdates);
                isRegistered = true;
            }
            catch (HttpException ex)
            {
                Log.ErrorException("SubscribeToService()", ex);
            }

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                IsRegistered = isRegistered;
                BindingANotificationsChannelToAToastNotification();
                ProgressText = string.Empty;
                ProgressIsVisible = false;
            });

        }

        private void SubscribeToChannelEvents()
        {
            if (HttpNotificationChannel == null)
            {
                return;
            }

            HttpNotificationChannel.ChannelUriUpdated += async (sender, args) => await SubscribeToService();

            HttpNotificationChannel.HttpNotificationReceived += (sender, args) =>
            {
                var s = "";
            };

            HttpNotificationChannel.ShellToastNotificationReceived += (sender, args) =>
            {
                if (args.Collection != null)
                {
                    var collection = (Dictionary<string, string>) args.Collection;

                    Deployment.Current.Dispatcher.BeginInvoke(() => App.ShowMessage(collection["wp:Text2"]));
                }
            };

            HttpNotificationChannel.ErrorOccurred += (sender, args) =>
            {
                Log.Error(args.Message);
                Log.Error(args.ErrorType.ToString());
            };
        }

        private void BindingANotificationsChannelToAToastNotification()
        {
            if (!HttpNotificationChannel.IsShellToastBound)
            {
                HttpNotificationChannel.BindToShellToast();
            }

            if (!HttpNotificationChannel.IsShellTileBound)
            {
                HttpNotificationChannel.BindToShellTile(new Collection<Uri> {new Uri("http://dev.scottisafool.co.uk")});
            }
        }

        #endregion

        [UsedImplicitly]
        private static string ParseANID(string anid)
        {
            if (!string.IsNullOrEmpty(anid))
            {
                var parts = anid.Split('&');
                var pairs = parts.Select(part => part.Split('='));
                var id = pairs
                    .Where(pair => pair.Length == 2 && pair[0] == "A")
                    .Select(pair => pair[1])
                    .FirstOrDefault();
                return id;
            }
            return "emulator";
        }

        private void SettingsSet(string key, object value)
        {
            if (!IsInDesignMode)
            {
                _applicationSettings.Set(key, value);
                _applicationSettings.Save();
            }
        }
    }
}