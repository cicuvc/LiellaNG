namespace Liella.TypeAnalysis.Metadata.Elements
{
    public enum EntryTagType
    {
        TypeDefEntry = 0x0,
        MethodDefEntry = 0x1,
        GenericParamEntry = 0x2,
        FieldEntry = 0x3,

        TypeInstEntry = 0x4,
        MethodInstEntry = 0x5,

        Primitive = 0x6,
        Pointer = 0x7,
        Reference = 0x8
    }
}
