using System;
using System.Diagnostics;
using System.Net;
using NUnit.Framework;

namespace Nano.Tests.RequestHandlers
{
    [TestFixture]
    public class FileRequestHandlerShould
    {
        [TestCase( "/ApiExplorer/" )]
        [TestCase( "/ApiExplorer/index.html" )]
        [TestCase( "/APIEXPLORER/INDEX.HTML", Description = "Testing all UPPERCASE" )]
        [TestCase( "/apiexplorer/index.html", Description = "Testing all lowercase" )]
        [TestCase( "/aPiExpLoRer/InDeX.hTMl", Description = "Testing UPPER and lower case" )]
        public void Return_A_File_That_Exists( string path )
        {
            using ( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFile( path, "www/ApiExplorer/index.html" );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + path );

                // Assert
                Assert.That( response.Contains( "Api Explorer" ) );
            }
        }

        [TestCase( "/ApiExplorer/" )]
        [TestCase( "/ApiExplorer/index.htm" )]
        [TestCase( "/NoSuchFile.html" )]
        [TestCase( "//////lkjasldkffj409803948/laijsdfoiuwpo0e9ouphasldifj/ljpowu98723984uoiasjdlfknasl;dijpasoeurpoweur/" )]
        public void Return_An_Http_404_When_The_Requested_File_Is_Not_Found( string path )
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFile( "/ApiExplorer/index.html", "www/ApiExplorer/index.html" );

                // Act
                var response = HttpHelper.GetHttpWebResponse( server.GetUrl() + path );

                // Visual Assertion
                Trace.WriteLine( response.StatusCode );
                Trace.WriteLine( response.GetResponseString());

                // Assert
                Assert.That( response.StatusCode == HttpStatusCode.NotFound );
            }
        }

        #region ETag Tests

        [TestCase( "/ApiExplorer/" )]
        public void Return_An_ETag_Header_When_A_File_Is_Returned( string path )
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFile( path, "www/ApiExplorer/index.html" );

                // Act
                string eTag;
                using ( var response = HttpHelper.GetHttpWebResponse( server.GetUrl() + path ) )
                {
                    eTag = response.GetResponseHeader("ETag");
                }

                // Visual Assertion
                Trace.WriteLine( "ETag Header Value: " + eTag );

                // Assert
                Assert.NotNull( eTag );
            }
        }

        [TestCase( "/ApiExplorer/" )]
        public void Return_Not_Modified_Http_Status_Code_304_When_Request_ETag_Matches_File_ETag( string path )
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFile( path, "www/ApiExplorer/index.html" );

                string initialETag;
                using ( var initialResponse = HttpHelper.GetHttpWebResponse( server.GetUrl() + path ) )
                {
                    initialETag = initialResponse.GetResponseHeader("ETag");
                }

                var request = HttpHelper.GetHttpWebRequest( server.GetUrl() + path );
                request.Headers["If-None-Match"] = initialETag;

                // Act
                HttpStatusCode responseCode;
                using ( var response = request.GetHttpWebResponse() )
                {
                    responseCode = response.StatusCode;
                }

                // Visual Assertion
                Trace.WriteLine( "HTTP Status Code: " + responseCode );

                // Assert
                Assert.That( responseCode == HttpStatusCode.NotModified );
            }
        }

        [TestCase( "/ApiExplorer/" )]
        public void Return_Matching_ETag_When_Server_Returns_Not_Modified( string path )
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFile( path, "www/ApiExplorer/index.html" );

                string initialETag;
                using ( var initialResponse = HttpHelper.GetHttpWebResponse( server.GetUrl() + path ) )
                {
                    initialETag = initialResponse.GetResponseHeader("ETag");
                }

                var request = HttpHelper.GetHttpWebRequest( server.GetUrl() + path );
                request.Headers["If-None-Match"] = initialETag;

                // Act
                string eTag;
                using ( var response = request.GetHttpWebResponse() )
                {
                    eTag = response.GetResponseHeader("ETag");
                }

                // Visual Assertion
                Trace.WriteLine( "ETag Header Value: " + eTag );

                // Assert
                Assert.That( initialETag == eTag );
            }
        }

        [TestCase( "/ApiExplorer/" )]
        public void Return_Empty_Body_When_Server_Returns_Not_Modified_Http_Status_Code_304_Because_Of_A_Matching_ETag( string path )
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFile( path, "www/ApiExplorer/index.html" );

                string initialETag;
                using ( var initialResponse = HttpHelper.GetHttpWebResponse( server.GetUrl() + path ) )
                {
                    Trace.WriteLine("Initial Response Length: " + initialResponse.GetResponseString().Length);
                    initialETag = initialResponse.GetResponseHeader("ETag");
                }

                var request = HttpHelper.GetHttpWebRequest( server.GetUrl() + path );
                request.Headers["If-None-Match"] = initialETag;

                // Act
                int responseLength;
                using ( var response = request.GetHttpWebResponse() )
                {
                    responseLength = response.GetResponseString().Length;
                }

                // Visual Assertion
                Trace.WriteLine( "Not Modified Response Length: " + responseLength );

                // Assert
                Assert.That( responseLength == 0 );
            }
        }

        #endregion ETag Tests

        #region Last-Modified Tests

        [TestCase( "/ApiExplorer/" )]
        public void Return_An_Last_Modified_Header_When_A_File_Is_Returned( string path )
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFile( path, "www/ApiExplorer/index.html" );

                // Act
                string lastModified;
                using ( var response = HttpHelper.GetHttpWebResponse( server.GetUrl() + path ) )
                {
                    lastModified = response.GetResponseHeader("Last-Modified");
                }

                // Visual Assertion
                Trace.WriteLine( "Last-Modified Header Value: " + lastModified );

                // Assert
                Assert.NotNull( lastModified );
            }
        }

        [TestCase( "/ApiExplorer/" )]
        public void Return_Not_Modified_Http_Status_Code_304_When_Request_Last_Modified_Header_Matches_File_Last_Modified_DateTime( string path )
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFile( path, "www/ApiExplorer/index.html" );

                string initialLastModified;
                using ( var initialResponse = HttpHelper.GetHttpWebResponse( server.GetUrl() + path ) )
                {
                    initialLastModified = initialResponse.GetResponseHeader("Last-Modified");
                }

                var request = HttpHelper.GetHttpWebRequest( server.GetUrl() + path );
                request.IfModifiedSince = DateTime.Parse( initialLastModified );

                // Act
                HttpStatusCode responseCode;
                using ( var response = request.GetHttpWebResponse() )
                {
                    responseCode = response.StatusCode;
                }

                // Visual Assertion
                Trace.WriteLine( "HTTP Status Code: " + responseCode );

                // Assert
                Assert.That( responseCode == HttpStatusCode.NotModified );
            }
        }

        [TestCase( "/ApiExplorer/" )]
        public void Return_Matching_Last_Modified_Header_When_Server_Returns_Not_Modified( string path )
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFile( path, "www/ApiExplorer/index.html" );

                string initialLastModified;
                using ( var initialResponse = HttpHelper.GetHttpWebResponse( server.GetUrl() + path ) )
                {
                    initialLastModified = initialResponse.GetResponseHeader("Last-Modified");
                }

                var request = HttpHelper.GetHttpWebRequest( server.GetUrl() + path );
                request.IfModifiedSince = DateTime.Parse( initialLastModified );

                // Act
                string lastModified;
                using ( var response = request.GetHttpWebResponse() )
                {
                    lastModified = response.GetResponseHeader("Last-Modified");
                }

                // Visual Assertion
                Trace.WriteLine( "Last-Modified Header Value: " + lastModified );

                // Assert
                Assert.That( initialLastModified == lastModified );
            }
        }

        [TestCase( "/ApiExplorer/" )]
        public void Return_Empty_Body_When_Server_Returns_Not_Modified_Http_Status_Code_304_Because_Of_A_Matching_Last_Modified_Header( string path )
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFile( path, "www/ApiExplorer/index.html" );

                string initialLastModified;
                using ( var initialResponse = HttpHelper.GetHttpWebResponse( server.GetUrl() + path ) )
                {
                    Trace.WriteLine("Initial Response Length: " + initialResponse.GetResponseString().Length);
                    initialLastModified = initialResponse.GetResponseHeader("Last-Modified");
                }
                
                var request = HttpHelper.GetHttpWebRequest( server.GetUrl() + path );
                request.IfModifiedSince = DateTime.Parse( initialLastModified );

                // Act
                int responseLength;
                using ( var response = request.GetHttpWebResponse() )
                {
                    responseLength = response.GetResponseString().Length;
                }

                // Visual Assertion
                Trace.WriteLine( "Not Modified Response Length: " + responseLength );

                // Assert
                Assert.That( responseLength == 0 );
            }
        }

        #endregion Last-Modified Tests
    }
}