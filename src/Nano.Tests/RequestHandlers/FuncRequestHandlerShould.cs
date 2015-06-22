using System;
using System.Diagnostics;
using Nano.Web.Core;
using Newtonsoft.Json;
using NUnit.Framework;

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
    }
}