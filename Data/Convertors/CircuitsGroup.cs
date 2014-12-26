using CoPilot.Core.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CoPilot.Speedway.Data.Convertors
{
    public class CircuitGroup
    {
        public List<State> States { get; set; }
        public List<Double> Laps { get; set; }
        public Circuit Circuit { get; set; }
        public String Name { get; set; }

        public TimeSpan FastestLap
        {
            get
            {
                TimeSpan best = TimeSpan.MaxValue;
                foreach (var lap in Laps)
                {
                    TimeSpan lapTime = new TimeSpan(0, 0, 0, 0, (int)lap);
                    if (lapTime < best)
                    {
                        best = lapTime;
                    }
                }
                return best;
            }
        }
    }

    public class CircuitsGroup : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ObservableCollection<Circuit> circuits = (ObservableCollection<Circuit>)value;
            Dictionary<string, CircuitGroup> groups = new Dictionary<string, CircuitGroup>();

            for (var i = 0; i < circuits.Count; i++)
            {
                CircuitGroup group = null;
                var circuit = circuits[i];
                var exists = groups.ContainsKey(circuit.Id);
                //no exists
                if (!exists)
                {
                    group = new CircuitGroup();
                    //create
                    group.Laps = new List<double>();
                    group.States = new List<State>();
                    //fill
                    group.Name = circuit.Name;
                    group.States.AddRange(circuit.States);
                    group.Circuit = circuit;
                    //add
                    groups.Add(circuit.Id, group);
                }
                //get
                group = groups[circuit.Id];
                //find fastest circle
                if (group.Circuit.Laps.Min() > circuit.Laps.Min())
                {
                    group.Circuit = circuit;
                }
                //add laps
                group.Laps.AddRange(circuit.Laps);
            }

            //return
            return groups.Values.ToList();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
