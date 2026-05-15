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
        private static System.Reflection.FieldInfo _destroyOnUseField;

        static void Postfix(Item __instance)
        {
            // Sécurité de base
            if (__instance == null) return;

            if (_destroyOnUseField == null)
            {
                _destroyOnUseField = AccessTools.Field(typeof(Item), "destroyOnUse");
            }

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
                        if (_destroyOnUseField != null && (bool)_destroyOnUseField.GetValue(__instance) == true)
                        {
                            _destroyOnUseField.SetValue(__instance, false);
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
                        catch { }
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
                    if (_destroyOnUseField != null)
                    {
                        // On remet à TRUE (l'objet doit se détruire après usage pour les non-Gobelins)
                        _destroyOnUseField.SetValue(__instance, true);
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
                    catch { }
                }
            }
        }
    }
}
