using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nano.Core;
using Nano.Host;
using Nano.Tests.TestHelpers;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Nano.Tests
{
    [TestFixture]
    public class ComplexParameterTests
    {
        public string AddEmployeeUrl = "http://localhost/NanoTests/ComplexParameterTestsApi/AddEmployeeRecord";

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var config = new NanoServerConfiguration( @"http://+:80/NanoTests/" );

            config.AddWebApi<ComplexParameterTestsApi>();

            NanoServer.Start( config );
        }

        /// <summary>
        /// Class being tested.
        /// </summary>
        public class ComplexParameterTestsApi
        {
            /// <summary>
            /// Adds an employee record and allows the user to specify the employee id.
            /// </summary>
            /// <param name="employee">Employee record.</param>
            /// <returns>Employee record.</returns>
            public static Employee AddEmployeeRecord( Employee employee )
            {
                return employee;
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
        public void Can_GET_Passing_Json_VIA_Query_String()
        {
            var employee = new ComplexParameterTestsApi.Employee { EmployeeId = 1000, FirstName = "Clark", LastName = "Kent" };

            var json = JsonConvert.SerializeObject( employee );

            var response = HttpHelper.Get( AddEmployeeUrl + "?employee=" + json );

            Console.WriteLine( response );

            Assert.That( response.Contains( "Clark" ) );
        }

        [Test]
        public void Can_POST_Passing_Json_VIA_Body()
        {
            var employee = new ComplexParameterTestsApi.Employee { EmployeeId = 1000, FirstName = "Clark", LastName = "Kent" };

            var json = JsonConvert.SerializeObject( employee );

            var parameters = new NameValueCollection { { "employee", json } };

            var response = HttpHelper.Post( AddEmployeeUrl, parameters );

            Console.WriteLine( response );

            Assert.That( response.Contains( "Clark" ) );
        }

        [Test]
        public void Can_GET_Passing_Json_VIA_Headers()
        {
            var employee = new ComplexParameterTestsApi.Employee { EmployeeId = 1000, FirstName = "Clark", LastName = "Kent" };

            var json = JsonConvert.SerializeObject( employee );

            var headers = new NameValueCollection { { "employee", json } };

            var response = HttpHelper.Get( AddEmployeeUrl, headers );

            Console.WriteLine( response );

            Assert.That( response.Contains( "Clark" ) );
        }

        [Test]
        public void Can_POST_Passing_Json_VIA_Headers()
        {
            var employee = new ComplexParameterTestsApi.Employee { EmployeeId = 1000, FirstName = "Clark", LastName = "Kent" };

            var json = JsonConvert.SerializeObject( employee );

            var headers = new NameValueCollection { { "employee", json } };

            var response = HttpHelper.Post( AddEmployeeUrl, new NameValueCollection(), headers );

            Console.WriteLine( response );

            Assert.That( response.Contains( "Clark" ) );
        }

        [Test]
        [Explicit]
        // On 10/25/14 it was 10 seconds to process 50,000 requests or 5,000 requests per second
        public void SimpleSynchronousPerformanceTest()
        {
            var stopWatch = Stopwatch.StartNew();

            for ( int i = 1; i <= 50000; i++ )
            {
                var employee = new ComplexParameterTestsApi.Employee { EmployeeId = i, FirstName = "Clark" + i, LastName = "Kent" + i };

                var json = JsonConvert.SerializeObject( employee );

                var response = HttpHelper.Get( AddEmployeeUrl + "?employee=" + json );
                
                Assert.That( response.Contains( "Clark" + i ) );
            }

            Console.WriteLine( "Test executed in " + stopWatch.ElapsedMilliseconds + " ms");
        }

        [Test]
        [Explicit]
        // On 10/25/14 it was 4.8 seconds to process 50,000 requests or ~10,000 requests per second
        public void SimpleAynchronousPerformanceTest()
        {
            var stopWatch = Stopwatch.StartNew();

            // Note that 5 parallel requests seem to be the sweet spot to gain the most performance
            // Adding a smaller or larger number resulted in decreased performance...
            Parallel.ForEach( Enumerable.Range( 1, 50000 ), new ParallelOptions { MaxDegreeOfParallelism = 5 }, i =>
            {
                var employee = new ComplexParameterTestsApi.Employee { EmployeeId = i, FirstName = "Clark" + i, LastName = "Kent" + i };

                var json = JsonConvert.SerializeObject( employee );

                var response = HttpHelper.Get( AddEmployeeUrl + "?employee=" + json );

                Assert.That( response.Contains( "Clark" + i ) );
            } );
            
            Console.WriteLine( "Test executed in " + stopWatch.ElapsedMilliseconds + " ms" );
        }
    }
}