namespace Nano.Core
{
    // Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. Licensed under the Apache License: http://www.apache.org/licenses/
    public static class OwinConstants
    {
        #region OWIN v1.0.0 - 3.2.1. Request Data

        // http://owin.org/spec/spec/owin-1.0.0.html

        public const string RequestScheme = "owin.RequestScheme";
        public const string RequestMethod = "owin.RequestMethod";
        public const string RequestPathBase = "owin.RequestPathBase";
        public const string RequestPath = "owin.RequestPath";
        public const string RequestQueryString = "owin.RequestQueryString";
        public const string RequestProtocol = "owin.RequestProtocol";
        public const string RequestHeaders = "owin.RequestHeaders";
        public const string RequestBody = "owin.RequestBody";

        #endregion

        #region OWIN v1.0.0 - 3.2.2. Response Data

        // http://owin.org/spec/spec/owin-1.0.0.html

        public const string ResponseStatusCode = "owin.ResponseStatusCode";
        public const string ResponseReasonPhrase = "owin.ResponseReasonPhrase";
        public const string ResponseProtocol = "owin.ResponseProtocol";
        public const string ResponseHeaders = "owin.ResponseHeaders";
        public const string ResponseBody = "owin.ResponseBody";

        #endregion

        #region OWIN v1.0.0 - 3.2.3. Other Data

        // http://owin.org/spec/spec/owin-1.0.0.html

        public const string CallCancelled = "owin.CallCancelled";

        public const string OwinVersion = "owin.Version";

        #endregion

        #region OWIN Keys for IAppBuilder.Properties

        internal static class Builder
        {
            public const string AddSignatureConversion = "builder.AddSignatureConversion";
            public const string DefaultApp = "builder.DefaultApp";
        }

        #endregion

        #region OWIN Key Guidelines and Common Keys - 6. Common keys

        // http://owin.org/spec/spec/CommonKeys.html

        public static class CommonKeys
        {
            public const string ClientCertificate = "ssl.ClientCertificate";
            public const string RemoteIpAddress = "server.RemoteIpAddress";
            public const string RemotePort = "server.RemotePort";
            public const string LocalIpAddress = "server.LocalIpAddress";
            public const string LocalPort = "server.LocalPort";
            public const string IsLocal = "server.IsLocal";
            public const string TraceOutput = "host.TraceOutput";
            public const string Addresses = "host.Addresses";
            public const string AppName = "host.AppName";
            public const string Capabilities = "server.Capabilities";
            public const string OnSendingHeaders = "server.OnSendingHeaders";
            public const string OnAppDisposing = "host.OnAppDisposing";
            public const string Scheme = "scheme";
            public const string Host = "host";
            public const string Port = "port";
            public const string Path = "path";
        }

        #endregion

        #region Security v0.1.0
        
        public static class Security
        {
            // 3.2. Per Request

            public const string User = "server.User";

            public const string Authenticate = "security.Authenticate";

            // 3.3. Response

            public const string SignIn = "security.SignIn";

            public const string SignOut = "security.SignOut";

            public const string SignOutProperties = "security.SignOutProperties";

            public const string Challenge = "security.Challenge";
        }

        #endregion
    }
}