using System;
using ByteDance.CloudSync.Match;

namespace ByteDance.CloudSync.MatchManager
{
    internal class BaseMatchOperation : IDisposable
    {
        private static ICloudGameMatchAPI _matchAPI;

        protected string MakeCallId(string name)
        {
            return $"[{name}]";
        }

        public static SdkDebugLogger Debug { get; set; } = new("CloudMatchOperation");

        public static ICloudGameMatchAPI MatchAPI
        {
            get => _matchAPI ?? CloudGameSdk.API;
            set => _matchAPI = value;
        }

        public static IMatchService MatchService { get; set; }

        public static string ExceptionToMessage(Exception exception)
        {
            return CloudMatchExtension.ExceptionToMessage(exception);
        }

        public virtual void Dispose() {}
    }
}