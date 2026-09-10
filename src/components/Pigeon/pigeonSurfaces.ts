import { Color, Float32BufferAttribute, MathUtils, SphereGeometry } from 'three';

/** Color the continuous surface itself instead of layering small feather meshes. */
export function createPigeonSurfaces() {
  const wing = new SphereGeometry(1, 48, 32);
  const neck = new SphereGeometry(1, 32, 24);
  const base = new Color('#a0a9ae');
  const stripe = new Color('#66727e');
  const green = new Color('#607b75');
  const violet = new Color('#777080');
  const color = new Color();
  for (const [geometry, isWing] of [[wing, true], [neck, false]] as const) {
    const positions = geometry.attributes.position;
    const colors = new Float32Array(positions.count * 3);
    for (let i = 0; i < positions.count; i++) {
      if (isWing) {
        const z = positions.getZ(i);
        const band = (center: number) => 1 - MathUtils.smoothstep(Math.abs(z - center), .065, .10);
        color.copy(base).lerp(stripe, Math.max(band(-.2), band(-.6)));
      } else {
        color.copy(violet).lerp(green, MathUtils.smoothstep(positions.getY(i), -.65, .55));
      }
      color.toArray(colors, i * 3);
    }
    geometry.setAttribute('color', new Float32BufferAttribute(colors, 3));
  }
  return { wing, neck };
}
