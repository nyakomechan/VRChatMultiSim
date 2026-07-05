using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

/// <summary>
/// MultiSim 動作確認用ギミック。Cube などの Collider 付きオブジェクトに付けて使う。
/// クリック(Interact)するたびに:
///  1. Ownership を取得して同期変数を更新し、RequestSerialization する(Manual Sync テスト)
///  2. 全員にネットワークイベントを送る(SendCustomNetworkEvent テスト)
/// 各エディタの Console ログで同期状況を確認できる。
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class MultiSimTestBoard : UdonSharpBehaviour
{
    [UdonSynced] private int _pressCount;
    [UdonSynced] private int _lastPresserId;

    public override void Interact()
    {
        VRCPlayerApi local = Networking.LocalPlayer;
        Networking.SetOwner(local, gameObject);

        _pressCount++;
        _lastPresserId = local.playerId;
        RequestSerialization();

        Debug.Log($"[MultiSimTestBoard] (self) pressed: count={_pressCount} by player {_lastPresserId}");

        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnPressedNetworkEvent), local.playerId, _pressCount);
    }

    public override void OnDeserialization()
    {
        Debug.Log($"[MultiSimTestBoard] OnDeserialization: count={_pressCount}, lastPresser={_lastPresserId}, " +
                  $"owner={Networking.GetOwner(gameObject).playerId}");
    }

    [NetworkCallable]
    public void OnPressedNetworkEvent(int presserId, int count)
    {
        Debug.Log($"[MultiSimTestBoard] NetworkEvent received: presser={presserId}, count={count}, " +
                  $"sender={NetworkCalling.CallingPlayer.playerId}");
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        Debug.Log($"[MultiSimTestBoard] OnPlayerJoined: {player.displayName} (id={player.playerId}, " +
                  $"isLocal={player.isLocal}, master={player.isMaster})");
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        Debug.Log($"[MultiSimTestBoard] OnPlayerLeft: {player.displayName} (id={player.playerId})");
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        Debug.Log($"[MultiSimTestBoard] OnOwnershipTransferred: new owner id={player.playerId}");
    }
}
