using DW.Domain.Abstract;
using DW.Wpf.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Views;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DW.Wpf.ViewModel
{
    public class LoginViewModel : ViewModelBase
    {
        #region binding properties
        private String message;
        /// <summary>
        /// Can be welcome message, error message...
        /// </summary>
        public String Message
        {
            get { return this.message; }
            set { this.message = value; RaisePropertyChanged("Message"); }
        }

        private String userName;
        public String UserName
        {
            get { return this.userName; }
            set 
            {
                this.userName = value;
                RaisePropertyChanged("UserName");
                if (!String.IsNullOrEmpty(this.userName))
                {
                    _loginEnabled = true;
                }
                else
                {
                    _loginEnabled = false;
                }
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        private RelayCommand loginCommand;
        public RelayCommand LoginCommand
        {
            get { return this.loginCommand; }
            private set { this.loginCommand = value; RaisePropertyChanged("LoginCommand"); }
        }
        #endregion

        #region password setting
        /// <summary>
        /// Set password user has given
        /// </summary>
        /// <param name="passWord"></param>
        public void SetPassWord(string passWord)
        {
            _passWord = passWord;
        }
        #endregion

        #region fields
        private IDistributionRepository _repository;
        private bool _inProgress;
        private bool _loginEnabled;
        private string _passWord;
        #endregion

        /// <summary>
        /// injection constructor
        /// </summary>
        /// <param name="repository"></param>
        public LoginViewModel(IDistributionRepository repository)
        {
            _repository = repository;
            _repository.Logout();
            _passWord = "";
            LoginCommand = new RelayCommand(login, () => { return _loginEnabled && !_inProgress; });
        }

        private async void login()
        {
            // disable login button while in progress
            _inProgress = true;
            if (await _repository.Login(userName, _passWord))
            {
                // login successful change view to order seup
                Messenger.Default.Send<ViewChange>(new ViewChange { To = VMLocator.OrderSetupViewKey });
            }
            else
            {
                // problem with login
                Message += _repository.CurrentUser.Error + "\n";
            }
            // enable login again
            _inProgress = false; 
            LoginCommand.RaiseCanExecuteChanged();
        }
    }
}
