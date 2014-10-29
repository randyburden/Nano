using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nano.Core
{
    /// <summary>
    /// NanoRequest representing an incoming HTTP request.
    /// </summary>
    /// <remarks>
    /// This wraps OWIN environment dictionary and provides strongly typed accessors.
    /// </remarks>
    public class NanoRequest
    {
        /// <summary>
        /// Constructs a new NanoRequest.
        /// </summary>
        /// <param name="nanoContext">The requests context.</param>
        public NanoRequest( NanoContext nanoContext )
        {
            NanoContext = nanoContext;
            QueryStringParameters = new Dictionary<string, string>();
            FormBodyParameters = new Dictionary<string, string>();
            HeaderParameters = new Dictionary<string, string>();
            MethodParameters = new List<MethodParameter>();
        }
        
        /// <summary>
        /// The requests context.
        /// </summary>
        public NanoContext NanoContext;

        /// <summary>
        /// HTTP Method.
        /// </summary>
        public string HttpMethod
        {
            get { return NanoContext.Get<string>( OwinConstants.RequestMethod ); }
            set { NanoContext.Set( OwinConstants.RequestMethod, value ); }
        }

        /// <summary>
        /// Full URI being requested.
        /// </summary>
        public Uri Uri
        {
            get { return NanoContext.Get<Uri>( NanoConstants.Uri ); }
            set { NanoContext.Set( NanoConstants.Uri, value ); }
        }

        /// <summary>
        /// Query string parameters.
        /// </summary>
        public Dictionary<string, string> QueryStringParameters
        {
            get { return NanoContext.Get<Dictionary<string, string>>( NanoConstants.QueryStringParameters ); }
            set { NanoContext.Set( NanoConstants.QueryStringParameters, value ); }
        }

        /// <summary>
        /// Form body parameters.
        /// </summary>
        public Dictionary<string, string> FormBodyParameters
        {
            get { return NanoContext.Get<Dictionary<string, string>>( NanoConstants.FormBodyParameters ); }
            set { NanoContext.Set( NanoConstants.FormBodyParameters, value ); }
        }

        /// <summary>
        /// Header parameters.
        /// </summary>
        public Dictionary<string, string> HeaderParameters
        {
            get { return NanoContext.Get<Dictionary<string, string>>( NanoConstants.HeaderParameters ); }
            set { NanoContext.Set( NanoConstants.HeaderParameters, value ); }
        }
        
        /// <summary>
        /// Method name being invoked.
        /// </summary>
        public string MethodName
        {
            get { return NanoContext.Get<string>( NanoConstants.MethodName ); }
            set { NanoContext.Set( NanoConstants.MethodName, value ); }
        }

        /// <summary>
        /// User defined web path being invoked.
        /// </summary>
        public string WebPath
        {
            get { return NanoContext.Get<string>( NanoConstants.WebPath ); }
            set { NanoContext.Set( NanoConstants.WebPath, value ); }
        }

        /// <summary>
        /// User defined type being invoked.
        /// </summary>
        public Type ApiType
        {
            get { return NanoContext.Get<Type>( NanoConstants.ApiType ); }
            set { NanoContext.Set( NanoConstants.ApiType, value ); }
        }

        /// <summary>
        /// MethodInfo for the method being invoked.
        /// </summary>
        public MethodInfo MethodInfo
        {
            get { return NanoContext.Get<MethodInfo>( NanoConstants.MethodInfo ); }
            set { NanoContext.Set( NanoConstants.MethodInfo, value ); }
        }

        /// <summary>
        /// Method invocation parameters.
        /// </summary>
        public List<MethodParameter> MethodParameters
        {
            get { return NanoContext.Get<List<MethodParameter>>( NanoConstants.MethodParameters ); }
            set { NanoContext.Set( NanoConstants.MethodParameters, value ); }
        }

        /// <summary>
        /// Method invocation parameter.
        /// </summary>
        public class MethodParameter
        {
            /// <summary>
            /// ParameterInfo for one of the parameters on the method being invoked.
            /// </summary>
            public ParameterInfo ParameterInfo;

            /// <summary>
            /// Parameter name.
            /// </summary>
            public string ParameterName;

            /// <summary>
            /// Paramter type.
            /// </summary>
            public Type ParameterType;

            /// <summary>
            /// Indicates if the parameter type is dynamic.
            /// </summary>
            public bool IsDynamic;
            
            /// <summary>
            /// Parameter value from the request.
            /// </summary>
            public string RequestParameterValue;
            
            /// <summary>
            /// The deserialized parameter value which will be passed to the method being invoked.
            /// </summary>
            public object MethodParameterValue;
        }

        /// <summary>
        /// Gets the combined request parameters from the form body, query string, and request headers.
        /// </summary>
        /// <returns>Dictionary of combined form body, query string, and request headers.</returns>
        public Dictionary<string, string> GetRequestParameters()
        {
            var queryParameters = new Dictionary<string, string>( QueryStringParameters, StringComparer.CurrentCultureIgnoreCase );

            queryParameters.Merge( FormBodyParameters );

            queryParameters.Merge( HeaderParameters );

            return queryParameters;
        }
    }
}