using System;
using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.Applications.Mylar
{
    public class MylarException : NzbDroneException
    {
        public MylarException(string message)
            : base(message)
        {
        }

        public MylarException(string message, params object[] args)
            : base(message, args)
        {
        }

        public MylarException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
