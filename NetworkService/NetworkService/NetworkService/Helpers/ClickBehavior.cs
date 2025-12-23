using System.Windows;
using System.Windows.Input;

namespace NetworkService.Helpers
{

    public static class ClickBehavior
    {
        public static readonly DependencyProperty ClickCommandProperty =
            DependencyProperty.RegisterAttached(
                "ClickCommand",
                typeof(ICommand),
                typeof(ClickBehavior),
                new PropertyMetadata(null, OnChanged));

        public static void SetClickCommand(DependencyObject d, ICommand value) => d.SetValue(ClickCommandProperty, value);
        public static ICommand GetClickCommand(DependencyObject d) => (ICommand)d.GetValue(ClickCommandProperty);

        public static readonly DependencyProperty ClickCommandParameterProperty =
            DependencyProperty.RegisterAttached(
                "ClickCommandParameter",
                typeof(object),
                typeof(ClickBehavior),
                new PropertyMetadata(null));

        public static void SetClickCommandParameter(DependencyObject d, object value) => d.SetValue(ClickCommandParameterProperty, value);
        public static object GetClickCommandParameter(DependencyObject d) => d.GetValue(ClickCommandParameterProperty);

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement el)
            {
                el.MouseLeftButtonUp -= El_MouseLeftButtonUp;
                if (e.NewValue != null)
                    el.MouseLeftButtonUp += El_MouseLeftButtonUp;
            }
        }

        private static void El_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var el = sender as UIElement;
            var cmd = GetClickCommand(el);
            if (cmd == null) return;
            var param = GetClickCommandParameter(el);
            if (cmd.CanExecute(param)) cmd.Execute(param);
        }
    }
}
