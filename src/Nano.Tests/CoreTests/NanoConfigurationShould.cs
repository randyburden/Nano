using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Net;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Nano.Tests.CoreTests
{
    [TestFixture]
    public class NanoConfigurationShould
    {
        [Test]
        public void Ensure_Default_DerivedClass_Path_Correct()
        {
            using (var server = NanoTestServer.Start())
            {
                // Arrange
                server.NanoConfiguration.AddMethods<DerivedClass>();
                var someBool = true.ToString().ToLower();

                // Act
                var response = HttpHelper.GetResponseString(server.GetUrl() + "/api/DerivedClass/DerivedClassBool?somebool=" + someBool);

                // Visual Assertion
                Trace.WriteLine(response);

                // Assert
                Assert.That(response.Contains(someBool));
            }
        }

        [Test]
        public void Ensure_Default_InnerClass_Path_Correct()
        {
            using (var server = NanoTestServer.Start())
            {
                // Arrange
                server.NanoConfiguration.AddMethods<NanoConfigurationShould.InnerClass>();
                var someBool = true.ToString().ToLower();

                // Act
                var response = HttpHelper.GetResponseString(server.GetUrl() + "/api/NanoConfigurationShould/InnerClass/InnerClassBool?somebool=" + someBool);

                // Visual Assertion
                Trace.WriteLine(response);

                // Assert
                Assert.That(response.Contains(someBool));
            }
        }

        public class InnerClass
        {
            public static bool InnerClassBool(bool someBool)
            {
                return someBool;
            }
        }
    }
    public class DerivedClass
    {
        public static bool DerivedClassBool(bool someBool)
        {
            return someBool;
        }
    }
}