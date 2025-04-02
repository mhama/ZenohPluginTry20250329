using System.Runtime.InteropServices;

namespace Zenoh.Plugins
{
    internal static unsafe partial class ZenohNative
    {
        // custom string marshalling
        
        [DllImport(__DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern z_result_t z_string_copy_from_str(z_owned_string_t* this_,  [MarshalAs(UnmanagedType.LPStr)] string str);
        
        [DllImport(__DllName, EntryPoint = "z_keyexpr_from_str", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern z_result_t z_keyexpr_from_str(z_owned_keyexpr_t* @this, [MarshalAs(UnmanagedType.LPStr)] string expr);

        [DllImport(__DllName, EntryPoint = "z_bytes_copy_from_str", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern z_result_t z_bytes_copy_from_str(z_owned_bytes_t* @this, [MarshalAs(UnmanagedType.LPStr)] string str);
        
        [DllImport(__DllName, EntryPoint = "zc_config_from_str", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern z_result_t zc_config_from_str(z_owned_config_t* @this, [MarshalAs(UnmanagedType.LPStr)] string s);
    }
}
