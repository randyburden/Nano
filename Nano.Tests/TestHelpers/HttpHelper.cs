using System.IO;
using System.Net;
using System.Text;

namespace Nano.Tests.TestHelpers
{
    /// <summary>
    /// Http Helper.
    /// </summary>
    public static class HttpHelper
    {
        /// <summary>
        /// Initiates a GET request against the given url and returns the response.
        /// </summary>
        /// <param name="url">Url.</param>
        /// <param name="headers">Optional headers.</param>
        /// <returns>Response from the GET request.</returns>
        public static string Get( string url, System.Collections.Specialized.NameValueCollection headers = null )
        {
            string response = null;

            using ( var webClient = new WebClient() )
            {
                if ( headers != null )
                    webClient.Headers.Add( headers );
                
                using ( var stream = webClient.OpenRead( url ) )
                {
                    if ( stream != null )
                    {
                        using ( var streamReader = new StreamReader( stream ) )
                        {
                            response = streamReader.ReadToEnd();
                        }
                    }
                }
            }

            return response;
        }

        /// <summary>
        /// Initiates a POST request against the given url with the supplied parameters and returns the response.
        /// </summary>
        /// <param name="url">Url.</param>
        /// <param name="formBodyParameters">Form body parameters.</param>
        /// <param name="headers">Optional headers.</param>
        /// <returns>Response from the POST request.</returns>
        public static string Post( string url, System.Collections.Specialized.NameValueCollection formBodyParameters, System.Collections.Specialized.NameValueCollection headers = null )
        {
            string response;

            using ( var webClient = new WebClient() )
            {
                if ( headers != null )
                    webClient.Headers.Add( headers );

                var responseBytes = webClient.UploadValues( url, "POST", formBodyParameters );

                response = Encoding.UTF8.GetString( responseBytes );
            }

            return response;
        }
    }
}