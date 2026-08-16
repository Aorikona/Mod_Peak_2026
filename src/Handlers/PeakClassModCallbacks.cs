using Photon.Pun;

namespace PeakClassMod.Handlers
{
    public class PeakClassModCallbacks : MonoBehaviourPunCallbacks
    {
        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer) => ClassModNetwork.ClearPlayerCache(otherPlayer.ActorNumber);
    }
}
