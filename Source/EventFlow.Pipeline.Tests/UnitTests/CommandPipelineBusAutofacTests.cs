// The MIT License (MIT)
// 
// Copyright (c) 2018 Domen Kogler
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
// https://github.com/eventflow/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands.Pipeline;
using EventFlow.Core;
using EventFlow.Core.Caching;
using EventFlow.Logs;
using EventFlow.Pipeline.Tests.Autofac;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using FluentAssertions;
using MediatR;
using Moq;
using NUnit.Framework;

namespace EventFlow.Pipeline.Tests.Commands.Autofac
{
    [TestFixture]
    [Category(Categories.Unit)]
    public class CommandPipelineBusAutofacTests : TestsForCommandPipelineBus
    {
        private IContainer Container;

        [SetUp]
        public void SetUp()
        {
        }

        protected override CommandPipelineBus CreateSut()
        {
            var storeMoq = ArrangeWorkingEventStore<TestExecutionResult>();
            return new CommandPipelineBus(Log, Container.Resolve<IMediator>(), storeMoq.Object, new DictionaryMemoryCache(Mock<ILog>()));
        }

        [Test]
        public async Task CommandHandlerIsInvoked()
        {
            // Arrange
            var command = new ThingyTestCommand();

            // Arrange Autofac container
            Container = new ContainerBuilder()
                .RegisterInstancesAsImplementedInterfaces(new ThingyTestCommandHandler())
                .BuildContainer();

            // Act
            var result = await Sut.Send(command, CancellationToken.None);

            // Assert
            result.Result.Message.Should().Be("Command ThingyTestCommandHandler");
        }

        [Test]
        public async Task PrehandlersAreInvoked()
        {
            // Arrange
            var command = new ThingyTestCommand();

            // Arrange Autofac container
            Container = new ContainerBuilder()
                .RegisterInstancesAsImplementedInterfaces(new ThingyTestCommandHandler())
                .RegisterOpenGenericsInAssemblyOf<ThingyTestCommand>()
                .BuildContainer();

            IAggregateUpdateResult<TestExecutionResult> result;

            // Act
            using (Container.BeginLifetimeScope())
            {
                result = await Sut.Send(command, CancellationToken.None);
            }

            // Assert
            result.Result.Message.Should().Be("Command Prehandler ThingyTestCommandHandler Posthandler");
        }

        private Mock<IAggregateStore> ArrangeWorkingEventStore<TExecutionResult>()
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
                .Returns(() => Task.FromResult((IAggregateUpdateResult<TExecutionResult>) new AggregateStore.AggregateUpdateResult<TExecutionResult>(result, new IDomainEvent[0])));
            return aggregateStoreMock;
        }
    }
}