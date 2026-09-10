import { useEffect, useRef } from 'react';
import { useFrame, useThree } from '@react-three/fiber';
import { OrbitControls } from '@react-three/drei';
import type { OrbitControls as OrbitControlsImpl } from 'three-stdlib';
import { PerspectiveCamera, Vector3 } from 'three';
import type { PigeonRuntime } from '../../types/pigeon';
import { usePigeonStore } from '../../stores/pigeonStore';
export function CameraController({ pigeon }: { pigeon: PigeonRuntime }) {
  const ref = useRef<OrbitControlsImpl>(null),
    saved = useRef<{ position: Vector3; target: Vector3 } | null>(null);
  const follow = usePigeonStore((s) => s.follow),
    dragging = usePigeonStore((s) => s.dragging),
    feeding = usePigeonStore((s) => s.feeding);
  const { camera, size } = useThree();
  const narrow = size.width < 700;
  const desired = useRef(new Vector3());
  useEffect(() => {
    if (!(camera instanceof PerspectiveCamera)) return;
    camera.fov = narrow ? 50 : 39;
    camera.updateProjectionMatrix();
    if (!usePigeonStore.getState().follow) {
      camera.position.set(...((narrow ? [17, 14, 20] : [10, 8.3, 12]) as [number, number, number]));
      ref.current?.target.set(0, 0.45, 0);
      ref.current?.update();
    }
  }, [narrow, camera]);
  useEffect(() => {
    if (!ref.current) return;
    if (follow) {
      saved.current = { position: camera.position.clone(), target: ref.current.target.clone() };
      const offset = camera.position.clone().sub(ref.current.target).normalize().multiplyScalar(5);
      ref.current.target.set(pigeon.position.x, pigeon.position.y + 0.8, pigeon.position.z);
      camera.position.copy(ref.current.target).add(offset);
    } else if (saved.current) {
      camera.position.copy(saved.current.position);
      ref.current.target.copy(saved.current.target);
      saved.current = null;
    }
    ref.current.update();
  }, [follow, camera, pigeon]);
  useFrame((_, dt) => {
    const controls = ref.current;
    if (!controls) return;
    if (follow && !dragging) {
      desired.current.set(pigeon.position.x, pigeon.position.y + 0.8, pigeon.position.z);
      const dx = (desired.current.x - controls.target.x) * (1 - Math.exp(-5 * dt)),
        dy = (desired.current.y - controls.target.y) * (1 - Math.exp(-5 * dt)),
        dz = (desired.current.z - controls.target.z) * (1 - Math.exp(-5 * dt));
      controls.target.add(new Vector3(dx, dy, dz));
      camera.position.add(new Vector3(dx, dy, dz));
    }
    const before = controls.target.clone();
    controls.target.x = Math.max(-7, Math.min(7, controls.target.x));
    controls.target.z = Math.max(-7, Math.min(7, controls.target.z));
    controls.target.y = Math.max(0.1, Math.min(5, controls.target.y));
    camera.position.add(controls.target.clone().sub(before));
    camera.position.y = Math.max(0.2, camera.position.y);
  });
  return (
    <OrbitControls
      ref={ref}
      makeDefault
      enabled={!dragging}
      enableRotate={!feeding}
      enablePan={!follow && !feeding}
      target={[0, 0.45, 0]}
      minDistance={3}
      maxDistance={25}
      maxPolarAngle={Math.PI / 2 - 0.09}
      minPolarAngle={0.2}
      enableDamping
      dampingFactor={0.08}
    />
  );
}
