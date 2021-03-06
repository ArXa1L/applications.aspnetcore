﻿namespace Vostok.Applications.AspNetCore.Helpers
{
    internal static class MicrosoftConstants
    {
        public const string ActionLogScope = "Microsoft.AspNetCore.Mvc.MvcCoreLoggerExtensions+ActionLogScope";
        public const string ActionLogScopeOld = "Microsoft.AspNetCore.Mvc.Internal.MvcCoreLoggerExtensions+ActionLogScope";

        public const string HostingLogScope = "Microsoft.AspNetCore.Hosting.HostingLoggerExtensions+HostingLogScope";
        public const string HostingLogScopeOld = "Microsoft.AspNetCore.Hosting.Internal.HostingLoggerExtensions+HostingLogScope";

        public const string ConnectionLogScope = "Microsoft.AspNetCore.Server.Kestrel.Core.Internal.ConnectionLogScope";
    }
}