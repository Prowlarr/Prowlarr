using System.Collections.Generic;
using NzbDrone.Common.Composition;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.SignalR;

namespace Prowlarr.Host
{
    public class MainAppContainerBuilder : ContainerBuilderBase
    {
        public static IContainer BuildContainer(StartupContext args)
        {
            var assemblies = new List<string>
                             {
                                 "Prowlarr.Host",
                                 "Prowlarr.Core",
                                 "Prowlarr.SignalR",
                                 "Prowlarr.Api.V1",
                                 "Prowlarr.Http"
                             };

            return new MainAppContainerBuilder(args, assemblies).Container;
        }

        private MainAppContainerBuilder(StartupContext args, List<string> assemblies)
            : base(args, assemblies)
        {
            AutoRegisterImplementations<MessageHub>();

            if (OsInfo.IsWindows)
            {
                Container.Register<INzbDroneServiceFactory, NzbDroneServiceFactory>();
            }
            else
            {
                Container.Register<INzbDroneServiceFactory, DummyNzbDroneServiceFactory>();
            }
        }
    }
}
