using System.Reflection.Metadata;
using System.Runtime.CompilerServices;


namespace Liella.TypeAnalysis.Utils
{
    public static class MetadataTokenHelpers
    {
        public static EntityHandle MakeEntityHandle(int metadataToken)
        {
            return Unsafe.BitCast<int, EntityHandle>(metadataToken);
        }
    }
}
