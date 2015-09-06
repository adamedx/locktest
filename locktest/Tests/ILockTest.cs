using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LockTest.Synchronization;

namespace LockTest.Tests
{
    interface ILockTest    
    {
        void Initialize();
        void RunTest(ISyncLock lockObject, TestParameters testParameters);
        string Description { get; }
    }
}
