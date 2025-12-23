using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetworkService.Model;

namespace NetworkService.Helpers
{
    public static class EntitiesRepositoryText
    {
        private static readonly string DataDir = Path.Combine("Data");
        private static readonly string FilePath = Path.Combine(DataDir, "entities.txt");

        public static void Save(ObservableCollection<ReactorTemp> entities)
        {
            Directory.CreateDirectory(DataDir);

            var lines = entities.Select(e =>
                $"{e.Id};{e.Name};{e.Type?.Name};{e.Type?.ImagePath};{(e.LastValue.HasValue ? e.LastValue.Value.ToString("F2", CultureInfo.InvariantCulture) : "")}"
            );

            File.WriteAllLines(FilePath, lines);
        }

        public static ObservableCollection<ReactorTemp> Load(ObservableCollection<SensorType> knownTypes)
        {
            var result = new ObservableCollection<ReactorTemp>();
            if (!File.Exists(FilePath)) return result;

            foreach (var line in File.ReadAllLines(FilePath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = line.Split(';');
                if (cols.Length < 5) continue;

                if (!int.TryParse(cols[0], out int id)) continue;

                var name = cols[1];
                var typeName = cols[2];
                var typeImagePath = cols[3];
                double? lastVal = null;

                if (!string.IsNullOrWhiteSpace(cols[4]) &&
                    double.TryParse(cols[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                {
                    lastVal = d;
                }

                var type = knownTypes.FirstOrDefault(t => t.Name == typeName);
                if (type == null && !string.IsNullOrEmpty(typeName))
                {
                    type = new SensorType { Name = typeName, ImagePath = typeImagePath };
                    knownTypes.Add(type);
                }

                result.Add(new ReactorTemp
                {
                    Id = id,
                    Name = name,
                    Type = type,
                    LastValue = lastVal
                });
            }

            return result;
        }
    }
}
