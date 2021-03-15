using System;
using Microsoft.AspNetCore.Mvc;

namespace NzbDrone.Http.REST.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RestPostByIdAttribute : HttpPostAttribute
    {
    }
}
