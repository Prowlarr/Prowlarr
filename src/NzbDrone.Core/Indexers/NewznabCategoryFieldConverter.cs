using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.Indexers
{
    public class NewznabCategoryFieldConverter : ISelectOptionsConverter
    {
        public List<SelectOption> GetSelectOptions()
        {
            var result = new List<SelectOption>();

            foreach (var category in NewznabStandardCategory.ParentCats)
            {
                result.Add(new SelectOption
                {
                    Value = category.Id,
                    Name = category.Name,
                    Hint = $"({category.Id})"
                });

                if (category.SubCategories != null)
                {
                    foreach (var subcat in category.SubCategories.OrderBy(cat => cat.Id))
                    {
                        result.Add(new SelectOption
                        {
                            Value = subcat.Id,
                            Name = subcat.Name,
                            Hint = $"({subcat.Id})",
                            ParentValue = category.Id
                        });
                    }
                }
            }

            return result;
        }
    }
}
