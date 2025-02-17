using System;

namespace NzbDrone.Core.Indexers
{
    public class IndexerFlag : IEquatable<IndexerFlag>
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public IndexerFlag()
        {
        }

        public IndexerFlag(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public bool Equals(IndexerFlag other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Name.Equals(other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as IndexerFlag);
        }

        public static bool operator ==(IndexerFlag left, IndexerFlag right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(IndexerFlag left, IndexerFlag right)
        {
            return !Equals(left, right);
        }

        public static IndexerFlag Internal => new ("internal", "Uploader is an internal release group");
        public static IndexerFlag Exclusive => new ("exclusive", "An exclusive release that must not be uploaded anywhere else");
        public static IndexerFlag FreeLeech => new ("freeleech", "Download doesn't count toward ratio");
        public static IndexerFlag NeutralLeech => new ("neutralleech", "Download and upload doesn't count toward ratio");
        public static IndexerFlag HalfLeech => new ("halfleech", "Release counts 50% to ratio");
        public static IndexerFlag Scene => new ("scene", "Uploader follows scene rules");
        public static IndexerFlag DoubleUpload => new ("doubleupload", "Seeding counts double for release");
    }
}
