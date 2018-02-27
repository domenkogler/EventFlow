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

using System.Threading;
using System.Threading.Tasks;
using Autofac;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands.Pipeline;
using EventFlow.Core.Caching;
using EventFlow.Logs;
using EventFlow.Pipeline.Tests.Autofac;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using MediatR;
using Moq;
using NUnit.Framework;

namespace EventFlow.Pipeline.Tests.Commands.Autofac
{
    [TestFixture]
    [Category(Categories.Unit)]
    public class CommandPipelineBusAutofacMoqTests : TestsForCommandPipelineBus
    {
        private IContainer Container;

        [SetUp]
        public void SetUp()
        {
        }

        protected override CommandPipelineBus CreateSut()
        {
            var storeMock = MockWorkingEventStore<IExecutionResult>();
            return new CommandPipelineBus(Log, Container.Resolve<IMediator>(), storeMock.Object, new DictionaryMemoryCache(Mock<ILog>()));
        }

        [Test]
        public async Task CommandHandlerIsInvoked()
        {
            // Arrange
            var commandHandlerMock = MockWorkingCommandHandler<ThingyAggregate, ThingyPingCommand>();
            var command = A<ThingyPingCommand>();
            
            // Arrange Autofac container
            Container = new ContainerBuilder()
                .RegisterInstancesAsImplementedInterfaces(commandHandlerMock.Object)
                .BuildContainer();

            // Act
            var result = await Sut.Send(command, CancellationToken.None);

            // Assert
            commandHandlerMock.Verify(
                h => h.ExecuteCommandAsync(It.IsAny<ThingyAggregate>(), It.IsAny<ThingyPingCommand>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task PrehandlersAreInvoked()
        {
            // Arrange
            var commandHandlerMock = MockWorkingCommandHandler<ThingyAggregate, ThingyPingCommand>();
            var aggPrehandlerMock = MockWorkingPrehandler<IHandleAggregate<ThingyAggregate>>();
            var thingyPingCommandPrehandlerMock = MockWorkingPrehandler<IHandleCommand<ThingyPingCommand>>();
            var thingyNopCommandPrehandlerMock = MockWorkingPrehandler<IHandleCommand<ThingyNopCommand>>();
            var prehandleAllMock = MockWorkingPrehandler<IHandle>();
            var command = A<ThingyPingCommand>();

            // Arrange Autofac container
            Container = new ContainerBuilder()
                .RegisterInstancesAsImplementedInterfaces(
                    commandHandlerMock.Object,
                    aggPrehandlerMock.Object,
                    thingyPingCommandPrehandlerMock.Object,
                    thingyNopCommandPrehandlerMock.Object,
                    prehandleAllMock.Object)
                .BuildContainer();

            // Act
            var result = await Sut.Send(command, CancellationToken.None);
            
            // Assert
            aggPrehandlerMock.Verify(h => h.Handle(It.IsAny<IHandleAggregate<ThingyAggregate>>(), It.IsAny<CancellationToken>()), Times.Once);
            thingyPingCommandPrehandlerMock.Verify(h => h.Handle(It.IsAny<IHandleCommand<ThingyPingCommand>>(), It.IsAny<CancellationToken>()), Times.Once);
            thingyNopCommandPrehandlerMock.Verify(h => h.Handle(It.IsAny<IHandleCommand<ThingyNopCommand>>(), It.IsAny<CancellationToken>()), Times.Never);
            prehandleAllMock.Verify(h => h.Handle(It.IsAny<IHandle>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}