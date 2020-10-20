using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace NzbDrone.Core.Indexers
{
    public class IndexerCategory
    {
        public int ID { get; set; }
        public string Name { get; set; }

        public List<IndexerCategory> SubCategories { get; private set; }

        public IndexerCategory() => SubCategories = new List<IndexerCategory>();

        public IndexerCategory(int id, string name)
        {
            ID = id;
            Name = name;
            SubCategories = new List<IndexerCategory>();
        }

        public bool Contains(IndexerCategory cat) =>
            Equals(this, cat) || SubCategories.Contains(cat);

        public JToken ToJson() =>
            new JObject
            {
                ["ID"] = ID,
                ["Name"] = Name
            };

        public override bool Equals(object obj) => (obj as IndexerCategory)?.ID == ID;

        // Get Hash code should be calculated off read only properties.
        // ID is not readonly
        public override int GetHashCode() => ID;
    }
}
