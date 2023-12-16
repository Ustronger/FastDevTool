using HandyControl.Interactivity;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace FastDevTool.View.Behaviors {
    internal class DragFileInBehavior : Microsoft.Xaml.Behaviors.Behavior<FrameworkElement>
    {


        public string FilePath {
            get { return (string)GetValue(FilePathProperty); }
            set { SetValue(FilePathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Path.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilePathProperty =
            DependencyProperty.Register("FilePath", typeof(string), typeof(DragFileInBehavior), new PropertyMetadata(string.Empty));


        protected override void OnAttached() {
            AssociatedObject.PreviewDragEnter += DragEnter;
            AssociatedObject.PreviewDragOver += DragEnter;
            AssociatedObject.PreviewDrop += Drop;
        }

        private void DragEnter(object sender, DragEventArgs e) {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
        private void Drop(object sender, DragEventArgs e) {
            FrameworkElement? framework = sender as FrameworkElement;
            FilePath = (e.Data.GetData(DataFormats.FileDrop) as Array)!.GetValue(0)!.ToString()!;
            BindingOperations.GetBindingExpression(this, FilePathProperty)?.UpdateSource();
            e.Handled = true;
        }
    }
}
