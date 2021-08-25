using System;
using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.Applications.LazyLibrarian
{
    public class LazyLibrarianException : NzbDroneException
    {
        public LazyLibrarianException(string message)
            : base(message)
        {
        }

        public LazyLibrarianException(string message, params object[] args)
            : base(message, args)
        {
        }

        public LazyLibrarianException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
