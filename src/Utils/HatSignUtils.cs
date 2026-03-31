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

            var panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(hatSign.transform, false);
            var image = panelObj.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0);

            var imageObj = new GameObject("CustomImage");
            imageObj.transform.SetParent(panelObj.transform, false);
            var img = imageObj.AddComponent<Image>();

            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "Mod_Peak_2026.assets.image.HatSign.raw";
            bool imageLoaded = false;
            int width = 4970;
            int height = 3268;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;

                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                if (buffer.Length != width * height * 4)
                {
                    return;
                }

                tex.LoadRawTextureData(buffer);
                tex.Apply();
                tex.filterMode = FilterMode.Bilinear;

                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
                img.sprite = sprite;
                imageLoaded = true;
            }

            if (!imageLoaded)
            {
                img.enabled = false; 
            }

            img.color = Color.white;

            hatSign.transform.position = new Vector3(-21.88f, 3.95f, 107.3971f);
            hatSign.transform.rotation = Quaternion.Euler(0, 90, 180);
            hatSign.transform.localScale = Vector3.one * 0.015f;

            
            var rect = panelObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 300);
            var imageRect = imageObj.GetComponent<RectTransform>();
            float scaleFactor = 1.1f; 

            float widthUI = 380 * scaleFactor;
            float heightUI = widthUI * ((float)height / width);


            imageRect.sizeDelta = new Vector2(widthUI, heightUI);
        }
    }
}
