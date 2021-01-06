using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace UOArtMerge
{
    /// <summary>
    /// Interaction logic for SaveDialog.xaml
    /// </summary>
    public partial class SaveDialog
    {
        private readonly int _set;
        private readonly MainWindow _window;
        private bool _canClose;

        public SaveDialog(int set, MainWindow window)
        {
            _set = set;
            _window = window;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;
            _ = SetWindowLong(handle, _gwlStyle, GetWindowLong(handle, _gwlStyle) & ~_wsSysMenu);

            Task.Factory.StartNew(() =>
            {
                _window.Save(_set);
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                {
                    _canClose = true;
                    Close();
                }));
            });
        }

        private const int _gwlStyle = -16;
        private const int _wsSysMenu = 0x80000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!_canClose)
            {
                e.Cancel = true;
            }
        }
    }
}
