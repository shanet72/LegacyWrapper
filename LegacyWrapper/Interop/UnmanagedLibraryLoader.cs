using System;
using System.Reflection;
using System.Reflection.Emit;
using LegacyWrapper.Common.Serialization;
using LegacyWrapper.ErrorHandling;
using PommaLabs.Thrower;

namespace LegacyWrapper.Interop
{
    internal class UnmanagedLibraryLoader
    {
        private const string AssemblyName = "LegacyWrapper";
        private const string ModuleName = "LegacyWrapper";
        private const string TypeName = "LegacyWrapper.WrapperType";

        public static CallResult InvokeUnmanagedFunction(CallData callData)
        {
            Type dllHandle = CreateTypeBuilder(callData);
            MethodInfo methodInfo = dllHandle.GetMethod(callData.ProcedureName);

            Raise<LegacyWrapperException>.If(methodInfo == null, $"Requested method {callData.ProcedureName} was not found in unmanaged DLL.");

            object result = methodInfo.Invoke(null, callData.Parameters);

            return new CallResult()
            {
                Result = result,
                Parameters = callData.Parameters
            };
        }

        private static Type CreateTypeBuilder(CallData callData)
        {
            AssemblyName asmName = new AssemblyName(AssemblyName);
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            ModuleBuilder modBuilder = asmBuilder.DefineDynamicModule(ModuleName);
            TypeBuilder typeBuilder = modBuilder.DefineType(TypeName, TypeAttributes.Class | TypeAttributes.Public);

            MethodBuilder pInvokeBuilder = modBuilder.DefinePInvokeMethod(
                name: callData.ProcedureName,
                dllName: callData.LibraryName,
                attributes: MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.PinvokeImpl,
                callingConvention: CallingConventions.Standard,
                returnType: callData.ReturnType,
                parameterTypes: callData.ParameterTypes,
                nativeCallConv: callData.CallingConvention,
                nativeCharSet: callData.CharSet);

            pInvokeBuilder.SetImplementationFlags(pInvokeBuilder.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);

            return typeBuilder.CreateType();
        }
    }
}
