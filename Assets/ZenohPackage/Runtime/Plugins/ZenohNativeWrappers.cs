using System;
using System.Runtime.InteropServices;
using System.Text;
using Zenoh.Plugins;
using AOT;
using UnityEngine; // For MonoPInvokeCallback attribute

namespace Zenoh
{
    public struct ZResult
    {
        public static readonly ZResult Ok = new ZResult();
        public uint resultCode;

        public bool IsError => resultCode != 0;
        public bool IsOk => resultCode == 0;

        internal ZResult(z_result_t result)
        {
            resultCode = (uint) result;
        }
    }

    // Wrapper for z_owned_bytes_t native type
    public unsafe class ZenohBytes : IDisposable
    {
        private z_owned_bytes_t* nativePtr;
        private bool disposed = false;

        public ZenohBytes()
        {
            // Allocate memory for the native bytes
            nativePtr = (z_owned_bytes_t*)Marshal.AllocHGlobal(sizeof(z_owned_bytes_t));
        }

        // Create from string
        public ZenohBytes(string text) : this()
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
            return Encoding.UTF8.GetString(bytes);
        }

        ~ZenohBytes()
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

    // Wrapper for z_owned_encoding_t native type
    public unsafe class ZenohEncoding : IDisposable
    {
        private z_owned_encoding_t* nativePtr;
        private bool disposed = false;

        public ZenohEncoding()
        {
            // Allocate memory for the native encoding
            nativePtr = (z_owned_encoding_t*)Marshal.AllocHGlobal(sizeof(z_owned_encoding_t));
        }

        // Constructor that takes ownership of an existing native pointer
        internal ZenohEncoding(z_owned_encoding_t* ptr)
        {
            if (ptr == null)
                throw new ArgumentNullException(nameof(ptr));
                
            nativePtr = ptr;
            disposed = false;
        }

        // Create from loaned encoding
        internal static ZenohEncoding FromLoaned(z_loaned_encoding_t* loanedEncoding)
        {
            if (loanedEncoding == null)
                throw new ArgumentNullException(nameof(loanedEncoding));

            ZenohEncoding encoding = new ZenohEncoding();
            ZenohNative.z_encoding_clone(encoding.nativePtr, loanedEncoding);
            return encoding;
        }

        // Get text/plain encoding
        public static ZenohEncoding TextPlain()
        {
            z_loaned_encoding_t* textPlainEncoding = ZenohNative.z_encoding_text_plain();
            return FromLoaned(textPlainEncoding);
        }

        // Get application/json encoding
        public static ZenohEncoding ApplicationJson()
        {
            z_loaned_encoding_t* jsonEncoding = ZenohNative.z_encoding_application_json();
            return FromLoaned(jsonEncoding);
        }

        // Move ownership of the native pointer to the caller
        internal z_owned_encoding_t* Move()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ZenohEncoding));
                
            z_owned_encoding_t* result = nativePtr;
            nativePtr = null; // Relinquish ownership
            disposed = true;  // Mark as disposed
            return result;
        }

        ~ZenohEncoding()
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
                ZenohNative.z_encoding_drop((z_moved_encoding_t*)nativePtr);
                Marshal.FreeHGlobal((IntPtr)nativePtr);
                nativePtr = null;
                disposed = true;
            }
        }

        // Expose native pointer if needed for advanced operations
        internal z_owned_encoding_t* NativePointer => nativePtr;
    }

    // Wrapper for z_loaned_sample_t* native type
    public unsafe class ZenohSampleRef
    {
        private readonly z_loaned_sample_t* nativeSample;

        internal ZenohSampleRef(z_loaned_sample_t* sample)
        {
            nativeSample = sample;
        }

        public byte[] GetPayload()
        {
            z_loaned_bytes_t* bytes = ZenohNative.z_sample_payload(nativeSample);
            // Convert to byte array and return
            return ZenohUtils.ConvertToByteArray(bytes);
        }

        // Add other properties as needed
        public string GetKeyExpr()
        {
            z_loaned_keyexpr_t* keyExpr = ZenohNative.z_sample_keyexpr(nativeSample);
            return ZenohUtils.GetKeyExprAsString(keyExpr);
        }
    }

    // Wrapper for z_owned_keyexpr_t native type
    public unsafe class ZenohKeyExpr : IDisposable
    {
        private z_owned_keyexpr_t* nativePtr;

        public ZenohKeyExpr(string keyExprStr)
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

        ~ZenohKeyExpr()
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
