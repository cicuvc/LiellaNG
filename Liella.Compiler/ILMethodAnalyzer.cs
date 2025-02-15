using Liella.TypeAnalysis.Utils;
using Liella.TypeAnalysis.Utils.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Backend.Compiler {
    public class ILMethodBasicBlock {
        public ILMethodAnalyzer Analyzer { get; }
        public int StartIndex { get; }
        public int EndIndex { get; }
        public int Length => EndIndex - StartIndex;
        public ILMethodBasicBlock? TrueExit { get; set; }
        public ILMethodBasicBlock? FalseExit { get; set; }
        public (ILOpCode opcode, ulong operand) this[int index] {
            get {
                if(EndIndex - StartIndex <= index) {
                    throw new ArgumentOutOfRangeException("Out of Basic block");
                }
                return Analyzer.Decoder.Instructions[StartIndex + index];
            }
        }
        public ILMethodBasicBlock(ILMethodAnalyzer analyzer, int startIndex, int endIndex) {
            Analyzer = analyzer;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }

    }
    public class ILMethodAnalyzer : FwdGraph<ILMethodBasicBlock, bool> {
        public ILDecoder Decoder { get; }
        public ILMethodAnalyzer(ILDecoder decoder) {
            Decoder = decoder;

            var opCodeMap = ILDecoder.OpCodeMap;

            var candidateStart = new HashSet<(int index, bool isStart)>();
            var candidateEnd = new Dictionary<int, (FlowControl flow, int trueExit, int falseExit)>();
            var blocks = new Dictionary<int, ILMethodBasicBlock>();

            for(var i = 0; i < decoder.Instructions.Length; i++) {
                var (ilCode, operand) = decoder.Instructions[i];
                var codeInfo = opCodeMap[ilCode];

                // Mark end of basic block
                var isBranch = codeInfo.FlowControl == FlowControl.Branch;
                var isCondBranch = codeInfo.FlowControl == FlowControl.Cond_Branch;
                var isTerminal = codeInfo.FlowControl == FlowControl.Return || codeInfo.FlowControl == FlowControl.Throw;

                if(isBranch || isCondBranch) {
                    var target = (i+1) + (int)operand;
                    candidateStart.Add((target, true));
                    candidateEnd.Add(i, (codeInfo.FlowControl, target, -1));
                }
                if(isCondBranch) {
                    var trueTarget = (i + 1) + (int)operand;
                    var falseTarget = (i + 1);
                    candidateStart.Add((trueTarget, true));
                    candidateStart.Add((falseTarget, true));
                    candidateEnd.Add(i, (codeInfo.FlowControl, trueTarget, falseTarget));
                }
                if(isTerminal) {
                    candidateEnd.Add(i, (codeInfo.FlowControl, -1, -1));
                }
            }

            foreach(var i in candidateEnd) candidateStart.Add((i.Key, false));
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
                } else {
                    throw new InvalidProgramException("Broken control flow structure");
                }
            }

            foreach(var i in m_Edges.Keys) {
                var endIndex = i.EndIndex;
                var endInfo = candidateEnd[endIndex];

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
