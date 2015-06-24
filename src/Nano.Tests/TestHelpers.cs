using System;
using System.IO;
using System.Linq;
using System.Net;
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
    /// <see cref="HttpWebResponse"/> extensions.
    /// </summary>
    public static class HttpWebResponseExtensions
    {
        /// <summary>
        /// Gets the response as a string.
        /// </summary>
        /// <param name="httpWebResponse">The HTTP web response.</param>
        /// <returns>The response as a string.</returns>
        public static string GetResponseString( this HttpWebResponse httpWebResponse )
        {
            using ( var stream = httpWebResponse.GetResponseStream() )
            {
                if ( stream != null )
                {
                    using ( var sr = new StreamReader( stream ) )
                    {
                        return sr.ReadToEnd();
                    }
                }
            }

            return null;
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
        /// <param name="url">The URL.</param>
        /// <param name="allowAutoRedirect">if set to <c>true</c> [allow automatic redirect].</param>
        /// <returns>HTTP response as a string.</returns>
        public static string GetResponseString( string url, bool allowAutoRedirect = true )
        {
            return GetHttpWebResponse( url, allowAutoRedirect ).GetResponseString();
        }

        /// <summary>
        /// Gets the response as a <see cref="HttpWebResponse"/>.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="allowAutoRedirect">if set to <c>true</c> [allow automatic redirect].</param>
        /// <returns>Response as a <see cref="HttpWebResponse"/>.</returns>
        public static HttpWebResponse GetHttpWebResponse( string url, bool allowAutoRedirect = true )
        {
            var request = WebRequest.Create( url ) as HttpWebRequest;
            request.AllowAutoRedirect = allowAutoRedirect;

            try
            {
                return ( HttpWebResponse ) request.GetResponse();
            }
            catch( WebException we )
            {
                var response = we.Response as HttpWebResponse;

                if( response == null )
                    throw;

                return response;
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
