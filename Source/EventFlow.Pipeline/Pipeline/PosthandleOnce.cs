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

using System.Threading.Tasks;
using EventFlow.Aggregates.ExecutionResults;
using MediatR.Pipeline;

namespace EventFlow.Pipeline
{
    // https://github.com/jbogard/MediatR/issues/128
    public abstract class PosthandleOnce<THandle, TExecutionResult> : IPosthandler<THandle, TExecutionResult>
        where THandle : IHandle
        where TExecutionResult : IExecutionResult
    {
        private bool _visited;
        private readonly object _lock = new object();

        public abstract Task Handle(THandle request, TExecutionResult result);

        Task IRequestPostProcessor<THandle, TExecutionResult>.Process(THandle request, TExecutionResult result)
        {
            if (_visited) return Task.FromResult(0);
            lock (_lock)
            {
                if (_visited) return Task.FromResult(0);
                _visited = true;
                return Handle(request, result);
            }
        }
    }
}