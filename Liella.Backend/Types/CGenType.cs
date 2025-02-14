using Liella.Backend.Components;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Formats.Tar;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Backend.Types
{
    public enum CGenTypeTag {
        Void = 0x8,
        Integer = 0x1,
        Function = 0x2,
        Float = 0x3,
        Pointer = 0x4,
        Struct = 0x5,
        Vector = 0x6,
        Array = 0x7,

        Unsigned = 0x100,
        ReferenceTypePtr = 0x200,
        ValueTypePtr = 0x400
    }

    public interface ICGenType<T> : ICGenType where T : ICGenType<T> {
        static abstract T CreateFromKey(T key, CodeGenTypeManager manager);
    }
    public abstract class CGenAbstractType<TType, TTag>: ICGenType, IEquatable<ICGenType> 
        where TType : CGenAbstractType<TType, TTag>, ICGenType<TType>
        where TTag: struct,IEquatable<TTag> {
        [ThreadStatic]
        protected static TType? m_HashKey;
        protected TTag m_InvariantPart;
        public TTag InvariantPart => m_InvariantPart;
        public abstract CGenTypeTag Tag { get; }
        public CGenAbstractType(in TTag invariant) {
            m_InvariantPart = invariant;
        }

        public bool Equals(ICGenType? other) {
            if(other is TType type) {
                return type.m_InvariantPart.Equals(m_InvariantPart);
            }
            return false;
        }
        public override int GetHashCode() => m_InvariantPart.GetHashCode();
        protected static TType CreateEntry(CodeGenTypeManager manager, in TTag invariantPart) {
            if(m_HashKey is null)
                m_HashKey = (TType)RuntimeHelpers.GetUninitializedObject(typeof(TType));
            m_HashKey.m_InvariantPart = invariantPart;
            return manager.GetEntryOrAdd(m_HashKey);
        }
    }
    public interface ICGenType:IEquatable<ICGenType> {
        CGenTypeTag Tag { get; }
    }
    public interface ICGenNumericType: ICGenType {
        public abstract int Width { get; }
    }
    public interface ICGenFunctionType : ICGenType {
        ICGenType ReturnType { get; }
        ReadOnlySpan<ICGenType> ParamTypes { get; }
    }
    public interface ICGenPointerType: ICGenType {
        ICGenType ElementType { get; }
    }
    public interface ICGenArrayType : ICGenType {
        ICGenType ElementType { get; }
        int Length { get; }
    }
    public interface ICGenStructType: ICGenType {
        public abstract ReadOnlySpan<ICGenType> Fields { get; }
    }

    public abstract class CGenTypeFactory {
        public abstract ICGenNumericType CreateIntType(int width, bool unsigned);
        public abstract ICGenNumericType Float32 { get; }
        public abstract ICGenNumericType Float64 { get; }
        public abstract ICGenType Void { get; }
        public abstract ICGenType Int1 { get; }

        public abstract ICGenStructType CreateStruct(ReadOnlySpan<ICGenType> types, bool packed, string? name = null);
        public abstract ICGenArrayType CreateArray(ICGenType elementType, int elementCount);
        public abstract ICGenPointerType CreatePointer(ICGenType elementType);
        public abstract ICGenFunctionType CreateFunction(ReadOnlySpan<ICGenType> arguments, ICGenType returnType, bool isVarArgs = false);
    }
}
