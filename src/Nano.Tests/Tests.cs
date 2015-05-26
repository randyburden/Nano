using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Text;
using Nano.Web.Core;
using Nano.Web.Core.Host.HttpListener;
using NUnit.Framework;

namespace Nano.Tests
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void DoItToIt()
        {
            var nanoConfiguration = new NanoConfiguration();
            nanoConfiguration.AddMethods<Customer>();

            using( HttpListenerNanoServer.Start( nanoConfiguration, "http://localhost:4545" ) )
            {
                var parameters = new NameValueCollection { { "customerNbr", "1" } };

                using( var client = new WebClient() )
                {
                    byte[] responsebytes = client.UploadValues( "http://localhost:4545/Customer/GetCustomer", "POST", parameters );
                    string responsebody = Encoding.UTF8.GetString( responsebytes );
                    Trace.WriteLine( responsebody );
                }
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
}