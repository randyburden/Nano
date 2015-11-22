using System;
using System.Diagnostics;
using NUnit.Framework;

namespace Nano.Tests.Host.HttpListener
{
    [TestFixture]
    public class HttpListenerConfigurationShould
    {
        [Test]
        public void Allow_Setting_An_ApplicationPath_Which_Will_Get_Ignored_During_Routing()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.HttpListenerConfiguration.ApplicationPath = "MyApplicationsVirtualPath"; // Note: This is what we are testing
                
                server.NanoConfiguration.AddFunc( "/SayHi", context => "Hi" );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/MyApplicationsVirtualPath/SayHi" );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( "Hi" ) );
            }
        }

        [Test]
        public void Allow_Setting_A_Multi_Segmented_ApplicationPath_Which_Will_Get_Ignored_During_Routing()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.HttpListenerConfiguration.ApplicationPath = "MyApplicationsVirtualPath/MyOtherVirtualPath"; // Note: This is what we are testing

                server.NanoConfiguration.AddFunc( "/SayHi", context => "Hi" );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/MyApplicationsVirtualPath/MyOtherVirtualPath/SayHi" );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( "Hi" ) );
            }
        }

        [Test( Description = "This is for crazy people that build Guid-based URLs.. they do exist. =)")]
        public void Allow_Setting_A_Guid_Based_ApplicationPath_Which_Will_Get_Ignored_During_Routing()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                var randomGuid = Guid.NewGuid().ToString();
                server.HttpListenerConfiguration.ApplicationPath = randomGuid; // Note: This is what we are testing

                server.NanoConfiguration.AddFunc( "/SayHi", context => "Hi" );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + randomGuid + "/SayHi" );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( "Hi" ) );
            }
        }

        [Test]
        public void Allow_Setting_An_ApplicationPath_With_Leading_And_Trailing_Forward_Slashes()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.HttpListenerConfiguration.ApplicationPath = "/MyApplicationsVirtualPath/"; // Note: This is what we are testing

                server.NanoConfiguration.AddFunc( "/SayHi", context => "Hi" );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/MyApplicationsVirtualPath/SayHi" );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( "Hi" ) );
            }
        }

        [Test]
        public void Ignore_An_Empty_Application_Path()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.HttpListenerConfiguration.ApplicationPath = ""; // Note: This is what we are testing

                server.NanoConfiguration.AddFunc( "/SayHi", context => "Hi" );
                
                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/SayHi" );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( "Hi" ) );
            }
        }
        
        [Test]
        public void Work_Without_Setting_An_Application_Path()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFunc( "/SayHi", context => "Hi" );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/SayHi" );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( "Hi" ) );
            }
        }
    }
}