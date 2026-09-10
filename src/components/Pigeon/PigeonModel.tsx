import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';
import { useFrame } from '@react-three/fiber';
import { AnimationMixer, Box3, Group, Vector3 } from 'three';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';
import type { GLTF } from 'three/examples/jsm/loaders/GLTFLoader.js';
import { clone } from 'three/examples/jsm/utils/SkeletonUtils.js';
import { ProceduralPigeon } from './ProceduralPigeon';
import { animateRig, captureRig } from './PigeonAnimation';
import type { Rig } from './PigeonAnimation';
import type { PigeonRuntime, PigeonState } from '../../types/pigeon';
import { usePigeonStore } from '../../stores/pigeonStore';
export const MODEL_CONFIG = { url: '/models/pigeon.glb', height: 1.67, rotationY: 0 };
const CLIPS: Record<PigeonState, string> = {
  IDLE: 'Idle',
  WALK: 'Walk',
  RUN: 'Run',
  PECK: 'Peck',
  EAT: 'Eat',
  FLY: 'Fly',
  LAND: 'Land',
  LOOK_AROUND: 'LookAround',
  LOOK_AT_FOOD: 'LookAround',
  PANIC: 'Fly',
  HELD: 'Fly',
};
let modelPromise: Promise<GLTF | null> | undefined;
function loadOptionalModel() {
  return (modelPromise ??= (async () => {
    try {
      const response = await fetch(MODEL_CONFIG.url);
      if (!response.ok) return null;
      const bytes = await response.arrayBuffer();
      if (bytes.byteLength < 12 || new DataView(bytes).getUint32(0, true) !== 0x46546c67)
        return null;
      return await new GLTFLoader().parseAsync(bytes, '/models/');
    } catch {
      return null;
    }
  })());
}
function LoadedModel({ gltf, pigeon }: { gltf: GLTF; pigeon: PigeonRuntime }) {
  const root = useMemo(() => clone(gltf.scene), [gltf]);
  const normalized = useMemo(() => {
    const box = new Box3().setFromObject(root),
      size = box.getSize(new Vector3()),
      center = box.getCenter(new Vector3()),
      scale = MODEL_CONFIG.height / (size.y || 1);
    return {
      scale,
      position: [-center.x * scale, -box.min.y * scale, -center.z * scale] as [
        number,
        number,
        number,
      ],
    };
  }, [root]);
  const fallback = useRef<Group>(null);
  const mixer = useMemo(() => new AnimationMixer(root), [root]);
  const active = useRef('');
  const rig = useMemo(() => captureRig(root), [root]);
  useEffect(() => {
    root.traverse((obj) => {
      obj.castShadow = true;
      obj.receiveShadow = true;
    });
    return () => {
      mixer.stopAllAction();
      mixer.uncacheRoot(root);
    };
  }, [mixer, root]);
  useFrame((_, delta) => {
    const ui = usePigeonStore.getState();
    if (ui.paused) return;
    const name = CLIPS[pigeon.state];
    const clip = gltf.animations.find((c) => c.name.toLowerCase() === name.toLowerCase());
    if (active.current !== name) {
      mixer.stopAllAction();
      if (clip) mixer.clipAction(clip).reset().fadeIn(0.18).play();
      active.current = name;
    }
    if (clip) {
      mixer.update(Math.min(delta, 0.05) * ui.speed);
      if (fallback.current) {
        fallback.current.position.y = 0;
        fallback.current.rotation.x = 0;
      }
    } else {
      animateRig(rig, pigeon);
      if (fallback.current && !rig.Body) {
        fallback.current.position.y = Math.sin(pigeon.age * 2.2) * 0.012;
        fallback.current.rotation.x = ['PECK', 'EAT'].includes(pigeon.state)
          ? 0.6 + Math.sin(pigeon.age * 10) * 0.12
          : 0;
      }
    }
  });
  return (
    <group ref={fallback}>
      <group rotation={[0, MODEL_CONFIG.rotationY, 0]}>
        <group position={normalized.position} scale={normalized.scale}>
          <primitive object={root} />
        </group>
      </group>
    </group>
  );
}
export function PigeonModel({ pigeon }: { pigeon: PigeonRuntime }) {
  const ref = useRef<Group>(null),
    rig = useRef<Rig>({});
  const [model, setModel] = useState<GLTF | null>(null);
  useEffect(() => {
    let mounted = true;
    void loadOptionalModel().then((value) => {
      if (mounted) {
        setModel(value);
        usePigeonStore.getState().set({ modelStatus: value ? 'GLB モデル' : '手続きモデル' });
      }
    });
    return () => {
      mounted = false;
    };
  }, []);
  useLayoutEffect(() => {
    if (ref.current) rig.current = captureRig(ref.current);
  }, [model]);
  useFrame(() => {
    if (!model && !usePigeonStore.getState().paused) animateRig(rig.current, pigeon);
  });
  return model ? <LoadedModel gltf={model} pigeon={pigeon} /> : <ProceduralPigeon ref={ref} />;
}
