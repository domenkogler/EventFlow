// The MIT License (MIT)
// 
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
using System.Collections.Generic;
using System.Linq;
using EventFlow.Configuration;
using StructureMap;

namespace EventFlow.StructureMap.Registrations
{
    internal abstract class StructureMapResolver : IResolver
    {
        protected StructureMapResolver() { }

        protected IContainer Container { get; set; }

        public T Resolve<T>()
        {
            return Container.GetInstance<T>();
        }

        public T Resolve<T>(string name)
        {
            return Container.GetInstance<T>(name);
        }

        public object Resolve(Type serviceType)
        {
            return Container.GetInstance(serviceType);
        }

        public object Resolve(Type serviceType, string name)
        {
            return Container.GetInstance(serviceType, name);
        }

        public IEnumerable<object> ResolveAll(Type serviceType)
        {
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(serviceType);
            return Container.GetAllInstances(enumerableType).OfType<object>();
        }

        public IEnumerable<Type> GetRegisteredServices()
        {
            return Container.Model.PluginTypes
                .Where(p => p.ProfileName != "DEFAULT")
                .Select(p => p.PluginType);
        }

        public bool HasRegistrationFor<T>()
            where T : class
        {
            var serviceType = typeof(T);
            return GetRegisteredServices().Any(t => serviceType == t);
        }
    }
}