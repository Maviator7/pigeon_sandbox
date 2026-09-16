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

- 左の施設を選び、街の空きマスをクリックして建設。施設には人間向けと鳩向けの両方の価値があります。
- 施設をクリックすると詳細を表示。移設は無料、改良はレベル3まで、撤去は投資額の70%を返金します。
- 「お願い」で住民の目標、「鳩図鑑」で名前・性格・行動、「条例」で街の方針を確認。
- 右ドラッグでカメラ回転、スクロールでズーム。Pause / Spaceで時間停止、速度で進行を早めます。
- 「街を保存」で保存。終了時にも自動保存し、Resetは現在未提供です（保存ファイルを削除すると初期状態に戻ります）。
- タッチ入力の入口も実装済み。1本指タップで建設・選択、2本指で回転・ズーム。

## 初版の内容と制限

駅前広場、6施設、4種類の性格を持つ名前つき鳩、来訪者の購買、街の収支、食料供給、鳩の幸福度、人間の満足度、混雑、清潔さ、お願い3件、条例3件、条件を満たした白い鳩の来訪、歩行・食事・水浴び・休憩・観光を実装。外部モデル・音素材は不要。鳩を追い払う、捕食、攻撃、負傷、死亡などの危害イベントはありません。

Unity非依存の `Core` を共通化し、将来のiOS/Android移植に備える。スマホ版のビルド、縦画面レイアウト、実機操作・性能検証は未実施。小画面向けUIは対応時に再調整する。

## 検証

`python3 unity/PigeonSandbox/Tests/verify.py` をリポジトリルートから実行。
Unity付属C#コンパイラでランタイムAPIを検証し、純粋AIの餌・呼びかけ・着地・境界・乱数再現性・容量制限、および街の建設・経済・性格ごとの目的地・お願い・希少個体・保存復元をテストする。

2026-09-15: C#コンパイルとAIテスト成功。ライセンス有効化後、Unity内の行動検証とMacビルドも成功。実アプリで鳩・公園・UIの描画、飛行・着地、餌表示と摂食回数3・慣れ53への変化を確認。実行ログに例外なし。スマホ実機検証は未実施。
