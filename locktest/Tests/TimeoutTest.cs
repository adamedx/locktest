using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using LockTest.Synchronization;

namespace LockTest.Tests
{
    public class TimeoutTest : LockTest
    {
        public TimeoutTest(string description) :
            base(description)
        {
            lockCheckReadyAcquired = new ManualResetEvent(false);
            lockReleaseEvent = new ManualResetEvent(false);
            variationCompleteEvent = new ManualResetEvent(false);
        }

        #region ILockTest Members

        public override void Initialize()
        {
        }

        public override void RunTest(Synchronization.ISyncLock lockObject, TestParameters testParameters)
        {
            if (lockObject is NoOpLock)
            {
                throw new InapplicableTestException("Timeout tests are not needed for no op locks");
            }

            Console.WriteLine("Trying simple timed wait with long timeout");

            bool acquired = lockObject.WaitForLock(new TimeSpan(1, 0, 0));

            if (!acquired)
            {
                throw new InvalidOperationException("Unable to acquire lock in single thread scenario");
            }

            Console.WriteLine("Successfully acquired lock after bounded wait");

            lockObject.Unlock();

            //
            // Wait for 0 seconds for lock after it is known to have been released
            //
            Console.WriteLine("Now wait for 0 and verify correct signaled state");

            TestLockWait(lockObject, new TimeSpan(0, 0, 0), new TimeSpan(0,0,0), true);

            //
            // Signal, Wait for 0 secondsm
            //
            Console.WriteLine("Now acquire the lock and verify that it is acquired after the other thread holds it for 0 s");
            TestLockWait(lockObject, new TimeSpan(0, 0, 5), new TimeSpan(0,0,0), false);

            //
            // Wait for 5s for a lock that is held for 3s
            //
            Console.WriteLine("Wait 5s for a lock held 3s");
            TestLockWait(lockObject, new TimeSpan(0, 0, 5), new TimeSpan(0, 0, 3), false);

            //
            // Wait for 0s for a lock that is held for 3s -- lock should fail to be released
            //
            Console.WriteLine("Wait 0s for a lock held 3s");
            TestLockWait(lockObject, new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 3), false);

            //
            // Wait for 1s for a lock that is held for 4s -- lock should fail to be released
            //
            Console.WriteLine("Wait 1s for a lock held 4s");
            TestLockWait(lockObject, new TimeSpan(0, 0, 1), new TimeSpan(0, 0, 4), false);

            //
            // Wait for 4s for a lock held 1s
            //
            Console.WriteLine("Wait 7s for a lock held 4s");
            TestLockWait(lockObject, new TimeSpan(0, 0, 7), new TimeSpan(0, 0, 4), false);
        }

        #endregion

        void TestLockWait(ISyncLock syncLock, TimeSpan waitBeforeCheckingLock, TimeSpan addtionalTimeBeforeRelease, bool waitForUnlock)
        {
            TimeSpan expectedWaitTime = new TimeSpan((long)(Math.Min(waitBeforeCheckingLock.Ticks, addtionalTimeBeforeRelease.Ticks))) + new TimeSpan(0,0,3);

            lockCheckReadyAcquired.Reset();
            lockReleaseEvent.Reset();
            variationCompleteEvent.Reset();

            lockWaitTime = waitBeforeCheckingLock; // +waitBeforeCheckingLock;
            releaseDelay = addtionalTimeBeforeRelease;

            lockThread = new Thread(LockThreadFunction);

            lockThread.Start(syncLock);

            WaitForLockAcquisition();

            // This should expire

            bool expired = ! syncLock.WaitForLock(new TimeSpan(0,0,0));

            if (! expired)
            {
                throw new InvalidOperationException("Lock was released before expected, due to either lock defect or race conditions that should be rare");
            }

            // Tell the other thread to release the lock
            lockReleaseEvent.Set();

            DateTime waitStart = DateTime.UtcNow;

            if (waitForUnlock)
            {
                WaitForLockRelease();
            }
            
            bool acquiredLock = syncLock.WaitForLock(lockWaitTime);

            DateTime waitEnd = DateTime.UtcNow;

            variationCompleteEvent.Set();

            if (!acquiredLock)
            {
                if (waitForUnlock || ( waitBeforeCheckingLock > addtionalTimeBeforeRelease) )
                {
                    throw new InvalidOperationException("Unable to acquire lock within the specified timeout");
                }
                else
                {
                    Console.WriteLine("Wait for lock timed out as expected");
                }
            }
            else
            {
                syncLock.Unlock();

                if (waitBeforeCheckingLock < addtionalTimeBeforeRelease)
                {
                    throw new InvalidOperationException("Wait did not time out as expected");
                }
                else
                {
                    Console.WriteLine("Wait was satisfied as expected");
                }
            }

            TimeSpan duration = waitEnd - waitStart;

            Console.WriteLine("Expected wait time was {0} and actual time was {1}", expectedWaitTime, duration);

            if (duration > expectedWaitTime)
            {
                throw new InvalidOperationException(string.Format("Expected wait time was {0} and actual time was {1}", expectedWaitTime, duration));
            }
        }

        void LockThreadFunction(object eventParameter)
        {
            ISyncLock syncLock = (ISyncLock)eventParameter;

            syncLock.Lock();

            lockCheckReadyAcquired.Set();

            lockReleaseEvent.WaitOne();

            variationCompleteEvent.WaitOne(releaseDelay);

            syncLock.Unlock();            
        }

        void WaitForLockAcquisition()
        {
            lockCheckReadyAcquired.WaitOne();
        }

        void WaitForLockReadyButNotAcqAcquisition()
        {
            lockCheckReadyAcquired.WaitOne();
        }


        void WaitForLockRelease()
        {
            lockThread.Join();
        }

        Thread lockThread;
        TimeSpan maxWaitDuration = new TimeSpan(0,1,0);
        TimeSpan releaseDelay;
        TimeSpan lockWaitTime;
        ManualResetEvent lockCheckReadyAcquired;
        ManualResetEvent lockReleaseEvent;
        ManualResetEvent variationCompleteEvent;
    }
}