using HarmonyLib;
using PeakClassMod;
using PeakClassMod.Handlers;
using PeakClassMod.Patches;
using PeakClassMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Zorro.Core;

namespace PeakClassMod.Handlers
{
    public class AdventurerHandler : MonoBehaviour
    {
        private static AdventurerHandler _localInstance;

        private Character _character;
        private TextMeshProUGUI _timeTextComponent;
        private CharacterController _cc;
        private TextMeshProUGUI _biomeData;
        private TextMeshProUGUI _stormInfoText;
        private Traverse _untilSwitchField;


        private void Start()
        {
            _character = GetComponent<Character>();

            if (_character == null || !_character.IsLocal)
                return;

            _cc = GetComponent<CharacterController>();
            CheckReached_Patch.OnBiomeReached += OnBiomeReached;
        }

        private void Update()
        {
            if (_character == null || !_character.IsLocal || !ClassCheckUtil.IsAdventurer(_character))
                return;

            if (_timeTextComponent == null) CreateUI();
            if (DayNightManager.instance == null) return;

            _timeTextComponent.text = DayNightManager.instance.TimeOfDayString();

            UpdateStormInfo();
        }

        private void UpdateStormInfo()
        {
            if (WindChillZone.instance == null)
            {
                _stormInfoText.text = "";
                return;
            }

            if (_untilSwitchField == null)
                _untilSwitchField = Traverse.Create(WindChillZone.instance).Field("untilSwitch");

            float untilSwitch = _untilSwitchField.GetValue<float>();
            int secondsLeft = Mathf.CeilToInt(Mathf.Max(0f, untilSwitch));

            string stormLabel = WindChillZone.instance.windActive
                ? "Storm End"
                : "Time until next storm";

            Vector3 playerPos = _character.Center;
            Bounds bounds = WindChillZone.instance.windZoneBounds;
            float distance = bounds.Contains(playerPos)
                ? 0f
                : Vector3.Distance(playerPos, bounds.ClosestPoint(playerPos));

            _stormInfoText.text = $"{stormLabel}: {secondsLeft}s\nFog distance: {distance:F0}m";
        }

        private void CreateUI()
        {
            GameObject parent =
                GameObject.Find("GAME/GUIManager/Canvas_HUD");

            if (parent == null) return;

            TMP_FontAsset fontAsset = null;

            foreach (var t in GameObject.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.font != null &&
                    t.font.name == "DarumaDropOne-Regular SDF")
                {
                    fontAsset = t.font;
                    break;
                }
            }

            GameObject obj = new GameObject("TimeText");
            obj.transform.SetParent(parent.transform, false);

            _timeTextComponent = obj.AddComponent<TextMeshProUGUI>();

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(910, 500);

            _timeTextComponent.fontSize = 40;
            _timeTextComponent.color = Color.white;
            _timeTextComponent.font = fontAsset;

            GameObject biome = new GameObject("BiomeText");
            biome.transform.SetParent(parent.transform, false);

            _biomeData = biome.AddComponent<TextMeshProUGUI>();

            RectTransform rectBiome = biome.GetComponent<RectTransform>();
            rectBiome.anchoredPosition = new Vector2(910, 450);
            _biomeData.fontSize = 40;
            _biomeData.color = Color.white;
            _biomeData.font = fontAsset;

            GameObject stormInfo = new GameObject("StormInfoText");
            stormInfo.transform.SetParent(parent.transform, false);

            _stormInfoText = stormInfo.AddComponent<TextMeshProUGUI>();

            RectTransform rectStormInfo = stormInfo.GetComponent<RectTransform>();
            rectStormInfo.anchoredPosition = new Vector2(910, 400);
            _stormInfoText.fontSize = 40;
            _stormInfoText.color = Color.white;
            _stormInfoText.font = fontAsset;
        }

        private void OnBiomeReached(Biome.BiomeType biome)
        {
            if (_character == null || !_character.IsLocal || !ClassCheckUtil.IsAdventurer(_character))
                return;

            _biomeData.text = biome.ToString() ?? "";

            //Si Shore on met le minuteur avant que la brume arrive + distance
            //Si tropique minuteur avant la pluie
            //Si root minuteur avant le vent
            //si montagne minuteur avant la tempete de neige
            //Si volcan minuteur avant monté de la lave + distance ?
            // Lava Fog Wind


            /*
             * Deux infos différentes à récupérer, avec des méthodes différentes :

- untilSwitch (le compte à rebours en secondes) est private dans WindChillZone → il faut passer par Traverse (HarmonyLib), comme déjà fait ailleurs dans le mod (GobelinBlowgunEnforcer.cs, AstronautStatHandler.cs).
- windZoneBounds est public → pas besoin de réflexion, on calcule juste la distance du joueur au point le plus proche de la boîte (0 si le joueur est déjà dedans).
             */

        }
    }
}

