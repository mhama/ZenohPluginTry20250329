using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using Zenoh.Plugins;

public unsafe class ZenohCameraReceiverTest : MonoBehaviour
{
    z_owned_session_t *ownedSessionPtr;// = new z_owned_session_t();
    z_owned_subscriber_t *ownedSubscriberPtr;
    z_owned_publisher_t *ownedPublisherPtr;// = new z_owned_publisher_t();
    bool initialized = false;
    
    static byte[] managedBuffer;
    private static object obj = new object(); // objの初期化を忘れずに
    static Texture2D texture;
    static SynchronizationContext syncContext;
    
    // レンダラーへの参照を追加
    [SerializeField] 
    private Renderer targetRenderer;
    
    // テクスチャが更新されたかを示すフラグ
    private static bool textureUpdated = false;
    
    void Start()
    {
        ownedSessionPtr = (z_owned_session_t *)Marshal.AllocHGlobal(sizeof(z_owned_session_t));
        ownedSubscriberPtr = (z_owned_subscriber_t *)Marshal.AllocHGlobal(sizeof(z_owned_subscriber_t));
        ownedPublisherPtr = (z_owned_publisher_t *)Marshal.AllocHGlobal(sizeof(z_owned_publisher_t));

        syncContext = SynchronizationContext.Current;
        // objの初期化を行う
        if (obj == null) obj = new object();
        
        // 対象のレンダラーが設定されていない場合は自身のレンダラーを使用
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
            
        ZenohUtils.OpenSession(ownedSessionPtr);
        StartCoroutine(TestSubscriber());
        texture = new Texture2D(1, 1);
    }

    void Update()
    {
        // テクスチャが更新された場合にのみマテリアルを更新
        if (textureUpdated && texture != null && targetRenderer != null)
        {
            // メイン（共有）マテリアルを更新する場合
            targetRenderer.material.mainTexture = texture;
            
            // または特定のプロパティに設定する場合
            // targetRenderer.material.SetTexture("_MainTex", texture);
            
            textureUpdated = false;
            Debug.Log("Material texture updated");
        }
    }

    void OnDestroy()
    {
        if (initialized)
        {
            // 初期化していないものを終了するとクラッシュするので注意
            ZenohUtils.CloseSession(ownedSessionPtr);
            initialized = false;
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
        z_owned_slice_t slice = new z_owned_slice_t();
        ZenohNative.z_bytes_to_slice(payload, &slice);
        var loanedSlice = ZenohNative.z_slice_loan(&slice);
        byte *buf = ZenohNative.z_slice_data(loanedSlice);
        long len = (long)ZenohNative.z_slice_len(loanedSlice);
        
        // managedBufferの更新はロック内で行う
        lock(obj)
        {
            if (managedBuffer == null || managedBuffer.Length < len)
            {
                managedBuffer = new byte[len];
            }
            Marshal.Copy((IntPtr) buf, managedBuffer, 0, (int)len);
        }

        ZenohNative.z_slice_drop((z_moved_slice_t *)&slice);
        
        // SynchronizationContextを使用してメインスレッドで実行
        try
        {
            // メインスレッドに委譲
            syncContext.Post(_ => {
                try
                {
                    // JPEGデータをコピーして、コールバック内でスレッドセーフに扱う
                    byte[] textureCopy;
                    lock(obj)
                    {
                        textureCopy = new byte[managedBuffer.Length];
                        Array.Copy(managedBuffer, textureCopy, managedBuffer.Length);
                    }
                    
                    // JPEG画像データをテクスチャに読み込む
                    if (texture != null && textureCopy != null && textureCopy.Length > 0)
                    {
                        texture.LoadImage(textureCopy);
                        textureUpdated = true; // テクスチャ更新フラグをセット
                        Debug.Log($"Texture updated: {texture.width}x{texture.height}");
                    }
                    else
                    {
                        Debug.LogWarning("Failed to update texture: texture or buffer is null/empty");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error updating texture: {e.Message}\n{e.StackTrace}");
                }
            }, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to post to main thread: {ex.Message}\n{ex.StackTrace}");
            
            // SynchronizationContextが利用できない場合のフォールバック
            // (この場合はUpdate内でtextureUpdatedフラグをチェックして処理する想定)
            lock(obj)
            {
                textureUpdated = true;
            }
        }

        /*
        // サンプルの種類を取得 (PUT/DELETE)
        ZSampleKindT kind = ZenohNative.z_sample_kind(sample);
        */
    }

    private static void HandleDrop(void *context)
    {
        
    }

    private IEnumerator TestSubscriber()
    {
        CreateSubscriber("rpi/camera/image_jpeg");
        initialized = true;
        yield return new WaitForSeconds(5.0f);
    }
    
    // サブスクライバーを作成する例
    private unsafe void CreateSubscriber(string keyExprStr)
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
