using CoPilot.Core.Data;
using CoPilot.Core.Utils;
using CoPilot.Speedway.Resources;
using CoPilot.Utils;
using GpsCalculation;
using Microsoft.Phone.Maps.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CoPilot.Speedway.Controller
{
    public enum RemoveType
    {
        SoftDelete,
        HardDelete
    }

    public enum StopType
    {
        Ok,
        NoLap,
        WrongLap
    }

    public class Data : Base
    {
        public const string DATA_FILE_NAME = "co-pilot-speedway.xml";
        public const string DATA_FILE = "/shared/transfers/" + DATA_FILE_NAME;

        //PRIVATE
        private Boolean changed = true;
        private Boolean returning = false;
        private Records data;
        private DispatcherTimer timer;
        private DispatcherTimer uptimer;
        private DateTime saved = DateTime.Now;
        private DateTime lapTime = DateTime.Now;
        private Circuit currentRecord = null;
        private GeoCircle geoCircle = null;
        private int from = 0;

        #region EVENTS

        public event EventHandler onStartRecording;
        public event EventHandler onStopRecording;
        public event EventHandler onUnitsChange;

        #endregion

        #region GET

        /// <summary>
        /// Is backup available
        /// </summary>
        public bool IsBackupAvailable
        {
            get
            {
                return data.Backup != null && !String.IsNullOrEmpty(data.Backup.Id);
            }
        }

        /// <summary>
        /// Records
        /// </summary>
        public Records Records
        {
            get
            {
                return data;
            }
        }

        #endregion

        #region State

        /// <summary>
        /// Name
        /// </summary>
        private string name = null;
        public String Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Position
        /// </summary>
        private GeoCoordinate position = GeoCoordinate.Unknown;
        public GeoCoordinate Position
        {
            get
            {
                return position;
            }
            set
            {
                if (position != null && value != null)
                {
                    if (position.Latitude == value.Latitude &&
                        position.Longitude == value.Longitude &&
                        position.Altitude == value.Altitude &&
                        position.HorizontalAccuracy == value.HorizontalAccuracy)
                    {
                        return;
                    }
                }
                position = value == null ? GeoCoordinate.Unknown : value;
                RaisePropertyChanged();
                triggerChange();
            }
        }

        /// <summary>
        /// Path
        /// </summary>
        private GeoCoordinateCollection path = null;
        public GeoCoordinateCollection Path
        {
            get
            {
                return path;
            }
            set
            {
                path = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Laps
        /// </summary>
        private ObservableCollection<TimeSpan> laps = new ObservableCollection<TimeSpan>();
        public ObservableCollection<TimeSpan> Laps
        {
            get
            {
                return laps;
            }
            set
            {
                laps = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Speed
        /// </summary>
        private Double speed = 0;
        public Double Speed
        {
            get
            {
                return speed;
            }
            set
            {
                if (speed == value)
                {
                    return;
                }
                speed = value;
                RaisePropertyChanged();
                RaisePropertyChanged("SpeedInfo");
                triggerChange();
            }
        }

        /// <summary>
        /// Speed info
        /// </summary>
        public String SpeedInfo
        {
            get
            {
                return Math.Round(DistanceExchange.GetOdometerWithRightDistance(new Odometer(this.speed, Distance.Km)), 1) + " " + this.Distance + " " + AppResources.PerHour;
            }
        }

        /// <summary>
        /// Uptime
        /// </summary>
        public TimeSpan Uptime
        {
            get
            {
                if (this.currentRecord == null)
                {
                    return TimeSpan.FromSeconds(0);
                }
                return DateTime.Now - this.lapTime;
            }
        }

        #endregion

        #region Setting

        /// <summary>
        /// Is gps Allowed
        /// </summary>
        public bool IsGpsAllowed
        {
            get
            {
                return data.GpsAllowed;
            }
            set
            {
                data.GpsAllowed = value;
                RaisePropertyChanged();
                this.Save();
            }
        }

        /// <summary>
        /// Is obd allowed
        /// </summary>
        public bool IsObdAllowed
        {
            get
            {
                return data.ObdAllowed;
            }
            set
            {
                data.ObdAllowed = value;
                RaisePropertyChanged();
                this.Save();
            }
        }

        /// <summary>
        /// Is backup on start allowed
        /// </summary>
        public bool IsBackupOnStart
        {
            get
            {
                return data.BackupOnStart;
            }
            set
            {
                data.BackupOnStart = value;
                RaisePropertyChanged();
                this.Save();
            }
        }

        /// <summary>
        /// Distance
        /// </summary>
        public Distance Distance
        {
            get
            {
                return data.Distance;
            }
            set
            {
                data.Distance = value;

                if (onUnitsChange != null)
                {
                    onUnitsChange.Invoke(this, EventArgs.Empty);
                }

                RaisePropertyChanged();
                refresh();
                this.Save();
            }
        }

        /// <summary>
        /// Obd device
        /// </summary>
        public String ObdDevice
        {
            get
            {
                return data.ObdDevice;
            }
            set
            {
                if (data.ObdDevice == value)
                {
                    return;
                }
                data.ObdDevice = value;
                RaisePropertyChanged();
                this.Save();
            }
        }

        /// <summary>
        /// Id
        /// </summary>
        public String Id
        {
            get
            {
                return data.Id;
            }
        }

        /// <summary>
        /// Backup
        /// </summary>
        public BackupInfo Backup
        {
            get
            {
                return data.Backup;
            }
            set
            {
                if (data.Backup == value)
                {
                    return;
                }
                data.Backup = value;
                RaisePropertyChanged();
                RaisePropertyChanged("IsBackupAvailable");
                this.Save();
            }
        }


        #endregion

        #region Circ

        /// <summary>
        /// Fills
        /// </summary>
        public ObservableCollection<Circuit> Circuits
        {
            get
            {
                return data.Times.Circuits;
            }
        }

        #endregion

        #region Spaces

        /// <summary>
        /// Size
        /// </summary>
        public Double Size
        {
            get
            {
                return Math.Round(data.Size / 1048576, 3);
            }
        }


        #endregion

        /// <summary>
        /// Data collector
        /// </summary>
        public Data()
        {
            var stream = this.openStreamForRead(DATA_FILE);
            data = Records.Load(stream);
        }

        /// <summary>
        /// Start timer
        /// </summary>
        public void StartRecording(Circuit record)
        {
            //disable call spamming
            if (this.currentRecord != null) {
                return;
            }
            //save current record
            this.currentRecord = record;
            //lap time
            this.lapTime = DateTime.Now;

            //set data
            this.currentRecord.Start = lapTime;
            this.currentRecord.States = new List<State>();
            this.currentRecord.Laps = new List<double>();

            //path
            this.Path = new GeoCoordinateCollection();
            this.Laps = new ObservableCollection<TimeSpan>();
            //clear returning
            this.returning = false;

            //start timer
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(200);
            timer.Tick += delegate
            {
                if (changed)
                {
                    createRecord();
                    changed = false;
                }
            };
            timer.Start();

            //star uptimer
            uptimer = new DispatcherTimer();
            uptimer.Interval = TimeSpan.FromMilliseconds(20);
            uptimer.Tick += delegate
            {
                RaisePropertyChanged("Uptime");
            };
            uptimer.Start();

            //start
            if (onStartRecording != null)
            {
                onStartRecording.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Stop recording
        /// </summary>
        public void StopRecording(StopType type = StopType.Ok)
        {
            var record = this.currentRecord;
            //clear and save
            this.currentRecord = null;
            this.geoCircle = null;
            this.from = 0;
            //stop timer
            if (this.timer != null && this.timer.IsEnabled)
            {
                this.timer.Stop();
                this.timer = null;
                this.uptimer.Stop();
                this.uptimer = null;
            }
            //no positions
            if (record == null)
            {
                return;
            }

            //save if valid
            if (record.Laps.Count >= 1)
            {
                //set id
                record.Id = this.retriveCircuitId(record);
                //set name
                record.Name = this.retriveCircuitName(record);
                //set duration
                record.Duration = DateTime.Now - record.Start;
                //save
                data.Times.Circuits.Insert(0, record);
                //save
                this.Save(true);
            }
            else
            {
                type = StopType.NoLap;
            }

            //clear
            this.Path = null;
            this.Laps = new ObservableCollection<TimeSpan>();

            //stop
            if (onStopRecording != null)
            {
                onStopRecording.Invoke(type, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Save
        /// </summary>
        public void Save(Boolean force = false)
        {
            var seconds = DateTime.Now.Subtract(saved).TotalSeconds;
            if (seconds > 15 || force)
            {
                var stream = this.openStreamForWrite(DATA_FILE);
                saved = DateTime.Now;
                data.Change = DateTime.Now;
                data.Save(stream);
                OnPropertyChanged("Size");
            }
        }

        /// <summary>
        /// From backup
        /// </summary>
        public void FromBackup()
        {
            var stream = this.openStreamForRead(DATA_FILE);
            this.data = Records.Load(stream);
            //set globals
            DistanceExchange.CurrentDistance = this.Distance;
            //refresh
            RaisePropertiesChanged();
        }

        #region PRIVATES

        /// <summary>
        /// Retrive circuit id
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        private string retriveCircuitId(Circuit record)
        {
            //records
            var records = this.Circuits;
            var current = Data.getPositions(record);

            //records
            foreach (var r in records)
            {
                //load geopositions
                var positions = Data.getPositions(r);
                //get similar paths
                Double similar = Geo.CirclesAreSimilar(current, positions);
                //similar paths
                if (similar >= 95)
                {
                    return r.Id;
                }
            }
            //no match
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Retrive circuit name
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        private string retriveCircuitName(Circuit record)
        {
            //records
            var records = this.Circuits;
            var count = records.Count + 1;

            //records
            foreach (var r in records)
            {
                if (record.Id == r.Id)
                {
                    return r.Name;
                }
            }

            //no match
            if (record.FastestLap.TotalSeconds < 60)
            {
                return "S" + count;
            }
            if (record.FastestLap.TotalSeconds < 90)
            {
                return "M" + count;
            }
            if (record.FastestLap.TotalSeconds < 120)
            {
                return "L" + count;
            }
            return "XL" + count;
        }

        /// <summary>
        /// Get positions
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static List<GeoPosition> getPositions(Circuit r)
        {
            var convertor = new GeoCoordinateConverter();
            return r.States.Select<State, GeoPosition>((e) =>
            {
                var pos = e.Position.Split(',');
                var Latitude = Double.Parse(pos[0], CultureInfo.InvariantCulture);
                var Longitude = Double.Parse(pos[1], CultureInfo.InvariantCulture);
                return new GeoPosition(Latitude, Longitude, 0);
            }).ToList();
        }

        /// <summary>
        /// Open stream for read
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private Stream openStreamForRead(String name)
        {
            var fileExists = Storage.FileExists(name);
            if (fileExists)
            {
                //stream
                return Storage.OpenFile(name, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            }
            return null;
        }

        /// <summary>
        /// Open stream for read
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private Stream openStreamForWrite(String name)
        {
            Stream stream;
            if (Storage.FileExists(name))
            {
                stream = Storage.OpenFile(name, FileMode.Truncate, FileAccess.ReadWrite, FileShare.Read);
            }
            else
            {
                stream = Storage.OpenFile(name, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            }
            return stream;
        }

        /// <summary>
        /// Create record
        /// </summary>
        private void createRecord()
        {
            Circuit circuit = this.currentRecord;
            GeoCoordinateCollection path = this.Path;
            //state
            var state = new State();
            state.Position = Position.ToString();
            state.Speed = Speed;
            //add
            circuit.States.Add(state);
            //path
            path.Add(Position);
            //detect if same path as previous
            detectSamePath();
            //detect circle
            detectCircle();
        }

        /// <summary>
        /// Same path as previous
        /// </summary>
        private void detectSamePath()
        {
            if (this.geoCircle != null)
            {
                var nearest = Geo.GetNearest(new GeoPosition(Position.Latitude, Position.Longitude, 0), this.geoCircle.Positions, out this.from, this.from);
                if (nearest > 0.1)
                {
                    StopRecording(StopType.WrongLap);
                }
            }
        }

        /// <summary>
        /// Detect circle
        /// </summary>
        private void detectCircle()
        {
            Circuit circuit = this.currentRecord;
            GeoCoordinateCollection path = this.Path;
            //circle detection
            Geo current = new Geo(new GeoPosition(Position.Latitude, Position.Longitude, Position.HorizontalAccuracy));
            GeoPosition first = new GeoPosition(path[0].Latitude, path[0].Longitude, path[0].HorizontalAccuracy);
            //distance to start
            var distance = current.distanceTo(first);
            //moves away from the start
            returning = returning || distance > 0.1;
            //check lap
            if (returning && distance <= 0.02)
            {
                //create lap
                createLap();
                //reset
                returning = false;
            }
        }

        /// <summary>
        /// Create lap
        /// </summary>
        public void createLap()
        {
            Circuit circuit = this.currentRecord;
            //no record found
            if (this.currentRecord == null)
            {
                return;
            }

            //get time elapsed
            TimeSpan lap = DateTime.Now.Subtract(this.lapTime);
            //add lap
            circuit.Laps.Add(lap.TotalMilliseconds);
            //add laps
            this.Laps.Insert(0, lap);
            //update lap time
            this.lapTime = DateTime.Now;
            //from
            this.from = 0;
            //null
            if (this.geoCircle == null)
            {
                //resolve circle
                this.geoCircle = Geo.GetCircle(Data.getPositions(circuit));
            }
        }

        /// <summary>
        /// Trigger change
        /// </summary>
        private void triggerChange()
        {
            changed = true;
        }

        /// <summary>
        /// Refresh
        /// </summary>
        private void refresh()
        {
            RaisePropertyChanged("SpeedInfo");
        }

        #endregion
    }
}
