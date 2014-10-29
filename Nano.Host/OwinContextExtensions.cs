using System.Threading.Tasks;
using Microsoft.Owin;
using Newtonsoft.Json;

namespace Nano.Host
{
    /// <summary>
    /// OwinContext extensions.
    /// </summary>
    public static class OwinContextExtensions
    {
        /// <summary>
        /// Returns the given object as a JSON-encoded response.
        /// </summary>
        /// <param name="context">Owin context.</param>
        /// <param name="obj">Object to serialize to JSON.</param>
        /// <param name="formatting">JSON formatting.</param>
        /// <returns>Task to write the JSON to the response body.</returns>
        public static Task ReturnAsJson( this IOwinContext context, object obj, Formatting formatting = Formatting.None )
        {
            context.Response.ContentType = "application/json";

            var json = JsonConvert.SerializeObject( obj, formatting );

            return context.Response.WriteAsync( json );
        }
    }
}