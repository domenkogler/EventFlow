using System;
using System.Collections.Generic;
using System.Reflection;
using Autofac;
using Autofac.Features.Variance;
using MediatR;
using MediatR.Pipeline;

namespace EventFlow.Pipeline.Tests.Autofac
{
    public static class AutofacContainerHelper
    {
        private static readonly Type[] MediatrOpenTypes =
        {
            typeof(IRequestHandler<,>),
            typeof(IRequestHandler<>),
            typeof(INotificationHandler<>)
        };

        public static IContainer BuildContainer(this ContainerBuilder builder)
        {
            builder.RegisterSource(new ContravariantRegistrationSource());
            builder.RegisterAssemblyTypes(typeof(IMediator).GetTypeInfo().Assembly).AsImplementedInterfaces();

            builder.RegisterGeneric(typeof(RequestPostProcessorBehavior<,>)).As(typeof(IPipelineBehavior<,>));
            builder.RegisterGeneric(typeof(RequestPreProcessorBehavior<,>)).As(typeof(IPipelineBehavior<,>));

            builder.Register<SingleInstanceFactory>(ctx =>
            {
                var c = ctx.Resolve<IComponentContext>();
                return t => c.Resolve(t);
            });

            builder.Register<MultiInstanceFactory>(ctx =>
            {
                var c = ctx.Resolve<IComponentContext>();
                return t => (IEnumerable<object>)c.Resolve(typeof(IEnumerable<>).MakeGenericType(t));
            });

            return builder.Build();
        }

        public static ContainerBuilder RegisterInstancesAsImplementedInterfaces(this ContainerBuilder builder, params object[] instances)
        {
            foreach (var instance in instances)
            {
                builder.RegisterInstance(instance).AsImplementedInterfaces().AsSelf();
            }
            return builder;
        }

        public static ContainerBuilder RegisterOpenGenericsInAssemblyOf<T>(this ContainerBuilder builder)
        {
            builder
                .RegisterAssemblyTypes(typeof(T).GetTypeInfo().Assembly)
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            var registrationBuilder = builder.RegisterAssemblyTypes(typeof(T).GetTypeInfo().Assembly);
            foreach (var mediatrOpenType in MediatrOpenTypes)
            {
                registrationBuilder
                    .AsClosedTypesOf(mediatrOpenType)
                    .AsImplementedInterfaces()
                    .InstancePerLifetimeScope();
            }
            return builder;
        }
    }
}