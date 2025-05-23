using UnityEngine;
using Zenoh; // Assuming this is the correct namespace for Zenoh wrapper
using System; // For System.Exception

public class PublisherOptionsTest : MonoBehaviour
{
    private Session session;
    private Publisher publisher;
    private KeyExpr keyExpr;
    private string keyExprString = "test/publisher_options_test"; // Unique key expression

    void Start()
    {
        Debug.Log("PublisherOptionsTest: Starting...");
        session = new Session();
        keyExpr = new KeyExpr(keyExprString);

        Debug.Log("PublisherOptionsTest: Opening session...");
        ZResult openResult = session.Open(null); // Assuming null is acceptable for default config
        if (!openResult.IsOk())
        {
            Debug.LogError($"PublisherOptionsTest: Failed to open session: {openResult}");
            enabled = false; // Disable script if session fails to open
            return;
        }
        Debug.Log("PublisherOptionsTest: Session opened successfully.");

        publisher = new Publisher();
        PublisherOptions pubOptions = new PublisherOptions
        {
            Priority = ZPriority.DataHigh, // Example priority
            CongestionControl = ZCongestionControl.Block // Example congestion control
        };
        Debug.Log($"PublisherOptionsTest: Declaring publisher with Priority={pubOptions.Priority}, CongestionControl={pubOptions.CongestionControl} on key '{keyExprString}'...");

        ZResult declareResult = publisher.Declare(session, keyExpr, pubOptions);
        if (!declareResult.IsOk())
        {
            Debug.LogError($"PublisherOptionsTest: Failed to declare publisher: {declareResult}");
            CleanUp(); // Clean up resources if declaration fails
            enabled = false; // Disable script
            return;
        }
        Debug.Log("PublisherOptionsTest: Publisher declared successfully.");

        try
        {
            string message = "Hello from PublisherOptionsTest with custom options!";
            // Assuming Publisher.Put(string) exists and handles encoding internally, or there's Bytes equivalent
            publisher.Put(message); 
            Debug.Log($"PublisherOptionsTest: Successfully published message: '{message}'");
        }
        catch (Exception e)
        {
            Debug.LogError($"PublisherOptionsTest: Failed to publish message: {e.Message}");
        }
    }

    void OnDestroy()
    {
        CleanUp();
    }

    void CleanUp()
    {
        Debug.Log("PublisherOptionsTest: Cleaning up resources...");
        
        // Dispose publisher first as it depends on the session
        if (publisher != null)
        {
            publisher.Dispose();
            publisher = null;
        }

        // Dispose KeyExpr (if it holds unmanaged resources or needs cleanup)
        if (keyExpr != null)
        {
            // Assuming KeyExpr might have a Dispose method or similar cleanup
            // If KeyExpr is purely managed and just holds a string, this might not be strictly necessary
            // but good practice if it could evolve.
            // keyExpr.Dispose(); // Let's assume KeyExpr doesn't have Dispose for now unless specified elsewhere.
            keyExpr = null; // Set to null for garbage collection
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
        Debug.Log("PublisherOptionsTest: Cleanup complete.");
    }
}
