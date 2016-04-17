extern alias HttpClient;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using HttpClient::OpenRiaServices.DomainServices.Client.PortableWeb;
using OpenRiaServices.DomainServices.Client;
using OpenRiaServices.DomainServices.Client.ApplicationServices;

namespace WpfWithPortable
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Create a WebContext and add it to the ApplicationLifetimeObjects collection.
        // This will then be available as WebContext.Current.
        //private WebContext webContext;

        public App()
        {
        //    // Create a WebContext and add it to the ApplicationLifetimeObjects collection.
        //    // This will then be available as WebContext.Current.
        //    webContext = new WebContext();
        //    webContext.Authentication = new FormsAuthentication();
        //    //webContext.Authentication = new WindowsAuthentication();
            
        //    webContext.Authentication.LoadUser((res) =>
        //    {
        //        if (res.HasError)
        //            MessageBox.Show("LoadUser failed with error: " + res.Error.ToString());
        //        else
        //            MessageBox.Show(string.Format("User loaded: Identity = {0}, authentication = {1}", res.User.Identity, res.User.Identity.IsAuthenticated));
        //    }, null);

            this.Startup += App_Startup;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            DomainContext.DomainClientFactory = new WebApiDomainClientFactory()
            {
                HttpClientHandler = new HttpClientHandler()
                {
                    UseProxy = true,
                    AutomaticDecompression = System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.GZip
                },
                // Uncomment this to debug in fiddler
                // ServerBaseUri = new Uri("http://localhost.fiddler:51359/ClientBin/", UriKind.Absolute)
                ServerBaseUri = new Uri("http://localhost:51359/ClientBin/", UriKind.Absolute)
            };

            DomainContext.DomainClientFactory = new OpenRiaServices.DomainServices.Client.Web.WebDomainClientFactory()
            {
                // Uncomment this to debug in fiddler
                // ServerBaseUri = new Uri("http://localhost.fiddler:51359/ClientBin/", UriKind.Absolute)
                ServerBaseUri = new Uri("http://localhost:51359/ClientBin/", UriKind.Absolute)
            };

            // Create a WebContext and add it to the ApplicationLifetimeObjects collection.
            // This will then be available as WebContext.Current.
            WebContext webContext = new WebContext();
            webContext.Authentication = new FormsAuthentication();
        }
    }
}
