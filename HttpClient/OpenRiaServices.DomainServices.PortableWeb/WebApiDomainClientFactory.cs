using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace OpenRiaServices.DomainServices.Client.PortableWeb
{
    public class WebApiDomainClientFactory : DomainClientFactory
    {
        public WebApiDomainClientFactory()
        {
            HttpClientHandler = new HttpClientHandler()
                {
                    CookieContainer = new System.Net.CookieContainer(),
                    UseCookies = true
                };
        }

        protected override DomainClient CreateDomainClientCore(Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint)
        {
            return new WebApiDomainClient(serviceContract, serviceUri, HttpClientHandler);
        }

        public HttpClientHandler HttpClientHandler { get; set; }
    }
}
