using System;
using Nano.Core;
using Nano.Host;
using Nano.Tests.TestHelpers;
using NUnit.Framework;

namespace Nano.Tests
{
    [TestFixture]
    public class WebApiEventTests
    {
        private const string AddEmployeeUrl = "http://localhost/NanoTests/WebApiEventTestsApi/AddEmployee";

        private NanoContext _apiPreInvokeEventNanoContext;

        private NanoContext _apiPostInvokeEventNanoContext;

        private Exception _apiUnhandledExceptionEventException;

        private NanoContext _apiUnhandledExceptionEventNanoContext;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var config = new NanoServerConfiguration( @"http://+:80/NanoTests/" );

            var webApi = config.AddWebApi<WebApiEventTestsApi>();

            webApi.ApiPreInvokeEvent.Add( context =>
            {
                _apiPreInvokeEventNanoContext = context;
            } );

            webApi.ApiPostInvokeEvent.Add( context =>
            {
                _apiPostInvokeEventNanoContext = context;
            } );

            webApi.ApiUnhandledExceptionEvent.Add( ( exception, context ) =>
            {
                _apiUnhandledExceptionEventException = exception;
                _apiUnhandledExceptionEventNanoContext = context;
            } );

            // start server
            NanoServer.Start( config );
        }

        /// <summary>
        /// Class being tested.
        /// </summary>
        public class WebApiEventTestsApi
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
        public void ApiPreInvokeEvent_Is_Called()
        {
            var response = HttpHelper.Get( AddEmployeeUrl + "?firstname=Clark&lastname=Kent" );

            Assert.NotNull( _apiPreInvokeEventNanoContext );

            Assert.That( _apiPreInvokeEventNanoContext.Request.QueryStringParameters.ContainsValue( "Clark" ) );

            //Assert.That( ApiPreInvokeEventNanoContext.Response.ResponseObject.ToJson().Contains( "Clark" ) );
        }

        [Test]
        public void ApiPostInvokeEvent_Is_Called()
        {
            var response = HttpHelper.Get( AddEmployeeUrl + "?firstname=Clark&lastname=Kent" );

            Assert.NotNull( _apiPostInvokeEventNanoContext );

            Assert.That( _apiPostInvokeEventNanoContext.Request.QueryStringParameters.ContainsValue( "Clark" ) );

            Assert.That( _apiPreInvokeEventNanoContext.Response.ResponseObject.ToJson().Contains( "Clark" ) );
        }

        [Test]
        public void ApiUnhandledExceptionEvent_Is_Called()
        {
            var response = HttpHelper.Get( AddEmployeeUrl + "?asdf=jkl;" );

            Assert.NotNull( _apiUnhandledExceptionEventException );

            Assert.NotNull( _apiUnhandledExceptionEventNanoContext );

            Assert.NotNull( _apiUnhandledExceptionEventNanoContext.Response.Error );

            Assert.That( _apiUnhandledExceptionEventNanoContext.Response.Error.Exception == _apiUnhandledExceptionEventException );

            Console.WriteLine( _apiUnhandledExceptionEventNanoContext.NanoEnvironmentPropertiesToJson() );

            Console.WriteLine( response );
        }
    }
}