using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using NetworkService.Helpers;
using NetworkService.Model;
using System.Collections.Specialized;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using NetworkService.Services;

namespace NetworkService.ViewModel
{
    public class MainWindowViewModel:BindableBase
    {
        public ObservableCollection<ReactorTemp> Entities { get; } = new ObservableCollection<ReactorTemp>();
        public ObservableCollection<SensorType> SensorTypes { get; } = new ObservableCollection<SensorType>();
        public EntitiesViewModel EntitiesVM { get; }

        private BindableBase _currentViewModel;
        public BindableBase CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public MyICommand<string> NavCommand { get; }

        public DisplayViewModel DisplayVM { get; }
        public GraphViewModel GraphVM { get; }

        public MyICommand OpenTerminalCommand { get; }
        private readonly IWindowService _windows = new WindowService();

        private int count => Entities?.Count ?? 0;
        private readonly string logPath = Path.Combine("Logs", "measurements.txt");
        private static readonly string SimulatorExePath =
    Path.GetFullPath(@"..\..\..\..\..\MeteringSimulator\MeteringSimulator\bin\Debug\MeteringSimulator.exe");

        public MainWindowViewModel()
        {
            NavCommand = new MyICommand<string>(OnNav);

            var rtd = new SensorType { Name = "RTD", ImagePath = "/Resources/Images/rtd.png" };
            var thm = new SensorType { Name = "TermoSprega", ImagePath = "/Resources/Images/thermocouple.png" };
            SensorTypes.Add(rtd);
            SensorTypes.Add(thm);


            var loaded = EntitiesRepositoryText.Load(SensorTypes);
            if (loaded.Count > 0)
            {
                foreach (var e in loaded) Entities.Add(e);
            }
            else
            {
                Entities.Add(new ReactorTemp { Id = 0, Name = "R-01", Type = rtd });
                Entities.Add(new ReactorTemp { Id = 1, Name = "R-02", Type = thm });
                Entities.Add(new ReactorTemp { Id = 2, Name = "R-03", Type = rtd });
                EntitiesRepositoryText.Save(Entities);
            }

            try { LoadLastValuesFromLog(Entities); }
            catch (Exception ex) { Debug.WriteLine("LoadLastValuesFromLog failed: " + ex); }
            

            Entities.CollectionChanged += (_, e) =>
            {

                EntitiesRepositoryText.Save(Entities);


                if (e.Action == NotifyCollectionChangedAction.Add ||
                    e.Action == NotifyCollectionChangedAction.Remove ||
                    e.Action == NotifyCollectionChangedAction.Reset)
                {
                    RestartSimulator();
                }
            };

            EntitiesVM = new EntitiesViewModel(Entities, SensorTypes);
            DisplayVM = new DisplayViewModel(Entities, SensorTypes); 
            GraphVM = new GraphViewModel(Entities, logPath);       


            CurrentViewModel = EntitiesVM;
            OpenTerminalCommand = new MyICommand(() => _windows.ShowTerminal(this));
            Directory.CreateDirectory("Logs");
            createListener();
        }

        private void OnNav(string destination)
        {
            switch (destination)
            {
                case "entities": CurrentViewModel = EntitiesVM; break;
                case "display": CurrentViewModel = DisplayVM; break;
                case "graph": CurrentViewModel = GraphVM; break;
            }
        }

        private void createListener()
        {
            var tcp = new TcpListener(IPAddress.Any, 25675);
            tcp.Start();

            var listeningThread = new Thread(() =>
            {
                while (true)
                {
                    var tcpClient = tcp.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(param =>
                    {
                        try
                        {
                            using (tcpClient)
                            using (NetworkStream stream = tcpClient.GetStream())
                            {
                                byte[] bytes = new byte[1024];
                                int i = stream.Read(bytes, 0, bytes.Length);
                                string incomming = Encoding.ASCII.GetString(bytes, 0, i);

                                if (incomming.Equals("Need object count"))
                                {
                                    byte[] data = Encoding.ASCII.GetBytes(count.ToString());
                                    stream.Write(data, 0, data.Length);
                                }
                                else
                                {
                                    var parts = incomming.Split(':');
                                    if (parts.Length == 2)
                                    {
                                        var left = parts[0]?.Trim();   // "Entitet_1"
                                        var right = parts[1]?.Trim();  // "272.5"
                                        if (!string.IsNullOrEmpty(left) && !string.IsNullOrEmpty(right) && left.StartsWith("Entitet_", System.StringComparison.OrdinalIgnoreCase))
                                        {
                                            var idxStr = left.Substring("Entitet_".Length);
                                            if (int.TryParse(idxStr, out int idx) &&
                                                double.TryParse(right, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                                            {
                                                if (idx >= 0 && idx < Entities.Count)
                                                {
                                                    Application.Current?.Dispatcher?.Invoke(() =>
                                                    {
                                                        Entities[idx].LastValue = value;
                                                    });
                                                    AppendLog(Entities[idx].Id.GetValueOrDefault(-1), value);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch {  }
                    }, null);
                }
            });

            listeningThread.IsBackground = true;
            listeningThread.Start();
        }

        private void AppendLog(int entityId, double value)
        {
            bool valid = (value >= 250.0 && value <= 350.0);
            string line = $"{System.DateTime.Now:O};EntityId={entityId};Value={value.ToString("F2", CultureInfo.InvariantCulture)};Valid={valid}";
            File.AppendAllText(logPath, line + System.Environment.NewLine);
        }

        private void RestartSimulator()
        {
            var fullPath = Path.GetFullPath(SimulatorExePath);

            System.Diagnostics.Debug.WriteLine("Simulator path: " + fullPath);
            System.Diagnostics.Debug.WriteLine("Exists: " + File.Exists(fullPath));
            try
            {

                foreach (var p in Process.GetProcessesByName("MeteringSimulator"))
                {
                    try { p.Kill(); p.WaitForExit(2000); } catch { /* ignoriši */ }
                }

                Thread.Sleep(200);
                if (File.Exists(SimulatorExePath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = SimulatorExePath,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(SimulatorExePath)
                    };
                    Process.Start(psi);
                }
                else
                {

                    Debug.WriteLine("MeteringSimulator.exe not found at: " + SimulatorExePath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to restart simulator: " + ex);
            }
        }

        private void LoadLastValuesFromLog(ObservableCollection<ReactorTemp> entities)
        {
            var logFile = Path.Combine("Logs", "measurements.txt");
            if (!File.Exists(logFile)) return;


            var latest = new Dictionary<int, (DateTime ts, double val)>();

            foreach (var line in File.ReadLines(logFile))
            {

                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(';');
                if (parts.Length < 4) continue;

                if (!DateTime.TryParse(parts[0], out var ts)) continue;

                int idIdx = parts[1].IndexOf('=');
                int valIdx = parts[2].IndexOf('=');
                if (idIdx < 0 || valIdx < 0) continue;

                if (!int.TryParse(parts[1].Substring(idIdx + 1), out var id)) continue;
                if (!double.TryParse(parts[2].Substring(valIdx + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var val)) continue;

                if (!latest.TryGetValue(id, out var cur) || ts > cur.ts)
                    latest[id] = (ts, val);
            }


            foreach (var e in entities)
            {
                if (e.Id.HasValue && latest.TryGetValue(e.Id.Value, out var lv))
                {
                    e.LastValue = lv.val;

                }
            }
        }

    }
}
