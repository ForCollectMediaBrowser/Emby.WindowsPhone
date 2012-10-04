﻿using System.Collections.Generic;
using GalaSoft.MvvmLight;
using MediaBrowser.ApiInteraction.WindowsPhone;
using MediaBrowser.WindowsPhone.Model;
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using MediaBrowser.Model.DTO;
using System.Threading.Tasks;

namespace MediaBrowser.WindowsPhone.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm/getstarted
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly INavigationService NavService;
        private readonly ApiClient ApiClient;
        private bool hasLoaded;
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(ApiClient apiClient, INavigationService navService)
        {
            ApiClient = apiClient;
            NavService = navService;
            Folders = new ObservableCollection<DtoBaseItem>();
            RecentItems = new ObservableCollection<DtoBaseItem>();
            if (IsInDesignMode)
            {
                RandomString = "blah";
                RecentItems.Add(new DtoBaseItem { Id = new Guid("2fc6f321b5f8bbe842fcd0eed089561d"), Name = "A Night To Remember" });
            }
            else
            {
                WireCommands();
                DummyFolder = new DtoBaseItem
                                  {
                                      Type = "folder",
                                      Name = "recent"
                                  };
            }
        }

        private void WireCommands()
        {
            PageLoaded = new RelayCommand(async () =>
            {
                if (NavService.IsNetworkAvailable && App.Settings.CheckHostAndPort() && !hasLoaded)
                {
                    ProgressIsVisible = true;
                    ProgressText = "Loading folders...";

                    bool folderLoaded = await GetFolders();

                    ProgressText = "Getting recent items...";

                    bool recentLoaded = await GetRecent();

                    hasLoaded = (folderLoaded && recentLoaded);
                    ProgressIsVisible = false;
                    hasLoaded = true;
                }
            });

            NavigateToPage = new RelayCommand<DtoBaseItem>(NavService.NavigateToPage);
        }

        private async Task<bool> GetRecent()
        {
            try
            {
                var items = await ApiClient.GetRecentlyAddedItemsAsync(App.Settings.LoggedInUser.Id);
                var episodesBySeries = items
                    .Where(x => x.Type == "Episode")
                    .GroupBy(l => l.ParentBackdropItemId)
                    .Select(g => new
                    {
                        Id = g.Key,
                        Name = g.Select(l => l.EpisodeInfo.SeriesName).FirstOrDefault(),
                        Count = g.Count(),
                        CreatedDate = g.OrderByDescending(l => l.DateCreated).First().DateCreated
                    }).ToList();
                var seriesList = new List<DtoBaseItem>();
                if (episodesBySeries != null && episodesBySeries.Any())
                {
                    seriesList.AddRange(episodesBySeries.Select(series => new DtoBaseItem
                    {
                        Name = string.Format("{0} ({1} items)", series.Name, series.Count),
                        Id = series.Id.Value,
                        DateCreated = series.CreatedDate,
                        Type = "Series",
                        SortName = Constants.GetTvInformationMsg
                    }));
                }
                var recent = items
                    .Where(x => x.Type != "Episode")
                    .Union(seriesList)
                    .OrderByDescending(x => x.DateCreated).ToList();
                RecentItems.Clear();
                recent.OrderBy(x => x.DateCreated).Take(6).ToList().ForEach(recentItem => RecentItems.Add(recentItem));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> GetFolders()
        {
            try
            {
                var item = await ApiClient.GetItemAsync(Guid.Empty, App.Settings.LoggedInUser.Id);
                Folders.Clear();
                item.Children.ToList().ForEach(folder => Folders.Add(folder));
                return true;
            }
            catch
            {
                return false;
            }
        }

        // UI properties
        public bool ProgressIsVisible { get; set; }
        public string ProgressText { get; set; }

        public RelayCommand PageLoaded { get; set; }
        public RelayCommand<DtoBaseItem> NavigateToPage { get; set; }
        public ObservableCollection<DtoBaseItem> Folders { get; set; }
        public ObservableCollection<DtoBaseItem> RecentItems { get; set; }
        public DtoBaseItem DummyFolder { get; set; }
        public string RandomString { get; set; }
    }
}