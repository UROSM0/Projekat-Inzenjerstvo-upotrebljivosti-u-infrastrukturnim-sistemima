using System.Windows;
using System.Windows.Input;
using NetworkService.Model;
using System;

namespace NetworkService.Helpers
{
    public static class DragAndDropBehavior
    {

        public static readonly DependencyProperty IsDragSourceProperty =
            DependencyProperty.RegisterAttached(
                "IsDragSource",
                typeof(bool),
                typeof(DragAndDropBehavior),
                new PropertyMetadata(false, OnIsDragSourceChanged));

        public static void SetIsDragSource(DependencyObject d, bool value) => d.SetValue(IsDragSourceProperty, value);
        public static bool GetIsDragSource(DependencyObject d) => (bool)d.GetValue(IsDragSourceProperty);

        public static readonly DependencyProperty DragDataProperty =
            DependencyProperty.RegisterAttached(
                "DragData",
                typeof(object),
                typeof(DragAndDropBehavior),
                new PropertyMetadata(null));

        public static void SetDragData(DependencyObject d, object value) => d.SetValue(DragDataProperty, value);
        public static object GetDragData(DependencyObject d) => d.GetValue(DragDataProperty);

        private static readonly DependencyProperty StartPointProperty =
            DependencyProperty.RegisterAttached("StartPoint", typeof(Point?), typeof(DragAndDropBehavior), new PropertyMetadata(null));

        private static void OnIsDragSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var el = d as UIElement;
            if (el == null) return;

            if ((bool)e.NewValue)
            {
                el.PreviewMouseLeftButtonDown += DragSource_PreviewMouseLeftButtonDown;
                el.PreviewMouseMove += DragSource_PreviewMouseMove;
                el.PreviewMouseLeftButtonUp += DragSource_PreviewMouseLeftButtonUp;
            }
            else
            {
                el.PreviewMouseLeftButtonDown -= DragSource_PreviewMouseLeftButtonDown;
                el.PreviewMouseMove -= DragSource_PreviewMouseMove;
                el.PreviewMouseLeftButtonUp -= DragSource_PreviewMouseLeftButtonUp;
            }
        }

        private static void DragSource_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var el = sender as UIElement;
            if (el == null) return;
            el.SetValue(StartPointProperty, e.GetPosition(el));
        }

        private static void DragSource_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var el = sender as UIElement;
            if (el == null) return;
            el.ClearValue(StartPointProperty);
        }

        private static void DragSource_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var el = sender as UIElement;
            if (el == null) return;

            var spObj = el.GetValue(StartPointProperty);
            if (!(spObj is Point sp)) return;

            var pos = e.GetPosition(el);
            var dx = pos.X - sp.X;
            var dy = pos.Y - sp.Y;
            if ((dx * dx + dy * dy) < 16) return; 

            var data = GetDragData(el);
            if (data == null) return;

            var dobj = new DataObject();

            dobj.SetData("ReactorTemp", data);
            dobj.SetData(typeof(ReactorTemp), data);
            dobj.SetData(typeof(object), data);
            dobj.SetData(DataFormats.Serializable, data);

            try
            {
                DragDrop.DoDragDrop(el, dobj, DragDropEffects.Move);
            }
            catch (Exception ex)
            {
 
                System.Diagnostics.Debug.WriteLine($"Drag failed: {ex.Message}");
            }

            el.ClearValue(StartPointProperty);
            e.Handled = true;
        }


        public static readonly DependencyProperty IsDropTargetProperty =
            DependencyProperty.RegisterAttached(
                "IsDropTarget",
                typeof(bool),
                typeof(DragAndDropBehavior),
                new PropertyMetadata(false, OnIsDropTargetChanged));

        public static void SetIsDropTarget(DependencyObject d, bool value) => d.SetValue(IsDropTargetProperty, value);
        public static bool GetIsDropTarget(DependencyObject d) => (bool)d.GetValue(IsDropTargetProperty);

        public static readonly DependencyProperty DropCommandProperty =
            DependencyProperty.RegisterAttached(
                "DropCommand",
                typeof(ICommand),
                typeof(DragAndDropBehavior),
                new PropertyMetadata(null));

        public static void SetDropCommand(DependencyObject d, ICommand value) => d.SetValue(DropCommandProperty, value);
        public static ICommand GetDropCommand(DependencyObject d) => (ICommand)d.GetValue(DropCommandProperty);

        public static readonly DependencyProperty DropCommandParamProperty =
            DependencyProperty.RegisterAttached(
                "DropCommandParam",
                typeof(object),
                typeof(DragAndDropBehavior),
                new PropertyMetadata(null));

        public static void SetDropCommandParam(DependencyObject d, object value) => d.SetValue(DropCommandParamProperty, value);
        public static object GetDropCommandParam(DependencyObject d) => d.GetValue(DropCommandParamProperty);

        private static void OnIsDropTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var el = d as UIElement;
            if (el == null) return;

            if ((bool)e.NewValue)
            {
                el.AllowDrop = true;
                el.DragEnter += DropTarget_DragEnter;
                el.DragOver += DropTarget_DragOver;
                el.Drop += DropTarget_Drop;
                el.DragLeave += DropTarget_DragLeave;
            }
            else
            {
                el.AllowDrop = false;
                el.DragEnter -= DropTarget_DragEnter;
                el.DragOver -= DropTarget_DragOver;
                el.Drop -= DropTarget_Drop;
                el.DragLeave -= DropTarget_DragLeave;
            }
        }

        private static void DropTarget_DragEnter(object sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("DragEnter triggered");
            DropTarget_DragOver(sender, e);
        }

        private static void DropTarget_DragLeave(object sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("DragLeave triggered");
        }

        private static void DropTarget_DragOver(object sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("DragOver triggered");


            if (e.Data.GetDataPresent(typeof(ReactorTemp)) ||
                e.Data.GetDataPresent("ReactorTemp") ||
                e.Data.GetDataPresent(typeof(object)) ||
                e.Data.GetDataPresent(DataFormats.Serializable))
            {
                e.Effects = DragDropEffects.Move;
                System.Diagnostics.Debug.WriteLine("Drop allowed");
            }
            else
            {
                e.Effects = DragDropEffects.None;
                System.Diagnostics.Debug.WriteLine("Drop not allowed");


                foreach (string format in e.Data.GetFormats())
                {
                    System.Diagnostics.Debug.WriteLine($"Available format: {format}");
                }
            }

            e.Handled = true;
        }

        private static void DropTarget_Drop(object sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Drop triggered");

            var el = sender as UIElement;
            if (el == null) return;

            var cmd = GetDropCommand(el);
            if (cmd == null)
            {
                System.Diagnostics.Debug.WriteLine("No drop command found");
                return;
            }

            object dragged = null;


            if (e.Data.GetDataPresent(typeof(ReactorTemp)))
                dragged = e.Data.GetData(typeof(ReactorTemp));
            else if (e.Data.GetDataPresent("ReactorTemp"))
                dragged = e.Data.GetData("ReactorTemp");
            else if (e.Data.GetDataPresent(typeof(object)))
                dragged = e.Data.GetData(typeof(object));
            else if (e.Data.GetDataPresent(DataFormats.Serializable))
                dragged = e.Data.GetData(DataFormats.Serializable);

            if (dragged == null)
            {
                System.Diagnostics.Debug.WriteLine("No dragged data found");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Dragged data: {dragged.GetType().Name}");

            var param = GetDropCommandParam(el);
            var payload = new DropPayload { Data = dragged, Param = param };

            if (cmd.CanExecute(payload))
            {
                cmd.Execute(payload);
                e.Handled = true;
                System.Diagnostics.Debug.WriteLine("Drop command executed successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Drop command cannot execute");
            }
        }
    }

    public class DropPayload
    {
        public object Data { get; set; }
        public object Param { get; set; }
    }
}