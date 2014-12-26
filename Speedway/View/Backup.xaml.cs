using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.ComponentModel;
using System.Windows.Input;
using CoPilot.Core.Utils;
using CoPilot.Interfaces;
using Controllers = CoPilot.Speedway.Controller;
using System.Runtime.CompilerServices;
using CoPilot.Speedway.Data;
using CoPilot.Speedway.Resources;
using System.Threading.Tasks;
using Microsoft.Phone.Tasks;

namespace CoPilot.Speedway.View
{
    public partial class Backup : PhoneApplicationPage, INotifyPropertyChanged
    {
        #region COMMANDS

        /// <summary>
        /// Backup Command
        /// </summary>
        public ICommand BackupCommand
        {
            get
            {
                return new RelayCommand(async (param) =>
                {
                    if (UploadProgress.InProgress)
                    {
                        return;
                    }

                    await ProcessBackupUpload();
                }, param => true);
            }
        }

        /// <summary>
        /// Show Command
        /// </summary>
        public ICommand ShowCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    this.ProcessShow((String)param);
                }, param => true);
            }
        }

        /// <summary>
        /// Tap Command
        /// </summary>
        public ICommand TapCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    this.closeMenuIfItsNecessary();
                }, param => true);
            }
        }

        /// <summary>
        /// Main backup Command
        /// </summary>
        public ICommand MainBackupTap
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    this.openMainBackupMenu();
                }, param => true);
            }
        }

        /// <summary>
        /// Download Command
        /// </summary>
        public ICommand DownloadCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    this.downloadBackup((String)param);
                }, param => true);
            }
        }

        /// <summary>
        /// Email Command
        /// </summary>
        public ICommand EmailCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    this.sendEmailLink((String)param);
                }, param => true);
            }
        }

        /// <summary>
        /// OneDriveCommand Command
        /// </summary>
        public ICommand OneDriveCommand
        {
            get
            {
                return new RelayCommand(async (param) =>
                {
                    await showLoginScreen();
                }, param => true);
            }
        }

        /// <summary>
        /// CancelCommand
        /// </summary>
        public ICommand CancelCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    Progress prg = param as Progress;
                    if (prg != null && !prg.Cancel.IsCancellationRequested && prg.Cancel.Token.CanBeCanceled)
                    {
                        try
                        {
                            prg.Cancel.Cancel();
                        }
                        catch { }
                        prg.InProgress = false;
                        prg.Selected = false;
                    }
                }, param => true);
            }
        }

        #endregion

        #region PROPERTY MENU

        /// <summary>
        /// Menu controller
        /// </summary>
        private Controllers.Menu menuController;
        public Controllers.Menu MenuController
        {
            get
            {
                return menuController;
            }
            set
            {
                menuController = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region PROPERTY

        /// <summary>
        /// Download progress
        /// </summary>
        public Progress downloadProgress = new Progress();
        public Progress DownloadProgress
        {
            get
            {
                return downloadProgress;
            }
            set
            {
                downloadProgress = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Upload progress
        /// </summary>
        public Progress uploadProgress = new Progress();
        public Progress UploadProgress
        {
            get
            {
                return uploadProgress;
            }
            set
            {
                uploadProgress = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Ftp controller
        /// </summary>
        private Controllers.Ftp ftpController;
        public Controllers.Ftp FtpController
        {
            get
            {
                return ftpController;
            }
            set
            {
                ftpController = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Data controller
        /// </summary>
        private Controllers.Data dataController;
        public Controllers.Data DataController
        {
            get
            {
                return dataController;
            }
            set
            {
                dataController = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        /// <summary>
        /// Backup
        /// </summary>
        public Backup()
        {
            InitializeComponent();
            InitializeControllers();

            this.createUploadProgress();
            this.createDownloadProgress();
            this.DataContext = this;
        }

        #region CONTROLLERS

        /// <summary>
        /// Initialize all controllers
        /// </summary>
        private void InitializeControllers()
        {
            //create controllers
            this.createMenuController();
        }

        /// <summary>
        /// Create menu controller
        /// </summary>
        private void createMenuController()
        {
            ///CONTROLLER
            MenuController = new Controllers.Menu();
        }

        #endregion

        #region PRIVATE

        /// <summary>
        /// Show login screen
        /// </summary>
        /// <returns></returns>
        private async Task showLoginScreen()
        {
            //visibility
            this.Visibility = System.Windows.Visibility.Collapsed;

            this.SupportedOrientations = SupportedPageOrientation.Portrait;
            await this.FtpController.Login();
            this.SupportedOrientations = SupportedPageOrientation.Landscape;

            //show
            await Task.Delay(500);
            this.Visibility = System.Windows.Visibility.Visible;
        }

        /// <summary>
        /// Process show
        /// </summary>
        /// <param name="name"></param>
        private void ProcessShow(string name)
        {
            switch (name)
            {
                case "MainData":
                    if (!String.IsNullOrEmpty(this.DataController.Backup.Url))
                    {
                        WebBrowserTask task = new WebBrowserTask();
                        task.Uri = new Uri(DataController.Backup.Url);
                        task.Show();
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Downlaod backup
        /// </summary>
        /// <param name="name"></param>
        private async void downloadBackup(string name)
        {
            switch (name)
            {
                case "MainData":
                    await ProcessBackupDownload();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Send email link
        /// </summary>
        /// <param name="p"></param>
        private void sendEmailLink(string name)
        {
            String url = "";
            String id = "";
            switch (name)
            {
                case "MainData":
                    url = DataController.Backup.Url;
                    id = DataController.Backup.Id;
                    break;
                default:
                    break;
            }

            var text = AppResources.SendByEmailBody.Replace("{E}", Environment.NewLine);

            //email
            EmailComposeTask emailComposeTask = new EmailComposeTask();
            emailComposeTask.Subject = AppResources.SendByEmailSubject;
            emailComposeTask.Body = String.Format(text, id, url);
            emailComposeTask.Show();
        }

        /// <summary>
        /// Open main backup menu
        /// </summary>
        private void openMainBackupMenu()
        {
            openMenuIfItsNecessary();
            MenuController.Context = Controllers.MenuContext.MainBackup;
        }



        /// <summary>
        /// ProcessBackupDownload
        /// </summary>
        /// <returns></returns>
        private async Task ProcessBackupDownload()
        {
            if (DownloadProgress.InProgress)
            {
                return;
            }

            //check backup
            Popup.MessageBox box = Popup.MessageBox.Create();
            box.Caption = AppResources.BackupApplyTitle;
            box.Message = AppResources.BackupApplyDescription;
            box.ShowLeftButton = true;
            box.ShowRightButton = true;
            box.LeftButtonText = AppResources.Ok;
            box.RightButtonText = AppResources.Cancel;

            box.Dismiss += async (sender, e1) =>
            {
                switch (e1)
                {
                    case Popup.MessageBoxResult.RightButton:
                    case Popup.MessageBoxResult.None:
                        break;
                    case Popup.MessageBoxResult.LeftButton:
                        await this.downloadNow();
                        break;
                    default:
                        break;
                }
            };

            box.IsOpen = true;
        }

        /// <summary>
        /// Download now
        /// </summary>
        /// <returns></returns>
        private async Task downloadNow()
        {
            //update progress
            DownloadProgress.BytesTransferred = 0;

            //download and apply backup
            DownloadStatus state = await FtpController.Download(DownloadProgress);

            //unselect
            DownloadProgress.Selected = false;
            DownloadProgress.InProgress = false;

            if (state == DownloadStatus.Complete)
            {
                //load data
                DataController.FromBackup();
            }
            else
            {
                Popup.MessageBox box = Popup.MessageBox.Create();
                box.Caption = AppResources.BackupNotFoundTitle;
                box.Message = AppResources.BackupNotFoundDescription;
                box.ShowLeftButton = true;
                box.ShowRightButton = false;
                box.LeftButtonText = AppResources.Ok;
                box.IsOpen = true;
            }
        }

        /// <summary>
        /// ProcessBackupUpload
        /// </summary>
        /// <returns></returns>
        private async Task ProcessBackupUpload()
        {
            UploadProgress.BytesTransferred = 0;

            //upload and save backup
            await FtpController.ProcessBackup(UploadProgress);
        }

        /// <summary>
        /// Create uplaod progress
        /// </summary>
        private void createUploadProgress()
        {
            UploadProgress.BytesTransferred = 0;
            UploadProgress.ProgressPercentage = 0;
            UploadProgress.Selected = true;
            UploadProgress.TotalBytes = 0;
            UploadProgress.Url = new Uri(Controllers.Data.DATA_FILE, UriKind.Relative);
            UploadProgress.Cancel = new System.Threading.CancellationTokenSource();
            UploadProgress.Type = Interfaces.Types.FileType.Data;
            UploadProgress.Preferences = ProgressPreferences.AllowOnCelluralAndBatery;
        }

        /// <summary>
        /// Create downlaod progress
        /// </summary>
        private void createDownloadProgress()
        {
            DownloadProgress.BytesTransferred = 0;
            DownloadProgress.Cancel = new System.Threading.CancellationTokenSource();
            DownloadProgress.ProgressPercentage = 0;
            DownloadProgress.Selected = true;
            DownloadProgress.TotalBytes = 0;
            DownloadProgress.Type = Interfaces.Types.FileType.Data;
            DownloadProgress.Url = new Uri(Controllers.Data.DATA_FILE, UriKind.Relative);
            DownloadProgress.Preferences = ProgressPreferences.AllowOnCelluralAndBatery;
        }

        #endregion

        #region OPEN MENU

        /// <summary>
        /// Open menu if its closed
        /// </summary>
        private void openMenuIfItsNecessary()
        {
            if (!MenuController.IsOpen)
            {
                MenuController.open();
            }
        }

        /// <summary>
        /// Close menu if its closed
        /// </summary>
        private Boolean closeMenuIfItsNecessary()
        {
            if (MenuController.IsOpen)
            {
                MenuController.close();
                return true;
            }
            return false;
        }

        #endregion

        #region GLOBAL EVENTS

        /// <summary>
        /// On navigated to
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var data = NavigationService.GetLastNavigationData();
            if (data != null)
            {
                DataContainer container = data as DataContainer;
                this.FtpController = container.FtpController;
                this.DataController = container.DataController;
            }

            //show message
            if (FtpController.IsOneDriveAvailable)
            {
                Popup.MessageBox box = Popup.MessageBox.Create();
                box.Caption = AppResources.Backup_NotSignedIn;
                box.Message = AppResources.Backup_NotSignedIn_Description;
                box.ShowLeftButton = true;
                box.ShowRightButton = false;
                box.LeftButtonText = AppResources.Ok;
                box.IsOpen = true;
            }

            //connect upload
            FtpController.Connect(this.UploadProgress);

            App.IsInactiveMode = false;
            base.OnNavigatedFrom(e);
        }

        /// <summary>
        /// On navigate from
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            App.IsInactiveMode = (e.Uri.ToString() == "app://external/");
            if (App.IsInactiveMode)
            {
                DataController.StopRecording();
                DataController.Save(true);
            }
            base.OnNavigatingFrom(e);
        }

        /// <summary>
        /// On back key press
        /// </summary>
        /// <param name="e"></param>
        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            //popup
            if (Popup.MessageBox.Hide())
            {
                e.Cancel = true;
            }
            if (e.Cancel == false && MenuController.IsOpen)
            {
                MenuController.close();
                e.Cancel = true;
            }
            base.OnBackKeyPress(e);
        }

        #endregion

        #region PROPERTY CHANGE

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// On property change
        /// </summary>
        /// <param name="name"></param>
        public void OnPropertyChanged(string name)
        {
            App.RootFrame.Dispatcher.BeginInvoke(() =>
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            });
        }

        /// <summary>
        /// Raise proeprty change
        /// </summary>
        /// <param name="caller"></param>
        public void RaisePropertyChanged([CallerMemberName] string caller = "")
        {
            App.RootFrame.Dispatcher.BeginInvoke(() =>
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(caller));
                }
            });
        }

        #endregion
    }
}