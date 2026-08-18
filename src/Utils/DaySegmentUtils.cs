using UnityEngine;

namespace PeakClassMod.Utils
{
	public static class DaySegmentUtils
	{
		public static bool IsDay()
		{
			if (DayNightManager.instance == null) return true;
			string timeOfDayString = DayNightManager.instance.TimeOfDayString();
			return timeOfDayString != "night" && timeOfDayString != "evening";
		}
	}
}