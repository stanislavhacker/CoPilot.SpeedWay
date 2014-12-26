using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PR = System.Windows.Controls.Primitives;
using Controllers = CoPilot.Speedway.Controller;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CoPilot.Speedway.Speedway.View;
using System.Threading.Tasks;
using Microsoft.Phone.Info;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Tasks;
using System.Windows.Input;
using CoPilot.Core.Utils;
using OdbCommunicator.OdbEventArg;
using OdbCommunicator;
using OdbCommunicator.OdbCommon;
using CoPilot.Utils.Enums;
using CoreData = CoPilot.Core.Data;
using Windows.System;
using System.Globalization;
using System.Windows.Resources;
using CoPilot.Speedway.Data;
using CoPilot.Utils;
using CoPilot.Speedway.Resources;
using CoPilot.Core.Data;
using Microsoft.Phone.Maps.Controls;
using System.Device.Location;
using System.Collections.ObjectModel;
using CoPilot.Speedway.Controller;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Media.PhoneExtensions;
using CoPilot.Speedway.Data.Convertors;
using System.Text;

namespace CoPilot.Speedway.View
{
    public partial class SpeedWay : PhoneApplicationPage, INotifyPropertyChanged
    {
        #region STATIC

        /// <summary>
        /// Tutorial end
        /// </summary>
        /// <param name="e"></param>
        private static void TutorialEnd(CancelEventArgs e)
        {
            var tutorial = Tutorial.Tutorial.Current;
            if (tutorial != null && tutorial.IsTutorial)
            {
                tutorial.Close();
                e.Cancel = true;
            }
        }

        #endregion

        #region PRIVATE

        private PR.Popup popup;
        private Popup.MessageBox readyBox = null;
        private Popup.MessageBox messageBox = null;
        private SplashScreen screen;
        private BackgroundWorker backroungWorker;
        private DispatcherTimer shareTimer = null;

        #endregion

        #region COMMANDS

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
        /// ViewCircuit Command
        /// </summary>
        public ICommand ViewCircuitCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    this.closeMenuIfItsNecessary();
                    this.showCircuit((Circuit) param);
                }, param => true);
            }
        }

        /// <summary>
        /// ViewCircuitGroups Command
        /// </summary>
        public ICommand ViewCircuitGroupsCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    this.closeMenuIfItsNecessary();
                    var group = (CircuitGroup)param;
                    this.showCircuit(group.Circuit);
                }, param => true);
            }
        }

        /// <summary>
        /// Zoom Command
        /// </summary>
        public ICommand ZoomCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    var map = this.SpeedWayMap;
                    map.SetView(map.Center, Zoom + 2, Microsoft.Phone.Maps.Controls.MapAnimationKind.Parabolic);
                }, param => true);
            }
        }

        /// <summary>
        /// Start Command
        /// </summary>
        public ICommand StartCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    this.waitCircuit();
                }, param => true);
            }
        }
        /// <summary>
        /// Stop Command
        /// </summary>
        public ICommand StopCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    DataController.StopRecording();
                }, param => true);
            }
        }
        /// <summary>
        /// Cancel Command
        /// </summary>
        public ICommand CancelCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    this.closeMenuIfItsNecessary();
                    this.showCircuit(null);
                }, param => true);
            }
        }

        /// <summary>
        /// ShareMediaCommand
        /// </summary>
        public ICommand ShareMediaCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    this.shareMediaTask();
                }, param => true);
            }
        }

        /// <summary>
        /// Tutorial Command
        /// </summary>
        public ICommand TutorialCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    Tutorial.Tutorial.Current.IsTutorial = true;
                    this.closeMenuIfItsNecessary();
                }, param => true);
            }
        }

        /// <summary>
        /// CoPilotCommand Command
        /// </summary>
        public ICommand CoPilotCommand
        {
            get
            {
                return new RelayCommand(async (param) =>
                {
                    await Launcher.LaunchUriAsync(new Uri("copilot:run"));
                }, param => true);
            }
        }

        /// <summary>
        /// Races Command
        /// </summary>
        public ICommand RacesTap
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    this.openRacesMenu();
                }, param => true);
            }
        }

        /// <summary>
        /// Best Command
        /// </summary>
        public ICommand BestTap
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    this.openBestMenu();
                }, param => true);
            }
        }

        /// <summary>
        /// Share Command
        /// </summary>
        public ICommand ShareTap
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    this.openShareMenu();
                    this.createShareImage();
                }, param => true);
            }
        }

        /// <summary>
        /// More Command
        /// </summary>
        public ICommand MoreTap
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    this.openMoreMenu();
                }, param => true);
            }
        }

        /// <summary>
        /// Gps toggle
        /// </summary>
        public ICommand GpsToggleCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    DataController.IsGpsAllowed = !DataController.IsGpsAllowed;
                    GpsController.IsGpsAllowed = DataController.IsGpsAllowed;
                }, param => true);
            }
        }

        /// <summary>
        /// Obd toggle
        /// </summary>
        public ICommand ObdToggleCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    DataController.IsObdAllowed = !DataController.IsObdAllowed;
                    BluetoothController.IsObdAllowed = DataController.IsObdAllowed;
                }, param => true);
            }
        }

        /// <summary>
        /// Bluetooth command
        /// </summary>
        public ICommand BluetoothCommand
        {
            get
            {
                return new RelayCommand(async (param) =>
                {
                    switch (BluetoothController.ErrorType)
                    {
                        case BluetoothErrorType.NotEnabled:
                            await Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:"));
                            break;
                        case BluetoothErrorType.NotSelected:
                            NavigationService.Navigate("/Speedway/View/Bluetooth.xaml", this.GetDefaultDataContainer());
                            break;
                        case BluetoothErrorType.NotFound:
                        case BluetoothErrorType.NoDevice:
                        case BluetoothErrorType.OutOfRange:
                        case BluetoothErrorType.FatalError:
                            BluetoothController.Scan();
                            break;
                        case BluetoothErrorType.None:
                        case BluetoothErrorType.NotAllowed:
                        default:
                            break;
                    }
                }, param => true);
            }
        }

        /// <summary>
        /// ObdView command
        /// </summary>
        public ICommand ObdViewCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    NavigationService.Navigate("/Speedway/View/ObdView.xaml", this.GetDefaultDataContainer());
                }, param => true);
            }
        }

        /// <summary>
        /// Change distance command
        /// </summary>
        public ICommand ChangeDistanceCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    CoreData.Distance currency = (CoreData.Distance)Enum.Parse(typeof(CoreData.Distance), (String)param);
                    DataController.Distance = currency;
                }, param => true);
            }
        }

        /// <summary>
        /// Backup command
        /// </summary>
        public ICommand BackupCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    NavigationService.Navigate("/Speedway/View/Backup.xaml", this.GetDefaultDataContainer());
                }, param => true);
            }
        }

        /// <summary>
        /// Rate Command
        /// </summary>
        public ICommand RateCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    MarketplaceReviewTask review = new MarketplaceReviewTask();
                    review.Show();
                }, param => true);
            }
        }

        /// <summary>
        /// Social command
        /// </summary>
        public ICommand SocialCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    this.openUrl(param);
                }, param => true);
            }
        }

        /// <summary>
        /// Privacy policy command
        /// </summary>
        public ICommand PrivacyPolicyCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    this.privacyPolicy();
                }, param => true);
            }
        }

        #endregion

        #region PROPERTY

        /// <summary>
        /// Share image path
        /// </summary>
        private string shareImagePath = null;
        public string ShareImagePath
        {
            get
            {
                return shareImagePath;
            }
            set
            {
                shareImagePath = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Map Zoom
        /// </summary>
        /// <returns></returns>
        private Double zoom = 14;
        public Double Zoom
        {
            get
            {
                return zoom;
            }
            set
            {
                zoom = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Device is on external power source
        /// </summary>
        /// <returns></returns>
        public Boolean IsExternaPowerSource
        {
            get
            {
                return DeviceStatus.PowerSource == PowerSource.External;
            }
        }

        /// <summary>
        /// IsWaiting
        /// </summary>
        /// <returns></returns>
        private Boolean isWaiting = false;
        public Boolean IsWaiting
        {
            get
            {
                return isWaiting;
            }
            set
            {
                isWaiting = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// IsRacing
        /// </summary>
        /// <returns></returns>
        private Boolean isRacing = false;
        public Boolean IsRacing
        {
            get
            {
                return isRacing;
            }
            set
            {
                isRacing = value;
                RaisePropertyChanged();
                RaisePropertyChanged("IsLowSignal");
            }
        }

        /// <summary>
        /// IsMoving
        /// </summary>
        /// <returns></returns>
        private Boolean isMoving = false;
        public Boolean IsMoving
        {
            get
            {
                return isMoving;
            }
            set
            {
                isMoving = value;
                RaisePropertyChanged();
                RaisePropertyChanged("IsStartEnabled");
                RaisePropertyChanged("IsStopMoving");
            }
        }

        /// <summary>
        /// IsViewing
        /// </summary>
        /// <returns></returns>
        private Boolean isViewing = false;
        public Boolean IsViewing
        {
            get
            {
                return isViewing;
            }
            set
            {
                isViewing = value;
                RaisePropertyChanged();
                RaisePropertyChanged("IsLowSignal");
                RaisePropertyChanged("IsStartEnabled");
                RaisePropertyChanged("IsStopMoving");
            }
        }

        /// <summary>
        /// Circuit
        /// </summary>
        private Circuit circuit = null;
        public Circuit Circuit
        {
            get
            {
                return circuit;
            }
            set
            {
                circuit = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// CanShare
        /// </summary>
        /// <returns></returns>
        private Boolean canShare = false;
        public Boolean CanShare
        {
            get
            {
                return canShare;
            }
            set
            {
                canShare = value;
                RaisePropertyChanged();
            }
        }



        /// <summary>
        /// IsLowSignal
        /// </summary>
        /// <returns></returns>
        public bool IsLowSignal
        {
            get
            {
                return !GpsController.IsAccured && !IsRacing && !IsViewing;
            }
        }

        /// <summary>
        /// IsStartEnabled
        /// </summary>
        /// <returns></returns>
        public bool IsStartEnabled
        {
            get
            {
                return GpsController.IsAccured && !IsMoving && !IsViewing;
            }
        }

        /// <summary>
        /// IsStopMoving
        /// </summary>
        /// <returns></returns>
        public bool IsStopMoving
        {
            get
            {
                return GpsController.IsAccured && IsMoving && !IsRacing && !IsViewing;
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

        #region PROPERTY GPS

        /// <summary>
        /// Gps controller
        /// </summary>
        private Controllers.Gps gpsController;
        public Controllers.Gps GpsController
        {
            get
            {
                return gpsController;
            }
            set
            {
                gpsController = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region PROPERTY BLUETOOTH

        /// <summary>
        /// Bluetooth controller
        /// </summary>
        private Controllers.Bluetooth bluetoothController;
        public Controllers.Bluetooth BluetoothController
        {
            get
            {
                return bluetoothController;
            }
            set
            {
                bluetoothController = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region PROPERTY DATA

        /// <summary>
        /// Date controller
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
                App.DataController = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region PROPERTY FTP

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
                App.FtpController = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        /// <summary>
        /// Sppedway
        /// </summary>
        public SpeedWay()
        {
            InitializeComponent();
            CreateSplashScreen();
            StartLoadingData();
        }

        #region SPLASH SCREEN

        /// <summary>
        /// Create splash screen
        /// </summary>
        private void CreateSplashScreen()
        {
            this.screen = new SplashScreen();
            this.screen.Width = Application.Current.Host.Content.ActualWidth;
            this.screen.Height = Application.Current.Host.Content.ActualHeight;

            this.popup = new PR.Popup();
            this.popup.Child = screen;
            this.popup.IsOpen = true;
        }

        /// <summary>
        /// Start loading data
        /// </summary>
        private void StartLoadingData()
        {            
            backroungWorker = new BackgroundWorker();
            backroungWorker.DoWork += startLoading;
            backroungWorker.RunWorkerCompleted += completeLoading;
            backroungWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Loading complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void completeLoading(object sender, RunWorkerCompletedEventArgs e)
        {
            var timeout = 300;

            this.Dispatcher.BeginInvoke(async () =>
            {
                await Task.Delay(timeout * 3);
                this.screen.Animate(timeout);
                await Task.Delay(timeout * 2);
                this.popup.IsOpen = false;
            });
        }

        /// <summary>
        /// Loading start
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startLoading(object sender, DoWorkEventArgs e)
        {
            //tutorial
            Tutorial.Tutorial.CoPilotApp = this;

            //init
            InitializeEvents();
            InitializeControllers();
            ResolveLicense();

            this.Dispatcher.BeginInvoke(() =>
            {
                this.setupControllers();
                this.DataContext = this;
            });
        }

        /// <summary>
        /// Resolve license
        /// </summary>
        private void ResolveLicense()
        {
        }

        #endregion

        #region CONTROLLERS

        /// <summary>
        /// Initialize all controllers
        /// </summary>
        private void InitializeControllers()
        {
            //create controllers
            this.createDataController();
            this.createMenuController();
            this.createGpsController();
            this.createBluetoothController();
            this.createFtpController();
        }

        /// <summary>
        /// Set up controllers
        /// </summary>
        private void setupControllers()
        {
            //data
            DistanceExchange.CurrentDistance = DataController.Distance;

            //gps
            GpsController.IsGpsAllowed = DataController.IsGpsAllowed;

            //ftp
            FtpController.IsWifiEnabled = DeviceNetworkInformation.IsNetworkAvailable && DeviceNetworkInformation.IsWiFiEnabled;
            FtpController.IsNetEnabled = DeviceNetworkInformation.IsWiFiEnabled || DeviceNetworkInformation.IsCellularDataEnabled;
            FtpController.DataController = this.DataController;
            FtpController.TryLogin();

            //bluetooth
            BluetoothController.PreselectedDevice = DataController.ObdDevice;
            BluetoothController.IsObdAllowed = DataController.IsObdAllowed;
            BluetoothController.Scan();

            //send error
            this.sendErrorIfExists();
        }

        /// <summary>
        /// Create menu controller
        /// </summary>
        private void createMenuController()
        {
            ///CONTROLLER
            MenuController = new Controllers.Menu();
        }

        /// <summary>
        /// Create bluetooth controller
        /// </summary>
        private void createBluetoothController()
        {
            BluetoothController = new Controller.Bluetooth();

            //Events
            BluetoothController.DataReceive += (object sender, OdbEventArgs e) =>
            {
                if (BluetoothController.IsSuccess)
                {
                    this.getAllResponses(e);
                }
            };
            BluetoothController.SelectedDeviceChange += (object sender, EventArgs e) =>
            {
                DataController.ObdDevice = BluetoothController.PreselectedDevice;
            };
        }

        /// <summary>
        /// Create gps controller
        /// </summary>
        private void createGpsController()
        {
            ///CONTROLLER
            GpsController = new Controllers.Gps();

            //Events
            GpsController.onChange += (object sender, EventArgs e) =>
            {
                DataController.Position = gpsController.Current;
                if (!Double.IsNaN(gpsController.Speed) && !BluetoothController.IsSuccess)
                {
                    DataController.Speed = gpsController.Speed;
                }
            };
            GpsController.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == "IsAccured")
                {
                    RaisePropertyChanged("IsLowSignal");
                    RaisePropertyChanged("IsStartEnabled");
                    RaisePropertyChanged("IsStopMoving");
                }
            };
        }

        /// <summary>
        /// Create data controller
        /// </summary>
        private void createDataController()
        {
            ///CONTROLLER
            DataController = new Controllers.Data();

            //Events
            DataController.onUnitsChange += (object sender, EventArgs e) =>
            {
                DistanceExchange.CurrentDistance = DataController.Distance;
            };
            dataController.onStartRecording += (object sender, EventArgs e) =>
            {
                //waiting, ok
                this.IsWaiting = false;
                this.IsRacing = true;
                //map zoom
                this.Zoom = 16;
                //menu close
                this.closeMenuIfItsNecessary();
                //hide dialog
                Popup.MessageBox.Hide();
            };
            dataController.onStopRecording += (object sender, EventArgs e) =>
            {
                //type
                StopType type = (StopType)sender;
                Circuit circle;
                //waiting, ok
                this.IsWaiting = false;
                this.IsRacing = false;

                //show errors
                switch (type)
                {
                    case StopType.NoLap:
                        this.showLapMessage(AppResources.NoLap_Title, AppResources.NoLap_Description);
                        break;
                    case StopType.WrongLap:
                        this.showLapMessage(AppResources.WrongLap_Title, AppResources.WrongLap_Description);
                        //show previous laps
                        circle = dataController.Circuits.First();
                        this.showCircuit(circle);
                        break;
                    default:
                        //show if is valid
                        circle = dataController.Circuits.First();
                        this.showCircuit(circle);
                        break;
                }


            };
            DataController.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                //speed check
                if (e.PropertyName == "Speed")
                {
                    //moving
                    this.IsMoving = DataController.Speed > 0;
                }
                //start circuit recording
                if (e.PropertyName == "Speed" && this.IsWaiting && DataController.Speed > 0) {
                    //start
                    var circuit = new CoPilot.Core.Data.Circuit();
                    DataController.StartRecording(circuit);
                }
            };
        }

        /// <summary>
        /// Create ftp controller
        /// </summary>
        private void createFtpController()
        {
            ///CONTROLLER
            FtpController = new Controllers.Ftp();

            //state change
            FtpController.OnStateChange += async (object sender, Interfaces.EventArgs.StateEventArgs e) =>
            {
                if (e.State == Interfaces.EventArgs.ConnectionStatus.Connected)
                {
                    //try backup now
                    await FtpController.BackupNow(DeviceNetworkInformation.IsNetworkAvailable && DataController.IsBackupOnStart);

                    //save data
                    Settings.Add("StorageConnected", (e.State == Interfaces.EventArgs.ConnectionStatus.Connected).ToString());
                }
            };

            FtpController.Error += (object sender, Interfaces.EventArgs.ErrorEventArgs e) =>
            {
                //save data
                Settings.Add("StorageConnected", false.ToString());
                FtpController.IsLogged = false;
            };
        }

        #endregion

        #region DATA CONTAINER

        /// <summary>
        /// Get default data container
        /// </summary>
        /// <returns></returns>
        private DataContainer GetDefaultDataContainer()
        {
            DataContainer data = new DataContainer();
            data.DataController = this.DataController;
            data.BluetoothController = this.BluetoothController;
            data.FtpController = this.FtpController;
            return data;
        }

        /// <summary>
        /// Get uri data container
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private DataContainer GetUriDataContainer(Uri uri)
        {
            DataContainer data = this.GetDefaultDataContainer();
            data.Uri = uri;
            return data;
        }

        #endregion

        #region CIRCUIT

        /// <summary>
        /// Start circuit
        /// </summary>
        private void waitCircuit()
        {
            //messages
            this.readyBox = Popup.MessageBox.Create();
            this.readyBox.Caption = AppResources.StartTitle;
            this.readyBox.Message = AppResources.StartDescription;
            this.readyBox.RightButtonText = AppResources.Cancel;
            this.readyBox.ShowLeftButton = false;
            this.readyBox.ShowRightButton = true;
            this.readyBox.Dismiss += (object sender, Popup.MessageBoxResult e) =>
            {
                //waiting
                this.IsWaiting = false;
            };
            //waiting
            this.IsWaiting = true;
            //open
            this.readyBox.IsOpen = true;
        }


        /// <summary>
        /// Show circuit
        /// </summary>
        /// <param name="circuit"></param>
        private void showCircuit(Circuit circuit)
        {
            if (circuit == null)
            {
                //data
                this.IsViewing = false;
                this.Circuit = null;
                this.GpsController.UseCenter = true;
                this.Zoom = 14;

                //path
                this.DataController.Path = null;
                this.DataController.Laps = null;
                this.DataController.Name = null;
            }
            else
            {
                //data
                this.IsViewing = true;
                this.Circuit = circuit;
                this.GpsController.UseCenter = false;

                //colection
                var collection = new GeoCoordinateCollection();
                var position = CoPilot.Speedway.Controller.Data.getPositions(circuit);
                foreach (var pos in position)
                {
                    collection.Add(new GeoCoordinate(pos.Latitude, pos.Longitude));
                }

                //time
                var times = new ObservableCollection<TimeSpan>();
                foreach (var time in circuit.Laps)
                {
                    times.Add(new TimeSpan(0, 0, 0, 0, (int)time));
                }

                //path
                this.DataController.Path = collection;
                this.DataController.Laps = times;
                this.DataController.Name = circuit.Name;
                //center
                LocationRectangle.CreateBoundingRectangle(collection);
                this.SpeedWayMap.SetView(LocationRectangle.CreateBoundingRectangle(collection), MapAnimationKind.Linear);
                //share image
                this.createShareImage();
            }
        }

        /// <summary>
        /// Create share image
        /// </summary>
        /// <param name="circuit"></param>
        private void createShareImage()
        {
            //clear
            this.ShareImagePath = null;
            this.CanShare = false;

            //circuit
            var circut = this.Circuit;
            if (circut == null)
            {
                return;
            }

            //timer
            if (shareTimer != null)
            {
                shareTimer.Stop();
            }

            //wait a while
            shareTimer = new DispatcherTimer();
            shareTimer.Interval = TimeSpan.FromSeconds(1);
            shareTimer.Tick += delegate
            {
                //stop
                shareTimer.Stop();
                //check circuit
                if (circuit == null)
                {
                    return;
                }

                //name
                var path = "/shared/" + circuit.Id + ".jpg";
                var map = this.SpeedWayMap;
                //load bitmap
                var bitmap = new WriteableBitmap(map, null);
                var stream = Storage.OpenFile(path, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite, System.IO.FileShare.ReadWrite);
                //using
                using (stream)
                {
                    bitmap.SaveJpeg(stream, (int)map.ActualWidth, (int)map.ActualHeight, 0, 100);
                }
                //set path
                this.ShareImagePath = path;
                this.CanShare = true;
            };
            shareTimer.Start();

        }

        #endregion

        #region PRIVATE

        /// <summary>
        /// Share media task
        /// </summary>
        private void shareMediaTask()
        {
            //library
            var lib = new MediaLibrary();
            Microsoft.Xna.Framework.Media.Picture picture;
            //stream from image
            var stream = Storage.OpenFile(this.ShareImagePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
            //save picture
            using (stream)
            {
                picture = lib.SavePicture(this.Circuit.Id + ".jpg", stream);
            }
            //share
            ShareMediaTask task = new ShareMediaTask();
            task.FilePath = picture.GetPath();
            task.Show();
        }

        /// <summary>
        /// Open url
        /// </summary>
        /// <param name="param"></param>
        private void openUrl(object param)
        {
            String type = (String)param;
            String url;

            switch (type)
            {
                case "Facebook":
                    url = "https://www.facebook.com/carcopilot";
                    break;
                case "Twitter":
                    url = "https://twitter.com/carcopilot";
                    break;
                case "GooglePlus":
                    url = "https://plus.google.com/u/0/b/115628070739534024707/115628070739534024707/posts";
                    break;
                case "Donate":
                    url = "https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=LVCK2P82YFGJ6";
                    break;
                case "Blog":
                    url = "http://carcopilot.blogspot.cz";
                    break;
                default:
                    url = type;
                    break;
            }

            WebBrowserTask www = new WebBrowserTask();
            www.Uri = new Uri(url);
            www.Show();
        }

        /// <summary>
        /// Privacy policy
        /// </summary>
        private void privacyPolicy()
        {
            var culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            StreamResourceInfo resource;
            Uri uri;
            try
            {
                uri = new Uri("Resources/PrivacyPolicy/" + culture + ".html", UriKind.Relative);
                resource = App.GetResourceStream(uri);
            }
            catch
            {
                uri = new Uri("Resources/PrivacyPolicy/en.html", UriKind.Relative);
                resource = App.GetResourceStream(uri);
            }
            NavigationService.Navigate("/Speedway/View/WebBrowser.xaml", this.GetUriDataContainer(uri));
        }

        /// <summary>
        /// Get all responses
        /// </summary>
        /// <param name="e"></param>
        private void getAllResponses(OdbEventArgs e)
        {
            this.getResponseFor(e.Client, OdbPids.Mode1_VehicleSpeed, data => DataController.Speed = data);
        }

        /// <summary>
        /// Get response for
        /// </summary>
        /// <param name="client"></param>
        /// <param name="odbPid"></param>
        /// <param name="action"></param>
        private void getResponseFor(OdbClient client, OdbPid odbPid, Action<Double> action)
        {
            var response = client.RequestFor(odbPid);
            if (response != null)
            {
                action(response.Data);
            }
        }

        /// <summary>
        /// Send error if exists
        /// </summary>
        private void sendErrorIfExists()
        {
            var error = Settings.Get("error");
            if (error != null)
            {
                var result = MessageBox.Show(AppResources.ReportDescription, AppResources.ReportTitle, MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    EmailComposeTask email = new EmailComposeTask();
                    email.To = "stanislav.hacker@live.com";
                    email.Subject = "Error in Co-Pilot app";

                    var message = new StringBuilder();
                    message.AppendLine("There is an error from application:");
                    message.AppendLine(Environment.NewLine);
                    message.AppendLine(error);

                    email.Body = message.ToString();
                    email.Show();
                }

                Settings.Remove("error");
                Settings.Save();
            }
        }

        #endregion

        #region OPEN MENU

        /// <summary>
        /// Open camera menu
        /// </summary>
        /// <returns></returns>
        private void openRacesMenu()
        {
            openMenuIfItsNecessary();
            MenuController.Context = Controllers.MenuContext.Races;
        }

        /// <summary>
        /// Open fuel menu
        /// </summary>
        /// <returns></returns>
        private void openBestMenu()
        {
            openMenuIfItsNecessary();
            MenuController.Context = Controllers.MenuContext.Best;
        }

        /// <summary>
        /// Open repair menu
        /// </summary>
        /// <returns></returns>
        private void openShareMenu()
        {
            openMenuIfItsNecessary();
            MenuController.Context = Controllers.MenuContext.Share;
        }

        /// <summary>
        /// Open more menu
        /// </summary>
        /// <returns></returns>
        private void openMoreMenu()
        {
            openMenuIfItsNecessary();
            MenuController.Context = Controllers.MenuContext.Other;
        }

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
        private void closeMenuIfItsNecessary()
        {
            if (MenuController.IsOpen)
            {
                MenuController.close();
            }
        }

        #endregion

        #region GLOBAL EVENTS

        /// <summary>
        /// Bind events on page
        /// </summary>
        private void InitializeEvents()
        {
            App.RootFrame.Obscured += onScreenLock;
            App.RootFrame.Unobscured += onScreenUnlock;
            DeviceStatus.PowerSourceChanged += triggerPowerSourceChanged;
            DeviceNetworkInformation.NetworkAvailabilityChanged += triggerNetworkAvailabilityChanged;
            this.OrientationChanged += triggerOrientationChanged;
        }

        /// <summary>
        /// Trigger newtwork change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void triggerNetworkAvailabilityChanged(object sender, NetworkNotificationEventArgs e)
        {

        }

        /// <summary>
        /// Trigger power source change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void triggerPowerSourceChanged(object sender, EventArgs e)
        {
            OnPropertyChanged("IsExternaPowerSource");
        }

        /// <summary>
        /// Orientation change event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void triggerOrientationChanged(object sender, OrientationChangedEventArgs e)
        {

        }

        /// <summary>
        /// On back key press
        /// </summary>
        /// <param name="e"></param>
        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            //popup
            if (this.messageBox == null && Popup.MessageBox.Hide())
            {
                e.Cancel = true;
            }

            //try end tutorial
            SpeedWay.TutorialEnd(e);

            //close menu
            if (e.Cancel == false && MenuController != null && MenuController.IsOpen)
            {
                MenuController.close();
                e.Cancel = true;
            }

            //cancel path
            if (e.Cancel == false && this.IsViewing)
            {
                e.Cancel = true;
                this.showCircuit(null);
            }

            //dialog before exit
            if (e.Cancel == false && this.messageBox == null)
            {
                e.Cancel = true;
                this.exitConfirmMessageBox();
            }

            //stop all
            if (e.Cancel == false)
            {
                DataController.StopRecording();
            }
            base.OnBackKeyPress(e);
        }

        /// <summary>
        /// Exit confirm box
        /// </summary>
        private void exitConfirmMessageBox()
        {
            this.messageBox = Popup.MessageBox.Create();
            this.messageBox.Caption = AppResources.AppEndTitle;
            this.messageBox.Message = AppResources.AppEndDescription;
            this.messageBox.ShowLeftButton = false;
            this.messageBox.ShowRightButton = true;
            this.messageBox.RightButtonText = AppResources.Cancel;

            this.messageBox.Dismiss += (sender, e1) =>
            {
                this.messageBox = null;
            };

            this.messageBox.IsOpen = true;
        }

        /// <summary>
        /// Show lap message
        /// </summary>
        /// <param name="title"></param>
        /// <param name="description"></param>
        private void showLapMessage(string title, string description)
        {
            var msg = Popup.MessageBox.Create();
            msg.Caption = title;
            msg.Message = description;
            msg.ShowLeftButton = true;
            msg.ShowRightButton = false;
            msg.LeftButtonText = AppResources.Ok;
            msg.IsOpen = true;
        }

        /// <summary>
        /// On navigate on page
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var fromInactive = e.NavigationMode == NavigationMode.New || App.IsInactiveMode;

            if (fromInactive)
            {
                ResolveLicense();
            }
            App.IsInactiveMode = false;
            base.OnNavigatedTo(e);
        }

        /// <summary>
        /// Save data
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            App.IsInactiveMode = (e.Uri.ToString() == "app://external/");
            if (e.NavigationMode == NavigationMode.Back || App.IsInactiveMode)
            {
                DataController.StopRecording();
                DataController.Save(true);
            }
            base.OnNavigatingFrom(e);
        }

        /// <summary>
        /// on screen lock
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onScreenLock(object sender, ObscuredEventArgs e)
        {
            if (DataController != null)
            {
                DataController.Save(true);
            }
        }

        /// <summary>
        /// on screen unlock
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onScreenUnlock(object sender, EventArgs e)
        {

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