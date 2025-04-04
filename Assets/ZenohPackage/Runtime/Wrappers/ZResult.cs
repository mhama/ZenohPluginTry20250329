using Zenoh.Plugins;

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
            resultCode = (uint)result;
        }
    }
}