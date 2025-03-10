using Liella.Backend.Compiler;

namespace Liella.Compiler.ILProcessors {
    public static class TypeCheckHelpers {
        public static bool OneOf<T>(LcTypeInfo type1, LcTypeInfo type2, out T? casted, out LcTypeInfo? other) where T : LcTypeInfo {
            (casted, other) = (null, null);
            if(type1 is T casted1) {
                (casted, other) = (casted1, type2);
                return true;
            }
            if(type2 is T casted2) {
                (casted, other) = (casted2, type1);
                return true;
            }
            return false;
        }

    }
}
