using Microsoft.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CR = CoPilot.Speedway.Controller;

namespace CoPilot.Speedway.Data
{
    public class DataContainer
    {
        public CR.Data DataController { get; set; }
        public CR.Ftp FtpController { get; set; }
        public CR.Bluetooth BluetoothController { get; set; }

        public Uri Uri { get; set; }
    }
}
