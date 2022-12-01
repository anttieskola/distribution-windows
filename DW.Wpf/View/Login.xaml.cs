using DW.Wpf.ViewModel;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : UserControl
    {
        public Login()
        {
            InitializeComponent();
            this.KeyDown += Login_KeyDown;
            this.Loaded += Login_Loaded;
        }

        void Login_Loaded(object sender, RoutedEventArgs e)
        {
            Username.Focus();
        }

        private LoginViewModel vm
        {
            get { return GridDataContext.DataContext as LoginViewModel; }
        }

        void Login_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !e.Handled && vm.LoginCommand.CanExecute(null))
            {
                e.Handled = true;
                vm.LoginCommand.Execute(null);
            }
        }

        private void Password_PasswordChanged(object sender, RoutedEventArgs e)
        {    
            PasswordBox pb = sender as PasswordBox;
            vm.SetPassWord(pb.Password);
        }
    }
}
