using System;
using System.Runtime.InteropServices;
using Zenoh.Plugins;

namespace ZenohPackage.Plugins
{
    // Options class for publisher declaration
    public class ZenohPublisherOptions
    {
        public ZenohPublisherOptions()
        {
            // Default constructor
        }

        // Apply options to native options structure
        internal unsafe void ApplyTo(z_publisher_options_t* options)
        {
            ZenohNative.z_publisher_options_default(options);
            // Apply custom options here if needed
        }
    }

    // Options class for publisher put operation
    public class ZenohPublisherPutOptions : IDisposable
    {
        private ZenohEncoding encoding;
        private bool disposed = false;

        public ZenohPublisherPutOptions()
        {
            // Default constructor
        }

        // Set encoding by moving ownership from the provided encoding
        public unsafe void SetMovedEncoding(ZenohEncoding sourceEncoding)
        {
            // Dispose existing encoding if any
            encoding?.Dispose();
            
            // Transfer ownership by moving the pointer
            if (sourceEncoding != null)
            {
                encoding = new ZenohEncoding(sourceEncoding.Move());
            }
            else
            {
                encoding = null;
            }
        }

        // Apply options to native options structure
        internal unsafe void ApplyTo(z_publisher_put_options_t* options)
        {
            ZenohNative.z_publisher_put_options_default(options);
            
            // Apply encoding if set - just cast to moved pointer
            if (encoding != null)
            {
                options->encoding = (z_moved_encoding_t*)encoding.NativePointer;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    encoding?.Dispose();
                    encoding = null;
                }

                disposed = true;
            }
        }

        ~ZenohPublisherPutOptions()
        {
            Dispose(false);
        }
    }

    // Wrapper for z_owned_publisher_t native type
    public unsafe class ZenohPublisher : IDisposable
    {
        private z_owned_publisher_t* nativePtr;
        private bool disposed = false;

        public ZenohPublisher()
        {
            // Allocate memory for the native publisher
            nativePtr = (z_owned_publisher_t*)Marshal.AllocHGlobal(sizeof(z_owned_publisher_t));
        }

        // Declare a publisher
        public void Declare(ZenohSession session, ZenohKeyExpr keyExpr, ZenohPublisherOptions options = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (keyExpr == null)
                throw new ArgumentNullException(nameof(keyExpr));

            z_loaned_session_t* loanedSession = session.LoanSession();
            z_loaned_keyexpr_t* loanedKeyExpr = keyExpr.Loan();
            
            z_publisher_options_t pubOptions = new z_publisher_options_t();
            if (options != null)
            {
                options.ApplyTo(&pubOptions);
            }
            else
            {
                ZenohNative.z_publisher_options_default(&pubOptions);
            }
            
            z_result_t result = ZenohNative.z_declare_publisher(
                loanedSession,
                nativePtr,
                loanedKeyExpr,
                &pubOptions);
                
            if (result != z_result_t.Z_OK)
            {
                throw new Exception("Failed to declare publisher");
            }
        }

        // Put string data
        public void Put(string data, ZenohPublisherPutOptions options = null)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            using (ZenohBytes bytes = new ZenohBytes(data))
            {
                PutBytes(bytes, options);
            }
        }

        // Put ZenohBytes data
        private void PutBytes(ZenohBytes bytes, ZenohPublisherPutOptions options = null)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            z_loaned_publisher_t* loanedPublisher = ZenohNative.z_publisher_loan(nativePtr);
            
            z_publisher_put_options_t putOptions = new z_publisher_put_options_t();
            if (options != null)
            {
                options.ApplyTo(&putOptions);
            }
            else
            {
                ZenohNative.z_publisher_put_options_default(&putOptions);
            }
            
            z_result_t result = ZenohNative.z_publisher_put(
                loanedPublisher, 
                (z_moved_bytes_t*)bytes.NativePointer, 
                &putOptions);
                
            if (result != z_result_t.Z_OK)
            {
                throw new Exception("Failed to publish data");
            }
        }

        ~ZenohPublisher()
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
                ZenohNative.z_publisher_drop((z_moved_publisher_t*)nativePtr);
                Marshal.FreeHGlobal((IntPtr)nativePtr);
                nativePtr = null;
                disposed = true;
            }
        }

        // Expose native pointer if needed for advanced operations
        internal z_owned_publisher_t* NativePointer => nativePtr;
    }
}