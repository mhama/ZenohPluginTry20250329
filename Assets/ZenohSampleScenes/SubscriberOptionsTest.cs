using UnityEngine;
using Zenoh; // Assuming this is the correct namespace for Zenoh wrapper
using System.Text; // For Encoding

public class SubscriberOptionsTest : MonoBehaviour
{
    private Session session;
    private Subscriber subscriber;
    private KeyExpr keyExpr;
    // Use the same key as PublisherOptionsTest if you want them to communicate
    private string keyExprString = "test/publisher_options_test"; 

    void Start()
    {
        Debug.Log("SubscriberOptionsTest: Starting...");
        session = new Session();
        // Match the key expression used by a publisher you want to receive from
        keyExpr = new KeyExpr(keyExprString); 

        Debug.Log("SubscriberOptionsTest: Opening session...");
        ZResult openResult = session.Open(null); // Use default config
        if (!openResult.IsOk())
        {
            Debug.LogError($"SubscriberOptionsTest: Failed to open session: {openResult}");
            enabled = false; // Disable script if session fails to open
            return;
        }
        Debug.Log("SubscriberOptionsTest: Session opened successfully.");

        subscriber = new Subscriber();
        SubscriberOptions subOptions = new SubscriberOptions
        {
            Reliability = ZReliability.Reliable 
            // Target option is harder to verify in simple test, so focusing on Reliability
        };
        Debug.Log($"SubscriberOptionsTest: Creating subscriber with Reliability={subOptions.Reliability} on key '{keyExprString}'...");
        
        // Pass the callback and options to CreateSubscriber
        ZResult createResult = subscriber.CreateSubscriber(session, keyExpr, HandleSampleReceived, subOptions);
        if (!createResult.IsOk())
        {
            Debug.LogError($"SubscriberOptionsTest: Failed to create subscriber: {createResult}");
            CleanUp(); // Clean up resources if creation fails
            enabled = false; // Disable script
            return;
        }
        Debug.Log($"SubscriberOptionsTest: Subscriber created successfully for key '{keyExprString}'. Listening with Reliability={subOptions.Reliability}.");
    }

    void HandleSampleReceived(SampleRef sample)
    {
        try
        {
            // Use ToByteArray() as per BytesRef implementation
            byte[] payloadBytes = sample.GetPayload().ToByteArray(); 
            string payloadString = Encoding.UTF8.GetString(payloadBytes);
            
            string receivedKeyExpr = "N/A";
            try 
            {
                // Assuming GetKeyExprRef() returns a KeyExprRef which has ToString() or similar
                // This part is for logging and might need adjustment based on KeyExprRef actual API
                receivedKeyExpr = sample.GetKeyExprRef()?.ToString() ?? "null KeyExprRef";
            }
            catch (System.Exception keyEx)
            {
                 Debug.LogWarning($"SubscriberOptionsTest: Could not get KeyExpr from sample: {keyEx.Message}");
            }

            Debug.Log($"SubscriberOptionsTest: Received sample on key '{receivedKeyExpr}' with payload: '{payloadString}'");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SubscriberOptionsTest: Error processing sample: {e.Message}");
        }
    }

    void OnDestroy()
    {
        CleanUp();
    }

    void CleanUp()
    {
        Debug.Log("SubscriberOptionsTest: Cleaning up resources...");
        
        // Dispose subscriber first
        if (subscriber != null)
        {
            subscriber.Dispose();
            subscriber = null;
        }

        // Dispose KeyExpr (if it holds unmanaged resources or needs cleanup)
        if (keyExpr != null)
        {
            // Assuming KeyExpr does not have a Dispose method for now.
            // keyExpr.Dispose(); 
            keyExpr = null;
        }

        // Close and dispose session last
        if (session != null)
        {
            if (session.IsOpen()) // Check if session is open before trying to close
            {
                session.Close();
            }
            session.Dispose();
            session = null;
        }
        Debug.Log("SubscriberOptionsTest: Cleanup complete.");
    }
}
