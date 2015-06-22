using System.Diagnostics;
using System.Net;
using NUnit.Framework;

namespace Nano.Tests.RequestHandlers
{
    [TestFixture]
    public class DirectoryRequestHandlerShould
    {
        [TestCase( "/ApiExplorer/", "Api Explorer" )]
        [TestCase( "/APIEXPLORER/", "Api Explorer", Description = "Testing all UPPERCASE" )]
        [TestCase( "/apiexplorer/", "Api Explorer", Description = "Testing all lowercase" )]
        [TestCase( "/aPiExpLoRer/", "Api Explorer", Description = "Testing UPPER and lower case" )]
        [TestCase( "/", "Hello World from the www index.html" )]
        public void Return_The_Default_Index_Dot_Html_File_When_Requesting_A_Directory_Path( string path, string expectedResponse )
        {
            using ( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddDirectory( "/", "www" );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + path );

                // Assert
                Assert.That( response.Contains( expectedResponse ) );
            }
        }

        [TestCase( "/ApiExplorer/index.html", "Api Explorer" )]
        [TestCase( "/index.html", "Hello World from the www index.html" )]
        public void Return_A_File_That_Exists( string path, string expectedResponse )
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddDirectory( "/", "www" );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + path );

                // Assert
                Assert.That( response.Contains( expectedResponse ) );
            }
        }

        [TestCase( "/NoSuchDirectory/" )]
        [TestCase( "/Random/Path.extension" )]
        public void Return_An_Http_404_When_The_Requested_Path_Does_Not_Exist_And_The_returnHttp404WhenFileWasNotFound_Parameter_Was_Set_To_True( string path )
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddDirectory( "/", "www", returnHttp404WhenFileWasNotFound: true );

                // Act
                var response = HttpHelper.GetHttpWebResponse( server.GetUrl() + path );

                // Visual Assertion
                Trace.WriteLine( response.GetResponseString() );

                // Assert
                Assert.That( response.StatusCode == HttpStatusCode.NotFound );
            }
        }

        [Test]
        public void Return_An_Http_200_When_The_Requested_Path_Does_Not_Exist_And_The_returnHttp404WhenFileWasNotFound_Parameter_Was_Set_To_False()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddDirectory( "/", "www", returnHttp404WhenFileWasNotFound: false );

                // Act
                var response = HttpHelper.GetHttpWebResponse( server.GetUrl() + "/NoSuchDirectory/" );
                
                // Assert
                Assert.That( response.StatusCode == HttpStatusCode.OK );
            }
        }

        [Test]
        public void Redirect_With_A_Trailing_Slash_When_The_Requested_Path_Exists_But_The_Url_Does_Not_Have_A_Trailing_Slash()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddDirectory( "/", "www" );

                // Act
                var response = HttpHelper.GetHttpWebResponse( server.GetUrl() + "/ApiExplorer", allowAutoRedirect: false );
                var location = response.GetResponseHeader( "location" );

                // Visual Assertion
                Trace.WriteLine( "Location Header Value: " + location );

                // Assert
                Assert.That( location == "/ApiExplorer/" );
            }
        }
    }
}