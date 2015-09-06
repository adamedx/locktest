using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;

namespace LockTest.Synchronization
{
    public class SpinLock : SimpleSpinLock
    {
        public SpinLock() :
            base(true)
        {
        }
    }
}
