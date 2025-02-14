using Liella.Backend.Components;
using LLVMSharp;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liella.Backend.LLVM {
    public class LLVMCodeGenBinaryObject : CodeGenBinaryObject, IDisposable
    {
        protected nint m_Buffer;
        protected int m_Length;
        protected LLVMMemoryBufferRef m_MemBuffer;
        private bool disposedValue;

        public unsafe override ReadOnlySpan<byte> ObjectBuffer => new((void*)m_Buffer, m_Length);
        public unsafe LLVMCodeGenBinaryObject(LLVMMemoryBufferRef memBuffer)
        {
            m_MemBuffer = memBuffer;
            m_Buffer = (nint)LLVMSharp.Interop.LLVM.GetBufferStart(memBuffer);
            m_Length = (int)LLVMSharp.Interop.LLVM.GetBufferSize(memBuffer);
        }

        protected unsafe virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    m_Buffer = nint.Zero;
                    m_Length = 0;
                }

                LLVMSharp.Interop.LLVM.DisposeMemoryBuffer(m_MemBuffer);
                disposedValue = true;
            }
        }

        ~LLVMCodeGenBinaryObject()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
