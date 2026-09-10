import { useLayoutEffect, useMemo, useRef } from 'react';
import { Color, InstancedMesh, Object3D } from 'three';
import type { ThreeEvent } from '@react-three/fiber';
export function Ground({ onPlace }: { onPlace?: (e: ThreeEvent<MouseEvent>) => void }) {
  const grass = useRef<InstancedMesh>(null);
  const stones = useMemo(
    () =>
      Array.from({ length: 38 }, (_, i) => {
        const a = (i / 38) * Math.PI * 2;
        return [Math.cos(a) * 7.45, Math.sin(a) * 7.45, a];
      }),
    [],
  );
  useLayoutEffect(() => {
    const dummy = new Object3D();
    const color = new Color();
    for (let i = 0; i < 420; i++) {
      const angle = i * 2.39996;
      const radius = Math.sqrt((i + 0.5) / 420) * 6.95;
      const x = Math.cos(angle) * radius,
        z = Math.sin(angle) * radius;
      dummy.position.set(x, -0.005, z);
      dummy.rotation.set(0, angle, 0.15 * Math.sin(i));
      const h = 0.06 + (Math.sin(i * 78.2) * 0.5 + 0.5) * 0.11;
      dummy.scale.set(0.035, h, 0.035);
      if (Math.abs(z) < 1.35 || (x < -0.6 && x > -3.8 && z < -2.3 && z > -4))
        dummy.scale.setScalar(0);
      dummy.updateMatrix();
      grass.current!.setMatrixAt(i, dummy.matrix);
      grass.current!.setColorAt(i, color.set(i % 3 === 0 ? '#9cad75' : '#839a64'));
    }
    grass.current!.instanceMatrix.needsUpdate = true;
  }, []);
  return (
    <group>
      <mesh position={[0, -0.31, 0]} receiveShadow>
        <cylinderGeometry args={[7.55, 7.35, 0.6, 96]} />
        <meshStandardMaterial color="#b8baa0" roughness={1} />
      </mesh>
      <mesh rotation={[-Math.PI / 2, 0, 0]} receiveShadow onClick={onPlace}>
        <circleGeometry args={[7.48, 96]} />
        <meshStandardMaterial color="#b5c58d" roughness={1} />
      </mesh>
      <mesh
        position={[0, 0.009, 0]}
        rotation={[-Math.PI / 2, 0, 0]}
        receiveShadow
        onClick={onPlace}
      >
        <planeGeometry args={[14.5, 2.65]} />
        <meshStandardMaterial color="#e6dec8" roughness={1} />
      </mesh>
      {stones.map(([x, z, a], i) => (
        <mesh key={i} position={[x, -0.015, z]} rotation={[0, -a, 0]} receiveShadow>
          <boxGeometry args={[0.19, 0.15, 1.19]} />
          <meshStandardMaterial color={i % 3 === 0 ? '#d9d9bf' : '#cccdb4'} roughness={1} />
        </mesh>
      ))}
      <instancedMesh ref={grass} args={[undefined, undefined, 420]}>
        <coneGeometry args={[1, 1, 3]} />
        <meshStandardMaterial roughness={1} />
      </instancedMesh>
      <mesh position={[0, -0.64, 0]} rotation={[-Math.PI / 2, 0, 0]} receiveShadow>
        <planeGeometry args={[200, 200]} />
        <meshStandardMaterial color="#e8ede3" />
      </mesh>
    </group>
  );
}
