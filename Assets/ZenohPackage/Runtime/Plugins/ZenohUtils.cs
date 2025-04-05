using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Zenoh.Plugins;

public static class ZenohUtils
{
    internal static unsafe string keyexprToStr(z_loaned_keyexpr_t *loanedKeyExpr)
    {
        z_view_string_t viewString = new z_view_string_t();
        ZenohNative.z_keyexpr_as_view_string(loanedKeyExpr, &viewString);
        z_loaned_string_t *loanedString = ZenohNative.z_view_string_loan(&viewString);
        return Marshal.PtrToStringAnsi((IntPtr)ZenohNative.z_string_data(loanedString));
    }

    internal static unsafe string stringToStr(z_loaned_string_t* loanedString)
    {
        return Marshal.PtrToStringAnsi((IntPtr)ZenohNative.z_string_data(loanedString));
    }
}
