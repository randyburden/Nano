using System;
using System.Collections.Specialized;
using Nano.Core;
using Nano.Host;
using Nano.Tests.TestHelpers;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Nano.Tests
{
    [TestFixture]
    public class DynamicParameterTests
    {
        public string AddEmployeeUrl = "http://localhost/NanoTests/DynamicParameterTestsApi/AddEmployee";

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var config = new NanoServerConfiguration( @"http://+:80/NanoTests/" );

            var webApi = config.AddWebApi<DynamicParameterTestsApi>();
            
            webApi.ApiPreInvokeEvent.Add( context =>
            {
            } );

            webApi.ApiPostInvokeEvent.Add( context =>
            {
            } );

            webApi.ApiUnhandledExceptionEvent.Add( ( exception, context ) =>
            {
            });

            NanoServer.Start( config );
        }

        /// <summary>
        /// Class being tested.
        /// </summary>
        public class DynamicParameterTestsApi
        {
            /// <summary>
            /// Adds an employee.
            /// </summary>
            /// <param name="request">Request containing an employee's firstName and lastName.</param>
            /// <returns>Employee record.</returns>
            public static Employee AddEmployee( dynamic request )
            {
                string firstName = request.firstname; // Test case-insensitive member access
                string lastName = request.LASTNAME; // Test case-insensitive member access

                return new Employee { EmployeeId = 1, FirstName = firstName.Trim(), LastName = lastName.Trim() };
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
        public void Can_GET_Passing_Parameters_Via_Query_String()
        {
            var response = HttpHelper.Get( AddEmployeeUrl + "?request={\"firstname\":\"Clark\",\"lastname\":\"Kent\"}" );

            Console.WriteLine( response );

            Assert.That( response.Contains( "Error" ) == false );

            Assert.That( response.Contains( "Clark" ) );
        }

        [Test]
        public void Can_POST_Passing_Parameters_Via_Form_Body()
        {
            var parameters = new NameValueCollection { { "request", "{\"firstname\":\"Clark\",\"lastname\":\"Kent\"}" } };

            var response = HttpHelper.Post( AddEmployeeUrl, parameters );

            Console.WriteLine( response );

            Assert.That( response.Contains( "Clark" ) );
        }

        [Test]
        public void Can_POST_Passing_Parameters_Via_Form_Body_Version_2()
        {
            var request = new { FirstName = "Clark", LastName = "Kent" };

            var json = JsonConvert.SerializeObject( request );

            var parameters = new NameValueCollection { { "request", json } };

            var response = HttpHelper.Post( AddEmployeeUrl, parameters );

            Console.WriteLine( response );

            var employee = JsonConvert.DeserializeObject<DynamicParameterTestsApi.Employee>( response );

            Assert.That( employee.FirstName == request.FirstName );

            Assert.That( employee.LastName == request.LastName );
        }

        [Test]
        public void Can_GET_Passing_Parameters_Via_Headers()
        {
            var headers = new NameValueCollection { { "request", "{\"firstname\":\"Clark\",\"lastname\":\"Kent\"}" } };

            string response = HttpHelper.Get( AddEmployeeUrl, headers );

            Console.WriteLine( response );

            Assert.That( response.Contains( "Clark" ) );
        }

        [Test]
        public void Can_POST_Passing_Parameters_Via_Headers()
        {
            var headers = new NameValueCollection { { "request", "{\"firstname\":\"Clark\",\"lastname\":\"Kent\"}" } };

            string response = HttpHelper.Post( AddEmployeeUrl, new NameValueCollection(), headers );

            Console.WriteLine( response );

            Assert.That( response.Contains( "Clark" ) );
        }
    }
}