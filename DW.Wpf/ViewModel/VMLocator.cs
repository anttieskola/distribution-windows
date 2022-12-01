using DW.Domain.Abstract;
using DW.Domain.Service;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;

namespace DW.Wpf.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the application
    /// to be referenced in xaml. Also setups unity for service locator.
    /// </summary>
    public class VMLocator
    {
        #region constructor
        /// <summary>
        /// In constructor we register types for unity and set it as our
        /// service locator.
        /// </summary>
        public VMLocator()
        {
            // TODO: We could or should use unity lifetime manager so it would store
            // a reference to repository and so fort it would be alive as long as we define
            // but currently root view has reference so it works as "lifetime manager".    
            UnityContainer container = new UnityContainer();
            //container.RegisterType<IDistributionRepository, DistributionRepository>(new InjectionFactory(d => DistributionRepository.Instance)); // real
            container.RegisterType<IDistributionRepository, DemoDistributionRepository>(new InjectionFactory(d => DemoDistributionRepository.Instance)); // demo
            UnityServiceLocator locator = new UnityServiceLocator(container);
            ServiceLocator.SetLocatorProvider(() => locator);
        }
        #endregion

        #region viewmodel definitions for xaml and view key strings
        public RootViewModel Root
        {
            get
            {
                return ServiceLocator.Current.GetInstance<RootViewModel>();
            }
        }

        public LoginViewModel Login
        {
            get
            {
                return ServiceLocator.Current.GetInstance<LoginViewModel>();
            }
        }
        public const string LoginViewKey = "Login";

        public OrderSetupViewModel OrderSetup
        {
            get
            {
                return ServiceLocator.Current.GetInstance<OrderSetupViewModel>();
            }
        }
        public const string OrderSetupViewKey = "OrderSetup";

        public OrderFillViewModel OrderFill
        {
            get
            {
                return ServiceLocator.Current.GetInstance<OrderFillViewModel>();
            }
        }
        public const string OrderFillViewKey = "OrderFill";
        #endregion
    }
}