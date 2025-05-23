using Zenoh.Plugins; // Assuming ZenohNative and native types are here

namespace Zenoh
{
    public enum ZReliability
    {
        BestEffort = 0, // Assuming Z_RELIABILITY_BEST_EFFORT = 0
        Reliable = 1    // Assuming Z_RELIABILITY_RELIABLE = 1
    }

    public enum ZTarget
    {
        None = 0,       // Assuming Z_TARGET_NONE = 0
        Local = 1,      // Assuming Z_TARGET_LOCAL = 1
        Remote = 2,     // Assuming Z_TARGET_REMOTE = 2
        LocalAndRemote = 3 // Assuming Z_TARGET_LOCAL_AND_REMOTE = 3. This might also be `All`.
    }

    public class SubscriberOptions
    {
        public SubscriberOptions()
        {
            // Default constructor
        }

        public ZReliability? Reliability { get; set; } = null;
        public ZTarget? Target { get; set; } = null;

        internal unsafe void ApplyTo(z_subscriber_options_t* options)
        {
            ZenohNative.z_subscriber_options_default(options);
            if (Reliability.HasValue)
            {
                options->reliability = (z_reliability_t)Reliability.Value;
            }
            if (Target.HasValue)
            {
                // Assuming z_subscriber_options_t has a 'target' field
                // The native field might be called something else like 'scope' or 'selection'.
                // For now, let's assume it's 'target'.
                options->target = (z_target_t)Target.Value;
            }
        }
    }
}
