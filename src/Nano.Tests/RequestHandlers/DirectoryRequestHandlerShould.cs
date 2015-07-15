using System;
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

        #region ETag Tests

        [Test]
        public void Return_An_ETag_Header_When_A_File_Is_Returned()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddDirectory( "/", "www" );

                // Act
                var response = HttpHelper.GetHttpWebResponse( server.GetUrl() + "/ApiExplorer" );
                var eTag = response.GetResponseHeader( "ETag" );

                // Visual Assertion
                Trace.WriteLine( "ETag Header Value: " + eTag );

                // Assert
                Assert.NotNull( eTag );
            }
        }

        [Test]
        public void Return_Not_Modified_Http_Status_Code_304_When_Request_ETag_Matches_File_ETag()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddDirectory( "/", "www" );
                var initialResponse = HttpHelper.GetHttpWebResponse( server.GetUrl() + "/ApiExplorer" );
                var initialETag = initialResponse.GetResponseHeader( "ETag" );
                var request = HttpHelper.GetHttpWebRequest( server.GetUrl() + "/ApiExplorer" );
                request.Headers["If-None-Match"] = initialETag;

                // Act
                var response = request.GetHttpWebResponse();
                var responseCode = response.StatusCode;
                
                // Visual Assertion
                Trace.WriteLine( "HTTP Status Code: " + responseCode );
                
                // Assert
                Assert.That( responseCode == HttpStatusCode.NotModified );
            }
        }

        [Test]
        public void Return_Matching_ETag_When_Server_Returns_Not_Modified()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddDirectory( "/", "www" );
                var initialResponse = HttpHelper.GetHttpWebResponse( server.GetUrl() + "/ApiExplorer" );
                var initialETag = initialResponse.GetResponseHeader( "ETag" );
                var request = HttpHelper.GetHttpWebRequest( server.GetUrl() + "/ApiExplorer" );
                request.Headers["If-None-Match"] = initialETag;

                // Act
                var response = request.GetHttpWebResponse();
                var eTag = response.GetResponseHeader( "ETag" );
                
                // Visual Assertion
                Trace.WriteLine( "ETag Header Value: " + eTag );

                // Assert
                Assert.That( initialETag == eTag );
            }
        }

        [Test]
        public void Return_Empty_Body_When_Server_Returns_Not_Modified_Http_Status_Code_304_Because_Of_A_Matching_ETag()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddDirectory( "/", "www" );
                var initialResponse = HttpHelper.GetHttpWebResponse( server.GetUrl() + "/ApiExplorer" );
                Trace.WriteLine( "Initial Response Length: " + initialResponse.GetResponseString().Length );
                var initialETag = initialResponse.GetResponseHeader( "ETag" );
                var request = HttpHelper.GetHttpWebRequest( server.GetUrl() + "/ApiExplorer" );
                request.Headers["If-None-Match"] = initialETag;

                // Act
                var response = request.GetHttpWebResponse();
                var responseLength = response.GetResponseString().Length;

                // Visual Assertion
                Trace.WriteLine( "Not Modified Response Length: " + responseLength );

                // Assert
                Assert.That( responseLength == 0 );
            }
        }

        #endregion ETag Tests

        #region Last-Modified Tests

        [Test]
        public void Return_An_Last_Modified_Header_When_A_File_Is_Returned()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddDirectory( "/", "www" );

                // Act
                var response = HttpHelper.GetHttpWebResponse( server.GetUrl() + "/ApiExplorer" );
                var lastModified = response.GetResponseHeader( "Last-Modified" );

                // Visual Assertion
                Trace.WriteLine( "Last-Modified Header Value: " + lastModified );

                // Assert
                Assert.NotNull( lastModified );
            }
        }

        [Test]
        public void Return_Not_Modified_Http_Status_Code_304_When_Request_Last_Modified_Header_Matches_File_Last_Modified_DateTime()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddDirectory( "/", "www" );
                var initialResponse = HttpHelper.GetHttpWebResponse( server.GetUrl() + "/ApiExplorer" );
                var initialLastModified = initialResponse.GetResponseHeader( "Last-Modified" );
                var request = HttpHelper.GetHttpWebRequest( server.GetUrl() + "/ApiExplorer" );
                request.IfModifiedSince = DateTime.Parse( initialLastModified );

                // Act
                var response = request.GetHttpWebResponse();
                var responseCode = response.StatusCode;

                // Visual Assertion
                Trace.WriteLine( "HTTP Status Code: " + responseCode );

                // Assert
                Assert.That( responseCode == HttpStatusCode.NotModified );
            }
        }

        [Test]
        public void Return_Matching_Last_Modified_Header_When_Server_Returns_Not_Modified()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddDirectory( "/", "www" );
                var initialResponse = HttpHelper.GetHttpWebResponse( server.GetUrl() + "/ApiExplorer" );
                var initialLastModified = initialResponse.GetResponseHeader( "Last-Modified" );
                var request = HttpHelper.GetHttpWebRequest( server.GetUrl() + "/ApiExplorer" );
                request.IfModifiedSince = DateTime.Parse( initialLastModified );

                // Act
                var response = request.GetHttpWebResponse();
                var lastModified = response.GetResponseHeader( "Last-Modified" );

                // Visual Assertion
                Trace.WriteLine( "Last-Modified Header Value: " + lastModified );

                // Assert
                Assert.That( initialLastModified == lastModified );
            }
        }

        [Test]
        public void Return_Empty_Body_When_Server_Returns_Not_Modified_Http_Status_Code_304_Because_Of_A_Matching_Last_Modified_Header()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddDirectory( "/", "www" );
                var initialResponse = HttpHelper.GetHttpWebResponse( server.GetUrl() + "/ApiExplorer" );
                Trace.WriteLine( "Initial Response Length: " + initialResponse.GetResponseString().Length );
                var initialLastModified = initialResponse.GetResponseHeader( "Last-Modified" );
                var request = HttpHelper.GetHttpWebRequest( server.GetUrl() + "/ApiExplorer" );
                request.IfModifiedSince = DateTime.Parse( initialLastModified );

                // Act
                var response = request.GetHttpWebResponse();
                var responseLength = response.GetResponseString().Length;

                // Visual Assertion
                Trace.WriteLine( "Not Modified Response Length: " + responseLength );

                // Assert
                Assert.That( responseLength == 0 );
            }
        }

        #endregion Last-Modified Tests
    }
}