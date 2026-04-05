using PeakClassMod.Utils;

namespace PeakClassMod.Utils
{
    public static class ClassCheckUtil
    {
        public static bool IsGeneral(Character ch) => HatUtils.HasAnyHat(ch, "Hat_Bandana", "Hat_Captain");
        public static bool IsTechPriest(Character ch) => HatUtils.HasHat(ch, "Hat_Crab");
        public static bool IsMedicHat(Character ch) => HatUtils.HasAnyHat(ch, "Hat_Medic", "MedicHelmet");
        public static bool IsMushroom(Character ch) => HatUtils.HasHat(ch, "Hat_Mushroom");
        public static bool IsRogueHat(Character ch) => HatUtils.HasHat(ch, "Hat_NinjaHeadband");
        public static bool IsGreatEpeeHat(Character ch) => HatUtils.HasHat(ch, "Hat_Propeller");
        public static bool IsAstronaut(Character ch) => HatUtils.HasHat(ch, "Hat_Astronaut");
        public static bool IsGobelin(Character ch) => HatUtils.HasAnyHat(ch, "Hat_Midsummer");
        public static bool IsScout(Character ch) => HatUtils.HasHat(ch, "Hat_Scoutmaster");
        public static bool IsDualNature(Character ch) => HatUtils.HasHat(ch, "Hat_DualNature");
    }
}
