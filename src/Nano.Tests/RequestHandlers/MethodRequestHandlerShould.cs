using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Nano.Tests.RequestHandlers
{
    [TestFixture]
    public class MethodRequestHandlerShould
    {
        [Test]
        public void Serialize_Returned_Objects_Into_Json_By_Default()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Echo>();
                var complexType = new Echo.ComplexType { Id = 456, Name = "Some Name" };
                var complexTypeJson = JsonConvert.SerializeObject( complexType );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/api/Echo/EchoComplexType?someComplexType=" + complexTypeJson );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( "{" ) && response.Contains( "}" ) );
            }
        }

        [Test]
        public void Accept_A_String_As_Input()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Echo>();
                var someString = "I am a string";

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/api/Echo/EchoString?someString=" + someString );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( someString ) );
            }
        }

        [Test]
        public void Accept_An_Int_As_Input()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Echo>();
                var someInt = int.MaxValue;

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/api/Echo/EchoInt?someInt=" + someInt );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( someInt.ToString() ) );
            }
        }

        [Test]
        public void Accept_A_Long_As_Input()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Echo>();
                var someLong = long.MaxValue;

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/api/Echo/EchoLong?someLong=" + someLong );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( someLong.ToString() ) );
            }
        }

        [Test]
        public void Accept_A_Decimal_As_Input()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Echo>();
                var someDecimal = decimal.MaxValue;

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/api/Echo/EchoDecimal?someDecimal=" + someDecimal );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( someDecimal.ToString() ) );
            }
        }

        [Test]
        public void Accept_A_Float_As_Input()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Echo>();
                var someFloat = "3.402823E+38";
                var someFloatEncoded = System.Uri.EscapeDataString( "3.402823E+38" );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/api/Echo/EchoFloat?someFloat=" + someFloatEncoded );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( someFloat ) );
            }
        }

        [Test]
        public void Accept_A_DateTime_As_Input()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Echo>();
                var someDateTimeString = "9999-12-31T23:59:59";
                var someDateTime = DateTime.Parse( someDateTimeString );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/api/Echo/EchoDateTime?someDateTime=" + someDateTime );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( someDateTimeString ) );
            }
        }

        [Test]
        public void Accept_An_Object_As_Input()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Echo>();
                var someObject = new { Name = "I am an object with a name property" };
                var someObjectJson = JsonConvert.SerializeObject( someObject );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/api/Echo/EchoObject?someObject=" + someObjectJson );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( someObjectJson ) );
            }
        }

        [Test]
        public void Accept_A_Dynamic_As_Input()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Echo>();
                dynamic someDynamic = new ExpandoObject();
                someDynamic.Name = "I am a dynamic object with a name property";
                string someDynamicJson = JsonConvert.SerializeObject( someDynamic );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/api/Echo/EchoDynamic?someDynamic=" + someDynamicJson );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( someDynamicJson ) );
            }
        }
        
        [Test]
        public void Accept_A_Complex_Type_As_Input()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Echo>();
                var complexType = new Echo.ComplexType { Id = 654, Name = "Some Name" };
                string complexTypeJson = JsonConvert.SerializeObject( complexType );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/api/Echo/EchoComplexType?someComplexType=" + complexTypeJson );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( complexTypeJson ) );
            }
        }

        [Test]
        public void Accept_A_Complex_Type_List_As_Input()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Echo>();

                var someComplexTypeList = new List<Echo.ComplexType>();

                for( var i = 1; i <= 50; i++ )
                    someComplexTypeList.Add( new Echo.ComplexType { Id = i, Name = "Name " + i } );

                string someComplexTypeListJson = JsonConvert.SerializeObject( someComplexTypeList );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/api/Echo/EchoComplexTypeList?someComplexTypeList=" + someComplexTypeListJson );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( someComplexTypeListJson ) );
            }
        }

        [Test]
        public void Accept_A_Complex_Type_IList_As_Input()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Echo>();

                var someComplexTypeList = new List<Echo.ComplexType>();

                for( var i = 1; i <= 50; i++ )
                    someComplexTypeList.Add( new Echo.ComplexType { Id = i, Name = "Name " + i } );

                string someComplexTypeListJson = JsonConvert.SerializeObject( someComplexTypeList );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/api/Echo/EchoComplexTypeIList?someComplexTypeList=" + someComplexTypeListJson );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( someComplexTypeListJson ) );
            }
        }

        [Test]
        public void Accept_A_Complex_Type_ICollection_As_Input()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Echo>();

                var someComplexTypeList = new List<Echo.ComplexType>();

                for( var i = 1; i <= 50; i++ )
                    someComplexTypeList.Add( new Echo.ComplexType { Id = i, Name = "Name " + i } );

                string someComplexTypeListJson = JsonConvert.SerializeObject( someComplexTypeList );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/api/Echo/EchoComplexTypeICollection?someComplexTypeList=" + someComplexTypeListJson );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( someComplexTypeListJson ) );
            }
        }

        [Test]
        public void Accept_A_Dictionary_As_Input()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Echo>();

                var dictionary = new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" } };

                string dictionaryJson = JsonConvert.SerializeObject( dictionary );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/api/Echo/EchoDictionaryOfStringString?dictionary=" + dictionaryJson );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( dictionaryJson ) );
            }
        }

        [Test]
        public void Accept_A_Dictionary_Of_Int_ComplexType_As_Input()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Echo>();

                var dictionary = new Dictionary<int, Echo.ComplexType>();

                for ( int i = 1; i <= 30; i++ )
                {
                    dictionary.Add( i, new Echo.ComplexType { Id = i, Name = "Name " + i });
                }

                string dictionaryJson = JsonConvert.SerializeObject( dictionary );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/api/Echo/EchoDictionaryOfIntComplexType?dictionary=" + dictionaryJson );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( dictionaryJson ) );
            }
        }

        public class Echo
        {
            public static string EchoString( string someString )
            {
                return someString;
            }

            public static int EchoInt( int someInt )
            {
                return someInt;
            }

            public static long EchoLong( long someLong )
            {
                return someLong;
            }

            public static decimal EchoDecimal( decimal someDecimal )
            {
                return someDecimal;
            }

            public static float EchoFloat( float someFloat )
            {
                return someFloat;
            }

            public static DateTime EchoDateTime( DateTime someDateTime )
            {
                return someDateTime;
            }

            public static object EchoObject( object someObject )
            {
                return someObject;
            }

            public static dynamic EchoDynamic( dynamic someDynamic )
            {
                return someDynamic;
            }

            public static ComplexType EchoComplexType( ComplexType someComplexType )
            {
                return someComplexType;
            }

            public static List<ComplexType> EchoComplexTypeList( List<ComplexType> someComplexTypeList )
            {
                return someComplexTypeList;
            }

            public static IList<ComplexType> EchoComplexTypeIList( IList<ComplexType> someComplexTypeList )
            {
                return someComplexTypeList;
            }

            public static ICollection<ComplexType> EchoComplexTypeICollection( ICollection<ComplexType> someComplexTypeList )
            {
                return someComplexTypeList;
            }

            public static Dictionary<string, string> EchoDictionaryOfStringString( Dictionary<string, string> dictionary )
            {
                return dictionary;
            }

            public static Dictionary<int, ComplexType> EchoDictionaryOfIntComplexType( Dictionary<int, ComplexType> dictionary )
            {
                return dictionary;
            }

            public class ComplexType
            {
                public int Id;
                public string Name;
            }
        }

        [Test( Description = "This is to test that Nano can support a large number of parameters." )]
        public void Accept_Two_Through_Fifteen_Integers_As_Input()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddMethods<AddIntegers>();
                
                // Act, Visual Assertion, Assert
                var response = HttpHelper.GetResponseString( server.GetUrl() + "/api/AddIntegers/AddTwoNumbers?number1=1&number2=2" );
                Trace.WriteLine( response );
                Assert.That( response.Contains( "3" ) );

                // Act, Visual Assertion, Assert
                response = HttpHelper.GetResponseString( server.GetUrl() + "/api/AddIntegers/AddThreeNumbers?number1=1&number2=2&number3=3" );
                Trace.WriteLine( response );
                Assert.That( response.Contains( "6" ) );

                // Act, Visual Assertion, Assert
                response = HttpHelper.GetResponseString( server.GetUrl() + "/api/AddIntegers/AddFourNumbers?number1=1&number2=2&number3=3&number4=4" );
                Trace.WriteLine( response );
                Assert.That( response.Contains( "10" ) );

                // Act, Visual Assertion, Assert
                response = HttpHelper.GetResponseString( server.GetUrl() + "/api/AddIntegers/AddFiveNumbers?number1=1&number2=2&number3=3&number4=4&number5=5" );
                Trace.WriteLine( response );
                Assert.That( response.Contains( "15" ) );

                // Act, Visual Assertion, Assert
                response = HttpHelper.GetResponseString( server.GetUrl() + "/api/AddIntegers/AddSixNumbers?number1=1&number2=2&number3=3&number4=4&number5=5&number6=6" );
                Trace.WriteLine( response );
                Assert.That( response.Contains( "21" ) );

                // Act, Visual Assertion, Assert
                response = HttpHelper.GetResponseString( server.GetUrl() + "/api/AddIntegers/AddSevenNumbers?number1=1&number2=2&number3=3&number4=4&number5=5&number6=6&number7=7" );
                Trace.WriteLine( response );
                Assert.That( response.Contains( "28" ) );

                // Act, Visual Assertion, Assert
                response = HttpHelper.GetResponseString( server.GetUrl() + "/api/AddIntegers/AddEightNumbers?number1=1&number2=2&number3=3&number4=4&number5=5&number6=6&number7=7&number8=8" );
                Trace.WriteLine( response );
                Assert.That( response.Contains( "36" ) );

                // Act, Visual Assertion, Assert
                response = HttpHelper.GetResponseString( server.GetUrl() + "/api/AddIntegers/AddNineNumbers?number1=1&number2=2&number3=3&number4=4&number5=5&number6=6&number7=7&number8=8&number9=9" );
                Trace.WriteLine( response );
                Assert.That( response.Contains( "45" ) );

                // Act, Visual Assertion, Assert
                response = HttpHelper.GetResponseString( server.GetUrl() + "/api/AddIntegers/AddTenNumbers?number1=1&number2=2&number3=3&number4=4&number5=5&number6=6&number7=7&number8=8&number9=9&number10=10" );
                Trace.WriteLine( response );
                Assert.That( response.Contains( "55" ) );

                // Act, Visual Assertion, Assert
                response = HttpHelper.GetResponseString( server.GetUrl() + "/api/AddIntegers/AddElevenNumbers?number1=1&number2=2&number3=3&number4=4&number5=5&number6=6&number7=7&number8=8&number9=9&number10=10&number11=11" );
                Trace.WriteLine( response );
                Assert.That( response.Contains( "66" ) );

                // Act, Visual Assertion, Assert
                response = HttpHelper.GetResponseString( server.GetUrl() + "/api/AddIntegers/AddTwelveNumbers?number1=1&number2=2&number3=3&number4=4&number5=5&number6=6&number7=7&number8=8&number9=9&number10=10&number11=11&number12=12" );
                Trace.WriteLine( response );
                Assert.That( response.Contains( "78" ) );

                // Act, Visual Assertion, Assert
                response = HttpHelper.GetResponseString( server.GetUrl() + "/api/AddIntegers/AddThirteenNumbers?number1=1&number2=2&number3=3&number4=4&number5=5&number6=6&number7=7&number8=8&number9=9&number10=10&number11=11&number12=12&number13=13" );
                Trace.WriteLine( response );
                Assert.That( response.Contains( "91" ) );

                // Act, Visual Assertion, Assert
                response = HttpHelper.GetResponseString( server.GetUrl() + "/api/AddIntegers/AddFourteenNumbers?number1=1&number2=2&number3=3&number4=4&number5=5&number6=6&number7=7&number8=8&number9=9&number10=10&number11=11&number12=12&number13=13&number14=14" );
                Trace.WriteLine( response );
                Assert.That( response.Contains( "105" ) );

                // Act, Visual Assertion, Assert
                response = HttpHelper.GetResponseString( server.GetUrl() + "/api/AddIntegers/AddFifteenNumbers?number1=1&number2=2&number3=3&number4=4&number5=5&number6=6&number7=7&number8=8&number9=9&number10=10&number11=11&number12=12&number13=13&number14=14&number15=15" );
                Trace.WriteLine( response );
                Assert.That( response.Contains( "120" ) );
            }
        }

        public class AddIntegers
        {
            public static int AddTwoNumbers( int number1, int number2 )
            {
                return number1 + number2;
            }

            public static int AddThreeNumbers( int number1, int number2, int number3 )
            {
                return number1 + number2 + number3;
            }

            public static int AddFourNumbers( int number1, int number2, int number3, int number4 )
            {
                return number1 + number2 + number3 + number4;
            }

            public static int AddFiveNumbers( int number1, int number2, int number3, int number4, int number5 )
            {
                return number1 + number2 + number3 + number4 + number5;
            }

            public static int AddSixNumbers( int number1, int number2, int number3, int number4, int number5, int number6 )
            {
                return number1 + number2 + number3 + number4 + number5 + number6;
            }

            public static int AddSevenNumbers( int number1, int number2, int number3, int number4, int number5, int number6, int number7 )
            {
                return number1 + number2 + number3 + number4 + number5 + number6 + number7;
            }

            public static int AddEightNumbers( int number1, int number2, int number3, int number4, int number5, int number6, int number7, int number8 )
            {
                return number1 + number2 + number3 + number4 + number5 + number6 + number7 + number8;
            }

            public static int AddNineNumbers( int number1, int number2, int number3, int number4, int number5, int number6, int number7, int number8, int number9 )
            {
                return number1 + number2 + number3 + number4 + number5 + number6 + number7 + number8 + number9;
            }

            public static int AddTenNumbers( int number1, int number2, int number3, int number4, int number5, int number6, int number7, int number8, int number9, int number10 )
            {
                return number1 + number2 + number3 + number4 + number5 + number6 + number7 + number8 + number9 + number10;
            }

            public static int AddElevenNumbers( int number1, int number2, int number3, int number4, int number5, int number6, int number7, int number8, int number9, int number10, int number11 )
            {
                return number1 + number2 + number3 + number4 + number5 + number6 + number7 + number8 + number9 + number10 + number11;
            }

            public static int AddTwelveNumbers( int number1, int number2, int number3, int number4, int number5, int number6, int number7, int number8, int number9, int number10, int number11, int number12 )
            {
                return number1 + number2 + number3 + number4 + number5 + number6 + number7 + number8 + number9 + number10 + number11 + number12;
            }

            public static int AddThirteenNumbers( int number1, int number2, int number3, int number4, int number5, int number6, int number7, int number8, int number9, int number10, int number11, int number12, int number13 )
            {
                return number1 + number2 + number3 + number4 + number5 + number6 + number7 + number8 + number9 + number10 + number11 + number12 + number13;
            }

            public static int AddFourteenNumbers( int number1, int number2, int number3, int number4, int number5, int number6, int number7, int number8, int number9, int number10, int number11, int number12, int number13, int number14 )
            {
                return number1 + number2 + number3 + number4 + number5 + number6 + number7 + number8 + number9 + number10 + number11 + number12 + number13 + number14;
            }

            public static int AddFifteenNumbers( int number1, int number2, int number3, int number4, int number5, int number6, int number7, int number8, int number9, int number10, int number11, int number12, int number13, int number14, int number15 )
            {
                return number1 + number2 + number3 + number4 + number5 + number6 + number7 + number8 + number9 + number10 + number11 + number12 + number13 + number14 + number15;
            }
        }
    }
}