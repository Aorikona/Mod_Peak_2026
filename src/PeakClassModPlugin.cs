// ============================================================
// PEAK Class Mod - Version: 5.1.0 (Gobelin Infinite Fix)
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Peak.Afflictions;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using Zorro.Core;
using PeakClassMod;
using System.Text;
using System.Threading.Tasks;
using PeakClassMod.Handlers;

namespace PeakClassMod

{
    [BepInPlugin("com.peakmod.classmod", "PEAK Class Mod", "5.1.0")]
    public class PeakClassModPlugin : BaseUnityPlugin
    {
        public static PeakClassModPlugin Instance;
        internal static ManualLogSource Log;
        private static Harmony harmony;

        public static bool scoutActive = false;
        public static bool medicActive = false;
        public static bool techPriestActive = false;
        public static bool mushroomancerActive = false;
        public static bool greatEpeeActive = false;
        public static bool rogueActive = false;
        public static bool generalActive = false;
        public static bool astronautActive = false;
        public static bool gobelinActive = false;
        public static bool wolfActive = false;
        public static string currentClass = "None";

        private GameObject _callbacksGO;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            harmony = new Harmony("com.peakmod.classmod");
            harmony.PatchAll();

            _callbacksGO = new GameObject("PeakClassModCallbacks");
            GameObject.DontDestroyOnLoad(_callbacksGO);
            _callbacksGO.AddComponent<PeakClassModCallbacks>();

            Log?.LogInfo("[ClassMod] Ready. Version 5.1.0 loaded (Infinite Fix).");
        }



        private void OnDestroy()
        {
            try { harmony?.UnpatchSelf(); } catch (Exception ex) { PeakClassModPlugin.Log?.LogError(ex); }
            if (_callbacksGO != null) UnityEngine.Object.Destroy(_callbacksGO);
        }

        public void EnsureComponent<T>(GameObject go) where T : UnityEngine.Component
        {
            if (go != null && go.GetComponent<T>() == null) go.AddComponent<T>();
        }

        public static List<string> BuildStartItemsByHatName(string hatName)
        {
            var items = new List<string>();
            if (string.IsNullOrEmpty(hatName)) return items;

            switch (hatName)
            {
                case "Hat_Mushroom":
                    for (int i = 0; i < 3; i++) items.Add(StartBerries[UnityEngine.Random.Range(0, StartBerries.Length)]);
                    break;
                case "Hat_Medic": items.Add("FirstAidKit"); items.Add("Cure-All"); break;
                case "Hat_Crab": items.Add("ChainShooter"); items.Add("ChainShooter"); break;
                case "Hat_NinjaHeadband": items.Add("RopeSpool"); items.Add("RopeSpool"); break;
                case "Hat_Astronaut": items.Add("BalloonBunch"); break;
                case "Hat_Midsummer": items.Add(ClassBalance.BLOWGUN); items.Add("MagicBean"); break;
            }
            return items;
        }

        private static readonly string[] StartBerries = new[] { "Shroomberry_Red", "Shroomberry_Yellow", "Shroomberry_Purple", "Shroomberry_Blue", "Shroomberry_Green" };
    }
}
