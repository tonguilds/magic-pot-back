namespace MagicPot.Backend.Services
{
    public class SyncException(long syncSeqno, long lastKnownSeqno)
        : Exception($"Sync failed: seqno {syncSeqno} is less than last known {lastKnownSeqno}.")
    {
        // Nothing
    }
}
