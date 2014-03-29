﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Cimbalino.Phone.Toolkit.Services;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using MediaBrowser.Model;
using MediaBrowser.WindowsPhone.AudioAgent;
using MediaBrowser.WindowsPhone.Model;
using MediaBrowser.WindowsPhone.Resources;
using Microsoft.Phone.BackgroundAudio;
using ScottIsAFool.WindowsPhone.ViewModel;
using INavigationService = MediaBrowser.WindowsPhone.Model.INavigationService;

namespace MediaBrowser.WindowsPhone.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class PlaylistViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly PlaylistHelper _playlistHelper;
        private readonly DispatcherTimer _playlistChecker;

        private DateTime _lastReadDate;

        /// <summary>
        /// Initializes a new instance of the PlaylistViewModel class.
        /// </summary>
        public PlaylistViewModel(INavigationService navigationService, IStorageService storageService)
        {
            _navigationService = navigationService;
            _playlistChecker = new DispatcherTimer { Interval = new TimeSpan(0, 0, 3) };
            _playlistChecker.Tick += PlaylistCheckerOnTick;

            Playlist = new ObservableCollection<PlaylistItem>();
            SelectedItems = new List<PlaylistItem>();
            if (IsInDesignMode)
            {
                Playlist = new ObservableCollection<PlaylistItem>
                {
                    new PlaylistItem {Artist = "John Williams", Album = "Jurassic Park OST", Id = 1, IsPlaying = true, TrackName = "Jurassic Park Theme"},
                    new PlaylistItem {Artist = "John Williams", Album = "Jurassic Park OST", Id = 2, IsPlaying = false, TrackName = "Journey to the Island"},
                    new PlaylistItem {Artist = "John Williams", Album = "Jurassic Park OST", Id = 3, IsPlaying = false, TrackName = "Incident at Isla Nublar"}
                };
                NowPlayingItem = Playlist[0];
            }
            else
            {
                _playlistHelper = new PlaylistHelper(storageService);
                BackgroundAudioPlayer.Instance.PlayStateChanged += OnPlayStateChanged;
                GetPlaylistItems();
                IsPlaying = BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing;
            }
        }

        public ObservableCollection<PlaylistItem> Playlist { get; set; }

        public List<PlaylistItem> SmallList
        {
            get
            {
                return NowPlayingItem == null
                    ? null 
                    : Playlist.Where(x => !x.IsPlaying && x.Id > NowPlayingItem.Id)
                              .OrderBy(x => x.Id)
                              .Take(3)
                              .ToList();
            }
        }

        public List<PlaylistItem> SelectedItems { get; set; }
        public PlaylistItem NowPlayingItem { get; set; }
        public bool IsPlaying { get; set; }
        public bool IsShuffled { get; set; }
        public bool IsOnRepeat { get; set; }

        public bool IsInSelectionMode { get; set; }

        public int SelectedAppBarIndex
        {
            get { return IsInSelectionMode ? 1 : 0; }
        }

        #region Commands

        public RelayCommand PlaylistPageLoaded
        {
            get
            {
                return new RelayCommand(() =>
                {
                    GetPlaylistItems();

                    if (!_playlistChecker.IsEnabled)
                    {
                        _playlistChecker.Start();
                    }
                });
            }
        }

        public RelayCommand ClearPlaylistCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    var result = MessageBox.Show(AppResources.MessageClearPlayList, AppResources.MessageAreYouSureTitle, MessageBoxButton.OKCancel);

                    if (result == MessageBoxResult.OK)
                    {
                        _playlistHelper.ClearPlaylist();

                        GetPlaylistItems();
                    }
                });
            }
        }

        public RelayCommand<SelectionChangedEventArgs> SelectionChangedCommand
        {
            get
            {
                return new RelayCommand<SelectionChangedEventArgs>(args =>
                {
                    if (args.AddedItems != null)
                    {
                        foreach (var item in args.AddedItems.Cast<PlaylistItem>())
                        {
                            SelectedItems.Add(item);
                        }
                    }

                    if (args.RemovedItems != null)
                    {
                        foreach (var item in args.RemovedItems.Cast<PlaylistItem>())
                        {
                            SelectedItems.Remove(item);
                        }
                    }
                    RaisePropertyChanged(() => SelectedItems);
                });
            }
        }

        public RelayCommand DeleteItemsCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    var result = MessageBox.Show(AppResources.MessageDeletePlaylistItems, AppResources.MessageAreYouSureTitle, MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        _playlistHelper.RemoveFromPlaylist(SelectedItems);

                        IsInSelectionMode = false;

                        GetPlaylistItems();
                    }
                });
            }
        }

        public RelayCommand NextTrackCommand
        {
            get
            {
                return new RelayCommand(() => GetTrack(true));
            }
        }

        public RelayCommand PreviousTrackCommand
        {
            get
            {
                return new RelayCommand(() => GetTrack(false));
            }
        }

        public RelayCommand PlayPauseCommand
        {
            get
            {
                return new RelayCommand(PlayPause);
            }
        }

        #endregion

        #region Private methods

        private void PlaylistCheckerOnTick(object sender, EventArgs eventArgs)
        {
            GetPlaylistItems();
        }

        private void OnPlayStateChanged(object sender, EventArgs e)
        {
            IsPlaying = BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing;
        }

        public override void WireMessages()
        {
            Messenger.Default.Register<NotificationMessage<List<PlaylistItem>>>(this, m =>
            {
                if (m.Notification.Equals(Constants.Messages.AddToPlaylistMsg))
                {
                    _playlistHelper.AddToPlaylist(m.Content);

                    Log.Info("Adding {0} item(s) to the playlist", m.Content.Count);
                }

                if (m.Notification.Equals(Constants.Messages.SetPlaylistAsMsg))
                {
                    _playlistHelper.ClearPlaylist();

                    _playlistHelper.AddToPlaylist(m.Content);

                    _navigationService.NavigateTo(Constants.Pages.NowPlayingView);

                    if (BackgroundAudioPlayer.Instance.PlayerState != PlayState.Playing)
                    {
                        BackgroundAudioPlayer.Instance.Play();
                    }
                }

                if (m.Notification.Equals(Constants.Messages.PlaylistPageLeftMsg))
                {
                    if (_playlistChecker.IsEnabled)
                    {
                        _playlistChecker.Stop();
                    }
                }
            });
        }

        private void PlayPause()
        {
            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
            {
                BackgroundAudioPlayer.Instance.Stop();
            }
            else
            {
                BackgroundAudioPlayer.Instance.Play();
            }

            GetPlaylistItems();
        }

        private void GetTrack(bool isNextNotPrevious)
        {
            if (isNextNotPrevious)
            {
                BackgroundAudioPlayer.Instance.SkipNext();
            }
            else
            {
                BackgroundAudioPlayer.Instance.SkipPrevious();
            }
        }

        private void GetPlaylistItems()
        {
            var playlist = _playlistHelper.GetPlaylist();

            if (playlist == null || playlist.ModifiedDate == _lastReadDate) return;

            _lastReadDate = playlist.ModifiedDate;

            Playlist = new ObservableCollection<PlaylistItem>(playlist.PlaylistItems);

            IsShuffled = playlist.IsShuffled;
            IsOnRepeat = playlist.IsOnRepeat;

            var nowPlaying = playlist.PlaylistItems.FirstOrDefault(x => x.IsPlaying);
            NowPlayingItem = nowPlaying;
        }

        [UsedImplicitly]
        private void OnIsShuffledChanged()
        {
            if (_playlistHelper.RandomiseTrackNumbers(IsShuffled))
                GetPlaylistItems();
        }

        [UsedImplicitly]
        private void OnIsOnRepeatChanged()
        {
            _playlistHelper.SetRepeat(IsOnRepeat);
        }

        #endregion
    }
}