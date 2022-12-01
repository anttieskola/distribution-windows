using DW.Wpf.Helpers;
using DW.Wpf.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DW.Wpf.View
{
    /// <summary>
    /// Interaction logic for OrderFill.xaml
    /// </summary>
    public partial class OrderFill : UserControl
    {
        public OrderFill()
        {
            InitializeComponent();
            // hookup to (un)loading of control
            this.Loaded += orderFill_Loaded;
            this.Unloaded += orderFill_Unloaded;
        }

        private OrderFillViewModel vm
        {
            get { return GridViewModel.DataContext as OrderFillViewModel; }
        }

        private void orderFill_Loaded(object sender, RoutedEventArgs e)
        {
            // hookup to keydown events that come from handheld scanner
            App.Current.MainWindow.KeyDown += WindowKeyDown;
            // hook autoscroll
            vm.Order.Devices.CollectionChanged += device_changed;
        }

        private void orderFill_Unloaded(object sender, RoutedEventArgs e)
        {
            // unhook keydown events or we would track them in other views as well
            App.Current.MainWindow.KeyDown -= WindowKeyDown;
            // unhook autoscroll
            vm.Order.Devices.CollectionChanged -= device_changed;
        }

        private async void WindowKeyDown(object sender, KeyEventArgs e)
        {
            // using helper to convert key into char as it quite complex
            await vm.InputChar(KeyToChar.Get(e.Key));
        }

        private void device_changed(object sender, NotifyCollectionChangedEventArgs e)
        {
            // poor man's autoscroll
            int count = ListViewDevices.Items.Count;
            if (count > 1)
            {
                ListViewDevices.ScrollIntoView(ListViewDevices.Items.GetItemAt( count - 1));
            }
        }

        private void ManualUidKeyDown(object sender, KeyEventArgs e)
        {
            // fire click event on button
            if (e.Key.Equals(Key.Enter) || e.Key.Equals(Key.Return))
            {
                if (vm.ManualUidRegisterCommand.CanExecute(null))
                {
                    vm.ManualUidRegisterCommand.Execute(null);
                }
                e.Handled = true;
            }
            e.Handled = false;
        }

        private void ManualInputChecked(object sender, RoutedEventArgs e)
        {
            // try to set focus on input box
            ManualUidTextBox.Focus();
        }
    }
}
