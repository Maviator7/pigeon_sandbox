export function Tree({
  position,
  scale = 1,
}: {
  position: [number, number, number];
  scale?: number;
}) {
  return (
    <group position={position} scale={scale}>
      <mesh position={[0, 1.25, 0]} castShadow>
        <cylinderGeometry args={[0.14, 0.24, 2.5, 7]} />
        <meshStandardMaterial color="#7b7254" roughness={1} />
      </mesh>
      <mesh position={[0.3, 1.95, 0]} rotation={[0, 0, -0.6]} castShadow>
        <cylinderGeometry args={[0.07, 0.11, 1.25, 6]} />
        <meshStandardMaterial color="#7b7254" />
      </mesh>
      {[
        [0, 3, 0, 1.25],
        [-0.7, 2.7, 0.15, 0.85],
        [0.6, 3.15, 0.1, 0.96],
        [0.12, 3.85, 0, 0.85],
        [0.2, 2.8, -0.7, 0.78],
      ].map(([x, y, z, s], i) => (
        <mesh key={i} position={[x, y, z]} scale={[s, s * 0.85, s]} castShadow receiveShadow>
          <icosahedronGeometry args={[1, 1]} />
          <meshStandardMaterial
            color={['#849b64', '#95a76d', '#a2b57c', '#b1bd82', '#7f9963'][i]}
            roughness={1}
          />
        </mesh>
      ))}
    </group>
  );
}
