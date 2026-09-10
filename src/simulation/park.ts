import type { Vec3 } from '../types/pigeon';
export const PARK_RADIUS = 7;
// Shared with scenery: circles include a body-width margin around solid props.
export const OBSTACLES = [
  { x: -4.8, z: -3.5, r: 0.6 },
  { x: 4.3, z: -3.8, r: 0.65 },
  { x: 5.8, z: 1.5, r: 0.6 },
  { x: -2.1, z: -3.2, r: 1.35 },
  { x: 2.5, z: -4.5, r: 1.35 },
];
export const distance = (a: Vec3, b: Vec3) => Math.hypot(a.x - b.x, a.z - b.z);
export function isWalkable(p: Vec3) {
  return (
    Math.hypot(p.x, p.z) <= PARK_RADIUS &&
    OBSTACLES.every((o) => Math.hypot(p.x - o.x, p.z - o.z) > o.r)
  );
}
export function clampToPark(p: Vec3): Vec3 {
  const r = Math.hypot(p.x, p.z),
    s = r > PARK_RADIUS ? PARK_RADIUS / r : 1;
  return { x: p.x * s, y: Math.max(0, p.y), z: p.z * s };
}
export function safePoint(p: Vec3): Vec3 {
  const result = clampToPark(p);
  for (const o of OBSTACLES) {
    const dx = result.x - o.x,
      dz = result.z - o.z,
      d = Math.hypot(dx, dz);
    if (d <= o.r) {
      const a = d < 0.001 ? 0 : Math.atan2(dx, dz);
      result.x = o.x + Math.sin(a) * (o.r + 0.07);
      result.z = o.z + Math.cos(a) * (o.r + 0.07);
    }
  }
  return clampToPark(result);
}
export function randomPoint(random: () => number): Vec3 {
  for (let i = 0; i < 20; i++) {
    const a = random() * Math.PI * 2,
      r = 1 + Math.sqrt(random()) * 5.5;
    const p = { x: Math.sin(a) * r, y: 0, z: Math.cos(a) * r };
    if (isWalkable(p)) return p;
  }
  return { x: 0, y: 0, z: 0 };
}
