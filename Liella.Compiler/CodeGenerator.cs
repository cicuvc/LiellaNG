using Liella.Backend.Compiler;
using Liella.Backend.Components;
using Liella.Backend.Types;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Compiler {
    public struct CodeBasicBlockIO {
        public ICGenType[] BlockInputTypes { get; }
        public CodeGenPhiValue[] BlockInputs { get; }
        public CodeGenValue[] BlockOutputs { get; }
        public CodeBasicBlockIO(int maxInputSize, int maxOutputSize) {
            BlockInputTypes = new ICGenType[maxInputSize];
            BlockInputs = new CodeGenPhiValue[maxInputSize];
            BlockOutputs = new CodeGenValue[maxOutputSize];
        }
    }
    public class CodeBasicBlockContext {
        public ILMethodBasicBlock BasicBlock { get; }
        public CodeGenBasicBlock CodeGenBlock { get; }
        public CodeBasicBlockIO BlockIO { get; protected set; }
        public int RequiredInputSize { get; protected set; }
        public int UpdateCounter { get; protected set; }
        public bool InQueue { get; protected set; }
        public CodeBasicBlockContext(ILMethodBasicBlock basicBlock,CodeGenBasicBlock codeGenBlock) {
            BasicBlock = basicBlock;
            CodeGenBlock = codeGenBlock;

            RequiredInputSize = -basicBlock.MinStackDepth;
        }
        public void MakeIOBuffer(FrozenDictionary<ILMethodBasicBlock, CodeBasicBlockContext> blockMap) {
            BlockIO = new CodeBasicBlockIO(RequiredInputSize, GetMaxSuccessorUseInput(blockMap));
        }

        protected void EnqueueBlock(Queue<CodeBasicBlockContext> updateQueue) {
            if(InQueue) return;
            InQueue = true;
            updateQueue.Enqueue(this);
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
                    blockMap[prev.Target].EnqueueBlock(updateQueue);
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
        }

        public void GenerateCode(ICodeGenerator emitter) {
            var functionBody = Method.GetMethodValueEnsureDef();

            var backendBasicBlocks = Method.ILCodeAnalyzer!
                .Select((e, idx) => {
                    var newBlock = functionBody.AddBasicBlock($"bb{idx}");
                    return new KeyValuePair<ILMethodBasicBlock, CodeBasicBlockContext>(e, new(e, newBlock));
                }).ToFrozenDictionary();

            var updateUpperBound = backendBasicBlocks.Count + 3;
            var updateQueue = new Queue<CodeBasicBlockContext>();
            foreach(var i in updateQueue) {
                i.UpdateInputSize(backendBasicBlocks, updateQueue);
            }
            while(updateQueue.Count != 0) {
                var currentNode = updateQueue.Dequeue();
                currentNode.UpdateInputSize(backendBasicBlocks, updateQueue);

                if(currentNode.UpdateCounter > updateUpperBound) {
                    throw new InvalidProgramException();
                }
            }
            foreach(var i in updateQueue) {
                i.MakeIOBuffer(backendBasicBlocks);
            }



        }

        private void GenerateForBlock() {

        }
    }
}
