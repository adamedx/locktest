using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using LockTest.Synchronization;

namespace LockTest.Tests
{
    public class ConcurrencyTest : LockTest
    {
        public ConcurrencyTest(Variant variant, string description) :
            base(variant.ToString() + ": " + description)
        {
            this.variant = variant;

            startThreadsEvent = new ManualResetEvent(false);
            endTestEvent = new ManualResetEvent(false);
        }

        public enum Variant
        {
            Default,
            ObjectLock,
            ManyThreads,
            BoundedWait
        }

        #region ILockTest Members

        public override void Initialize()
        {
            startThreadsEvent.Reset();
            endTestEvent.Reset();

            int threadCount = variant == Variant.ManyThreads ? manyThreadCount : defaultThreadCount;

            desiredValues = new bool[threadCount];

            iterationCounts = new int[desiredValues.Length];
            
            completedEvents = new ManualResetEvent[desiredValues.Length];
            
            testException = null;
        }

        public override void RunTest(ISyncLock syncLock, TestParameters testParameters)
        {
            this.syncLock = (ISyncLock)syncLock;
           
            requireLock = !(syncLock is NoOpLock);

            testTimeoutSeconds = testParameters.MaxWaitSeconds;

            Thread[] threads = new Thread[desiredValues.Length];

            for (int threadIndex = 0; threadIndex < threads.Length; threadIndex++)
            {
                threads[threadIndex] = new Thread(ContendingThreadFunction);

                completedEvents[threadIndex] = new ManualResetEvent(false);

                threads[threadIndex].Start(threadIndex);
            }

            startThreadsEvent.Set();

            Console.WriteLine("Sleeping for {0} seconds", testParameters.MaxWaitSeconds);

            Thread.Sleep(testParameters.MaxWaitSeconds * 1000);

            Console.WriteLine("Signaling threads to terminate");

            endTestEvent.Set();

            Console.WriteLine("Waiting for {0} threads to complete after termination signal", threads.Length);

            WaitHandle.WaitAll(completedEvents);

            Console.WriteLine("Completed all waits for thread ids:");

            for (int threadIndex = 0; threadIndex < threads.Length; threadIndex++)
            {
                Console.WriteLine("Thread: {0}\tIterations: {1}", threads[threadIndex].ManagedThreadId, iterationCounts[threadIndex]);
            }

            if (!requireLock)
            {
                if (!expectedSynchronizationErrorOccurred)
                {
                    throw new InvalidOperationException("Expected synchronization error due to absence of lock did not occur.");
                }
                
                Console.WriteLine("Expected synchronization error successfully detected.");
            }
            else if (null != testException)
            {
                throw testException;
            }
        }

        #endregion

        void ContendingThreadFunction(object context)
        {
            startThreadsEvent.WaitOne();

            while (!endTestEvent.WaitOne(0))
            {
                bool desiredValue;

                bool consistent = false;

                bool currentProtectedValue;

                if (requireLock && variant != Variant.Default)
                {
                    using (ObjectLock.Lock(this))
                    {
                        EditState(context, desiredValues, out desiredValue, ref protectedValue, ref consistent, out currentProtectedValue);
                    }
                }
                else
                {
                    if (requireLock)
                    {
                        if (variant == Variant.BoundedWait)
                        {
                            bool expired = syncLock.WaitForLock(new TimeSpan(0, 0, testTimeoutSeconds));

                            if ( expired )
                            {
                                throw new TimeoutException("A timeout occurred with a bounded wait, the wait should have returned sooner");
                            }
                        }
                        else
                        {
                            syncLock.Lock();
                        }
                    }

                    EditState(context, desiredValues, out desiredValue, ref protectedValue, ref consistent, out currentProtectedValue);

                    if (requireLock)
                    {
                        syncLock.Unlock();
                    }
                }                 

                desiredValues[(int)context] = desiredValue;

                iterationCounts[(int)context]++;

                if (!consistent)
                {
                    Console.WriteLine("Synchronization error: {0} should be {1}", currentProtectedValue, desiredValue);

                    SynchronizationException newException = new SynchronizationException(string.Format("Synchronization error detected in thread", Thread.CurrentThread.ManagedThreadId));

                    Interlocked.CompareExchange(ref testException, newException, null);

                    expectedSynchronizationErrorOccurred = requireLock == false;
                        
                    break;
                }
            }

            completedEvents[(int)context].Set();
        }

        static void EditState(object context, bool[] desiredValues, out bool desiredValue, ref bool protectedValue, ref bool consistent, out bool currentProtectedValue)
        {
            desiredValue = !desiredValues[(int)context];

            protectedValue = desiredValue;

            Thread.Yield();

            currentProtectedValue = protectedValue;

            consistent = currentProtectedValue == desiredValue;
        }

        Variant variant;

        const int defaultThreadCount = 2;
        const int manyThreadCount = 8;

        bool protectedValue;

        bool requireLock;

        bool[] desiredValues;

        int[] iterationCounts;

        ManualResetEvent endTestEvent;

        ManualResetEvent startThreadsEvent;

        ManualResetEvent[] completedEvents;

        ISyncLock syncLock;

        Exception testException;

        int testTimeoutSeconds;

        bool expectedSynchronizationErrorOccurred;
    }
}
