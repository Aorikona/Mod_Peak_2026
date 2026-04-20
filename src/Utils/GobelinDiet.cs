using System;
using UnityEngine;

namespace PeakClassMod.Utils
{
    // ==================================================
    // GOBELIN DIET (VERSION TAGS OPTIMISÉE)
    // ==================================================
    public static class GobelinDiet
    {
        public static bool IsPackagedFood(Item item)
        {
            if (item == null) return false;

            // On vérifie si l'objet possède le tag "PackagedFood"
            // C'est une opération binaire (Bitwise check) car c'est un enum [Flags]
            bool hasTag = (item.itemTags & Item.ItemTags.PackagedFood) != 0;

            return hasTag;
        }
    }
}
