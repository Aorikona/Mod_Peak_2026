using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace PeakClassMod.Utils
{
    public class ImageLoader : MonoBehaviour
    {
        public static ImageLoader Instance;

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Load(string path, Action<Texture2D> callback)
        {
            StartCoroutine(LoadCoroutine(path, callback));
        }

        private IEnumerator LoadCoroutine(string path, Action<Texture2D> callback)
        {
            string url = "file://" + path;

            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
                callback(null);
            }
            else
            {
                callback(DownloadHandlerTexture.GetContent(request));
            }
        }
    }
}
