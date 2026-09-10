import { describe, expect, it } from 'vitest';
import {
  createPigeon,
  decideNextAction,
  updateNeeds,
  selectTargetFood,
  probabilities,
} from '../src/simulation/brain';
import { advance, releasePigeon, callPigeon } from '../src/simulation/controller';
import { isWalkable } from '../src/simulation/park';
import type { Food } from '../src/types/pigeon';
const food: Food = { id: 'f1', position: { x: 1, y: 0, z: 0 }, amount: 1 };
const context = (p = createPigeon()) => ({
  pigeon: p,
  foods: [] as Food[],
  userDistance: 5,
  random: () => 0.4,
});
describe('PigeonBrain priorities', () => {
  it('chooses food before random wandering', () => {
    const c = context();
    c.foods = [food];
    expect(decideNextAction(c).state).toBe('LOOK_AT_FOOD');
  });
  it('prioritizes fear over food', () => {
    const c = context();
    c.foods = [food];
    c.pigeon.needs.fear = 90;
    expect(decideNextAction(c).state).toBe('PANIC');
  });
  it('rests instead of taking off when exhausted', () => {
    const c = context();
    c.pigeon.needs.energy = 2;
    expect(decideNextAction(c).state).toBe('IDLE');
  });
  it('reacts to a nearby user only when untrusted', () => {
    const c = context();
    c.userDistance = 0.1;
    c.pigeon.needs.trust = 0;
    expect(decideNextAction(c).state).toBe('PANIC');
  });
  it('uses random input for different choices', () => {
    const c = context();
    expect(decideNextAction({ ...c, random: () => 0.05 }).state).not.toBe(
      decideNextAction({ ...c, random: () => 0.85 }).state,
    );
  });
  it('selects nearest detectable food', () => {
    expect(
      selectTargetFood({ x: 0, y: 0, z: 0 }, [
        food,
        { ...food, id: 'near', position: { x: 0.5, y: 0, z: 0 } },
      ])?.id,
    ).toBe('near');
    expect(selectTargetFood({ x: 20, y: 0, z: 0 }, [food])).toBeUndefined();
  });
  it('keeps needs within 0..100 under long time updates', () => {
    const p = createPigeon();
    for (let i = 0; i < 100; i++) p.needs = updateNeeds(p.needs, 'FLY', 10);
    expect(Object.values(p.needs).every((n) => n >= 0 && n <= 100)).toBe(true);
    expect(p.needs.energy).toBe(0);
  });
  it('exposes a normalized distribution', () => {
    expect(probabilities(createPigeon()).reduce((sum, p) => sum + p.chance, 0)).toBeCloseTo(1);
  });
});
describe('simulation journeys', () => {
  it('finds, approaches, eats and removes food, reducing hunger and increasing trust', () => {
    const p = createPigeon();
    let foods = [food];
    const before = { ...p.needs };
    const states = new Set<string>();
    for (let i = 0; i < 1500 && foods.length; i++) {
      const events = advance(p, foods, 1 / 60, () => 0.4);
      states.add(p.state);
      foods = foods.filter((f) => !events.eaten.includes(f.id));
    }
    expect(foods).toHaveLength(0);
    expect(states.has('LOOK_AT_FOOD')).toBe(true);
    expect(states.has('WALK')).toBe(true);
    expect(states.has('EAT')).toBe(true);
    expect(p.needs.hunger).toBeLessThan(before.hunger);
    expect(p.needs.trust).toBeGreaterThan(before.trust);
  });
  it('releases from height into flight and lands within the park', () => {
    const p = createPigeon();
    p.position.y = 3;
    p.state = 'HELD';
    releasePigeon(p, () => 0.4);
    expect(p.state).toBe('FLY');
    let landed = false;
    for (let i = 0; i < 900; i++) {
      advance(p, [], 1 / 60, () => 0.4);
      if (String(p.state) === 'IDLE' && p.position.y === 0) {
        landed = true;
        break;
      }
    }
    expect(landed).toBe(true);
    expect(isWalkable(p.position)).toBe(true);
  });
  it('releases close to the ground into LAND', () => {
    const p = createPigeon();
    p.position.y = 0.3;
    releasePigeon(p);
    expect(p.state).toBe('LAND');
  });
  it('holds position while held and increases fear', () => {
    const p = createPigeon();
    p.state = 'HELD';
    p.position.y = 2;
    const fear = p.needs.fear;
    advance(p, [], 1, () => 0.4);
    expect(p.position.y).toBe(2);
    expect(p.needs.fear).toBeGreaterThan(fear);
  });
  it('Call directs a calm pigeon to an actual location', () => {
    const p = createPigeon();
    callPigeon(p, { x: 2, y: 0, z: 0 });
    let nearest = Infinity;
    for (let i = 0; i < 480; i++) {
      advance(p, [], 1 / 60, () => 0.4);
      nearest = Math.min(nearest, Math.hypot(p.position.x - 2, p.position.z));
    }
    expect(nearest).toBeLessThan(0.4);
  });
  it('walks and flies for simulated minutes without leaving bounds or entering obstacles', () => {
    const p = createPigeon();
    let seed = 123;
    const random = () => {
      seed = (seed * 1664525 + 1013904223) >>> 0;
      return seed / 4294967296;
    };
    for (let i = 0; i < 18000; i++) {
      advance(p, [], 1 / 30, random);
      expect(Math.hypot(p.position.x, p.position.z)).toBeLessThanOrEqual(7.01);
      if (p.position.y === 0) expect(isWalkable(p.position)).toBe(true);
      expect(Number.isFinite(p.heading)).toBe(true);
    }
  });
});

describe('food close to obstacles', () => {
  it.each([
    { x: 5.8, y: 0, z: 2.15 },
    { x: -4.8, y: 0, z: -2.85 },
  ])('can eat valid food next to a trunk at $x / $z', (position) => {
    const p = createPigeon();
    p.needs.hunger = 80;
    const crumb = { id: 'tree-food', amount: 1, position };
    let eaten = false;
    for (let i = 0; i < 18000; i++) {
      if (advance(p, [crumb], 1 / 30, () => 0.4).eaten.length) {
        eaten = true;
        break;
      }
    }
    expect(eaten).toBe(true);
  });
});
