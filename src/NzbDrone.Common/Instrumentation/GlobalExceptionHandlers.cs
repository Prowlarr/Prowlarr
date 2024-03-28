using System;
using System.Threading.Tasks;
using NLog;

namespace NzbDrone.Common.Instrumentation
{
    public static class GlobalExceptionHandlers
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(GlobalExceptionHandlers));
        public static void Register()
        {
            AppDomain.CurrentDomain.UnhandledException += HandleAppDomainException;
            TaskScheduler.UnobservedTaskException += HandleTaskException;
        }

        private static void HandleTaskException(object sender, UnobservedTaskExceptionEventArgs eventArgs)
        {
            eventArgs.SetObserved();

            eventArgs.Exception.Handle(exception =>
            {
                Console.WriteLine("Task Error: {0} {1}", exception.GetType(), exception);
                Logger.Error(exception, "Task Error");

                return true;
            });
        }

        private static void HandleAppDomainException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;

            if (exception == null)
            {
                return;
            }

            if (exception is NullReferenceException &&
                exception.ToString().Contains("Microsoft.AspNet.SignalR.Transports.TransportHeartbeat.ProcessServerCommand"))
            {
                Logger.Warn("SignalR Heartbeat interrupted");
                return;
            }

            Console.WriteLine("EPIC FAIL: {0}", exception);
            Logger.Fatal(exception, "EPIC FAIL.");
        }
    }
}
