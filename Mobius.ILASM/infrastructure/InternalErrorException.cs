using System;
using System.Collections.Generic;
using System.Text;

namespace Mobius.ILasm.infrastructure
{
    public class InternalErrorException : Exception
    {
        public InternalErrorException()
                : base("Internal error")
        {
        }

        public InternalErrorException(string message)
                : base(message)
        {
        }
    }
}
