using Liella.Backend.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Compiler.ILProcessors {
    public class ArithmeticEmit : ICodeProcessor {
        public string Name => nameof(ArithmeticEmit);
        [ILCodeHandler(ILOpCode.Add)]
        public void ArithmetricAdd(ILOpCode opcode, ulong operand) {

        }
    }
}
