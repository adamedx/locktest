using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LockTest.Synchronization
{
    public interface ISyncLock
    {
        void Lock();

        bool WaitForLock(TimeSpan timeout);

        void Unlock();
    }
}
