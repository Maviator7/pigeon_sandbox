import {
  Bread,
  HandWaving,
  Camera,
  ArrowCounterClockwise,
  ArrowUpRight,
} from '@phosphor-icons/react';
import { usePigeonStore } from '../../stores/pigeonStore';
import { useFoodStore } from '../../stores/foodStore';
import { soundManager } from '../../audio/SoundManager';
export function resetPark() {
  useFoodStore.getState().clear();
  usePigeonStore.getState().reset();
  soundManager.stop();
}
export function ControlPanel() {
  const feeding = usePigeonStore((s) => s.feeding),
    follow = usePigeonStore((s) => s.follow),
    set = usePigeonStore((s) => s.set);
  return (
    <aside className="control-panel glass" aria-label="公園の操作">
      <div className="panel-eyebrow">LET’S SPEND SOME TIME</div>
      <button
        className={`tool feed-tool ${feeding ? 'active' : ''}`}
        aria-label="Feed パンくずを置く"
        aria-pressed={feeding}
        onClick={() =>
          set({ feeding: !feeding, toast: feeding ? '' : '地面をクリックしてパンくずを置こう' })
        }
      >
        <span className="tool-icon">
          <Bread size={23} />
        </span>
        <span>
          <strong>Feed</strong>
          <small>パンくずを置く</small>
        </span>
        <kbd>F</kbd>
      </button>
      <button
        aria-label="Call 鳩を呼ぶ"
        className="tool"
        onClick={() => {
          const s = usePigeonStore.getState();
          set({ callKey: s.callKey + 1, toast: 'おーい。こちらへ来てくれるかな？' });
        }}
      >
        <span className="tool-icon">
          <HandWaving size={23} />
        </span>
        <span>
          <strong>Call</strong>
          <small>鳩を呼ぶ</small>
        </span>
        <kbd>C</kbd>
      </button>
      <button
        className={`tool ${follow ? 'active' : ''}`}
        aria-label="Pigeon Cam 鳩追尾"
        aria-pressed={follow}
        onClick={() => set({ follow: !follow })}
      >
        <span className="tool-icon">
          <Camera size={23} />
        </span>
        <span>
          <strong>Pigeon Cam</strong>
          <small>{follow ? '追尾中 · クリックで解除' : '鳩の目線に、近づく'}</small>
        </span>
        <kbd>P</kbd>
      </button>
      <div className="tool-divider" />
      <button
        aria-label="Reset はじめから、もう一度"
        className="tool reset-tool"
        onClick={resetPark}
      >
        <span className="tool-icon">
          <ArrowCounterClockwise size={21} />
        </span>
        <span>
          <strong>Reset</strong>
          <small>はじめから、もう一度</small>
        </span>
        <ArrowUpRight size={15} />
      </button>
      <div className="panel-note">
        {feeding ? '地面をクリックで配置 · Escで終了' : '鳩をクリックすると、気持ちが見えます。'}
      </div>
    </aside>
  );
}
