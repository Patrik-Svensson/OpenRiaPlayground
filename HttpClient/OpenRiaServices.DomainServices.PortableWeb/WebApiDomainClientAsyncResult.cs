using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;

namespace OpenRiaServices.DomainServices.Client.PortableWeb
{
    /// <summary>
    /// Internal <see cref="IAsyncResult"/> used during <see cref="WebDomainClient&lt;TContract&gt;"/> operations.
    /// </summary>
    internal sealed class WebApiDomainClientAsyncResult : DomainClientAsyncResult
    {
        /// <summary>
        /// The contract type class
        /// </summary>
        private Type _interfaceType;
        private IEnumerable<ChangeSetEntry> _changeSetEntries;
        private MethodInfo _beginOperationMethod;
        private MethodInfo _endOperationMethod;
        private readonly string _operationName;

        /// <summary>
        /// Initializes a new <see cref="WebDomainClientAsyncResult&lt;TContract&gt;"/> instance used for Query operations.
        /// </summary>
        /// <param name="callback">Optional <see cref="AsyncCallback"/> to invoke upon completion.</param>
        /// <param name="asyncState">Optional user state information that will be passed to the <paramref name="callback"/>.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="domainClient"/> is null.</exception>
        /// <exception cref="ArgumentNullException">if <paramref name="endOperationMethod"/> is null.</exception>
        private WebApiDomainClientAsyncResult(WebApiDomainClient domainClient, EntityQuery query, AsyncCallback callback, object asyncState)
            : base(domainClient, callback, asyncState)
        {
            // base class validates domainClient
            if (query == null)
                throw new ArgumentNullException("query");

            _interfaceType = domainClient.ServiceInterfaceType;
            _operationName = query.QueryName;
        }

        /// <summary>
        /// Initializes a new <see cref="WebDomainClientAsyncResult&lt;TContract&gt;"/> instance used for Submit operations.
        /// </summary>
        /// <param name="entityChangeSet">The Submit operation <see cref="EntityChangeSet"/>.</param>
        /// <param name="changeSetEntries">The collection of <see cref="ChangeSetEntry"/>s to submit.</param>
        /// <param name="callback">Optional <see cref="AsyncCallback"/> to invoke upon completion.</param>
        /// <param name="asyncState">Optional user state information that will be passed to the <paramref name="callback"/>.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="domainClient"/> is null.</exception>
        /// <exception cref="ArgumentNullException">if <paramref name="endOperationMethod"/> is null.</exception>
        private WebApiDomainClientAsyncResult(WebApiDomainClient domainClient, EntityChangeSet entityChangeSet, IEnumerable<ChangeSetEntry> changeSetEntries, AsyncCallback callback, object asyncState)
            : base(domainClient, entityChangeSet, callback, asyncState)
        {
            // base class validates domainClient

            _interfaceType = domainClient.ServiceInterfaceType;
            _operationName = "SubmitChanges";
            _changeSetEntries = changeSetEntries;
        }

        /// <summary>
        /// Initializes a new <see cref="WebDomainClientAsyncResult&lt;TContract&gt;"/> instance used for Invoke operations.
        /// </summary>
        /// <param name="invokeArgs">The arguments to the Invoke operation.</param>
        /// <param name="callback">Optional <see cref="AsyncCallback"/> to invoke upon completion.</param>
        /// <param name="asyncState">Optional user state information that will be passed to the <paramref name="callback"/>.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="domainClient"/> is null.</exception>
        /// <exception cref="ArgumentNullException">if <paramref name="endOperationMethod"/> is null.</exception>
        private WebApiDomainClientAsyncResult(WebApiDomainClient domainClient, InvokeArgs invokeArgs, AsyncCallback callback, object asyncState)
            : base(domainClient, invokeArgs, callback, asyncState)
        {
            // base class validates domainClient
            if (invokeArgs == null)
                throw new ArgumentNullException("invokeArgs");

            _interfaceType = domainClient.ServiceInterfaceType;
            _operationName = invokeArgs.OperationName;
        }

        /// <summary>
        /// Gets a collection of <see cref="ChangeSetEntry"/>s used with Submit operations.
        /// </summary>
        public IEnumerable<ChangeSetEntry> ChangeSetEntries
        {
            get
            {
                return this._changeSetEntries;
            }
        }

        /// <summary>
        /// Gets the method that completes an asynchronous operation.
        /// </summary>
        internal MethodInfo EndOperationMethod
        {
            get
            {
                return this._endOperationMethod ?? (_endOperationMethod = ResolveEndMethod());
            }
        }

        /// <summary>
        /// Gets the method that starts an asynchronous operation.
        /// </summary>
        internal MethodInfo BeginOperationMethod
        {
            get
            {
                return this._beginOperationMethod ?? (_beginOperationMethod = ResolveBeginMethod());
            }
        }

        /// <summary>
        /// Gets the name of the operation.
        /// </summary>
        internal string OperationName
        {
            get
            {
                return this._operationName;
            }
        }

        /// <summary>
        /// Creates a new <see cref="WebDomainClientAsyncResult&lt;TContract&gt;"/> used for Query operations.
        /// </summary>
        /// <param name="domainClient">The <see cref="WebDomainClient&lt;TContract&gt;"/> associated with this result.</param>
        /// 
        /// <param name="endOperationMethod">The method that completes an asynchronous operation.</param>
        /// <param name="callback">The <see cref="AsyncCallback"/> to invoke upon completion.</param>
        /// <param name="asyncState">Optional user state information that will be passed to the <paramref name="callback"/>.</param>
        /// <returns>A <see cref="WebDomainClientAsyncResult&lt;TContract&gt;"/> used for Query operations</returns>
        public static WebApiDomainClientAsyncResult CreateQueryResult(WebApiDomainClient domainClient, EntityQuery query, AsyncCallback callback, object asyncState)
        {
            return new WebApiDomainClientAsyncResult(domainClient, query, callback, asyncState);
        }

        /// <summary>
        /// Creates a new <see cref="WebDomainClientAsyncResult&lt;TContract&gt;"/> used for Submit operations.
        /// </summary>
        /// <param name="domainClient">The <see cref="WebDomainClient&lt;TContract&gt;"/> associated with this result.</param>
        /// 
        /// <param name="endOperationMethod">The method that completes an asynchronous operation.</param>
        /// <param name="entityChangeSet">The Submit operation <see cref="EntityChangeSet"/>.</param>
        /// <param name="changeSetEntries">The collection of <see cref="ChangeSetEntry"/>s to submit.</param>
        /// <param name="callback">The <see cref="AsyncCallback"/> to invoke upon completion.</param>
        /// <param name="asyncState">Optional user state information that will be passed to the <paramref name="callback"/>.</param>
        /// <returns>A <see cref="WebDomainClientAsyncResult&lt;TContract&gt;"/> used for Submit operations</returns>
        public static WebApiDomainClientAsyncResult CreateSubmitResult(WebApiDomainClient domainClient, EntityChangeSet entityChangeSet, IEnumerable<ChangeSetEntry> changeSetEntries, AsyncCallback callback, object asyncState)
        {
            return new WebApiDomainClientAsyncResult(domainClient, entityChangeSet, changeSetEntries, callback, asyncState);
        }

        /// <summary>
        /// Creates a new <see cref="WebDomainClientAsyncResult&lt;TContract&gt;"/> used for Invoke operations.
        /// </summary>
        /// <param name="domainClient">The <see cref="WebDomainClient&lt;TContract&gt;"/> associated with this result.</param>
        /// 
        /// <param name="endOperationMethod">The method that completes an asynchronous operation.</param>
        /// <param name="invokeArgs">The arguments to the Invoke operation.</param>
        /// <param name="callback">The <see cref="AsyncCallback"/> to invoke upon completion.</param>
        /// <param name="asyncState">Optional user state information that will be passed to the <paramref name="callback"/>.</param>
        /// <returns>A <see cref="WebDomainClientAsyncResult&lt;TContract&gt;"/> used for Invoke operations</returns>
        public static WebApiDomainClientAsyncResult CreateInvokeResult(WebApiDomainClient domainClient, InvokeArgs invokeArgs, AsyncCallback callback, object asyncState)
        {
            return new WebApiDomainClientAsyncResult(domainClient, invokeArgs, callback, asyncState);
        }

        /// <summary>
        /// Attempts to cancel this operation and aborts the underlying request if cancellation was successfully requested.
        /// </summary>
        public override void Cancel()
        {
            base.Cancel();

            if (this.CancellationRequested)
            {
                // TODO: Dispose/abort HttpClient

            }
        }


        private new WebApiDomainClient DomainClient { get { return (WebApiDomainClient)base.DomainClient; } }
        private Type ServiceInterfaceType { get { return this.DomainClient.ServiceInterfaceType; } }

        private MethodInfo ResolveBeginMethod()
        {
            var methodName = "Begin" + _operationName;
#if REFLECTION_V2
            MethodInfo m = this._interfaceType.GetTypeInfo().GetDeclaredMethod(methodName);
#else
            MethodInfo m = this._interfaceType.GetMethod(methodName);
#endif
            if (m == null)
            {
                throw new MissingMemberException(string.Format(CultureInfo.CurrentCulture, "Resource.WebDomainClient_OperationDoesNotExist", _operationName));
            }
            return m;
        }

        private MethodInfo ResolveEndMethod()
        {
            var methodName = "End" + _operationName;
#if REFLECTION_V2
            MethodInfo m = this._interfaceType.GetTypeInfo().GetDeclaredMethod(methodName);
#else
            MethodInfo m = this._interfaceType.GetMethod(methodName);
#endif
            if (m == null)
            {
                throw new MissingMemberException(string.Format(CultureInfo.CurrentCulture, "Resource.WebDomainClient_OperationDoesNotExist", _operationName));
            }
            return m;
        }
    }
}
