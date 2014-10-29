using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Owin;
using Nano.Core;

namespace Nano.Host
{
    /// <summary>
    /// OwinContext mapper.
    /// </summary>
    public static class OwinToNanoContextMapper
    {
        /// <summary>
        /// Maps an OwinContext to a NanoContext.
        /// </summary>
        /// <param name="owinContext">Current OwinContext.</param>
        /// <param name="webApi">WebApi being invoked.</param>
        /// <returns>The mapped NanoContext.</returns>
        public static NanoContext Map( this IOwinContext owinContext, WebApi webApi )
        {
            var nanoContext = new NanoContext( owinContext.Environment, webApi );

            try
            {
                nanoContext.Request.ApiType = webApi.ApiType;

                //nanoContext.Request.HttpMethod = owinContext.Request.Method;

                nanoContext.Request.Uri = owinContext.Request.Uri;

                nanoContext.Request.WebPath = webApi.WebPath;

                nanoContext.Request.MethodName = owinContext.Request.Uri.Segments.Last();

                if ( String.IsNullOrWhiteSpace( nanoContext.Request.MethodName ) || nanoContext.Request.MethodName.Contains( "." ) )
                    throw new Exception( "No method to invoke" ); // TODO: This should be a HTTP 404 NOT FOUND

                nanoContext.Request.MethodInfo = webApi.ApiType.GetMethod( nanoContext.Request.MethodName, BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy );

                if ( nanoContext.Request.MethodInfo == null )
                    throw new Exception( "Method not found: " + nanoContext.Request.MethodName ); // TODO: This should be a HTTP 404 NOT FOUND

                nanoContext.Request.QueryStringParameters = owinContext.Request.GetQueryParameters();

                nanoContext.Request.FormBodyParameters = owinContext.Request.GetBodyParameters();

                nanoContext.Request.HeaderParameters = owinContext.Request.GetHeaderParameters();

                // Gets the combined request parameters from the form body, query string, and request headers.
                var requestParameters = nanoContext.Request.GetRequestParameters();

                // Try to satisfy each method parameter by searching for the parameter name in the request parameters
                // and converting the value to the required type
                foreach ( var parameterInfo in nanoContext.Request.MethodInfo.GetParameters().OrderBy( x => x.Position ) )
                {
                    var methodParameter = new NanoRequest.MethodParameter();

                    methodParameter.ParameterInfo = parameterInfo;

                    methodParameter.ParameterName = parameterInfo.Name;

                    methodParameter.ParameterType = parameterInfo.ParameterType;

                    methodParameter.IsDynamic = parameterInfo.IsDynamic();

                    // Handle NanoContext parameters by injecting the current NanoContext as the parameter value
                    if ( methodParameter.ParameterType == typeof ( NanoContext ) )
                    {
                        methodParameter.MethodParameterValue = nanoContext;

                        nanoContext.Request.MethodParameters.Add( methodParameter );

                        continue;
                    }

                    // Try to get the method parameter value from the request parameters
                    requestParameters.TryGetValue( methodParameter.ParameterName, out methodParameter.RequestParameterValue );

                    // If the method parameter value is null or could not be found in the request parameters
                    if ( String.IsNullOrWhiteSpace( methodParameter.RequestParameterValue ) )
                    {
                        // Handle optional parameters
                        if ( parameterInfo.IsOptional )
                        {
                            methodParameter.MethodParameterValue = Type.Missing;

                            continue;
                        }

                        // Handle nullable parameters
                        if ( Nullable.GetUnderlyingType( parameterInfo.ParameterType ) != null )
                        {
                            methodParameter.MethodParameterValue = null;

                            continue;
                        }

                        string errorMessage = String.Format( "The form body, query string, and header parameters do not contain a parameter named '{0}' which is a required parameter for method '{1}'", methodParameter.ParameterName, nanoContext.Request.MethodName );

                        throw new Exception( errorMessage ); // TODO: This should be a HTTP 422 UNPROCESSABLE ENTITY or a 400 BAD REQUEST
                    }

                    try
                    {
                        var underlyingType = Nullable.GetUnderlyingType( methodParameter.ParameterType ) ?? methodParameter.ParameterType;

                        // Try to convert the request parameter value to the method parameter values type
                        // Note we are currently leveraging Json.Net to handle the heavy load of type conversions
                        if ( JsonHelpers.TryParseJson( methodParameter.RequestParameterValue, underlyingType, methodParameter.IsDynamic, out methodParameter.MethodParameterValue ) == false )
                            throw new Exception( "Type conversion error" );
                    }
                    catch ( Exception )
                    {
                        string errorMessage = String.Format( "An error occurred converting the parameter named '{0}' and value '{1}' to type {2} which is a required parameter for method '{3}'", methodParameter.ParameterName, methodParameter.RequestParameterValue, methodParameter.ParameterType,
                            nanoContext.Request.MethodName ); // TODO: This should be a HTTP 422 UNPROCESSABLE ENTITY or a 400 BAD REQUEST

                        throw new Exception( errorMessage );
                    }

                    nanoContext.Request.MethodParameters.Add( methodParameter );
                }
            }
            catch ( Exception ex )
            {
                nanoContext.Response.Error = new NanoError { ErrorMessage = ex.Message, Exception = ex };
            }

            return nanoContext;
        }

        /// <summary>
        /// Determines if the parameter is dynamic.
        /// </summary>
        /// <param name="parameterInfo">Parameter type.</param>
        /// <returns>True if dynamic.</returns>
        public static bool IsDynamic( this ParameterInfo parameterInfo )
        {
            return parameterInfo.GetCustomAttributes( typeof( DynamicAttribute ), true ).Length > 0;
        }
    }
}