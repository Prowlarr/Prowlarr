namespace NzbDrone.Core.Indexers;

public class IndexerDownloadResponse
{
    public byte[] Data { get; private set; }
    public long ElapsedTime { get; private set; }

    public IndexerDownloadResponse(byte[] data, long elapsedTime = 0)
    {
        Data = data;
        ElapsedTime = elapsedTime;
    }
}
