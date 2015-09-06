using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using LockTest.Synchronization;

namespace LockTest.Tests
{
    public class UnbalancedCallTest : LockTest
    {
        public UnbalancedCallTest(string description) :
            base(description)
        {
        }

        #region ILockTest Members

        public override void Initialize()
        {
        }

        public override void RunTest(ISyncLock lockObject, TestParameters testParameters)
        {
            if (lockObject is NoOpLock)
            {
                throw new InapplicableTestException("Unbalanced calls test not applicable for this lock type");
            }

            ISyncLock syncLock = (ISyncLock)lockObject;

            
            Exception releaseUnlockedException = null;

            Console.WriteLine("Trying unlock with no lock -- this should fail");
            
            try
            {
                syncLock.Unlock();
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("Caught expected InvalidOperationException");
                releaseUnlockedException = e;
            }

            Console.WriteLine("Lock / unlock succeeded");

            if (releaseUnlockedException == null)
            {
                throw new InvalidOperationException("Unlock of already unlocked lock should not have succeeded");
            }
            
            Console.WriteLine("Simple lock / unlock -- this should succeed");

            syncLock.Lock();
            syncLock.Unlock();

            
            Console.WriteLine("Re-entrant lock scenario -- try locking twice -- this should fail for some locks, succeed for others");

            Exception reentrantLockException = null;

            syncLock.Lock();

            try
            {
                Console.WriteLine("Second lock attempt");
                syncLock.Lock();
                Console.WriteLine("Second lock granted");
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("Expected InvalidOperationException caught");
                reentrantLockException = e;
            }

            syncLock.Unlock();
            
            if (syncLock.GetType() == typeof(SimpleSpinLock))
            {
                if (reentrantLockException == null)
                {
                    throw new InvalidOperationException("Lock should not allow re-entrant call to already locked lock");
                }
            }
            else
            {
                Console.WriteLine("Successful re-entrant lock with lock count 2, now perform extra unlock");

                syncLock.Unlock();

                Console.WriteLine("Successfully performed second unlock");

                VerifyUnlocked(syncLock);
            }
            

            // Now test higher scale re-entrance

            Console.WriteLine("Testing locking {0} times on the same thread, then unlocking the same #", lockMax);

            if (!(syncLock is SimpleSpinLock))
            {
                for (int lockCount = 0; lockCount < lockMax; lockCount++)
                {
                    syncLock.Lock();
                }

                for (int unlockCount = 0; unlockCount < lockMax; unlockCount++)
                {
                    syncLock.Unlock();
                }

                VerifyUnlocked(syncLock);
            }

            Console.WriteLine("Successfully verified lock re-entrance count for non-trivial number of re-entrant calls");
            
            Console.WriteLine("Lock once, unlock twice -- this should fail");

            syncLock.Lock();
            syncLock.Unlock();

            Exception unbalancedException = null;

            try
            {
                syncLock.Unlock();
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("Expected InvalidOperationException caught");

                unbalancedException = e;
            }

            if (unbalancedException == null)
            {
                throw new InvalidOperationException("Lock should not allow unlock to be called twice after one call to lock");
            }
             
        }

        #endregion

        void VerifyUnlocked(ISyncLock syncLock)
        {
            InvalidOperationException tooManyUnlocksException = null;

            try
            {
                syncLock.Unlock();
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("Caught expected exception after unlocking too many times: {0}: {1}", e.GetType().ToString(), e.Message);
                tooManyUnlocksException = e;
            }

            if (tooManyUnlocksException == null)
            {
                throw new InvalidOperationException("Unlock should not have succeeded after being in what should have been an unlocked state");
            }
        }

        const int lockMax = 257;
    }
}
