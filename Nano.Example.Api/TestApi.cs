using Nano.Core;

namespace Nano.Example.Api
{
    /// <summary>
    /// Provides method for testing Nano.
    /// </summary>
    public class TestApi
    {
        /// <summary>
        /// Tests a method with a complex input and returns back the passed in input.
        /// </summary>
        /// <param name="employee">Complex input.</param>
        /// <returns>Complex output.</returns>
        public static Employee ComplexInput( Employee employee )
        {
            return employee;
        }

        /// <summary>
        /// Tests a method with a complex input and returns back the HTTP Method verb used to access the method.
        /// </summary>
        /// <param name="employee">Complex input.</param>
        /// <param name="nanoContext">Current NanoContext for the web request.</param>
        /// <returns>Http Method Verb used to access the method.</returns>
        public static string ComplexInputWithNanoContextInjected( Employee employee, NanoContext nanoContext )
        {
            return nanoContext.Request.Uri.ToString();
        }

        /// <summary>
        /// Tests a method that has no input or output. 
        /// </summary>
        public static void VoidMethodWithNoInputOrOutput()
        {
            // Does thing..
        }
    }
}