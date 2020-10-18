using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common;
using NzbDrone.Common.Composition;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Jobs;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.SignalR;
using NzbDrone.Test.Common;
using Prowlarr.Host;

namespace NzbDrone.App.Test
{
    [TestFixture]
    public class ContainerFixture : TestBase
    {
        private IContainer _container;

        [SetUp]
        public void SetUp()
        {
            var args = new StartupContext("first", "second");

            _container = MainAppContainerBuilder.BuildContainer(args);

            _container.Register<IMainDatabase>(new MainDatabase(null));

            // set up a dummy broadcaster to allow tests to resolve
            var mockBroadcaster = new Mock<IBroadcastSignalRMessage>();
            _container.Register<IBroadcastSignalRMessage>(mockBroadcaster.Object);
        }

        [Test]
        public void should_be_able_to_resolve_indexers()
        {
            _container.Resolve<IEnumerable<IIndexer>>().Should().NotBeEmpty();
        }

        [Test]
        public void container_should_inject_itself()
        {
            var factory = _container.Resolve<IServiceFactory>();

            factory.Build<IIndexerFactory>().Should().NotBeNull();
        }

        [Test]
        [Ignore("Shit appveyor")]
        public void should_return_same_instance_of_singletons()
        {
            var first = _container.ResolveAll<IHandle<ApplicationShutdownRequested>>().OfType<Scheduler>().Single();
            var second = _container.ResolveAll<IHandle<ApplicationShutdownRequested>>().OfType<Scheduler>().Single();

            first.Should().BeSameAs(second);
        }
    }
}
