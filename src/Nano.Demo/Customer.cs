using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using Nano.Web.Core;

namespace Nano.Demo
{
    /// <summary>
    /// Customer API.
    /// </summary>
    public class Customer
    {
        /// <summary>
        /// Creates the customer.
        /// </summary>
        /// <param name="firstName">The first name.</param>
        /// <param name="lastName">The last name.</param>
        /// <returns>Customer.</returns>
        public static CustomerModel CreateCustomer( string firstName, string lastName )
        {
            return new CustomerModel
            {
                CustomerId = 1,
                FirstName = firstName,
                LastName = lastName
            };
        }

        /// <summary>
        /// Updates the customer.
        /// </summary>
        /// <param name="customerModel">The customer model.</param>
        /// <returns>The updated customer model.</returns>
        public static CustomerModel UpdateCustomer( CustomerModel customerModel )
        {
            if ( customerModel == null )
                throw new ArgumentNullException( "customerModel" );

            return customerModel;
        }

        /// <summary>
        /// Gets a person by id.
        /// </summary>
        /// <param name="personId">The person identifier.</param>
        /// <returns>Person.</returns>
        public static Person GetPerson( int personId )
        {
            return new Person
            {
                PersonId = personId,
                FirstName = "Clark",
                LastName = "Kent",
                Addresses = new List<Address>
                {
                    new Address
                    {
                        AddressId = 1,
                        Address1 = "100 Sweet Street",
                        Address2 = "",
                        City = "Metropolis",
                        State = "NY",
                        ZipCode = "10548"
                    },
                    new Address
                    {
                        AddressId = 1,
                        Address1 = "200 Sweet Street",
                        Address2 = "",
                        City = "Metropolis",
                        State = "NY",
                        ZipCode = "10548"
                    }
                }
            };
        }

        /// <summary>
        /// Gets a customer.
        /// </summary>
        /// <param name="customerNbr">The customer number.</param>
        /// <returns>Customer object.</returns>
        public static object GetCustomer( int customerNbr )
        {
            return new
            {
                CustomerNbr = customerNbr,
                FirstName = "Clark",
                LastName = "Kent"
            };
        }

        /// <summary>
        /// Returns NanoContext stuff.
        /// </summary>
        /// <param name="nanoContext">The nano context.</param>
        /// <returns>NanoContext stuff.</returns>
        public static object GetContext( NanoContext nanoContext )
        {
            Func<System.Collections.Specialized.NameValueCollection, Dictionary<string, object>> nameValueCollectionToDictionary = collection =>
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>();

                foreach ( string parameterName in collection )
                {
                    dictionary.Add( parameterName, collection[ parameterName ] );
                }

                return dictionary;
            };

            return new
            {
                Request = new
                {
                    nanoContext.Request.HttpMethod,
                    QueryStringParameters = nameValueCollectionToDictionary( nanoContext.Request.QueryStringParameters ),
                    FormBodyParameters = nameValueCollectionToDictionary( nanoContext.Request.FormBodyParameters ),
                    HeaderParameters = nameValueCollectionToDictionary( nanoContext.Request.HeaderParameters )
                },
                NanoConfiguration = new
                {
                    nanoContext.NanoConfiguration.ApplicationRootFolderPath,
                    BackgroundTasks = nanoContext.NanoConfiguration.BackgroundTasks.Select( task => task == null ? null : new
                    {
                        task.Name,
                        task.AllowOverlappingRuns,
                        task.MillisecondInterval,
                        BackgroundTaskRunHistory = task.BackgroundTaskRunHistory.Select( context => context == null ? null : new
                        {
                            context.StartDateTime,
                            context.EndDateTime,
                            context.TaskResult
                        })
                    }),
                    RequestHandlers = nanoContext.NanoConfiguration.RequestHandlers.Select( handler => new
                    {
                        handler.UrlPath,
                        Type = handler.GetType().Name,
                        PreInvokeHandlers = handler.EventHandler.PreInvokeHandlers.Select( invokeHandler => new
                        {
                            invokeHandler.Method.Name
                        }),
                        PostInvokeHandlers = handler.EventHandler.PostInvokeHandlers.Select(invokeHandler => new
                        {
                            invokeHandler.Method.Name
                        }),
                        UnhandledExceptionHandlers = handler.EventHandler.UnhandledExceptionHandlers.Select(invokeHandler => new
                        {
                            invokeHandler.Method.Name
                        })
                    }),
                }
            };
        }

        /// <summary>
        /// Gets the Correlation Id passed to the operation or generated by the request.
        /// </summary>
        /// <returns>The correlation identifier.</returns>
        public static string GetCorrelationId()
        {
            var correlationId = CallContext.LogicalGetData( "X-CorrelationId" );

            return correlationId == null ? "No CorrelationId found" : correlationId.ToString();
        }

        /// <summary>
        /// Creates a <span style="font-weight: bold;">customer</span>. 
        /// Some cool link relevant to this operation: <a href="https://github.com/AmbitEnergyLabs/Nano">Nano Github Homepage</a>
        /// <p onclick="alert('Yo dog, I heard you like JavaScript so I put JavaScript in your HTML description in your XML method comments in your C# class!')">
        ///     Look, there's HTML in my XML comments... <i>crazy!!!</i>
        /// </p>
        /// </summary>
        /// <param name="firstName">First name.</param>
        /// <param name="lastName">Last name.</param>
        /// <returns>Customer.</returns>
        public static CustomerModel CreatePendingCustomer( string firstName, string lastName = null )
        {
            return new CustomerModel
            {
                CustomerId = 1,
                FirstName = firstName,
                LastName = lastName
            };
        }

        /// <summary>
        /// Delays a response by a given number of seconds.
        /// </summary>
        /// <param name="delayInSeconds">Number of seconds to delay before responding.</param>
        /// <returns>Delay in seconds.</returns>
        public static int DelayedResponse( int delayInSeconds = 3 )
        {
            Thread.Sleep( delayInSeconds * 1000 );

            return delayInSeconds;
        }

        /// <summary>
        /// Throws the given number of nested exceptions. The default is 3.
        /// </summary>
        /// <param name="numberOfInnerExceptions">Number of nested exceptions to throw.</param>
        /// <returns>This method always throws an exception.</returns>
        public static int ThrowException( int numberOfInnerExceptions = 3 )
        {
            if ( numberOfInnerExceptions < 1 )
                numberOfInnerExceptions = 1;

            Exception exception = null;

            if ( numberOfInnerExceptions >= 1 )
            {
                for ( int i = 1; i <= numberOfInnerExceptions; i++ )
                {
                    try
                    {
                        if ( exception != null )
                            throw new Exception( "This is exception number " + i, exception );

                        throw new Exception( "This is exception number " + i );
                    }
                    catch ( Exception e )
                    {
                        exception = e;
                    }
                }
            }

            throw exception;
        }

        /// <summary>
        /// Creates a customer.
        /// </summary>
        /// <param name="customer">Customer model.</param>
        /// <returns>Customer.</returns>
        public static dynamic CreateDynamicCustomer( dynamic customer )
        {
            if ( customer == null )
                throw new ArgumentNullException( "customer" );

            return new
            {
                customer.CustomerId,
                customer.FirstName,
                customer.LastName
            };
        }

        /// <summary>
        /// Returns the details of the files uploaded.
        /// </summary>
        /// <param name="nanoContext">The Nano context.</param>
        /// <returns>Uploaded file details.</returns>
        public static dynamic GetUploadedFilesDetails( NanoContext nanoContext )
        {
            if ( nanoContext.Request.Files.Count == 0 )
                throw new Exception( "No file uploaded" );

            var results = new List<object>();

            foreach ( var file in nanoContext.Request.Files )
            {
                results.Add( new
                {
                    file.ContentType,
                    file.FileName,
                    file.Name,
                    FileLength = file.Value.Length
                } );
            }

            return results;
        }

        /// <summary>
        /// Echos back the uploaded file to the client.
        /// </summary>
        /// <param name="nanoContext">The Nano context.</param>
        public static void EchoUploadedFile( NanoContext nanoContext )
        {
            if ( nanoContext.Request.Files.Count == 0 )
                throw new Exception( "No file uploaded" );

            var file = nanoContext.Request.Files.FirstOrDefault();

            if ( file == null )
                throw new Exception( "No file uploaded" );

            nanoContext.Response.ContentType = file.ContentType;
            nanoContext.Response.HeaderParameters.Add( "Content-Disposition", "attachment; filename=" + file.FileName );
            nanoContext.Response.ResponseStreamWriter = stream =>
            {
                file.Value.CopyTo( stream );
            };
        }

        /// <summary>
        /// Downloads the customer Excel report.
        /// </summary>
        /// <param name="nanoContext">The Nano context.</param>
        /// <param name="customerId">The customer id.</param>
        public static Stream DownloadCustomerExcelReport( dynamic nanoContext, int customerId )
        {
            var htmlTable = @"
<table>
    <thead>
        <tr style=""background-color: yellow;"">
            <th>Customer Id</th>
            <th>First Name</th>
            <th>Last Name</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td>" + customerId + @"</td>
            <td>Bob</td>
            <td>Smith</td>
        </tr>
    </tbody>
</table>
";

            nanoContext.Response.ContentType = "application/vnd.ms-excel";
            nanoContext.Response.HeaderParameters.Add( "Content-Disposition", "attachment; filename=CustomerReport-" + customerId + ".xls" );
            return new MemoryStream( Encoding.UTF8.GetBytes( htmlTable ) );
        }

        /// <summary>
        /// Customer.
        /// </summary>
        public class CustomerModel
        {
            /// <summary>
            /// The customer identifier.
            /// </summary>
            public int CustomerId;

            /// <summary>
            /// First name.
            /// </summary>
            public string FirstName;

            /// <summary>
            /// Last name.
            /// </summary>
            public string LastName;
        }

        /// <summary>
        /// Person.
        /// </summary>
        public class Person
        {
            /// <summary>
            /// The person identifier.
            /// </summary>
            public int PersonId;

            /// <summary>
            /// First name.
            /// </summary>
            public string FirstName;

            /// <summary>
            /// Last name.
            /// </summary>
            public string LastName;

            /// <summary>
            /// The persons list of addresses.
            /// </summary>
            public IList<Address> Addresses = new List<Address>();
        }

        /// <summary>
        /// Address.
        /// </summary>
        public class Address
        {
            /// <summary>
            /// The address identifier.
            /// </summary>
            public int AddressId;

            /// <summary>
            /// The address line 1.
            /// </summary>
            public string Address1;

            /// <summary>
            /// The address line 2.
            /// </summary>
            public string Address2;

            /// <summary>
            /// The city.
            /// </summary>
            public string City;

            /// <summary>
            /// The state.
            /// </summary>
            public string State;

            /// <summary>
            /// The zip code.
            /// </summary>
            public string ZipCode;
        }
    }

    /// <summary>
    /// This is an example of a near duplicate class to test that the ApiExplorer
    /// will properly handle duplicate method names within different classes.
    /// </summary>
    public class Customer2
    {
        /// <summary>
        /// Creates the customer.
        /// </summary>
        /// <param name="firstName">The first name.</param>
        /// <param name="lastName">The last name.</param>
        /// <returns>Customer.</returns>
        public static Customer.CustomerModel CreateCustomer( string firstName, string lastName )
        {
            return new Customer.CustomerModel
            {
                CustomerId = 1,
                FirstName = firstName,
                LastName = lastName
            };
        }
        
        /// <summary>
        /// This is a void method with no inputs that does nothing.
        /// </summary>
        public static void DoNothing()
        {
            // Do nothing to test a void method
        }
    }
}