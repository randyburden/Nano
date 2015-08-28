using System;
using System.Diagnostics;
using System.IO;
using System.Net;
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

			using ( HttpListenerNanoServer.Start( config, ApiProxy.Configuration.BaseApiUrl ) )
			{
				Trace.WriteLine( "CreateCustomer" );
				var createCustomerResponse = ApiProxy.Customer.CreateCustomer( "Clark", "Kent" );
				Trace.WriteLine( JsonConvert.SerializeObject( createCustomerResponse ) );
				Assert.That( createCustomerResponse.FirstName == "Clark" && createCustomerResponse.LastName == "Kent" );
				Trace.WriteLine( "" );

				Trace.WriteLine( "GetPerson" );
				var getPersonResponse = ApiProxy.Customer.GetPerson( 123 );
				Trace.WriteLine( JsonConvert.SerializeObject( getPersonResponse ) );
				Assert.That( getPersonResponse.FirstName == "Clark" && getPersonResponse.LastName == "Kent" && getPersonResponse.Addresses.Count == 2 );
				Trace.WriteLine( "" );

				Trace.WriteLine( "GetCustomer" );
				dynamic getCustomerResponse = ApiProxy.Customer.GetCustomer( 123 );
				string getCustomerResponseJson = JsonConvert.SerializeObject( getCustomerResponse );
				Trace.WriteLine( getCustomerResponseJson );
				Assert.That( getCustomerResponse.FirstName == "Clark" && getCustomerResponse.LastName == "Kent" );
				Trace.WriteLine( "" );

				Trace.WriteLine( "GetContext" );
				dynamic getContextResponse = ApiProxy.Customer.GetContext();
				string getContextResponseJson = JsonConvert.SerializeObject( getContextResponse );
				Trace.WriteLine( getContextResponseJson );
				Assert.That( getContextResponse.Request.HttpMethod == "POST" );
				Trace.WriteLine( "" );

                Trace.WriteLine( "GetCorrelationId" );
			    var correlationId = Guid.NewGuid().ToString();
                string getCorrelationIdResponse = ApiProxy.Customer.GetCorrelationId( correlationId );
                Trace.WriteLine( getCorrelationIdResponse );
                Assert.That( correlationId == getCorrelationIdResponse );
                Trace.WriteLine( "" );

				Trace.WriteLine( "CreatePendingCustomer" );
				var createPendingCustomerResponse = ApiProxy.Customer.CreatePendingCustomer( "Clark", "Kent" );
				Trace.WriteLine( JsonConvert.SerializeObject( createPendingCustomerResponse ) );
				Assert.That( createPendingCustomerResponse.FirstName == "Clark" && createPendingCustomerResponse.LastName == "Kent" );
				Trace.WriteLine( "" );

				Trace.WriteLine( "CreateDynamicCustomer" );
				dynamic createDynamicCustomerResponse = ApiProxy.Customer.CreateDynamicCustomer( new { CustomerId = 1, FirstName = "Clark", LastName = "Kent" } );
				string createDynamicCustomerResponseJson = JsonConvert.SerializeObject( createDynamicCustomerResponse );
				Trace.WriteLine( createDynamicCustomerResponseJson );
				Assert.That( createDynamicCustomerResponse.FirstName == "Clark" && createDynamicCustomerResponse.LastName == "Kent" );
				Trace.WriteLine( "" );
			}
		}
	}

    public class ApiProxy
    {
        /*** This is a Nano auto-generated C# client proxy created from the following url: http://localhost:4545/ApiExplorer/?GenerateCSharpProxy=True ***/

        /// <summary>Configuration settings and functions for the proxy.</summary>
        public static class Configuration
        {
            /// <summary>The base API URL.</summary>
            public static string BaseApiUrl = "http://localhost:4545";

            /// <summary>Gets or sets the length of time, in milliseconds, before the request times out. The default value is 300,000 milliseconds (5 minutes).</summary>
            public static int Timeout = 300000;

            /// <summary>Serializes the given object to a string.</summary>
            public static Func<object, string> Serialize = obj =>
            {
                if (obj is string) return obj.ToString();
                if (obj == null) return null;
                return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            };

            /// <summary>Deserializes the given string to an object of the given type.</summary>
            public static Func<string, Type, object> Deserialize = (str, type) => Newtonsoft.Json.JsonConvert.DeserializeObject(str, type);

            /// <summary>Gets a new WebClient.</summary>
            public static Func<WebClient> GetWebClient = () => new ProxyWebClient();

            /// <summary>Proxy Web Client.</summary>
            public class ProxyWebClient : WebClient
            {
                protected override WebRequest GetWebRequest(Uri uri)
                {
                    WebRequest webRequest = base.GetWebRequest(uri);
                    webRequest.Timeout = Timeout;
                    return webRequest;
                }
            }
        }

        public static class Customer
        {
            /// <summary>Creates the customer.</summary>
            /// <param name="firstName">The first name.</param>
            /// <param name="lastName">The last name.</param>
            public static CustomerModel CreateCustomer(String firstName, String lastName, object correlationId = null)
            {
                var parameters = new System.Collections.Specialized.NameValueCollection { { "firstName", Configuration.Serialize(firstName) }, { "lastName", Configuration.Serialize(lastName) } };
                return PostJson<CustomerModel>(Configuration.BaseApiUrl + "/api/Customer/CreateCustomer", parameters, correlationId);
            }

            /// <summary>Updates the customer.</summary>
            /// <param name="customerModel">The customer model.</param>
            public static CustomerModel UpdateCustomer(CustomerModel customerModel, object correlationId = null)
            {
                var parameters = new System.Collections.Specialized.NameValueCollection { { "customerModel", Configuration.Serialize(customerModel) } };
                return PostJson<CustomerModel>(Configuration.BaseApiUrl + "/api/Customer/UpdateCustomer", parameters, correlationId);
            }

            /// <summary>Gets a person by id.</summary>
            /// <param name="personId">The person identifier.</param>
            public static Person GetPerson(Int32 personId, object correlationId = null)
            {
                var parameters = new System.Collections.Specialized.NameValueCollection { { "personId", Configuration.Serialize(personId) } };
                return PostJson<Person>(Configuration.BaseApiUrl + "/api/Customer/GetPerson", parameters, correlationId);
            }

            /// <summary>Gets a customer.</summary>
            /// <param name="customerNbr">The customer number.</param>
            public static Object GetCustomer(Int32 customerNbr, object correlationId = null)
            {
                var parameters = new System.Collections.Specialized.NameValueCollection { { "customerNbr", Configuration.Serialize(customerNbr) } };
                return PostJson<Object>(Configuration.BaseApiUrl + "/api/Customer/GetCustomer", parameters, correlationId);
            }

            /// <summary>Returns NanoContext stuff.</summary>
            public static Object GetContext(object correlationId = null)
            {
                var parameters = new System.Collections.Specialized.NameValueCollection { };
                return PostJson<Object>(Configuration.BaseApiUrl + "/api/Customer/GetContext", parameters, correlationId);
            }

            /// <summary>Gets the Correlation Id passed to the operation or generated by the request.</summary>
            public static String GetCorrelationId(object correlationId = null)
            {
                var parameters = new System.Collections.Specialized.NameValueCollection { };
                return PostJson<String>(Configuration.BaseApiUrl + "/api/Customer/GetCorrelationId", parameters, correlationId);
            }

            /// <summary>Creates a <span style="font-weight: bold;">customer</span>.
            /// Some cool link relevant to this operation: <a href="https://github.com/AmbitEnergyLabs/Nano">Nano Github Homepage</a><p onclick="alert('Yo dog, I heard you like JavaScript so I put JavaScript in your HTML description in your XML method comments in your C# class!')">
            /// Look, there's HTML in my XML comments... <i>crazy!!!</i></p></summary>
            /// <param name="firstName">First name.</param>
            /// <param name="lastName">Last name.</param>
            public static CustomerModel CreatePendingCustomer(String firstName, String lastName, object correlationId = null)
            {
                var parameters = new System.Collections.Specialized.NameValueCollection { { "firstName", Configuration.Serialize(firstName) }, { "lastName", Configuration.Serialize(lastName) } };
                return PostJson<CustomerModel>(Configuration.BaseApiUrl + "/api/Customer/CreatePendingCustomer", parameters, correlationId);
            }

            /// <summary>Delays a response by a given number of seconds.</summary>
            /// <param name="delayInSeconds">Number of seconds to delay before responding.</param>
            public static Int32 DelayedResponse(Int32 delayInSeconds, object correlationId = null)
            {
                var parameters = new System.Collections.Specialized.NameValueCollection { { "delayInSeconds", Configuration.Serialize(delayInSeconds) } };
                return PostJson<Int32>(Configuration.BaseApiUrl + "/api/Customer/DelayedResponse", parameters, correlationId);
            }

            /// <summary>Creates a customer.</summary>
            /// <param name="customer">Customer model.</param>
            public static Object CreateDynamicCustomer(Object customer, object correlationId = null)
            {
                var parameters = new System.Collections.Specialized.NameValueCollection { { "customer", Configuration.Serialize(customer) } };
                return PostJson<Object>(Configuration.BaseApiUrl + "/api/Customer/CreateDynamicCustomer", parameters, correlationId);
            }

            /// <summary>Downloads the customer Excel report.</summary>
            /// <param name="customerId">The customer id.</param>
            public static Stream DownloadCustomerExcelReport(Int32 customerId, object correlationId = null)
            {
                var parameters = new System.Collections.Specialized.NameValueCollection { { "customerId", Configuration.Serialize(customerId) } };
                return PostJson<Stream>(Configuration.BaseApiUrl + "/api/Customer/DownloadCustomerExcelReport", parameters, correlationId);
            }
        }

        public static class Customer2
        {
            /// <summary>Creates the customer.</summary>
            /// <param name="firstName">The first name.</param>
            /// <param name="lastName">The last name.</param>
            public static CustomerModel CreateCustomer(String firstName, String lastName, object correlationId = null)
            {
                var parameters = new System.Collections.Specialized.NameValueCollection { { "firstName", Configuration.Serialize(firstName) }, { "lastName", Configuration.Serialize(lastName) } };
                return PostJson<CustomerModel>(Configuration.BaseApiUrl + "/api/Customer2/CreateCustomer", parameters, correlationId);
            }
        }


        private static T PostJson<T>(string url, System.Collections.Specialized.NameValueCollection parameters, object correlationId = null)
        {
            using (var client = Configuration.GetWebClient())
            {
                if (correlationId == null)
                    correlationId = System.Runtime.Remoting.Messaging.CallContext.LogicalGetData("X-CorrelationId");

                if (correlationId != null)
                    client.Headers.Add("X-CorrelationId", correlationId.ToString());

                byte[] responsebytes = client.UploadValues(url, "POST", parameters);
                string responsebody = Encoding.UTF8.GetString(responsebytes);
                var obj = (T)Configuration.Deserialize(responsebody, typeof(T));
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