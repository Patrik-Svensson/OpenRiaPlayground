using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using OpenRiaServices.DomainServices.Client;
using OpenRiaServices.DomainServices.Client.ApplicationServices;
using OpenRiaServices.DomainServices.Client.PortableWeb;
using TestDomainServices;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Windows8App
{
    public class Proxy : System.Net.IWebProxy
    {
        public System.Net.ICredentials Credentials
        {
            get;
            set;
        }

        private readonly Uri _proxyUri;

        public Proxy(Uri proxyUri)
        {
            _proxyUri = proxyUri;
        }

        public Uri GetProxy(Uri destination)
        {
            return _proxyUri;
        }

        public bool IsBypassed(Uri host)
        {
            return false;
        }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        readonly ServerSideAsyncDomainContext _ctx = new ServerSideAsyncDomainContext(
            App.DomainClientFactory.CreateDomainClient(typeof(ServerSideAsyncDomainContext.IServerSideAsyncDomainServiceContract),
                            new Uri(@"TestDomainServices-ServerSideAsyncDomainService.svc", UriKind.Relative), false));

        public MainPage()
        {
            this.InitializeComponent();
        }


        public string Status1
        {
            get { return _staus1; }
            set
            {
                _staus1 = DateTime.Now.ToString() + ": " + value; ;
                txt.Text = (_staus1 + Environment.NewLine + _staus2);
            }
        }
        private string _staus1 = string.Empty;

        public string Status2
        {
            get { return _staus2; }
            set
            {
                _staus2 = DateTime.Now.ToString() + ": " + value;
                txt.Text = (_staus1 + Environment.NewLine + _staus2);
            }
        }

        private string _staus2 = string.Empty;

        private void Invoke1_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Status1 = string.Format("starting Invoke 1");
                var res = _ctx.AddOne(23);
                res.Completed += (o, args) =>
                {
                    Status1 = string.Format("AddOneTaskAsync(23) = {0}", res.Value);
                };
            }
            catch (Exception ex)
            {
                ShowMessage("Error: " + ex.Message);
            }
        }

        private static void ShowMessage(string message)
        {
            var dlg = new MessageDialog(message);
            dlg.ShowAsync();
        }

        private async void Invoke2_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Status2 = string.Format("starting Invoke 2");
                var res = await _ctx.AddOneAsync(22);
                Status2 = string.Format("AddOneTaskAsync(22) = {0}", res.Value);
            }
            catch (Exception ex)
            {
                ShowMessage("Error: " + ex.Message);
            }
        }

        private void Get_Click(object sender, RoutedEventArgs e)
        {
            var query = _ctx.GetRangeQuery();
            query.IncludeTotalCount = true;
            query = (from r in query
                     where r.Id > 1
                     orderby r.Id descending
                     select r
                    ).Take(2);

            _ctx.Load(query
                , res =>
                {
                    entities.ItemsSource = res.Entities;
                    _items.ItemsSource = _ctx.RangeItems;
                }, null);
        }

        private void Async1_OnClick(object sender, RoutedEventArgs e)
        {
            _ctx.Load(_ctx.GetRangeByIdTaskQuery(1), (res) =>
            {
                if (res.HasError)
                {
                    res.MarkErrorAsHandled();
                    async1Res.Text = res.Error.ToString();
                }
                else
                    async1Res.Text = string.Format("Returned a {0}", res.Entities.FirstOrDefault());
            }, null);
        }

        private void Async2_OnClick(object sender, RoutedEventArgs e)
        {
            _ctx.Load(_ctx.GetQueryableRangeTaskQuery()
                .Where(r => string.Compare(r.Text, "Text 4") < 0), (res) =>
            {
                if (res.HasError)
                {
                    res.MarkErrorAsHandled();
                    async2Res.Text = res.Error.ToString();
                }
                else
                    async2Res.Text = string.Format("Returned a {0}", res.Entities.FirstOrDefault());
            }, null);
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            _ctx.SubmitChanges(res =>
                {
                    if (res.HasError)
                    {
                        res.MarkErrorAsHandled();
                        submitRes.Text = res.Error.ToString();
                    }
                    else
                        submitRes.Text = "Submit ok";
                }, null);
        }

        private void NormalException_OnClick(object sender, RoutedEventArgs e)
        {
            _ctx.Load(_ctx.GetRangeWithNormalExceptionQuery(), res =>
            {
                UpdateResults(res, _normalExceptionRes, "Normal exception");
            }, null);
        }

        private void NotAuthorized_OnClick(object sender, RoutedEventArgs e)
        {
            _ctx.Load(_ctx.GetRangeWithNotAuthorizedQuery(), res =>
            {
                UpdateResults(res, _notAuthorizedRes, "Mot authorized");
            }, null);
        }

        private void DomainException_OnClick(object sender, RoutedEventArgs e)
        {
            _ctx.Load(_ctx.GetRangeByIdWithExceptionFirstQuery(1), res =>
            {
                UpdateResults(res, _domainExceptionRes, "Domain exception");
            }, null);
        }

        private static void UpdateResults(LoadOperation<RangeItem> res, TextBlock textBox, string operation)
        {
            if (res.HasError)
            {
                res.MarkErrorAsHandled();
                textBox.Text = string.Format("{0} had error of type {1} with message '{2}'",
                        operation,
                        res.Error.GetType(),
                        res.Error.Message);
            }
            else
                textBox.Text = string.Format("{0} loaded {1} entities successfully", operation, res.Entities.Count());
        }

        private static void UpdateResults<T>(InvokeOperation<T> res, TextBlock textBox, string operation)
        {
            if (res.HasError)
            {
                res.MarkErrorAsHandled();
                textBox.Text = string.Format("{0} had error of type {1} with message '{2}'",
                        operation,
                        res.Error.GetType(),
                        res.Error.Message);
            }
            else
                textBox.Text = string.Format("{0} invoked successfully with result {1}", operation, res.Value);
        }

        private void InvokeComplex_OnClick(object sender, RoutedEventArgs e)
        {
            ComplexType2 c = new ComplexType2{ A =23, B ="Lite text"};
            _ctx.GetRangeWithComplexParameterPOST(c, 23, "heJ", res => 
                {
                    UpdateResults(res, _invokeComplexRes1, "GetRangeWithComplexParameterPOST");
                }, null);

            _ctx.GetRangeWithComplexParameterGET(c, 23, "heJ", res => 
                {
                    UpdateResults(res, _invokeComplexRes2, "GetRangeWithComplexParameterGET");
                }, null);
        }

        private void Login_OnClick(object sender, RoutedEventArgs e)
        {
            var loginParameters = new LoginParameters(_loginName.Text, _loginPassword.Text);

            WebContext.Current.Authentication.Login(loginParameters,
                (res) =>
                {
                    if (!res.HasError)
                        _userName.Text = res.User.Identity.Name;
                    else
                        _userName.Text = string.Format("Login error: {0}", res.Error);
                }, null);
        }

        private void Logout_OnClick(object sender, RoutedEventArgs e)
        {
            WebContext.Current.Authentication.Logout(false);
        }
    }
}
