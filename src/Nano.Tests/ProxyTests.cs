using System;
using System.Diagnostics;
using System.Text;
using Nano.Demo;
using Nano.Web.Core;
using Nano.Web.Core.Host.HttpListener;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Nano.Tests
{
	[TestFixture]
	public class ProxyTests
	{
		[Test]
		public void Test_Proxy_Generated_Code()
		{
			var config = new NanoConfiguration();
			config.AddMethods<Customer>( "/api/customer/" );

			using ( HttpListenerNanoServer.Start( config, ApiProxy.BaseApiUrl ) )
			{
				Trace.WriteLine( "CreateCustomer" );
				var createCustomerResponse = ApiProxy.CreateCustomer( "Clark", "Kent" );
				Trace.WriteLine( JsonConvert.SerializeObject( createCustomerResponse ) );
				Assert.That( createCustomerResponse.FirstName == "Clark" && createCustomerResponse.LastName == "Kent" );
				Trace.WriteLine( "" );

				Trace.WriteLine( "GetPerson" );
				var getPersonResponse = ApiProxy.GetPerson( 123 );
				Trace.WriteLine( JsonConvert.SerializeObject( getPersonResponse ) );
				Assert.That( getPersonResponse.FirstName == "Clark" && getPersonResponse.LastName == "Kent" && getPersonResponse.Addresses.Count == 2 );
				Trace.WriteLine( "" );

				Trace.WriteLine( "GetCustomer" );
				dynamic getCustomerResponse = ApiProxy.GetCustomer( 123 );
				string getCustomerResponseJson = JsonConvert.SerializeObject( getCustomerResponse );
				Trace.WriteLine( getCustomerResponseJson );
				Assert.That( getCustomerResponse.FirstName == "Clark" && getCustomerResponse.LastName == "Kent" );
				Trace.WriteLine( "" );

				Trace.WriteLine( "GetContext" );
				dynamic getContextResponse = ApiProxy.GetContext();
				string getContextResponseJson = JsonConvert.SerializeObject( getContextResponse );
				Trace.WriteLine( getContextResponseJson );
				Assert.That( getContextResponse.Url != null && getContextResponse.HttpMethod == "POST" );
				Trace.WriteLine( "" );

                Trace.WriteLine( "GetCorrelationId" );
			    var correlationId = Guid.NewGuid().ToString();
                string getCorrelationIdResponse = ApiProxy.GetCorrelationId( correlationId );
                Trace.WriteLine( getCorrelationIdResponse );
                Assert.That( correlationId == getCorrelationIdResponse );
                Trace.WriteLine( "" );

				Trace.WriteLine( "CreatePendingCustomer" );
				var createPendingCustomerResponse = ApiProxy.CreatePendingCustomer( "Clark", "Kent" );
				Trace.WriteLine( JsonConvert.SerializeObject( createPendingCustomerResponse ) );
				Assert.That( createPendingCustomerResponse.FirstName == "Clark" && createPendingCustomerResponse.LastName == "Kent" );
				Trace.WriteLine( "" );

				Trace.WriteLine( "CreateDynamicCustomer" );
				dynamic createDynamicCustomerResponse = ApiProxy.CreateDynamicCustomer( new { CustomerId = 1, FirstName = "Clark", LastName = "Kent" } );
				string createDynamicCustomerResponseJson = JsonConvert.SerializeObject( createDynamicCustomerResponse );
				Trace.WriteLine( createDynamicCustomerResponseJson );
				Assert.That( createDynamicCustomerResponse.FirstName == "Clark" && createDynamicCustomerResponse.LastName == "Kent" );
				Trace.WriteLine( "" );
			}
		}
	}

    public class ApiProxy
    {
        /*** This is a Nano auto-generated C# client proxy created from the following url: http://localhost/Nano.Demo.Mvc4/ApiExplorer/?GenerateCSharpProxy=True ***/

        /// <summary>The base API URL.</summary>
        public static string BaseApiUrl = "http://localhost:4545";

        /// <summary>Serializes the given object to a string.</summary>
        public static Func<object, string> Serialize = obj =>
        {
            if( obj is string ) return obj.ToString();
            return JsonConvert.SerializeObject( obj );
        };

        /// <summary>Deserializes the given string to an object of the given type.</summary>
        public static Func<string, Type, object> Deserialize = ( str, type ) => Newtonsoft.Json.JsonConvert.DeserializeObject( str, type );

        public static CustomerModel CreateCustomer( String firstName, String lastName, object correlationId = null )
        {
            var parameters = new System.Collections.Specialized.NameValueCollection { { "firstName", Serialize( firstName ) }, { "lastName", Serialize( lastName ) } };
            return PostJson<CustomerModel>( BaseApiUrl + "/api/customer/createcustomer", parameters, correlationId );
        }

        public static CustomerModel UpdateCustomer( CustomerModel customerModel, object correlationId = null )
        {
            var parameters = new System.Collections.Specialized.NameValueCollection { { "customerModel", Serialize( customerModel ) } };
            return PostJson<CustomerModel>( BaseApiUrl + "/api/customer/updatecustomer", parameters, correlationId );
        }

        public static Person GetPerson( Int32 personId, object correlationId = null )
        {
            var parameters = new System.Collections.Specialized.NameValueCollection { { "personId", Serialize( personId ) } };
            return PostJson<Person>( BaseApiUrl + "/api/customer/getperson", parameters, correlationId );
        }

        public static Object GetCustomer( Int32 customerNbr, object correlationId = null )
        {
            var parameters = new System.Collections.Specialized.NameValueCollection { { "customerNbr", Serialize( customerNbr ) } };
            return PostJson<Object>( BaseApiUrl + "/api/customer/getcustomer", parameters, correlationId );
        }

        public static Object GetContext( object correlationId = null )
        {
            var parameters = new System.Collections.Specialized.NameValueCollection { };
            return PostJson<Object>( BaseApiUrl + "/api/customer/getcontext", parameters, correlationId );
        }

        public static String GetCorrelationId( object correlationId = null )
        {
            var parameters = new System.Collections.Specialized.NameValueCollection { };
            return PostJson<String>( BaseApiUrl + "/api/customer/getcorrelationid", parameters, correlationId );
        }

        public static CustomerModel CreatePendingCustomer( String firstName, String lastName, object correlationId = null )
        {
            var parameters = new System.Collections.Specialized.NameValueCollection { { "firstName", Serialize( firstName ) }, { "lastName", Serialize( lastName ) } };
            return PostJson<CustomerModel>( BaseApiUrl + "/api/customer/creatependingcustomer", parameters, correlationId );
        }

        public static Object CreateDynamicCustomer( Object customer, object correlationId = null )
        {
            var parameters = new System.Collections.Specialized.NameValueCollection { { "customer", Serialize( customer ) } };
            return PostJson<Object>( BaseApiUrl + "/api/customer/createdynamiccustomer", parameters, correlationId );
        }

        private static T PostJson<T>( string url, System.Collections.Specialized.NameValueCollection parameters, object correlationId = null )
        {
            using( var client = new System.Net.WebClient() )
            {
                if( correlationId == null )
                    correlationId = System.Runtime.Remoting.Messaging.CallContext.LogicalGetData( "X-CorrelationId" );

                if( correlationId != null )
                    client.Headers.Add( "X-CorrelationId", correlationId.ToString() );

                byte[] responsebytes = client.UploadValues( url, "POST", parameters );
                string responsebody = Encoding.UTF8.GetString( responsebytes );
                var obj = (T)Deserialize( responsebody, typeof( T ) );
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

        public class Person
        {
            public Int32 PersonId;
            public String FirstName;
            public String LastName;
            public System.Collections.Generic.IList<Address> Addresses;
        }

        public class Address
        {
            public Int32 AddressId;
            public String Address1;
            public String Address2;
            public String City;
            public String State;
            public String ZipCode;
        }

        #endregion Nested Types
    }
}