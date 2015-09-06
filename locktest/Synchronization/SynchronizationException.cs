using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LockTest.Synchronization
{
    public class SynchronizationException : Exception
    {
        public SynchronizationException()
        {           
        }

        public  SynchronizationException(string message) : 
            base(message)
        {

        }
    }
}
