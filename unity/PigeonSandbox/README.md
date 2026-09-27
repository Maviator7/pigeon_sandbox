# 鳩市長の街づくり — Unityプロジェクト

Unity **6000.3.11f1**で開発するMac向けの箱庭ゲームです。人と鳩が暮らす駅前広場から街を育て、個性的な鳩たちの生活を観察します。ゲーム全体の紹介は[リポジトリのREADME](../../README.md)にあります。

## 開発・ビルド

1. Unity Hubでこのフォルダを開き、`Assets/Scenes/Park.unity` を再生します。
2. シーンを再生成する場合はEditorメニュー **Pigeon Sandbox > Create observation scene** を使います。
3. **Pigeon Sandbox > Build Mac app** で `Builds/Mac/Pigeon Sandbox.app` を生成します。`Builds` はGitの対象外です。

ビルドは `Park.unity` がないときだけシーンを生成し、Player設定も値が変わるときだけ書き込みます。ビルドしてもシーンや `ProjectSettings` に差分は出ません。

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

リポジトリのルートで実行します。Unity付属のC#コンパイラで `Assets` 以下のC#（Runtime・Editor）をすべてコンパイルし、`Tests/` の描画に依存しないテストを実行します。`Tests/` に追加したファイルは自動で対象になり、エントリは `TownChecks.Main` です。Editorメニューの **Verify simulation**（Macビルド時にも実行）と **Verify camera controls** も利用できます。

C#の書式は、Unity同梱のRoslynを使う整形ツールで揃えます。

```sh
python3 unity/PigeonSandbox/Tools/format.py          # 整形
python3 unity/PigeonSandbox/Tools/format.py --check  # 確認のみ
```

`Assets/PigeonSandbox/Core` は描画から独立した街・鳩の行動と保存、`Runtime` は3Dモデル・マップ・入力・UI、`Editor` はビルドと検証です。現在の配布対象はmacOS 12以降（Apple Silicon・Intel）。スマートフォン版のビルド、縦画面、実機操作・性能の検証はまだ行っていません。

## パフォーマンス計測

目標は、iPhone 15相当で最大マップ（17×17マスすべてに施設、鳩15羽）を60fpsで動かすことです。

- **計測オーバーレイ：** プレイ中にF3で、FPS・フレーム時間・描画呼び出し回数・GC確保量を左下に表示します。描画とGCの値はDevelopment Buildでのみ取得でき、通常ビルドでは `n/a` になります。
- **ベンチマーク：** `Assets/Scenes/Benchmark.unity` は、施設で埋まった最大マップを毎回同じ配置で作ります（`TownSimulation.CreateBenchmark`）。セーブデータの読み書きはしません。`-benchmark-no-ui` を付けると画面（IMGUI）を描かずに計測します。5秒の準備のあと20秒間計測し、3秒ごとに条例を切り替えて施設の建て直しも計測に含めます。フレームレートの上限は外して計測します。
- **実行方法：** **Pigeon Sandbox > Build Mac benchmark** でDevelopment Buildを作り、次のコマンドで実行します。終わるとアプリは自動で終了し、`~/Library/Logs/PigeonSandbox/Pigeon Sandbox/Player.log` に `PIGEON BENCHMARK` 行が残ります。

```sh
open -W -n "unity/PigeonSandbox/Builds/MacBenchmark/Pigeon Sandbox Benchmark.app" --args -benchmark-quit
grep "PIGEON BENCHMARK" ~/Library/Logs/PigeonSandbox/Pigeon\ Sandbox/Player.log
```

### iOS実機での計測

1. XcodeにApple IDを登録し、iPhoneを接続してデベロッパモードを有効にします（初回のみ）。
2. **Pigeon Sandbox > Build iOS benchmark (Xcode project)**（バッチは `BuildMac.BuildIOSBenchmark`）で `Builds/iOSBenchmark` にXcodeプロジェクトを作ります。環境変数 `PIGEON_APPLE_TEAM`（Team ID）を設定しておくと、そのビルドだけに署名チームを適用します（`ProjectSettings` には保存しません）。バンドルIDは `PIGEON_IOS_BUNDLE_ID` で変えられます。
3. Unityは既定でiPhoneを60Hzに制限する設定（`CADisableMinimumFrameDurationOnPhone=false`）を書き込みます。ベンチマーク用のビルドだけはこれを外し、余裕がどれだけあるかを測れるようにしています。ゲーム本体は60fpsのままです。
4. 署名・インストール・実行はコマンドでもできます。初回は、iPhoneの 設定 → 一般 → VPNとデバイス管理 で開発元を信頼します。

```sh
cd unity/PigeonSandbox/Builds/iOSBenchmark
xcodebuild -project Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Release \
  -destination "id=<デバイスのUDID>" -derivedDataPath /tmp/pigeon-ios -allowProvisioningUpdates build
xcrun devicectl device install app --device <UDID> /tmp/pigeon-ios/Build/Products/Release-iphoneos/PigeonSandbox.app
xcrun devicectl device process launch --device <UDID> --console --terminate-existing com.PigeonSandbox.Pigeon-Sandbox
```

約25秒で、画面左下とコンソールに `PIGEON BENCHMARK` 行が出ます。`cpuMainMs`・`cpuRenderMs`・`gpuMs` は `FrameTimingManager` による1フレームあたりの処理時間です。画面の更新上限（60Hz）に張り付いているときは、`cpuRenderMs` に更新待ちの時間が含まれます。余裕を判断するときは、上限を外した状態のフレーム時間を見てください。

### 記録

| 日付 | 変更 | 端末 | 平均FPS | フレーム時間 平均/p99/最大 (ms) | 描画呼び出し | GC確保 平均/最大 (KB/フレーム) |
| --- | --- | --- | --- | --- | --- | --- |
| 2026-09-27 | 最適化前（v0.3.0） | Mac（M4 Max） | 104〜111 | 9.0〜9.6 / 18.0 / 44〜51 | 約2,890 | 131 / 425 |
| 2026-09-27 | 施設の差分更新・共有マテリアル・Coreの確保削減 | Mac（M4 Max） | 116〜118 | 8.5〜8.6 / 17.0 / 22 | 約2,880 | 70 / 151 |
| 2026-09-27 | 同上、UIなし（`-benchmark-no-ui`） | Mac（M4 Max） | 84〜85 | 11.7〜11.9 / 20〜21 / 29〜31 | 約2,710 | 4.1 / 58 |
| 2026-09-27 | メッシュの焼き込み・ズーム連動の影 | Mac（M4 Max） | 194〜196 | 5.1〜5.2 / 14.9〜15.0 / 22〜23 | 1,340 | 70 / 151 |
| 2026-09-27 | 同上 | iPhone 17（A19）、60Hz上限あり | 60.0 | 16.67 / 17.2〜17.7 / 20〜22 | 約970 | 78 / 159 |
| 2026-09-27 | 同上 | iPhone 17（A19）、120Hz（上限なし） | 119.8〜119.9 | 8.34〜8.35 / 8.7 / 13.5〜16.7 | 約970 | 78 / 173 |

施設・地面・来訪者は1つずつ、鳩は動く部位ごとに、プリミティブを1枚のメッシュへ焼き込みます（`MeshBaker`）。色・つや・金属感は頂点に持たせ、共有シェーダー `PigeonSandbox/Vertex Color Lit` で描きます。最大マップのレンダラー数は約4,000から385になりました。影を描く距離はズームに合わせて変わり（全体表示では従来の70と同じ）、スマートフォンではハードシャドウ・1カスケード・中解像度にします。

iPhone 17では、120Hzでも1フレーム約8.3msに収まり、メインスレッドのCPU時間は約2.8ms、GPUは約5.5msでした。60fpsの予算（16.7ms）に対して約2倍の余裕があります。iPhone 15（A16）では実機で測っていません。CPU・GPUが約1.5倍遅いと仮定すると1フレーム約12.5msで、60fpsに収まる見込みです。施設を3×3マスごとに結合して描画呼び出しを半分（約970→465回）にする案も試しましたが、iPhone 17での結果は変わらず、MacのFPSは下がったため採用していません。

UIなしの計測は、GC確保のうち画面（IMGUI）以外が占める分を切り分けるためのものです。フレーム時間は実行ごとの揺れが大きいため、UIのありなしでは比べません。残りのGC確保（約66KB/フレーム）はIMGUIの画面によるもので、UIの作り直しで解消します。

## リリース

バージョンを上げるときは `Assets/PigeonSandbox/Editor/BuildMac.cs` の `ReleaseVersion` と `ProjectSettings/ProjectSettings.asset` の `bundleVersion` を一致させます。テストとMacビルド後、アプリ・日本語の案内・Noto Sans JPのライセンスをZIPに入れ、対応するコミットにタグを付けてGitHub Releasesへ公開します。公開済みZIPは差し替えず、新しいバージョンとして配布します。
