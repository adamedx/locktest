using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LockTest.Synchronization
{
    public class AutoLock : IDisposable
    {
        AutoLock(ISyncLock baseLock)
        {
            this.baseLock = baseLock;
        }

        public static AutoLock Lock(ISyncLock syncLock)
        {
            AutoLock result = new AutoLock(syncLock);

            syncLock.Lock();

            return result;
        }

        #region IDisposable Members

        public void Dispose()
        {
            baseLock.Unlock();
        }

        #endregion

        protected ISyncLock baseLock;
    }
}
