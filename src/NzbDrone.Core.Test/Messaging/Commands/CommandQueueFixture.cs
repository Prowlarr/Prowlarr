using FizzWare.NBuilder;
using NUnit.Framework;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Update.Commands;

namespace NzbDrone.Core.Test.Messaging.Commands
{
    [TestFixture]
    public class CommandQueueFixture : CoreTest<CommandQueue>
    {
        private void GivenStartedExclusiveCommand()
        {
            var commandModel = Builder<CommandModel>
                .CreateNew()
                .With(c => c.Name = "ApplicationUpdate")
                .With(c => c.Body = new ApplicationUpdateCommand())
                .With(c => c.Status = CommandStatus.Started)
                .Build();

            Subject.Add(commandModel);
        }
    }
}
