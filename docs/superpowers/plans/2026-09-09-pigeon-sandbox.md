# Pigeon Sandbox Implementation Plan

Goal: 観察、餌、クリック、持ち上げ、飛行着地の体験を成立させる。
Architecture: 描画非依存の個体Runtime + Brain、R3Fによる投影、ZustandのUIスナップショット。写真は形態の参考に使用し、外部送信しない。
Tech Stack: React 19, TypeScript, Vite 6 (ローカルNode 18対応), Three.js, R3F 9, drei 10, Zustand 5, Vitest 3。

## フェーズと確認

- [x] 1: package.json, main.tsx, App.tsxを作成。npm install / npm run build。
- [x] 2: Scene/Environmentに地面、小道、木、ベンチ。ビルド。
- [x] 3: Pigeon/ProceduralPigeonに分離パーツ、翼帯、首の光沢。ビルド。
- [x] 4: simulation/controllerとパーツ歩行。速度・境界のテスト→実装→テスト。
- [x] 5: simulation/brain。恐怖優先・餌優先・確率・needs範囲テスト→実装→テスト。
- [x] 6: foodStore/Food。発見→歩行→摂食→消滅の統合テスト→実装→テスト。
- [x] 7: クリック/ドラッグ/Call/Reset。ポインタキャプチャ、キャンセル時復帰。ブラウザ操作確認。
- [x] 8: FLY/LAND。公園内に着地、高低リリース、疲労回復のテスト→実装→テスト。
- [x] 9: ControlPanel/ObservationPanel/CameraController。5Hz更新・追尾解除・表示確率一致。
- [x] 10: GLBローダー、SoundManager、README/モデルガイド。npm test / npm run build / ブラウザ表示と操作、狭い画面確認。

## 設計詳細

状態: IDLE LOOK_AROUND LOOK_AT_FOOD WALK RUN PECK EAT PANIC FLY LAND + HELD。
餌の検知距離は7m、接近は0.4m。食べたイベントでfoodStoreから削除する。
飛行は上昇、旋回、着陸地点接近、降下。地面は半径7mの円内。木・ベンチを障害物として回避。
左右の足、翼、首、頭を独立させ、頭のhold/thrustを鋸歯状の位相で近似。
GLBをfetch/parseし、存在しない・壊れたモデルは仮モデルに戻す。スケールを高さで正規化。名前付きパーツの手続きアニメーション、名前がないときも全身の呼吸・傾きフォールバック。
AI観察パネルは実際に使う確率と優先イベントを明記し、履歴を表示。

## 検証記録

2026-09-10: docs/verification.md に単体テスト・ビルド・実ブラウザ検証を記録。
