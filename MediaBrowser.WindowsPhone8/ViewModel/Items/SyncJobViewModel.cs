﻿using GalaSoft.MvvmLight.Command;
using MediaBrowser.Model.ApiClient;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Sync;
using MediaBrowser.WindowsPhone.Model.Interfaces;
using MediaBrowser.WindowsPhone.Resources;

namespace MediaBrowser.WindowsPhone.ViewModel.Items
{
    public class SyncJobViewModel : ViewModelBase
    {
        public SyncJobViewModel(SyncJob syncJob, INavigationService navigationService, IConnectionManager connectionManager)
            : base(navigationService, connectionManager)
        {
            SyncJob = syncJob;
        }

        public SyncJob SyncJob { get; set; }

        public string Name
        {
            get
            {
                return SyncJob != null && !string.IsNullOrEmpty(SyncJob.Name) ? SyncJob.Name : AppResources.LabelUntitled;
            }
        }

        public string ItemCount
        {
            get
            {
                if (SyncJob == null || SyncJob.ItemCount == 0)
                {
                    return string.Format(AppResources.LabelMultipleItems, 0);
                }

                return SyncJob.ItemCount == 1
                    ? AppResources.LabelOneItem
                    : string.Format(AppResources.LabelMultipleItems, SyncJob.ItemCount);
            }
        }

        public string Status
        {
            get
            {
                if (SyncJob == null)
                {
                    return string.Empty;
                }

                var id = string.Format("SyncJobStatus{0}", SyncJob.Status);

                return AppResources.ResourceManager.GetString(id);
            }
        }

        public RelayCommand NavigateToDetailsCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    NavigationService.NavigateTo(Constants.Pages.Sync.SyncJobDetailView);
                });
            }
        }

        public RelayCommand DeleteSyncJobCommand
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    try
                    {
                        await ApiClient.CancelSyncJob(SyncJob.Id);
                    }
                    catch (HttpException ex)
                    {
                        Utils.HandleHttpException("DeleteSyncJobCommand", ex, NavigationService, Log);
                    }
                });
            }
        }
    }
}
