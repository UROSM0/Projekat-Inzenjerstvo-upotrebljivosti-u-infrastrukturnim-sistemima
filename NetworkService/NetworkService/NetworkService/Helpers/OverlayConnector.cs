using System.Collections.ObjectModel;
using System.Windows;
using NetworkService.ViewModel;

namespace NetworkService.Helpers
{
    public static class OverlayConnector
    {
        public static readonly DependencyProperty ConnectionsProperty =
            DependencyProperty.RegisterAttached(
                "Connections",
                typeof(ObservableCollection<ConnectionVM>),
                typeof(OverlayConnector),
                new PropertyMetadata(null, OnConnectionsChanged));

        public static void SetConnections(DependencyObject d, ObservableCollection<ConnectionVM> value)
        {
            if (d != null) d.SetValue(ConnectionsProperty, value);
        }

        public static ObservableCollection<ConnectionVM> GetConnections(DependencyObject d)
        {
            // >>> ključna linija – zaštita od null:
            if (d == null) return null;
            return (ObservableCollection<ConnectionVM>)d.GetValue(ConnectionsProperty);
        }

        private static void OnConnectionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var fe = d as FrameworkElement;
            if (fe == null) return;

            fe.LayoutUpdated -= Fe_LayoutUpdated;
            if (e.NewValue != null)
                fe.LayoutUpdated += Fe_LayoutUpdated;
        }

        private static void Fe_LayoutUpdated(object sender, System.EventArgs e)
        {
            var fe = sender as FrameworkElement;
            if (fe == null) return;

            var cons = GetConnections(fe);
            if (cons == null) return;

            foreach (var c in cons)
            {
                // ConnectionVM treba da bude robustan (da proveri da li su slotovi/elementi null)
                c.UpdateEndpoints(fe);
            }
        }
    }
}
