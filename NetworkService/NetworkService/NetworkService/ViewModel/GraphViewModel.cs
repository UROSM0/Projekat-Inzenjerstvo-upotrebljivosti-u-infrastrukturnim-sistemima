
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using NetworkService.Helpers;
using NetworkService.Model;

namespace NetworkService.ViewModel
{
    public class GraphViewModel : BindableBase, IDisposable
    {
        public ObservableCollection<ReactorTemp> Entities { get; }
        public string LogPath { get; }

        private ReactorTemp _selectedEntity;
        public ReactorTemp SelectedEntity
        {
            get => _selectedEntity;
            set
            {
                if (SetAndNotify(ref _selectedEntity, value))
                {
                    ReloadLastFive();
                }
            }
        }

        
        public ObservableCollection<MeasurementPoint> Points { get; } = new ObservableCollection<MeasurementPoint>();

        
        private readonly DispatcherTimer _timer;
        private long _lastLength = 0;
        private string _carry = string.Empty;

        public GraphViewModel(ObservableCollection<ReactorTemp> entities, string logPath)
        {
            Entities = entities;
            LogPath = logPath;

            
            SelectedEntity = Entities.FirstOrDefault();

            
            _timer = new DispatcherTimer(DispatcherPriority.Background);
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += (s, e) => TailLog();
            _timer.Start();
        }

        private bool SetAndNotify<T>(ref T field, T val, string propName = null)
        {
            if (object.Equals(field, val)) return false;
            field = val;
            OnPropertyChanged(propName ?? nameof(SelectedEntity));
            return true;
        }

        private void ReloadLastFive()
        {
            Points.Clear();
            if (SelectedEntity == null || !SelectedEntity.Id.HasValue) return;
            if (!File.Exists(LogPath)) return;

            
            var lines = File.ReadLines(LogPath);
            var filtered = lines
                .Select(ParseLine)
                .Where(m => m != null && m.EntityId == SelectedEntity.Id.Value)
                .Select(m => m); 

            var last5 = filtered
                .Reverse()      
                .Take(5)
                .Reverse()
                .ToList();

            foreach (var m in last5)
            {
                if (m == null) continue;
                Points.Add(new MeasurementPoint { Value = m.Value, Timestamp = m.Timestamp });
            }
        }

        private void TailLog()
        {
            try
            {
                if (!File.Exists(LogPath)) return;

                using (var fs = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (_lastLength > fs.Length)
                    {
                       
                        _lastLength = 0;
                        _carry = string.Empty;
                    }

                    fs.Seek(_lastLength, SeekOrigin.Begin);
                    using (var sr = new StreamReader(fs))
                    {
                        var text = sr.ReadToEnd();
                        _lastLength = fs.Position;

                        if (string.IsNullOrEmpty(text)) return;

                        
                        text = _carry + text;
                        var parts = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                        
                        _carry = (text.EndsWith("\n") || text.EndsWith("\r\n")) ? string.Empty : (parts.LastOrDefault() ?? string.Empty);
                        int limit = _carry.Length == 0 ? parts.Length : Math.Max(0, parts.Length - 1);

                        for (int i = 0; i < limit; i++)
                        {
                            var parsed = ParseLine(parts[i]);
                            if (parsed == null) continue;

                            if (SelectedEntity != null && SelectedEntity.Id.HasValue && parsed.EntityId == SelectedEntity.Id.Value)
                            {
                                Points.Add(new MeasurementPoint { Value = parsed.Value, Timestamp = parsed.Timestamp });
                                
                                while (Points.Count > 5) Points.RemoveAt(0);
                            }
                        }
                    }
                }
            }
            catch
            {
                
            }
        }

        private LogEntry ParseLine(string line)
        {
            
            if (string.IsNullOrWhiteSpace(line)) return null;
            var parts = line.Split(';');
            if (parts.Length < 4) return null;

            DateTime ts;
            if (!DateTime.TryParse(parts[0], null, DateTimeStyles.RoundtripKind, out ts)) return null;

            int idIdx = parts[1].IndexOf('=');
            int valIdx = parts[2].IndexOf('=');
            if (idIdx < 0 || valIdx < 0) return null;

            int id;
            if (!int.TryParse(parts[1].Substring(idIdx + 1), out id)) return null;

            double val;
            if (!double.TryParse(parts[2].Substring(valIdx + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out val)) return null;

            return new LogEntry { Timestamp = ts, EntityId = id, Value = val };
        }

        public void Dispose()
        {
            _timer.Stop();
        }
    }

    public class MeasurementPoint : BindableBase
    {
        private double _value;
        public double Value
        {
            get { return _value; }
            set { SetProperty(ref _value, value); }
        }

        private DateTime _timestamp;
        public DateTime Timestamp
        {
            get { return _timestamp; }
            set { SetProperty(ref _timestamp, value); }
        }
    }

    internal class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public int EntityId { get; set; }
        public double Value { get; set; }
    }
}
