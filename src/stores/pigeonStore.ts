import { create } from 'zustand';
import { createPigeon } from '../simulation/brain';
import type { PigeonRuntime } from '../types/pigeon';
interface PigeonStore {
  snapshot: PigeonRuntime;
  selected: boolean;
  feeding: boolean;
  follow: boolean;
  dragging: boolean;
  paused: boolean;
  speed: number;
  resetKey: number;
  callKey: number;
  sound: boolean;
  help: boolean;
  modelStatus: string;
  toast: string;
  publish: (p: PigeonRuntime) => void;
  set: (patch: Partial<Omit<PigeonStore, 'set' | 'publish' | 'reset'>>) => void;
  reset: () => void;
}
export const usePigeonStore = create<PigeonStore>((set) => ({
  snapshot: createPigeon(),
  selected: false,
  feeding: false,
  follow: false,
  dragging: false,
  paused: false,
  speed: 1,
  resetKey: 0,
  callKey: 0,
  sound: false,
  help: false,
  modelStatus: '手続きモデル',
  toast: '',
  set: (patch) => set(patch),
  publish: (p) =>
    set({
      snapshot: {
        ...p,
        position: { ...p.position },
        needs: { ...p.needs },
        transitions: [...p.transitions],
      },
    }),
  reset: () =>
    set((s) => ({
      resetKey: s.resetKey + 1,
      callKey: 0,
      snapshot: createPigeon(),
      feeding: false,
      follow: false,
      dragging: false,
      paused: false,
      speed: 1,
      toast: '公園をはじめの状態に戻しました',
    })),
}));
