﻿using System.Windows.Navigation;
using MediaBrowser.Model.Dto;
using Emby.WindowsPhone.ViewModel;

namespace Emby.WindowsPhone.Views
{
    /// <summary>
    /// Description for EpisodeView.
    /// </summary>
    public partial class EpisodeView
    {
        /// <summary>
        /// Initializes a new instance of the EpisodeView class.
        /// </summary>
        public EpisodeView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if(e.NavigationMode == NavigationMode.New)
            {
                var item = (BaseItemDto)App.SelectedItem;
                var vm = ViewModelLocator.GetTvViewModel(item.SeriesId);
                vm.SelectedEpisode = item;
                DataContext = vm;
                vm.GetEpisodeDetails();
            }
        }
    }
}