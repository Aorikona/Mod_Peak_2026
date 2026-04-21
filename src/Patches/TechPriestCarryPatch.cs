using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;
using PeakClassMod.Utils;

namespace PeakClassMod.Patches
{
    [HarmonyPatch]
    public class TechPriestCarryPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var methods = new List<MethodBase>();

            var update = AccessTools.Method(typeof(CharacterCarrying), "Update");
            if (update != null) methods.Add(update);

            var startCarry = AccessTools.Method(typeof(CharacterCarrying), "RPCA_StartCarry");
            if (startCarry != null) methods.Add(startCarry);

            return methods;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            FieldInfo fullyPassedOutField = AccessTools.Field(typeof(CharacterData), "fullyPassedOut");
            FieldInfo passedOutField = AccessTools.Field(typeof(CharacterData), "passedOut");
            FieldInfo deadField = AccessTools.Field(typeof(CharacterData), "dead");

            MethodInfo bypassMethod = AccessTools.Method(typeof(TechPriestCarryPatch), nameof(ShouldBypassPassedOutCheck));

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand is FieldInfo field)
                {
                    if (field == fullyPassedOutField || field == passedOutField)
                    {
                        // We are about to pop the CharacterData from stack to load fullyPassedOut.
                        // We can duplicate the CharacterData object *before* Ldfld, and pass it along with the boolean to our method.
                        codes.Insert(i, new CodeInstruction(OpCodes.Dup));
                        // Stack is now [..., CharacterData, CharacterData]
                        // After Ldfld (at i+1), stack is [..., CharacterData, bool fullyPassedOut]

                        // Pass CharacterData and the bool to bypassMethod
                        codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, bypassMethod));

                        i += 2; // skip the inserted instructions
                    }
                }
            }

            return codes;
        }

        public static bool ShouldBypassPassedOutCheck(CharacterData data, bool originalPassedOut)
        {
            if (data == null) return originalPassedOut;

            // We need to know if the character carrying this person is a TechPriest.
            // But we don't have CharacterCarrying here.
            // Wait, we can get Character.localCharacter, but we need to ensure the carrier is the local player.
            // If we are looking at this, and we only want the *local* TechPriest to have the carry buff:
            Character local = Character.localCharacter;
            if (local != null && local.refs != null && local.refs.view != null && local.refs.view.IsMine && ClassManager.IsTechPriestActive())
            {
                // TechPriest can carry conscious players.
                // But if they are dead, they must still be dropped! (originalPassedOut wouldn't cover dead, usually it's fullyPassedOut || dead. Wait, if dead is checked separately, fullyPassedOut can just return true. But wait, if dead is checked, it will fail the other check.
                // The task says "bypass this specific boolean check exclusively for the Tech Priest. ... ensuring the Tech Priest still drops dead players but keeps conscious ones."
                // If we return true here, we bypass fullyPassedOut check (it acts like they ARE passed out).
                if (!data.dead)
                {
                    return true;
                }
            }

            return originalPassedOut;
        }
    }
}
