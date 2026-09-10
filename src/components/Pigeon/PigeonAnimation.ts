import { Object3D } from 'three';
import type { PigeonRuntime } from '../../types/pigeon';
type Part = {
  node: Object3D;
  position: [number, number, number];
  rotation: [number, number, number];
};
export type Rig = Partial<
  Record<'Body' | 'Neck' | 'Head' | 'WingL' | 'WingR' | 'LegL' | 'LegR' | 'Tail', Part>
>;
export function captureRig(root: Object3D): Rig {
  const rig: Rig = {};
  for (const name of ['Body', 'Neck', 'Head', 'WingL', 'WingR', 'LegL', 'LegR', 'Tail'] as const) {
    const node = root.getObjectByName(name);
    if (node)
      rig[name] = {
        node,
        position: node.position.toArray(),
        rotation: [node.rotation.x, node.rotation.y, node.rotation.z],
      };
  }
  return rig;
}
export function animateRig(rig: Rig, p: PigeonRuntime) {
  for (const part of Object.values(rig)) {
    part.node.position.fromArray(part.position);
    part.node.rotation.set(...part.rotation);
  }
  const walking = p.state === 'WALK' || p.state === 'RUN',
    flying = p.state === 'FLY' || p.state === 'LAND' || p.state === 'PANIC',
    held = p.state === 'HELD';
  const eating = p.state === 'EAT' || p.state === 'PECK',
    time = p.age;
  const phase = (p.gait / (Math.PI * 2)) % 1;
  // Hold/thrust: the head travels back relative to the moving body, then catches up quickly.
  const bob = phase < 0.68 ? 0.1 - 0.2 * (phase / 0.68) : -0.1 + 0.2 * ((phase - 0.68) / 0.32);
  if (rig.Body) {
    rig.Body.node.position.y +=
      Math.sin(time * 2.2) * 0.009 + (walking ? Math.abs(Math.sin(p.gait)) * 0.017 : 0);
    rig.Body.node.rotation.x += flying ? 0.2 : eating ? 0.78 + Math.sin(time * 10) * 0.04 : 0;
  }
  if (rig.Neck) {
    rig.Neck.node.position.z += walking ? bob : 0;
    rig.Neck.node.rotation.x += eating ? 0.55 + Math.sin(time * 10) * 0.08 : 0;
    rig.Neck.node.position.y -= eating ? 0.16 + Math.sin(time * 10) * 0.025 : 0;
  }
  if (rig.Head) {
    rig.Head.node.rotation.y +=
      p.state === 'LOOK_AROUND' ? Math.sin(time * 2.1) * 0.7 : Math.sin(time * 0.9) * 0.1;
    rig.Head.node.rotation.z += p.quirk > 0.8 && !walking ? Math.sin(p.elapsed * 2) * 0.55 : 0;
  }
  for (const [side, key] of [
    [-1, 'WingL'],
    [1, 'WingR'],
  ] as const) {
    const part = rig[key];
    if (part)
      part.node.rotation.z +=
        side *
        (flying
          ? 1.1 + Math.sin(time * 24) * 0.8
          : held
            ? 0.7 + Math.sin(time * 15) * 0.3
            : p.quirk > 0.95
              ? Math.max(0, Math.sin(p.elapsed * 4)) * 0.2
              : 0);
  }
  if (rig.LegL)
    rig.LegL.node.rotation.x += walking
      ? Math.sin(p.gait) * 0.62
      : held
        ? Math.sin(time * 7) * 0.25
        : flying
          ? 0.65
          : 0;
  if (rig.LegR)
    rig.LegR.node.rotation.x += walking
      ? -Math.sin(p.gait) * 0.62
      : held
        ? -Math.sin(time * 7 + 0.4) * 0.25
        : flying
          ? 0.65
          : 0;
  if (rig.Tail) rig.Tail.node.rotation.x += flying ? -0.25 : Math.sin(time * 1.5) * 0.025;
}
