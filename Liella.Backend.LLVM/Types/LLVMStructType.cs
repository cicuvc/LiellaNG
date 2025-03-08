using Liella.Backend.Types;
using LLVMSharp.Interop;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

namespace Liella.Backend.LLVM.Types
{
    [DebuggerVisualizer("Microsoft.VisualStudio.DebuggerVisualizers.TextVisualizer, Microsoft.VisualStudio.DebuggerVisualizers")]
    public class LLVMNamedStructType:CGenAbstractType<LLVMNamedStructType, LLVMNamedStructTag>, ICGenType<LLVMNamedStructType>, ICGenNamedStructType, ILLVMType {
        protected (ICGenType type, int offset)[] m_Fields = Array.Empty<(ICGenType types, int offset)>();
        protected int m_Size;
        protected int m_Alignment;
        public override CGenTypeTag Tag => CGenTypeTag.Struct;
        LLVMTypeRef ILLVMType.InternalType => InvariantPart.InternalType;

        public ReadOnlySpan<(ICGenType type, int offset)> Fields => m_Fields;

        public override int Size => m_Size;

        public override int Alignment => m_Alignment;
        public string Name => InvariantPart.Name;
        public LLVMNamedStructType(in LLVMNamedStructTag tag) : base(tag) { }
        public static LLVMNamedStructType CreateFromKey(LLVMNamedStructType key, CodeGenTypeManager manager) {
            return new(key.InvariantPart);
        }
        public static LLVMNamedStructType Create(LLVMTypeRef namedStruct, string name, CodeGenTypeManager manager) {
            return CreateEntry(manager, new LLVMNamedStructTag(namedStruct, name));
        }

        public void SetStructBody(ReadOnlySpan<ICGenType> fields) {
            var fieldsArray = fields.ToArray();
            m_Fields = fieldsArray.Zip(CGenStructLayoutHelpers.LayoutStruct(fields, out m_Size)).ToArray();
            m_Alignment = fieldsArray.Select(e => e.Alignment).DefaultIfEmpty(1).Max();

            InvariantPart.InternalType.StructSetBody(m_Fields.Select(e => ((ILLVMType)e.type).InternalType).ToArray(), false);
        }
        public override string ToString() {
            var printer = new CGenFormattedPrinter();
            PrettyPrint(printer, 1);
            return printer.Dump();
        }

        public override void PrettyPrint(CGenFormattedPrinter printer, int expandLevel) {
            if(expandLevel <= 0) printer.Append(Name);
            else {
                printer.Append($"struct {Name} = {{");
                using(printer.BeginIndent()) {
                    foreach(var i in m_Fields) {
                        i.type.PrettyPrint(printer, expandLevel - 1);
                        printer.AppendLine(",");
                    }
                }
                printer.Append("}");
            }
        }
    }
    public class LLVMStructType : CGenAbstractType<LLVMStructType, LLVMStructTag>, ICGenType<LLVMStructType>, ICGenStructType, ILLVMType
    {
        public override CGenTypeTag Tag => CGenTypeTag.Struct;
        LLVMTypeRef ILLVMType.InternalType => InvariantPart.InternalType;

        public ImmutableArray<(ICGenType type, int offset)> StructTypes => InvariantPart.StructTypes;


        public override int Size => InvariantPart.Size;

        public override int Alignment => InvariantPart.Alignment;

        ReadOnlySpan<(ICGenType type, int offset)> ICGenStructType.Fields => InvariantPart.StructTypes.AsSpan();

        public LLVMStructType(in LLVMStructTag tag) : base(tag) {
        
        }
        public static LLVMStructType CreateFromKey(LLVMStructType key, CodeGenTypeManager manager)
        {
            return new(key.InvariantPart);
        }
        public static LLVMStructType Create(ImmutableArray<ICGenType> elemenetTypes, CodeGenTypeManager manager)
        {
            var types = elemenetTypes.Select(e => ((ILLVMType)e).InternalType).ToArray();
            return CreateEntry(manager, new LLVMStructTag(LLVMTypeRef.CreateStruct(types, false), elemenetTypes));
        }
        public static LLVMStructType Create(LLVMTypeRef types, ImmutableArray<ICGenType> elemenetTypes, CodeGenTypeManager manager)
        {
            return CreateEntry(manager, new LLVMStructTag(types, elemenetTypes));
        }
        public override void PrettyPrint(CGenFormattedPrinter printer, int expandLevel) {
            printer.AppendLine("struct {");
            using(printer.BeginIndent()) {
                foreach(var i in InvariantPart.StructTypes) {
                    i.type.PrettyPrint(printer, expandLevel - 1);
                    printer.AppendLine(",");
                }
            }
            printer.Append("}");
        }
        public override string ToString() {
            var printer = new CGenFormattedPrinter();
            PrettyPrint(printer, 4);
            return printer.Dump();
        }

    }
}
