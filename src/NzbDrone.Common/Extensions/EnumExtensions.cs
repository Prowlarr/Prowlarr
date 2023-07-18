using System;
using System.Linq;

namespace NzbDrone.Common.Extensions
{
    public static class EnumExtensions
    {
        public static T GetAttribute<T>(this Enum value)
            where T : Attribute
        {
            var enumType = value.GetType();
            var name = Enum.GetName(enumType, value);

            return name == null ? null : enumType.GetField(name)?.GetCustomAttributes(false).OfType<T>().SingleOrDefault();
        }
    }
}
