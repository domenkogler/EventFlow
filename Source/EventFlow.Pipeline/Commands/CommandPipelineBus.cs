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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Core;
using EventFlow.Core.Caching;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.Pipeline;
using MediatR;

namespace EventFlow.Commands.Pipeline
{
    public class CommandPipelineBus : ICommandPipelineBus
    {
        private readonly ILog _log;
        private readonly IMediator _mediatr;
        private readonly IAggregateStore _aggregateStore;
        private readonly IMemoryCache _memoryCache;

        public CommandPipelineBus(ILog log, IMediator mediator, IAggregateStore aggregateStore, IMemoryCache memoryCache)
        {
            _log = log;
            _mediatr = mediator;
            _aggregateStore = aggregateStore;
            _memoryCache = memoryCache;
        }
        
        // Hide method for clarity
        async Task<TExecutionResult> ICommandBus.PublishAsync<TAggregate, TIdentity, TExecutionResult>(
            ICommand<TAggregate, TIdentity, TExecutionResult> command, CancellationToken cancellationToken)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            var updateResult = await ExecuteCommand(command, cancellationToken);
            return updateResult.Result;
        }

        // Use Send for method name - MaediatR conventions for messages dispatched to a single handler
        public async Task<IAggregateUpdateResult<TResult>> Send<TAggregate, TIdentity, TResult>(ICommand<TAggregate, TIdentity, TResult> command, CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TResult : IExecutionResult
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            return await ExecuteCommand(command, cancellationToken);
        }

        private async Task<IAggregateUpdateResult<TExecutionResult>> ExecuteCommand<TAggregate, TIdentity, TExecutionResult>(
            ICommand<TAggregate, TIdentity, TExecutionResult> command, CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            var commandDescription =
                $"command '{command.GetType().PrettyPrint()}' with ID '{command.SourceId}' on aggregate '{typeof(TAggregate).PrettyPrint()}'";
            _log.Verbose(() => $"Executing {commandDescription}");

            IAggregateUpdateResult<TExecutionResult> aggregateUpdateResult;
            try
            {
                aggregateUpdateResult = await _aggregateStore
                    .UpdateAsync<TAggregate, TIdentity, TExecutionResult>(command.AggregateId, command.SourceId, UpdateAggregate, cancellationToken)
                    .ConfigureAwait(false);

                async Task<TExecutionResult> UpdateAggregate(TAggregate aggregate, CancellationToken token)
                {
                    var ctor = await CommandHandlerObjectCtorCache<TAggregate, TExecutionResult>(command.GetType(), token);
                    var commandHandlerObject = ctor(new object[] { aggregate, command });
                    return await _mediatr.Send((IRequest<TExecutionResult>) commandHandlerObject, token);
                }
            }
            catch (Exception exception)
            {
                _log.Debug(exception, $"Execution of {commandDescription} failed due to exception '{exception.GetType().PrettyPrint()}' with message: {exception.Message}");
                throw;
            }
            
            _log.Verbose(() =>
            {
                var success = aggregateUpdateResult.Result?.IsSuccess;
                if (aggregateUpdateResult.DomainEvents.Any())
                return $"Execution of {commandDescription} did NOT result in any domain events, was success:{success}";
                var events = string.Join(", ", aggregateUpdateResult.DomainEvents.Select(d => d.EventType.PrettyPrint()));
                return $"Execution of {commandDescription} resulted in these events: {events}, was success: {success}";
            });

            return aggregateUpdateResult;
        }

        private Task<ILHelper.GenericMethod> CommandHandlerObjectCtorCache<TAggregate, TExecutionResult>(Type commandType, CancellationToken cancellationToken)
        {
            return _memoryCache.GetOrAddAsync(
                CacheKey.With(GetType(), commandType.GetCacheKey()),
                TimeSpan.FromDays(1),
                _ =>
                {
                    var instanceType = typeof(CommandPipelineHandlerObject<,,>).MakeGenericType(typeof(TAggregate), typeof(TExecutionResult), commandType);
                    var ctor = instanceType.GetTypeInfo().GetConstructors().First();
                    var ctorDelegate = ILHelper.GenerateConstructor2(ctor, typeof(TAggregate), commandType);
                    return Task.FromResult(ctorDelegate);
                },
                cancellationToken);
        }
    }
}
