using System.Collections.Generic;

namespace NzbDrone.Core.Indexers.Definitions.Xthor
{
    public class XthorResponse
    {
        public XthorError Error { get; set; }
        public XthorUser User { get; set; }
        public List<XthorTorrent> Torrents { get; set; }
    }

    /// <summary>
    /// State of API
    /// </summary>
    public class XthorError
    {
        public int Code { get; set; }
        public string Descr { get; set; }
    }

    /// <summary>
    /// User Informations
    /// </summary>
    public class XthorUser
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public long Uploaded { get; set; }
        public long Downloaded { get; set; }
        public int Uclass { get; set; } // Class is a reserved keyword.
        public decimal Bonus_point { get; set; }
        public int Hits_and_run { get; set; }
        public string Avatar_url { get; set; }
    }

    /// <summary>
    /// Torrent Informations
    /// </summary>
    public class XthorTorrent
    {
        public int Id { get; set; }
        public int Category { get; set; }
        public int Seeders { get; set; }
        public int Leechers { get; set; }
        public string Name { get; set; }
        public int Times_completed { get; set; }
        public long Size { get; set; }
        public int Added { get; set; }
        public int Freeleech { get; set; }
        public int Numfiles { get; set; }
        public string Release_group { get; set; }
        public string Download_link { get; set; }
        public int Tmdb_id { get; set; }

        public override string ToString() => string.Format(
            "[XthorTorrent: id={0}, category={1}, seeders={2}, leechers={3}, name={4}, times_completed={5}, size={6}, added={7}, freeleech={8}, numfiles={9}, release_group={10}, download_link={11}, tmdb_id={12}]",
            Id,
            Category,
            Seeders,
            Leechers,
            Name,
            Times_completed,
            Size,
            Added,
            Freeleech,
            Numfiles,
            Release_group,
            Download_link,
            Tmdb_id);
    }
}
