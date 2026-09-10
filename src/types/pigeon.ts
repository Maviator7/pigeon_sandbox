export type PigeonState =
  | 'IDLE'
  | 'LOOK_AROUND'
  | 'LOOK_AT_FOOD'
  | 'WALK'
  | 'RUN'
  | 'PECK'
  | 'EAT'
  | 'PANIC'
  | 'FLY'
  | 'LAND'
  | 'HELD';

// すべての状態は観察と交流のための非暴力的な行動です。
// PANIC は人の接近や操作に対する一時的な驚きで、捕食・攻撃・負傷を表しません。
export interface Vec3 {
  x: number;
  y: number;
  z: number;
}
export interface Needs {
  hunger: number;
  curiosity: number;
  fear: number;
  energy: number;
  trust: number;
}
export interface Food {
  id: string;
  position: Vec3;
  amount: number;
}
export interface PigeonRuntime {
  id: string;
  state: PigeonState;
  needs: Needs;
  position: Vec3;
  heading: number;
  speed: number;
  target: Vec3 | null;
  foodId: string | null;
  elapsed: number;
  duration: number;
  age: number;
  goal: string;
  gait: number;
  quirk: number;
  flightStart: Vec3;
  callTarget: Vec3 | null;
  eaten: number;
  transitions: { state: PigeonState; time: number }[];
}
export interface BrainContext {
  pigeon: PigeonRuntime;
  foods: readonly Food[];
  userDistance: number;
  random: () => number;
}
export interface Decision {
  state: PigeonState;
  goal: string;
  target?: Vec3;
  foodId?: string;
}
