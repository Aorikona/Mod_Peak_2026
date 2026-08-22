namespace PeakClassMod.Utils
{
	public static class ClassCheckUtil
	{
		public static bool IsGeneral(Character ch) => (ch != null && ch.IsLocal && ClassManager.IsGeneralActive()) || HatUtils.HasAnyHat(ch, "Hat_Bandana", "Hat_Captain", "Bandana", "Captain");
		public static bool IsTechPriest(Character ch) => (ch != null && ch.IsLocal && ClassManager.IsTechPriestActive()) || HatUtils.HasAnyHat(ch, "Hat_Crab", "Crab");
		public static bool IsMedicHat(Character ch) => (ch != null && ch.IsLocal && ClassManager.IsMedicActive()) || HatUtils.HasAnyHat(ch, "Hat_Medic", "MedicHelmet", "Medic");
		public static bool IsMushroom(Character ch) => (ch != null && ch.IsLocal && ClassManager.IsMushroomancerActive()) || HatUtils.HasAnyHat(ch, "Hat_Mushroom", "Mushroom");
		public static bool IsRogueHat(Character ch) => (ch != null && ch.IsLocal && ClassManager.IsRogueActive()) || HatUtils.HasAnyHat(ch, "Hat_NinjaHeadband", "Ninja");
		public static bool IsGreatEpeeHat(Character ch) => (ch != null && ch.IsLocal && ClassManager.IsGreatEpeeActive()) || HatUtils.HasAnyHat(ch, "Hat_Propeller", "Propeller");
		public static bool IsAstronaut(Character ch) => (ch != null && ch.IsLocal && ClassManager.IsAstronautActive()) || HatUtils.HasAnyHat(ch, "Hat_Astronaut", "Astronaut");
		public static bool IsGobelin(Character ch) => (ch != null && ch.IsLocal && ClassManager.IsGobelinActive()) || HatUtils.HasAnyHat(ch, "Hat_Midsummer", "Midsummer", "Gobelin", "FlowerCrown", "Crown");
		public static bool IsScout(Character ch) => (ch != null && ch.IsLocal && ClassManager.IsScoutActive()) || HatUtils.HasAnyHat(ch, "Hat_Scoutmaster", "Scoutmaster", "Scout");
		public static bool IsWolf(Character ch) => (ch != null && ch.IsLocal && ClassManager.IsWolfActive()) || HatUtils.HasAnyHat(ch, "Hat_WolfEars", "WolfEars", "Wolf");
		public static bool IsCook(Character ch) => (ch != null && ch.IsLocal && ClassManager.IsCookActive()) || HatUtils.HasAnyHat(ch, "Hat_Chef", "Chef", "Cook");
	}
}