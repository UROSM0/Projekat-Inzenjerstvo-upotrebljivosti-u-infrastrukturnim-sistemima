
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace NetworkService.Controls
{
    public class BarChart : FrameworkElement
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(BarChart),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnItemsSourceChanged));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty LowProperty =
            DependencyProperty.Register(nameof(Low), typeof(double), typeof(BarChart),
                new FrameworkPropertyMetadata(250.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double Low
        {
            get => (double)GetValue(LowProperty);
            set => SetValue(LowProperty, value);
        }

        public static readonly DependencyProperty HighProperty =
            DependencyProperty.Register(nameof(High), typeof(double), typeof(BarChart),
                new FrameworkPropertyMetadata(350.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double High
        {
            get => (double)GetValue(HighProperty);
            set => SetValue(HighProperty, value);
        }

        public static readonly DependencyProperty ValidBrushProperty =
            DependencyProperty.Register(nameof(ValidBrush), typeof(Brush), typeof(BarChart),
                new FrameworkPropertyMetadata(Brushes.SeaGreen, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush ValidBrush
        {
            get => (Brush)GetValue(ValidBrushProperty);
            set => SetValue(ValidBrushProperty, value);
        }

        public static readonly DependencyProperty InvalidBrushProperty =
            DependencyProperty.Register(nameof(InvalidBrush), typeof(Brush), typeof(BarChart),
                new FrameworkPropertyMetadata(Brushes.IndianRed, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush InvalidBrush
        {
            get => (Brush)GetValue(InvalidBrushProperty);
            set => SetValue(InvalidBrushProperty, value);
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = (BarChart)d;

            if (e.OldValue is INotifyCollectionChanged oldObs)
                oldObs.CollectionChanged -= chart.Items_CollectionChanged;
            if (e.NewValue is INotifyCollectionChanged newObs)
                newObs.CollectionChanged += chart.Items_CollectionChanged;

            chart.InvalidateVisual();
        }

        private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            var rect = new Rect(new Point(0, 0), RenderSize);
            if (rect.Width < 10 || rect.Height < 10) return;

          
            var axisBrush = TryFindResource("Clr.Text") as Brush ?? Brushes.Black;
            var gridBrush = TryFindResource("Clr.Border") as Brush ?? Brushes.LightGray;
            var bandBrush = TryFindResource("Clr.RowAlt") as Brush ?? new SolidColorBrush(Color.FromArgb(24, 0, 0, 0));

           
            const double left = 64;
            const double right = 16;
            const double top = 28;
            const double bottom = 40;

            var plot = new Rect(left, top, rect.Width - left - right, rect.Height - top - bottom);
            if (plot.Width <= 0 || plot.Height <= 0) return;

           
            dc.DrawRectangle(FindResource("Clr.Surface") as Brush ?? Brushes.White, null, rect);


            var items = ItemsSource?.Cast<object>()?.ToList() ?? new System.Collections.Generic.List<object>();


            double[] vals = items
                .Select(it => SafeGetDouble(it, "Value"))
                .Where(v => !double.IsNaN(v))
                .ToArray();

            
            double minV = vals.Length > 0 ? vals.Min() : Low;
            double maxV = vals.Length > 0 ? vals.Max() : High;

            
            double yMin = Math.Min(Low, minV) - 10;
            double yMax = Math.Max(High, maxV) + 10;
            if (yMax <= yMin) { yMax = yMin + 1; }

            
            var penAxis = new Pen(axisBrush, 1.2);
            
            dc.DrawLine(penAxis, new Point(plot.Left, plot.Top), new Point(plot.Left, plot.Bottom));
            
            dc.DrawLine(penAxis, new Point(plot.Left, plot.Bottom), new Point(plot.Right, plot.Bottom));


            DrawYTick(dc, plot, yMin, yMin, yMax, axisBrush, gridBrush);
            DrawYTick(dc, plot, Low, yMin, yMax, axisBrush, gridBrush);
            DrawYTick(dc, plot, High, yMin, yMax, axisBrush, gridBrush);
            DrawYTick(dc, plot, yMax, yMin, yMax, axisBrush, gridBrush);


            double yLowPix = ValueToY(Low, plot, yMin, yMax);
            double yHighPix = ValueToY(High, plot, yMin, yMax);
            var band = new Rect(plot.Left, yHighPix, plot.Width, yLowPix - yHighPix);
            dc.DrawRectangle(bandBrush, null, band);

            
            int n = Math.Min(vals.Length, 5);
            if (n > 0)
            {
                double gap = 10;
                double barW = Math.Max(12, (plot.Width - (gap * (n + 1))) / n);

                var ts = items.Select(it => SafeGetDate(it, "Timestamp")).ToArray();

                for (int i = 0; i < n; i++)
                {
                    int idx = vals.Length - n + i;
                    double v = vals[idx];
                    bool isValid = (v >= Low && v <= High);

                    double x = plot.Left + gap + i * (barW + gap);
                    double y = ValueToY(v, plot, yMin, yMax);
                    var bar = new Rect(x, y, barW, plot.Bottom - y);

                    dc.DrawRectangle(isValid ? ValidBrush : InvalidBrush, null, bar);

                   
                    var tsLabel = ts.Length > idx && ts[idx] != DateTime.MinValue
                        ? ts[idx].ToString("HH:mm:ss")
                        : (i + 1).ToString(CultureInfo.InvariantCulture);

                    DrawText(dc, tsLabel, new Point(x + barW / 2, plot.Bottom + 4), axisBrush, 10, centered: true);
                }
            }

            
            DrawText(dc, "Value (°C)", new Point(8, plot.Top - 20), axisBrush, 11);
            DrawText(dc, "time →", new Point(plot.Right - 32, plot.Bottom + 20), axisBrush, 11);
        }

        private static double SafeGetDouble(object obj, string prop)
        {
            var pi = obj.GetType().GetProperty(prop);
            if (pi == null) return double.NaN;
            var v = pi.GetValue(obj);
            if (v is double d) return d;
            if (v == null) return double.NaN;
            return double.TryParse(v.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var dd) ? dd : double.NaN;
        }

        private static DateTime SafeGetDate(object obj, string prop)
        {
            var pi = obj.GetType().GetProperty(prop);
            if (pi == null) return DateTime.MinValue;
            var v = pi.GetValue(obj);
            if (v is DateTime dt) return dt;
            if (v == null) return DateTime.MinValue;
            return DateTime.TryParse(v.ToString(), out var d) ? d : DateTime.MinValue;
        }

        private static double ValueToY(double val, Rect plot, double yMin, double yMax)
        {
            double t = (val - yMin) / (yMax - yMin);
            t = Clamp(t, 0, 1);
            return plot.Bottom - t * plot.Height;
        }

        private static void DrawYTick(DrawingContext dc, Rect plot, double value, double yMin, double yMax, Brush axisBrush, Brush gridBrush)
        {
            double y = ValueToY(value, plot, yMin, yMax);

            
            var penGrid = new Pen(gridBrush, 0.5) { DashStyle = DashStyles.Dash };
            dc.DrawLine(penGrid, new Point(plot.Left, y), new Point(plot.Right, y));

            
            var ft = new FormattedText(
                value.ToString("F0", CultureInfo.InvariantCulture),
#if NETCOREAPP
        CultureInfo.CurrentUICulture,
#else
                System.Globalization.CultureInfo.CurrentCulture,
#endif
                FlowDirection.LeftToRight,
                new Typeface("Consolas"),
                10,
                axisBrush,
                VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip);

            dc.DrawText(ft, new Point(plot.Left - ft.Width - 4, y - ft.Height / 2));
        }


        private static void DrawText(DrawingContext dc, string text, Point p, Brush brush, double size, bool centered = false)
        {
            var ft = new FormattedText(
                text,
#if NETCOREAPP
                CultureInfo.CurrentUICulture,
#else
                System.Globalization.CultureInfo.CurrentCulture,
#endif
                FlowDirection.LeftToRight,
                new Typeface("Consolas"),
                size,
                brush,
                VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip);

            if (centered) p.X -= ft.Width / 2.0;
            dc.DrawText(ft, p);
        }

        private static double Clamp(double val, double min, double max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }
    }
}
