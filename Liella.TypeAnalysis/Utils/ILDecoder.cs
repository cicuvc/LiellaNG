using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Liella.TypeAnalysis.Utils
{
    public class ILDecoder : IEnumerable<(int offset, ILOpCode opcode, ulong operand)>
    {
        public static FrozenDictionary<ILOpCode, OpCode> OpCodeMap { get; }
        public static FrozenDictionary<ILOpCode, int> OpCodeSizeMap { get; }
        static ILDecoder()
        {
            var opCodeMap = new Dictionary<ILOpCode, OpCode>();
            var opCodeSizeMap = new Dictionary<ILOpCode, int>();
            foreach (var i in typeof(OpCodes).GetFields())
            {
                var value = (OpCode)(i.GetValue(null) ?? default(OpCode));
                opCodeMap.Add((ILOpCode)value.Value, value);

                switch (value.OperandType)
                {
                    case OperandType.InlineNone:
                        {
                        opCodeSizeMap.Add((ILOpCode)value.Value, 0);
                            break;
                        }
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar:
                        {
                        opCodeSizeMap.Add((ILOpCode)value.Value, 1);
                            break;
                        }
                    case OperandType.InlineSwitch:
                        {
                        opCodeSizeMap.Add((ILOpCode)value.Value, 4);
                            break;
                        }

                    case OperandType.InlineVar:
                        {
                        opCodeSizeMap.Add((ILOpCode)value.Value, 2);
                            break;
                        }
                    case OperandType.ShortInlineR:
                    case OperandType.InlineBrTarget:
                    case OperandType.InlineField:
                    case OperandType.InlineI:
                    case OperandType.InlineMethod:
                    case OperandType.InlineSig:
                    case OperandType.InlineString:
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                        {
                        opCodeSizeMap.Add((ILOpCode)value.Value, 4);
                            break;
                        }
                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                        {
                        opCodeSizeMap.Add((ILOpCode)value.Value, 8);
                            break;
                        }
                    default: throw new NotImplementedException();
                }
            }
            OpCodeMap = opCodeMap.ToFrozenDictionary();
            OpCodeSizeMap = opCodeSizeMap.ToFrozenDictionary();
        }

        protected ImmutableArray<byte> m_ILCodes = ImmutableArray<byte>.Empty;
        protected ImmutableArray<(int offset, ILOpCode opcode, ulong operand)> m_Insts;
        public ImmutableArray<(int offset, ILOpCode opcode, ulong operand)> Instructions => m_Insts;
        public ILDecoder(ImmutableArray<byte> ilCode)
        {
            m_ILCodes = ilCode;

            var codeSpan = ilCode.AsSpan();
            var instBuilder = new List<(int offset, ILOpCode opcode, ulong operand)>();

            for (var i = 0; i < ilCode.Length;)
            {
                var (opcode, operand, size) = DecodeSingleOpCode(codeSpan.Slice(i));
                instBuilder.Add((i, opcode, operand));

                i += size;
            }

            var totalInstCount = instBuilder.Count;
            for(var i = 0; i < totalInstCount; i++) {
                var (offset, opcode, operand) = instBuilder[i];

                var codeInfo = OpCodeMap[opcode];

                if((codeInfo.FlowControl == FlowControl.Branch) || (codeInfo.FlowControl == FlowControl.Cond_Branch)) {
                    var nextInstStartOffset = (i == totalInstCount - 1) ? ilCode.Length : instBuilder[i + 1].offset;
                    var targetOffset = nextInstStartOffset + (int)operand;

                    var targetIndex = -1;
                    var l = 0;
                    var r = totalInstCount;
                    while(l + 1 <= r) {
                        var mid = (l + r) >> 1;
                        var midOffset = instBuilder[mid].offset;
                        if(midOffset > targetOffset) {
                            r = mid;
                        } else if(midOffset < targetOffset) {
                            l = mid + 1;
                        } else {
                            targetIndex = mid;
                            break;
                        }
                    }
                    if(targetIndex < 0)
                        throw new KeyNotFoundException($"IL instruction at offset {offset} not found");

                    instBuilder[i] = (offset, opcode, (ulong)targetIndex);
                }
            }

            m_Insts = instBuilder.ToImmutableArray();
        }
        protected static (ILOpCode code, ulong operand, int length) DecodeSingleOpCode(ReadOnlySpan<byte> code)
        {
            var i = 0;
            var ilOpcode = (ILOpCode)code[i++];
            if ((uint)ilOpcode >= 249)
            {
                ilOpcode = (ILOpCode)(((uint)ilOpcode << 8) + code[i++]);
            }
            var opcode = OpCodeMap[ilOpcode];


            var operandSize = opcode.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or
                OperandType.ShortInlineVar => 1,

                OperandType.InlineVar => 2,

                OperandType.InlineField or OperandType.ShortInlineR or
                OperandType.InlineI or OperandType.InlineMethod or
                OperandType.InlineSig or OperandType.InlineString or
                OperandType.InlineTok or OperandType.InlineType or
                OperandType.InlineBrTarget => 4,

                OperandType.InlineI8 or
                OperandType.InlineR => 8,

                _ => throw new NotImplementedException()

            };

            var operand = operandSize switch
            {
                0 => 0ul,
                1 => (ulong)(sbyte)code[i],
                2 => (ulong)MemoryMarshal.AsRef<short>(code.Slice(i, 2)),
                4 => (ulong)MemoryMarshal.AsRef<int>(code.Slice(i, 4)),
                8 => (ulong)MemoryMarshal.AsRef<long>(code.Slice(i, 8)),
                _ => throw new NotSupportedException()
            };

            return (ilOpcode, operand, i + operandSize);

        }

        public IEnumerator<(int offset, ILOpCode opcode, ulong operand)> GetEnumerator()
            => ((IEnumerable<(int offset, ILOpCode opcode, ulong operand)>)m_Insts).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
