import { expect, it } from 'vitest';
import { Group, Vector3 } from 'three';
import { animateRig, captureRig } from '../src/components/Pigeon/PigeonAnimation';
import { createPigeon } from '../src/simulation/brain';
it('pecks near the ground without pushing the head through it', () => {
  const root = new Group(),
    body = new Group(),
    neck = new Group(),
    head = new Group();
  body.name = 'Body';
  neck.name = 'Neck';
  head.name = 'Head';
  neck.position.set(0, 0.99, 0.23);
  head.position.set(0, 0.4, 0.095);
  root.add(body);
  body.add(neck);
  neck.add(head);
  const rig = captureRig(root),
    pigeon = createPigeon();
  pigeon.state = 'EAT';
  pigeon.quirk = 0;
  let lowestBeak = Infinity;
  for (let i = 0; i < 120; i++) {
    pigeon.age = i / 60;
    animateRig(rig, pigeon);
    root.updateMatrixWorld(true);
    expect(head.getWorldPosition(new Vector3()).y - 0.235).toBeGreaterThanOrEqual(0);
    lowestBeak = Math.min(lowestBeak, head.localToWorld(new Vector3(0, -0.048, 0.265)).y);
  }
  expect(lowestBeak).toBeLessThan(0.12);
});
