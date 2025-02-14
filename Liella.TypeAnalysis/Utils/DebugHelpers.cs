using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Liella.TypeAnalysis.Namespaces;

namespace Liella.TypeAnalysis.Utils
{
    public static class DebugHelpers
    {
        public static void PrintImageNamespaceTree(NamespaceQueryTree tree)
        {
            CliTreePrint.TreePrint<NamespaceNodeBase>(tree.RootNamespace, (u) =>
            {
                return u;
            }, (e) =>
            {
                if (e is NamespaceNode)
                    return ($"\x1B[37m{e.Name}\x1B[0m", e.Name.Length);
                return ($"\x1B[32m{e.Name}\x1B[0m", e.Name.Length);
            });
        }
    }
}
