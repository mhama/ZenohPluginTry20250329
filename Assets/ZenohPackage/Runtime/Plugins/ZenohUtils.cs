using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Zenoh.Plugins;
using ZenohPackage.Plugins;

public static class ZenohUtils
{
    internal static unsafe string keyexprToStr(z_loaned_keyexpr_t *loanedKeyExpr)
    {
        z_view_string_t viewString = new z_view_string_t();
        ZenohNative.z_keyexpr_as_view_string(loanedKeyExpr, &viewString);
        z_loaned_string_t *loanedString = ZenohNative.z_view_string_loan(&viewString);
        return Marshal.PtrToStringAnsi((IntPtr)ZenohNative.z_string_data(loanedString));
    }
    
    // Wrapper method with the name used in ZenohSampleRef
    internal static unsafe string GetKeyExprAsString(z_loaned_keyexpr_t *loanedKeyExpr)
    {
        return keyexprToStr(loanedKeyExpr);
    }
    
    // Convert z_loaned_bytes_t to byte array
    internal static unsafe byte[] ConvertToByteArray(z_loaned_bytes_t* bytes)
    {
        if (bytes == null)
            return new byte[0];
            
        // Convert bytes to slice
        z_owned_slice_t slice = new z_owned_slice_t();
        ZenohNative.z_bytes_to_slice(bytes, &slice);
        
        // Loan the slice to access its data
        var loanedSlice = ZenohNative.z_slice_loan(&slice);
        
        // Get the data pointer and length
        byte* buf = ZenohNative.z_slice_data(loanedSlice);
        long len = (long)ZenohNative.z_slice_len(loanedSlice);
        
        // Create a new byte array and copy the data
        byte[] result = new byte[len];
        if (len > 0)
        {
            Marshal.Copy((IntPtr)buf, result, 0, (int)len);
        }
        
        // Clean up the slice
        ZenohNative.z_slice_drop((z_moved_slice_t*)&slice);
        
        return result;
    }

    internal static unsafe string stringToStr(z_loaned_string_t* loanedString)
    {
        return Marshal.PtrToStringAnsi((IntPtr)ZenohNative.z_string_data(loanedString));
    }

    internal static unsafe z_moved_string_t* z_move(z_owned_string_t* str) => (z_moved_string_t*)str;
}
