using PeakClassMod;
using PeakClassMod.Handlers;
using PeakClassMod.Utils;
using System;
using UnityEngine;
using HarmonyLib;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeakClassMod.Patches
{
    [HarmonyPatch(typeof(RunManager), "StartRun")]
    public class RunManager_StartRun_Patch
    {
        static void Postfix()
        {
            if (PhotonNetwork.IsMasterClient) { ClassModNetwork.HasReceivedStartItems.Clear(); }
            PeakClassModPlugin.Instance?.StartCoroutine(AddEffectsDelayed());
        }
        public static IEnumerator AddEffectsDelayed()
        {
            yield return new WaitForSeconds(1.5f); var lc = Character.localCharacter; if (lc == null) { yield return new WaitForSeconds(1f); lc = Character.localCharacter; if (lc == null) yield break; }
            if (ClassCheckUtil.IsMushroom(lc)) { ClassManager.ActivateMushroomancerClass(); ClassManager.StartMushroomancerWatch(lc); }
            else if (ClassCheckUtil.IsGeneral(lc)) { ClassManager.ActivateGeneralClass(); float c = lc.refs?.afflictions?.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Curse) ?? 0f; if (c < ClassBalance.GENERAL_CURSE_MIN) lc.refs?.afflictions?.SetStatus(CharacterAfflictions.STATUSTYPE.Curse, ClassBalance.GENERAL_CURSE_MIN); }
            else if (ClassCheckUtil.IsGreatEpeeHat(lc)) { ClassManager.ActivateGreatEpeeClass(); ClassManager.StartGreatEpeeRoutines(lc); }
            else if (ClassCheckUtil.IsMedicHat(lc)) { ClassManager.ActivateMedicClass(); float c = lc.refs?.afflictions?.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Curse) ?? 0f; if (c < ClassBalance.MEDIC_CURSE) lc.refs?.afflictions?.SetStatus(CharacterAfflictions.STATUSTYPE.Curse, ClassBalance.MEDIC_CURSE); }
            else if (ClassCheckUtil.IsAstronaut(lc)) ClassManager.ActivateAstronautClass();
            else if (ClassCheckUtil.IsTechPriest(lc)) ClassManager.ActivateTechPriestClass();
            else if (ClassCheckUtil.IsRogueHat(lc)) ClassManager.ActivateRogueClass();
            else if (ClassCheckUtil.IsGobelin(lc)) ClassManager.ActivateGobelinClass();
            else if (ClassCheckUtil.IsScout(lc)) ClassManager.ActivateScoutClass();
            else if (ClassCheckUtil.IsWolf(lc)) ClassManager.ActivateWolfClass();
        }
    }
}
