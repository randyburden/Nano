using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Nano.Demo;
using Nano.Web.Core;
using Nano.Web.Core.Internal;
using Nano.Web.Core.Metadata;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Nano.Tests.ModelBinder
{
    [TestFixture]
    public class GetNanoMetaDataShould
    {
        [Test]
        public void Return_A_Correctly_Formatted_Type_Name_For_List_Of_Dictionary()
        {
            using (var server = NanoTestServer.Start())
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Customer>();

                // Act
                var response = HttpHelper.GetResponseString(server.GetUrl() + "/metadata/GetNanoMetadata");
                var deserializedResponse = server.NanoConfiguration.SerializationService.Deserialize<ApiMetadata>(response);

                var inputParam = deserializedResponse.Operations.FirstOrDefault( x => x.Name == "TakeListOfDictionarysParameter" )?.InputParameters.FirstOrDefault();

                Trace.WriteLine(inputParam);

                // Assert
                Assert.AreEqual("List<Dictionary<Int32,Object>>", inputParam?.Type );
            }
        }

        [Test]
        public void Return_A_Correctly_Formatted_Type_Name_For_Dictionary_Of_Dictionary()
        {
            using (var server = NanoTestServer.Start())
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Customer>();

                // Act
                var response = HttpHelper.GetResponseString(server.GetUrl() + "/metadata/GetNanoMetadata");
                var deserializedResponse = server.NanoConfiguration.SerializationService.Deserialize<ApiMetadata>(response);

                var inputParam = deserializedResponse.Operations.FirstOrDefault(x => x.Name == "TakeDictionaryOfDictionarysParameter")?.InputParameters.FirstOrDefault();

                Trace.WriteLine(inputParam);

                // Assert
                Assert.AreEqual("Dictionary<Int32,Dictionary<Int32,Object>>", inputParam?.Type);
            }
        }

        [Test]
        public void Return_A_Correctly_Formatted_Type_Name_For_A_Tuple()
        {
            using (var server = NanoTestServer.Start())
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Customer>();

                // Act
                var response = HttpHelper.GetResponseString(server.GetUrl() + "/metadata/GetNanoMetadata");
                var deserializedResponse = server.NanoConfiguration.SerializationService.Deserialize<ApiMetadata>(response);

                var inputParam = deserializedResponse.Operations.FirstOrDefault(x => x.Name == "TakeTupleParameter")?.InputParameters.FirstOrDefault();

                Trace.WriteLine(inputParam);

                // Assert
                Assert.AreEqual("Tuple<Int32,String,Object>", inputParam?.Type);
            }
        }

        [Test]
        public void Return_A_Correct_Default_Int_Value()
        {
            using (var server = NanoTestServer.Start())
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Customer>();

                // Act
                var response = HttpHelper.GetResponseString(server.GetUrl() + "/metadata/GetNanoMetadata");
                var deserializedResponse = server.NanoConfiguration.SerializationService.Deserialize<ApiMetadata>(response);

                var inputParam = deserializedResponse.Operations.FirstOrDefault(x => x.Name == "TakeAParameterThatHasADefaultValue")?.InputParameters.FirstOrDefault();

                Trace.WriteLine(inputParam);

                // Assert
                Assert.AreEqual(16, inputParam?.DefaultValue);
            }
        }

        [Test]
        public void Return_A_Correct_Default_String_Value()
        {
            using (var server = NanoTestServer.Start())
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Customer>();

                // Act
                var response = HttpHelper.GetResponseString(server.GetUrl() + "/metadata/GetNanoMetadata");
                var deserializedResponse = server.NanoConfiguration.SerializationService.Deserialize<ApiMetadata>(response);

                var inputParam = deserializedResponse.Operations.FirstOrDefault(x => x.Name == "TakeAStringThatHasADefaultValue")?.InputParameters.FirstOrDefault();

                Trace.WriteLine(inputParam);

                // Assert
                Assert.AreEqual("sixteen", inputParam?.DefaultValue);
            }
        }

        [Test]
        public void Return_A_Correct_Default_Boolean_Value()
        {
            using (var server = NanoTestServer.Start())
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Customer>();

                // Act
                var response = HttpHelper.GetResponseString(server.GetUrl() + "/metadata/GetNanoMetadata");
                var deserializedResponse = server.NanoConfiguration.SerializationService.Deserialize<ApiMetadata>(response);

                var inputParam = deserializedResponse.Operations.FirstOrDefault(x => x.Name == "TakeABoolThatHasADefaultValue")?.InputParameters.FirstOrDefault();

                Trace.WriteLine(inputParam);

                // Assert
                Assert.AreEqual(true, inputParam?.DefaultValue);
            }
        }

        [Test]
        public void Return_A_Correct_Default_Null_Value()
        {
            using (var server = NanoTestServer.Start())
            {
                // Arrange
                server.NanoConfiguration.AddMethods<Customer>();

                // Act
                var response = HttpHelper.GetResponseString(server.GetUrl() + "/metadata/GetNanoMetadata");
                var deserializedResponse = server.NanoConfiguration.SerializationService.Deserialize<ApiMetadata>(response);

                var inputParam = deserializedResponse.Operations.FirstOrDefault(x => x.Name == "TakeAStringThatHasADefaultValueOfNull")?.InputParameters.FirstOrDefault();

                Trace.WriteLine(inputParam);

                // Assert
                Assert.AreEqual(null, inputParam?.DefaultValue);
                Assert.AreEqual(true, inputParam?.HasDefaultValue);
            }
        }
    }
}