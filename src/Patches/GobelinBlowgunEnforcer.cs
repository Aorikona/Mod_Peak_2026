using HarmonyLib;
using UnityEngine;
using PeakClassMod.Utils;

namespace PeakClassMod.Patches
{
    // ==================================================
    // PROTECTION ABSOLUE DE LA SARBACANE (CLIENT FIX & SOULBOUND V2)
    // ==================================================
    [HarmonyPatch(typeof(Item), "Update")]
    public class GobelinBlowgunEnforcer
    {
        static void Postfix(Item __instance)
        {
            // Sécurité de base
            if (__instance == null) return;

            // On ne s'intéresse qu'aux sarbacanes
            if (PerformanceCache.IsBlowgun(__instance))
            {
                Character holder = __instance.holderCharacter;
                bool heldByGobelin = holder != null && ClassCheckUtil.IsGobelin(holder);

                // --- CAS 1 : TENU PAR UN GOBELIN ---
                if (heldByGobelin)
                {
                    // A. Logique Infini (Seulement Local pour éviter conflits réseau)
                    if (holder.IsLocal)
                    {
                        // On force la NON-destruction
                        if (PerformanceCache.DestroyOnUseField != null && (bool)PerformanceCache.DestroyOnUseField.GetValue(__instance) == true)
                        {
                            PerformanceCache.DestroyOnUseField.SetValue(__instance, false);
                        }

                        // On force les munitions à -1 (infini)
                        try
                        {
                            if (__instance.data != null && __instance.HasData(DataEntryKey.ItemUses))
                            {
                                var usesData = __instance.GetData<OptionableIntItemData>(DataEntryKey.ItemUses);
                                if (usesData != null && usesData.Value != -1) usesData.Value = -1;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            PeakClassModPlugin.Log?.LogError(ex);
                        }
                    }

                    // B. Logique Soulbound (Restrictions d'inventaire)
                    if (__instance.UIData != null)
                    {
                        __instance.UIData.canDrop = false;        // Touche G interdite
                        __instance.UIData.canBackpack = false;    // Interdit de mettre dans le sac (pour donner)
                        __instance.UIData.canThrow = false;       // Interdit de lancer


                        // CORRECTION CRITIQUE ICI :
                        __instance.UIData.canPocket = true;       // DOIT ÊTRE TRUE sinon l'objet tombe quand on l'équipe !
                    }
                }
                // --- CAS 2 : TENU PAR QUELQU'UN D'AUTRE (OU AU SOL) ---
                else
                {
                    // Si ce n'est pas un Gobelin qui la tient, on remet tout à la normale.
                    // C'est crucial si le Gobelin meurt et lâche son arme.

                    // 1. Réactiver les mouvements d'inventaire
                    if (__instance.UIData != null)
                    {
                        __instance.UIData.canDrop = true;
                        __instance.UIData.canBackpack = true;
                        __instance.UIData.canThrow = true;

                        __instance.UIData.canPocket = true;
                    }

                    // 2. RETIRER L'INFINI (Correction du bug pour les autres joueurs)
                    // On remet la destruction active
                    if (PerformanceCache.DestroyOnUseField != null && (bool)PerformanceCache.DestroyOnUseField.GetValue(__instance) == false)
                    {
                        PerformanceCache.DestroyOnUseField.SetValue(__instance, true);
                    }

                    // On remet les munitions à 1 (usage unique) si elles étaient à -1
                    try
                    {
                        if (__instance.data != null && __instance.HasData(DataEntryKey.ItemUses))
                        {
                            var usesData = __instance.GetData<OptionableIntItemData>(DataEntryKey.ItemUses);
                            if (usesData != null && usesData.Value == -1)
                            {
                                usesData.Value = 1;
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        PeakClassModPlugin.Log?.LogError(ex);
                    }
                }
            }
        }
    }
}
