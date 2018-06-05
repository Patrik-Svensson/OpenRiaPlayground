
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Windows;
using OpenRiaServices.DomainServices.Client;
using OpenRiaServices.DomainServices.Client.ApplicationServices;
using OpenRiaServices.DomainServices.Client.PortableWeb;
using OpenRiaServices.DomainServices.Client.Web;

namespace HttpClientExampleClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.Startup += App_Startup;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            // Web client 
            DomainContext.DomainClientFactory = new WebApiDomainClientFactory()
            {
                HttpClientHandler = new HttpClientHandler()
                {
                    UseProxy = true,
                    AutomaticDecompression = System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.GZip
                },
                ServerBaseUri = new Uri("http://localhost:51359/ClientBin/", UriKind.Absolute)
            };


            // Enable HTTP/2 support
            DomainContext.DomainClientFactory = new WebApiDomainClientFactory()
            {
                HttpClientHandler = new Http2CustomHandler()
                {
                    AutomaticDecompression = System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.GZip,
                    WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy,
                },
                ServerBaseUri = new Uri("https://localhost:44300/ClientBin/", UriKind.Absolute)
            };

            /*
            DomainContext.DomainClientFactory = new OpenRiaServices.DomainServices.Client.Web.WebDomainClientFactory()
            {
                // Uncomment this to debug in fiddler
                // ServerBaseUri = new Uri("http://localhost.fiddler:51359/ClientBin/", UriKind.Absolute)
                ServerBaseUri = new Uri("http://localhost:51359/ClientBin/", UriKind.Absolute)
            };


            
            */

            // Create a WebContext and add it to the ApplicationLifetimeObjects collection.
            // This will then be available as WebContext.Current.
            WebContext webContext = new WebContext();
            webContext.Authentication = new FormsAuthentication();


            var main = new MainWindow();
            main.Show();
        }
    }
}
