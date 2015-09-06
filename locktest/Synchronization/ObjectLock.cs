using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LockTest.Synchronization
{
    public class ObjectLock : IDisposable
    {
        static ObjectLock()
        {
            stateLock = new SimpleSpinLock();
            objectToLockMap = new Dictionary<object, ObjectLock>();
        }

        public static ObjectLock Lock(object syncObject)
        {
            ObjectLock resultLock;

            using (AutoLock.Lock(stateLock))
            {
                if (!objectToLockMap.TryGetValue(syncObject, out resultLock))
                {
                    resultLock = new ObjectLock(syncObject);

                    objectToLockMap.Add(syncObject, resultLock);
                }

                resultLock.referenceCount++;
            }

            resultLock.instanceLock.Lock();

            return resultLock;            
        }

        #region IDisposable Members

        public void Dispose()
        {
            Unlock();
        }

        #endregion

        void Unlock()
        {
            using (AutoLock.Lock(stateLock))
            {
                referenceCount--;

                if (referenceCount < 0)
                {
                    throw new ArgumentException(string.Format("Internal synchronization error -- negative reference count: {0}", referenceCount));
                }

                if (0 == referenceCount)
                {                    
                    if (! objectToLockMap.Remove(syncObject))
                    {
                        throw new SynchronizationException("Internal synchronization error -- failed to remove object lock");
                    }
                }
            }

            instanceLock.Unlock();
        }

        ObjectLock(object syncObject)
        {
            this.syncObject = syncObject;
            instanceLock = new SimpleSpinLock();
        }

        int referenceCount = 0;
        SimpleSpinLock instanceLock;
        object syncObject;

        static ISyncLock stateLock;
        static Dictionary<object, ObjectLock> objectToLockMap;
    }
}
