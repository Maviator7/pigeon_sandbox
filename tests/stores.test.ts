import { expect, it } from 'vitest';
import { usePigeonStore } from '../src/stores/pigeonStore';
import { useFoodStore } from '../src/stores/foodStore';
it('reset clears pending call commands and interaction modes', () => {
  usePigeonStore.getState().set({ callKey: 5, follow: true, feeding: true, dragging: true });
  usePigeonStore.getState().reset();
  expect(usePigeonStore.getState().callKey).toBe(0);
  expect(usePigeonStore.getState().follow).toBe(false);
  expect(usePigeonStore.getState().dragging).toBe(false);
});
it('rejects food outside the park, inside props, and above capacity', () => {
  const store = useFoodStore.getState();
  store.clear();
  expect(store.add({ x: 20, y: 0, z: 0 })).toBe(false);
  expect(store.add({ x: -4.8, y: 0, z: -3.5 })).toBe(false);
  for (let i = 0; i < 40; i++) expect(store.add({ x: 0, y: 0, z: 0 })).toBe(true);
  expect(store.add({ x: 0, y: 0, z: 0 })).toBe(false);
  expect(useFoodStore.getState().foods).toHaveLength(40);
  store.clear();
});
