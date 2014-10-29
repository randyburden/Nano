using System;
using System.IO;
using System.Text;
using Nano.Core;
using Nano.Host;
using Nano.Tests.TestHelpers;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Nano.Tests
{
    [TestFixture]
    public class NanoContextInjectedParameterTests
    {
        public string AddEmployeeUrl = "http://localhost/NanoTests/NanoContextInjectedParameterTestsApi/AddEmployee";

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var config = new NanoServerConfiguration( @"http://+:80/NanoTests/" );

            var webApi = config.AddWebApi<NanoContextInjectedParameterTestsApi>();

            webApi.ApiPreInvokeEvent.Add( context =>
            {
            } );

            webApi.ApiPostInvokeEvent.Add( context =>
            {
            } );

            webApi.ApiUnhandledExceptionEvent.Add( ( exception, context ) =>
            {
            } );

            NanoServer.Start( config );
        }

        /// <summary>
        /// Class being tested.
        /// </summary>
        public class NanoContextInjectedParameterTestsApi
        {
            /// <summary>
            /// Adds an employee.
            /// </summary>
            /// <param name="nanoContext">NanoContext.</param>
            /// <returns>The HTTP Method used when calling this API method.</returns>
            public static string EchoHttpMethod( NanoContext nanoContext )
            {
                return nanoContext.Request.HttpMethod;
            }

            /// <summary>
            /// Adds an employee.
            /// </summary>
            /// <param name="employee">Employee to add.</param>
            /// <param name="nanoContext">NanoContext.</param>
            /// <returns>Employee record.</returns>
            public static Stream StreamExample( Employee employee, NanoContext nanoContext )
            {
                nanoContext.Response.ContentType = "application/json";

                var json = JsonConvert.SerializeObject( employee );

                var bytes = Encoding.UTF8.GetBytes( json );

                var stream = new MemoryStream( bytes );
                
                return stream;
            }

            /// <summary>
            /// An employee.
            /// </summary>
            public class Employee
            {
                /// <summary>
                /// Employee Id.
                /// </summary>
                public int EmployeeId;

                /// <summary>
                /// Employees first name.
                /// </summary>
                public string FirstName;

                /// <summary>
                /// Employees last name.
                /// </summary>
                public string LastName;
            }
        }

        [Test]
        public void Can_Call_A_Method_With_An_Injected_NanoContext()
        {
            const string url = "http://localhost/NanoTests/NanoContextInjectedParameterTestsApi/EchoHttpMethod";

            var response = HttpHelper.Get( url );

            Console.WriteLine( response );
            
            Assert.That( response.ToUpper().Contains( "GET" ) );
        }

        [Test]
        public void Can_Access_A_Method_That_Returns_A_Stream()
        {
            const string url = "http://localhost/NanoTests/NanoContextInjectedParameterTestsApi/StreamExample";

            var response = HttpHelper.Get( url + "?employee={\"firstname\":\"Clark\",\"lastname\":\"Kent\"}" );

            Console.WriteLine( response );

            Assert.That( response.Contains( "Error" ) == false );

            Assert.That( response.Contains( "Clark" ) );
        }
    }
}