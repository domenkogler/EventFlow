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
using EventFlow.Core.Caching;
using EventFlow.Logs;
using EventFlow.Pipeline.Tests.StructureMap;
using EventFlow.Queries.Pipeline;
using EventFlow.TestHelpers;
using FluentAssertions;
using MediatR;
using NUnit.Framework;
using StructureMap;

namespace EventFlow.Pipeline.Tests.Query.StructureMap
{
    [TestFixture]
    [Category(Categories.Unit)]
    public class QueryPipelineProcessorStructureMapTests : TestsFor<QueryPipelineProcessor>
    {
        private Registry Registry { get; set; }
        private IContainer Container => new Container(Registry);

        [SetUp]
        public void SetUp()
        {
            Registry = new Registry();
        }

        protected override QueryPipelineProcessor CreateSut()
        {
            return new QueryPipelineProcessor(Log, Container.GetInstance<IMediator>(), new DictionaryMemoryCache(Mock<ILog>()));
        }

        [Test]
        public async Task CommandHandlerIsInvoked()
        {
            // Arrange
            var query = new TestQuery();

            // Arrange StructureMap container
            Registry.AddMediatRegistryForAssemby<TestQuery>();

            var have = Container.WhatDoIHave();

            // Act
            var result = await Sut.Send(query, CancellationToken.None);

            // Assert
            result.Result.Should().Be("Query TestQueryHandler");
        }
    }
}