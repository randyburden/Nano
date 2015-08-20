using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Nano.Web.Core;
using Nano.Web.Core.Host.HttpListener;

namespace Nano.Tests
{
    /// <summary>
    /// Nano test server.
    /// </summary>
    public static class NanoTestServer
    {
        /// <summary>
        /// Creates an <see cref="HttpListenerNanoServer"/> with defaults to help write small unit tests.
        /// </summary>
        /// <returns></returns>
        public static HttpListenerNanoServer Start()
        {
            var nanoConfiguration = new NanoConfiguration();
            const string url = "http://localhost:4545/";
            return HttpListenerNanoServer.Start( nanoConfiguration, url );
        }
    }

    /// <summary>
    /// <see cref="HttpListenerNanoServer"/> extensions.
    /// </summary>
    public static class HttpListenerNanoServerExtensions
    {
        /// <summary>
        /// Gets the URL used to start the <see cref="HttpListenerNanoServer"/>.
        /// </summary>
        /// <param name="httpListenerNanoServer">The HTTP listener nano server.</param>
        /// <returns>URL.</returns>
        public static string GetUrl( this HttpListenerNanoServer httpListenerNanoServer )
        {
            return httpListenerNanoServer.HttpListenerConfiguration.HttpListener.Prefixes.FirstOrDefault();
        }
    }

    /// <summary>
    /// Http helpers.
    /// </summary>
    public static class HttpHelper
    {
        /// <summary>
        /// Encodes the URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>Encoded url.</returns>
        public static string EncodeUrl( string url )
        {
            return System.Uri.EscapeDataString( url ).Replace( "%20", "+" );
        }

        /// <summary>
        /// Gets the response as a string.
        /// </summary>
        /// <param name="httpWebResponse">The HTTP web response.</param>
        /// <returns>The response as a string.</returns>
        public static string GetResponseString( this HttpWebResponse httpWebResponse )
        {
            using( var stream = httpWebResponse.GetResponseStream() )
            {
                if( stream != null )
                {
                    using( var sr = new StreamReader( stream ) )
                    {
                        return sr.ReadToEnd();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the response as a string.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="allowAutoRedirect">if set to <c>true</c> [allow automatic redirect].</param>
        /// <returns>HTTP response as a string.</returns>
        public static string GetResponseString( string url, bool allowAutoRedirect = true )
        {
            using ( var response = GetHttpWebResponse( url, allowAutoRedirect ) )
            {
                return response.GetResponseString();
            }
        }

        /// <summary>
        /// Gets the response as a <see cref="HttpWebResponse"/>.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="allowAutoRedirect">if set to <c>true</c> [allow automatic redirect].</param>
        /// <returns>Response as a <see cref="HttpWebResponse"/>.</returns>
        public static HttpWebResponse GetHttpWebResponse( string url, bool allowAutoRedirect = true )
        {
            var request = GetHttpWebRequest( url, allowAutoRedirect );
            return request.GetHttpWebResponse();
        }

        /// <summary>
        /// Gets the HTTP web request.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="allowAutoRedirect">if set to <c>true</c> [allow automatic redirect].</param>
        /// <returns><see cref="HttpWebRequest"/>.</returns>
        public static HttpWebRequest GetHttpWebRequest( string url, bool allowAutoRedirect = true )
        {
            var request = WebRequest.Create( url ) as HttpWebRequest;
            request.AllowAutoRedirect = allowAutoRedirect;
            return request;
        }

        /// <summary>
        /// Gets the response as a <see cref="HttpWebResponse"/>.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Response as a <see cref="HttpWebResponse"/>.</returns>
        public static HttpWebResponse GetHttpWebResponse( this HttpWebRequest request )
        {
            try
            {
                return ( HttpWebResponse ) request.GetResponse();
            }
            catch ( WebException we )
            {
                var response = we.Response as HttpWebResponse;

                if ( response == null )
                    throw;

                return response;
            }
        }

        /// <summary>
        /// Posts JSON to a URL.
        /// </summary>
        /// <param name="url">Url to post to.</param>
        /// <param name="json">JSON to post.</param>
        /// <returns>JSON response.</returns>
        public static string PostJson( string url, string json )
        {
            using( var webClient = new WebClient() )
            {
                webClient.Headers.Add( HttpRequestHeader.ContentType, "application/json" );

                webClient.Encoding = Encoding.UTF8;

                byte[] data = Encoding.UTF8.GetBytes( json );

                var result = webClient.UploadData( new Uri( url ), "POST", data );

                string responseString = Encoding.UTF8.GetString( result );

                return responseString;
            }
        }

        /// <summary>
        /// Posts the specified URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="nameValueCollection">The name value collection to send to the server.</param>
        /// <returns>Response string</returns>
        public static string Post( string url, NameValueCollection nameValueCollection )
        {
            using( var client = new WebClient() )
            {
                byte[] responsebytes = client.UploadValues( url, "POST", nameValueCollection );
                string responsebody = Encoding.UTF8.GetString( responsebytes );
                return responsebody;
            }
        }
    }

    /// <summary>
    /// TimeSpan Helper.
    /// </summary>
    public static class TimeSpanHelper
    {
        /// <summary>
        /// Returns a formatted string of the given timespan.
        /// </summary>
        /// <remarks>
        /// Supports up to microsecond resolution.
        /// </remarks>
        /// <example>
        /// 1 days 6 hours 52 min 34 sec 556 ms
        /// </example>
        /// <returns>A string with a customized output of the elapsed time.</returns>
        public static string GetFormattedTime( this TimeSpan timeSpan )
        {
            string elapsedTime;

            if( timeSpan.Days > 0 )
                elapsedTime = string.Format( "{0:%d} d {0:%h} hrs {0:%m} min {0:%s} sec {0:%fff} ms", timeSpan );
            else if( timeSpan.Hours > 0 )
                elapsedTime = string.Format( "{0:%h} hrs {0:%m} min {0:%s} sec {0:%fff} ms", timeSpan );
            else if( timeSpan.Minutes > 0 )
                elapsedTime = string.Format( "{0:%m} min {0:%s} sec {0:%fff} ms", timeSpan );
            else if( timeSpan.Seconds > 0 )
                elapsedTime = string.Format( "{0:%s} sec {0:%fff} ms", timeSpan );
            else if( timeSpan.TotalMilliseconds > 0.9999999 )
                elapsedTime = string.Format( "{0} ms", timeSpan.TotalMilliseconds );
            else
                elapsedTime = string.Format( "{0} µs", timeSpan.TotalMilliseconds * 1000.0 );

            return elapsedTime;
        }
    }
}
