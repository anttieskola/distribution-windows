using DW.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DW.Domain.Abstract
{
    /// <summary>
    /// Distribution repository interface definition. Thru this
    /// view models interract with business logic.
    /// </summary>
    public interface IDistributionRepository
    {
        #region collections
        /// <summary>
        /// Distributor collection
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Distributor>> Distributors();
        
        /// <summary>
        /// Product model collection
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Product>> Products();

        /// <summary>
        /// Label printer collection
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> LabelPrinters();

        /// <summary>
        /// Label template collection
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<LabelTemplate>> LabelTemplates();
        #endregion

        #region user functionality
        /// <summary>
        /// Current user logged in,  set this by calling login
        /// </summary>
        User CurrentUser { get; }

        /// <summary>
        /// Check are we logged in and login still valid
        /// </summary>
        /// <returns></returns>
        Task<bool> IsLoggedIn();

        /// <summary>
        /// Login with given info
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="passWord"></param>
        /// <returns></returns>
        Task<bool> Login(string userName, string passWord);

        /// <summary>
        /// Logout
        /// </summary>
        /// <returns></returns>
        bool Logout();
        #endregion

        #region order functionality
        /// <summary>
        /// Current order in process, set by creating new
        /// </summary>
        Order CurrentOrder { get; }

        /// <summary>
        /// Does current environment have supported label printer
        /// </summary>
        bool IsLabelPrintingAvailable { get; }

        /// <summary>
        /// Create order
        /// </summary>
        /// <param name="distributor"></param>
        /// <param name="product"></param>
        /// <param name="manufacturingDate"></param>
        /// <returns></returns>
        bool CreateOrder(Distributor distributor, Product product, DateTime? manufacturingDate = null);

        /// <summary>
        /// Create order, with label printing
        /// </summary>
        /// <param name="customer"></param>
        /// <param name="product"></param>
        /// <param name="labelPrinter"></param>
        /// <param name="labelTemplate"></param>
        /// <param name="manufacturingDate"></param>
        /// <returns></returns>
        bool CreateOrder(Distributor customer, Product product, string labelPrinter, LabelTemplate labelTemplate, DateTime? manufacturingDate = null);

        /// <summary>
        /// Save order, must be called before creating new one
        /// </summary>
        /// <returns></returns>
        bool SaveOrder();

        /// <summary>
        /// Register device, must have order in progress
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        Task<RegisterDeviceResponse> RegisterDevice(Device device);
        /// <summary>
        /// Print label for device
        /// </summary>
        /// <returns></returns>
        Task<PrintLabelResponse> PrintLabel(Device device, LabelTemplate labelTemplate);
        #endregion

        #region state management
        /// <summary>
        /// Load settings (during launch)
        /// </summary>
        /// <returns></returns>
        bool LoadState();

        /// <summary>
        /// Save settings (during exit)
        /// </summary>
        /// <returns></returns>
        void SaveState();
        #endregion
    }
}
