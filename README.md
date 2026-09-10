# Pigeon Sandbox

小さな公園で、気ままな一羽の鳩と過ごす3Dシミュレーター。
自律歩行、首振り、キョロキョロ、餌探し、ついばみ、突然のダッシュ、持ち上げ、飛行・着地を実装しています。写真の体形・翼帯・首の色を参考にした**仮の手続きモデル**です。写真から3Dモデルを生成したものではありません。

## 起動

Node.js 22以降を推奨（`.nvmrc` 同梱）。npmを使用します。

```bash
npm install
npm run dev
```

表示されたローカルURLを開きます。既定は http://127.0.0.1:5173 。サーバーはローカルホストだけに公開します。

```bash
npm run build    # TypeScript strictチェック + 本番ビルド
npm run preview  # dist の確認
npm test         # 描画非依存AI・移動・ストアのテスト
npm run test:watch
```

Vite 6を使っているためアプリの起動・ビルドはNode 18環境でも確認済みですが、テストと依存パッケージのエンジン要件を満たすにはNode 22以降を使用してください。

## あそびかた

| 操作                   | 動作                                                                |
| ---------------------- | ------------------------------------------------------------------- |
| Feed / F               | 餌モード。地面をクリックするとパンくずを置く。再選択またはEscで終了 |
| Call / C               | カメラ側の公園内地点へ鳩を呼ぶ。飛行・持ち上げ中は反応しない        |
| Pigeon Cam / P         | 鳩を追尾。再選択で元の視点に戻る                                    |
| Reset                  | 鳩、餌、速度、一時停止、カメラと操作指示を初期化                    |
| 鳩をクリック           | 首を傾ける・周囲を見る。観察パネルを開く                            |
| 鳩をドラッグ           | 画面上方向へ引くと持ち上がる。離すと高さに応じて着地・飛行          |
| 鳩をダブルクリック     | 追尾カメラ切り替え                                                  |
| 背景を左ドラッグ       | カメラ回転                                                          |
| ホイール / ピンチ      | ズーム                                                              |
| 右ドラッグ / 2本指移動 | カメラを平行移動                                                    |
| Space / 再生ボタン     | 一時停止・再開                                                      |
| 速度ボタン             | 1× → 2× → 0.5×                                                      |

餌は最大40か所。公園外、木・ベンチの内部には置けません。空腹が少ないとすぐに餌を食べないことがあります。検知範囲外の餌は散歩で近づいてから発見します。
一時停止中は時間が進まず、持ち上げて離した後の着地も再生時に進みます。ブラウザを閉じると状態は保存されません。

## 技術構成

React 19 / TypeScript / Vite 6 / Three.js / React Three Fiber 9 / drei 10 / Zustand 5 / Vitest。
カメラはOrbitControls。飛行・着地は補間制御で、物理エンジンは使っていません。フォントはDM SansとNoto Sans JPをローカル配信します。実行中に写真や操作情報を外部送信する処理はありません。

## フォルダ構成

```text
src/
  components/
    Scene.tsx                  Canvas、照明、公園の組み立て
    Environment/               Ground、Tree、Bench
    Pigeon/
      Pigeon.tsx               シミュレーションの更新とポインタ操作
      PigeonModel.tsx          GLB読込・正規化・AnimationMixer
      ProceduralPigeon.tsx     仮の鳩モデル（独立パーツ）
      PigeonAnimation.ts      頭のhold/thrust、歩行、翼、脚のアニメーション
    Food/FoodManager.tsx      餌の描画
    Camera/CameraController.tsx
    ui/                       操作、観察、再生、ヘルプ
  simulation/
    brain.ts                  確率・状態判断・内部パラメータ
    controller.ts             移動、状態遷移、飛行・摂食・入力
    park.ts                   境界、障害物、到達可能な地点
  stores/                     操作・観察スナップショットと餌
  types/pigeon.ts             個体、状態、座標、餌の型
  audio/SoundManager.ts       後から音を登録するための管理クラス
public/models/                pigeon.glb の配置先
public/audio/                 音源の配置先
tests/                        AI・移動・餌・Resetの回帰テスト
docs/model-guide.md           モデル差し替えガイド
```

## AIの設計

`decideNextAction(context)` は状態・needs・餌・ユーザー距離・注入した乱数から次の行動を返します。ReactやThree.jsへの依存はありません。

- `PANIC`、疲労回復、餌、呼び出しを自由行動より優先。
- `LOOK_AT_FOOD → WALK → EAT → IDLE`。摂食イベントを受けて餌を削除し、hungerを減らしtrustを増やします。
- `FLY → LAND → IDLE`。着地点は公園内の障害物を避けた地点に制限。
- 自由行動の確率は `probabilities()` をUIとAIで共有します。UIの確率は、餌や警戒による割り込みを含む予測ではありません。
- `advance()` は個体のRuntimeを更新し、食べた餌・音イベントを返します。乱数を固定すると単体テストで再現できます。
- クリックで警戒度が少し上がり、持ち上げ中は大きく上がります。画面ポインタの接近を現実の人間の接近として扱う処理はありません。

## モデル・アニメーション差し替え

`public/models/pigeon.glb` を置いて再読み込みするだけで読み込みを試みます。未配置、不正データ、読込失敗時は仮モデルで動きます。高さは自動で1.67単位へ正規化し、足元を地面へ合わせます。

対応クリップ名：`Idle`, `Walk`, `Run`, `Peck`, `Eat`, `Fly`, `Land`, `LookAround`（大文字小文字を区別しません）。対応クリップがあれば優先します。ない場合は名前付きパーツ・ボーンをコードで動かします。詳細と調整場所は [モデル差し替えガイド](docs/model-guide.md) を参照してください。

## 音

現時点では無音です。音ボタンはSoundManagerの有効状態を切り替えます。
`SoundManager.register('coo', '/audio/coo.mp3')` のように登録できます。`coo`, `flap`, `peck`, `step` を想定しています。足音の歩行周期や羽ばたき周期など、詳細な音タイミングは素材追加時にイベントを拡張してください。

## パフォーマンスと拡張

毎フレームの位置・姿勢をRuntimeとThreeオブジェクトに保持し、ReactのStateを更新しません。UIは5Hz。草はInstancedMesh、DPR上限は1.75、影は2048px。60fpsを目標とした設計ですが、全端末での60fpsや30羽での性能を保証するものではありません。

複数羽では個体ごとに `createPigeon(id)` を作り、共有する餌リストと世界情報を各 `advance()` に渡します。現在の観察ストアは1羽用なので、複数羽へ拡張する際はIDをキーにしたスナップショットと選択IDへ変更してください。群れ・Boids・性格はBrainContextや移動ベクトルを拡張し、餌争奪はワールド側で同一フレームの摂食イベントを仲裁します。GLBではSkeletonUtils.cloneにより個体ごとのスケルトンを分離します。

猫・NPC・天候・繁殖などは未実装。保存、バックエンド、実際の写真からのモデル生成も含みません。
