import {
  Pause,
  Play,
  Question,
  SpeakerSlash,
  SpeakerHigh,
  Sun,
  Bird,
  Cursor,
  MouseScroll,
  X,
} from '@phosphor-icons/react';
import { useEffect, useRef } from 'react';
import { usePigeonStore } from '../../stores/pigeonStore';
import { useFoodStore } from '../../stores/foodStore';
import { soundManager } from '../../audio/SoundManager';
export function Header() {
  return (
    <header className="app-header">
      <div className="brand">
        <span className="brand-mark">
          <Bird size={31} weight="duotone" />
        </span>
        <div>
          <h1>
            Pigeon Sandbox<span>小さな公園、気ままな一羽。</span>
          </h1>
        </div>
      </div>
      <div className="weather">
        <Sun size={19} />
        <span>
          いつもの公園<small>やわらかな昼下がり</small>
        </span>
      </div>
    </header>
  );
}
export function StatusBar() {
  const paused = usePigeonStore((s) => s.paused),
    speed = usePigeonStore((s) => s.speed),
    sound = usePigeonStore((s) => s.sound),
    set = usePigeonStore((s) => s.set),
    count = useFoodStore((s) => s.foods.length),
    feeding = usePigeonStore((s) => s.feeding),
    follow = usePigeonStore((s) => s.follow),
    dragging = usePigeonStore((s) => s.dragging);
  return (
    <>
      <div className="scene-hint" aria-live="polite">
        {dragging
          ? '上へドラッグで持ち上げる · 離すと着地'
          : feeding
            ? 'パンくずを置きたい地面をクリック'
            : follow
              ? 'Pigeon Cam · 小さな動きまで、じっくりと'
              : '鳩をクリックで観察 · ドラッグで持ち上げる'}
      </div>
      <footer className="bottom-area">
        <div className="quiet-note">
          <span className="status-dot" />
          <span>
            {paused ? 'ひと休み中。' : '今日も、鳩のペースで。'}
            <small>A LITTLE PARK. A LIFE OF ITS OWN.</small>
          </span>
        </div>
        <div className="playback glass">
          <button
            className="icon-button"
            aria-label={paused ? '再生' : '一時停止'}
            onClick={() => set({ paused: !paused })}
          >
            {paused ? <Play weight="fill" size={17} /> : <Pause weight="fill" size={17} />}
          </button>
          <div className="playback-separator" />
          <button
            className="speed-button"
            aria-label="再生速度を変更"
            onClick={() => set({ speed: speed === 1 ? 2 : speed === 2 ? 0.5 : 1 })}
          >
            {speed}×
          </button>
          <span className="playback-separator" />
          <span className="crumb-count">
            パンくず <b>{count.toString().padStart(2, '0')}</b>
          </span>
          <span className="playback-separator" />
          <button
            className="icon-button"
            aria-label="音の有効・無効"
            aria-pressed={sound}
            onClick={() => {
              soundManager.enabled = !sound;
              if (sound) soundManager.stop();
              set({
                sound: !sound,
                toast: !sound ? '音を有効にしました（音素材は未登録です）' : '音をオフにしました',
              });
            }}
          >
            {sound ? <SpeakerHigh size={19} /> : <SpeakerSlash size={19} />}
          </button>
        </div>
        <button aria-label="あそびかた" className="help-button" onClick={() => set({ help: true })}>
          <Question size={21} />
          <span>あそびかた</span>
        </button>
      </footer>
    </>
  );
}
export function Toast() {
  const toast = usePigeonStore((s) => s.toast),
    set = usePigeonStore((s) => s.set);
  useEffect(() => {
    if (!toast) return;
    const timer = setTimeout(() => set({ toast: '' }), 4200);
    return () => clearTimeout(timer);
  }, [toast, set]);
  return toast ? (
    <div role="status" className="toast">
      {toast}
    </div>
  ) : null;
}
export function Help() {
  const open = usePigeonStore((s) => s.help),
    set = usePigeonStore((s) => s.set),
    ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    if (open) ref.current?.showModal();
    else ref.current?.close();
  }, [open]);
  return (
    <dialog
      ref={ref}
      className="help-dialog"
      onCancel={() => set({ help: false })}
      onClick={(e) => {
        if (e.target === e.currentTarget) set({ help: false });
      }}
    >
      <div className="help-content">
        <button
          className="icon-button dialog-close"
          aria-label="あそびかたを閉じる"
          onClick={() => set({ help: false })}
        >
          <X size={22} />
        </button>
        <Bird size={42} weight="duotone" />
        <h2>鳩のペースで、遊ぼう。</h2>
        <p>
          何もしなくても、きょろきょろ、トコトコ。
          <br />
          たまに理由もなく走ります。
        </p>
        <ul>
          <li>
            <Cursor size={20} />
            <span>
              <b>クリック</b> 鳩の気持ちを観察
            </span>
          </li>
          <li>
            <Cursor size={20} />
            <span>
              <b>鳩をドラッグ</b> 持ち上げて、離すと着地
            </span>
          </li>
          <li>
            <MouseScroll size={20} />
            <span>
              <b>背景をドラッグ / ホイール</b> 回転 / ズーム
            </span>
          </li>
          <li>
            <MouseScroll size={20} />
            <span>
              <b>右ドラッグ</b> 視点を平行移動
            </span>
          </li>
          <li>
            <Cursor size={20} />
            <span>
              <b>鳩をダブルクリック</b> 追尾カメラの切り替え
            </span>
          </li>
        </ul>
        <p className="keyboard-help">
          <kbd>F</kbd> 餌　<kbd>C</kbd> 呼ぶ　<kbd>P</kbd> 追尾
          <br />
          <kbd>Space</kbd> 一時停止　<kbd>Esc</kbd> モード解除
        </p>
        <small>
          タッチ操作：1本指で回転、2本指で拡大・移動。
          <br />
          これは仮モデルによるシミュレーションです。
        </small>
        <button className="primary-button" onClick={() => set({ help: false })}>
          公園にもどる
        </button>
      </div>
    </dialog>
  );
}
