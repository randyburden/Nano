using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Nano.Web.Core;
using Nano.Web.Core.Host.HttpListener;
using NUnit.Framework;

namespace Nano.Tests.SpeedTests
{
    [TestFixture]
    public class MethodSpeedTest
    {
        public class Customer
        {
            public static CustomerModel CreateCustomer( string firstName, string lastName )
            {
                return new CustomerModel
                {
                    CustomerId = 1,
                    FirstName = firstName,
                    LastName = lastName
                };
            }

            public class CustomerModel
            {
                public int CustomerId;
                public string FirstName;
                public string LastName;
            }
        }

        [Explicit]
        [TestCase( 1 )]
        [TestCase( 100 )]
        [TestCase( 1000 )]
        [TestCase( 10000 )]
        [TestCase( 100000 )]
        public void SimpleSpeedTest( int requestCount )
        {
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;

            var nanoConfiguration = new NanoConfiguration();
            nanoConfiguration.AddMethods<Customer>();

            using( HttpListenerNanoServer.Start( nanoConfiguration, "http://localhost:4545" ) )
            {
                var parameters = new NameValueCollection { { "firstName", "Clark" }, { "lastName", "Kent" } };
                var stopwatch = Stopwatch.StartNew();

                Parallel.For( 0, requestCount, i =>
                {
                    using( var client = new WebClient() )
                    {
                        byte[] responsebytes = client.UploadValues( "http://localhost:4545/Customer/CreateCustomer", "POST", parameters );
                        string responsebody = Encoding.UTF8.GetString( responsebytes );
                        if( requestCount == 1 ) Trace.WriteLine( responsebody );
                    }
                } );

                var elapsedTime = stopwatch.Elapsed;
                Trace.WriteLine( string.Format( "{0} requests completed in {1}", requestCount, elapsedTime.GetFormattedTime() ) );
                var averageRequestTimeInMilliseconds = elapsedTime.TotalMilliseconds / requestCount;
                var averageRequestTimeSpan = TimeSpan.FromTicks( (long)( TimeSpan.TicksPerMillisecond * averageRequestTimeInMilliseconds ) );
                Trace.WriteLine( string.Format( "Average request time: {0}", averageRequestTimeSpan.GetFormattedTime() ) );
            }
        }
    }
}