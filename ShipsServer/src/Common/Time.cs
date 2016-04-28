using System;

namespace ShipsServer.Common
{
    static class Time
    {
        public static int GetMSTime()
        {
            return DateTime.Now.Second;
        }

        public static int GetMSTimeDiff(int oldMSTime, int newMSTime)
        {
            // getMSTime() have limited data range and this is case when it overflow in this tick
            if (oldMSTime > newMSTime)
                return (int)((0xFFFFFFFF - oldMSTime) + newMSTime);
            else
                return (int)(newMSTime - oldMSTime);
        }

        public static int GetMSTimeDiffToNow(int oldMSTime)
        {
            return GetMSTimeDiff(oldMSTime, GetMSTime());
        }

        public static long UnixTimeNow()
        {
            var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)timeSpan.TotalSeconds;
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }
}
