import type {
  BrainContext,
  Decision,
  Food,
  Needs,
  PigeonRuntime,
  PigeonState,
  Vec3,
} from '../types/pigeon';
import { distance, isWalkable, randomPoint } from './park';
export const STATE_LABELS: Record<PigeonState, string> = {
  IDLE: 'ひとやすみ',
  LOOK_AROUND: 'きょろきょろ',
  LOOK_AT_FOOD: 'パンくず、発見',
  WALK: 'おさんぽ',
  RUN: '突然のダッシュ',
  PECK: 'つん、つん',
  EAT: 'もぐもぐ',
  PANIC: 'びっくり！',
  FLY: 'ひとっ飛び',
  LAND: '着地中',
  HELD: 'つかまった…',
};
export function createPigeon(id = '001'): PigeonRuntime {
  return {
    id,
    state: 'LOOK_AROUND',
    needs: { hunger: 67, curiosity: 74, fear: 8, energy: 88, trust: 32 },
    position: { x: 0, y: 0, z: 0.6 },
    heading: 0.65,
    speed: 0,
    target: null,
    foodId: null,
    elapsed: 0,
    duration: 2.4,
    age: 0,
    goal: 'ここは、どんな場所だろう',
    gait: 0,
    quirk: 0,
    flightStart: { x: 0, y: 0, z: 0 },
    callTarget: null,
    eaten: 0,
    transitions: [],
  };
}
export function updateNeeds(needs: Needs, state: PigeonState, dt: number): Needs {
  const moving = state === 'WALK' || state === 'RUN';
  const flying = state === 'FLY' || state === 'LAND';
  const next = {
    hunger: needs.hunger + dt * 0.16,
    curiosity: needs.curiosity + dt * (state === 'LOOK_AROUND' ? -0.35 : 0.08),
    fear: needs.fear + dt * (state === 'HELD' ? 7 : -2.2),
    energy: needs.energy + dt * (flying ? -3.7 : moving ? -0.55 : 1.5),
    trust: needs.trust - dt * 0.006,
  };
  for (const key of Object.keys(next) as (keyof Needs)[])
    next[key] = Math.max(0, Math.min(100, next[key]));
  return next;
}
export function selectTargetFood(position: Vec3, foods: readonly Food[]) {
  let best: Food | undefined,
    nearest = 7;
  for (const food of foods) {
    const d = distance(position, food.position);
    if (d < nearest && isWalkable(food.position)) {
      nearest = d;
      best = food;
    }
  }
  return best;
}
export function probabilities(p: PigeonRuntime): { state: PigeonState; chance: number }[] {
  if (p.needs.energy < 12) return [{ state: 'IDLE', chance: 1 }];
  const fly = p.needs.energy > 30 ? 0.04 : 0;
  return [
    { state: 'WALK', chance: 0.51 },
    { state: 'LOOK_AROUND', chance: 0.23 },
    { state: 'PECK', chance: 0.12 },
    { state: 'RUN', chance: 0.06 },
    { state: 'IDLE', chance: 0.08 - fly },
    { state: 'FLY', chance: fly },
  ];
}
export function decideNextAction({
  pigeon: p,
  foods,
  userDistance,
  random,
}: BrainContext): Decision {
  if (p.needs.fear > 65 || (userDistance < 0.5 && p.needs.trust < 20))
    return { state: 'PANIC', goal: 'ちょっと離れておこう' };
  if (p.needs.energy < 12) return { state: 'IDLE', goal: '少し休んで、体力を戻そう' };
  const food = p.needs.hunger > 12 ? selectTargetFood(p.position, foods) : undefined;
  if (food)
    return {
      state: 'LOOK_AT_FOOD',
      goal: 'あそこにパンくずがある！',
      target: { ...food.position },
      foodId: food.id,
    };
  if (p.callTarget)
    return { state: 'WALK', goal: '呼ばれたほうへ行ってみよう', target: p.callTarget };
  const sample = random();
  let sum = 0;
  let state: PigeonState = 'IDLE';
  for (const item of probabilities(p)) {
    sum += item.chance;
    if (sample < sum) {
      state = item.state;
      break;
    }
  }
  const goals: Partial<Record<PigeonState, string>> = {
    IDLE: 'なんだか、いい天気',
    WALK: '気になる場所まで、トコトコ',
    LOOK_AROUND: '何か動いたような…',
    RUN: '理由はないけど、走りたい',
    PECK: 'ここにも何か落ちているかな',
    FLY: '向こう側まで飛んでみよう',
  };
  return {
    state,
    goal: goals[state] ?? 'のんびり過ごしている',
    target: ['WALK', 'RUN', 'FLY'].includes(state) ? randomPoint(random) : undefined,
  };
}
