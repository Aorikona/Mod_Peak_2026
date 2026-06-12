using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeakClassMod.Utils
{
    public static class DaySegmentUtils
    {
        public static bool IsDay()
        {
            if (DayNightManager.instance == null)
            {
                return true;
            }

            string timeOfDayString = DayNightManager.instance.TimeOfDayString();
            if (timeOfDayString == "night" || timeOfDayString == "evening")
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
