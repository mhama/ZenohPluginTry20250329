using System;
using System.Runtime.InteropServices;
using Zenoh.Plugins;

namespace Zenoh
{
    // Wrapper for z_owned_encoding_t native type

    public unsafe class Encoding : IDisposable
    {
        private z_owned_encoding_t* nativePtr;
        private bool disposed = false;

        public Encoding()
        {
            // Allocate memory for the native encoding
            nativePtr = (z_owned_encoding_t*)Marshal.AllocHGlobal(sizeof(z_owned_encoding_t));
        }

        // Constructor that takes ownership of an existing native pointer
        internal Encoding(z_owned_encoding_t* ptr)
        {
            if (ptr == null)
                throw new ArgumentNullException(nameof(ptr));
                
            nativePtr = ptr;
            disposed = false;
        }

        // Create from loaned encoding
        internal static Encoding FromLoaned(z_loaned_encoding_t* loanedEncoding)
        {
            if (loanedEncoding == null)
                throw new ArgumentNullException(nameof(loanedEncoding));

            Encoding encoding = new Encoding();
            ZenohNative.z_encoding_clone(encoding.nativePtr, loanedEncoding);
            return encoding;
        }

        // Get text/plain encoding
        public static Encoding TextPlain()
        {
            z_loaned_encoding_t* textPlainEncoding = ZenohNative.z_encoding_text_plain();
            return FromLoaned(textPlainEncoding);
        }

        // Get application/json encoding
        public static Encoding ApplicationJson()
        {
            z_loaned_encoding_t* jsonEncoding = ZenohNative.z_encoding_application_json();
            return FromLoaned(jsonEncoding);
        }

        // Move ownership of the native pointer to the caller
        internal z_owned_encoding_t* Move()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(Encoding));
                
            z_owned_encoding_t* result = nativePtr;
            nativePtr = null; // Relinquish ownership
            disposed = true;  // Mark as disposed
            return result;
        }

        ~Encoding()
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
}