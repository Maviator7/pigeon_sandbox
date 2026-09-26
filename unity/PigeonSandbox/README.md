# 鳩市長の街づくり — Unityプロジェクト

Unity **6000.3.11f1**で開発するMac向けの箱庭ゲームです。人と鳩が暮らす駅前広場から街を育て、個性的な鳩たちの生活を観察します。ゲーム全体の紹介は[リポジトリのREADME](../../README.md)にあります。

## 開発・ビルド

1. Unity Hubでこのフォルダを開き、`Assets/Scenes/Park.unity` を再生します。
2. シーンを再生成する場合はEditorメニュー **Pigeon Sandbox > Create observation scene** を使います。
3. **Pigeon Sandbox > Build Mac app** で `Builds/Mac/Pigeon Sandbox.app` を生成します。`Builds` はGitの対象外です。

リポジトリのルートからコマンドでもビルドできます。

```sh
/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -projectPath "$PWD/unity/PigeonSandbox" \
  -executeMethod PigeonSandbox.Editor.BuildMac.Build \
  -logFile /tmp/pigeon-unity-build.log
```

## ゲームの内容

- **街づくり：** パン屋、噴水、集合住宅、街路樹、広場、時計台、オープンカフェ、花壇の公園を建設・改良・移設・撤去します。土地は予算で9×9から最大17×17マスまで拡張できます。
- **街の運営：** 予算、食料供給、鳩の幸福度、人の満足度、混雑、清潔さを見ながら配置を考えます。来訪者は買い物をして街に収入をもたらします。条例と街の住民からのお願いもあります。
- **鳩の観察：** 鳩には名前・性格・羽色があり、図鑑からフォーカスや改名ができます。食事・水浴び・羽繕い・日向ぼっこ・ごきげんクルクルなどの行動をします。
- **出会いと交流：** 青灰、ごま模様、茶色、白黒まだら、白い鳩がいます。街の条件を整えると新しい羽色が訪れます。鳩同士は散歩・休憩・水浴びを共にして仲良しになり、再会のクルクルを見せることがあります。
- **一日の変化：** 120秒で一日が進み、朝・昼・夕方の光と行動が変わります。鳩本人から、配置と実際の利用で叶う小さなお願いも届きます。期限・罰・金銭報酬はありません。
- **街の催し：** 3日ごとにパン屋の朝市・水辺の午後・時計台の夕暮れを開催できます。会場となる施設を近くに置き、鳩と人の行動を見守ると、参加した鳩の名前入り絵はがきが残ります。催しに期限や失敗のペナルティはなく、準備からやり直せます。

鳩を追い払う、捕食、攻撃、負傷、死亡などの危害イベントは実装しません。外部の3Dモデルや音素材がなくても動作します。

## 操作と保存

左クリックで建設・選択、左ドラッグでマップ移動、右ドラッグでカメラ回転、ホイールまたは上端の＋／−でズームします。「街全体」で視点を戻せます。「鳩図鑑」で名前を押すと追従し、名前の横の「改名」で1〜24文字の名前に変更できます。Spaceで一時停止・再開し、画面上部で速度を変更できます。

「街を保存」から手動保存できます。30秒ごとと終了時にも自動保存します。セーブファイルは `~/Library/Application Support/PigeonSandbox/Pigeon Sandbox/mayor-town.json` です。旧バージョンの街は読み込み時に新しい羽色図鑑・仲良し関係・鳩のお願い・催しへ移行します。催しの進行と絵はがきは保存されます。進行中の散歩などの一時的な動作は再開時に初期化します。

## 検証

```sh
python3 unity/PigeonSandbox/Tests/verify.py
```

リポジトリのルートで実行すると、Unity付属のC#コンパイラでランタイムを確認し、描画に依存しないシミュレーションのテストを実行します。Editorメニューの **Verify simulation** と **Verify camera controls** も利用できます。Macビルドの生成時にはシミュレーション検証を実行します。

`Assets/PigeonSandbox/Core` は描画から独立した街・鳩の行動と保存、`Runtime` は3Dモデル・マップ・入力・UI、`Editor` はビルドと検証です。現在の配布対象はmacOS 12以降（Apple Silicon・Intel）。スマートフォン版のビルド、縦画面、実機操作・性能の検証はまだ行っていません。

## リリース

バージョンを上げるときは `Assets/PigeonSandbox/Editor/BuildMac.cs` の `ReleaseVersion` と `ProjectSettings/ProjectSettings.asset` の `bundleVersion` を一致させます。テストとMacビルド後、アプリ・日本語の案内・Noto Sans JPのライセンスをZIPに入れ、対応するコミットにタグを付けてGitHub Releasesへ公開します。公開済みZIPは差し替えず、新しいバージョンとして配布します。
