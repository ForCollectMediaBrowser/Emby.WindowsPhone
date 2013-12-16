﻿using System.Threading.Tasks;
using Cimbalino.Phone.Toolkit.Services;
using MediaBrowser.Model;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Net;
using PropertyChanged;
using ScottIsAFool.WindowsPhone.Logging;

namespace MediaBrowser.Services
{
    [ImplementPropertyChanged]
    public class AuthenticationService
    {
        private static AuthenticationService _current;
        private IApplicationSettingsService _settingsService;
        private static IExtendedApiClient _apiClient;
        private static ILog _logger;
        
        public static AuthenticationService Current
        {
            get { return _current ?? (_current = new AuthenticationService()); }
        }

        public AuthenticationService()
        {
            _logger = new WPLogger(typeof (AuthenticationService));
        }

        public void Start(IExtendedApiClient apiClient)
        {
            _apiClient = apiClient;
            _settingsService = new ApplicationSettingsService();

            CheckIfUserSignedIn();
        }

        private void CheckIfUserSignedIn()
        {
            var user = _settingsService.Get<UserDto>(Constants.Settings.SelectedUserSetting);

            if (user != null)
            {
                LoggedInUser = user;
                IsLoggedIn = true;
                _apiClient.CurrentUserId = LoggedInUserId;
            }
        }

        public async Task Login(string selectedUserName, string pinCode)
        {
            try
            {
                _logger.Info("Authenticating user [{0}]", selectedUserName);

                var result = await _apiClient.AuthenticateUserAsync(selectedUserName, pinCode.ToHash());

                _logger.Info("Logged in as [{0}]", selectedUserName);

                LoggedInUser = result.User;
                IsLoggedIn = true;
                _apiClient.CurrentUserId = LoggedInUserId;

                _settingsService.Set(Constants.Settings.SelectedUserSetting, LoggedInUser);
                _settingsService.Save();
                _logger.Info("User [{0}] has been saved", selectedUserName);
            }
            catch (HttpException ex)
            {
                _logger.ErrorException("Login()", ex);
            }
        }

        public void Logout()
        {
            LoggedInUser = null;
            IsLoggedIn = false;

            _settingsService.Reset(Constants.Settings.SelectedUserSetting);
            _settingsService.Save();
        }

        public UserDto LoggedInUser { get; private set; }
        public bool IsLoggedIn { get; private set; }

        public string LoggedInUserId
        {
            get
            {
                return LoggedInUser.Id;
            }
        }
    }
}
