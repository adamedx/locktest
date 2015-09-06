using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LockTest.Synchronization;

namespace LockTest.Tests
{
    public class AutoLockTest : LockTest
    {
        public AutoLockTest(string description) :
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

            Console.WriteLine("Normal usage scenario -- autolock create / dispose with using");

            ExerciseStandardUsage(lockObject, false);

            Console.WriteLine("Normal usage scenario -- succeeded");

            
            Console.WriteLine("Exception in using -- lock should be freed");

            try
            {
                ExerciseStandardUsage(lockObject, true);
            }
            catch (IntentionalTestException e)
            {
                Console.WriteLine("Expected exception caught: {0}: {1}", e.GetType().ToString(), e.Message);
            }             

            Console.WriteLine("Validate that the lock is freed by exercising the normal scenario again");

            ExerciseStandardUsage(lockObject, false);
             
        }

        #endregion

        void ExerciseStandardUsage(ISyncLock lockObject, bool throwException)
        {
            
            using (AutoLock.Lock(lockObject))
            {
                Console.WriteLine("Inside autolock block");

                if (throwException)
                {
                    Console.WriteLine("Testing exception thrown in autolock block -- will now throw exception");

                    throw new IntentionalTestException("Intentional exception thrown inside of an autolock block");
                }
            }
            
            InvalidOperationException unlockException = null;

            Console.WriteLine("Validate that the lock is correctly unlocked after exiting using block");

            try
            {
                lockObject.Unlock();
            }
            catch (InvalidOperationException e)
            {
                unlockException = e;
            }

            if (unlockException != null)
            {
                Console.WriteLine("Caught expected exception from unlocking an unlocked object");
            }
            else
            {
                throw new InvalidOperationException("Failed to get exception in unlocking unlocked object after an autolock was exited");
            }
        }

        internal class IntentionalTestException : Exception
        {
            internal IntentionalTestException(string message) :
                base(message)
            {
            }
        }
    }
}
