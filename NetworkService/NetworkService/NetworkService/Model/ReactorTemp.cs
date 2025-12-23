using System.Collections.ObjectModel;
using System.ComponentModel;
using NetworkService.Helpers;

namespace NetworkService.Model
{
    public class ReactorTemp : ValidationBase
    {
        private int? _id;
        private string _name = "";
        private SensorType _type;
        private double? _lastValue;
        public static ObservableCollection<int> UsedIds { get; set; } = new ObservableCollection<int>();


        public int? Id
        {
            get => _id;
            set { if (_id != value) { _id = value; OnPropertyChanged(nameof(Id)); } }
        }

        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); } }
        }

        public SensorType Type
        {
            get => _type;
            set { if (_type != value) { _type = value; OnPropertyChanged(nameof(Type)); } }
        }

        public double? LastValue
        {
            get => _lastValue;
            set { if (_lastValue != value) { _lastValue = value; OnPropertyChanged(nameof(LastValue)); } }
        }

        protected override void ValidateSelf()
        {
            if (Id < 0)
                ValidationErrors[nameof(Id)] = "ID must be a non-negative integer.";
            else if (Id==null)
                ValidationErrors[nameof(Id)] = "ID is required.";

            if (string.IsNullOrWhiteSpace(Name)) ValidationErrors[nameof(Name)] = "Name is required.";
            if (Type == null) ValidationErrors[nameof(Type)] = "Select a sensor type.";
            
        }
    }
}
