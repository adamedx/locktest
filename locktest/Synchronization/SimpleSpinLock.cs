//#define UNSAFE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


namespace LockTest.Synchronization
{
    public class SimpleSpinLock : ISyncLock
    {
        public SimpleSpinLock()
        {
        }

        protected SimpleSpinLock(bool reentrant)
        {
            Reentrant = reentrant;
        }

        public void Lock()
        {
            WaitForLock(TimeWatcher.InfiniteTimeout);
        }

        public bool WaitForLock(TimeSpan timeout)
        {            
            bool hasLock = false;

            TimeWatcher timeWatcher = new TimeWatcher(timeout);            

            int currentThreadId = Thread.CurrentThread.ManagedThreadId;

            while (!hasLock)
            {
                int owningThreadId = Interlocked.CompareExchange(ref owningThreadIdState, currentThreadId, -1);

                hasLock = owningThreadId == -1;

                if (owningThreadId == currentThreadId)
                {
                    if (!Reentrant)
                    {
                        throw new InvalidOperationException(string.Format("Thread id {0} tried to take a lock it already owns", currentThreadId));
                    }

                    hasLock = true;
                }

                if (timeWatcher.IsExpired)
                {
                    break;
                }
            }

            if (hasLock && Reentrant)
            {
#if UNSAFE               
//#error Remove this error to test unsafe scenarios that require interlocks / memory barriers
                lockCount++;
#else
                Interlocked.Increment(ref lockCount);
#endif
            }

            return hasLock;
        }

        public void Unlock()
        {
            int currentThreadId = Thread.CurrentThread.ManagedThreadId;
#if UNSAFE
            int owningThreadId = owningThreadIdState;
#else
            int owningThreadId = Interlocked.CompareExchange(ref owningThreadIdState, -1, -1);
#endif
            if (owningThreadId == -1)
            {
                throw new InvalidOperationException(string.Format("Thread id {0} attempted to unlock a thread that was not locked", currentThreadId));
            }

            if (owningThreadId != currentThreadId)
            {
                if (!Reentrant)
                {
                    throw new InvalidOperationException(string.Format("Thread id {0} attempted to release a lock owned by thread id {1}", currentThreadId, owningThreadId));
                }
            }

            int currentLocks = 0;

            if (Reentrant)
            {
#if UNSAFE
                lockCount--;
                currentLocks = lockCount;  
#else
                Interlocked.Decrement(ref lockCount);
                currentLocks = Interlocked.CompareExchange(ref lockCount, 0, 0);
#endif      
            }
           
            if (currentLocks == 0)
            {
                Interlocked.Exchange(ref owningThreadIdState, -1);
            }
        }

        protected bool Reentrant { get; private set; }

        int owningThreadIdState = -1;
        int lockCount = 0;
    }
}
