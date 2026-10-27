# Changelog

E'sWeaponsLib(EWL)の変更履歴です。形式は [Keep a Changelog](https://keepachangelog.com/ja/1.1.0/) に沿います。

- 書くのは利用者(プレイヤー・Mod開発者)に見える変化だけです。詳しい説明は [docs/](docs/index.md) にあります。
- 公開面 = XMLカスタムモジュールの名前・要素と、C#の公開型・メンバー(一覧は [api-surface.txt](api-surface.txt))。
- 既存のブロックXMLかコンパイル済みの外部DLLが壊れる変更は **[Breaking]** と書き、「旧 → 新」の例を付けます。
- 1.0より前は破壊的変更がありえます。1.0以降、破壊的変更はメジャー版でだけ行います。

## [Unreleased]

### 武装

- MP クライアント: `KeepFireIntervalOnRespawn` の武装が、リスポーンをまたいでも `FireCooldownRemaining` の目安(発射間隔の残り)を引き継ぐ。
  旧: リスポーンで目安が 0 になり、実際は撃てないのに待ちなしと見えていた。待機弾も持ち越し中は出ない。
- MP クライアント: 発射間隔の目安を、弾が出た時ではなく発射が届いた時から数える(ホストと同じ起点)。待機弾は弾が出るまで表示する。

## [0.3.0] - 2026-10-25

### 武装

- 貫通: 上限に達する前に装甲を抜けた弾は、出口の外(弾の大きさの分だけ先)へ出て命中直前の速度・向きのまま飛び続け、残りの貫通量で次の装甲も貫通する。
  爆発する弾は抜けた所で起爆を始める。旧: 抜けた所で止まって命中・起爆していた(弾の大きさを考えず、出口に弾がはみ出していた)。
- 貫通: ダメージ・衝撃力・命中イベントを、通ったブロックそれぞれに与える。旧: 止まった所のブロックだけ。命中イベントの位置は各ブロックへの入口。
- 貫通: 貫通するのはマシンのブロックだけにした。地面・レベルの物体には普通の弾として当たり、跳ねる。旧: 地面も装甲として数え、地面に埋まって止まっていた。
- 貫通: 弾は装甲の中で止まらない。旧: 上限に達した所で装甲の中に止まり、止まったブロックにくっついていた。
  - 命中したブロック自体を抜けられない弾は貫通せず、貫通しない弾と同じく普通に当たる(物理の衝突で装甲を押す)。
  - `Total` で手前の装甲を抜け、奥のブロックで上限に達した弾は、そのブロックの入口の手前から飛び続け、そのブロックに普通の弾として当たる。
  - `FirstBlock` で抜けたが裏のブロックが近く弾が入らないときは、前端が裏面の手前に来る所から飛び続け、そのブロックに物理の衝突で当たる。
  - 貫通して置き直された弾は、その位置で重なるマシンのブロック(次に当たるブロックを除く)とは、弾が消えるまで衝突しない(通った装甲に穴が開いた扱い)。
  - 抜けた弾が出口の先に入るのに要る長さと置き直す位置は、弾の位置からずらした当たり判定でも実際の位置・長さで求める。
- 起爆遅延(`DetonationDelay`)・時限信管(`TimeFuse`)・寿命(`Lifetime`)を物理時間で数える。旧: 描画フレームで数えていたため、再生速度や fps で
  命中後に弾が押せる物理ステップ数が変わり、威力が変わっていた(例: 起爆遅延 0.02 秒の弾が、等速では命中後3〜4ステップ、50%では2ステップ押していた)。
  起爆遅延は命中後の物理ステップ数(0.01秒ごと、端数は切り上げ)になるので、等速で遊んでいた武装は命中の手応えが弱まることがある。
- 当たり判定の有効化(`ColliderEnableDelay`)・推力(`ThrustTime`)・誘導の開始・命中後に消えるまで(爆発も即消滅もしない弾)も物理時間で数える。
  旧: 描画フレームで数えていたため、再生速度や fps で当たり判定が付く位置と推力の総量が変わっていた。
- `ImpactForce` の力を、衝突直前の弾の速度から求める。旧: 衝突後の速度(止まった弾ではほぼ0、跳ねた弾では逆向きだった)。命中の手応えが変わるので、従来に近づけたい武装は値を調整する。
- 貫通: `PenetrationMode` が `FirstBlock` の弾は、1ブロックを抜けたら貫通を終え、以後は普通の弾として当たる(跳ねる)。旧: 残りの貫通量で次のブロックも次々に貫通していた。

- `StartDelay`(`WeaponSpec.StartDelay`、既定0)を追加。シミュ開始・リスポーンのたびに撃てない時間 [s]。
- `KeepFireIntervalOnRespawn`(`WeaponSpec.KeepFireIntervalOnRespawn`、既定 false)を追加。true ならレベルロジックのリスポーンで発射間隔の残りを引き継ぎ、`StartDelay` とは長い方を待つ。通常のシミュ停止・開始では引き継がない。
- `Weapon.FireCooldownRemaining`(次の発射を受け付けるまでの残り [s])を追加。
- `Projectile/AerodynamicStability`(`ProjectileSpec.AerodynamicStability` [1/m]、既定0)を追加。飛行中に機首を飛ぶ向きへ寄せ(約 1/値 m で向き直る)、曲射した弾の機首が放物線に沿う。速いほど強く、遅い弾にはほとんど効かない。

## [0.2.1] - 2026-10-19

### 武装

- `Booster/Lift`(`BoosterSpec.Lift`、既定0)を追加。機首に垂直な速度成分を打ち消して飛ぶ向きを機首へ寄せ、推力が切れた後も誘導で曲がれる。
- `Projectile/PenetrationMode`(`ProjectileSpec.PenetrationMode`)を追加。`FirstBlock` で最初に当たったブロックの厚みだけ貫通する。既定の `Total` は従来どおり合計で数える。
- 貫通: 速い弾で接触が装甲の奥で報告されると、命中した装甲が数えられず、止まる位置がずれたり貫通しなかったりしたのを直した。

## [0.2.0] - 2026-10-19

### 武装

- `EwlWeapon` に `<FireMode>` を追加。`SemiAuto` で、キーを押すたびに1回(バースト1回)だけ撃つ。既定の `Auto` は従来どおり。
- `Projectile` に `<Penetration>`(C#は `ProjectileSpec.Penetration`)を追加。装甲を進む向きの厚みで上限まで貫通し、止まった所で命中・起爆する。既定の0は貫通しない。

### 爆発

- `Projectile/Explosion/ForwardOffset`(C#: `ExplosionSpec.ForwardOffset`)を追加。爆発地点を弾の前方へ指定距離 [m] ずらす(HEAT 等)。既定 0 は従来どおり。

### 誘導

- **[Breaking]** 誘導の目標は自動で選ばず、C#の `Weapon.Target` で指定する。旧: 発射の瞬間に、発射方向の前方30度以内で最も近い敵のコアを自動で選んでいた。
  新: `Weapon.Target` に入れた目標を、`TryFire` が発射を受け付けた時点で確定する。XMLだけの武装は目標を持たず、無誘導で飛ぶ。
  例: `_weapon.Target = _weapon.FindTarget(camera.transform.position, camera.transform.forward, 6f);`
- **[Breaking]** 誘導中の弾に掛かっていた隠れた減速(物理フレームごとに速度の約0.9%)を廃止した。誘導する弾が速くなるので、旧と同じ速さにするには `Projectile/Drag` を足す
  (旧と同等は約 `0.9`)か、`Projectile/Booster/MaxSpeed` で上限を決める。
- 追加: `Weapon.FindTarget`(照準の中心に最も近い敵のコアを探す)、`GuidanceSpec.ReleaseAngle` / XML `Projectile/Guidance/ReleaseAngle`
  (機首と目標方向の角がこれを超えたら誘導を解除する。既定0は解除しない)。
- 変更: 目標を失った・解除された弾は、その時点の向きを保って直進する(発射方向へ戻らない。別の目標を追い直さない)。
- 変更: 操舵は、目標が弾の到達までに動く先(迎撃点。重力を受ける弾は落ちる分だけ上)を狙い、機首を速度の遅れの分だけ先へ向ける。
  山なりに撃ったときや動く目標で、前後・横に外れにくくなった(同じ `Gain` でも曲がり方が変わる)。
- 追加: `GuidanceSpec.PredictionIterations` / XML `Projectile/Guidance/PredictionIterations`(迎撃点の予測の反復回数。既定3、0は目標の今の位置を狙う)。
- 文書: 誘導の挙動を [誘導ミサイル](docs/guide/guidance.md) にまとめた。誘導は「仮実装」ではなくなったが、1.0までは挙動・係数を変えることがある。
- 追加: テスト用のコンソールコマンド `ewl_dummy`(そのピアでだけ敵として扱う標的を置く・消す)。

### 推進

- 追加: `BoosterSpec.MaxSpeed` / XML `Projectile/Booster/MaxSpeed`(推力で加速する上限の速さ [m/s]。既定0は上限なし)。

## [0.1.0] - 2026-10-17

最初の公開版(α版)です。この版でできることをまとめます。

### XMLカスタムモジュール

- `EwlWeapon`: ブロックを武装にする。発射キー、連射・切替式、発射間隔・遅延・拡散・反動、バースト、総弾数・弾倉・リロード、
  弾の物理・コライダー・見た目、ダメージ・衝撃力・命中物にくっつく弾、時限信管・起爆遅延、爆発、推進(ロケット)・安定翼(風見トルク)、
  発射・弾・爆発のエフェクトと音、待機弾の表示(ビルド中も)。発射レート・遅延・時限信管・弾倉容量・Power(初速)・ブースターの推力・燃焼時間はスライダーで変えられる。1ブロックに複数書ける。
- `EwlBlockPhysics`: ブロックの接合強度・物理マテリアル・接合の回転軸を設定し、コライダー(ブロックを置ける面を含む)を足す。

### C# API(`EWL.Api`)

- `WeaponSpec` / `ProjectileSpec` / `ExplosionSpec` / `BoosterSpec`: 武装・弾・爆発・推進の定義(純データ)。
- `Weapon`: 自分のモジュールやブロックスクリプトから武装を作って駆動する部品(`Create` / `TryFire` / `TryReload` / `Destroy`)。
  状態(`Ammo` / `ReserveAmmo` / `IsReloading`)とイベント(`Fired` / `ReloadStarted` / `Hit`)を持ち、`GetAll` でブロック上の武装を列挙できる。
  `CreateLoadedProjectilePreview` でビルド中のブロックに待機弾の見た目を置ける。
- `Weapon.AnyHit` / `ProjectileHit` / `Attacker`: 誰の弾がどこに当たったか、バニラのボムを誰が起爆させたか。
  `AnyHit` はBesiege標準の大砲(「爆発する砲弾」の爆風を含む)・クロスボウ・FuelCannonの弾も通知し、`ProjectileHit.Source`(`HitSource`)で武器の種類が分かる。
- `BundleAssets`: AssetBundleからアセットを取り出して覚える。

### マルチプレイ

- EWLのモジュール・`Weapon` で作った武装は、利用者が何もしなくても同期される(弾はホストが処理し、クライアントには見た目を出す)。

### 読み込み

- EWLはタイトル画面では読み込まれない(無効にしても再起動は不要)。EWLを使うModは `Mod.xml` の `LoadOrder` を1以上にする。

### 既知の制限

- 誘導(`ProjectileSpec.Guidance` / XML `Projectile/Guidance`)は仮実装で、今後まとめて作り直す予定です(破壊的変更になります)。
- 途中参加したプレイヤーには、参加前に撃たれた弾は見えません(次の弾から見えます)。

[Unreleased]: https://github.com/EEX-bsg/EsWeaponsLib/compare/0.3.0...HEAD
[0.3.0]: https://github.com/EEX-bsg/EsWeaponsLib/releases/tag/0.3.0
[0.2.1]: https://github.com/EEX-bsg/EsWeaponsLib/releases/tag/0.2.1
[0.2.0]: https://github.com/EEX-bsg/EsWeaponsLib/releases/tag/0.2.0
[0.1.0]: https://github.com/EEX-bsg/EsWeaponsLib/releases/tag/0.1.0
