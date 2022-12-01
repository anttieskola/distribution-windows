using DW.Domain.Abstract;
using DW.Domain.Entity;
using DW.Domain.Helpers;
#if (DYMO)
using Dymo;
#endif
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace DW.Domain.Service
{
    /// <summary>
    /// Service for distribution
    /// Note, that all dymo functionality is behind compilation definition
    /// so you can also compile code without label printer support.
    /// </summary>
    public class DistributionRepository : IDistributionRepository
    {
        #region constants
        // common
        private static string API_CONTENT_TYPE = "application/json; charset=utf-8";
        private static string API_ACCEPT_TYPE_TEXT = "text/plain";
        private static string API_ACCEPT_TYPE_JSON = "application/json";

        // cloud api (php)
        // Note, remember dont add starting or ending / in paths
        private static string API_PHP_HEADER_AUTH_TOKEN = "X-AUTH-TOKEN";
#if (DEBUG)
        private static Uri API_PHP_SERVER = new Uri("https://ioliving.com");
#else
        private static Uri API_PHP_SERVER = new Uri("https://ioliving.com");
#endif
        private static string API_PHP_PATH_LOGIN = "api/admin/login.php";
        private static string API_PHP_PATH_LOGIN_CHECK = "api/admin/authentication.php";
        private static string API_PHP_PATH_DISTRIBUTORS = "api/admin/distributors.php";
        private static string API_PHP_PATH_PRODUCTS = "api/admin/products.php";
        private static string API_PHP_PATH_REGISTER = "api/admin/register.php";

        #endregion

        #region fields
        private User _user;
        private Order _order;
        private bool _printingSupported;
        #endregion

        #region constructor - lazy singleton
        // Lazy used to give thread safety
        // http://msdn.microsoft.com/en-us/library/dd997286%28v=vs.110%29.aspx
        private static readonly Lazy<DistributionRepository> lazy =
            new Lazy<DistributionRepository>(() => new DistributionRepository());
        /// <summary>
        /// Get instance of the repository
        /// </summary>
        public static DistributionRepository Instance
        {
            get { return lazy.Value; }
        }
        /// <summary>
        /// Private constructor as singleton implementation
        /// </summary>
        private DistributionRepository()
        {
            _user = new User();
            _printingSupported = testLabelPrintingSupport();
        }
        #endregion

        #region login / identity
        /// <summary>
        /// Current logged user, empty data if not logged in
        /// </summary>
        public User CurrentUser
        {
            get { return _user; }
        }

        /// <summary>
        /// Check are we logged in and login still valid
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsLoggedIn()
        {
            // checking if no token
            if (String.IsNullOrEmpty(_user.Token))
            {
                return false;
            }
            // checking is token valid anymore by making request to api
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(API_PHP_SERVER + API_PHP_PATH_LOGIN_CHECK);
            req.ContentType = API_CONTENT_TYPE;
            req.Accept = API_ACCEPT_TYPE_TEXT;
            req.Method = "GET";
            // current token
            req.Headers.Add(API_PHP_HEADER_AUTH_TOKEN, _user.Token);
            try
            {
                // only interested is request response 200 / OK
                using (HttpWebResponse res = (HttpWebResponse)await req.GetResponseAsync())
                {
                    if (res.StatusCode == HttpStatusCode.OK)
                    {
                        return true;
                    }
                }
            } catch (WebException we)
            {
                // just returning false always, so even when network error that might be bit troubling but anyway...
            }
            return false;
        }

        public async Task<bool> Login(string userName, string passWord)
        {
            // setup request
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(API_PHP_SERVER + API_PHP_PATH_LOGIN);
            req.ContentType = API_CONTENT_TYPE;
            req.Accept = API_ACCEPT_TYPE_TEXT;
            req.Method = "POST";
            string data = JsonConvert.SerializeObject(new { username = userName, password = passWord });
            // catch network/request issues
            try
            {
                // write data to request, can already trigger exception if no network
                using (var sw = new StreamWriter(await req.GetRequestStreamAsync()))
                {
                    sw.Write(data);
                    sw.Flush();
                    sw.Close(); // set's content length
                }
                // get response and handle result
                using (HttpWebResponse res = (HttpWebResponse)await req.GetResponseAsync())
                {
                    if (res.StatusCode == HttpStatusCode.OK)
                    {
                        // checking token as status can be ok but no token
                        if (res.Headers[API_PHP_HEADER_AUTH_TOKEN] != null)
                        {
                            _user.UserName = userName;
                            _user.Token = res.Headers[API_PHP_HEADER_AUTH_TOKEN];
                            return true; // all ok
                        }
                        else
                        {
                            _user.Error = "No authentication token received from server";
                        }
                    }
                    else
                    {
                        _user.Error = "Invalid cloud server response";
                    }
                }
            }
            catch (WebException we)
            {
                // check response and it status
                if (we.Response != null)
                {
                    using (HttpWebResponse res = (HttpWebResponse)we.Response)
                    {
                        if (res.StatusCode == HttpStatusCode.Forbidden)
                        {
                            // wrong password/username
                            _user.Error = "Authentication failed";
                        }
                        else if (res.StatusCode == HttpStatusCode.BadRequest)
                        {
                            // post data wrong
                            _user.Error = "Malformed content";
                        }
                        else if (res.StatusCode == HttpStatusCode.NotFound)
                        {
                            // path not found on server
                            _user.Error = "Cloud server API path not found.";
                        }
                    }
                }
                else
                {
                    // no network, not working dns...
                    _user.Error = we.Message;
                }
            }
            return false;
        }

        public bool Logout()
        {
            _user = new User();
            _order = null;
            return true;
        }
        #endregion

        #region collections
        /// <summary>
        /// Distributor list
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Distributor>> Distributors()
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(API_PHP_SERVER + API_PHP_PATH_DISTRIBUTORS);
            req.ContentType = API_CONTENT_TYPE;
            req.Accept = API_ACCEPT_TYPE_JSON;
            // authentication
            req.Headers.Add(API_PHP_HEADER_AUTH_TOKEN, _user.Token);
            List<Distributor> cl = null;
            // catch network issues
            try
            {
                HttpWebResponse res = (HttpWebResponse)await req.GetResponseAsync();
                if (res.StatusCode == HttpStatusCode.OK)
                {
                    using (StreamReader sr = new StreamReader(res.GetResponseStream()))
                    using (JsonTextReader jr = new JsonTextReader(sr))
                    {
                        // Todo: Is it possible to lookup for certain token?
                        while (jr.Read())
                        {
                            // now we just look for array token
                            if (jr.TokenType == JsonToken.StartArray)
                            {
                                // parse array
                                JsonSerializer js = new JsonSerializer();
                                cl = js.Deserialize<List<Distributor>>(jr);
                                break; // we done all that is needed
                            }
                        }
                    }
                }
                else
                {
                    Tracer.Error("StatusCode != OK ({0})", res.StatusCode);
                }
            }
            catch (WebException we)
            {
                // no-op
                Tracer.Error("WebException {0}", we.Message);
            }
            return cl;
        }

        /// <summary>
        /// Product list
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Product>> Products()
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(API_PHP_SERVER + API_PHP_PATH_PRODUCTS);
            req.ContentType = API_CONTENT_TYPE;
            req.Accept = API_ACCEPT_TYPE_JSON;
            // authentication
            req.Headers.Add(API_PHP_HEADER_AUTH_TOKEN, _user.Token);
            List<Product> hwmList = null;
            // catch network issues
            try
            {
                HttpWebResponse res = (HttpWebResponse)await req.GetResponseAsync();
                if (res.StatusCode == HttpStatusCode.OK)
                {
                    using (StreamReader sr = new StreamReader(res.GetResponseStream()))
                    using (JsonTextReader jr = new JsonTextReader(sr))
                    {
                        // serializer
                        JsonSerializer js = new JsonSerializer();
                        // load's whole json
                        JObject json = JObject.Load(jr);
                        // find hw array
                        JArray jsonHwArray = (JArray)json["products"];
                        // deserialize
                        hwmList = jsonHwArray.ToObject<List<Product>>(js);
                    }
                }
                else
                {
                    Tracer.Error("StatusCode != OK ({0})", res.StatusCode);
                }
            }
            catch (WebException we)
            {
                // no-op
                Tracer.Error("WebException {0}", we.Message);
            }
            return hwmList;
        }

        /// <summary>
        /// Returns list of available label printers
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> LabelPrinters()
        {
            // acquire list of printers from dymo api
            List<string> printers = new List<string>();
            if (_printingSupported)
            {
#if (DYMO)
                DymoAddIn da = new DymoAddIn();
                string printersString = da.GetDymoPrinters();
                printers = new List<string>(printersString.Split('|'));
#endif
            }
            return printers;
        }

        public async Task<IEnumerable<LabelTemplate>> LabelTemplates()
        {
            // TODO: server missing labels, so using one local template
            return new List<LabelTemplate> 
            {
                new LabelTemplate { Name = "Example", TemplateFile = "Example.label" }
            };
        }
        #endregion

        #region order
        /// <summary>
        /// Current created order you are filling
        /// </summary>
        public Order CurrentOrder
        {
            get { return _order; }
        }

        /// <summary>
        /// Does current pc have dymo label printer installed?
        /// </summary>
        public bool IsLabelPrintingAvailable
        {
            get
            {
                return _printingSupported;
            }
        }

        /// <summary>
        /// Create order to be filled
        /// </summary>
        /// <param name="distributor"></param>
        /// <param name="product"></param>
        /// <param name="manufacturingDate"></param>
        /// <returns></returns>
        public bool CreateOrder(Distributor distributor, Product product, DateTime? manufacturingDate = null)
        {
            if (CurrentOrder == null)
            {
                _order = new Order
                {
                    Distributor = distributor,
                    Product = product,
                    ManufacturingDate = manufacturingDate,
                    LabelPrint = false,
                    LabelPrinter = null,
                    LabelTemplate = null,
                    Devices = new ObservableCollection<Device>()
                };
                return true;
            }
            return false;
        }

        /// <summary>
        /// Create order to be filled with label printing
        /// </summary>
        /// <param name="customer"></param>
        /// <param name="product"></param>
        /// <param name="labelPrinter"></param>
        /// <param name="labelTemplate"></param>
        /// <param name="manufacturingDate"></param>
        /// <returns></returns>
        public bool CreateOrder(Distributor customer, Product product, string labelPrinter, LabelTemplate labelTemplate, DateTime? manufacturingDate = null)
        {
            if (CurrentOrder == null)
            {
                _order = new Order
                {
                    Distributor = customer,
                    Product = product,
                    ManufacturingDate = manufacturingDate,
                    LabelPrint = true,
                    LabelPrinter = labelPrinter,
                    LabelTemplate = labelTemplate,
                    Devices = new ObservableCollection<Device>()
                };
                return true;
            }
            return false;
        }

        /// <summary>
        /// Save order, currently does not do anything
        /// but figured its best way to work if need support this
        /// in the future.
        /// </summary>
        /// <returns></returns>
        public bool SaveOrder()
        {
            if (_order == null)
            {
                return false;
            }
            _order = null;
            return true;
        }

        /// <summary>
        /// Register device into service, Order must be created before
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public async Task<RegisterDeviceResponse> RegisterDevice(Device device)
        {
            // return
            RegisterDeviceResponse ret = new RegisterDeviceResponse { Success = false, Error = "" };
            // check that order has been created
            if (CurrentOrder == null)
            {
                ret.Error = "Create order first.";
                return ret;
            }
            // request
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(API_PHP_SERVER + API_PHP_PATH_REGISTER);
            req.ContentType = API_CONTENT_TYPE;
            req.Accept = API_ACCEPT_TYPE_TEXT;
            req.Method = "POST";
            // authentication
            req.Headers.Add(API_PHP_HEADER_AUTH_TOKEN, _user.Token);
            // data, without and with manufacturing date
            string data;
            if (CurrentOrder.ManufacturingDate != null)
            {
                // mysql basic format of date, http://dev.mysql.com/doc/refman/5.1/en/datetime.html
                string date = CurrentOrder.ManufacturingDate.GetValueOrDefault().ToString("yyyy-MM-dd");
                data = JsonConvert.SerializeObject(new
                {
                    uid = formatUid(device.Uid),
                    hw = device.Product.Hw,
                    sw = device.Product.Sw,
                    distributor = CurrentOrder.Distributor.Id,
                    manufactured = date
                });
            }
            else
            {
                data = JsonConvert.SerializeObject(new
                {
                    uid = formatUid(device.Uid),
                    hw = device.Product.Hw,
                    sw = device.Product.Sw,
                    distributor = CurrentOrder.Distributor.Id
                });
            }
            // network issues or bad status codes from server
            try
            {
                // write data
                using (var sw = new StreamWriter(await req.GetRequestStreamAsync()))
                {
                    sw.Write(data);
                    sw.Flush();
                    sw.Close();
                }
                using (HttpWebResponse res = (HttpWebResponse)await req.GetResponseAsync())
                {
                    if (res.StatusCode == HttpStatusCode.OK)
                    {
                        // return 200, check content
                        using (StreamReader sr = new StreamReader(res.GetResponseStream()))
                        {
                            // check response body
                            try
                            {
                                string line = sr.ReadLine();
                                int value = int.Parse(line);
                                if (value == 0)
                                {
                                    // 0 means no errres
                                    ret.Success = true;
                                }
                                else if (value == 1)
                                {
                                    // 1 means already registered
                                    ret.Error = "Device already registered.";
                                }
                                else
                                {
                                    // valid integer but we don't know what it means
                                    ret.Error = "Cloud server unknown return value.";
                                }

                            }
                            catch (Exception)
                            {
                                // most likely php error
                                ret.Error = "Cloud server internal error.";
                            }
                        }
                    }
                    else
                    {
                        // status code wrong
                        ret.Error = "Invalid server response";
                    }
                }
            }
            catch (WebException we)
            {
                if (we.Response != null)
                {
                    using (HttpWebResponse res = (HttpWebResponse)we.Response)
                    {
                        if (res.StatusCode == HttpStatusCode.BadRequest)
                        {
                            // our data input is malformed
                            ret.Error = String.Format("({0}) Malformed input.", (int)res.StatusCode);
                        }
                        else if (res.StatusCode == HttpStatusCode.Forbidden)
                        {
                            //  invalid or expired authentication token
                            ret.Error = String.Format("({0}) Your previous authentication has expired. Please logout and login again to continue.", (int)res.StatusCode);
                        }
                        else if (res.StatusCode == HttpStatusCode.NotFound)
                        {
                            ret.Error = String.Format("({0}) Cloud server Api path not found.", (int)res.StatusCode);
                        }
                        else if (res.StatusCode == HttpStatusCode.InternalServerError)
                        {
                            // server error
                            ret.Error = String.Format("({0}) Cloud server internal error.", (int)res.StatusCode);
                        }
                    }
                }
                // if no response failure happened when writing data
                else
                {
                    ret.Error = "Network problem.\n" + we.Message;
                }
            }
            return ret;
        }

        /// <summary>
        /// Print label using dymo printer
        /// </summary>
        /// <param name="device"></param>
        /// <param name="labelTemplate"></param>
        /// <returns></returns>
        public async Task<PrintLabelResponse> PrintLabel(Device device, LabelTemplate labelTemplate)
        {
            PrintLabelResponse ret = new PrintLabelResponse { Success = false };
#if (DYMO)
            // check order set to print labels
            if (_order.LabelPrint)
            {
                // Wrapping printing be asyncronous to as it in testing
                // caused UI to be be unresponsive for a second or two.
                Task<bool> printTask = Task<bool>.Run(() =>
                {
                    // select printer and open template
                    DymoAddIn da = new DymoAddIn();
                    da.SelectPrinter(_order.LabelPrinter);
                    da.Open(labelTemplate.TemplateFile);

                    // set template values
                    DymoLabels dl = new DymoLabels();

                    // if param is true only objects that can be modified are on the list
                    string objectsString = dl.GetObjectNames(true);
                    List<string> objects = new List<string>(objectsString.Split('|'));

                    // TODO: would it be best to throw exception if label template
                    // does not contain all required fields. Now we just add info
                    // if the field is defined.
                    if (objects.Any(o => o.Equals("Customer")))
                    {
                        dl.SetField("Customer", _order.Distributor.Name);
                    }
                    if (objects.Any(o => o.Equals("Uid")))
                    {
                        dl.SetField("Uid", device.Uid);
                    }
                    if (objects.Any(o => o.Equals("Hardware")))
                    {
                        dl.SetField("Hardware",
                            String.Format("Hardware: {0}", device.Product.Hw));
                    }
                    if (objects.Any(o => o.Equals("Firmware")))
                    {
                        dl.SetField("Firmware",
                            String.Format("Firmware: {0}", device.Product.Sw));
                    }
                    if (objects.Any(o => o.Equals("Code")))
                    {
                        dl.SetField("Code", "123456789012");
                    }
                    da.Print(1, false);
                    return true;
                });
                ret.Success = await printTask;
            }
#endif
            return ret;
        }
        #endregion

        #region state management
        /// <summary>
        /// Load state of service on app startup
        /// </summary>
        /// <returns></returns>
        public bool LoadState()
        {
            using (IsolatedStorageFile iso = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly, null, null))
            {
                string file = "user.dat";
                try
                {
                    using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(file, FileMode.Open, iso))
                    {
                        DataContractSerializer dcs = new DataContractSerializer(typeof(User));
                        _user = (User)dcs.ReadObject(stream);
                        return true;
                    }
                }
                catch (FileNotFoundException)
                {
                    // no user dat written yet
                }
            }
            return false;
        }

        /// <summary>
        /// Save state of service on app shutdown
        /// </summary>
        public void SaveState()
        {
            using (IsolatedStorageFile iso = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly, null, null))
            {
                // currently we just save identity
                string file = "user.dat";
                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(file, FileMode.Create, iso))
                {
                    DataContractSerializer dcs = new DataContractSerializer(typeof(User));
                    dcs.WriteObject(stream, _user);
                }
            }
        }
        #endregion

        #region private methods
        /// <summary>
        /// Format uid for PHP server, this
        /// means it needs to be integer (Int64)
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        internal static Int64 formatUid(string uid)
        {
            try
            {
                return Int64.Parse(uid, NumberStyles.HexNumber);
            }
            catch (FormatException fe)
            {
                Tracer.Error(fe.Message);
            }
            catch (OverflowException ofe)
            {
                Tracer.Error(ofe.Message);
            }
            throw new ArgumentException("Invalid uid.");
        }

        /// <summary>
        /// check is sdk installed on machine
        /// check we can find atleast one printer
        /// </summary>
        /// <returns></returns>
        private bool testLabelPrintingSupport()
        {
#if (DYMO)
            try
            {
                // dymo api
                DymoAddIn da = new DymoAddIn();
                // char '|' separates printers
                string printersString = da.GetDymoPrinters();
                List<string> printers = new List<string>(printersString.Split('|'));
                if (printers.Count > 0)
                {
                    // driver ok, and printer found
                    return true;
                }
                // no printers found (but no com exception)
                return false;

            }
            catch (COMException ce)
            {
                // sdk not installed
                // printer not installed (sdk is but no printer in windows). 
                Tracer.Error(ce.Message);
            }
            catch (Exception e)
            {
                // other reason, should not happen
                Tracer.Error(e.Message);
            }
            return false;
#else
            return false;
#endif
        }
        #endregion
    }
}
