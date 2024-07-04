namespace MagicPot.Backend.Services
{
    using TonLibDotNet;

    public class BlockchainReader(ILogger<BlockchainReader> logger, ITonClient tonClient)
    {
        public async Task<long> EnsureSynced(long lastKnownSeqno = 0)
        {
            await tonClient.InitIfNeeded();
            var blockId = await tonClient.Sync();
            logger.LogDebug("Synced to masterchain block {Seqno}.", blockId.Seqno);

            if (blockId.Seqno < lastKnownSeqno)
            {
                tonClient.Deinit();
                throw new SyncException(blockId.Seqno, lastKnownSeqno);
            }

            return blockId.Seqno;
        }
    }
}
