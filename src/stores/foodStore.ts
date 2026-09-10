import { create } from 'zustand';
import type { Food, Vec3 } from '../types/pigeon';
import { isWalkable } from '../simulation/park';
interface FoodStore {
  foods: Food[];
  add: (position: Vec3) => boolean;
  remove: (id: string) => void;
  clear: () => void;
}
let sequence = 0;
export const useFoodStore = create<FoodStore>((set) => ({
  foods: [],
  add: (position) => {
    if (!isWalkable(position)) return false;
    let added = false;
    set((s) => {
      if (s.foods.length >= 40) return s;
      added = true;
      return {
        foods: [
          ...s.foods,
          { id: `crumb-${++sequence}`, position: { ...position, y: 0 }, amount: 1 },
        ],
      };
    });
    return added;
  },
  remove: (id) => set((s) => ({ foods: s.foods.filter((f) => f.id !== id) })),
  clear: () => set({ foods: [] }),
}));
