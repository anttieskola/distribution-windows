using DW.Domain.Abstract;
using DW.Wpf.Messages;
using DW.Wpf.View;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DW.Wpf.ViewModel
{
    /// <summary>
    /// Acts as a container/controller for the views and has always visible "toolbar".
    /// </summary>
    public class RootViewModel : ViewModelBase
    {
        #region bindable properties
        private string userName;
        /// <summary>
        /// Current logged in username, empty if nobody logged in.
        /// </summary>
        public string UserName
        {
            get { return this.userName; }
            private set { this.userName = value; RaisePropertyChanged("UserName"); }
        }

        private string version;
        /// <summary>
        /// Application version information, see AssemblyInformationalVersion for definition.
        /// </summary>
        public string Version
        {
            get { return this.version; }
            private set { this.version = value; RaisePropertyChanged("Version"); }
        }

        private UserControl currentView;
        /// <summary>
        /// Current view
        /// </summary>
        public UserControl CurrentView
        {
            get { return this.currentView; }
            set { this.currentView = value; RaisePropertyChanged("CurrentView"); }
        }

        private RelayCommand logoutCommand;
        /// <summary>
        /// Logout command
        /// </summary>
        public RelayCommand LogoutCommand
        {
            get { return this.logoutCommand; }
            private set { this.logoutCommand = value; RaisePropertyChanged("LogoutCommand"); }
        }

        private RelayCommand helpCommand;
        /// <summary>
        /// Help command
        /// </summary>
        public RelayCommand HelpCommand
        {
            get { return this.helpCommand; }
            private set { this.helpCommand = value; RaisePropertyChanged("HelpCommand"); }
        }
        #endregion

        #region fields
        private IDistributionRepository _repository;
        #endregion

        /// <summary>
        /// injection constructor
        /// </summary>
        /// <param name="repository"></param>
        public RootViewModel(IDistributionRepository repository)
        {
            // service
            _repository = repository;
            // commands
            helpCommand = new RelayCommand(showHelp);
            logoutCommand = new RelayCommand(logout, canLogout);
            // design setup
            if (IsInDesignMode)
            {
                designInit();
            }
            else
            {
                // listen to view change messages
                Messenger.Default.Register<ViewChange>(this, changeView);
                // version info
                try
                {
                    string versionNumber = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
                    version = "Version " + versionNumber;
                }
                catch (InvalidDeploymentException)
                {
                    // this happens when application has not been installed
                    version = "Not installed (debugging)";
                }
            }
        }

        /// <summary>
        /// Logout command canExecute
        /// </summary>
        /// <returns></returns>
        private bool canLogout()
        {
            return UserName != null;
        }

        /// <summary>
        /// Logout command execute
        /// </summary>
        private void logout()
        {
            MessageBoxResult mbr = MessageBox.Show("Are you sure you want to logout?",
                "Confirmation", MessageBoxButton.YesNo);
            switch (mbr)
            {
                case MessageBoxResult.Yes:
                    _repository.Logout();
                    changeView(new ViewChange { To = VMLocator.LoginViewKey });
                    break;
            }
        }

        /// <summary>
        /// Help command execute
        /// </summary>
        private void showHelp()
        {
            Help hlp = new Help();
            hlp.Show();
        }

        /// <summary>
        /// Change view of the application.
        /// This is set as listener for view change messages.
        /// </summary>
        /// <param name="vc"></param>
        private void changeView(ViewChange vc)
        {
            // check login when view change
            if (vc.To != VMLocator.LoginViewKey)
            {
                if (!Task<bool>.Run(async () => { return await _repository.IsLoggedIn(); }).Result)
                {
                    if (!vc.HideWarnings)
                    {
                        MessageBox.Show("Your previous authentication has expired. Select OK to go login view.", "Warning", MessageBoxButton.OK);
                    }
                    vc.To = VMLocator.LoginViewKey;
                }
            }

            // change view
            switch (vc.To)
            {
                case VMLocator.LoginViewKey:
                    UserName = null;
                    CurrentView = new Login();
                    break;
                case VMLocator.OrderSetupViewKey:
                    UserName = _repository.CurrentUser.UserName;
                    CurrentView = new OrderSetup();
                    break;
                case VMLocator.OrderFillViewKey:
                    UserName = _repository.CurrentUser.UserName;
                    CurrentView = new OrderFill();
                    break;
                default:
                    throw new ArgumentException("Unknown viewchange target.");
            }
            LogoutCommand.RaiseCanExecuteChanged();
        }

        #region state management
        /// <summary>
        /// Load saved application state
        /// </summary>
        /// <returns></returns>
        public void LoadState()
        {
            _repository.LoadState();
            changeView(new ViewChange { To = VMLocator.OrderSetupViewKey, HideWarnings = true });
        }

        /// <summary>
        /// Save application state
        /// </summary>
        /// <returns></returns>
        public void SaveState()
        {
            _repository.SaveState();
        }
        #endregion

        #region design init
        private void designInit()
        {
            userName = "JamesBond";
            version = "Version 1.0";
            currentView = new OrderSetup();
        }
        #endregion
    }
}
