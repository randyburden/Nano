namespace Nano.Demo.TopshelfSelfHost
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
        public static object GetContext( dynamic nanoContext )
        {
            return new
            {
                nanoContext.Request.Url,
                HttpMethod = nanoContext.Request.HttpMethod
            };
        }

        /// <summary>
        /// Creates a <span style="font-weight: bold;">customer</span>. Wiki article: <a href="https://wiki.ambitenergy.com/wiki/5378/developer-protocol-sdlc">Weston's Rant</a>
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
        /// Creates a customer.
        /// </summary>
        /// <param name="customer">Customer model.</param>
        /// <returns>Customer.</returns>
        public static dynamic CreateDynamicCustomer( dynamic customer )
        {
            return new
            {
                customer.CustomerId,
                customer.FirstName,
                customer.LastName
            };
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
    }
}