using UdonSharp;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;

/// <summary>
/// MultiSim の PlayerData 同期動作確認用ギミック。Cube などの Collider 付きオブジェクトに付けて使う。
/// クリック(Interact)するたびに自分の PlayerData を更新する:
///   pd_count (int) / pd_message (string) / pd_position (Vector3)
/// OnPlayerDataUpdated で「誰の」「どのキーが」「どう変わったか」をログ出力し、値を読み戻して表示する。
/// 2エディタで実行し、片方でクリックしたときにもう片方のエディタでも
/// OnPlayerDataUpdated (remote) が発火して同じ値が読めれば同期成功。
/// </summary>
public class MultiSimPlayerDataBoard : UdonSharpBehaviour
{
    private int _localCount;

    public override void Interact()
    {
        _localCount++;
        VRCPlayerApi local = Networking.LocalPlayer;

        PlayerData.SetInt("pd_count", _localCount);
        PlayerData.SetString("pd_message", $"press {_localCount} from player {local.playerId}");
        PlayerData.SetVector3("pd_position", local.GetPosition());

        Debug.Log($"[MultiSimPlayerDataBoard] (self) set: pd_count={_localCount}");
    }

    public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos)
    {
        string who = player.isLocal ? "self" : "remote";
        Debug.Log($"[MultiSimPlayerDataBoard] OnPlayerDataUpdated: player {player.playerId} ({who}), {infos.Length} key(s)");

        foreach (PlayerData.Info info in infos)
        {
            if (info.State == PlayerData.State.Unchanged)
            {
                continue;
            }
            Debug.Log($"[MultiSimPlayerDataBoard]   {info.Key}: {info.State}");
        }

        if (PlayerData.HasKey(player, "pd_count"))
        {
            int count = PlayerData.GetInt(player, "pd_count");
            string message = PlayerData.GetString(player, "pd_message");
            Vector3 position = PlayerData.GetVector3(player, "pd_position");
            Debug.Log($"[MultiSimPlayerDataBoard]   values: count={count}, message='{message}', position={position}");
        }
    }

    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        Debug.Log($"[MultiSimPlayerDataBoard] OnPlayerRestored: player {player.playerId} (isLocal={player.isLocal})");
    }
}
