using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;

namespace PeakClassMod
{
    // ==================================================
    // CONSTANTES DE BALANCE
    // ==================================================
    public static class ClassBalance
    {
        // --- GOBELIN ---
        // Nom exact trouvé grâce à votre espionnage
        public const string BLOWGUN = "HealingDart Variant";

        public const float GOBELIN_SIZE = 0.5f;
        public const float GOBELIN_CURSE_PER_SHOT = 0.05f;

        // --- AUTRES CLASSES ---
        public const float ASTRONAUT_JUMP_IMPULSE = 1200f;
        public const float ASTRONAUT_SPRINT_MULT = 1.7f;
        public const float ASTRONAUT_HUNGER_MULT = 1.3f;
        public const float ASTRONAUT_MAX_WALK_SPEED = 1.6f;
        public const float ASTRONAUT_LOW_GRAV_DURATION = 3f;

        public const float GENERAL_CURSE_MIN = 0.35f;
        public const float GENERAL_HUNGER_MULT = 2.0f;
        public const float GENERAL_WEIGHT_MULT = 2.0f;

        public const float MEDIC_CURSE = 0.15f;
        public const int MEDIC_MAX_HEALS = 10;
        public const float MEDIC_HEAL_AMOUNT = 0.1f;
        public const float MEDIC_HEAL_COOLDOWN = 1f;

        public const float SCOUT_STAMINA_MULT = 0.80f;
        public const float SCOUT_HUNGER_MULT = 1.7f;
        public const float SCOUT_WEIGHT_MULT = 0.5f;

        public const float ROGUE_STAMINA_MULT = 1.7f;
        public const int ROGUE_EXTRA_JUMPS = 2;

        public const float GREATEPEE_SLEEP_MIN = 30f;
        public const float GREATEPEE_SLEEP_MAX = 60f;
        public const float GREATEPEE_EFFECT_DURATION = 99999f;

        public const float MUSHROOM_STAMINA_COOLDOWN = 10f;
        public const float MUSHROOM_CURSE_AMOUNT = 0.02f;

        public const float ITEM_REQUEST_DELAY = 0.3f;
        public const float ITEM_GIVE_COOLDOWN = 2f;
        public const float ITEM_CACHE_DURATION = 300f;
        public const float SIZE_CHECK_INTERVAL = 0.5f;
    }
}
