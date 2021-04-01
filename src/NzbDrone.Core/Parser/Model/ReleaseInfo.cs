using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Parser.Model
{
    public class ReleaseInfo
    {
        public string Guid { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public long? Size { get; set; }
        public string DownloadUrl { get; set; }
        public string InfoUrl { get; set; }
        public string CommentUrl { get; set; }
        public int IndexerId { get; set; }
        public string Indexer { get; set; }
        public int IndexerPriority { get; set; }
        public DownloadProtocol DownloadProtocol { get; set; }
        public int? Grabs { get; set; }
        public int? Files { get; set; }
        public int TvdbId { get; set; }
        public int TvRageId { get; set; }
        public int ImdbId { get; set; }
        public int TmdbId { get; set; }
        public DateTime PublishDate { get; set; }

        public string Origin { get; set; }
        public string Source { get; set; }
        public string Container { get; set; }
        public string Codec { get; set; }
        public string Resolution { get; set; }
        public ICollection<IndexerCategory> Category { get; set; }

        public IndexerFlags IndexerFlags { get; set; }

        public int Age
        {
            get { return DateTime.UtcNow.Subtract(PublishDate).Days; }

            //This prevents manually downloading a release from blowing up in mono
            //TODO: Is there a better way?
            private set { }
        }

        public double AgeHours
        {
            get { return DateTime.UtcNow.Subtract(PublishDate).TotalHours; }

            //This prevents manually downloading a release from blowing up in mono
            //TODO: Is there a better way?
            private set { }
        }

        public double AgeMinutes
        {
            get { return DateTime.UtcNow.Subtract(PublishDate).TotalMinutes; }

            //This prevents manually downloading a release from blowing up in mono
            //TODO: Is there a better way?
            private set { }
        }

        public override string ToString()
        {
            return string.Format("[{0}] {1} [{2}]", PublishDate, Title, Size);
        }

        public virtual string ToString(string format)
        {
            switch (format.ToUpperInvariant())
            {
                case "L": // Long format
                    var stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine("Guid: " + Guid ?? "Empty");
                    stringBuilder.AppendLine("Title: " + Title ?? "Empty");
                    stringBuilder.AppendLine("Size: " + Size ?? "Empty");
                    stringBuilder.AppendLine("InfoUrl: " + InfoUrl ?? "Empty");
                    stringBuilder.AppendLine("DownloadUrl: " + DownloadUrl ?? "Empty");
                    stringBuilder.AppendLine("Indexer: " + Indexer ?? "Empty");
                    stringBuilder.AppendLine("CommentUrl: " + CommentUrl ?? "Empty");
                    stringBuilder.AppendLine("DownloadProtocol: " + DownloadProtocol ?? "Empty");
                    stringBuilder.AppendLine("TvdbId: " + TvdbId ?? "Empty");
                    stringBuilder.AppendLine("TvRageId: " + TvRageId ?? "Empty");
                    stringBuilder.AppendLine("ImdbId: " + ImdbId ?? "Empty");
                    stringBuilder.AppendLine("PublishDate: " + PublishDate ?? "Empty");
                    return stringBuilder.ToString();
                default:
                    return ToString();
            }
        }

        public static long GetBytes(string str)
        {
            var valStr = new string(str.Where(c => char.IsDigit(c) || c == '.').ToArray());
            var unit = new string(str.Where(char.IsLetter).ToArray());
            var val = ParseUtil.CoerceFloat(valStr);
            return GetBytes(unit, val);
        }

        public static long GetBytes(string unit, float value)
        {
            unit = unit.Replace("i", "").ToLowerInvariant();
            if (unit.Contains("kb"))
            {
                return BytesFromKB(value);
            }

            if (unit.Contains("mb"))
            {
                return BytesFromMB(value);
            }

            if (unit.Contains("gb"))
            {
                return BytesFromGB(value);
            }

            if (unit.Contains("tb"))
            {
                return BytesFromTB(value);
            }

            return (long)value;
        }

        public static long BytesFromTB(float tb) => BytesFromGB(tb * 1024f);

        public static long BytesFromGB(float gb) => BytesFromMB(gb * 1024f);

        public static long BytesFromMB(float mb) => BytesFromKB(mb * 1024f);

        public static long BytesFromKB(float kb) => (long)(kb * 1024f);
    }

    [Flags]
    public enum IndexerFlags
    {
        G_Freeleech = 1, //General
        G_Halfleech = 2, //General, only 1/2 of download counted
        G_DoubleUpload = 4, //General
        PTP_Golden = 8, //PTP
        PTP_Approved = 16, //PTP
        HDB_Internal = 32, //HDBits, internal
        AHD_Internal = 64, // AHD, internal
        G_Scene = 128, //General, the torrent comes from the "scene"
        G_Freeleech75 = 256, //Currently only used for AHD, signifies a torrent counts towards 75 percent of your download quota.
        G_Freeleech25 = 512, //Currently only used for AHD, signifies a torrent counts towards 25 percent of your download quota.
        AHD_UserRelease = 1024 // AHD, internal
    }
}
