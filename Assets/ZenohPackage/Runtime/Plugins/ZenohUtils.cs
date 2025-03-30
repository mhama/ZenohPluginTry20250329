using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Zenoh.Plugins;

public static class ZenohUtils
{
    internal static unsafe void OpenSession(z_owned_session_t* ownedSession)
    {
        Debug.Log("Starting Zenoh open session test...");
        
        try
        {
            // 1. まずConfigurationを作成（所有権を持つ）
            z_owned_config_t ownedConfig = new z_owned_config_t();
            z_result_t configResult = ZenohNative.z_config_default(&ownedConfig);
            if (configResult != z_result_t.Z_OK)
            {
                Debug.LogError("Failed to create Zenoh config");
                return;
            }
            
            // 3. OpenOptionsを準備
            z_open_options_t openOptions = new z_open_options_t();
            ZenohNative.z_open_options_default(&openOptions);
            
            // 4. 設定を使用してセッションを開く
            // Rustと同様に、z_openはownedConfigの所有権を消費(move)する
            ZenohNative.z_config_default(&ownedConfig);
            
            configResult = ZenohNative.z_open(ownedSession, (z_moved_config_t *)&ownedConfig, &openOptions);


            if (configResult != z_result_t.Z_OK)
            {
                Debug.LogError("Failed to open Zenoh session");
                return;
            }
            
            Debug.Log("Zenoh session opened successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception in Zenoh open test: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    internal static unsafe void CloseSession(z_owned_session_t* ownedSession)
    {
        Debug.Log("Closing Zenoh session ...");
        try
        {
            // 5. セッションの借用（Rustのborrow概念）- 所有権なしで一時的に使用
            z_loaned_session_t* loanedSession = ZenohNative.z_session_loan(ownedSession);
            
            // 6. セッションを閉じる準備
            z_close_options_t closeOptions = new z_close_options_t();
            ZenohNative.z_close_options_default(&closeOptions);
            
            // 7. セッションを閉じる - 借用したポインタを使用
            z_result_t closeResult = ZenohNative.z_close(loanedSession, &closeOptions);
            if (closeResult != z_result_t.Z_OK)
            {
                Debug.LogError("Error closing Zenoh session");
            }
            
            // 8. リソースを解放 - 所有権を持つオブジェクトを明示的に解放
            ZenohNative.z_session_drop((z_moved_session_t*)ownedSession);
            
            Debug.Log("Zenoh session closed.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception in Zenoh close test: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
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

    internal static unsafe z_moved_string_t* z_move(z_owned_string_t* str) => (z_moved_string_t*)str;
}
