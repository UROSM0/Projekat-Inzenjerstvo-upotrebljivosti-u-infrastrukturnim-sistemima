
using System.Windows;
using System.Windows.Media;
using NetworkService.Helpers; 

namespace NetworkService.ViewModel
{
    public class ConnectionVM : BindableBase
    {
        public ConnectionVM() { }

        
        public ConnectionVM(SlotVM a, SlotVM b)
        {
            From = a;
            To = b;
        }

      
        private SlotVM _from;
        public SlotVM From
        {
            get => _from;
            set { SetProperty(ref _from, value); OnEndpointsChanged(); }
        }

        private SlotVM _to;
        public SlotVM To
        {
            get => _to;
            set { SetProperty(ref _to, value); OnEndpointsChanged(); }
        }

        public SlotVM A
        {
            get => From;
            set => From = value;
        }

        public SlotVM B
        {
            get => To;
            set => To = value;
        }

        private Point _fromPoint;
        public Point FromPoint
        {
            get => _fromPoint;
            set
            {
                if (SetPoint(ref _fromPoint, value, nameof(FromPoint)))
                    OnPropertyChanged(nameof(LineGeometry));
            }
        }

        private Point _toPoint;
        public Point ToPoint
        {
            get => _toPoint;
            set
            {
                if (SetPoint(ref _toPoint, value, nameof(ToPoint)))
                    OnPropertyChanged(nameof(LineGeometry));
            }
        }

        
        public Geometry LineGeometry => new LineGeometry(FromPoint, ToPoint);

        
        public void UpdateEndpoints(FrameworkElement overlayRoot)
        {
            if (From?.Element == null || To?.Element == null || overlayRoot == null)
                return;

            var p1 = From.Element.TranslatePoint(
                new Point(From.Element.ActualWidth / 2.0, From.Element.ActualHeight / 2.0),
                overlayRoot);

            var p2 = To.Element.TranslatePoint(
                new Point(To.Element.ActualWidth / 2.0, To.Element.ActualHeight / 2.0),
                overlayRoot);

            FromPoint = p1;
            ToPoint = p2;
        }

        private void OnEndpointsChanged()
        {
            OnPropertyChanged(nameof(LineGeometry));
        }

        private bool SetPoint(ref Point field, Point val, string propertyName)
        {
            if (field.X == val.X && field.Y == val.Y) return false;
            field = val;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
