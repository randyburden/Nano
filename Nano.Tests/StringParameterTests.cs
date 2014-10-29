using System;
using System.Collections.Specialized;
using Nano.Core;
using Nano.Host;
using Nano.Tests.TestHelpers;
using NUnit.Framework;

namespace Nano.Tests
{
    [TestFixture]
    public class StringParameterTests
    {
        public string AddEmployeeUrl = "http://localhost/NanoTests/StringParameterTestsApi/AddEmployee";

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var config = new NanoServerConfiguration( @"http://+:80/NanoTests/" );

            var webApi = config.AddWebApi<StringParameterTestsApi>();
            
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
        public class StringParameterTestsApi
        {
            /// <summary>
            /// Adds an employee.
            /// </summary>
            /// <param name="firstName">Employee first name.</param>
            /// <param name="lastName">Employee last name.</param>
            /// <returns>Employee record.</returns>
            public static Employee AddEmployee( string firstName, string lastName )
            {
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
            var response = HttpHelper.Get( AddEmployeeUrl + "?firstname=Clark&lastname=Kent" );

            Console.WriteLine( response );

            Assert.That( response.Contains( "Clark" ) );
        }

        [Test]
        public void Can_POST_Passing_Parameters_Via_Form_Body()
        {
            var parameters = new NameValueCollection { { "firstName", "Clark" }, { "lastName", "Kent" } };

            var response = HttpHelper.Post( AddEmployeeUrl, parameters );

            Console.WriteLine( response );

            Assert.That( response.Contains( "Clark" ) );
        }

        [Test]
        public void Can_GET_Passing_Parameters_Via_Headers()
        {
            var headers = new NameValueCollection { { "firstName", "Clark" }, { "lastName", "Kent" } };

            string response = HttpHelper.Get( AddEmployeeUrl, headers );

            Console.WriteLine( response );

            Assert.That( response.Contains( "Clark" ) );
        }

        [Test]
        public void Can_POST_Passing_Parameters_Via_Headers()
        {
            var headers = new NameValueCollection { { "firstName", "Clark" }, { "lastName", "Kent" } };

            string response = HttpHelper.Post( AddEmployeeUrl, new NameValueCollection(), headers );

            Console.WriteLine( response );

            Assert.That( response.Contains( "Clark" ) );
        }
    }
}