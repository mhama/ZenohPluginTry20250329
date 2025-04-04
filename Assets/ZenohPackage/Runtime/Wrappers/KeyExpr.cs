using System;
using System.Runtime.InteropServices;
using Zenoh.Plugins;

namespace Zenoh
{
    // Wrapper for z_owned_keyexpr_t native type
    public unsafe class KeyExpr : IDisposable
    {
        private z_owned_keyexpr_t* nativePtr;

        public KeyExpr(string keyExprStr)
        {
            // Allocate memory for the native key expression
            nativePtr = (z_owned_keyexpr_t*)Marshal.AllocHGlobal(sizeof(z_owned_keyexpr_t));
            
            z_result_t result = ZenohNative.z_keyexpr_from_str(nativePtr, keyExprStr);
            if(result != z_result_t.Z_OK)
            {
                Marshal.FreeHGlobal((IntPtr)nativePtr);
                nativePtr = null;
                throw new Exception("Failed to create key expression");
            }
        }

        // Transparently loan the key expression to obtain a z_loaned_keyexpr_t*
        internal z_loaned_keyexpr_t* Loan()
        {
            return ZenohNative.z_keyexpr_loan(nativePtr);
        }

        ~KeyExpr()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (nativePtr != null)
            {
                ZenohNative.z_keyexpr_drop((z_moved_keyexpr_t*)nativePtr);
                Marshal.FreeHGlobal((IntPtr)nativePtr);
                nativePtr = null;
            }
        }

        // Expose native pointer if needed for advanced operations
        internal z_owned_keyexpr_t* NativePointer => nativePtr;
    }
}