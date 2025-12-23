using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using NetworkService.ViewModel;

namespace NetworkService.Helpers
{
    public class ConnectionsAdorner : Adorner
    {
        public ObservableCollection<ConnectionVM> Connections
        {
            get => (ObservableCollection<ConnectionVM>)GetValue(ConnectionsProperty);
            set => SetValue(ConnectionsProperty, value);
        }

        public static readonly DependencyProperty ConnectionsProperty =
            DependencyProperty.Register(nameof(Connections), typeof(ObservableCollection<ConnectionVM>),
                typeof(ConnectionsAdorner),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnConnectionsChanged));

        private static void OnConnectionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ad = (ConnectionsAdorner)d;

            if (e.OldValue is INotifyCollectionChanged oldColl)
                oldColl.CollectionChanged -= ad.Connections_CollectionChanged;
            if (e.NewValue is INotifyCollectionChanged newColl)
                newColl.CollectionChanged += ad.Connections_CollectionChanged;

            ad.InvalidateVisual();
        }

        private void Connections_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            
            InvalidateVisual();
        }

        public ConnectionsAdorner(UIElement adorned) : base(adorned)
        {
            IsHitTestVisible = false; 

            if (adorned is FrameworkElement fe)
                fe.LayoutUpdated += (_, __) => InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            if (Connections == null) return;

            var feRoot = AdornedElement as FrameworkElement;
            if (feRoot == null) return;

           
            var pen = new Pen((Brush)feRoot.FindResource("Clr.Accent"), 2.0) { DashStyle = DashStyles.Dash };

            foreach (var c in Connections)
            {
                var from = c?.A?.Element;
                var to = c?.B?.Element;
                if (from == null || to == null) continue;

                
                var p1 = from.TranslatePoint(new Point(from.ActualWidth / 2.0, from.ActualHeight / 2.0), feRoot);
                var p2 = to.TranslatePoint(new Point(to.ActualWidth / 2.0, to.ActualHeight / 2.0), feRoot);

                dc.DrawLine(pen, p1, p2);
            }
        }
    }
}
