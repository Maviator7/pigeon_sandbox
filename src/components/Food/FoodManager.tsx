import { useFoodStore } from '../../stores/foodStore';
export function FoodManager() {
  const foods = useFoodStore((s) => s.foods);
  return (
    <group>
      {foods.map((food) => (
        <group key={food.id} position={[food.position.x, 0.045, food.position.z]}>
          {Array.from({ length: 7 }, (_, i) => (
            <mesh
              key={i}
              position={[Math.sin(i * 5) * 0.16, (i % 2) * 0.02, Math.cos(i * 5) * 0.14]}
              rotation={[i, 0.2 * i, 0.4]}
              castShadow
            >
              <dodecahedronGeometry args={[i % 2 ? 0.037 : 0.047, 0]} />
              <meshStandardMaterial color={i % 3 ? '#d6a96b' : '#f4db9e'} roughness={1} />
            </mesh>
          ))}
          <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, -0.025, 0]}>
            <ringGeometry args={[0.25, 0.27, 32]} />
            <meshBasicMaterial color="#a5824e" transparent opacity={0.45} />
          </mesh>
        </group>
      ))}
    </group>
  );
}
