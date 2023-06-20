using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NzbDrone.Core.Indexers
{
    public class IndexerCapabilitiesCategories
    {
        private readonly List<CategoryMapping> _categoryMapping = new List<CategoryMapping>();
        private readonly List<IndexerCategory> _torznabCategoryTree = new List<IndexerCategory>();

        public List<string> GetTrackerCategories() => _categoryMapping
            .Where(m => m.NewzNabCategory < 100000)
            .Select(m => m.TrackerCategory).Distinct().ToList();

        public List<IndexerCategory> GetTorznabCategoryTree(bool sorted = false)
        {
            if (!sorted)
            {
                return _torznabCategoryTree;
            }

            // we build a new tree, original is unsorted
            // first torznab categories ordered by id and then custom cats ordered by name
            var sortedTree = _torznabCategoryTree
                .Select(c =>
                {
                    var sortedSubCats = c.SubCategories.OrderBy(x => x.Id);
                    var newCat = new IndexerCategory(c.Id, c.Name);
                    newCat.SubCategories.AddRange(sortedSubCats);
                    return newCat;
                }).OrderBy(x => x.Id >= 100000 ? "zzz" + x.Name : x.Id.ToString()).ToList();

            return sortedTree;
        }

        public List<IndexerCategory> GetTorznabCategoryList(bool sorted = false)
        {
            var tree = GetTorznabCategoryTree(sorted);

            // create a flat list (without subcategories)
            var newFlatList = new List<IndexerCategory>();
            foreach (var cat in tree)
            {
                newFlatList.Add(cat.CopyWithoutSubCategories());
                newFlatList.AddRange(cat.SubCategories);
            }

            return newFlatList;
        }

        public void AddCategoryMapping(int trackerCategory, IndexerCategory newznabCategory, string trackerCategoryDesc = null) =>
            AddCategoryMapping(trackerCategory.ToString(), newznabCategory, trackerCategoryDesc);

        public void AddCategoryMapping(string trackerCategory, IndexerCategory torznabCategory, string trackerCategoryDesc = null)
        {
            _categoryMapping.Add(new CategoryMapping(trackerCategory, trackerCategoryDesc, torznabCategory.Id));
            AddTorznabCategoryTree(torznabCategory);

            if (trackerCategoryDesc == null)
            {
                return;
            }

            // create custom cats (1:1 categories) if trackerCategoryDesc is defined
            // - if trackerCategory is "integer" we use that number to generate custom category id
            // - if trackerCategory is "string" we compute a hash to generate fixed integer id for the custom category
            //   the hash is not perfect but it should work in most cases. we can't use sequential numbers because
            //   categories are updated frequently and the id must be fixed to work in 3rd party apps
            if (!int.TryParse(trackerCategory, out var trackerCategoryInt))
            {
                var hashed = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(trackerCategory));
                trackerCategoryInt = BitConverter.ToUInt16(hashed, 0); // id between 0 and 65535 < 100000
            }

            var customCat = new IndexerCategory(trackerCategoryInt + 100000, trackerCategoryDesc);
            _categoryMapping.Add(new CategoryMapping(trackerCategory, trackerCategoryDesc, customCat.Id));
            AddTorznabCategoryTree(customCat);
        }

        public List<string> MapTorznabCapsToTrackers(int[] queryCategories, bool mapChildrenCatsToParent = false)
        {
            var expandedQueryCats = ExpandTorznabQueryCategories(queryCategories, mapChildrenCatsToParent);
            var result = _categoryMapping
                .Where(c => expandedQueryCats.Contains(c.NewzNabCategory))
                .Select(mapping => mapping.TrackerCategory).Distinct().ToList();
            return result;
        }

        public ICollection<IndexerCategory> MapTrackerCatToNewznab(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Array.Empty<IndexerCategory>();
            }

            var cats = _categoryMapping
                       .Where(m =>
                           !string.IsNullOrWhiteSpace(m.TrackerCategory) &&
                           string.Equals(m.TrackerCategory, input, StringComparison.InvariantCultureIgnoreCase))
                       .Select(c => NewznabStandardCategory.AllCats.FirstOrDefault(n => n.Id == c.NewzNabCategory) ?? new IndexerCategory { Id = c.NewzNabCategory })
                       .ToList();
            return cats;
        }

        public ICollection<IndexerCategory> MapTrackerCatDescToNewznab(string trackerCategoryDesc)
        {
            if (string.IsNullOrWhiteSpace(trackerCategoryDesc))
            {
                return Array.Empty<IndexerCategory>();
            }

            var cats = _categoryMapping
                .Where(m =>
                    !string.IsNullOrWhiteSpace(m.TrackerCategoryDesc) &&
                    string.Equals(m.TrackerCategoryDesc, trackerCategoryDesc, StringComparison.InvariantCultureIgnoreCase))
                .Select(c => NewznabStandardCategory.AllCats.FirstOrDefault(n => n.Id == c.NewzNabCategory) ?? new IndexerCategory { Id = c.NewzNabCategory })
                .ToList();
            return cats;
        }

        public int[] SupportedCategories(int[] categories)
        {
            if (categories == null || categories.Length == 0)
            {
                return Array.Empty<int>();
            }

            var subCategories = _torznabCategoryTree.SelectMany(c => c.SubCategories);
            var allCategories = _torznabCategoryTree.Concat(subCategories);
            return allCategories.Where(c => categories.Contains(c.Id)).Select(c => c.Id).ToArray();
        }

        public void Concat(IndexerCapabilitiesCategories rhs)
        {
            // exclude indexer specific categories (>= 100000)
            // we don't concat _categoryMapping because it makes no sense for the aggregate indexer
            rhs.GetTorznabCategoryList().Where(x => x.Id < 100000).ToList().ForEach(AddTorznabCategoryTree);
        }

        public List<int> ExpandTorznabQueryCategories(int[] queryCategories, bool mapChildrenCatsToParent = false)
        {
            var expandedQueryCats = new List<int>();

            if (queryCategories == null)
            {
                return expandedQueryCats;
            }

            foreach (var queryCategory in queryCategories)
            {
                expandedQueryCats.Add(queryCategory);
                if (queryCategory >= 100000)
                {
                    continue;
                }

                var parentCat = _torznabCategoryTree.FirstOrDefault(c => c.Id == queryCategory);
                if (parentCat != null)
                {
                    // if it's parent cat we add all the children
                    expandedQueryCats.AddRange(parentCat.SubCategories.Select(c => c.Id));
                }
                else if (mapChildrenCatsToParent)
                {
                    // if it's child cat and mapChildrenCatsToParent is enabled we add the parent
                    var queryCategoryTorznab = new IndexerCategory(queryCategory, "");
                    parentCat = _torznabCategoryTree.FirstOrDefault(c => c.Contains(queryCategoryTorznab));
                    if (parentCat != null)
                    {
                        expandedQueryCats.Add(parentCat.Id);
                    }
                }
            }

            return expandedQueryCats.Distinct().ToList();
        }

        private void AddTorznabCategoryTree(IndexerCategory torznabCategory)
        {
            // build the category tree
            if (NewznabStandardCategory.ParentCats.Contains(torznabCategory))
            {
                // parent cat
                if (!_torznabCategoryTree.Contains(torznabCategory))
                {
                    _torznabCategoryTree.Add(torznabCategory.CopyWithoutSubCategories());
                }
            }
            else
            {
                // child or custom cat
                var parentCat = NewznabStandardCategory.ParentCats.FirstOrDefault(c => c.Contains(torznabCategory));
                if (parentCat != null)
                {
                    // child cat
                    var nodeCat = _torznabCategoryTree.FirstOrDefault(c => c.Equals(parentCat));
                    if (nodeCat != null)
                    {
                        // parent cat already exists
                        if (!nodeCat.Contains(torznabCategory))
                        {
                            nodeCat.SubCategories.Add(torznabCategory);
                        }
                    }
                    else
                    {
                        // create parent cat and add child
                        nodeCat = parentCat.CopyWithoutSubCategories();
                        nodeCat.SubCategories.Add(torznabCategory);
                        _torznabCategoryTree.Add(nodeCat);
                    }
                }
                else
                {
                    // custom cat
                    if (torznabCategory.Id > 1000 && torznabCategory.Id < 10000)
                    {
                        var potentialParent = NewznabStandardCategory.ParentCats.FirstOrDefault(c => (c.Id / 1000) == (torznabCategory.Id / 1000));
                        if (potentialParent != null)
                        {
                            var nodeCat = _torznabCategoryTree.FirstOrDefault(c => c.Equals(potentialParent));
                            if (nodeCat != null)
                            {
                                // parent cat already exists
                                if (!nodeCat.Contains(torznabCategory))
                                {
                                    nodeCat.SubCategories.Add(torznabCategory);
                                }
                            }
                            else
                            {
                                // create parent cat and add child
                                nodeCat = potentialParent.CopyWithoutSubCategories();
                                nodeCat.SubCategories.Add(torznabCategory);
                                _torznabCategoryTree.Add(nodeCat);
                            }
                        }
                        else
                        {
                            _torznabCategoryTree.Add(torznabCategory);
                        }
                    }
                    else
                    {
                        _torznabCategoryTree.Add(torznabCategory);
                    }
                }
            }
        }
    }
}
