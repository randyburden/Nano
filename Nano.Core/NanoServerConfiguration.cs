using System;
using System.Collections.Generic;

namespace Nano.Core
{
    public class NanoServerConfiguration
    {
        public NanoServerConfiguration()
        {
        }

        public NanoServerConfiguration( string baseUrl )
        {
            BaseUrl = baseUrl;
        }

        public string BaseUrl = "http://localhost/";

        public List<BackgroundTask> BackgroundTasks = new List<BackgroundTask>();
        public List<StaticFileServer> StaticFileServers = new List<StaticFileServer>();
        public List<WebApi> WebApis = new List<WebApi>();
        public List<WebAdminPage> WebAdminPages = new List<WebAdminPage>();

        // global error handling callback - all nano exceptions will call this
        public Action<NanoServerConfiguration> OnAllErrors; // TODO: Perhaps a C# event

        // global API error handling callback - all api exceptions will call this
        public Action<NanoServerConfiguration> OnAllApiErrors;

        // global pre and post api method callbacks
        public Action<NanoServerConfiguration, WebApi, NanoRequest> OnAllApiMethodPreInvoke;
        public Action<NanoServerConfiguration, WebApi, NanoRequest> OnAllApiMethodPostInvoke;

        // global task error handling callback - all api exceptions will call this
        public Action<NanoServerConfiguration, BackgroundTask> OnAllTasksErrors;

        // global pre and post task method callbacks
        public Action<NanoServerConfiguration, BackgroundTask> OnAllTasksPreInvoke;
        public Action<NanoServerConfiguration, BackgroundTask> OnAllTasksPostInvoke;
    }

    public class BackgroundTask
    {
        public string Name;
        public Action Task;
        public int MillisecondInterval;
        public bool AllowOverlappingRuns = true; // todo: what should the default be?

        // task specific callbacks
        public Action<NanoServerConfiguration, BackgroundTask> OnTaskError;
        public Action<NanoServerConfiguration, BackgroundTask> OnTaskPreInvoke;
        public Action<NanoServerConfiguration, BackgroundTask> OnTaskPostInvoke;
    }

    public class StaticFileServer
    {
        /// <summary>
        /// Instantiates a new static file server.
        /// </summary>
        /// <param name="fileSystemPath"></param>
        /// <param name="webPath">Web path that will route to this endpoint.</param>
        public StaticFileServer( string fileSystemPath, string webPath )
        {
            FileSystemPath = fileSystemPath;

            WebPath = webPath;
        }

        public string FileSystemPath;
        public string WebPath;
    } // TODO: Find where exceptions are thrown if at all.. some hook



    public class WebAdminPage
    {
        public string WebPath;
        public Func<string> Func;
    }
}