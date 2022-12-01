using DW.Wpf.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace DW.Wpf.View
{
    /// <summary>
    /// Interaction logic for Root.xaml
    /// </summary>
    public partial class Root : Window
    {
        private RootViewModel vm
        {
            get { return GridDataContext.DataContext as RootViewModel; }
        }

        public Root()
        {
            InitializeComponent();
            // state management
            this.Loaded += RootLoaded;
            this.Closed += RootClosed;
        }

        private void RootLoaded(object sender, RoutedEventArgs e)
        {
            vm.LoadState();
        }

        private void RootClosed(object sender, EventArgs e)
        {
            vm.SaveState();
        }
    }
}
