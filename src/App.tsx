import { Component, useEffect } from 'react';
import type { ReactNode } from 'react';
import { Scene } from './components/Scene';
import { ControlPanel } from './components/ui/ControlPanel';
import { PigeonDebugPanel } from './components/ui/PigeonDebugPanel';
import { Header, StatusBar, Toast, Help } from './components/ui/StatusBar';
import { usePigeonStore } from './stores/pigeonStore';
class SceneBoundary extends Component<{ children: ReactNode }, { error: boolean }> {
  state = { error: false };
  static getDerivedStateFromError() {
    return { error: true };
  }
  render() {
    return this.state.error ? (
      <div className="webgl-fallback">
        <h2>公園を表示できませんでした。</h2>
        <p>WebGLの設定を確認して、もう一度お試しください。</p>
        <button onClick={() => window.location.reload()}>再読み込み</button>
      </div>
    ) : (
      this.props.children
    );
  }
}
export default function App() {
  const feeding = usePigeonStore((s) => s.feeding);
  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if (
        event.repeat ||
        (event.target instanceof HTMLElement &&
          ['INPUT', 'TEXTAREA', 'BUTTON', 'SUMMARY'].includes(event.target.tagName))
      )
        return;
      const s = usePigeonStore.getState();
      if (s.help) {
        if (event.key === 'Escape') s.set({ help: false });
        return;
      }
      switch (event.key.toLowerCase()) {
        case 'f':
          s.set({ feeding: !s.feeding });
          break;
        case 'c':
          s.set({ callKey: s.callKey + 1 });
          break;
        case 'p':
          s.set({ follow: !s.follow });
          break;
        case ' ':
          event.preventDefault();
          s.set({ paused: !s.paused });
          break;
        case 'escape':
          s.set({ feeding: false, follow: false });
          break;
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, []);
  return (
    <main className={`app ${feeding ? 'feeding' : ''}`}>
      <div className="scene">
        <SceneBoundary>
          <Scene />
        </SceneBoundary>
      </div>
      <Header />
      <ControlPanel />
      <PigeonDebugPanel />
      <StatusBar />
      <Toast />
      <Help />
    </main>
  );
}
