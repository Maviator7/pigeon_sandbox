# Pigeon Sandbox — Unity / Mac

Web版と並行して開発するMac向け初版。Unity 6000.3.11f1を使用。

## 開く

1. Unity Hubにこのディレクトリをプロジェクトとして追加。
2. Editorライセンスを有効にして開く。
3. 初回に `Assets/Scenes/Park.unity` が自動生成される。Playで開始。
4. シーンを再生成する場合は `Pigeon Sandbox > Create observation scene`。

## Macアプリ生成

`Pigeon Sandbox > Build Mac app` でAI検証後にビルドする。
出力は `Builds/Mac/Pigeon Sandbox.app`。BuildsはGit対象外。

```sh
/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -projectPath "$PWD/unity/PigeonSandbox" \
  -executeMethod PigeonSandbox.Editor.BuildMac.Build \
  -logFile /tmp/pigeon-unity-build.log
```

上のコマンドはリポジトリルートから実行する。

## 操作

- Seedsを選び、地面をクリックして餌を置く。最大60粒。
- Callを選び、地面をクリックすると鳩がその場所まで歩く。
- Flyで飛行、Restで休憩。飛行中の呼びかけは待機する。
- 右ドラッグでカメラ回転、スクロールでズーム。
- Pause / Spaceで時間停止、Resetで初期化、Escapeでアプリ終了。
- タッチ入力の入口も実装済み。1本指タップで餌・呼びかけ、2本指で回転・ズーム。

## 初版の内容と制限

鳩1羽、公園、滑らかな仮モデル、歩行・摂食・飛行・着地・休憩、空腹・体力・人への慣れ、観察UIを実装。外部モデル・音素材は不要。UIは現在英語。水場は景観のみで、水浴びAI・図鑑・複数羽・保存は今後の拡張。

Unity非依存の `Core` を共通化し、将来のiOS/Android移植に備える。スマホ版のビルド、縦画面レイアウト、実機操作・性能検証は未実施。小画面向けUIは対応時に再調整する。

## 検証

`python3 unity/PigeonSandbox/Tests/verify.py` をリポジトリルートから実行。
Unity付属C#コンパイラで全コードのAPI参照を検証し、純粋AIの餌・呼びかけ・着地・境界・乱数再現性・容量制限をテストする。

2026-09-15: C#コンパイルとAIテスト成功。ライセンス有効化後、Unity内の行動検証とMacビルドも成功。実アプリで鳩・公園・UIの描画、飛行・着地、餌表示と摂食回数3・慣れ53への変化を確認。実行ログに例外なし。スマホ実機検証は未実施。
