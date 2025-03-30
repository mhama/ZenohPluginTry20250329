using System;
using System.Runtime.InteropServices;

namespace Zenoh.Plugins
{
#if UNITY_ANDROID && !UNITY_EDITOR
    // some definitions need to pass compilation
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct z_loaned_shm_client_storage_t { [NonSerialized]public int dummy; }
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct z_owned_shm_t  { [NonSerialized]public int dummy; }
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct z_loaned_shm_t  { [NonSerialized]public int dummy; }
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct z_moved_shm_t  { [NonSerialized]public int dummy; }
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct z_moved_shm_mut_t  { [NonSerialized]public int dummy; }
#endif
}