import { useEffect, useMemo } from 'react';
import { Canvas, useThree } from '@react-three/fiber';
import type { ThreeEvent } from '@react-three/fiber';
import { Ground } from './Environment/Ground';
import { Tree } from './Environment/Tree';
import { Bench } from './Environment/Bench';
import { Pigeon } from './Pigeon/Pigeon';
import { FoodManager } from './Food/FoodManager';
import { CameraController } from './Camera/CameraController';
import { createPigeon } from '../simulation/brain';
import { callPigeon } from '../simulation/controller';
import { usePigeonStore } from '../stores/pigeonStore';
import { useFoodStore } from '../stores/foodStore';
import { soundManager } from '../audio/SoundManager';
function Park() {
  const pigeon = useMemo(() => createPigeon(), []);
  const callKey = usePigeonStore((s) => s.callKey);
  const { camera } = useThree();
  useEffect(() => {
    if (callKey > 0) {
      const factor = 3 / Math.hypot(camera.position.x, camera.position.z);
      callPigeon(pigeon, { x: camera.position.x * factor, y: 0, z: camera.position.z * factor });
      soundManager.play('coo');
    }
  }, [callKey, camera, pigeon]);
  function place(e: ThreeEvent<MouseEvent>) {
    e.stopPropagation();
    const ui = usePigeonStore.getState();
    if (!ui.feeding || e.delta > 5) return;
    const added = useFoodStore.getState().add({ x: e.point.x, y: 0, z: e.point.z });
    ui.set({
      toast: added
        ? 'パンくずを置きました。気づいてくれるかな？'
        : '木やベンチを避け、公園の地面に置いてください（最大40か所）',
    });
  }
  return (
    <>
      <color attach="background" args={['#e8ede3']} />
      <fog attach="fog" args={['#e8ede3', 30, 65]} />
      <ambientLight intensity={0.6} />
      <hemisphereLight args={['#fff8e9', '#8b987e', 1.8]} />
      <directionalLight
        position={[-5, 10, 4]}
        intensity={2.5}
        castShadow
        shadow-mapSize={[2048, 2048]}
        shadow-camera-left={-12}
        shadow-camera-right={12}
        shadow-camera-top={12}
        shadow-camera-bottom={-12}
        shadow-normalBias={0.035}
      />
      <Ground onPlace={place} />
      <Tree position={[-4.8, 0, -3.5]} />
      <Tree position={[4.3, 0, -3.8]} scale={1.2} />
      <Tree position={[5.8, 0, 1.5]} scale={0.85} />
      <Bench position={[-2.1, 0, -3.2]} />
      <Bench position={[2.5, 0, -4.5]} rotation={-0.25} />
      <Pigeon pigeon={pigeon} />
      <FoodManager />
      <CameraController pigeon={pigeon} />
    </>
  );
}
export function Scene() {
  const resetKey = usePigeonStore((s) => s.resetKey);
  return (
    <Canvas
      key={resetKey}
      shadows
      dpr={[1, 1.75]}
      camera={{ position: [10, 8.3, 12], fov: 39, near: 0.1, far: 150 }}
      gl={{ antialias: true, powerPreference: 'high-performance' }}
      fallback={
        <div className="webgl-fallback">
          3D表示にはWebGL対応ブラウザが必要です。ブラウザのハードウェアアクセラレーションを有効にして再読み込みしてください。
        </div>
      }
    >
      <Park key={resetKey} />
    </Canvas>
  );
}
