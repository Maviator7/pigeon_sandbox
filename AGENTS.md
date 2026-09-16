# Pigeon Sandbox

写真を参考にした仮モデルと、描画非依存AIによる3D鳩シミュレーター。

## コマンド

- npm install
- npm run dev
- npm run build
- npm test
- Unity版: `unity/PigeonSandbox` をUnity 6000.3.11f1で開く。初回にParkシーンを生成。
- Unity版ビルド: Editorメニュー `Pigeon Sandbox > Build Mac app`。
- Unity版C#検証: `python3 unity/PigeonSandbox/Tests/verify.py`（Unityライセンス不要）。

## 構成と規約

- src/simulation: Three.js/ReactをimportしないAI・移動。乱数は注入する。
- src/components: R3F描画、パーツアニメーション、UI。
- src/stores: 操作指示、餌、5Hzの観察スナップショット。
- tests: AI・一連の行動のVitestテスト。
- TypeScript strict。毎フレームのReact state更新は禁止。
- モデル・音素材がなくても動作を維持する。
- 個人プロジェクト。エージェント定義はホーム側を使用する。
- コマンドや構成が変わったらこのファイルも更新する。
- unity/PigeonSandbox/Assets/PigeonSandbox/Core: Unity非依存のC# AI。乱数は注入する。
- unity/PigeonSandbox/Assets/PigeonSandbox/Runtime: 鳩モデル、公園、入力、観察UI。
- 鳩を観察し、個体差や行動を楽しむ平和なシミュレーター。鳩に危害を加えるイベントは実装しない。
