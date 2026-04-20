using PeakClassMod;
using PeakClassMod.Utils;
using System;
using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zorro.Core;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(CharacterCustomization), "SetCharacterHat")]
    public class SetHat_Patch
    {
        static void Postfix(int index)
        {
            var hats = Singleton<Customization>.Instance?.hats;
            string hatName = (hats != null && index >= 0 && index < hats.Length) ? hats[index]?.name : "Unknown";
            HatUtils.SetLastLocalHatName(hatName);
            switch (hatName)
            {
                case "Hat_Scoutmaster": ClassManager.ActivateScoutClass(); break;
                case "Hat_Medic": case "MedicHelmet": ClassManager.ActivateMedicClass(); break;
                case "Hat_Crab": ClassManager.ActivateTechPriestClass(); break;
                case "Hat_Mushroom": ClassManager.ActivateMushroomancerClass(); break;
                case "Hat_Propeller": ClassManager.ActivateGreatEpeeClass(); break;
                case "Hat_NinjaHeadband": ClassManager.ActivateRogueClass(); break;
                case "Hat_Bandana": case "Hat_Captain": ClassManager.ActivateGeneralClass(); break;
                case "Hat_Astronaut": ClassManager.ActivateAstronautClass(); break;
                case "Hat_Midsummer": ClassManager.ActivateGobelinClass(); break;
                case "Hat_WolfEars": ClassManager.ActivateWolfClass(); break;
                default: ClassManager.DeactivateAllClasses(); break;
            }
            if (Character.localCharacter != null && GUIManager.instance?.bar != null) GUIManager.instance.bar.ChangeBar();
        }
    }
}
