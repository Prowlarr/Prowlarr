using System;

namespace NzbDrone.Core.Indexers
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class SearchConstraintAttribute : Attribute
    {
        public SearchConstraintAttribute(SearchConstraintResetBehavior resetBehavior = SearchConstraintResetBehavior.Default)
        {
            ResetBehavior = resetBehavior;
        }

        public SearchConstraintResetBehavior ResetBehavior { get; }
    }

    public enum SearchConstraintResetBehavior
    {
        Default,
        False,
        Empty,
        Null
    }
}
