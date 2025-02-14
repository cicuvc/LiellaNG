using Liella.TypeAnalysis.Metadata.Elements;
using Liella.TypeAnalysis.Metadata.Entry;
using Liella.TypeAnalysis.Utils.Graph;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;

using InstantStackNode = Liella.TypeAnalysis.Utils.Graph.GraphStructStack<Liella.TypeAnalysis.Metadata.Elements.IInstantiationEntry>.Node;


namespace Liella.TypeAnalysis.Metadata
{
    public class TypeCollector
    {
        public TypeEnvironment TypeEnv { get; }
        public HashSet<IEntityEntry> ActivatedEntity { get; } = new();
        protected Queue<IEntityEntry> m_CollectPendingQueue = new();
        protected HashSet<IInstantiationEntry> m_GenericInstEntity = new();
        protected Dictionary<MethodDefEntry, List<MethodDefEntry>> m_VirtualMethodChain = new();

        public TypeCollector(TypeEnvironment env)
        {
            TypeEnv = env;

        }
        public void CollectEntities(IEnumerable<IEntityEntry> collectRoot)
        {
            foreach (var i in collectRoot) NotifyEntity(i);

            while (m_CollectPendingQueue.Count != 0)
            {
                var currentEntity = m_CollectPendingQueue.Dequeue();
                if (ActivatedEntity.Contains(currentEntity)) continue;

                //if(currentEntity.Name.Contains("CC")) Debugger.Break();
                currentEntity.ActivateEntry(this);

                if (!currentEntity.IsGenericInstantiation)
                    ActivatedEntity.Add(currentEntity);
            }
        }

        public void RegisterVirtualChain(MethodDefEntry implMethod, MethodDefEntry prototype)
        {
            if (!m_VirtualMethodChain.TryGetValue(prototype, out var impls))
            {
                m_VirtualMethodChain.Add(prototype, impls = new List<MethodDefEntry>() { implMethod });
            }
            else
            {
                impls.Add(implMethod);
            }
        }
        public void NotifyEntity(IEntityEntry entry, TypeInstantiationEntry? optionTypeInst = null)
        {
            if (ActivatedEntity.Contains(entry)) return;

            if (entry.IsGenericInstantiation)
            {
                var primaryInst = ((IInstantiationEntry)entry).AsPrimary(TypeEnv.EntryManager);
                m_GenericInstEntity.Add(primaryInst);

                // [WARN] Do not delete
                //if(entry is TypeInstantiationEntry typeInst) {
                //    var definition = (TypeDefEntry)typeInst.Definition;
                //    foreach(var i in definition.GetDetails().VirtualMethods) {
                //        var virtualInstantiation = MethodInstantiation.Create(TypeEnv.EntryManager, typeInst, i, i.MethodArguments);

                //        NotifyEntity(virtualInstantiation);
                //    }
                //}
            }


            m_CollectPendingQueue.Enqueue(entry);

        }
        /// <summary>
        /// Expand tree structure of instantiation
        /// </summary>
        /// <param name="genericDAG"></param>
        /// <param name="typeInstantiation"></param>
        /// <param name="isPrimary"></param>
        protected void ExpandInstantiation(FwdGraph<IEntityEntry, (IInstantiationEntry? inst, bool isPrimary)> genericDAG, IInstantiationEntry typeInstantiation, bool isPrimary = true)
        {
            foreach (var (placeholder, instParam) in typeInstantiation.FormalArguments.Zip(typeInstantiation.ActualArguments))
            {
                var isLeaf = !(instParam is IInstantiationEntry);

                if (isPrimary || isLeaf)
                {
                    genericDAG.AddEdge(placeholder, instParam, (typeInstantiation, isPrimary));
                }
                else
                {
                    genericDAG.AddEdge(typeInstantiation, instParam, (typeInstantiation, isPrimary));
                }

                if (!isLeaf)
                {
                    ExpandInstantiation(genericDAG, (IInstantiationEntry)instParam, false);
                }
                else
                {
                    if (!isPrimary) genericDAG.AddEdge(typeInstantiation, placeholder, (typeInstantiation, isPrimary));
                }
            }
        }
        /// <summary>
        /// Make edges from instantiatons to generic placeholders
        /// </summary>
        /// <param name="genericDAG"></param>
        /// <param name="typeInstantiation"></param>
        /// <param name="isPrimary"></param>
        protected void ProcessInstantiation(FwdGraph<IEntityEntry, (IInstantiationEntry? inst, bool isPrimary)> genericDAG, IInstantiationEntry typeInstantiation, bool isPrimary = true)
        {
            foreach (var i in typeInstantiation.FormalArguments)
            {
                genericDAG.AddEdge(typeInstantiation, i, (typeInstantiation, true));
            }
            if (typeInstantiation is MethodInstantiation methodInst)
            {
                if (methodInst.ExactDeclType is IInstantiationEntry declInst)
                {
                    genericDAG.AddEdge(methodInst, declInst.AsPrimary(TypeEnv.EntryManager), (methodInst, true));
                }
                else
                {
                    genericDAG.AddEdge(methodInst, methodInst.ExactDeclType, (methodInst, true));
                }

            }
            ExpandInstantiation(genericDAG, typeInstantiation, true);
        }
        /// <summary>
        /// Instantiate all undertermined types defined on generic DAG
        /// </summary>
        /// <param name="genericDAG">Generic variale DAG</param>
        public void PopulateGenericInstantiations(FwdGraph<SCCNode<IEntityEntry>, FwdGraph<IEntityEntry, (IInstantiationEntry? inst, bool isPrimary)>.Edge> genericDAG)
        {

            var instantiationStack = new GraphStructStack<IInstantiationEntry>();

            var sortedDAG = GraphHelpers.TopoSort(genericDAG).Reverse();

            var sccNodeCandidateTypeCache = new Dictionary<SCCNode<IEntityEntry>, IEnumerable<(IEntityEntry type, InstantStackNode instStack, IInstantiationEntry actualInst)>>();
            var sccInstantiationCache = new Dictionary<SCCNode<IEntityEntry>, IEnumerable<(IEntityEntry type, InstantStackNode instStack)>>();

            var localInstantiationCache = new Dictionary<InstantStackNode, (int idx, ITypeEntry[] args)>();
            var localGenericFuncDeclTypeCache = new Dictionary<InstantStackNode, ITypeEntry>();

            // Do propagation
            foreach (var i in sortedDAG)
            {
                if (i.InternalNodes.First() is GenericPlaceholderTypeEntry)
                {

                    var candidates = new List<(IEntityEntry type, InstantStackNode instStack, IInstantiationEntry actualInst)>();
                    foreach (var j in genericDAG.GetForwardEdge(i))
                    {
                        var targetNode = j.Target;
                        var targetType = j.ExtraData.Target;
                        var actualInst = j.ExtraData.ExtraData.inst!;
                        var isPrimary = j.ExtraData.ExtraData.isPrimary;

                        if (targetType is GenericPlaceholderTypeEntry placeholder)
                        {
                            candidates.AddRange(
                                sccNodeCandidateTypeCache[targetNode].Select(e =>
                                (e.type, isPrimary ? instantiationStack.Push(e.instStack, actualInst) : e.instStack, actualInst)));
                        }
                        else
                        {
                            candidates.AddRange(
                                sccInstantiationCache[targetNode].Select(
                                e => (e.type, isPrimary ? instantiationStack.Push(e.instStack, actualInst) : e.instStack, actualInst)));
                        }
                    }
                    sccNodeCandidateTypeCache.Add(i, candidates);
                }
                else if (i.InternalNodes.First() is IInstantiationEntry)
                {
                    var declTypeNode = (SCCNode<IEntityEntry>?)null;

                    var instantiationNode = (IInstantiationEntry)i.InternalNodes.First();
                    var primaryNode = instantiationNode.IsPrimary;

                    localInstantiationCache.Clear();
                    localGenericFuncDeclTypeCache.Clear();

                    // Collect params
                    var paramIdx = 0;
                    var isSecondaryInst = false;
                    var maxStackDepth = 0;
                    foreach (var j in genericDAG.GetForwardEdge(i))
                    { // forward edge is sequential
                        isSecondaryInst |= !j.ExtraData.ExtraData.isPrimary;

                        var targetIsPlaceholder = sccNodeCandidateTypeCache.ContainsKey(j.Target);

                        // Find declType edge
                        if (!targetIsPlaceholder && primaryNode)
                        { // method primary instantiation
                            Debug.Assert(instantiationNode is MethodInstantiation);

                            declTypeNode = j.Target;
                        }
                        else
                        {
                            if (targetIsPlaceholder)
                            {
                                foreach (var k in sccNodeCandidateTypeCache[j.Target])
                                {
                                    if (k.actualInst != instantiationNode) continue;

                                    maxStackDepth = Math.Max(maxStackDepth, k.instStack.Length);
                                    if (!localInstantiationCache.TryGetValue(k.instStack, out var typeParamList))
                                    {
                                        localInstantiationCache.Add(k.instStack, typeParamList = (paramIdx, new ITypeEntry[instantiationNode.ArgumentCount]));
                                    }
                                    typeParamList.args[paramIdx] = (ITypeEntry)k.type;
                                }
                            }
                            else
                            {
                                foreach (var k in sccInstantiationCache[j.Target])
                                {
                                    maxStackDepth = Math.Max(maxStackDepth, k.instStack.Length);
                                    if (!localInstantiationCache.TryGetValue(k.instStack, out var typeParamList))
                                    {
                                        localInstantiationCache.Add(k.instStack, typeParamList = (paramIdx, new ITypeEntry[instantiationNode.ArgumentCount]));
                                    }
                                    typeParamList.args[paramIdx] = (ITypeEntry)k.type;
                                }
                            }
                            paramIdx++;
                        }
                    }

                    if (instantiationNode is MethodInstantiation methodInstNA && ((IInstantiationEntry)methodInstNA).ArgumentCount == 0)
                    {
                        Debug.Assert(declTypeNode is not null);

                        foreach (var (k, v) in sccInstantiationCache[declTypeNode])
                        {
                            localInstantiationCache.Add(instantiationStack.Push(v.Next!, instantiationNode), (0, []));
                            maxStackDepth = Math.Max(maxStackDepth, v.Length);
                        }
                    }

                    var staticKeys = localInstantiationCache.Where(e => e.Key.Length < maxStackDepth);
                    var dynamicKeys = localInstantiationCache.Where(e => e.Key.Length == maxStackDepth);

                    var prunedDynamicStacks = dynamicKeys.Select(e => e.Key.Next).ToHashSet();

                    foreach (var (k, v) in staticKeys)
                    {
                        foreach (var j in dynamicKeys)
                        {
                            if (j.Key.Length == maxStackDepth)
                            {
                                j.Value.args[v.idx] = v.args[v.idx];
                            }
                        }
                    }

                    // Handle situation that type arguments scope is different from method arguments
                    // For example, In scope [T1, T2] instantiation F<int>::Func<T1>, F is decoupled with context
                    if (declTypeNode is not null)
                    {
                        foreach (var k in sccInstantiationCache[declTypeNode])
                        {
                            // Not decoupled
                            if (prunedDynamicStacks.Contains(k.instStack.Next))
                            {
                                localGenericFuncDeclTypeCache.Add(instantiationStack.Push(k.instStack.Next ?? instantiationStack.Empty, instantiationNode), (ITypeEntry)k.type);
                            }
                            else
                            {
                                foreach (var (dynK, dynV) in dynamicKeys)
                                    localGenericFuncDeclTypeCache.Add(dynK, (ITypeEntry)k.type);
                            }
                        }
                    }

                    var instantiations = new List<(IEntityEntry type, InstantStackNode instStack)>();

                    foreach (var (k, v) in dynamicKeys)
                    {
                        if (instantiationNode is TypeInstantiationEntry typeInst)
                        {
                            var typeDef = (TypeDefEntry)instantiationNode.Definition;
                            var type = TypeInstantiationEntry.Create(TypeEnv.EntryManager, typeDef, v.args.ToImmutableArray(), true);
                            ActivatedEntity.Add(type);

                            Console.WriteLine($"Activate type {type}");

                            instantiations.Add((type, k));

                        }
                        else if (instantiationNode is MethodInstantiation methodInst)
                        {
                            var func = MethodInstantiation.Create(TypeEnv.EntryManager, localGenericFuncDeclTypeCache[k], (MethodDefEntry)methodInst.Definition, v.args.ToImmutableArray());

                            ActivatedEntity.Add(func);

                            Console.WriteLine($"Activate method {func}");
                            instantiations.Add((func, k));
                        }
                    }

                    sccInstantiationCache.Add(i, instantiations);


                }
                else
                {
                    var generalNode = i.InternalNodes.First();

                    sccInstantiationCache.Add(i, new[] { (generalNode, (InstantStackNode)instantiationStack.Empty) });
                }
            }


        }
        public void BuildGenericTypeDAG()
        {
            var genericParams = ActivatedEntity.Where(e => e is GenericPlaceholderTypeEntry).ToArray();

            var genericDAG = new FwdGraph<IEntityEntry, (IInstantiationEntry? inst, bool isPrimary)>();

            // map from type definition to type instantiations
            var gvnTypeInsts = m_GenericInstEntity.OfType<TypeInstantiationEntry>()
                .GroupBy(e => e.Definition).ToDictionary(e => e.Key);

            // map from (implClass, prototypeMethod) -> implMethod
            var prototypeToOverrideDefMap = m_GenericInstEntity.OfType<TypeInstantiationEntry>().SelectMany(e =>
            {
                return ((TypeDefEntry)e.Definition).Methods.Where(e => e.Attriutes.HasFlag(MethodAttributes.Virtual) && !e.Attriutes.HasFlag(MethodAttributes.NewSlot));
            }).ToDictionary(e =>
            {
                return (e.DeclType, e.GetDetails().VirtualMethodPrototype);
            });

            // all virtual method decl instantiation, gvn functions
            var gvnExpandSet = m_GenericInstEntity.Where(e =>
            {
                if (e is not MethodInstantiation methodInst) return false;
                return ((MethodDefEntry)methodInst.Definition).Attriutes.HasFlag(MethodAttributes.Virtual & MethodAttributes.NewSlot);
            }).Where(e => m_VirtualMethodChain.ContainsKey((MethodDefEntry)e.Definition)).ToArray();

            foreach (MethodInstantiation i in gvnExpandSet)
            {
                // gvn method def
                var methodDef = (MethodDefEntry)i.Definition;

                // gvn decl type def
                var declTypeDef = (TypeDefEntry)methodDef.DeclType;

                // all candidate override methods
                var expandSet = m_VirtualMethodChain[methodDef];

                foreach (var j in expandSet)
                {
                    if (gvnTypeInsts.TryGetValue(j.DeclType, out var typeInst))
                    {

                        // iterate over type instantiations of classes containing override methods
                        foreach (var k in typeInst)
                        {
                            //var declRealClass = LookupInheritChain(k, declTypeDef);

                            var overrideMethodDef = prototypeToOverrideDefMap[((ITypeEntry)k.Definition, methodDef)];

                            var overrideMethodInst = MethodInstantiation.Create(TypeEnv.EntryManager, k, overrideMethodDef, i.MethodArguments);

                            m_GenericInstEntity.Add(overrideMethodInst);
                        }
                    }
                    if (j.DeclType.TypeArguments.Length == 0)
                    { // non-generic type
                        var overrideMethodInst = MethodInstantiation.Create(TypeEnv.EntryManager, j.DeclType, j, i.MethodArguments);
                        m_GenericInstEntity.Add(overrideMethodInst);
                    }
                }
            }

            foreach (var i in m_GenericInstEntity)
            {
                ProcessInstantiation(genericDAG, i);
            }

            genericDAG.Dump();

            var sccGraph = GraphHelpers.Tarjan(genericDAG);

            foreach (var i in sccGraph)
            {
                if (i.InternalNodes.OfType<GenericPlaceholderTypeEntry>().Count() != 0 &&
                    i.InternalNodes.OfType<IInstantiationEntry>().Count() != 0)
                {
                    throw new Exception("Infinity generic loop detected");
                }
            }

            PopulateGenericInstantiations(sccGraph);
        }

    }
}
