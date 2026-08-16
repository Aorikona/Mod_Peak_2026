using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using UnityEngine.UI;
using UnityEngine.Networking;
using Peak;
using System.IO;
using BepInEx;
using System.Reflection;
using System.Resources;

namespace PeakClassMod.Utils
{
    [HarmonyPatch(typeof(AirportCheckInKiosk), "Start")]
    public class HatSignUtils
    {
        public static GameObject hatSign;

        static void Postfix()
        {
            if (hatSign != null) return;

            hatSign = new GameObject("HatSignPanel");
            var canvas = hatSign.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            hatSign.AddComponent<CanvasScaler>();
            hatSign.AddComponent<GraphicRaycaster>();
            int width = 2048; 
            int height = 1349;

            var panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(hatSign.transform, false);
            var image = panelObj.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0);

            var imageObj = new GameObject("CustomImage");
            imageObj.transform.SetParent(panelObj.transform, false);
            var img = imageObj.AddComponent<Image>();

            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "Mod_Peak_2026.assets.image.HatSign.png";

            string folderPath = Path.Combine(Paths.PluginPath, "PeakClassModAssets");
            Directory.CreateDirectory(folderPath);
            string tempPath = Path.Combine(folderPath, "temp_HatSign.png");

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Debug.LogError("Image introuvable dans le DLL !");
                    return;
                }

                using (FileStream file = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(file);
                }
            }

            if (ImageLoader.Instance == null)
            {
                new GameObject("ImageLoader").AddComponent<ImageLoader>();
            }

            ImageLoader.Instance.Load(tempPath, (tex) =>
            {
                if (tex == null)
                {
                    Debug.LogError("Texture NULL");
                    return;
                }

                Sprite sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f)
                );

                img.sprite = sprite;
                img.color = Color.white;

                hatSign.transform.position = new Vector3(-6.4f, 5.15f, 122.5f);
                hatSign.transform.rotation = Quaternion.Euler(0, 0, 0);
                hatSign.transform.localScale = new Vector3(0.038f, 0.035f, 0.035f);

                var rect = panelObj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(400, 300);
                var imageRect = imageObj.GetComponent<RectTransform>();
                float scaleFactor = 1.1f;

                float widthUI = 380 * scaleFactor;
                float heightUI = widthUI * ((float)height / width);

                imageRect.sizeDelta = new Vector2(widthUI, heightUI);
            });
        }
    }
}
