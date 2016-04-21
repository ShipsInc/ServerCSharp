using System;

namespace ShipsServer.Common
{
    class Timer
    {
        public static UInt32 GetMSTime()
        {
            return (UInt32)DateTime.Now.Second;
        }

        public static UInt32 GetMSTimeDiff(UInt32 oldMSTime, UInt32 newMSTime)
        {
            // getMSTime() have limited data range and this is case when it overflow in this tick
            if (oldMSTime > newMSTime)
                return (0xFFFFFFFF - oldMSTime) + newMSTime;
            else
                return newMSTime - oldMSTime;
        }

        public static UInt32 GetMSTimeDiffToNow(UInt32 oldMSTime)
        {
            return GetMSTimeDiff(oldMSTime, GetMSTime());
        }

    }
}
