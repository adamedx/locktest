using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LockTest.Synchronization;

namespace LockTest.Tests
{
    public class NoOpLock : ISyncLock
    {
        #region ISyncLock Members

        public void Lock()
        {
        }

        public bool WaitForLock(TimeSpan timeout)
        {
            return true;
        }

        public void Unlock()
        {
        }

        #endregion
    }
}
