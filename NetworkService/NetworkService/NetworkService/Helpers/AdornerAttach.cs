using System.Windows;
using System.Windows.Documents;
using NetworkService.ViewModel;

namespace NetworkService.Helpers
{
    public static class AdornerAttach
    {
        public static readonly DependencyProperty ConnectionsProperty =
            DependencyProperty.RegisterAttached(
                "Connections",
                typeof(System.Collections.ObjectModel.ObservableCollection<ConnectionVM>),
                typeof(AdornerAttach),
                new PropertyMetadata(null, OnConnectionsChanged));

        public static void SetConnections(DependencyObject d, System.Collections.ObjectModel.ObservableCollection<ConnectionVM> value)
            => d.SetValue(ConnectionsProperty, value);

        public static System.Collections.ObjectModel.ObservableCollection<ConnectionVM> GetConnections(DependencyObject d)
            => (System.Collections.ObjectModel.ObservableCollection<ConnectionVM>)d.GetValue(ConnectionsProperty);

        private static void OnConnectionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement el)
            {
                var layer = AdornerLayer.GetAdornerLayer(el);
                if (layer == null) return;

               
                var existing = layer.GetAdorners(el);
                if (existing != null)
                {
                    foreach (var ad in existing)
                        if (ad is ConnectionsAdorner) layer.Remove(ad);
                }

                if (e.NewValue != null)
                {
                    var ad = new ConnectionsAdorner(el) { Connections = (System.Collections.ObjectModel.ObservableCollection<ConnectionVM>)e.NewValue };
                    layer.Add(ad);
                }
            }
        }
    }
}
