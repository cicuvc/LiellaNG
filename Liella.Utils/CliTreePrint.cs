namespace Liella.TypeAnalysis {
    public class CliTreePrint {
        public static void TreePrint<TNode>(TNode root, Func<TNode, IEnumerable<TNode>> treeFunction, Func<TNode, (string line, int length)> nodeStringFunction) {
            var indentPosition = new List<(int pos, char ch)>();
            void PrintNode(TNode currentNode, int prefixLength) {
                var currentIndex = indentPosition.Count;
                
                var buffer = (Span<char>)stackalloc char[prefixLength];

                for(var i = 0; i < indentPosition.Count; i++) {
                    var indent = indentPosition[i];
                    buffer[indent.pos] = indent.ch;
                    if(indent.ch == '└') {
                        indentPosition[i] = (indent.pos, ' ');
                    }
                }
                    
                Console.Write(new string(buffer));
                Console.Write("─ ");

                var (nodeString, nodeLength) = nodeStringFunction(currentNode);
                Console.WriteLine(nodeString);

                var indentPos = prefixLength + 2 + nodeLength / 2;
                indentPosition.Add((indentPos, '│'));

                var enumerator = treeFunction(currentNode).GetEnumerator();

                var hasNext = enumerator.MoveNext();
                if(hasNext) {
                    do {
                        var currentValue = enumerator.Current;
                        hasNext = enumerator.MoveNext();

                        if(!hasNext) 
                            indentPosition[currentIndex] = (indentPos, '└');
                        PrintNode(currentValue, indentPos + 1);
                    } while(hasNext);
                }
                
                indentPosition.RemoveAt(indentPosition.Count - 1);
            }

            PrintNode(root, 0);
        }
    }

}
