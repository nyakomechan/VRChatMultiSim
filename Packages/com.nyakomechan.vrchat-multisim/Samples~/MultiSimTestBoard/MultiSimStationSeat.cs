using UdonSharp;
using UnityEngine;

using VRC.SDKBase;

/// <summary>
/// MultiSim の Station 同期確認用ギミック。VRCStation と Collider が付いた
/// GameObject(椅子)にアタッチして使う。クリック(Interact)で着席。
/// OnStationEntered / OnStationExited は全インスタンスで発火するので、
/// 各エディタの Console で同期を確認できる。
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class MultiSimStationSeat : UdonSharpBehaviour
{
    private VRC.SDK3.Components.VRCStation _station;

    void Start()
    {
        _station = (VRC.SDK3.Components.VRCStation)GetComponent(typeof(VRC.SDK3.Components.VRCStation));
    }

    public override void Interact()
    {
        _station.UseStation(Networking.LocalPlayer);
    }

    public override void OnStationEntered(VRCPlayerApi player)
    {
        Debug.Log($"[MultiSimStationSeat] OnStationEntered: {player.displayName} (id={player.playerId}, isLocal={player.isLocal})");
    }

    public override void OnStationExited(VRCPlayerApi player)
    {
        Debug.Log($"[MultiSimStationSeat] OnStationExited: {player.displayName} (id={player.playerId}, isLocal={player.isLocal})");
    }
}
