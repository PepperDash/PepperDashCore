using Crestron.SimplSharp;
using Serilog.Core;
using Serilog.Events;

namespace PepperDash.Core.Logging
{
    public class CrestronEnricher : ILogEventEnricher
    {
        private static readonly string AppName;

        static CrestronEnricher()
        {
            AppName = CrestronEnvironment.DevicePlatform switch
            {
                eDevicePlatform.Appliance => $"App {InitialParametersClass.ApplicationNumber}",
                eDevicePlatform.Server => $"{InitialParametersClass.RoomId}",
                _ => string.Empty
            };
        }
            

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var property = propertyFactory.CreateProperty("App", AppName);

            logEvent.AddOrUpdateProperty(property);
        }
    }
}
