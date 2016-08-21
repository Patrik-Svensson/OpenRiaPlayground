using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using OpenRiaServices.DomainServices.Client;
using OpenRiaServices.DomainServices.Client.ApplicationServices;
using OpenRiaServices.DomainServices.Client.PortableWeb;
using SilverlightApplication1.Web;
using Xamarin.Forms;

namespace XamarinXamlApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Handle when your app starts
            DomainContext.DomainClientFactory = new WebApiDomainClientFactory()
            {
                HttpClientHandler = new HttpClientHandler()
                {
                    AutomaticDecompression = System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.GZip
                },
                // TODO: REPLACE WITH IP OF YOUR COMPUTER
                // We might needto query this !
                //ServerBaseUri = new Uri("http://localhost:51359/ClientBin/", UriKind.Absolute)
                ServerBaseUri = new Uri("http://169.254.80.80:51359/ClientBin/", UriKind.Absolute)
                
            };

            // Create a WebContext and add it to the ApplicationLifetimeObjects collection.
            // This will then be available as WebContext.Current.
            WebContext webContext = new WebContext();
            webContext.Authentication = new FormsAuthentication()
            {
                DomainContext = new AuthenticationDomainService1()
            };

            MainPage = new XamarinXamlApp.MainPage();
        }

        protected override void OnStart()
        {

        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
