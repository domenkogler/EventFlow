// The MIT License (MIT)
// 
// Copyright (c) 2018 Domen Kogler
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
using EventFlow.Core;
using EventFlow.Core.Caching;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.Pipeline;
using MediatR;

namespace EventFlow.Queries.Pipeline
{
    public class QueryPipelineProcessor : IQueryPipelineProcessor
    {
        private readonly ILog _log;
        private readonly IMediator _mediatr;
        private readonly IMemoryCache _memoryCache;

        public QueryPipelineProcessor(ILog log, IMediator mediator, IMemoryCache memoryCache)
        {
            _log = log;
            _mediatr = mediator;
            _memoryCache = memoryCache;
        }

        async Task<TResult> IQueryProcessor.ProcessAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            return await ExecuteQuery(query, cancellationToken);
        }

        TResult IQueryProcessor.Process<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
        {
            var result = default(TResult);
            using (var a = AsyncHelper.Wait)
            {
                a.Run(((IQueryProcessor) this).ProcessAsync(query, cancellationToken), r => result = r);
            }
            return result;
        }

        public async Task<TResult> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
        //    where TQuery : IQuery<TResult>
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            return await ExecuteQuery(query, cancellationToken);
        }

        private async Task<TResult> ExecuteQuery<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
        //    where TQuery : IQuery<TResult>
        {
            var ctor = await CommandHandlerObjectCtorCache<TResult>(query.GetType(), cancellationToken);
            var queryHandlerObject = ctor(new object[] { query });
            _log.Verbose(() => $"Executing query '{query.GetType().PrettyPrint()}'");
            return await _mediatr.Send((IRequest<TResult>) queryHandlerObject, cancellationToken);
        }

        private Task<ILHelper.GenericMethod> CommandHandlerObjectCtorCache<TResult>(Type queryType, CancellationToken cancellationToken)
        {
            return _memoryCache.GetOrAddAsync(
                CacheKey.With(GetType(), queryType.GetCacheKey()),
                TimeSpan.FromDays(1),
                _ =>
                {
                    var instanceType = typeof(QueryPipelineHandlerObject<,>).MakeGenericType(queryType, typeof(TResult));
                    var ctor = instanceType.GetTypeInfo().GetConstructors().First();
                    var ctorDelegate = ILHelper.GenerateConstructor1(ctor, queryType);
                    return Task.FromResult(ctorDelegate);
                },
                cancellationToken);
        }
    }
}