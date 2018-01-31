using System;
using System.IO;

namespace Predix.Pipeline.Helper
{
    public static class Utility
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static int ToEpoch(this DateTime dateTime)
        {
            TimeSpan timeSpan = dateTime - new DateTime(1970, 1, 1);
            return (int)timeSpan.TotalSeconds;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="unixTime"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this int unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        public static string ActiveBin { get; set; } =
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
    }
}
