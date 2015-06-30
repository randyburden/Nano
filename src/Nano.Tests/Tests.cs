using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Text;
using Nano.Demo;
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
                    byte[] responsebytes = client.UploadValues( "http://localhost:4545/api/Customer/GetCustomer", "POST", parameters );
                    string responsebody = Encoding.UTF8.GetString( responsebytes );
                    Trace.WriteLine( responsebody );
                }
            }
        }
    }
}