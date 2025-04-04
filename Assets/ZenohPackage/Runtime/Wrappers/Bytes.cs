using System;
using System.Runtime.InteropServices;
using Zenoh.Plugins;

namespace Zenoh
{
    // Wrapper for z_owned_bytes_t native type
    public unsafe class Bytes : IDisposable
    {
        private z_owned_bytes_t* nativePtr;
        private bool disposed = false;

        public Bytes()
        {
            // Allocate memory for the native bytes
            nativePtr = (z_owned_bytes_t*)Marshal.AllocHGlobal(sizeof(z_owned_bytes_t));
        }

        // Create from string
        public Bytes(string text) : this()
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            z_result_t result = ZenohNative.z_bytes_copy_from_str(nativePtr, text);
            if (result != z_result_t.Z_OK)
            {
                Dispose();
                throw new Exception("Failed to create bytes from string");
            }
        }
        
        internal z_loaned_bytes_t* Loan()
        {
            return ZenohNative.z_bytes_loan(nativePtr);
        }

        // Convert to byte array
        public byte[] ToByteArray()
        {
            z_owned_slice_t slice = new z_owned_slice_t();
            ZenohNative.z_bytes_to_slice(Loan(), &slice);
            
            z_loaned_slice_t* loanedSlice = ZenohNative.z_slice_loan(&slice);
            byte* buf = ZenohNative.z_slice_data(loanedSlice);
            long len = (long)ZenohNative.z_slice_len(loanedSlice);
            
            byte[] result = new byte[len];
            if (len > 0)
            {
                Marshal.Copy((IntPtr)buf, result, 0, (int)len);
            }
            
            ZenohNative.z_slice_drop((z_moved_slice_t*)&slice);
            
            return result;
        }

        // Convert to string
        public override string ToString()
        {
            byte[] bytes = ToByteArray();
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        ~Bytes()
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
            if (!disposed && nativePtr != null)
            {
                ZenohNative.z_bytes_drop((z_moved_bytes_t*)nativePtr);
                Marshal.FreeHGlobal((IntPtr)nativePtr);
                nativePtr = null;
                disposed = true;
            }
        }

        // Expose native pointer if needed for advanced operations
        internal z_owned_bytes_t* NativePointer => nativePtr;
    }
}