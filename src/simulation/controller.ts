import type { Decision, Food, PigeonRuntime, PigeonState, Vec3 } from '../types/pigeon';
import { decideNextAction, selectTargetFood, updateNeeds } from './brain';
import { clampToPark, distance, isWalkable, OBSTACLES, randomPoint, safePoint } from './park';
const duration: Partial<Record<PigeonState, number>> = {
  IDLE: 2.5,
  LOOK_AROUND: 2.4,
  LOOK_AT_FOOD: 0.8,
  PECK: 1.8,
  EAT: 2.2,
  PANIC: 0.65,
  FLY: 3.6,
  LAND: 1.2,
  RUN: 1.8,
  WALK: 7,
};
export function transition(
  p: PigeonRuntime,
  decision: Decision,
  random: () => number = Math.random,
) {
  p.state = decision.state;
  p.elapsed = 0;
  p.duration = (duration[p.state] ?? 2) * (0.85 + random() * 0.3);
  p.goal = decision.goal;
  p.target = decision.target ?? null;
  p.foodId = decision.foodId ?? null;
  p.quirk = random();
  p.transitions.unshift({ state: p.state, time: p.age });
  p.transitions.length = Math.min(p.transitions.length, 6);
  if (p.state === 'FLY') {
    p.flightStart = { ...p.position };
    p.target = safePoint(p.target ?? randomPoint(random));
    p.target.y = 0;
  }
  if (p.state === 'LAND') p.flightStart = { ...p.position };
}
export function releasePigeon(p: PigeonRuntime, random: () => number = Math.random) {
  p.position = clampToPark(p.position);
  transition(
    p,
    p.position.y > 0.8
      ? { state: 'FLY', goal: '羽ばたいて、安全な場所へ', target: randomPoint(random) }
      : { state: 'LAND', goal: 'そっと地面へ', target: safePoint({ ...p.position, y: 0 }) },
    random,
  );
}
export function touchPigeon(p: PigeonRuntime) {
  if (['HELD', 'FLY', 'LAND', 'PANIC'].includes(p.state)) return;
  p.needs.fear = Math.min(100, p.needs.fear + 6);
  transition(p, { state: 'LOOK_AROUND', goal: 'ん？ 呼んだ？' });
  p.quirk = 0.92;
}
export function callPigeon(p: PigeonRuntime, target: Vec3) {
  if (['HELD', 'FLY', 'LAND', 'PANIC'].includes(p.state)) return;
  p.callTarget = safePoint({ ...target, y: 0 });
  transition(p, { state: 'LOOK_AROUND', goal: '誰かが呼んでいる' }, () => 0.5);
  p.duration = 0.65;
}
function turn(p: PigeonRuntime, target: Vec3, dt: number, rate = 5) {
  const desired = Math.atan2(target.x - p.position.x, target.z - p.position.z);
  const difference = Math.atan2(Math.sin(desired - p.heading), Math.cos(desired - p.heading));
  p.heading += difference * (1 - Math.exp(-rate * dt));
}
function move(p: PigeonRuntime, dt: number) {
  if (!p.target) return;
  let steer = p.target;
  // Obstacle repulsion steers around tree trunks and benches instead of intersecting them.
  const vx = steer.x - p.position.x,
    vz = steer.z - p.position.z,
    length = Math.hypot(vx, vz) || 1;
  let dx = vx / length,
    dz = vz / length;
  for (const o of OBSTACLES) {
    const ox = p.position.x - o.x,
      oz = p.position.z - o.z,
      d = Math.hypot(ox, oz);
    if (d < o.r + 1) {
      const strength = (o.r + 1 - d) * 2.5 * Math.min(1, length * length);
      dx += (ox / (d || 1)) * strength;
      dz += (oz / (d || 1)) * strength;
    }
  }
  steer = { x: p.position.x + dx, y: 0, z: p.position.z + dz };
  turn(p, steer, dt);
  const goalSpeed = p.state === 'RUN' ? 1.65 : 0.58;
  p.speed += (goalSpeed - p.speed) * (1 - Math.exp(-5 * dt));
  const step = Math.min(p.speed * dt, distance(p.position, p.target));
  const next = {
    x: p.position.x + Math.sin(p.heading) * step,
    y: 0,
    z: p.position.z + Math.cos(p.heading) * step,
  };
  if (isWalkable(next)) {
    p.position = next;
    p.gait += step * 14;
  } else {
    p.heading += dt * 4;
    p.speed *= 0.5;
  }
}
/** Pure simulation step. Mutates only this individual; returns consumable world events. */
export function advance(
  p: PigeonRuntime,
  foods: readonly Food[],
  dt: number,
  random: () => number = Math.random,
) {
  const events = { eaten: [] as string[], sounds: [] as ('coo' | 'flap' | 'peck' | 'step')[] };
  dt = Math.max(0, Math.min(dt, 1));
  p.age += dt;
  p.elapsed += dt;
  p.needs = updateNeeds(p.needs, p.state, dt);
  if (p.state === 'HELD') return events;
  if (p.state === 'FLY') {
    const t = Math.min(1, p.elapsed / p.duration),
      start = p.flightStart,
      end = p.target!;
    const ease = t * t * (3 - 2 * t);
    const arc = Math.sin(Math.PI * t);
    p.position = clampToPark({
      x: start.x + (end.x - start.x) * ease + arc * 0.6,
      y: start.y * (1 - ease) + Math.sin(Math.PI * t) * 2.7 + ease * 0.65,
      z: start.z + (end.z - start.z) * ease - arc * 0.4,
    });
    turn(p, end, dt, 3);
    p.speed = 2.5;
    if (t === 1)
      transition(p, { state: 'LAND', goal: '着地点を確認、ゆっくり降りよう', target: end }, random);
    return events;
  }
  if (p.state === 'LAND') {
    const t = Math.min(1, p.elapsed / p.duration),
      end = p.target ?? safePoint({ ...p.position, y: 0 });
    p.position = {
      x: p.flightStart.x + (end.x - p.flightStart.x) * t,
      y: p.flightStart.y * (1 - t) * (1 - t),
      z: p.flightStart.z + (end.z - p.flightStart.z) * t,
    };
    if (t === 1) {
      p.position = safePoint({ ...end, y: 0 });
      p.needs.fear = Math.min(p.needs.fear, 28);
      transition(p, { state: 'IDLE', goal: 'ふう。無事に着いた' }, random);
    }
    return events;
  }
  if (p.state === 'PANIC') {
    if (p.elapsed >= p.duration) {
      transition(
        p,
        p.needs.energy > 15
          ? { state: 'FLY', goal: '少し離れた場所まで逃げよう', target: randomPoint(random) }
          : { state: 'RUN', goal: '走って距離をとろう', target: randomPoint(random) },
        random,
      );
      events.sounds.push('flap');
      p.needs.fear = Math.min(p.needs.fear, 50);
    }
    return events;
  }
  // A newly placed crumb interrupts idle exploration, but not the look/eat sequence.
  if (
    !p.foodId &&
    p.needs.hunger > 12 &&
    ['IDLE', 'WALK', 'PECK', 'LOOK_AROUND'].includes(p.state) &&
    !p.callTarget
  ) {
    const food = selectTargetFood(p.position, foods);
    if (food) {
      transition(
        p,
        {
          state: 'LOOK_AT_FOOD',
          goal: 'あそこにパンくずがある！',
          target: { ...food.position },
          foodId: food.id,
        },
        random,
      );
      return events;
    }
  }
  if (p.foodId && !foods.some((f) => f.id === p.foodId)) {
    transition(p, { state: 'LOOK_AROUND', goal: 'あれ、パンくずがなくなった' }, random);
    return events;
  }
  if (p.state === 'LOOK_AT_FOOD') {
    if (p.target) turn(p, p.target, dt);
    if (p.elapsed >= p.duration)
      transition(
        p,
        {
          state: 'WALK',
          goal: 'パンくずのところへ向かっている',
          target: p.target!,
          foodId: p.foodId!,
        },
        random,
      );
  } else if (p.state === 'WALK' || p.state === 'RUN') {
    move(p, dt);
    if (p.target && distance(p.position, p.target) < 0.38) {
      if (p.foodId)
        transition(
          p,
          {
            state: 'EAT',
            goal: 'おいしいパンくずを、つんつん',
            target: p.target,
            foodId: p.foodId,
          },
          random,
        );
      else {
        p.callTarget = null;
        transition(p, { state: 'LOOK_AROUND', goal: '着いた。何があるかな' }, random);
      }
    } else if (p.elapsed > p.duration) {
      p.callTarget = null;
      transition(p, { state: 'IDLE', goal: 'ちょっと立ち止まろう' }, random);
    }
  } else if (p.state === 'EAT') {
    if (p.elapsed >= p.duration) {
      if (p.foodId) {
        events.eaten.push(p.foodId);
        events.sounds.push('peck');
        p.eaten++;
        p.needs.hunger = Math.max(0, p.needs.hunger - 24);
        p.needs.trust = Math.min(100, p.needs.trust + 7);
      }
      transition(p, { state: 'IDLE', goal: 'ごちそうさま。もう少し歩こう' }, random);
    }
  } else if (p.elapsed >= p.duration) {
    transition(p, decideNextAction({ pigeon: p, foods, userDistance: 5, random }), random);
  }
  if (!['WALK', 'RUN'].includes(p.state)) p.speed *= Math.exp(-8 * dt);
  return events;
}
