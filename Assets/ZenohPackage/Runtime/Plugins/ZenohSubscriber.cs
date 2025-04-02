using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using Zenoh.Plugins;

namespace ZenohPackage.Plugins
{
    // Delegate for C# callback
    public delegate void SampleReceivedCallback(ZenohSampleRef sample);

    // Wrapper for z_owned_subscriber_t native type
    public unsafe class ZenohSubscriber : IDisposable
    {
        private z_owned_subscriber_t* nativePtr;
        private SampleReceivedCallback callback;
        private GCHandle callbackHandle; // To prevent garbage collection
        private ZenohClosureSample closure; // Keep a reference to prevent garbage collection
        private static readonly ZenohNative.z_closure_sample_call_delegate StaticCallbackHandler = HandleSampleCallback;

        public ZenohSubscriber()
        {
            // Allocate memory for the native subscriber
            nativePtr = (z_owned_subscriber_t*)Marshal.AllocHGlobal(sizeof(z_owned_subscriber_t));
        }

        // Creates a subscriber using the provided ZenohSession and ZenohKeyExpr.
        // Loans of objects are handled transparently.
        public void CreateSubscriber(ZenohSession session, ZenohKeyExpr keyExpr, SampleReceivedCallback callback = null)
        {
            this.callback = callback;
            
            // If we have a callback, create a GCHandle to prevent it from being garbage collected
            if (callback != null && !callbackHandle.IsAllocated)
            {
                callbackHandle = GCHandle.Alloc(this);
            }
            
            z_loaned_session_t* loanedSession = session.LoanSession();
            z_loaned_keyexpr_t* loanedKeyExpr = keyExpr.Loan();
            
            // Create a closure with our static callback handler if a callback was provided
            this.closure = callback != null 
                ? ZenohClosureSample.Create(StaticCallbackHandler, null, GCHandle.ToIntPtr(callbackHandle).ToPointer())
                : ZenohClosureSample.CreateDefault();
            
            z_subscriber_options_t options = new z_subscriber_options_t();
            ZenohNative.z_subscriber_options_default(&options);
            
            z_result_t result = ZenohNative.z_declare_subscriber(
                loanedSession,
                nativePtr,
                loanedKeyExpr,
                (z_moved_closure_sample_t*)this.closure.NativePointer,
                &options);
            if(result != z_result_t.Z_OK)
            {
                throw new Exception("Failed to create subscriber");
            }
        }

        // Static method to handle the native callback
        [MonoPInvokeCallback(typeof(ZenohNative.z_closure_sample_call_delegate))]
        private static void HandleSampleCallback(z_loaned_sample_t* sample, void* context)
        {
            if (context == null) return;
            
            // Convert context pointer back to GCHandle and get the subscriber instance
            GCHandle handle = GCHandle.FromIntPtr((IntPtr)context);
            ZenohSubscriber subscriber = (ZenohSubscriber)handle.Target;
            
            // Call the C# callback if it exists
            if (subscriber != null && subscriber.callback != null)
            {
                ZenohSampleRef zenohSample = new ZenohSampleRef(sample);
                subscriber.callback(zenohSample);
            }
        }

        public void Dispose()
        {
            if (nativePtr != null)
            {
                ZenohNative.z_subscriber_drop((z_moved_subscriber_t*)nativePtr);
                Marshal.FreeHGlobal((IntPtr)nativePtr);
                nativePtr = null;
            }
            
            if (callbackHandle.IsAllocated)
            {
                callbackHandle.Free();
            }
            
            if (closure != null)
            {
                closure.Dispose();
                closure = null;
            }
        }

        // Expose native pointer if needed for advanced operations
        internal z_owned_subscriber_t* NativePointer => nativePtr;
    }

    // Wrapper for z_owned_closure_sample_t native type
    public unsafe class ZenohClosureSample : IDisposable
    {
        private z_owned_closure_sample_t* nativePtr;
        private bool disposed = false;

        private ZenohClosureSample()
        {
            // Allocate memory for the native closure sample
            nativePtr = (z_owned_closure_sample_t*)Marshal.AllocHGlobal(sizeof(z_owned_closure_sample_t));
        }

        // Create a closure sample with explicit callbacks and context.
        internal static ZenohClosureSample Create(ZenohNative.z_closure_sample_call_delegate sampleCall, ZenohNative.z_closure_sample_drop_delegate dropCall, void* context)
        {
            ZenohClosureSample instance = new ZenohClosureSample();
            ZenohNative.z_closure_sample(instance.nativePtr, sampleCall, dropCall, context);
            return instance;
        }
        
        // Create a default closure sample with null callbacks.
        public static ZenohClosureSample CreateDefault()
        {
            return Create(null, null, null);
        }

        // Expose the native closure for use in native calls.
        internal z_owned_closure_sample_t* NativePointer => nativePtr;

        ~ZenohClosureSample()
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
                ZenohNative.z_closure_sample_drop((z_moved_closure_sample_t*)nativePtr);
                Marshal.FreeHGlobal((IntPtr)nativePtr);
                nativePtr = null;
                disposed = true;
            }
        }
    }
}