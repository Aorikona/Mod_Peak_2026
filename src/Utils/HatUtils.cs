using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Zorro.Core;
using Photon.Pun;
using System.Threading.Tasks;

namespace PeakClassMod.Utils
{
    public static class HatUtils
    {
        private static string _lastLocalHatName;
        private static PersistentPlayerDataService _playerDataService;

        public static void SetLastLocalHatName(string hatName) { if (!string.IsNullOrEmpty(hatName)) _lastLocalHatName = hatName; }

        public static bool TryGetHatName(Character ch, out string hatName)
        {
            hatName = null; if (ch == null) return false;
            if (_playerDataService == null) _playerDataService = GameHandler.GetService<PersistentPlayerDataService>();
            var ppds = _playerDataService;
            PersistentPlayerData data = null;
            if (ch.photonView?.Owner != null) data = ppds?.GetPlayerData(ch.photonView.Owner);
            if (data == null && ch.IsLocal && PhotonNetwork.LocalPlayer != null) data = ppds?.GetPlayerData(PhotonNetwork.LocalPlayer);
            if (data != null) { var hats = Singleton<Customization>.Instance?.hats; int i = data.customizationData.currentHat; if (hats != null && i >= 0 && i < hats.Length && hats[i] != null) hatName = hats[i].name; }
            if (string.IsNullOrEmpty(hatName) && ch.IsLocal) hatName = _lastLocalHatName;
            return !string.IsNullOrEmpty(hatName);
        }
        public static bool HasAnyHat(Character ch, params string[] names) { if (!TryGetHatName(ch, out var name)) return false; return names.Contains(name, StringComparer.OrdinalIgnoreCase); }
        public static bool HasHat(Character ch, string expected) => TryGetHatName(ch, out var name) && string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);
    }
}
