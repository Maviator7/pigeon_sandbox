# Pigeon friendship implementation plan

**Goal:** 承認済みの仲良し関係を既存ペア行動と図鑑に追加する。
**Architecture:** Unity非依存 TownSocial が関係とペア行動を管理。TownSimulation は保存連携。SandboxApp は既存の高さ計測式ラベルを再利用する。
**Tech Stack:** C#, Unity 6000.3, IMGUI, 既存verify.py。

- [x] Tests/SocialChecks.cs：近くにいるだけでは関係が育たない、休憩で育つ、改名追従、再会ダンス、一緒の水浴び・ベンチ、保存互換性と不正データをテスト。まず未実装で失敗を確認。
- [x] Core/TownSocial.cs：共有時間とBestFriend、再会状態、親しい相手優先、噴水・ベンチの到着位置を追加。
- [x] Core/TownSimulation.cs：TownSaveへ関係リスト追加。Capture/Restoreで検証とコピー。
- [x] Runtime/SandboxApp.cs：住民カードに改行対応の仲間名を追加。Runtime/TownWorld.cs：社会的な休憩の高さを飛行と解釈しない。
- [x] Tests/verify.py実行、既存回帰と新規テストを確認。READMEを更新。
- [x] Macビルドと保存した街での再起動確認（成功）。自動生成のみのPark/GraphicsSettings差分を除外。公開版・Gitは自動更新しない。

検証結果：verify.py 150 PASS、Unity JsonUtilityの新旧セーブ検証成功、Macビルド成功。設計・実装レビュー済み。
