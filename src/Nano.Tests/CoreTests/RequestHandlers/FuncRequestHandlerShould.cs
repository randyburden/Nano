﻿using System;
using System.Diagnostics;
using System.Net;
using Nano.Web.Core;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Nano.Tests.RequestHandlers
{
    [TestFixture]
    public class FuncRequestHandlerShould
    {
        [Test]
        public void Serialize_Returned_Objects_Into_Json_By_Default()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFunc( "/GetCurrentTime", context =>
                {
                    return new { CurrentTime = DateTime.Now };
                } );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/GetCurrentTime" );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( "{" ) && response.Contains( "}" ) );
            }
        }

        [Test]
        public void Await_Tasks_And_Return_The_Result()
        {
            using (var server = NanoTestServer.Start())
            {
                // Arrange
                server.NanoConfiguration.AddFunc("/GetStringAsync", context => Task.FromResult("Async"));

                // Act
                var response = HttpHelper.GetResponseString(server.GetUrl() + "/GetStringAsync");

                // Assert
                Assert.AreEqual(string.Concat("\"Async\""), response);
            }
        }
        
        [Test]
        public void Extract_Request_Parameters_Using_The_Generic_Bind_NanoContext_Extension_Method()
        {
            Func_Will_Echo_Input_Parameter<string>( "Clark" );
            Func_Will_Echo_Input_Parameter<int>( "987" );
            Func_Will_Echo_Input_Parameter<long>( "987654987" );
            Func_Will_Echo_Input_Parameter<decimal>( "987.6548" );
            Func_Will_Echo_Input_Parameter<DateTime>( "2015-06-22" );
            Func_Will_Echo_Input_Parameter<object>( "654" );
            Func_Will_Echo_Input_Parameter<dynamic>( "Blah" );

            var json = JsonConvert.SerializeObject( new Animal { Name = "Monkey" } );

            Func_Will_Echo_Input_Parameter<object>( json );
            Func_Will_Echo_Input_Parameter<dynamic>( json );
            Func_Will_Echo_Input_Parameter<Animal>( json );

            json = JsonConvert.SerializeObject( new { Name = "Monkey" } );

            Func_Will_Echo_Input_Parameter<object>( json );
            Func_Will_Echo_Input_Parameter<dynamic>( json );
            Func_Will_Echo_Input_Parameter<Animal>( json );
        }

        [Test]
        public void Extract_Request_Parameters_Using_The_Bind_NanoContext_Extension_Method()
        {
            Func_Will_Echo_Input_Parameter_Using_Generic_Type<string>( "Clark" );
            Func_Will_Echo_Input_Parameter_Using_Generic_Type<int>( "987" );
            Func_Will_Echo_Input_Parameter_Using_Generic_Type<long>( "987654987" );
            Func_Will_Echo_Input_Parameter_Using_Generic_Type<decimal>( "987.6548" );
            Func_Will_Echo_Input_Parameter_Using_Generic_Type<DateTime>( "2015-06-22" );
            Func_Will_Echo_Input_Parameter_Using_Generic_Type<object>( "654" );
            Func_Will_Echo_Input_Parameter_Using_Generic_Type<dynamic>( "Blah" );

            var json = JsonConvert.SerializeObject( new Animal { Name = "Monkey" } );

            Func_Will_Echo_Input_Parameter_Using_Generic_Type<object>( json );
            Func_Will_Echo_Input_Parameter_Using_Generic_Type<dynamic>( json );
            Func_Will_Echo_Input_Parameter_Using_Generic_Type<Animal>( json );

            json = JsonConvert.SerializeObject( new { Name = "Monkey" } );

            Func_Will_Echo_Input_Parameter_Using_Generic_Type<object>( json );
            Func_Will_Echo_Input_Parameter_Using_Generic_Type<dynamic>( json );
            Func_Will_Echo_Input_Parameter_Using_Generic_Type<Animal>( json );
        }

        public class Animal
        {
            public string Name;
        }

        public void Func_Will_Echo_Input_Parameter_Using_Generic_Type<T>( string parameterValue )
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFunc( "/Echo", context =>
                {
                    var value = context.Bind<T>( "value" );
                    return value;
                } );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/Echo?value=" + parameterValue );

                // Visual Assertion
                Trace.WriteLine( typeof( T ).Name + " - Input: '" + parameterValue + "' Output: '" + response + "'" );
                
                // Assert
                Assert.That( response.Contains( parameterValue ) );
            }
        }

        public void Func_Will_Echo_Input_Parameter<T>( string parameterValue )
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFunc( "/Echo", context =>
                {
                    var value = context.Bind( typeof( T ), "value" );
                    return value;
                } );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/Echo?value=" + parameterValue );

                // Visual Assertion
                Trace.WriteLine( typeof( T ).Name + " - Input: '" + parameterValue + "' Output: '" + response + "'" );

                // Assert
                Assert.That( response.Contains( parameterValue ) );
            }
        }

        #region ETag Tests
        
        [Test]
        public void Return_An_ETag_Header_When_An_Http_200_Response_Is_Returned()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFunc( "/api/GetPerson", context =>
                {
                    var id = context.GetRequestParameterValue<int>( "id" );
                    return new { Id = id, FirstName = "Clark", LastName = "Kent" };
                } );

                // Act
                string eTag;
                using ( var response = HttpHelper.GetHttpWebResponse( server.GetUrl() + "/api/GetPerson?id=1" ) )
                {
                    eTag = response.GetResponseHeader("ETag");
                }

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
                server.NanoConfiguration.AddFunc( "/api/GetPerson", context =>
                {
                    var id = context.GetRequestParameterValue<int>( "id" );
                    return new { Id = id, FirstName = "Clark", LastName = "Kent" };
                } );

                string initialETag;
                using ( var initialResponse = HttpHelper.GetHttpWebResponse( server.GetUrl() + "/api/GetPerson?id=1" ) )
                {
                    initialETag = initialResponse.GetResponseHeader("ETag");
                }

                var request = HttpHelper.GetHttpWebRequest( server.GetUrl() + "/api/GetPerson?id=1" );
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

        [Test]
        public void Return_Matching_ETag_When_Server_Returns_Not_Modified()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFunc( "/api/GetPerson", context =>
                {
                    var id = context.GetRequestParameterValue<int>( "id" );
                    return new { Id = id, FirstName = "Clark", LastName = "Kent" };
                } );

                string initialETag;
                using ( var initialResponse = HttpHelper.GetHttpWebResponse( server.GetUrl() + "/api/GetPerson?id=1" ) )
                {
                    initialETag = initialResponse.GetResponseHeader("ETag");
                }

                var request = HttpHelper.GetHttpWebRequest( server.GetUrl() + "/api/GetPerson?id=1" );
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

        [Test]
        public void Return_Non_Matching_ETag_When_Server_Does_Not_Return_Not_Modified()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFunc( "/api/GetPerson", context =>
                {
                    var id = context.GetRequestParameterValue<int>( "id" );
                    return new { Id = id, FirstName = "Clark", LastName = "Kent" };
                } );

                string initialETag;
                using ( var initialResponse = HttpHelper.GetHttpWebResponse( server.GetUrl() + "/api/GetPerson?id=1" ) )
                {
                    initialETag = initialResponse.GetResponseHeader("ETag");
                }
                    
                var request = HttpHelper.GetHttpWebRequest( server.GetUrl() + "/api/GetPerson?id=2" );
                request.Headers["If-None-Match"] = initialETag;

                // Act
                string eTag;
                using ( var response = request.GetHttpWebResponse() )
                {
                    eTag = response.GetResponseHeader( "ETag" );
                }

                // Visual Assertion
                Trace.WriteLine( "1st ETag Header Value: " + initialETag );
                Trace.WriteLine( "2nd ETag Header Value: " + eTag );

                // Assert
                Assert.That( initialETag != eTag );
            }
        }

        [Test]
        public void Return_Empty_Body_When_Server_Returns_Not_Modified_Http_Status_Code_304_Because_Of_A_Matching_ETag()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFunc( "/api/GetPerson", context =>
                {
                    var id = context.GetRequestParameterValue<int>( "id" );
                    return new { Id = id, FirstName = "Clark", LastName = "Kent" };
                } );

                string initialETag;
                using ( var initialResponse = HttpHelper.GetHttpWebResponse( server.GetUrl() + "/api/GetPerson?id=1" ) )
                {
                    Trace.WriteLine("Initial Response Length: " + initialResponse.GetResponseString().Length);
                    initialETag = initialResponse.GetResponseHeader("ETag");
                }
                
                var request = HttpHelper.GetHttpWebRequest( server.GetUrl() + "/api/GetPerson?id=1" );
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
    }
}