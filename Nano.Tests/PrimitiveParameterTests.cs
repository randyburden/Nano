using System;
using System.Globalization;
using Nano.Core;
using Nano.Host;
using Nano.Tests.TestHelpers;
using NUnit.Framework;

namespace Nano.Tests
{
    [TestFixture]
    public class PrimitiveParameterTests
    {
        public string BaseUrl = "http://localhost/NanoTests/PrimitiveParameterTestsApi/";

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var config = new NanoServerConfiguration( @"http://+:80/NanoTests/" );

            var webApi = config.AddWebApi<PrimitiveParameterTestsApi>();
            
            NanoServer.Start( config );
        }

        /// <summary>
        /// Class being tested.
        /// </summary>
        public class PrimitiveParameterTestsApi
        {
            /// <summary>
            /// Echos back the passed in value.
            /// </summary>
            /// <param name="value">Value to echo back.</param>
            /// <returns>Value being echoed back.</returns>
            public static int EchoInt( int value )
            {
                return value;
            }

            /// <summary>
            /// Echos back the passed in value.
            /// </summary>
            /// <param name="value">Value to echo back.</param>
            /// <returns>Value being echoed back.</returns>
            public static long EchoLong( long value )
            {
                return value;
            }

            /// <summary>
            /// Echos back the passed in value.
            /// </summary>
            /// <param name="value">Value to echo back.</param>
            /// <returns>Value being echoed back.</returns>
            public static double EchoDouble( double value )
            {
                return value;
            }

            /// <summary>
            /// Echos back the passed in value.
            /// </summary>
            /// <param name="value">Value to echo back.</param>
            /// <returns>Value being echoed back.</returns>
            public static decimal EchoDecimal( decimal value )
            {
                return value;
            }

            /// <summary>
            /// Echos back the passed in value.
            /// </summary>
            /// <param name="value">Value to echo back.</param>
            /// <returns>Value being echoed back.</returns>
            public static bool EchoBool( bool value )
            {
                return value;
            }

            /// <summary>
            /// Echos back the passed in value.
            /// </summary>
            /// <param name="value">Value to echo back.</param>
            /// <returns>Value being echoed back.</returns>
            public static DateTime EchoDateTime( DateTime value )
            {
                return value;
            }

            /// <summary>
            /// Echos back the passed in value.
            /// </summary>
            /// <param name="value">Value to echo back.</param>
            /// <returns>Value being echoed back.</returns>
            public static char EchoChar( char value )
            {
                return value;
            }

            /// <summary>
            /// Echos back the passed in value.
            /// </summary>
            /// <param name="value">Value to echo back.</param>
            /// <returns>Value being echoed back.</returns>
            public static Guid EchoGuid( Guid value )
            {
                return value;
            }

            /// <summary>
            /// Echos back the passed in value.
            /// </summary>
            /// <param name="value">Value to echo back.</param>
            /// <returns>Value being echoed back.</returns>
            public static object EchoObject( object value )
            {
                return value;
            }
        }

        [Test]
        public void EchoInt()
        {
            var response = HttpHelper.Get( BaseUrl + "EchoInt" + "?value=" + int.MaxValue );

            Console.WriteLine( response );

            Assert.That( response.Contains( int.MaxValue.ToString() ) );
        }

        [Test]
        public void EchoLong()
        {
            var response = HttpHelper.Get( BaseUrl + "EchoLong" + "?value=" + long.MaxValue );

            Console.WriteLine( response );

            Assert.That( response.Contains( long.MaxValue.ToString() ) );
        }

        [Test]
        public void EchoDouble()
        {
            var value = double.MaxValue.ToString( "r" );

            Console.WriteLine( value );

            var response = HttpHelper.Get( BaseUrl + "EchoDouble" + "?value=" + System.Uri.EscapeDataString( value ) );

            Console.WriteLine( response );

            Assert.That( response.Contains( value ) );
        }

        [Test]
        public void EchoDecimal()
        {
            var response = HttpHelper.Get( BaseUrl + "EchoDecimal" + "?value=" + decimal.MaxValue );

            Console.WriteLine( response );

            Assert.That( response.Contains( decimal.MaxValue.ToString() ) );
        }

        [Test]
        public void EchoBool()
        {
            var value = true.ToString().ToLower();

            var response = HttpHelper.Get( BaseUrl + "EchoBool" + "?value=" + value );

            Console.WriteLine( response );

            Assert.That( response.ToLower().Contains( value ) );
        }

        [Test]
        public void EchoDateTime()
        {
            var value = DateTime.UtcNow.ToString( "yyyy-MM-ddTHH:mm:sszzz", DateTimeFormatInfo.InvariantInfo );
            
            var response = HttpHelper.Get( BaseUrl + "EchoDateTime" + "?value=" + value );

            Console.WriteLine( response );

            Assert.That( response.Contains( value ) );
        }

        [Test]
        public void EchoChar()
        {
            const char charValue = 'R';

            var response = HttpHelper.Get( BaseUrl + "EchoChar" + "?value=" + charValue );

            Console.WriteLine( response );

            Assert.That( response.Contains( charValue.ToString() ) );
        }

        [Test]
        public void EchoGuid()
        {
            var guid = Guid.NewGuid();

            var response = HttpHelper.Get( BaseUrl + "EchoGuid" + "?value=" + guid );

            Console.WriteLine( response );

            Assert.That( response.Contains( guid.ToString() ) );
        }

        [Test]
        public void EchoObject()
        {
            var obj = new { Color = "Gold" };

            var response = HttpHelper.Get( BaseUrl + "EchoObject" + "?value=" + obj );

            Console.WriteLine( response );

            Assert.That( response.Contains( obj.ToString() ) );
        }
    }
}