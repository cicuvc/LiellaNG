using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Liella.TypeAnalysis {
    public class AssemblyReaderTuple : IDisposable {
        private bool m_HasDisposed;
        public PEReader BinaryReader { get; }
        public MetadataReader MetaReader { get; }
        public bool IsPruneEnabled { get; }
        public AssemblyToken Token { get; }
        public AssemblyReaderTuple(Stream assemblyStream, bool isPruneEnabled) {
            BinaryReader = new(assemblyStream);
            MetaReader = BinaryReader.GetMetadataReader();
            IsPruneEnabled = isPruneEnabled;

            var asmDef = MetaReader.GetAssemblyDefinition();
            Token = new(MetaReader, asmDef);

        }

        protected virtual void Dispose(bool disposing) {
            if(!m_HasDisposed) {
                if(disposing) {
                    BinaryReader.Dispose();
                }
                m_HasDisposed = true;
            }
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
