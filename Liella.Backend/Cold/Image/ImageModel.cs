using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Backend.Cold.Image {
    public struct SymbolName {
        public string Name { get; set; }
    }
    public enum SymbolAttributes {

    }
    public class LinkerMemoryInfo {
        public LinkerMemoryInfo(string name, ulong address, ulong length) {
            Name = name;
            Address = address;
            Length = length;
        }

        public string Name { get; }
        public ulong Address { get; }
        public ulong Length { get; }

    }
    public abstract class LinkerSymbol {
        public SymbolName SymName { get; }
    }
    public abstract class SectionConstructionOp {

    }
    
    public class ImportSection {

    }
    public class ImageModel {
        public SymbolName EntryPoint { get; set; }
        public Dictionary<string, LinkerMemoryInfo> MemoryRegions { get; } = new();

    }
}
