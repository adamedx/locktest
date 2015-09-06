using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LockTest.Synchronization
{
    
    public class Lock : ISyncLock
    {
        public Lock()
        {
            queueLock = new SimpleSpinLock();
            owningThreadId = -1;
            threadSuspensionEvent = new ManualResetEvent(true);
            waiters = new Queue<int>();
        }

        #region ISyncLock Members

        void ISyncLock.Lock()
        {
            WaitForLock(TimeWatcher.InfiniteTimeout);
        }

        public bool WaitForLock(TimeSpan timeout)
        {
            return AcquireLock(timeout);
        }

        public void Unlock()
        {
            ReleaseLock();
        }

        #endregion

        bool AcquireLock(TimeSpan timeout)
        {
            int currentThreadId = Thread.CurrentThread.ManagedThreadId;

            TimeWatcher timeWatcher = new TimeWatcher(timeout);

            bool hasLock = false;

            using (AutoLock.Lock(queueLock))
            {
                waiters.Enqueue(currentThreadId);
            }

            while (!hasLock)
            {
                //
                // See if there's no owner
                //
                int currentOwnerId = Interlocked.CompareExchange(ref owningThreadId, currentThreadId, -1);

                int nextThreadId;

                using (AutoLock.Lock(queueLock))
                {
                    nextThreadId = waiters.Peek();

                    if (currentOwnerId == -1 || currentOwnerId == currentThreadId)
                    {
                        hasLock = true;
                        waiters.Dequeue();
                    }
                }

                if (!hasLock)
                {
                    if (timeWatcher.IsExpired)
                    {
                        break;
                    }

                    //
                    // Wait our turn to check
                    //
                    TimeSpan maximumRemainingTime = timeWatcher.TimeRemaining;

                    bool expired = ! SuspendCallerUntilUnlocked(maximumRemainingTime);

                    if ( expired )
                    {
                        break;
                    }
                }
            }

            if (hasLock)
            {
                Interlocked.Increment(ref lockCount);

                SuspendWaiters();
            }
            else
            {
                using (AutoLock.Lock(queueLock))
                {
                    waiters.Dequeue();
                }
            }

            return hasLock;
        }

        void ReleaseLock()
        {
            int currentThreadId = Thread.CurrentThread.ManagedThreadId;

            //
            // Make sure we currently own this lock
            //
            int currentOwnerId = Interlocked.CompareExchange(ref owningThreadId, currentThreadId, currentThreadId);

            if (currentOwnerId != currentThreadId)
            {
                throw new InvalidOperationException("Attempt to release a lock not currently owned by this thread");
            }

            //
            // Now actually release the lock and transfer ownership to the next waiter,
            // but only if we haven't performed a re-entrant lock
            //

            int currentLockCount = Interlocked.Decrement(ref lockCount);

            if (currentLockCount == 0)
            {
                Interlocked.Exchange(ref owningThreadId, -1);
            }

            //
            // Now resume waiters so they can check to see if they can retry
            // taking the lock
            //
            ReleaseWaiters();
        }

        void SuspendWaiters()
        {
            threadSuspensionEvent.Reset();
        }

        void ReleaseWaiters()
        {
            threadSuspensionEvent.Set();
        }

        bool SuspendCallerUntilUnlocked(TimeSpan timeout)
        {
            return threadSuspensionEvent.WaitOne(timeout);
        }
        
        int owningThreadId;

        int lockCount;

        ManualResetEvent threadSuspensionEvent;

        SimpleSpinLock queueLock;

        Queue<int> waiters;
    }
     
}
