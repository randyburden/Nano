using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Nano.Tests
{
    [TestFixture]
    public class ProxyTests
    {
        [Test]
        public void TestAdd()
        {
            Trace.WriteLine( ApiProxy.Add( 1, 2 ) );
            Trace.WriteLine( ApiProxy.CreateCustomer( "Clark", "Kent" ) );
            Trace.WriteLine( ApiProxy.GetCustomer( 1 ) );
            Trace.WriteLine( ApiProxy.GetContext() );
            Trace.WriteLine( JsonConvert.SerializeObject( ApiProxy.CreatePendingCustomer( "Bob", "Smith" ) ) );
            dynamic customer = new ExpandoObject();
            customer.CustomerId = 5000;
            customer.FirstName = "Ray";
            customer.LastName = "Man";
            var result = ApiProxy.CreateDynamicCustomer( customer );
            string stringResult = result.ToString();
            Trace.WriteLine( stringResult );
            Trace.WriteLine( JsonConvert.SerializeObject( ApiProxy.AddUser( new ApiProxy.UserModel { UserId = 8, UserName = "Superman" } ) ) );
        }
    }

    public class ApiProxy
    {
        /*** Nano-generated web api proxy. Edit at your own discretion. ***/
        /*** This is an auto-generated web api proxy generated from the following url: http://localhost:4545/ApiExplorer/ ***/
        public static string BaseApiUrl = "http://localhost:4545";

        public static Int32 Add( Int32 number1, Nullable<Int32> number2 )
        {
            var parameters = new System.Collections.Specialized.NameValueCollection { { "number1", Newtonsoft.Json.JsonConvert.SerializeObject( number1 ) }, { "number2", Newtonsoft.Json.JsonConvert.SerializeObject( number2 ) } };
            return PostJson<Int32>( BaseApiUrl + "/api/customer/add", parameters );
        }

        public static CustomerModel CreateCustomer( String firstName, String lastName )
        {
            var parameters = new System.Collections.Specialized.NameValueCollection { { "firstName", Newtonsoft.Json.JsonConvert.SerializeObject( firstName ) }, { "lastName", Newtonsoft.Json.JsonConvert.SerializeObject( lastName ) } };
            return PostJson<CustomerModel>( BaseApiUrl + "/api/customer/createcustomer", parameters );
        }

        public static Object GetCustomer( Int32 customerNbr )
        {
            var parameters = new System.Collections.Specialized.NameValueCollection { { "customerNbr", Newtonsoft.Json.JsonConvert.SerializeObject( customerNbr ) } };
            return PostJson<Object>( BaseApiUrl + "/api/customer/getcustomer", parameters );
        }

        public static Object GetContext()
        {
            var parameters = new System.Collections.Specialized.NameValueCollection { };
            return PostJson<Object>( BaseApiUrl + "/api/customer/getcontext", parameters );
        }

        public static CustomerModel CreatePendingCustomer( String firstName, String lastName )
        {
            var parameters = new System.Collections.Specialized.NameValueCollection { { "firstName", Newtonsoft.Json.JsonConvert.SerializeObject( firstName ) }, { "lastName", Newtonsoft.Json.JsonConvert.SerializeObject( lastName ) } };
            return PostJson<CustomerModel>( BaseApiUrl + "/api/customer/creatependingcustomer", parameters );
        }

        public static Object CreateDynamicCustomer( Object customer )
        {
            var parameters = new System.Collections.Specialized.NameValueCollection { { "customer", Newtonsoft.Json.JsonConvert.SerializeObject( customer ) } };
            return PostJson<Object>( BaseApiUrl + "/api/customer/createdynamiccustomer", parameters );
        }

        public static UserModel AddUser( UserModel user )
        {
            var parameters = new System.Collections.Specialized.NameValueCollection { { "user", Newtonsoft.Json.JsonConvert.SerializeObject( user ) } };
            return PostJson<UserModel>( BaseApiUrl + "/api/customer/adduser", parameters );
        }

        private static T PostJson<T>( string url, System.Collections.Specialized.NameValueCollection parameters )
        {
            using( var client = new System.Net.WebClient() )
            {
                byte[] responsebytes = client.UploadValues( url, "POST", parameters );
                string responsebody = Encoding.UTF8.GetString( responsebytes );
                var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<T>( responsebody );
                return obj;
            }
        }
        #region Nested Types
        public class CustomerModel
        {
            public Int32 CustomerId;
            public String FirstName;
            public String LastName;
        }

        public class UserModel
        {
            public Int32 UserId;
            public String UserName;
        }

        #endregion Nested Types
    }
}