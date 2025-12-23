using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NetworkService.ViewModel;


namespace NetworkService.Helpers
{
    public static class SlotElementBinder
    {
        public static readonly DependencyProperty BindProperty =
            DependencyProperty.RegisterAttached(
                "Bind",
                typeof(bool),
                typeof(SlotElementBinder),
                new PropertyMetadata(false, OnBindChanged));

        public static void SetBind(DependencyObject d, bool value) => d.SetValue(BindProperty, value);
        public static bool GetBind(DependencyObject d) => (bool)d.GetValue(BindProperty);

        private static void OnBindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement fe)
            {
                if ((bool)e.NewValue)
                {
                    fe.Loaded += Fe_Loaded;
                    fe.Unloaded += Fe_Unloaded;
                }
                else
                {
                    fe.Loaded -= Fe_Loaded;
                    fe.Unloaded -= Fe_Unloaded;
                }
            }
        }

        private static void Fe_Loaded(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            if (fe?.DataContext is SlotVM slot)
            {
                slot.Element = fe;
            }
        }

        private static void Fe_Unloaded(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            if (fe?.DataContext is SlotVM slot)
            {
                if (ReferenceEquals(slot.Element, fe))
                    slot.Element = null;
            }
        }
    }
}
