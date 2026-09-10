import { Bird, X, ArrowUpRight, Eye } from '@phosphor-icons/react';
import { STATE_LABELS, probabilities } from '../../simulation/brain';
import { usePigeonStore } from '../../stores/pigeonStore';
import type { Needs } from '../../types/pigeon';
const metrics: { key: keyof Needs; name: string; ja: string }[] = [
  { key: 'hunger', name: 'Hunger', ja: '空腹度' },
  { key: 'fear', name: 'Fear', ja: '警戒度' },
  { key: 'curiosity', name: 'Curiosity', ja: '好奇心' },
  { key: 'energy', name: 'Energy', ja: '体力' },
  { key: 'trust', name: 'Trust', ja: '信頼度' },
];
export function PigeonDebugPanel() {
  const p = usePigeonStore((s) => s.snapshot),
    selected = usePigeonStore((s) => s.selected),
    set = usePigeonStore((s) => s.set),
    modelStatus = usePigeonStore((s) => s.modelStatus);
  if (!selected)
    return (
      <button
        aria-label="鳩の気持ちを観察"
        className="observation-invite glass"
        onClick={() => set({ selected: true })}
      >
        <Eye size={20} />
        <span>
          一羽の、小さな世界。<small>気持ちをのぞいてみる</small>
        </span>
        <ArrowUpRight size={17} />
      </button>
    );
  return (
    <aside className="observation-panel glass" aria-label="鳩の観察パネル">
      <div className="observation-head">
        <span className="panel-eyebrow">FIELD NOTES</span>
        <button
          className="icon-button"
          aria-label="観察パネルを閉じる"
          onClick={() => set({ selected: false })}
        >
          <X size={17} />
        </button>
      </div>
      <div className="identity">
        <span className="bird-avatar">
          <Bird weight="duotone" size={35} />
        </span>
        <div>
          <h2>
            Pigeon <span>#{p.id}</span>
          </h2>
          <p>
            カワラバト <span>Columba livia</span>
          </p>
        </div>
      </div>
      <div className="state-row">
        <span className="status-dot" />
        <strong>{STATE_LABELS[p.state]}</strong>
        <code data-testid="pigeon-state">{p.state}</code>
      </div>
      <div className="metrics">
        {metrics.map(({ key, name, ja }) => (
          <div className={`metric metric-${key}`} key={key}>
            <div className="metric-label">
              <span>
                {name}
                <small>{ja}</small>
              </span>
              <b>
                {Math.round(p.needs[key])}
                <span>/100</span>
              </b>
            </div>
            <div
              role="meter"
              aria-label={ja}
              aria-valuenow={Math.round(p.needs[key])}
              aria-valuemin={0}
              aria-valuemax={100}
              className="meter"
            >
              <span style={{ width: `${p.needs[key]}%` }} />
            </div>
          </div>
        ))}
      </div>
      <div className="goal">
        <div className="panel-eyebrow">ON MY MIND</div>
        <p>「{p.goal}」</p>
      </div>
      <details className="probabilities">
        <summary>
          次の行動の確率<span>+</span>
        </summary>
        <p className="probability-note">自由行動時の抽選。餌・警戒・呼び出しを優先。</p>
        {probabilities(p)
          .filter((item) => item.chance > 0)
          .map((item) => (
            <div className="probability-row" key={item.state}>
              <span>{item.state.replaceAll('_', ' ')}</span>
              <span>{Math.round(item.chance * 100)}%</span>
            </div>
          ))}
      </details>
      <div className="observation-foot">
        <span>{modelStatus}</span>
        <span>
          食べた餌 <b>{p.eaten}</b>
        </span>
      </div>
    </aside>
  );
}
