﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TwitchLeecher.Core.Models;
using TwitchLeecher.Gui.Interfaces;
using TwitchLeecher.Services.Interfaces;
using TwitchLeecher.Shared.Commands;
using TwitchLeecher.Shared.Events;

namespace TwitchLeecher.Gui.ViewModels
{
    public class SearchResultViewVM : ViewModelBase, INavigationState
    {
        #region Fields

        private readonly ITwitchService _twitchService;
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;
        private readonly INotificationService _notificationsService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IPreferencesService _preferencesService;
        private readonly IFilenameService _filenameService;

        private readonly object _commandLockObject;

        private ICommand _viewCommand;
        private ICommand _downloadCommand;
        private ICommand _seachCommand;

        #endregion Fields

        #region Constructors

        public SearchResultViewVM(
            ITwitchService twitchService,
            IDialogService dialogService,
            INavigationService navigationService,
            INotificationService notificationService,
            IEventAggregator eventAggregator,
            IPreferencesService preferencesService,
            IFilenameService filenameService)
        {
            _twitchService = twitchService;
            _dialogService = dialogService;
            _navigationService = navigationService;
            _notificationsService = notificationService;
            _eventAggregator = eventAggregator;
            _preferencesService = preferencesService;
            _filenameService = filenameService;

            _twitchService.PropertyChanged += TwitchService_PropertyChanged;

            _commandLockObject = new object();
        }

        #endregion Constructors

        #region Properties

        public double ScrollPosition { get; set; }

        public ObservableCollection<TwitchVideo> Videos
        {
            get
            {
                return _twitchService.Videos;
            }
        }

        public ICommand ViewCommand
        {
            get
            {
                if (_viewCommand == null)
                {
                    _viewCommand = new DelegateCommand<string>(ViewVideo);
                }

                return _viewCommand;
            }
        }

        public ICommand DownloadCommand
        {
            get
            {
                if (_downloadCommand == null)
                {
                    _downloadCommand = new DelegateCommand<string>(DownloadVideo);
                }

                return _downloadCommand;
            }
        }

        public ICommand SeachCommnad
        {
            get
            {
                if (_seachCommand == null)
                {
                    _seachCommand = new DelegateCommand(ShowSearch);
                }

                return _seachCommand;
            }
        }

        #endregion Properties

        #region Methods

        private void ViewVideo(string id)
        {
            try
            {
                lock (_commandLockObject)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        TwitchVideo video = Videos.Where(v => v.Id == id).FirstOrDefault();

                        if (video != null && video.Url != null && video.Url.IsAbsoluteUri)
                        {
                            Process.Start(video.Url.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void DownloadVideo(string id)
        {
            try
            {
                lock (_commandLockObject)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        TwitchVideo video = Videos.Where(v => v.Id == id).FirstOrDefault();

                        if (video != null)
                        {
                            VodAuthInfo vodAuthInfo = _twitchService.RetrieveVodAuthInfo(video.Id);

                            if (!vodAuthInfo.Privileged && vodAuthInfo.SubOnly)
                            {
                                if (!_twitchService.IsAuthorized)
                                {
                                    _dialogService.ShowMessageBox("This video is sub-only! Please authorize your Twitch account by clicking the Twitch button in the menu.", "SUB HYPE!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                }
                                else
                                {
                                    _dialogService.ShowMessageBox("This video is sub-only but you are not subscribed to '" + video.Channel + "'!", "SUB HYPE!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                }

                                return;
                            }

                            Preferences currentPrefs = _preferencesService.CurrentPreferences.Clone();

                            string filename = _filenameService.SubstituteWildcards(currentPrefs.DownloadFileName, video);

                            DownloadParameters downloadParams = new DownloadParameters(video, vodAuthInfo, video.Qualities.First(), currentPrefs.DownloadFolder, filename);

                            _navigationService.ShowDownload(downloadParams);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        public void ShowSearch()
        {
            try
            {
                lock (_commandLockObject)
                {
                    _navigationService.ShowSearch();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        protected override List<MenuCommand> BuildMenu()
        {
            List<MenuCommand> menuCommands = base.BuildMenu();

            if (menuCommands == null)
            {
                menuCommands = new List<MenuCommand>();
            }

            menuCommands.Add(new MenuCommand(SeachCommnad, "New Search", "Search"));

            return menuCommands;
        }

        #endregion Methods

        #region EventHandlers

        private void TwitchService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string propertyName = e.PropertyName;

            FirePropertyChanged(propertyName);

            if (propertyName.Equals(nameof(Videos)))
            {
                ScrollPosition = 0;
            }
        }

        #endregion EventHandlers
    }
}