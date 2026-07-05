# VRChat MultiSim

ParrelSync でクローンした複数の Unity Editor を localhost TCP で接続し、**ビルドせずに Editor 上で VRChat ワールドのマルチプレイ動作確認**を行うツールです。VRChat ClientSim に NetCode 的な通信レイヤーを追加するイメージで動作します。

## 仕組み

- **ParrelSync のオリジナルプロジェクト = ホスト**(プレイヤーID 1、マスター)、**クローン = クライアント**として自動判別されます。
- ClientSim は通常どおり起動しますが、Ready 化(Udon の開始と `OnPlayerJoined` の配信)をハンドシェイク完了まで保留します。これにより **全インスタンスでプレイヤーID・マスターが一致した状態で Udon が動き始めます**。
- 同期処理は ClientSim 同梱のシリアライズ基盤(`ClientSimNetworkIdHolder` / NetworkID)をそのまま利用します。NetworkID はシーンの Transform パス順に決定的に割り当てられるため、クローン間で必ず一致します。

## 同期されるもの

| 機能 | 対応 |
|---|---|
| `[UdonSynced]` 変数 (Continuous / Manual + `RequestSerialization`) | ✅ |
| `OnPreSerialization` / `OnPostSerialization` / `OnDeserialization` | ✅ |
| `SendCustomNetworkEvent`(引数付き `[NetworkCallable]` 含む、`NetworkCalling.CallingPlayer` 対応) | ✅ |
| Ownership (`Networking.SetOwner` / `TakeOwnership` / Pickup による移譲 / `OnOwnershipTransferred`) | ✅ |
| `VRCObjectSync`(位置・回転・Rigidbody) | ✅(10Hz、補間なし) |
| `VRCObjectPool` | ✅(同期変数として) |
| プレイヤーの参加/退出 (`OnPlayerJoined` / `OnPlayerLeft`)、マスター判定 | ✅ |
| プレイヤー位置の反映(リモートプレイヤーの移動) | ✅(10Hz) |
| レイトジョイン(参加時に現在の同期状態を受信) | ✅ |
| ボイス、アバター、Station 着席の同期、PlayerData/Persistence | ❌ 未対応 |
| `VRCInstantiate` した動的オブジェクトの同期 | ❌(VRChat 本番同様、ObjectPool を使ってください) |

## 使い方

1. **メニュー → Tools → VRChat MultiSim → Enable Multiplayer Simulation** をオンにする
   (EditorPrefs 保存なのでオリジナル/クローンの両方に効きます)
2. **Tools → VRChat MultiSim → Open ParrelSync Clones Manager** からクローンを作成し、クローンエディタを開く
3. **オリジナル側で ▶ Play**(ホストがポート 24685 で待受開始)
4. **クローン側で ▶ Play**(自動でホストに接続し、プレイヤーID 2 として参加)
5. 両方の Game ビューで動作確認。Console に `[MultiSim]` プレフィックスでログが出ます

- Play の順番は「ホストが先」推奨ですが、クライアントは最大 15 秒間リトライするので多少前後しても繋がります。
- ホストが見つからない場合は警告を出して**通常のシングルプレイ ClientSim として起動**します(ハングしません)。
- 3 人以上のテストはクローンを追加するだけです(Clones Manager から複数作成可)。

## 動作確認用サンプル

`Assets/MultiSimSamples/MultiSimTestBoard.cs`(UdonSharp・Manual Sync)をシーンの Cube 等に付けると:

- クリック(Interact)→ Ownership 取得 + 同期変数インクリメント + `RequestSerialization` + 全員へネットワークイベント送信
- 各エディタの Console で `OnDeserialization` / NetworkEvent の受信を確認できます

## 制限・注意

- **VRChat SDK (com.vrchat.worlds) 3.10.x 向け**です。ClientSim 内部にリフレクションでアクセスしているため、SDK 更新で動かなくなった場合は Console に「どのメンバーが見つからないか」がエラー表示されます。
- 同期は JSON/TCP による簡易実装です。実際の VRChat のような帯域制限・送信レート制御・補間は再現しません(Continuous は約 10Hz のポーリング)。
- Continuous オブジェクトの `OnPreSerialization` 内で行った変更は 1 tick 遅れて送信されます(Manual は正確です)。
- マスター移譲は「ホストが最初のマスター」を前提としています。ホスト(オリジナル)の Play を先に止めるとセッション終了扱いになり、クローンはシングルプレイ状態で継続します。
- リモートプレイヤーの見た目は ClientSim 標準のリモートプレイヤー(カプセル)で、位置・回転のみ反映されます(ボーン単位の姿勢は未対応)。
- ホストが自分のインスタンスオーナーになります。`isInstanceOwner` はホストのみ true です。
- ポートは `Tools/VRChat MultiSim` の設定(既定 24685)。他ツールと衝突する場合は `MultiSimPrefs` の EditorPrefs キー `MultiSim.Port` を変更してください。

## ファイル構成

```
Assets/MultiSim/
  MultiSimCore.cs       起動制御・ハンドシェイク・ID再割当・Ready制御・同期ループ
  MultiSimTransport.cs  localhost TCP(長さプレフィックス付き JSON)
  MultiSimRegistry.cs   NetworkID → ClientSimNetworkIdHolder のマップ構築
  MultiSimReflect.cs    ClientSim 内部へのリフレクション橋渡し(SDK 3.10.x 固定)
  MultiSimJson.cs       VRCJson ベースのメッセージ/イベント引数シリアライズ
  MultiSimPrefs.cs      設定(EditorPrefs)
  MultiSimMenu.cs       Tools メニュー
Assets/MultiSimSamples/
  MultiSimTestBoard.cs  動作確認用 UdonSharp ギミック
```

すべて `#if UNITY_EDITOR` で囲まれており、ワールドのビルド/アップロードには一切含まれません。
