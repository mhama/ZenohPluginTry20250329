using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using Zenoh;

public class ZenohCameraReceiverTest2 : MonoBehaviour
{
    private ZenohSession session;
    private ZenohSubscriber subscriber;
    private ZenohKeyExpr keyExpr;
    private bool initialized = false;
    
    private static byte[] managedBuffer;
    private static object obj = new object();
    private static Texture2D texture;
    private static SynchronizationContext syncContext;

    [SerializeField]
    private TextAsset zenohConfigText;
    
    // Reference to the renderer
    [SerializeField] 
    private Renderer targetRenderer;
    
    // Flag indicating if the texture has been updated
    private static bool textureUpdated = false;
    
    void Start()
    {
        syncContext = SynchronizationContext.Current;
        // Initialize the lock object
        if (obj == null) obj = new object();
        
        // If no target renderer is set, use this object's renderer
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
            
        // Create and open Zenoh session
        session = new ZenohSession();
        string conf = zenohConfigText == null ? null : zenohConfigText?.text;
        var result = session.Open(conf);

        if (!result.IsOk)
        {
            Debug.LogError("Failed to open session");
            return;
        }
        
        // Create subscriber
        StartCoroutine(TestSubscriber());

        texture = new Texture2D(1, 1);
    }

    void Update()
    {
        // Only update the material if the texture has been updated
        if (textureUpdated && texture != null && targetRenderer != null)
        {
            targetRenderer.material.mainTexture = texture;
            textureUpdated = false;
            Debug.Log("Material texture updated");
        }
    }

    void OnDestroy()
    {
        if (initialized)
        {
            // Close the session
            session.Close();
            initialized = false;
        }
        
        // Release resources
        if (keyExpr != null)
        {
            keyExpr.Dispose();
            keyExpr = null;
        }
        
        if (subscriber != null)
        {
            subscriber.Dispose();
            subscriber = null;
        }
        
        if (session != null)
        {
            session.Dispose();
            session = null;
        }
    }
     
    // Callback for when a sample is received
    private void OnSampleReceived(ZenohSampleRef sample)
    {
        // Get data from the sample
        byte[] data = sample.GetPayload();
        string keyExpr = sample.GetKeyExpr();
        
        Debug.Log($"received: keyexpr: {keyExpr}");
        
        // Update the managed buffer inside a lock
        lock(obj)
        {
            if (managedBuffer == null || managedBuffer.Length < data.Length)
            {
                managedBuffer = new byte[data.Length];
            }
            Array.Copy(data, managedBuffer, data.Length);
        }

        // Execute on the main thread using SynchronizationContext
        try
        {
            // Delegate to the main thread
            syncContext.Post(_ => {
                try
                {
                    // Copy JPEG data to handle it thread-safely within the callback
                    byte[] textureCopy;
                    lock(obj)
                    {
                        textureCopy = new byte[managedBuffer.Length];
                        Array.Copy(managedBuffer, textureCopy, managedBuffer.Length);
                    }
                    
                    // Load JPEG image data into the texture
                    if (texture != null && textureCopy != null && textureCopy.Length > 0)
                    {
                        texture.LoadImage(textureCopy);
                        textureUpdated = true; // Set the texture update flag
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
            
            // Fallback if SynchronizationContext is not available
            // (In this case, we expect to check the textureUpdated flag in Update)
            lock(obj)
            {
                textureUpdated = true;
            }
        }
    }

    private IEnumerator TestSubscriber()
    {
        CreateSubscriber("rpi/camera/image_jpeg");
        initialized = true;
        yield return new WaitForSeconds(5.0f);
    }
    
    // Example of creating a subscriber
    private void CreateSubscriber(string keyExprStr)
    {
        // Create key expression
        keyExpr = new ZenohKeyExpr(keyExprStr);
        
        // Create subscriber
        subscriber = new ZenohSubscriber();
        
        // Register subscriber with callback
        subscriber.CreateSubscriber(session, keyExpr, OnSampleReceived);
        
        Debug.Log($"Subscriber created for key expression: {keyExprStr}");
    }
}
