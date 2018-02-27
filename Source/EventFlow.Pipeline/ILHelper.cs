using System;
using System.Reflection;
using System.Reflection.Emit;

namespace EventFlow.Pipeline
{
    public static class ILHelper
    {
        public delegate object GenericMethod(object[] args);

        public static GenericMethod GenerateConstructor1(ConstructorInfo ctor, Type argument1)
        {
            // https://ayende.com/blog/3167/creating-objects-perf-implications
            DynamicMethod method = new DynamicMethod("CreateIntance", ctor.DeclaringType, new[] { typeof(object[]) }, true);
            ILGenerator gen = method.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);//arr
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ldelem_Ref);
            gen.Emit(OpCodes.Castclass, argument1);
            gen.Emit(OpCodes.Newobj, ctor);// new Created
            gen.Emit(OpCodes.Ret);
            return (GenericMethod)method.CreateDelegate(typeof(GenericMethod));
        }

        public static GenericMethod GenerateConstructor2(ConstructorInfo ctor, Type argument1, Type argument2)
        {
            // https://ayende.com/blog/3167/creating-objects-perf-implications
            DynamicMethod method = new DynamicMethod("CreateIntance", ctor.DeclaringType, new[] { typeof(object[]) }, true);
            ILGenerator gen = method.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);//arr
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ldelem_Ref);
            gen.Emit(OpCodes.Castclass, argument1);
            gen.Emit(OpCodes.Ldarg_0);//arr
            gen.Emit(OpCodes.Ldc_I4_1);
            gen.Emit(OpCodes.Ldelem_Ref);
            gen.Emit(OpCodes.Castclass, argument2);
            gen.Emit(OpCodes.Newobj, ctor);// new Created
            gen.Emit(OpCodes.Ret);
            return (GenericMethod)method.CreateDelegate(typeof(GenericMethod));
        }
    }
}