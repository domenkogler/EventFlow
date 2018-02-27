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
using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Core;
using MediatR;

namespace EventFlow.Commands.Pipeline
{
    public abstract class CommandPipelineHandler<TAggregate, TIdentity, TResult, TCommand> :
        CommandHandler<TAggregate, TIdentity, TResult, TCommand>,
        ICommandPipelineHandler<TAggregate, TIdentity, TResult, TCommand>
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
        where TResult : IExecutionResult
        where TCommand : ICommand<TAggregate, TIdentity, TResult>
    {
        async Task<TResult> IRequestHandler<ICommandPipelineHandlerObject<TAggregate, TResult, TCommand>, TResult>.Handle(ICommandPipelineHandlerObject<TAggregate, TResult, TCommand> request, CancellationToken cancellationToken)
        {
            return await ExecuteCommandAsync(request.Aggregate, request.Command, cancellationToken);
        }
    }

    public abstract class CommandPipelineHandler<TAggregate, TExecutionResult, TCommand> :
        CommandPipelineHandler<TAggregate, IIdentity, TExecutionResult, TCommand>
        where TAggregate : IAggregateRoot<IIdentity>
        //where TIdentity : IIdentity
        where TCommand : ICommand<TAggregate, IIdentity, TExecutionResult>
        where TExecutionResult : IExecutionResult
    {
    }

    public abstract class CommandPipelineHandler<TAggregate, TCommand> :
        CommandPipelineHandler<TAggregate, IExecutionResult, TCommand>
        where TAggregate : IAggregateRoot<IIdentity>
        where TCommand : ICommand<TAggregate, IIdentity, IExecutionResult>
    {
        public override async Task<IExecutionResult> ExecuteCommandAsync(TAggregate aggregate, TCommand command, CancellationToken cancellationToken)
        {
            await ExecuteAsync(aggregate, command, cancellationToken).ConfigureAwait(false);
            return ExecutionResult.Success();
        }

        public abstract Task ExecuteAsync(TAggregate aggregate, TCommand command, CancellationToken token);
    }
}