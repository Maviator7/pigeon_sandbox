import { forwardRef, useEffect, useMemo } from 'react';
import { createPigeonSurfaces } from './pigeonSurfaces';
import type { Group } from 'three';

/** +Z is forward; origin is between the feet. Named groups are the procedural rig. */
export const ProceduralPigeon = forwardRef<Group>(function ProceduralPigeon(_, ref) {
  const surfaces = useMemo(createPigeonSurfaces, []);
  useEffect(() => () => { surfaces.wing.dispose(); surfaces.neck.dispose(); }, [surfaces]);
  return (
    <group ref={ref}>
      <group name="Body">
        <mesh
          position={[0, 0.68, -0.06]}
          rotation={[0.2, 0, 0]}
          scale={[0.35, 0.48, 0.49]}
          castShadow
        >
          <sphereGeometry args={[1, 28, 20]} />
          <meshStandardMaterial color="#818b97" roughness={0.87} />
        </mesh>
        <mesh position={[0, 0.76, 0.21]} scale={[0.305, 0.39, 0.3]} castShadow>
          <sphereGeometry args={[1, 24, 18]} />
          <meshStandardMaterial color="#788390" roughness={0.8} />
        </mesh>
        <group name="Neck" position={[0, 0.99, 0.23]}>
          <mesh
            position={[0, 0.12, 0]}
            rotation={[-0.18, 0, 0]}
            scale={[0.218, 0.35, 0.215]}
            geometry={surfaces.neck}
            dispose={null}
            castShadow
          >
            <meshPhysicalMaterial vertexColors roughness={0.7} metalness={0.08} iridescence={0.3} />
          </mesh>
          <group name="Head" position={[0, 0.4, 0.095]}>
            <mesh scale={[0.224, 0.235, 0.24]} castShadow>
              <sphereGeometry args={[1, 28, 20]} />
              <meshStandardMaterial color="#616e7c" roughness={0.75} />
            </mesh>
            <mesh position={[0, -0.048, 0.265]} rotation={[Math.PI / 2 + 0.2, 0, 0]} castShadow>
              <coneGeometry args={[0.07, 0.25, 10]} />
              <meshStandardMaterial color="#343c46" />
            </mesh>
            <mesh position={[0, 0.006, 0.227]} scale={[0.075, 0.043, 0.064]}>
              <sphereGeometry args={[1, 16, 10]} />
              <meshStandardMaterial color="#ece8d9" roughness={0.82} />
            </mesh>
            {[-1, 1].map((side) => (
              <group
                key={side}
                position={[side * 0.198, 0.037, 0.094]}
                rotation={[0, side * 1.14, 0]}
              >
                <mesh scale={[1, 1, 0.32]}>
                  <sphereGeometry args={[0.048, 18, 12]} />
                  <meshStandardMaterial color="#a7a9a0" />
                </mesh>
                <mesh position={[0, 0, 0.017]} scale={[1, 1, 0.28]}>
                  <sphereGeometry args={[0.035, 18, 12]} />
                  <meshStandardMaterial color="#db8c4a" />
                </mesh>
                <mesh position={[0, 0, 0.03]} scale={[1, 1, 0.28]}>
                  <sphereGeometry args={[0.019, 16, 12]} />
                  <meshStandardMaterial color="#152126" roughness={0.15} />
                </mesh>
                <mesh position={[-0.009, 0.011, 0.036]}>
                  <sphereGeometry args={[0.006, 8, 8]} />
                  <meshBasicMaterial color="white" />
                </mesh>
              </group>
            ))}
          </group>
        </group>
        {[-1, 1].map((side) => (
          <group
            key={side}
            name={side < 0 ? 'WingL' : 'WingR'}
            position={[side * 0.285, 0.88, 0.06]}
          >
            <mesh
              position={[side * 0.013, -0.13, -0.25]}
              rotation={[0.45, 0, side * 0.09]}
              scale={[0.105, 0.29, 0.52]}
              geometry={surfaces.wing}
              dispose={null}
              castShadow
            >
              <meshStandardMaterial vertexColors roughness={0.88} />
            </mesh>
          </group>
        ))}
        <group name="Tail" position={[0, 0.43, -0.46]} rotation={[0.25, 0, 0]}>
          <mesh position={[0, 0, -0.21]} scale={[0.20, 0.035, 0.35]} castShadow>
            <sphereGeometry args={[1, 32, 16]} />
            <meshStandardMaterial color="#616e7b" roughness={0.9} />
          </mesh>
        </group>
      </group>
      {[-1, 1].map((side) => (
        <group key={side} name={side < 0 ? 'LegL' : 'LegR'} position={[side * 0.14, 0.32, 0.01]}>
          <mesh position={[0, -0.11, 0]} rotation={[0.12, 0, 0]} castShadow>
            <cylinderGeometry args={[0.022, 0.026, 0.24, 8]} />
            <meshStandardMaterial color="#bf8290" />
          </mesh>
          {[-1, 0, 1].map((toe) => (
            <mesh
              key={toe}
              position={[toe * 0.035, -0.255, 0.07]}
              rotation={[Math.PI / 2, 0, -toe * 0.4]}
              castShadow
            >
              <capsuleGeometry args={[0.014, 0.17, 3, 6]} />
              <meshStandardMaterial color="#c18a95" />
            </mesh>
          ))}
          <mesh position={[0, -0.25, -0.045]} rotation={[Math.PI / 2, 0, 0.1]} castShadow>
            <capsuleGeometry args={[0.012, 0.09, 3, 6]} />
            <meshStandardMaterial color="#af7b89" />
          </mesh>
        </group>
      ))}
    </group>
  );
});
