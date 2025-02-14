using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Liella.Backend.Cold.Image.ELF {
    public enum ElfFileType {
        Unknown = 0x0,
        Relocatable = 0x01,
        Executable = 0x02,
        Shared = 0x03,
        Core = 0x04,
    }
    public enum ElfFileMachineType {
        X86 = 0x03,
        AMD64 = 0x3E
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ElfFileHeader<TPtrSize> where TPtrSize : unmanaged, INumber<TPtrSize> {
        private fixed byte m_MagicWord[4];
        private byte m_Class;
        private byte m_Endian;
        private byte m_Version;
        private fixed byte m_Padding[9];
        private short m_Type;
        private short m_MachineType;
        private int m_ElfVersion;
        private TPtrSize m_Entry;
        private TPtrSize m_ProgramHeaderOffset;
        private TPtrSize m_SectionHeaderOffset;
        private int m_Flags;
        private short m_ElfHeaderSize;
        private short m_ProgramHeaderSize;
        private short m_PrgoramHeaderNumber;
        private short m_SectionHeaderSize;
        private short m_SectionHeaderNumber;
        private short m_StringTableIndex;
        public uint Magic => (((uint)m_MagicWord[3]) << 24) | (((uint)m_MagicWord[2]) << 16) | (((uint)m_MagicWord[1]) << 8) | ((uint)m_MagicWord[0]);
        public bool Is64Bit => m_Class == 0x02;
        public bool IsLittleEndian => m_Endian == 0x01;
        public byte Version => m_Version;
        public int Flags => m_Flags;
        public ElfFileType Type => (ElfFileType)m_Type;
        public ElfFileMachineType MachineType => (ElfFileMachineType)m_MachineType;
        public int ElfVersion => m_ElfVersion;
        public TPtrSize Entry => m_Entry;
        public TPtrSize ProgramHeaderOffset => m_ProgramHeaderOffset;
        public TPtrSize SectionHeaderOffset => m_SectionHeaderOffset;
        public short ElfHeaderSize => m_ElfHeaderSize;
        public short ElfProgramHeaderSize => m_ProgramHeaderSize;
        public short SectionHeaderSize => m_SectionHeaderSize;
        private short PrgoramHeaderNumber => m_PrgoramHeaderNumber;
        public short SectionHeaderNumber => m_SectionHeaderNumber;
        public short StringTableIndex => m_StringTableIndex;
        public static int Size => sizeof(ElfFileHeader<TPtrSize>);

    }
    public enum ElfFileSectionType {
        Unused = 0x0,
        ProgramData = 0x1,
        SymbolTable = 0x2,
        StringTable = 0x3,
        Relocations = 0x4,
        HashTable = 0x5,
        Dynamic = 0x6,
        Notes = 0x7,
        Uninitialized = 0x8,
        RelocationNoAddends = 0x9,
        Reserved = 0xA,
        DynamicSymbolTable = 0xB,
        ArrayConstructors = 0xE,
        ArrayDestructors = 0xF
    }
    public enum ElfFileSectionFlags {
        Write = 0x1,
        Allocation = 0x2,
        Executable = 0x4,
        Merged = 0x10,
        Strings = 0x20,
        LinkInfo = 0x40,
        LinkOrder = 0x80,
        Tls = 0x400
    }
    public enum ElfSymbolVisibility {
        Local = 0x0,
        Global = 0x1,
        Weak = 0x2
    }
    public enum ElfSymbolType {
        NoType = 0x0,
        Object = 0x1,
        Function = 0x2,
        Section = 0x3,
        File = 0x4,
        Common = 0x5,
        Tls = 0x6
    }
    public interface IElfSymbol {
        public int NameOffset { get; }
        public ElfSymbolVisibility Visibility { get; }
        public ElfSymbolType Type { get; }
        public int StringTableIndex { get; }
        public long Value { get; }
        public long SymbolSize { get; }
    }
    public struct ElfFileSymbolData64 {
        private int m_Name;
        private byte m_TypeAttribute;
        private byte m_Other;
        private short m_StringTableIndex;
        private long m_Value;
        private long m_Size;
        public int NameOffset => m_Name;
        public ElfSymbolVisibility Visibility => (ElfSymbolVisibility)(m_TypeAttribute >> 4);
        public ElfSymbolType Type => (ElfSymbolType)(m_TypeAttribute & 0xf);
        public int StringTableIndex => m_StringTableIndex;
        public long Value => m_Value;
        public long SymbolSize => m_Size;
        public unsafe static int Size => sizeof(ElfFileSymbolData64);
    }
    public unsafe struct ElfFileSectionHeader<TPtrType> where TPtrType : unmanaged, INumber<TPtrType> {
        private int m_NameOffset;
        private int m_Type;
        private int m_Flags;
        private TPtrType m_VirtualAddress;
        private TPtrType m_SectionOffset;
        private TPtrType m_SecionSize;
        private int m_LinkedSection;
        private int m_ExtraInfo;
        private TPtrType m_Alignment;
        private TPtrType m_EntrySize;

        public int NameOffset => m_NameOffset;
        public ElfFileSectionType Type => (ElfFileSectionType)m_Type;
        public ElfFileSectionFlags Flags => (ElfFileSectionFlags)m_Flags;
        public TPtrType VirtualAddress => m_VirtualAddress;
        public TPtrType SectionOffset => m_SectionOffset;
        public TPtrType SectionSize => m_SecionSize;

        public int LinkedSection => m_LinkedSection;
        public int ExtraInfo => m_ExtraInfo;
        public TPtrType Alignment => m_Alignment;
        public TPtrType EntrySize => m_EntrySize;
        public static int Size => sizeof(ElfFileSectionHeader<TPtrType>);
    }
}
