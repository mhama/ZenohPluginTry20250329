using System;
using System.Collections;
using UnityEngine;
using ZenohPackage.Plugins;

public class ZenohSimplePubSubTest2 : MonoBehaviour
{
    private ZenohSession session;
    private ZenohSubscriber subscriber;
    private ZenohPublisher publisher;
    private bool initialized = false;
    
    [SerializeField]
    private TextAsset zenohConfigText;

    void Start()
    {
        // Initialize Zenoh session
        session = new ZenohSession();
        var conf = zenohConfigText?.text;
        session.Open(conf);
        initialized = true;
        
        string keyExpr = "myhome/kitchen/temp";
        StartCoroutine(TestPublisher(keyExpr));
        StartCoroutine(TestSubscriber());
    }

    void OnDestroy()
    {
        if (initialized)
        {
            // Clean up resources
            if (publisher != null)
            {
                publisher.Dispose();
                publisher = null;
            }
            
            if (subscriber != null)
            {
                subscriber.Dispose();
                subscriber = null;
            }
            
            if (session != null)
            {
                session.Close();
                session.Dispose();
                session = null;
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
            PublishMessage(i, keyExpr);
            yield return new WaitForSeconds(0.1f);
        }
    }

    bool StartPublisher(string keyExpr)
    {
        Debug.Log("Starting Zenoh publisher test...");
        
        try
        {
            // Create key expression using the wrapper
            using (ZenohKeyExpr keyExpression = new ZenohKeyExpr(keyExpr))
            {
                // Create publisher
                publisher = new ZenohPublisher();
                publisher.Declare(session, keyExpression);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception in Publisher test: {ex.Message}\n{ex.StackTrace}");
            return false;
        }

        Debug.Log($"Publisher declared on '{keyExpr}'");
        return true;
    }
    
    void PublishMessage(int idx, string keyExpr)
    {
        try
        {
            string message = $"[{idx:D4}] Unity Zenoh Message";
            Debug.Log($"Putting Data ('{keyExpr}': '{message}')...");
            
            // Create put options with text/plain encoding
            using (ZenohEncoding textPlainEncoding = ZenohEncoding.TextPlain())
            using (ZenohPublisherPutOptions options = new ZenohPublisherPutOptions())
            {
                options.SetMovedEncoding(textPlainEncoding);
                
                // Publish the message
                publisher.Put(message, options);
            }
            
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception in Publisher test: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    //
    // Subscriber
    //
    
    // Callback for handling received samples
    private void HandleSample(ZenohSampleRef sample)
    {
        string keyExpr = sample.GetKeyExpr();
        byte[] payload = sample.GetPayload();
        string payloadStr = System.Text.Encoding.UTF8.GetString(payload);
        
        Debug.Log($"Received: keyexpr: {keyExpr}");
        Debug.Log($"Payload: {payloadStr}");
    }
    
    public IEnumerator TestSubscriber()
    {
        CreateSubscriber("myhome/kitchen/temp");
        yield return new WaitForSeconds(5.0f);
    }
    
    public void CreateSubscriber(string keyExprStr)
    {
        try
        {
            // Create key expression using the wrapper
            using (ZenohKeyExpr keyExpression = new ZenohKeyExpr(keyExprStr))
            {
                // Create subscriber with callback
                subscriber = new ZenohSubscriber();
                subscriber.CreateSubscriber(session, keyExpression, HandleSample);
                
                Debug.Log($"Subscriber created for '{keyExprStr}'");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception creating subscriber: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
