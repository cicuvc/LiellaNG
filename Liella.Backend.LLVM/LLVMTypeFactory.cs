﻿using Liella.Backend.Components;
using Liella.Backend.LLVM.Types;
using Liella.Backend.Types;
using System.Collections.Immutable;

namespace Liella.Backend.LLVM {
    public class LLVMTypeFactory : CGenTypeFactory
    {
        public CodeGenContext Context { get; }
        public CodeGenTypeManager Manager { get; }
        public override ICGenNumericType Float32 { get; }

        public override ICGenNumericType Float64 { get; }

        public override ICGenType Void { get; }
        public override ICGenType Int1 { get; }
        public override ICGenType Int32 { get; }
        public override ICGenType VoidPtr { get; }

        public LLVMTypeFactory(CodeGenContext context)
        {
            Context = context;
            Manager = context.TypeManager;

            Float32 = LLVMNumericType.CreateFloat32(Manager);
            Float64 = LLVMNumericType.CreateFloat64(Manager);
            Void = LLVMVoidType.Create(Manager);
            Int1 = LLVMNumericType.CreateInt(1, Manager);
            Int32 = LLVMNumericType.CreateInt(32, Manager);
            VoidPtr = LLVMPointerType.Create(Void, 0, Manager);
        }

        public override ICGenArrayType CreateArray(ICGenType elementType, int elementCount)
        {
            return LLVMArrayType.Create(elementType, elementCount, Manager);
        }

        public override ICGenFunctionType CreateFunction(ReadOnlySpan<ICGenType> arguments, ICGenType returnType, bool isVarArgs = false)
        {
            return LLVMFunctionType.Create(arguments.ToImmutableArray(), returnType, isVarArgs, Manager);
        }

        public override ICGenNumericType CreateIntType(int width, bool unsigned)
        {
            return unsigned ? LLVMNumericType.CreateUInt(width, Manager) : LLVMNumericType.CreateInt(width, Manager);
        }

        public override ICGenPointerType CreatePointer(ICGenType elementType)
        {
            return LLVMPointerType.Create(elementType, 0, Manager);
        }

        public override ICGenStructType CreateStruct(ReadOnlySpan<ICGenType> types, string? name = null)
        {
            if (name is not null)
            {
                var structRef = CreateStruct(name);
                structRef.SetStructBody(types);
                return structRef;
            }
            return LLVMStructType.Create(types.ToImmutableArray(), Manager);
        }

        public override ICGenNamedStructType CreateStruct(string name) {
            var llvmContext = ((CodeGenLLVMContext)Context).ContextRef;
            var type = llvmContext.CreateNamedStruct(name);

            return LLVMNamedStructType.Create(type, name, Manager);
        }
    }
}
