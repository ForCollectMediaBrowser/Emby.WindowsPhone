﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Emby.WindowsPhone.Helpers;
using Emby.WindowsPhone.Localisation;
using Emby.WindowsPhone.Model;
using Emby.WindowsPhone.Model.Interfaces;
using Emby.WindowsPhone.Services;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using MediaBrowser.Model.ApiClient;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Net;
using ScottIsAFool.WindowsPhone;

namespace Emby.WindowsPhone.ViewModel.LiveTv
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class RecordedTvViewModel : ViewModelBase
    {
        private bool _programmesLoaded;

        private DateTime? _programmesLastRun;

        /// <summary>
        /// Initializes a new instance of the RecordedTvViewModel class.
        /// </summary>
        public RecordedTvViewModel(INavigationService navigationService, IConnectionManager connectionManager)
            : base(navigationService, connectionManager)
        {

            GroupBy = RecordedGroupBy.RecordedDate;
        }

        public List<BaseItemDto> RecordedProgrammes { get; set; }
        public List<Group<BaseItemDto>> GroupedRecordedProgrammes { get; set; }

        public bool HasRecordedItems
        {
            get { return !RecordedProgrammes.IsNullOrEmpty(); }
        }

        public RecordedGroupBy GroupBy { get; set; }

        public RelayCommand RecordedTvViewLoaded
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    GroupBy = App.SpecificSettings.DefaultRecordedGroupBy;
                    await LoadProgrammes(false);
                });
            }
        }

        public RelayCommand RefreshCommand
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    await LoadProgrammes(true);
                });
            }
        }

        public RelayCommand<BaseItemDto> ItemTappedCommand
        {
            get
            {
                return new RelayCommand<BaseItemDto>(item =>
                {
                    if (SimpleIoc.Default.GetInstance<ProgrammeViewModel>() != null)
                    {
                        Messenger.Default.Send(new NotificationMessage(item, Constants.Messages.ProgrammeItemChangedMsg));
                        NavigationService.NavigateTo(Constants.Pages.LiveTv.RecordingView);
                    }   
                });
            }
        }

        private async Task LoadProgrammes(bool isRefresh)
        {
            if (!NavigationService.IsNetworkAvailable || (_programmesLoaded && !isRefresh && !LiveTvHelper.HasExpired(_programmesLastRun)))
            {
                return;
            }

            try
            {
                SetProgressBar(AppResources.SysTrayGettingRecordedItems);

                var query = new RecordingQuery
                {
                    IsInProgress = false,
                    Status = RecordingStatus.Completed,
                    UserId = AuthenticationService.Current.LoggedInUserId
                };

                var items = await ApiClient.GetLiveTvRecordingsAsync(query);

                if (items != null && !items.Items.IsNullOrEmpty())
                {
                    RecordedProgrammes = items.Items.OrderBy(x => x.StartDate).ToList();
                    await GroupProgrammes();

                    _programmesLoaded = true;
                    _programmesLastRun = DateTime.Now;
                }

            }
            catch (HttpException ex)
            {
                Utils.HandleHttpException(ex, "LoadProgrammes(" + isRefresh + ")", NavigationService, Log);
            }

            SetProgressBar();
        }

        

        private async Task GroupProgrammes()
        {
            if (RecordedProgrammes.IsNullOrEmpty())
            {
                return;
            }

            SetProgressBar(AppResources.SysTrayRegrouping);

            await Task.Run(() =>
            {
                var groupedItems = new List<Group<BaseItemDto>>();
                switch (GroupBy)
                {
                    case RecordedGroupBy.RecordedDate:
                        groupedItems = (from p in RecordedProgrammes
                                        where p.StartDate.HasValue
                                        group p by p.StartDate.Value.ToLocalTime().Date
                                            into grp
                                            orderby grp.Key descending 
                                            select new Group<BaseItemDto>(Utils.CoolDateName(grp.Key), grp)).ToList();

                        break;
                    case RecordedGroupBy.ShowName:
                        groupedItems = (from p in RecordedProgrammes
                                        group p by p.Name
                                            into grp
                                            orderby grp.Key
                                            select new Group<BaseItemDto>(grp.Key, grp)).ToList();
                        break;
                    case RecordedGroupBy.Channel:
                        groupedItems = (from p in RecordedProgrammes
                                        group p by p.ChannelName
                                            into grp
                                            orderby grp.Key
                                            select new Group<BaseItemDto>(grp.Key, grp)).ToList();
                        break;
                }

                Deployment.Current.Dispatcher.BeginInvoke(() => GroupedRecordedProgrammes = groupedItems);
            });
        }

        public override void WireMessages()
        {
            Messenger.Default.Register<NotificationMessage>(this, async m =>
            {
                if (m.Notification.Equals(Constants.Messages.ChangeRecordingGroupingMsg))
                {
                    GroupBy = (RecordedGroupBy) m.Sender;
                    await GroupProgrammes();

                    SetProgressBar();
                }
            });
        }

        public override void UpdateProperties()
        {
            RaisePropertyChanged(() => HasRecordedItems);
        }
    }
}