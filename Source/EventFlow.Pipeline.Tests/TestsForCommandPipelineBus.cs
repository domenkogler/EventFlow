using System;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using EventFlow.Commands.Pipeline;
using EventFlow.Core;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using Moq;

namespace EventFlow.Pipeline.Tests
{
    public abstract class TestsForCommandPipelineBus : TestsFor<CommandPipelineBus>
    {
        protected Mock<IAggregateStore> MockWorkingEventStore<TExecutionResult>()
            where TExecutionResult : IExecutionResult
        {
            var aggregateStoreMock = InjectMock<IAggregateStore>();
            TExecutionResult result = default(TExecutionResult);
            aggregateStoreMock
                .Setup(s => s.UpdateAsync(
                    It.IsAny<ThingyId>(),
                    It.IsAny<ISourceId>(),
                    It.IsAny<Func<ThingyAggregate, CancellationToken, Task<TExecutionResult>>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<ThingyId, ISourceId, Func<ThingyAggregate, CancellationToken, Task<TExecutionResult>>, CancellationToken>(async (i, s, f, c) => { result = await f(A<ThingyAggregate>(), c); })
                .Returns(() => Task.FromResult((IAggregateUpdateResult<TExecutionResult>)new AggregateStore.AggregateUpdateResult<TExecutionResult>(result, new IDomainEvent[0])));
            return aggregateStoreMock;
        }

        protected Mock<CommandPipelineHandler<TAggregate, TCommand>> MockWorkingCommandHandler<TAggregate, TCommand>()
            where TAggregate : IAggregateRoot<IIdentity>
            where TCommand : ICommand<TAggregate, IIdentity, IExecutionResult>
        {
            var handlerMock = InjectMock<CommandPipelineHandler<TAggregate, TCommand>>();
            handlerMock
                .Setup(s => s.ExecuteCommandAsync(It.IsAny<TAggregate>(), It.IsAny<TCommand>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(ExecutionResult.Success()));
            return handlerMock;
        }

        protected Mock<PrehandleOnce<THandle>> MockWorkingPrehandler<THandle>()
            where THandle : IHandle
        {
            var prehandlerMock = InjectMock<PrehandleOnce<THandle>>();
            prehandlerMock
                .Setup(s => s.Handle(It.IsAny<THandle>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(0));
            return prehandlerMock;
        }
    }
}