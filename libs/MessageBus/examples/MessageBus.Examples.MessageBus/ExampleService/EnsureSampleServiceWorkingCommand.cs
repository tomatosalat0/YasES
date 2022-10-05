using System.Threading;
using System.Threading.Tasks;

namespace MessageBus.Examples.MessageBus.ExampleService
{
    [Topic("Commands/ExampleService/EnsureSampleServiceWorking")]
    public class EnsureSampleServiceWorkingCommand : IMessageCommand
    {
        public MessageId MessageId { get; } = MessageId.NewId();

        public sealed class Handler : IAsyncMessageCommandHandler<EnsureSampleServiceWorkingCommand>
        {
            private readonly MessageBrokerMessageBus _system;

            public Handler(MessageBrokerMessageBus system)
            {
                _system = system;
            }

            public async Task HandleAsync(EnsureSampleServiceWorkingCommand command)
            {
                ExampleServiceQuery serviceQuery = new ExampleServiceQuery(command.MessageId);
                await _system.FireQuery<ExampleServiceQuery, ExampleServiceQuery.Result>(serviceQuery, CancellationToken.None);
            }
        }
    }
}
