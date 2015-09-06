using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LockTest.Synchronization;

namespace LockTest.Tests
{
    public abstract class LockTest : ILockTest
    {
        public LockTest(string description)
        {
            Description = description;
        }

        #region ILockTest Members

        public abstract void Initialize();

        public abstract void RunTest(ISyncLock lockObject, TestParameters testParameters);

        public string Description { get; protected set; }

        #endregion
    }
}
