using Liella.Backend.Compiler;
using Liella.Backend.Components;
using Liella.Backend.Types;
using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Compiler {
    public struct BlockInputTypeList : IEquatable<BlockInputTypeList> {
        private ImmutableArray<LcTypeInfo> m_TypeArray;
        public BlockInputTypeList(ImmutableArray<LcTypeInfo> typeArray) {
            m_TypeArray = typeArray;
        }
        public LcTypeInfo this[int index] => m_TypeArray[index];
        public override int GetHashCode() {
            var hashCode = new HashCode();
            foreach(var i in m_TypeArray) hashCode.Add(i);
            return hashCode.ToHashCode();
        }
        public bool Equals(BlockInputTypeList other) {
            return m_TypeArray.SequenceEqual(other.m_TypeArray);
        }
    }
    public struct CodeBasicBlockIO {
        
        public HashSet<BlockInputTypeList> BlockInputTypes { get; }
        public CodeGenPhiValue[] BlockInputs { get; }
        public CodeGenValue[] BlockOutputs { get; }
        public CodeBasicBlockIO(int maxInputSize, int maxOutputSize) {
            BlockInputTypes = new HashSet<BlockInputTypeList>();
            BlockInputs = new CodeGenPhiValue[maxInputSize];
            BlockOutputs = new CodeGenValue[maxOutputSize];
        }
    }
    public class CodeBasicBlockContext {
        public ILMethodBasicBlock BasicBlock { get; }
        public CodeGenBasicBlock CodeGenBlock { get; }
        public CodeBasicBlockIO BlockIO { get; protected set; }
        public int RequiredInputSize { get; protected set; }
        public int UpdateCounter { get; set; }
        public bool InQueue { get; set; }
        public CodeBasicBlockContext(ILMethodBasicBlock basicBlock,CodeGenBasicBlock codeGenBlock) {
            BasicBlock = basicBlock;
            CodeGenBlock = codeGenBlock;

            RequiredInputSize = -basicBlock.MinStackDepth;
        }
        public void MakeIOBuffer(FrozenDictionary<ILMethodBasicBlock, CodeBasicBlockContext> blockMap) {
            BlockIO = new CodeBasicBlockIO(RequiredInputSize, GetMaxSuccessorUseInput(blockMap));
        }
        
        protected int GetMaxSuccessorUseInput(FrozenDictionary<ILMethodBasicBlock, CodeBasicBlockContext> blockMap) {
            var maxSuccessorUsedInput = 0;
            var trueExit = BasicBlock.TrueExit;
            var falseExit = BasicBlock.FalseExit;
            if(trueExit is not null) {
                maxSuccessorUsedInput = Math.Max(maxSuccessorUsedInput, blockMap[trueExit].RequiredInputSize);
            }
            if(falseExit is not null) {
                maxSuccessorUsedInput = Math.Max(maxSuccessorUsedInput, blockMap[falseExit].RequiredInputSize);
            }
            return maxSuccessorUsedInput;
        }
        public void UpdateInputSize(
            FrozenDictionary<ILMethodBasicBlock, CodeBasicBlockContext> blockMap,
            Queue<CodeBasicBlockContext> updateQueue) {

            /**
             * Here we use a SPFA-like(x) Bellman-ford-like method to determine the minimal 
             * number of phi nodes to be inserted at the beginning of the block.
             * Apply dynamic programming on CFG. State transition:
             * 
             * MaxSuccessorUseInput[i] = max { ReqiuredInputs[successors of i], 0 }
             * ReqiuredInputs[i] = max(-MinStackDepth, MaxSuccessorUseInput[i] - FinalStackDepth)
             *                          (Used by self)         (Used by successors)
             *                          
             * When current block exits, the stack contains at least *FinalStackDepth* elements (which is 
             * produced by current block) and if *FinalStackDepth* elements cannot meet the requirement of
             * all successors, we have to get MaxSuccessorUseInput[i] - FinalStackDepth inputs
             * 
             * In the DP process, ReqiuredInputs[i] may be affected by its successors, so we maintain a update
             * queue, and once ReqiuredInputs[i] is updated, the predecessors of [i] are inserted into update.
             * queue. For an N-node CFG, tt's easy to notice that ReqiuredInputs[i] must be non-descending, and 
             * if ReqiuredInputs[i] is updated for more than N times, its update path (the chain where the newest 
             * ReqiuredInputs[i] is progagated through) contains at least N + 1 nodes and there are at least one
             * node appearing at least twice, which indicates a updating loop to stop the algorithm from converging.
             * This situation only happens due to inbalanced stack operations or invalid intermediate codes. 
             * 
             * For an N-node CFG, each node has no more than 2 successors so time upper bound is O(2N^2). 
             */

            InQueue = false;
            UpdateCounter++;
            var maxSuccessorUsedInput = GetMaxSuccessorUseInput(blockMap);

            var newRequiredInputSize = Math.Max(-BasicBlock.MinStackDepth, maxSuccessorUsedInput - BasicBlock.FinalStackDepthDelta);
            if(RequiredInputSize < newRequiredInputSize) {
                RequiredInputSize = newRequiredInputSize;

                foreach(var prev in BasicBlock.Analyzer.GetInvEdge(BasicBlock)) {
                    var prevBlock = blockMap[prev.Target];
                    if(!prevBlock.InQueue) {
                        InQueue = true;

                        updateQueue.Enqueue(prevBlock);
                    }
                }
            }
        }
    }
    public struct CodeGenerator {
        public LcMethodInfo Method { get; }
        public CodeGenerator(LcMethodInfo method) {
            if(!method.HasBody) 
                throw new ArgumentException("Unable to generate code for dummy function");
            Method = method;

            SetupFunctionInfo();
        }

        public void SetupFunctionInfo() {
            var functionBody = Method.GetMethodValueEnsureDef();

            var backendBasicBlocks = Method.ILCodeAnalyzer!
                .Select((e, idx) => {
                    var newBlock = functionBody.AddBasicBlock($"bb{idx}");
                    return new KeyValuePair<ILMethodBasicBlock, CodeBasicBlockContext>(e, new(e, newBlock));
                }).ToFrozenDictionary();

            var updateUpperBound = backendBasicBlocks.Count + 3;
            var updateQueue = new Queue<CodeBasicBlockContext>();
            foreach(var (k, v) in backendBasicBlocks) {
                v.UpdateInputSize(backendBasicBlocks, updateQueue);
            }
            while(updateQueue.Count != 0) {
                var currentNode = updateQueue.Dequeue();
                currentNode.UpdateInputSize(backendBasicBlocks, updateQueue);

                if(currentNode.UpdateCounter > updateUpperBound) {
                    throw new InvalidProgramException();
                }
            }
            foreach(var (k, v) in backendBasicBlocks) {
                v.MakeIOBuffer(backendBasicBlocks);
            }

            /** Stack slot type collection stage
             * Since CFG may not be DAG, we cannot do topo-sort. To collect stack-propagated
             * types, the only thing to be promised is for every basic block at least one of its
             * predecessors got accessed before. So we start from entry block and do bfs
             */


            var typeCheckQueue = new Queue<(CodeBasicBlockContext block, ImmutableArray<LcTypeInfo>)>();
            var entryBlock = backendBasicBlocks[Method.ILCodeAnalyzer!.EntryBlock];
            entryBlock.InQueue = true;
            typeCheckQueue.Enqueue((entryBlock, ImmutableArray<LcTypeInfo>.Empty));

            var codeGenEvalContext = new CodeGenEvaluationContext(Method.Context);
            codeGenEvalContext.IsTypeOnlyStage = true;
            codeGenEvalContext.CurrentMethod = Method;

            var instDispatcher = Method.Context.CodeProcessor;

            while(typeCheckQueue.Count != 0) {
                var (currentBlock, currentStack) = typeCheckQueue.Dequeue();
                currentBlock.InQueue = false;

                var blockInputTypes = currentBlock.BlockIO.BlockInputTypes;
                var reuqiredInputSize = currentBlock.RequiredInputSize;
                if(reuqiredInputSize > currentStack.Length)
                    throw new InvalidProgramException("Stack inputs insufficient for current block");

                var inputTypeList = new BlockInputTypeList(currentStack[..reuqiredInputSize]);

                if(!blockInputTypes.Contains(inputTypeList)) {
                    blockInputTypes.Add(inputTypeList);

                    codeGenEvalContext.TypeStack.Clear();
                    foreach(var i in currentStack.Reverse()) codeGenEvalContext.TypeStack.Push(i);

                    foreach(var (offset, code, operand) in currentBlock.BasicBlock) {
                        instDispatcher.Emit(code, operand, codeGenEvalContext);
                    }

                    var finalStack = codeGenEvalContext.TypeStack.ToImmutableArray();

                    if(currentBlock.BasicBlock.TrueExit is not null) {
                        var trueBlock = backendBasicBlocks[currentBlock.BasicBlock.TrueExit];
                        if(!trueBlock.InQueue) {
                            trueBlock.InQueue = true;
                            typeCheckQueue.Enqueue((trueBlock, finalStack));
                        }
                    }

                    if(currentBlock.BasicBlock.FalseExit is not null) {
                        var falseBlock = backendBasicBlocks[currentBlock.BasicBlock.FalseExit];
                        if(!falseBlock.InQueue) {
                            falseBlock.InQueue = true;
                            typeCheckQueue.Enqueue((falseBlock, finalStack));
                        }
                    }
                }
            }

            // Check input type compatibliity
            foreach(var (k,v) in backendBasicBlocks) {
                using var blockBuilder = v.CodeGenBlock.GetCodeGenerator();
                var candidateInputTypes = v.BlockIO.BlockInputTypes.ToArray();
                for(var i = 0; i < v.RequiredInputSize; i++) {
                    var types = candidateInputTypes.Select(e => e[i]).ToArray();
                    var slotType = MergeInputTypes(types);

                    v.BlockIO.BlockInputs[i] = blockBuilder.CreatePhi(slotType);
                }
            }
        }

        private ICGenType MergeInputTypes(LcTypeInfo[] types) {
            var typeFactory = Method.CgContext.TypeFactory;
            if(types.All(e=>e is LcPointerTypeInfo)) {
                return typeFactory.CreatePointer(typeFactory.Void);
            }
            if(types.All(e => e is LcReferenceTypeInfo)) {
                return typeFactory.CreatePointer(typeFactory.Void);
            }
            if(types.All(e => e is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.Int32 })) {
                return typeFactory.Int32;
            }
            if(types.All(e => e is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.Int64 })) {
                return typeFactory.CreateIntType(64, false);
            }
            if(types.All(e => e is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.IntPtr })) {
                var defaultPtrSize = Method.CgContext.TypeManager.Configuration.PointerSize;
                return typeFactory.CreateIntType(defaultPtrSize * 8, false);
            }
            if(types.All(e => e is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.Single })) {
                return typeFactory.Float32;
            }
            if(types.All(e => e is LcPrimitiveTypeInfo { PrimitiveCode: PrimitiveTypeCode.Double })) {
                return typeFactory.Float64;
            }
            if(types.All(e => e is LcTypeDefInfo { Entry: { IsValueType: true } })) {
                var firstType = types.First().Entry;
                if(types.All(e=>e.Entry == firstType)) {
                    return Method.ResolveContextType(firstType).GetInstanceTypeEnsureDef();
                }
            }
            if(types.All(e => e is LcTypeDefInfo { Entry: { IsValueType: false } })) {
                return Method.Context.PrimitiveTypes[PrimitiveTypeCode.Object].GetInstanceTypeEnsureDef();
            }

            throw new NotSupportedException();
        }
        public void GenerateCode(ICodeGenerator emitter) {
            

        }

        private void GenerateForBlock() {

        }
    }
}
