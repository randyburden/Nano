using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Nano.Demo;
using Nano.Web.Core.Internal;
using NUnit.Framework;

namespace Nano.Tests.ModelBinder
{
    [TestFixture]
    public class GetParameterTypeNameShould
    {
        [Test]
        public void Return_A_Correctly_Formatted_Type_Name_For_String()
        {
            Type customerType = typeof(Customer);
            MethodInfo[] methods = customerType.GetMethods(BindingFlags.Public | BindingFlags.Static);

            var genericListMethod = methods.FirstOrDefault(x => x.Name == "CreatePendingCustomer");

            ParameterInfo parameterInfo = genericListMethod?.GetParameters()[0];
            var result = genericListMethod.GetParameterTypeName(parameterInfo);

            Trace.WriteLine(result);

            Assert.AreEqual("System.String", result);
        }

        [Test]
        public void Return_A_Correctly_Formatted_Type_Name_For_A_Custom_Type()
        {
            Type customerType = typeof(Customer);
            MethodInfo[] methods = customerType.GetMethods(BindingFlags.Public | BindingFlags.Static);

            var genericListMethod = methods.FirstOrDefault(x => x.Name == "GetUploadedFilesDetails");

            ParameterInfo parameterInfo = genericListMethod?.GetParameters()[0];
            var result = genericListMethod.GetParameterTypeName(parameterInfo);

            Trace.WriteLine(result);

            Assert.AreEqual("Nano.Web.Core.NanoContext", result);
        }

        [Test]
        public void Return_A_Correctly_Formatted_Type_Name_For_Generic_List()
        {
            Type customerType = typeof ( Customer );
            MethodInfo[] methods = customerType.GetMethods(BindingFlags.Public | BindingFlags.Static);

            var genericListMethod = methods.FirstOrDefault( x => x.Name == "TakeGenericListParameter" );

            ParameterInfo parameterInfo = genericListMethod?.GetParameters()[0];
            var result = genericListMethod.GetParameterTypeName( parameterInfo );

            Trace.WriteLine( result );

            Assert.AreEqual("System.Collections.Generic.List{System.Int32}", result);
        }

        [Test]
        public void Return_A_Correctly_Formatted_Type_Name_For_Dictionary()
        {
            Type customerType = typeof(Customer);
            MethodInfo[] methods = customerType.GetMethods(BindingFlags.Public | BindingFlags.Static);

            var genericListMethod = methods.FirstOrDefault(x => x.Name == "TakeDictionaryParameter");

            ParameterInfo parameterInfo = genericListMethod?.GetParameters()[0];
            var result = genericListMethod.GetParameterTypeName(parameterInfo);

            Trace.WriteLine(result);

            Assert.AreEqual("System.Collections.Generic.Dictionary{System.Int32,System.String}", result);
        }

        [Test]
        public void Return_A_Correctly_Formatted_Type_Name_For_A_List_Of_Lists()
        {
            Type customerType = typeof(Customer);
            MethodInfo[] methods = customerType.GetMethods(BindingFlags.Public | BindingFlags.Static);

            var genericListMethod = methods.FirstOrDefault(x => x.Name == "TakeListOfListParameter");

            ParameterInfo parameterInfo = genericListMethod?.GetParameters()[0];
            var result = genericListMethod.GetParameterTypeName(parameterInfo);

            Trace.WriteLine(result);

            Assert.AreEqual("System.Collections.Generic.List{System.Collections.Generic.List{System.Int32}}", result);
        }

        [Test]
        public void Return_A_Correctly_Formatted_Type_Name_For_Nested_Functions()
        {
            Type customerType = typeof(Customer);
            MethodInfo[] methods = customerType.GetMethods(BindingFlags.Public | BindingFlags.Static);

            var genericListMethod = methods.FirstOrDefault(x => x.Name == "TakeVeryNestedFuncParameter");

            ParameterInfo parameterInfo = genericListMethod?.GetParameters()[0];
            var result = genericListMethod.GetParameterTypeName(parameterInfo);

            Trace.WriteLine(result);

            Assert.AreEqual("System.Func{System.Func{System.Func{System.Func{System.Int32}}}}", result);
        }
    }
}
