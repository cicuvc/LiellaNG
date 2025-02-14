using System.Reflection.Metadata;

namespace Liella.TypeAnalysis {
    public record AssemblyToken(
        string Name, Version Version,
        string Culture, byte[] PublicKeyToken) {
        public AssemblyToken(MetadataReader metaReader, AssemblyDefinition asmDef)
            : this(metaReader.GetString(asmDef.Name),
                  asmDef.Version,
                  metaReader.GetString(asmDef.Culture),
                  metaReader.GetBlobBytes(asmDef.PublicKey)) { }
        public AssemblyToken(MetadataReader metaReader, AssemblyReference asmRef)
            : this(metaReader.GetString(asmRef.Name),
                  asmRef.Version,
                  metaReader.GetString(asmRef.Culture),
                  metaReader.GetBlobBytes(asmRef.PublicKeyOrToken)) { }
    }
}
