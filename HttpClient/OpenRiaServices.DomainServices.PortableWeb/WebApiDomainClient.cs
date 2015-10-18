using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace OpenRiaServices.DomainServices.Client.PortableWeb
{

    public class WebApiDomainClient : OpenRiaServices.DomainServices.Client.DomainClient
    {
        private static readonly Dictionary<Type, Dictionary<Type, DataContractSerializer>> s_globalSerializerCache = new Dictionary<Type, Dictionary<Type, DataContractSerializer>>();
        private static readonly DataContractSerializer s_faultSerializer = new DataContractSerializer(typeof(DomainServiceFault));
        Dictionary<Type, DataContractSerializer> _serializerCache;

        Type _serviceInterface;
        HttpClient _httpClientCache;

        public WebApiDomainClient(Type serviceInterface, Uri baseUri, HttpClientHandler handler)
        {
            _serviceInterface = serviceInterface;
            _httpClientCache = new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri(baseUri.AbsoluteUri + "/binary/", UriKind.Absolute),
            };

            lock (s_globalSerializerCache)
            {
                if (!s_globalSerializerCache.TryGetValue(serviceInterface, out _serializerCache))
                {
                    _serializerCache = new Dictionary<Type, DataContractSerializer>();
                    s_globalSerializerCache.Add(serviceInterface, _serializerCache);
                }
            }
        }

        internal Type ServiceInterfaceType { get { return _serviceInterface; } }

        HttpClient HttpClient
        {
            get
            {
                return _httpClientCache;
            }
        }

        #region Begin*** Methods
        /// <summary>
        /// Method called by the framework to begin an Invoke operation asynchronously. Overrides
        /// should not call the base method.
        /// </summary>
        /// <param name="invokeArgs">The arguments to the Invoke operation.</param>
        /// <param name="callback">The callback to invoke when the invocation has been completed.</param>
        /// <param name="userState">Optional user state associated with this operation.</param>
        /// <returns>
        /// An asynchronous result that identifies this invocation.
        /// </returns>
        protected override IAsyncResult BeginInvokeCore(InvokeArgs invokeArgs, AsyncCallback callback, object userState)
        {
            var result = WebApiDomainClientAsyncResult.CreateInvokeResult(this, invokeArgs, callback, userState);

            return BeginWebRequest(result, invokeArgs.HasSideEffects, invokeArgs.Parameters, queryOptions: null);
        }

        /// <summary>
        /// Method called by the framework to asynchronously process the specified <see cref="T:OpenRiaServices.DomainServices.Client.EntityChangeSet" />.
        /// Overrides should not call the base method.
        /// </summary>
        /// <param name="changeSet">The <see cref="T:OpenRiaServices.DomainServices.Client.EntityChangeSet" /> to submit to the DomainService.</param>
        /// <param name="callback">The callback to invoke when the submit has been executed.</param>
        /// <param name="userState">Optional user state associated with this operation.</param>
        /// <returns>
        /// An asynchronous result that identifies this submit request.
        /// </returns>
        protected override IAsyncResult BeginSubmitCore(EntityChangeSet changeSet, AsyncCallback callback, object userState)
        {
            var result = WebApiDomainClientAsyncResult.CreateSubmitResult(this, changeSet, changeSet.GetChangeSetEntries().ToList(), callback, userState);

            var parameters = new Dictionary<string, object>() {
                {"changeSet", result.ChangeSetEntries}
            };

            return BeginWebRequest(result, hasSideEffects: true, parameters: parameters, queryOptions: null);
        }

        /// <summary>
        /// Method called by the framework to begin the asynchronous query operation.
        /// </summary>
        /// <param name="query">The query to invoke.</param>
        /// <param name="callback">The callback to invoke when the query has been executed.</param>
        /// <param name="userState">Optional user state associated with this operation.</param>
        /// <returns>
        /// An asynchronous result that identifies this query.
        /// </returns>
        protected override IAsyncResult BeginQueryCore(EntityQuery query, AsyncCallback callback, object userState)
        {
            var result = WebApiDomainClientAsyncResult.CreateQueryResult(this, query, callback, userState);
            List<ServiceQueryPart> queryOptions = query.Query != null ? QuerySerializer.Serialize(query.Query) : null;

            if (query.IncludeTotalCount)
            {
                queryOptions = queryOptions ?? new List<ServiceQueryPart>();
                queryOptions.Add(new ServiceQueryPart()
                {
                    QueryOperator = "includeTotalCount",
                    Expression = "True"
                });
            }

            return BeginWebRequest(result, query.HasSideEffects, query.Parameters, queryOptions);
        }
        #endregion

        #region End*** Methods
        /// <summary>
        /// Gets the result of a Query operation.
        /// </summary>
        /// <param name="asyncResult">An asynchronous result that identifies a query.</param>
        /// <returns>The results returned by the query.</returns>
        protected override QueryCompletedResult EndQueryCore(IAsyncResult asyncResult)
        {
            var result = this.EndAsyncResult(asyncResult, AsyncOperationType.Query, /* cancel */ false);
            IEnumerable<ValidationResult> validationErrors = null;

            try
            {
                var queryResult = (QueryResult)ReadResponse(result, result.EndOperationMethod.ReturnType);
                if (queryResult != null)
                {
                    return new QueryCompletedResult(
                        queryResult.GetRootResults().Cast<Entity>(),
                        queryResult.GetIncludedResults().Cast<Entity>(),
                        queryResult.TotalCount,
                        Enumerable.Empty<ValidationResult>());
                }
            }
            catch (FaultException<DomainServiceFault> fe)
            {
                if (fe.Detail.OperationErrors != null)
                {
                    validationErrors = fe.Detail.GetValidationErrors();
                }
                else
                {
                    throw GetExceptionFromServiceFault(fe.Detail);
                }
            }

            return new QueryCompletedResult(
                    Enumerable.Empty<Entity>(),
                    Enumerable.Empty<Entity>(),
                /* totalCount */ 0,
                    validationErrors ?? Enumerable.Empty<ValidationResult>());
        }

        /// <summary>
        /// Gets the result of an Invoke operation.
        /// </summary>
        /// <param name="asyncResult">An asynchronous result that identifies an invocation.</param>
        /// <returns>The results returned by the invocation.</returns>
        protected override InvokeCompletedResult EndInvokeCore(IAsyncResult asyncResult)
        {
            var result = this.EndAsyncResult(asyncResult, AsyncOperationType.Invoke, /* cancel */ false);
            IEnumerable<ValidationResult> validationErrors = null;
            object returnValue = null;

            try
            {
                returnValue = ReadResponse(result, result.EndOperationMethod.ReturnType);
            }
            catch (FaultException<DomainServiceFault> fe)
            {
                if (fe.Detail.OperationErrors != null)
                {
                    validationErrors = fe.Detail.GetValidationErrors();
                }
                else
                {
                    throw GetExceptionFromServiceFault(fe.Detail);
                }
            }

            return new InvokeCompletedResult(returnValue, validationErrors ?? Enumerable.Empty<ValidationResult>());
        }

        /// <summary>
        /// Gets the result of a Submit operation.
        /// </summary>
        /// <param name="asyncResult">An asynchronous result that identifies an invocation.</param>
        /// <returns>The results returned by the invocation.</returns>
        protected override SubmitCompletedResult EndSubmitCore(IAsyncResult asyncResult)
        {
            var result = this.EndAsyncResult(asyncResult, AsyncOperationType.Submit, /* cancel */ false);
            var changeSet = result.EntityChangeSet;

            try
            {
                var returnValue = (IEnumerable<ChangeSetEntry>)ReadResponse(result, typeof(IEnumerable<ChangeSetEntry>));
                return new SubmitCompletedResult(changeSet, returnValue ?? Enumerable.Empty<ChangeSetEntry>());
            }
            catch (FaultException<DomainServiceFault> fe)
            {
                throw GetExceptionFromServiceFault(fe.Detail);
            }
        }
        #endregion

        #region Private methods for making requests
        /// <summary>
        /// Invokes a web request for the operation defined by the <see cref="WebApiDomainClientAsyncResult"/>
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="hasSideEffects">if set to <c>true</c> then the request will always be a POST operation.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="queryOptions">The query options.</param>
        private IAsyncResult BeginWebRequest(WebApiDomainClientAsyncResult result, bool hasSideEffects, IDictionary<string, object> parameters,
            IList<ServiceQueryPart> queryOptions)
        {
            Task<HttpResponseMessage> response = null;
            // Add parameters to query string for get methods
            if (!hasSideEffects)
            {
                response = GetAsync(result, parameters, queryOptions);
            }
            // It is a POST
            if (response == null)
            {
                response = PostAsync(result, parameters, queryOptions);
            }

            result.InnerAsyncResult = response;
            response.ContinueWith(task =>
            {
                result.Complete();
            }, TaskContinuationOptions.ExecuteSynchronously);

            return result;
        }

        /// <summary>
        /// Initiates a POST request for the given operation and return the server respose (as a task).
        /// </summary>
        /// <param name="result">The result object which contains information about which operation was performed.</param>
        /// <param name="parameters">The parameters to the server method, or <c>null</c> if no parameters.</param>
        /// <param name="queryOptions">The query options if any.</param>
        /// <returns></returns>
        private Task<HttpResponseMessage> PostAsync(WebApiDomainClientAsyncResult result, IDictionary<string, object> parameters, IList<ServiceQueryPart> queryOptions)
        {
            var ms = new System.IO.MemoryStream();
            var writer = System.Xml.XmlDictionaryWriter.CreateBinaryWriter(ms);

            // Write message
            {
                var rootNamespace = "http://tempuri.org/";
                bool hasQueryOptions = (queryOptions != null && queryOptions.Count > 0);

                if (hasQueryOptions)
                {
                    writer.WriteStartElement("MessageRoot");
                    writer.WriteStartElement("QueryOptions");
                    foreach (var queryOption in queryOptions)
                    {
                        writer.WriteStartElement("QueryOption");
                        writer.WriteAttributeString("Name", queryOption.QueryOperator);
                        writer.WriteAttributeString("Value", queryOption.Expression);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                writer.WriteStartElement(result.OperationName, rootNamespace); // <OperationName>

                // Write all parameters
                if (parameters != null && parameters.Count > 0)
                {
                    foreach (var param in parameters)
                    {
                        writer.WriteStartElement(param.Key);  // <ParameterName>
                        if (param.Value != null)
                        {
                            var serializer = GetSerializer(param.Value.GetType());
                            serializer.WriteObjectContent(writer, param.Value);
                        } 
                        else
                        {
                            // Null input
                            writer.WriteAttributeString("i","nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
                        }
                        writer.WriteEndElement();            // </ParameterName>
                    }
                }

                writer.WriteEndDocument(); // </OperationName> and </MessageRoot> if present
                writer.Flush();
            }

            // TODO: Custom optimized implementation of StreamContent (like WebApi's PushStreamContent)?
            // so we don't have to have special dispose logic below
            ms.Seek(0, System.IO.SeekOrigin.Begin);
            var content = new StreamContent(ms);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/msbin1");

            // Keep reference to dictionary so that we can dispose of it correctly after the request has been posted
            // otherwise there is a small risk that it will be finalized and that it might corrupt the stream
            return HttpClient.PostAsync(result.OperationName, content)
                .ContinueWith(res =>
                {
                    writer.Dispose();

                    return res;
                })
                .Unwrap();
        }

        /// <summary>
        /// Initiates a GET request for the given operation and return the server respose (as a task).
        /// </summary>
        /// <param name="result">The result object which contains information about which operation was performed.</param>
        /// <param name="parameters">The parameters to the server method, or <c>null</c> if no parameters.</param>
        /// <param name="queryOptions">The query options if any.</param>
        /// <returns></returns>
        private Task<HttpResponseMessage> GetAsync(WebApiDomainClientAsyncResult result, IDictionary<string, object> parameters, IList<ServiceQueryPart> queryOptions)
        {
            int i = 0;
            var uriBuilder = new StringBuilder();
            uriBuilder.Append(result.OperationName);

            // Parameters
            if (parameters != null && parameters.Count > 0)
            {
                foreach (var param in parameters)
                {
                    uriBuilder.Append(i++ == 0 ? '?' : '&');
                    uriBuilder.Append(Uri.EscapeDataString(param.Key));
                    uriBuilder.Append("=");
                    if (param.Value != null)
                    {
                        var value = QueryStringConverter.ConvertValueToString(param.Value, param.Value.GetType());
                        uriBuilder.Append(Uri.EscapeDataString(value));
                    }
                }
            }

            // Query options
            if (queryOptions != null && queryOptions.Count > 0)
            {
                foreach (var queryPart in queryOptions)
                {
                    uriBuilder.Append(i++ == 0 ? "?$" : "&$");
                    uriBuilder.Append(queryPart.QueryOperator);
                    uriBuilder.Append("=");
                    uriBuilder.Append(Uri.EscapeDataString(queryPart.Expression));
                }
            }

            // TODO: Switch to POST if uri becomes to long, we can do so by returning nul ...l
            var uri = uriBuilder.ToString();
            return HttpClient.GetAsync(uri);
        }
        #endregion

        #region Private methods for reading responses

        /// <summary>
        /// Reads a response from the service and converts it to the specified return type.
        /// </summary>
        /// <param name="result">The result object which contains information about which operation was performed.</param>
        /// <param name="returnType">Type which should be returned.</param>
        /// <returns></returns>
        /// <exception cref="OpenRiaServices.DomainServices.Client.DomainOperationException">On server errors which did not produce expected output</exception>
        /// <exception cref="FaultException{DomainServiceFault}">If server returned a DomainServiceFault</exception>
        private object ReadResponse(WebApiDomainClientAsyncResult result, Type returnType)
        {
            HttpResponseMessage response = ((Task<HttpResponseMessage>)result.InnerAsyncResult).Result;

            if (!response.IsSuccessStatusCode)
            {
                var message = string.Format(Resources.DomainClient_UnexpectedHttpStatusCode, (int)response.StatusCode, response.StatusCode);

                if (response.StatusCode == HttpStatusCode.BadRequest)
                    throw new DomainOperationException(message, OperationErrorStatus.NotSupported, (int)response.StatusCode, null);
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                    throw new DomainOperationException(message, OperationErrorStatus.Unauthorized, (int)response.StatusCode, null);
                else
                    throw new DomainOperationException(message, OperationErrorStatus.ServerError, (int)response.StatusCode, null);
            }

            var ms = response.Content.ReadAsStreamAsync().Result;
            using (var reader = System.Xml.XmlDictionaryReader.CreateBinaryReader(ms, System.Xml.XmlDictionaryReaderQuotas.Max))
            {
                reader.Read();

                // Domain Fault
                if (reader.LocalName == "Fault")
                {
                    throw ReadFaultException(reader, result.OperationName);
                }
                else
                {
                    // Validate that we are no on ****Response node
                    VerifyReaderIsAtNode(reader, result.OperationName, "Response");
                    reader.ReadStartElement(); // Read to next which should be ****Result

                    // Validate that we are no on ****Result node
                    VerifyReaderIsAtNode(reader, result.OperationName, "Result");

                    var serializer = GetSerializer(returnType);
                    return serializer.ReadObject(reader, verifyObjectName: false);
                }
            }
        }

        /// <summary>
        /// Transitions an <see cref="IAsyncResult"/> instance to a completed state.
        /// </summary>
        /// <param name="asyncResult">An asynchronous result that identifies an invocation.</param>
        /// <param name="operationType">The expected operation type.</param>
        /// <param name="cancel">Boolean indicating whether or not the operation has been canceled.</param>
        /// <returns>A <see cref="WebDomainClientAsyncResult&lt;TContract&gt;"/> reference.</returns>
        /// <exception cref="ArgumentNullException"> if <paramref name="asyncResult"/> is null.</exception>
        /// <exception cref="ArgumentException"> if <paramref name="asyncResult"/> is not of type <cref name="TAsyncResult"/>.</exception>
        /// <exception cref="InvalidOperationException"> if <paramref name="asyncResult"/> has been canceled.</exception>
        /// <exception cref="InvalidOperationException"> if <paramref name="asyncResult"/>'s End* method has already been invoked.</exception>
        /// <exception cref="InvalidOperationException"> if <paramref name="asyncResult"/> has not completed.</exception>
        private WebApiDomainClientAsyncResult EndAsyncResult(IAsyncResult asyncResult, AsyncOperationType operationType, bool cancel)
        {
            var clientResult = asyncResult as WebApiDomainClientAsyncResult;
            if ((clientResult != null) && (!object.ReferenceEquals(this, clientResult.DomainClient) || clientResult.AsyncOperationType != operationType))
            {
                throw new ArgumentException(Resources.WrongAsyncResult, "asyncResult");
            }

            return AsyncResultBase.EndAsyncOperation<WebApiDomainClientAsyncResult>(asyncResult, cancel);
        }

        /// <summary>
        /// Verifies the reader is at node with LocalName equal to operationName + postfix.
        /// If the reader is at any other node, then a <see cref="DomainOperationException is thrown"/>
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="operationName">Name of the operation.</param>
        /// <param name="postfix">The postfix.</param>
        /// <exception cref="OpenRiaServices.DomainServices.Client.DomainOperationException">If reader is not at the expected xml element</exception>
        private static void VerifyReaderIsAtNode(System.Xml.XmlDictionaryReader reader, string operationName, string postfix)
        {
            // localName should be operationName + postfix
            if (!(reader.LocalName.Length == operationName.Length + postfix.Length
                && reader.LocalName.StartsWith(operationName)
                && reader.LocalName.EndsWith(postfix)))
            {
                throw new DomainOperationException(
                    string.Format(Resources.DomainClient_UnexpectedResultContent, operationName + postfix, reader.LocalName)
                    , OperationErrorStatus.ServerError, 0, null);
            }
        }

        /// <summary>
        /// Constructs an exception based on a service fault.
        /// </summary>
        /// <param name="serviceFault">The fault received from a service.</param>
        /// <returns>The constructed exception.</returns>
        private static Exception GetExceptionFromServiceFault(DomainServiceFault serviceFault)
        {
            // Status was OK but there still was a server error. We need to transform
            // the error into the appropriate client exception
            if (serviceFault.IsDomainException)
            {
                return new DomainException(serviceFault.ErrorMessage, serviceFault.ErrorCode, serviceFault.StackTrace);
            }
            else if (serviceFault.ErrorCode == 400)
            {
                return new DomainOperationException(serviceFault.ErrorMessage, OperationErrorStatus.NotSupported, serviceFault.ErrorCode, serviceFault.StackTrace);
            }
            else if (serviceFault.ErrorCode == 401)
            {
                return new DomainOperationException(serviceFault.ErrorMessage, OperationErrorStatus.Unauthorized, serviceFault.ErrorCode, serviceFault.StackTrace);
            }
            else
            {
                // for anything else: map to ServerError
                return new DomainOperationException(serviceFault.ErrorMessage, OperationErrorStatus.ServerError, serviceFault.ErrorCode, serviceFault.StackTrace);
            }
        }

        /// <summary>
        /// Reads a Fault reply from the service.
        /// </summary>
        /// <param name="reader">The reader, which should start at the "Fault" element.</param>
        /// <param name="operationName">Name of the operation.</param>
        /// <returns>A FaultException with the details in the server reply</returns>
        private static FaultException<DomainServiceFault> ReadFaultException(System.Xml.XmlDictionaryReader reader, string operationName)
        {
            FaultCode faultCode;
            FaultReason faultReason;
            List<FaultReasonText> faultReasons = new List<FaultReasonText>();

            reader.ReadStartElement("Fault"); // <Fault>
            reader.ReadStartElement("Code");  // <Code>
            reader.ReadStartElement("Value"); // <Value>
            faultCode = new FaultCode(reader.ReadContentAsString());
            reader.ReadEndElement(); // </Value>
            reader.ReadEndElement(); // </Code>

            reader.ReadStartElement("Reason");
            while (reader.LocalName == "Text")
            {
                var lang = reader.XmlLang;
                reader.ReadStartElement("Text");
                var text = reader.ReadContentAsString();
                reader.ReadEndElement();

                faultReasons.Add(new FaultReasonText(text, lang));
            }
            reader.ReadEndElement(); // </Reason>
            faultReason = new FaultReason(faultReasons);

            reader.ReadStartElement("Detail"); // <Detail>
            var fault = (DomainServiceFault)s_faultSerializer.ReadObject(reader);
            return new FaultException<DomainServiceFault>(fault, faultReason, faultCode, operationName);
        }
        #endregion

        #region Serialization helpers

        /// <summary>
        /// Gets a <see cref="DataContractSerializer"/> which can be used to serialized the specified type.
        /// The serializers are cached for performance reasons.
        /// </summary>
        /// <param name="type">type which should be serializable.</param>
        /// <returns>A <see cref="DataContractSerializer"/> which can be used to serialize the type</returns>
        private DataContractSerializer GetSerializer(Type type)
        {
            DataContractSerializer serializer;
            lock (_serializerCache)
            {
                if (!_serializerCache.TryGetValue(type, out serializer))
                {
                    // TODO: ENsure that DateTimeOffset is part of known types 
                    // Unlike other primitive types, the DateTimeOffset structure is not a known type by default, so it must be manually added to the list of known types.
                    serializer = new DataContractSerializer(type, EntityTypes);
                    _serializerCache.Add(type, serializer);
                }
            }

            return serializer;
        }
        #endregion
    }
}
