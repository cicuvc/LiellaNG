using System.Collections;
using System.Collections.Immutable;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Liella.TypeAnalysis.Utils
{
    public class ILDecoder : IEnumerable<(ILOpCode opcode, ulong operand)>
    {
        private static Dictionary<ILOpCode, OpCode> m_OpCodeMap = new();
        private static Dictionary<ILOpCode, int> m_OpCodeSizeMap = new();
        public static Dictionary<ILOpCode, OpCode> OpCodeMap => m_OpCodeMap;
        static ILDecoder()
        {
            foreach (var i in typeof(OpCodes).GetFields())
            {
                var value = (OpCode)(i.GetValue(null) ?? default(OpCode));
                m_OpCodeMap.Add((ILOpCode)value.Value, value);

                switch (value.OperandType)
                {
                    case OperandType.InlineNone:
                        {
                            m_OpCodeSizeMap.Add((ILOpCode)value.Value, 0);
                            break;
                        }
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar:
                        {
                            m_OpCodeSizeMap.Add((ILOpCode)value.Value, 1);
                            break;
                        }
                    case OperandType.InlineSwitch:
                        {
                            m_OpCodeSizeMap.Add((ILOpCode)value.Value, 4);
                            break;
                        }

                    case OperandType.InlineVar:
                        {
                            m_OpCodeSizeMap.Add((ILOpCode)value.Value, 2);
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
                            m_OpCodeSizeMap.Add((ILOpCode)value.Value, 4);
                            break;
                        }
                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                        {
                            m_OpCodeSizeMap.Add((ILOpCode)value.Value, 8);
                            break;
                        }
                    default: throw new NotImplementedException();
                }
            }
        }

        protected ImmutableArray<byte> m_ILCodes = ImmutableArray<byte>.Empty;
        protected ImmutableArray<(ILOpCode opcode, ulong operand)> m_Insts;
        public ILDecoder(ImmutableArray<byte> ilCode)
        {
            m_ILCodes = ilCode;

            var codeSpan = ilCode.AsSpan();
            var instBuilder = ImmutableArray.CreateBuilder<(ILOpCode opcode, ulong operand)>();

            for (var i = 0; i < ilCode.Length;)
            {
                var (opcode, operand, size) = DecodeSingleOpCode(codeSpan.Slice(i));
                instBuilder.Add((opcode, operand));

                i += size;
            }

            m_Insts = instBuilder.ToImmutable();
        }
        protected static (ILOpCode code, ulong operand, int length) DecodeSingleOpCode(ReadOnlySpan<byte> code)
        {
            var i = 0;
            var ilOpcode = (ILOpCode)code[i++];
            if ((uint)ilOpcode >= 249)
            {
                ilOpcode = (ILOpCode)(((uint)ilOpcode << 8) + code[i++]);
            }
            var opcode = m_OpCodeMap[ilOpcode];


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

        public IEnumerator<(ILOpCode opcode, ulong operand)> GetEnumerator()
            => ((IEnumerable<(ILOpCode opcode, ulong operand)>)m_Insts).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
