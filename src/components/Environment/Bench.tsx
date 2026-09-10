export function Bench({
  position,
  rotation = 0,
}: {
  position: [number, number, number];
  rotation?: number;
}) {
  return (
    <group position={position} rotation={[0, rotation, 0]}>
      {[0, 1, 2, 3].map((i) => (
        <mesh key={'seat' + i} position={[0, 0.62, i * 0.15 - 0.22]} castShadow receiveShadow>
          <boxGeometry args={[2.3, 0.085, 0.13]} />
          <meshStandardMaterial color="#aa7850" roughness={0.88} />
        </mesh>
      ))}
      {[0, 1, 2].map((i) => (
        <mesh
          key={'back' + i}
          position={[0, 0.95 + i * 0.18, -0.3]}
          rotation={[-0.12, 0, 0]}
          castShadow
        >
          <boxGeometry args={[2.3, 0.14, 0.08]} />
          <meshStandardMaterial color="#b5865c" />
        </mesh>
      ))}
      {[-0.83, 0.83].map((x) => (
        <group key={x}>
          {[-0.22, 0.22].map((z) => (
            <mesh key={z} position={[x, 0.31, z]} castShadow>
              <boxGeometry args={[0.075, 0.62, 0.075]} />
              <meshStandardMaterial color="#34473e" />
            </mesh>
          ))}
          <mesh position={[x, 0.88, -0.3]} castShadow>
            <boxGeometry args={[0.065, 1.02, 0.065]} />
            <meshStandardMaterial color="#34473e" />
          </mesh>
          <mesh position={[x, 0.91, 0]} castShadow>
            <boxGeometry args={[0.09, 0.065, 0.72]} />
            <meshStandardMaterial color="#34473e" />
          </mesh>
        </group>
      ))}
    </group>
  );
}
