﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using Cimbalino.Phone.Toolkit.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Querying;
using MediaBrowser.Shared;
using MediaBrowser.WindowsPhone.Model;
using MediaBrowser.WindowsPhone.Resources;
using INavigationService = MediaBrowser.WindowsPhone.Model.INavigationService;

#if !WP8
using ScottIsAFool.WindowsPhone;
using Wintellect.Sterling;

#endif

namespace MediaBrowser.WindowsPhone.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MusicViewModel : ViewModelBase
    {
        private readonly ExtendedApiClient _apiClient;
        private readonly INavigationService _navigationService;
        private readonly ILog _logger;
        private readonly IApplicationSettingsService _settingsService;

        private List<BaseItemDto> _artistTracks;
        private bool _gotAlbums;
        /// <summary>
        /// Initializes a new instance of the MusicViewModel class.
        /// </summary>
        public MusicViewModel(ExtendedApiClient apiClient, INavigationService navigationService, IApplicationSettingsService applicationSettings)
        {
            _navigationService = navigationService;
            _apiClient = apiClient;
            _logger= new WPLogger(typeof(MusicViewModel));
            _settingsService = applicationSettings;

            SelectedTracks = new List<BaseItemDto>();
            if (IsInDesignMode)
            {
                SelectedArtist = new BaseItemDto
                {
                    Name = "Hans Zimmer",
                    Id = "179d32421632781047c73c9bd501adea"
                };
                SelectedAlbum = new BaseItemDto
                {
                    Name = "The Dark Knight Rises",
                    Id = "f8d5c8cbcbd39bc75c2ba7ada65d4319",
                };
                Albums = new ObservableCollection<BaseItemDto>
                             {
                                 new BaseItemDto {Name = "The Dark Knight Rises", Id = "f8d5c8cbcbd39bc75c2ba7ada65d4319", ProductionYear = 2012},
                                 new BaseItemDto {Name = "Batman Begins", Id = "03b6dbb15e4abcca6ee336a2edd79ba6", ProductionYear = 2005},
                                 new BaseItemDto {Name = "Sherlock Holmes", Id = "6e2d519b958d440d034c3ba6eca008a4", ProductionYear = 2010}
                             };
                AlbumTracks = new List<BaseItemDto>
                                  {
                                      new BaseItemDto {Name = "Bombers Over Ibiza (Junkie XL Remix)", IndexNumber = 1, ParentIndexNumber = 2, RunTimeTicks = 3487920000, Id = "7589bfbe8b10d0191e305d92f127bd01"},
                                      new BaseItemDto {Name = "A Storm Is Coming", Id = "1ea1fd991c70b33c596611dadf24defc", IndexNumber = 1, ParentIndexNumber = 1, RunTimeTicks = 369630000},
                                      new BaseItemDto {Name = "On Thin Ice", Id = "2696da6a01f254fbd7e199a191bd5c4f", IndexNumber = 2, ParentIndexNumber = 1, RunTimeTicks = 1745500000},
                                  }.OrderBy(x => x.ParentIndexNumber)
                                   .ThenBy(x => x.IndexNumber).ToList();

                SortedTracks = Utils.GroupArtistTracks(AlbumTracks);
            }
            else
            {
                WireCommands();
                WireMessages();
            }

        }

        private void WireMessages()
        {
            Messenger.Default.Register<NotificationMessage>(this, m =>
            {
                if (m.Notification.Equals(Constants.MusicArtistChangedMsg))
                {
                    Albums = new ObservableCollection<BaseItemDto>();
                    _artistTracks = new List<BaseItemDto>();
                    AlbumTracks = new List<BaseItemDto>();
                    SelectedArtist = (BaseItemDto)m.Sender;
                    _gotAlbums = false;
                }
                if (m.Notification.Equals(Constants.MusicAlbumChangedMsg))
                {
                    SelectedAlbum = (BaseItemDto)m.Sender;
                    if (_artistTracks != null)
                    {
                        AlbumTracks = _artistTracks.Where(x => x.ParentId == SelectedAlbum.Id)
                                                  .OrderBy(x => x.ParentIndexNumber)
                                                  .ThenBy(x => x.IndexNumber).ToList();
                    }
                }
            });
        }

        private void WireCommands()
        {
            ArtistPageLoaded = new RelayCommand(async () =>
                                                    {
                                                        if (_navigationService.IsNetworkAvailable && !_gotAlbums)
                                                        {
                                                            ProgressText = AppResources.SysTrayGettingAlbums;
                                                            ProgressIsVisible = true;

                                                            await GetArtistInfo();

                                                            ProgressText = string.Empty;
                                                            ProgressIsVisible = false;
                                                        }
                                                    });

            AlbumPageLoaded = new RelayCommand(async () =>
                                                   {
                                                       if (AlbumTracks == null)
                                                       {
                                                           ProgressText = "Getting tracks...";
                                                           ProgressIsVisible = true;
                                                           try
                                                           {
                                                               await GetArtistInfo();

                                                               AlbumTracks = _artistTracks.Where(x => x.ParentId == SelectedAlbum.Id)
                                                                                         .OrderBy(x => x.ParentIndexNumber)
                                                                                         .ThenBy(x => x.IndexNumber).ToList();
                                                           }
                                                           catch
                                                           {

                                                           }
                                                       }
                                                   });

            AlbumTapped = new RelayCommand<BaseItemDto>(album =>
                                                            {
                                                                SelectedAlbum = album;
                                                                AlbumTracks = _artistTracks.Where(x => x.ParentId == SelectedAlbum.Id)
                                                                                          .OrderBy(x => x.IndexNumber)
                                                                                          .ToList();
                                                            });

            AlbumPlayTapped = new RelayCommand<BaseItemDto>(album =>
                                                                {

                                                                });

            SelectionChangedCommand = new RelayCommand<SelectionChangedEventArgs>(args =>
                                                                                      {
                                                                                          if (args.AddedItems != null)
                                                                                          {
                                                                                              foreach (var track in args.AddedItems.Cast<BaseItemDto>())
                                                                                              {
                                                                                                  SelectedTracks.Add(track);
                                                                                              }
                                                                                          }

                                                                                          if (args.RemovedItems != null)
                                                                                          {
                                                                                              foreach (var track in args.RemovedItems.Cast<BaseItemDto>())
                                                                                              {
                                                                                                  SelectedTracks.Remove(track);
                                                                                              }
                                                                                          }

                                                                                          SelectedTracks = SelectedTracks.OrderBy(x => x.IndexNumber).ToList();
                                                                                      });

            AddToNowPlayingCommand = new RelayCommand(() =>
                                                          {
                                                              if (!SelectedTracks.Any()) return;

                                                              var currentPlaylist = _settingsService.Get(Constants.CurrentPlaylist, new List<PlaylistItem>());

                                                              var i = 1;
                                                              
                                                              SelectedTracks.ForEach(item =>
                                                                                         {
                                                                                             var playlistItem = item.ToPlaylistItem(_apiClient);
                                                                                             playlistItem.Id = currentPlaylist.Count + i;
                                                                                             currentPlaylist.Add(playlistItem);
                                                                                             i++;
                                                                                         });

                                                              _settingsService.Set(Constants.CurrentPlaylist, currentPlaylist);
                                                          });

            PlayItemsCommand = new RelayCommand(() =>
                                                    {

                                                    });
        }

        private async Task GetArtistInfo()
        {
            try
            {
                _logger.LogFormat("Getting information for Artist [{0}] ({1})", LogLevel.Info, SelectedArtist.Name, SelectedArtist.Id);

                var artistQuery = new ArtistsQuery
                                      {

                                      };
                SelectedArtist = await _apiClient.GetItemAsync(SelectedArtist.Id, App.Settings.LoggedInUser.Id);
            }
            catch (HttpException ex)
            {
                _logger.Log(ex.Message, LogLevel.Fatal);
                _logger.Log(ex.StackTrace, LogLevel.Fatal);
            }

            _gotAlbums = await GetAlbums();

            await GetArtistTracks();

            SortTracks();
        }

        private void SortTracks()
        {
            if (_artistTracks != null && _artistTracks.Any())
            {
                SortedTracks = Utils.GroupArtistTracks(_artistTracks);
            }
        }

        private async Task<bool> GetArtistTracks()
        {
            try
            {
                var query = new ItemQuery
                {
                    UserId = App.Settings.LoggedInUser.Id,
                    Artists = new[] { SelectedArtist.Name },
                    Recursive = true,
                    Fields = new[] { ItemFields.AudioInfo, ItemFields.ParentId, },
                    IncludeItemTypes = new[] { "Audio" }
                };

                _logger.LogFormat("Getting tracks for artist [{0}] ({1})", LogLevel.Info, SelectedArtist.Name, SelectedArtist.Id);

                var items = await _apiClient.GetItemsAsync(query);

                if (items != null && items.Items.Any())
                {
                    _artistTracks = items.Items.ToList();
                }

                return true;
            }
            catch (HttpException ex)
            {
                _logger.Log(ex.Message, LogLevel.Fatal);
                _logger.Log(ex.StackTrace, LogLevel.Fatal);
                return false;
            }
        }

        private async Task<bool> GetAlbums()
        {
            try
            {
                var query = new ItemQuery
                                {
                                    UserId = App.Settings.LoggedInUser.Id,
                                    Artists = new [] {SelectedArtist.Name},
                                    Recursive = true,
                                    Fields = new[] {ItemFields.AudioInfo, ItemFields.ParentId, },
                                    IncludeItemTypes = new []{"MusicAlbum"}
                                };

                _logger.LogFormat("Getting albums for artist [{0}] ({1})", LogLevel.Info, SelectedArtist.Name, SelectedArtist.Id);

                var items = await _apiClient.GetItemsAsync(query);
                if (items != null && items.Items.Any())
                {
                    //// Extract the album items from the results
                    //var albums = items.Items.Where(x => x.Type == "MusicAlbum").ToList();

                    //// Extract the track items from the results
                    //_artistTracks = items.Items.Where(y => y.Type == "Audio").ToList();

                    //var nameId = (from a in _artistTracks
                    //              select new KeyValuePair<string, string>(a.Album, a.ParentId)).Distinct();

                    //// This sets the album names correctly based on what's in the track information (rather than folder name)
                    //foreach (var ni in nameId)
                    //{
                    //    var item = albums.SingleOrDefault(x => x.Id == ni.Value);
                    //    item.Name = ni.Key;
                    //}

                    foreach (var album in items.Items)
                    {
                        Albums.Add(album);
                    }
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(ex.Message, LogLevel.Fatal);
                _logger.Log(ex.StackTrace, LogLevel.Fatal);
                return false;
            }
        }

        public string ProgressText { get; set; }
        public bool ProgressIsVisible { get; set; }

        public bool IsInSelectionMode { get; set; }
        public int SelectedAppBarIndex { get { return IsInSelectionMode ? 1 : 0; } }

        public BaseItemDto SelectedArtist { get; set; }
        public BaseItemDto SelectedAlbum { get; set; }
        public ObservableCollection<BaseItemDto> Albums { get; set; }
        public List<BaseItemDto> AlbumTracks { get; set; }
        public List<Group<BaseItemDto>> SortedTracks { get; set; }
        public List<BaseItemDto> SelectedTracks { get; set; }

        public RelayCommand ArtistPageLoaded { get; set; }
        public RelayCommand AlbumPageLoaded { get; set; }
        public RelayCommand<BaseItemDto> AlbumTapped { get; set; }
        public RelayCommand<BaseItemDto> AlbumPlayTapped { get; set; }
        public RelayCommand<SelectionChangedEventArgs> SelectionChangedCommand { get; set; }
        public RelayCommand AddToNowPlayingCommand { get; set; }
        public RelayCommand PlayItemsCommand { get; set; }
    }
}