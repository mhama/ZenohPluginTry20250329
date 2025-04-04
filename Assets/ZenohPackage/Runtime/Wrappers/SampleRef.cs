using Zenoh.Plugins;

namespace Zenoh
{
    // Wrapper for z_loaned_sample_t* native type
    public unsafe class SampleRef
    {
        private readonly z_loaned_sample_t* nativeSample;

        internal SampleRef(z_loaned_sample_t* sample)
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
}