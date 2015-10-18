using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace OpenRiaServices.DomainServices.Client.PortableWeb
{
    /// <summary>
    /// This file is based on the OpenRiaServices.DomainServices.Client/Hosting.WebHttpQueryStringConverter 
    /// </summary>
    static class QueryStringConverter
    {
        public static string ConvertValueToString(object parameter, Type parameterType)
        {
            if (parameterType == null)
            {
                throw new ArgumentNullException("parameterType");
            }
            if (parameterType.IsValueType && parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            if (System.ServiceModel.Dispatcher.QueryStringConverter.CanConvert(parameterType))
            {
                return System.ServiceModel.Dispatcher.QueryStringConverter.ConvertValueToString(parameter, parameterType);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                new System.Runtime.Serialization.Json.DataContractJsonSerializer(parameterType).WriteObject(ms, parameter);
                byte[] result = ms.ToArray();
                string value = Encoding.UTF8.GetString(result, 0, result.Length);

                return Uri.EscapeDataString(value);
            }
        }
    }
}
