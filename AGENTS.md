# Pigeon Sandbox

鳩と人が暮らす街をつくる、Unity製の箱庭ゲーム「鳩市長の街づくり」。現在の開発対象はUnity版（macOS。iOSは準備中）。

## コマンド

- Unity版: `unity/PigeonSandbox` をUnity 6000.3.11f1で開く。`Assets/Scenes/Park.unity` がなければ初回に生成。
- C#検証: `python3 unity/PigeonSandbox/Tests/verify.py`（Unityライセンス不要）。Runtime・EditorのC#をすべてコンパイルし、`Tests/*.cs` を実行。
- C#整形: `python3 unity/PigeonSandbox/Tools/format.py`（`--check` で確認のみ）。Unity同梱のRoslynを使う。
- Macビルド: Editorメニュー `Pigeon Sandbox > Build Mac app`。バッチ実行は `-executeMethod PigeonSandbox.Editor.BuildMac.Build`。
- ベンチマーク: `Pigeon Sandbox > Build Mac benchmark`（バッチは `BuildMac.BuildBenchmark`）で `Builds/MacBenchmark/` にDevelopment Buildを作る。`open -W -n "unity/PigeonSandbox/Builds/MacBenchmark/Pigeon Sandbox Benchmark.app" --args -benchmark-quit` で実行し、`~/Library/Logs/PigeonSandbox/Pigeon Sandbox/Player.log` の `PIGEON BENCHMARK` 行を読む。`-benchmark-waterfront` を加えると水辺地区を含む338施設、付けなければ駅前のみの289施設を計測する。`-benchmark-no-ui` は画面なしの計測（zshでは引数を1つずつ書く）。
- iOSベンチマーク: `Pigeon Sandbox > Build iOS benchmark (Xcode project)`（バッチは `BuildMac.BuildIOSBenchmark`）で `Builds/iOSBenchmark` にXcodeプロジェクトを作り、実機で実行する。`PIGEON_APPLE_TEAM`（そのビルドだけに適用し、`ProjectSettings` には保存しない）・`PIGEON_IOS_BUNDLE_ID` で署名チームとバンドルIDを指定できる。ベンチマーク用のビルドは60Hzの上限を外す。署名・インストール・実行の手順はUnity版READMEを参照。
- UIフォント: 同梱フォントはサブセット。UIに文字列を追加したら `python3 unity/PigeonSandbox/Tools/subset_font.py`（fontTools）で再生成する。
- 旧Web試作（参考資料・保守対象外）: `npm install`、`npm run dev`、`npm run build`、`npm test`。

## 構成と規約

- `unity/PigeonSandbox/Assets/PigeonSandbox/Core`: Unity非依存のC#。街・水辺地区・鳩・来訪者の行動、お願い、お祭り、保存、ベンチマーク用の街。乱数は注入する。
- `unity/PigeonSandbox/Assets/PigeonSandbox/Runtime`: 手続き生成の3Dモデル（プリミティブを `MeshBaker` で1メッシュに焼き込み、`Shaders/VertexColorLit.shader` で描く）、マップ、入力、IMGUIの画面、計測オーバーレイ（F3）とベンチマーク実行。
- `unity/PigeonSandbox/Assets/PigeonSandbox/Editor`: ビルド、シーン生成、Unity内での検証。
- `unity/PigeonSandbox/Tests`: Coreのテスト。新しいファイルは `verify.py` が自動で拾う。エントリは `TownChecks.Main`。
- `unity/PigeonSandbox/Tools`: 開発用スクリプト（Unityのビルド対象外）。
- 旧Web試作（`src/`、`tests/`、`public/`、`docs/model-guide.md`、`docs/verification.md`）は参考資料として残し、変更しない。
- 鳩の行動は `BirdActivity` で判定し、表示用の文字列（`Action`）と比較しない。保存形式を変えたら `TownSimulation.CurrentSaveVersion` を上げる。
- C#は `Tools/format.py` の書式に揃える。Coreの変更にはテストを添える。
- 性能目標: iPhone 15相当、最大マップ（17×17・鳩15羽）で60fps。性能に関わる変更はベンチマークの前後比較を残す。
- ビルドでシーンや `ProjectSettings` に差分を出さない（シーンは存在しないときだけ生成）。
- モデル・音素材がなくても動作を維持する。
- 鳩を観察し、個体差や行動を楽しむ平和なゲーム。鳩に危害を加えるイベントは実装しない。
- 個人プロジェクト。エージェント定義はホーム側を使用する。
- コマンドや構成が変わったらこのファイルを更新する（`CLAUDE.md` はこのファイルへのシンボリックリンク）。
