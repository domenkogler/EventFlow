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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Configuration.Decorators;
using EventFlow.Core;
using EventFlow.Core.IoC;
using EventFlow.Extensions;
using StructureMap;

namespace EventFlow.StructureMap.Registrations
{
    public interface IStartable
    {
        //bool WasStarted { get; }
        void Start();
    }

    internal class StructureMapServiceRegistration : ServiceRegistration, IServiceRegistration
    {
        private readonly Registry _register = new Registry();
        private readonly DecoratorService _decoratorService = new DecoratorService();

        public StructureMapServiceRegistration(params Registry[] registries)
        {
            foreach (var registry in registries)
            {
                _register.IncludeRegistry(registry);
            }

            _register.For<IStartable>().Use<StructureMapStartable>();
            _register.For<IResolver>().Use<StructureMapScopeResolver>();
            _register.For<IResolverContext>().Use<ResolverContext>();
            _register.For<IScopeResolver>().Use(ctx => new StructureMapScopeResolver(ctx.GetInstance<IContainer>().GetNestedContainer()));
            _register.ForSingletonOf<IDecoratorService>().Use(_decoratorService);
        }

        private IResolverContext ResolverContext(IContext ctx)
        {
            return new ResolverContext(new StructureMapScopeResolver(ctx.GetInstance<IContainer>()));
            //return ctx.GetInstance<IResolverContext>();
        }

        //private TService Decorate<TService>(TService service, IContext ctx)
        //{
        //    return _decoratorService.Decorate(service, ResolverContext(ctx));
        //}

        //private TService Decorate<TService>(IContext ctx)
        //{
        //    return _decoratorService.Decorate(ctx.GetInstance<TService>(), ResolverContext(ctx));
        //}

        //private object Decorate(Type serviceType, IContext ctx)
        //{
        //    return _decoratorService.Decorate(ctx.GetInstance(serviceType), ResolverContext(ctx));
        //}

        public void Register<TService, TImplementation>(
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
            where TImplementation : class, TService
            where TService : class
        {
            if (lifetime == Lifetime.Singleton)
            {
                var serviceConfig = _register.ForSingletonOf<TService>();
                if (keepDefault) serviceConfig.Add<TImplementation>();
                else serviceConfig.Use<TImplementation>();
            }
            else
            {
                var serviceConfig = _register.For<TService>();
                var instanceConfig = keepDefault ? serviceConfig.Add<TImplementation>() : serviceConfig.Use<TImplementation>();
                instanceConfig.AlwaysUnique();
            }
        }

        public void Register<TService>(
            Func<IResolverContext, TService> factory,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
            where TService : class
        {
            Register(ctx => factory(ResolverContext(ctx)), lifetime, keepDefault);
        }

        private void Register<TService>(
            Expression<Func<IContext, TService>> factory,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
            where TService : class
        {
            if (lifetime == Lifetime.Singleton)
            {
                var serviceConfig = _register.ForSingletonOf<TService>();
                if (keepDefault) serviceConfig.Add(factory);
                else serviceConfig.Use(factory);
            }
            else
            {
                var serviceConfig = _register.For<TService>();
                var instanceConfig = keepDefault ? serviceConfig.Add(factory) : serviceConfig.Use(factory);
                instanceConfig.AlwaysUnique();
            }
        }

        private void Register(
            Type serviceType,
            Expression<Func<IContext, object>> factory,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
        {
            if (lifetime == Lifetime.Singleton)
            {
                var serviceConfig = _register.ForSingletonOf(serviceType);
                if (keepDefault) serviceConfig.Add(factory);
                else serviceConfig.Use(factory);
            }
            else
            {
                var serviceConfig = _register.For(serviceType);
                var instanceConfig = keepDefault ? serviceConfig.Add(factory) : serviceConfig.Use(factory);
                instanceConfig.AlwaysUnique();
            }
        }

        public void Register(
            Type serviceType,
            Type implementationType,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
        {
            Register(serviceType, ctx => ctx.GetInstance(implementationType));
        }

        public void RegisterType(
            Type serviceType,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
        {
            Register(serviceType, ctx => ctx.GetInstance(serviceType), lifetime, keepDefault);
        }

        public void RegisterGeneric(
            Type serviceType,
            Type implementationType,
            Lifetime lifetime = Lifetime.AlwaysUnique,
            bool keepDefault = false)
        {
            if (lifetime == Lifetime.Singleton)
            {
                var serviceConfig = _register.ForSingletonOf(serviceType);
                if (keepDefault) serviceConfig.Add(implementationType);
                else serviceConfig.Use(implementationType);
            }
            else
            {
                var serviceConfig = _register.For(serviceType);
                var instanceConfig = keepDefault ? serviceConfig.Add(implementationType) : serviceConfig.Use(implementationType);
                instanceConfig.AlwaysUnique();
            }
        }

        public void RegisterIfNotRegistered<TService, TImplementation>(
            Lifetime lifetime = Lifetime.AlwaysUnique)
            where TService : class
            where TImplementation : class, TService
        {
            Register<TService, TImplementation>(lifetime, true);
        }

        public void Decorate<TService>(Func<IResolverContext, TService, TService> factory)
        {
            _register.For<TService>().DecorateAllWith((ctx, service) => factory(ctx.GetInstance<IResolverContext>(), service));
        }

        public IRootResolver CreateResolver(bool validateRegistrations)
        {
            var rootResolver = new StructureMapRootResolver(_register);
            if (validateRegistrations)
            {
                rootResolver.ValidateRegistrations();
            }
            return rootResolver;
        }

        public class StructureMapStartable : IStartable
        {
            private readonly IReadOnlyCollection<IBootstrap> _bootstraps;

            public StructureMapStartable(
                IEnumerable<IBootstrap> bootstraps)
            {
                _bootstraps = OrderBootstraps(bootstraps);
            }

            public void Start()
            {
                using (var a = AsyncHelper.Wait)
                {
                    a.Run(StartAsync(CancellationToken.None));
                }
            }

            private Task StartAsync(CancellationToken cancellationToken)
            {
                return Task.WhenAll(_bootstraps.Select(b => b.BootAsync(cancellationToken)));
            }
        }
    }
}