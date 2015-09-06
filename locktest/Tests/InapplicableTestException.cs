using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LockTest.Tests
{
    public class InapplicableTestException : Exception
    {
        public InapplicableTestException(string message) :
            base(message)
        {
        }

    }
}
