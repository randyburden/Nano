using Nano.Core;

namespace Nano.Host
{
    /// <summary>
    /// Provides helper methods and syntax sugar for quickly configuring a NanoServer.
    /// </summary>
    public static class NanoServerConfigurationExtensions
    {
        /// <summary>
        /// Adds a class as an available web api endpoint at the specified web path.
        /// </summary>
        /// <typeparam name="T">Class type to expose as a web api endpoint.</typeparam>
        /// <param name="nanoServerConfiguration">NanoServer configuration.</param>
        /// <param name="webPath">Web path / URL fragment that will route to this endpoint.</param>
        public static WebApi AddWebApi<T>( this NanoServerConfiguration nanoServerConfiguration, string webPath = null )
        {
            var webApi = new WebApi( typeof ( T ), webPath );

            nanoServerConfiguration.WebApis.Add( webApi );

            return webApi;
        }

        /// <summary>
        /// Adds a file system directory as an available web api endpoint at the specified web path.
        /// </summary>
        /// <param name="nanoServerConfiguration">NanoServer configuration.</param>
        /// <param name="fileSystemPath"></param>
        /// <param name="webPath">Web path that will route to this endpoint.</param>
        public static StaticFileServer AddStaticFileServer( this NanoServerConfiguration nanoServerConfiguration, string fileSystemPath, string webPath )
        {
            var staticFileServer = new StaticFileServer( fileSystemPath, webPath );

            nanoServerConfiguration.StaticFileServers.Add( staticFileServer );

            return staticFileServer;
        }
    }
}