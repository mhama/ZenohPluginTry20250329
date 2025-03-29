using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

using Zenoh.Plugins;

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

    }
}

public unsafe class ZenohNativeTest : MonoBehaviour
{
    z_owned_session_t *ownedSessionPtr;// = new z_owned_session_t();
    z_owned_subscriber_t *ownedSubscriberPtr;
    z_owned_publisher_t *ownedPublisherPtr;// = new z_owned_publisher_t();
    bool initialized = false;
    
    void Start()
    {
        ownedSessionPtr = (z_owned_session_t *)Marshal.AllocHGlobal(sizeof(z_owned_session_t));
        ownedSubscriberPtr = (z_owned_subscriber_t *)Marshal.AllocHGlobal(sizeof(z_owned_subscriber_t));
        ownedPublisherPtr = (z_owned_publisher_t *)Marshal.AllocHGlobal(sizeof(z_owned_publisher_t));
        TestString();
        ZenohUtils.OpenSession(ownedSessionPtr);
        initialized = true;
        string keyExpr = "myhome/kitchen/temp";
        StartCoroutine(TestPublisher(keyExpr));
        StartCoroutine(TestSubscriber());
    }

    void OnDestroy()
    {
        if (initialized)
        {
            // 初期化していないものを終了するとクラッシュするので注意
            ZenohUtils.CloseSession(ownedSessionPtr);
        }
        
        if (ownedSessionPtr != null)
        {
            Marshal.FreeHGlobal((IntPtr)ownedSessionPtr);
        }
        
        if (ownedSubscriberPtr != null)
        {
            Marshal.FreeHGlobal((IntPtr)ownedSubscriberPtr);
        }
        
        if (ownedPublisherPtr != null)
        {
            Marshal.FreeHGlobal((IntPtr)ownedPublisherPtr);
        }
    }

    void TestString()
    {
        unsafe
        {
            // 基本的なテスト
            z_owned_string_t ownedStr = new z_owned_string_t();
            z_owned_string_t* ownedStrPtr = &ownedStr;
            //var ary = "hello unity".ToCharArray();            
            //fixed (char* pArray = &ary[0])
            {
                var result = ZenohNative.z_string_copy_from_str(ownedStrPtr, "hello unity");
                var loanedStringPtr = ZenohNative.z_string_loan(ownedStrPtr);
                var outStr = ZenohNative.z_string_data(loanedStringPtr);
                Debug.Log($"outStr: {Marshal.PtrToStringAnsi((IntPtr)outStr)}");
                ZenohNative.z_string_drop(ZenohUtils.z_move(ownedStrPtr));
            }
        }
    }
    
    //
    // Publisher
    //

    IEnumerator TestPublisher(string keyExpr)
    {
        if (!StartPublisher(keyExpr))
        {
            yield break;
        }
        for (int i = 0; i < 100; i++)
        {
            LoopPublisher(i, keyExpr);
            yield return new WaitForSeconds(0.1f);
        }
        ReleasePublisher();
    }

    bool StartPublisher(string keyExpr)
    {
        Debug.Log("Starting Zenoh publisher test...");
        z_owned_keyexpr_t ownedKeyExpr = new z_owned_keyexpr_t();

        try
        {
            // 1. セッションの借用
            z_loaned_session_t *loanedSession = ZenohNative.z_session_loan(ownedSessionPtr);

            // 2. KeyExprを宣言
            z_result_t result = ZenohNative.z_keyexpr_from_str(&ownedKeyExpr, keyExpr);
            if (result != z_result_t.Z_OK)
            {
                Debug.LogError("Unable to create key expression!");
                return false;
            }

            // 3. KeyExprを借用
            z_loaned_keyexpr_t *loanedKeyExprPtr = ZenohNative.z_keyexpr_loan(&ownedKeyExpr);

            // 4. Publisherのオプションを設定
            z_publisher_options_t pubOptions = new z_publisher_options_t();
            ZenohNative.z_publisher_options_default(&pubOptions);

            // 5. Publisherを宣言
            result = ZenohNative.z_declare_publisher(loanedSession,
                ownedPublisherPtr,
                loanedKeyExprPtr,
                &pubOptions);
            if (result != z_result_t.Z_OK)
            {
                Debug.LogError("Unable to declare Publisher for key expression!");
                ZenohNative.z_keyexpr_drop((z_moved_keyexpr_t *)&ownedKeyExpr);
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception in Publisher test: {ex.Message}\n{ex.StackTrace}");
        }

        Debug.Log($"Publisher declared on '{keyExpr}'");
        return true;
    }
    
    void LoopPublisher(int idx, string keyExpr)
    {
        try
        {
            z_loaned_publisher_t *loanedPublisher = ZenohNative.z_publisher_loan(ownedPublisherPtr);
            
            // メッセージ生成
            string message = $"[{idx:D4}] Unity Zenoh Message";
            Debug.Log($"Putting Data ('{keyExpr}': '{message}')...");

            // パブリッシュオプション設定
            z_publisher_put_options_t putOptions = new z_publisher_put_options_t();
            ZenohNative.z_publisher_put_options_default(&putOptions);

            // ペイロード作成
            z_owned_bytes_t payload = new z_owned_bytes_t();
            ZenohNative.z_bytes_copy_from_str(&payload, message);

            // オプショナル: エンコーディング設定
            z_owned_encoding_t encoding = new z_owned_encoding_t();
            // text/plain エンコーディングを取得
            z_loaned_encoding_t *textPlainEncodingPtr = ZenohNative.z_encoding_text_plain();
            ZenohNative.z_encoding_clone(&encoding, textPlainEncodingPtr);
            putOptions.encoding = (z_moved_encoding_t *)&encoding;
            //putOptions.SetMovedEncoding(&encoding);

            // データ送信
            z_result_t putResult = ZenohNative.z_publisher_put(loanedPublisher, (z_moved_bytes_t *)&payload, &putOptions);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception in Publisher test: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // 1秒待機
     //       yield return new WaitForSeconds(1.0f);
     //   }
        
     void ReleasePublisher()
     {
        // 8. リソース解放
        ZenohNative.z_publisher_drop((z_moved_publisher_t *)ownedPublisherPtr);
        
        Debug.Log("Publisher test completed");
    }
    
    //
    // Subscriber
    //
    
    // 実際のメッセージを処理するコールバック
    private static void HandleSample(z_loaned_sample_t *sample, void *context)
    {
        // サンプルからキー式を取得
        z_loaned_keyexpr_t *keyexprTmp = ZenohNative.z_sample_keyexpr(sample);
        Debug.Log($"received: keyexpr: {ZenohUtils.keyexprToStr(keyexprTmp)}");            
        
        // サンプルからデータを取得
        z_loaned_bytes_t *payload = ZenohNative.z_sample_payload(sample);
        z_owned_string_t payloadStr = new z_owned_string_t();
        ZenohNative.z_bytes_to_string(payload, &payloadStr);
        z_loaned_string_t *str  = ZenohNative.z_string_loan(&payloadStr);
        Debug.Log($"payload:: {ZenohUtils.stringToStr(str)}"); 
    }

    private static void HandleDrop(void *context)
    {
        
    }

    public IEnumerator TestSubscriber()
    {
        CreateSubscriber("myhome/kitchen/temp");
        yield return new WaitForSeconds(5.0f);
    }
    
    //[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void CallFunction(z_loaned_sample_t* sample, void* context)
    {
        // sample, context を用いた処理を記述
        Console.WriteLine("CallFunction invoked.");
    }

    // drop に対応するコールバック
    //[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void DropFunction(void* context)
    {
        // context を解放するなどの後処理を行う
        Console.WriteLine("DropFunction invoked.");
    }
    
    // サブスクライバーを作成する例
    public unsafe void CreateSubscriber(string keyExprStr)
    {
        z_loaned_session_t *loanedSession = ZenohNative.z_session_loan(ownedSessionPtr);
        // キー式を作成
        z_owned_keyexpr_t ownedKeyExpr = new z_owned_keyexpr_t();
        z_result_t result = ZenohNative.z_keyexpr_from_str(&ownedKeyExpr, keyExprStr);
        if (result != z_result_t.Z_OK)
        {
            throw new Exception("Failed to create key expression");
        }

        // キー式を借用
        z_loaned_keyexpr_t *loanedKeyExpr = ZenohNative.z_keyexpr_loan(&ownedKeyExpr);
        Debug.Log($"loanedKeyExpr: {ZenohUtils.keyexprToStr(loanedKeyExpr)}");

        // コールバックのクロージャを作成
        z_owned_closure_sample_t ownedClosure = new z_owned_closure_sample_t();
        ZenohNative.z_closure_sample(
            &ownedClosure,
            HandleSample,
            HandleDrop,
            null);

        Debug.Log("created callback closure");
        
        // サブスクライバーオプションを設定
        z_subscriber_options_t options = new z_subscriber_options_t();
        ZenohNative.z_subscriber_options_default(&options);
        
        // サブスクライバーを作成
        z_owned_subscriber_t ownedSubscriber = new z_owned_subscriber_t();
        result = ZenohNative.z_declare_subscriber(
            loanedSession,
            &ownedSubscriber,
            loanedKeyExpr,
            (z_moved_closure_sample_t *)&ownedClosure,
            &options);
        if (result != z_result_t.Z_OK)
        {
            throw new Exception("Failed to create subscriber");
        }
    }
}
