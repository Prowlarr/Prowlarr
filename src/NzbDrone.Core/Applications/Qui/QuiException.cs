using System;
using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.Applications.Qui
{
    public class QuiException : NzbDroneException
    {
        public QuiException(string message)
            : base(message)
        {
        }

        public QuiException(string message, params object[] args)
            : base(message, args)
        {
        }

        public QuiException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
