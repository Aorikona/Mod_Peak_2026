using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeakClassMod.Utils
{
    internal class DaySegmentUtils
    {
        public bool IsDay()
        {
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
