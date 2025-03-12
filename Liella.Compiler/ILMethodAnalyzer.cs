using Liella.Backend.Components;
using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;
using Liella.TypeAnalysis.Utils;
using Liella.TypeAnalysis.Utils.Graph;
using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Backend.Compiler {
    public class ILMethodBasicBlock {
        public static FrozenDictionary<StackBehaviour, int> StackDepthDelta = (new Dictionary<StackBehaviour, int>() {
            { StackBehaviour.Pop0, 0 },
            { StackBehaviour.Pop1, -1 },
            { StackBehaviour.Pop1_pop1, -2 },
            { StackBehaviour.Popi, -1 },
            { StackBehaviour.Popi_pop1, -2},
            { StackBehaviour.Popi_popi, -2 },
            { StackBehaviour.Popi_popi8, -2 },
            { StackBehaviour.Popi_popi_popi, -3 },
            { StackBehaviour.Popi_popr4, -2 },
            { StackBehaviour.Popi_popr8, -2 },
            { StackBehaviour.Popref, -1 },
            { StackBehaviour.Popref_pop1, -2 },
            { StackBehaviour.Popref_popi, -2 },
            { StackBehaviour.Popref_popi_pop1, -3 },
            { StackBehaviour.Popref_popi_popi, -3 },
            { StackBehaviour.Popref_popi_popi8, -3 },
            { StackBehaviour.Popref_popi_popr4, -3 },
            { StackBehaviour.Popref_popi_popr8, -3 },
            { StackBehaviour.Popref_popi_popref, -3 },
            { StackBehaviour.Push0, 0 },
            { StackBehaviour.Push1, 1 },
            { StackBehaviour.Push1_push1, 2 },
            { StackBehaviour.Pushi, 1 },
            { StackBehaviour.Pushi8, 1 },
            { StackBehaviour.Pushr4, 1 },
            { StackBehaviour.Pushr8, 1 },
            { StackBehaviour.Pushref, 1 },
        }).ToFrozenDictionary();
        public ILMethodAnalyzer Analyzer { get; }
        public int StartIndex { get; }
        public int EndIndex { get; }
        public int Length => EndIndex - StartIndex;
        public ILMethodBasicBlock? TrueExit { get; set; }
        public ILMethodBasicBlock? FalseExit { get; set; }
        public int MinStackDepth { get; }
        public int MaxStackDepth { get; }
        public int FinalStackDepthDelta { get; }
        public (int offset, ILOpCode opcode, ulong operand) this[int index] {
            get {
                if(EndIndex - StartIndex <= index) {
                    throw new ArgumentOutOfRangeException("Out of Basic block");
                }
                return Analyzer.Decoder.Instructions[StartIndex + index];
            }
        }
        public static int ResolveVarPushSize(IMethodEntry methodEntry,ILOpCode code, ulong operand, StackBehaviour behavior) {
            switch(code) {
                case ILOpCode.Call:
                case ILOpCode.Callvirt: {
                    var targetMethod = methodEntry.TypeEnv.TokenResolver.ResolveMethodToken(methodEntry.AsmInfo, MetadataTokenHelpers.MakeEntityHandle((int)operand), methodEntry.GetGenericContext(), out var declType);
                    return targetMethod.Signature.ReturnType is PrimitiveTypeEntry { InvariantPart: { TypeCode: PrimitiveTypeCode.Void } } ? 0 : 1;
                }
                case ILOpCode.Ret: {
                    return 0;
                }
                default: {
                    throw new NotImplementedException();
                }
            }
        }
        public static int ResolveVarPopSize(IMethodEntry methodEntry, ILOpCode code, ulong operand, StackBehaviour behavior) {
            switch(code) {
                case ILOpCode.Newobj:
                case ILOpCode.Call:
                case ILOpCode.Callvirt: {
                    var targetMethod = methodEntry.TypeEnv.TokenResolver.ResolveMethodToken(methodEntry.AsmInfo, MetadataTokenHelpers.MakeEntityHandle((int)operand), methodEntry.GetGenericContext(), out var declType);
                    var popSize = targetMethod.Signature.ParameterTypes.Length;
                    if((!targetMethod.Attributes.HasFlag(MethodAttributes.Static)) && (code != ILOpCode.Newobj)) popSize++;
                    return -popSize;
                }
                case ILOpCode.Ret: {
                    return methodEntry.Signature.ReturnType is PrimitiveTypeEntry { InvariantPart: { TypeCode: PrimitiveTypeCode.Void } } ? 0 : -1;
                }
                default: {
                    throw new NotImplementedException();
                }
            }
        }
        public ILMethodBasicBlock(ILMethodAnalyzer analyzer, int startIndex, int endIndex) {
            Analyzer = analyzer;
            StartIndex = startIndex;
            EndIndex = endIndex;


            var initStackDepth = 0;
            var minStackDepth = int.MaxValue;
            var maxStackDepth = -int.MaxValue;
            var instructions = analyzer.Decoder.Instructions[startIndex..endIndex];

            foreach(var (_, code, operand) in instructions) {
                var opCodeInfo = ILDecoder.OpCodeMap[code];
                var pushDelta = StackDepthDelta.TryGetValue(opCodeInfo.StackBehaviourPush, out var pushD) ? pushD : ResolveVarPushSize(analyzer.Entry, code, operand, opCodeInfo.StackBehaviourPush);
                var popDelta = StackDepthDelta.TryGetValue(opCodeInfo.StackBehaviourPop, out var popD) ? popD : ResolveVarPopSize(analyzer.Entry, code, operand, opCodeInfo.StackBehaviourPop);

                initStackDepth += popDelta;
                minStackDepth = Math.Min(minStackDepth, initStackDepth);

                initStackDepth += pushDelta;
                maxStackDepth = Math.Max(maxStackDepth, initStackDepth);
            }

            MinStackDepth = minStackDepth;
            MaxStackDepth = maxStackDepth;
            FinalStackDepthDelta = initStackDepth;
        }

        public ImmutableArray<(int offset, ILOpCode opcode, ulong operand)>.Enumerator GetEnumerator() {
            return Analyzer.Decoder.Instructions[StartIndex..EndIndex].GetEnumerator();
        }
    }
    public class ILMethodAnalyzer : FwdGraph<ILMethodBasicBlock, bool> {
        public IMethodEntry Entry { get; }
        public ILDecoder Decoder { get; }
        public ILMethodBasicBlock EntryBlock { get; } = null!;
        public ILMethodAnalyzer(IMethodEntry entry,ILDecoder decoder) {
            Decoder = decoder;
            Entry = entry;

            var opCodeMap = ILDecoder.OpCodeMap;

            var candidateStart = new HashSet<(int index, bool isStart)>();
            var candidateEnd = new Dictionary<int, (FlowControl flow, int trueExit, int falseExit)>();
            var blocks = new Dictionary<int, ILMethodBasicBlock>();

            candidateStart.Add((0, true));

            for(var i = 0; i < decoder.Instructions.Length; i++) {
                var (currInstOffset, ilCode, operand) = decoder.Instructions[i];
                
                var codeInfo = opCodeMap[ilCode];

                // Mark end of basic block
                var isBranch = codeInfo.FlowControl == FlowControl.Branch;
                var isCondBranch = codeInfo.FlowControl == FlowControl.Cond_Branch;
                var isTerminal = codeInfo.FlowControl == FlowControl.Return || codeInfo.FlowControl == FlowControl.Throw;

                if(isBranch) {
                    var target = (int)operand;
                    candidateStart.Add((target, true));
                    candidateEnd.Add(i, (codeInfo.FlowControl, target, -1));
                }
                if(isCondBranch) {
                    var trueTarget = (int)operand;
                    var falseTarget = (i + 1);
                    candidateStart.Add((trueTarget, true));
                    candidateStart.Add((falseTarget, true));
                    candidateEnd.Add(i, (codeInfo.FlowControl, trueTarget, falseTarget));
                }
                if(isTerminal) {
                    candidateEnd.Add(i, (codeInfo.FlowControl, -1, -1));
                }
            }

            foreach(var i in candidateEnd) candidateStart.Add((i.Key + 1, false));
            var sections = candidateStart.ToList();
            sections.Sort((u, v) => {
                if(u.index == v.index) return (u.isStart ? 1 : -1);
                return u.index.CompareTo(v.index);
            });

            if(sections.Count % 2 != 0) {
                throw new InvalidProgramException("Broken control flow");
            }

            for(var i = 0; i < sections.Count; i += 2) {
                if(sections[i].isStart && !sections[i + 1].isStart) {
                    var basicBlock = new ILMethodBasicBlock(this, sections[i].index, sections[i + 1].index);
                    AddNode(basicBlock);

                    blocks.Add(basicBlock.StartIndex, basicBlock);

                    if(basicBlock.StartIndex == 0) EntryBlock = basicBlock;
                } else {
                    throw new InvalidProgramException("Broken control flow structure");
                }
            }

            foreach(var i in m_Edges.Keys) {
                var endIndex = i.EndIndex;
                var endInfo = candidateEnd[endIndex - 1];

                if(endInfo.trueExit >= 0) {
                    AddEdge(i, i.TrueExit = blocks[endInfo.trueExit], true);
                }
                if(endInfo.falseExit >= 0) {
                    AddEdge(i, i.FalseExit = blocks[endInfo.falseExit], false);
                }
            }
        }
    }
}
