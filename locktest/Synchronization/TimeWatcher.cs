using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LockTest.Synchronization
{
    public class TimeWatcher
    {
        public TimeWatcher(TimeSpan duration)
        {
            DateTime now = DateTime.UtcNow;

            startTime = now - pastEpsilon;

            endTime = duration == TimeSpan.MinValue ? startTime : startTime + duration;
            remaining = TimeSpan.MinValue;

            neverExpires = duration == infiniteTimeSpan ? true : false;

            expired = duration == TimeSpan.MinValue ? true : false;
        }

        public bool IsExpired
        {
            get
            {
                return neverExpires ? false : TimeRemaining == TimeSpan.MinValue;
            }
        }

        public TimeSpan TimeRemaining
        {
            get
            {
                TimeSpan result = TimeSpan.MinValue;

                if (neverExpires)
                {
                    return TimeWatcher.InfiniteTimeout;
                }

                if (!expired)
                {
                    DateTime now = DateTime.UtcNow;

                    if (DateTime.Compare(startTime, now) > 0)
                    {
                        expired = true;
                    }
                    else if (DateTime.Compare(now, endTime) > 0)
                    {
                        expired = true;
                    }
                    else
                    {
                        result = endTime - now;
                    }
                }

                return result;
            }
        }

        public static TimeSpan InfiniteTimeout
        {
            get
            {
                return infiniteTimeSpan;
            }
        }

        TimeSpan pastEpsilon = new TimeSpan(1);

        DateTime startTime;
        DateTime endTime;

        TimeSpan remaining;

        bool expired;

        bool neverExpires;

        static TimeSpan infiniteTimeSpan = new TimeSpan(0, 0, 0, 0, -1);
    }
}
