using System;
using Nano.Core;
using Nano.Host;
using Nano.Tests.TestHelpers;
using NUnit.Framework;

namespace Nano.Tests
{
    [TestFixture]
    public class VoidMethodTests
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var config = new NanoServerConfiguration( @"http://+:80/NanoTests/" );

            config.AddWebApi<VoidMethodTestsApi>();
            
            NanoServer.Start( config );
        }

        /// <summary>
        /// Class being tested.
        /// </summary>
        public class VoidMethodTestsApi
        {
            /// <summary>
            /// Adds an employee.
            /// </summary>
            /// <param name="employee">Employee to add.</param>
            /// <param name="nanoContext">NanoContext.</param>
            /// <returns>Employee record.</returns>
            public static void VoidMethodThatWritesJsonToTheResponseBody( Employee employee, NanoContext nanoContext )
            {
                nanoContext.ReturnAsJson( employee );
            }

            /// <summary>
            /// Adds an employee.
            /// </summary>
            /// <param name="employee">Employee to add.</param>
            /// <returns>Employee record.</returns>
            public static void VoidMethodThatDoesNotWriteAnythingToTheResponseBody( Employee employee)
            {
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
        public void Can_Call_A_Void_Method_That_Writes_Json_To_The_Response_Body()
        
        {
            const string url = "http://localhost/NanoTests/VoidMethodTestsApi/VoidMethodThatWritesJsonToTheResponseBody";

            var response = HttpHelper.Get( url + "?employee={\"firstname\":\"Clark\",\"lastname\":\"Kent\"}" );

            Console.WriteLine( response );
            
            Assert.That( response.Contains( "Clark" ) );
        }

        [Test]
        public void Can_Call_A_Void_Method_That_Does_Not_Write_Anything_To_The_Response_Body()
        {
            const string url = "http://localhost/NanoTests/VoidMethodTestsApi/VoidMethodThatDoesNotWriteAnythingToTheResponseBody";

            var response = HttpHelper.Get( url + "?employee={\"firstname\":\"Clark\",\"lastname\":\"Kent\"}" );

            Console.WriteLine( response );
            
            Assert.That( response == "" );
        }
    }
}