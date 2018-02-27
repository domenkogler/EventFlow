using System.Linq;
using System.Reflection;
using MediatR;
using MediatR.Pipeline;
using StructureMap;

namespace EventFlow.Pipeline.Tests.StructureMap
{
    public static class StructureMapContainerHelper
    {
        public class MediatRRegistry : Registry
        {
            public MediatRRegistry(Assembly assembly)
            {
                Scan(s =>
                {
                    s.Assembly(assembly);
                    s.WithDefaultConventions();
                    s.ConnectImplementationsToTypesClosing(typeof(IRequestHandler<>)); // Handlers with no response
                    s.ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>)); // Handlers with a response
                    s.ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>));
                });

                For<SingleInstanceFactory>().Use<SingleInstanceFactory>(ctx => SingleInstanceFactory(ctx));
                For<MultiInstanceFactory>().Use<MultiInstanceFactory>(ctx => MultiInstanceFactory(ctx));
                For<IMediator>().Use<Mediator>();
            }

            private static SingleInstanceFactory SingleInstanceFactory(IContext ctx)
            {
                return type =>
                {
                    var instance = ctx.GetInstance(type);
                    return instance;
                };
            }

            private static MultiInstanceFactory MultiInstanceFactory(IContext ctx)
            {
                return type =>
                {
                    var instances = ctx.GetAllInstances(type).ToArray();
                    return instances;
                };
            }
        }

        public class PipelineRegistry : Registry
        {
            public PipelineRegistry()
            {
                //Pipeline
                For(typeof(IPipelineBehavior<,>)).Add(typeof(RequestPreProcessorBehavior<,>));
                For(typeof(IPipelineBehavior<,>)).Add(typeof(RequestPostProcessorBehavior<,>));
            }
        }

        public static Registry AddMediatRegistryForAssemby<T>(this Registry registry)
        {
            registry.IncludeRegistry(new MediatRRegistry(typeof(T).Assembly));
            return registry;
        }

        public static Registry AddPipelineRegistry(this Registry registry)
        {
            registry.IncludeRegistry(new PipelineRegistry());
            return registry;
        }
    }
}