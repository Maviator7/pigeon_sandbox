import { useEffect, useRef } from 'react';
import { useFrame, useThree } from '@react-three/fiber';
import type { ThreeEvent } from '@react-three/fiber';
import { Html } from '@react-three/drei';
import { Group, Plane, Raycaster, Vector2, Vector3 } from 'three';
import { PigeonModel } from './PigeonModel';
import type { PigeonRuntime } from '../../types/pigeon';
import { advance, releasePigeon, touchPigeon, transition } from '../../simulation/controller';
import { clampToPark } from '../../simulation/park';
import { STATE_LABELS } from '../../simulation/brain';
import { usePigeonStore } from '../../stores/pigeonStore';
import { useFoodStore } from '../../stores/foodStore';
import { soundManager } from '../../audio/SoundManager';

export function Pigeon({ pigeon }: { pigeon: PigeonRuntime }) {
  const group = useRef<Group>(null),
    publishTimer = useRef(0);
  const { camera, gl } = useThree();
  const pointer = useRef<{
    id: number;
    x: number;
    y: number;
    plane: Plane;
    offset: Vector3;
    dragged: boolean;
  } | null>(null);
  const snapshot = usePigeonStore((s) => s.snapshot),
    selected = usePigeonStore((s) => s.selected);
  useFrame((_, delta) => {
    const ui = usePigeonStore.getState();
    const dt = Math.min(delta, 0.05) * ui.speed;
    if (!ui.paused) {
      const events = advance(pigeon, useFoodStore.getState().foods, dt);
      for (const id of events.eaten) useFoodStore.getState().remove(id);
      for (const name of events.sounds) soundManager.play(name);
    }
    if (group.current) {
      group.current.position.set(pigeon.position.x, pigeon.position.y, pigeon.position.z);
      group.current.rotation.y = pigeon.heading;
    }
    publishTimer.current += delta;
    if (publishTimer.current >= 0.2) {
      publishTimer.current = 0;
      ui.publish(pigeon);
    }
  });
  useEffect(() => {
    const raycaster = new Raycaster(),
      ndc = new Vector2(),
      point = new Vector3();
    const end = () => {
      const active = pointer.current;
      if (!active) return;
      if (active.dragged) releasePigeon(pigeon);
      else touchPigeon(pigeon);
      if (gl.domElement.hasPointerCapture(active.id))
        gl.domElement.releasePointerCapture(active.id);
      pointer.current = null;
      usePigeonStore.getState().set({ dragging: false });
      usePigeonStore.getState().publish(pigeon);
    };
    const move = (event: PointerEvent) => {
      const active = pointer.current;
      if (!active || event.pointerId !== active.id) return;
      if (!active.dragged && Math.hypot(event.clientX - active.x, event.clientY - active.y) < 6)
        return;
      if (!active.dragged) {
        active.dragged = true;
        transition(pigeon, { state: 'HELD', goal: 'わ、足が地面についてない！' });
        pigeon.needs.fear = Math.min(100, pigeon.needs.fear + 14);
      }
      const rect = gl.domElement.getBoundingClientRect();
      ndc.set(
        ((event.clientX - rect.left) / rect.width) * 2 - 1,
        (-(event.clientY - rect.top) / rect.height) * 2 + 1,
      );
      raycaster.setFromCamera(ndc, camera);
      if (raycaster.ray.intersectPlane(active.plane, point)) {
        point.add(active.offset);
        pigeon.position = clampToPark({
          x: point.x,
          y: Math.max(0.06, Math.min(5, point.y)),
          z: point.z,
        });
      }
    };
    const up = (e: PointerEvent) => {
      if (pointer.current?.id === e.pointerId) end();
    };
    window.addEventListener('pointermove', move);
    window.addEventListener('pointerup', up);
    window.addEventListener('pointercancel', up);
    window.addEventListener('blur', end);
    return () => {
      window.removeEventListener('pointermove', move);
      window.removeEventListener('pointerup', up);
      window.removeEventListener('pointercancel', up);
      window.removeEventListener('blur', end);
      if (pointer.current && gl.domElement.hasPointerCapture(pointer.current.id))
        gl.domElement.releasePointerCapture(pointer.current.id);
      usePigeonStore.getState().set({ dragging: false });
    };
  }, [camera, gl, pigeon]);
  function down(e: ThreeEvent<PointerEvent>) {
    e.stopPropagation();
    if (e.button !== 0) return;
    const normal = camera.getWorldDirection(new Vector3());
    normal.y = 0;
    normal.normalize();
    const plane = new Plane().setFromNormalAndCoplanarPoint(normal, e.point);
    pointer.current = {
      id: e.pointerId,
      x: e.clientX,
      y: e.clientY,
      plane,
      offset: new Vector3(pigeon.position.x, pigeon.position.y, pigeon.position.z).sub(e.point),
      dragged: false,
    };
    gl.domElement.setPointerCapture(e.pointerId);
    usePigeonStore.getState().set({ selected: true, dragging: true, feeding: false });
  }
  return (
    <group
      ref={group}
      position={[pigeon.position.x, pigeon.position.y, pigeon.position.z]}
      rotation={[0, pigeon.heading, 0]}
    >
      <PigeonModel pigeon={pigeon} />
      <mesh
        position={[0, 0.85, -0.02]}
        onPointerDown={down}
        onClick={(e) => e.stopPropagation()}
        onDoubleClick={(e) => {
          e.stopPropagation();
          const ui = usePigeonStore.getState();
          ui.set({ follow: !ui.follow, selected: true });
        }}
        onPointerOver={(e) => {
          e.stopPropagation();
          gl.domElement.style.cursor = 'grab';
        }}
        onPointerOut={() => {
          gl.domElement.style.cursor = '';
        }}
      >
        <boxGeometry args={[0.85, 1.8, 1.25]} />
        <meshBasicMaterial transparent opacity={0} depthWrite={false} />
      </mesh>
      {selected && (
        <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.018 - pigeon.position.y, 0]}>
          <ringGeometry args={[0.57, 0.59, 48]} />
          <meshBasicMaterial color="#597960" transparent opacity={0.6} />
        </mesh>
      )}
      <Html position={[0, 2.06, 0]} center style={{ pointerEvents: 'none' }} zIndexRange={[5, 0]}>
        <div className="pigeon-label">
          <span className="status-dot" />
          {STATE_LABELS[snapshot.state]}
        </div>
      </Html>
    </group>
  );
}
