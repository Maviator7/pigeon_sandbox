using UnityEngine;

namespace PigeonSandbox
{
    /// <summary>Asset-free procedural pigeon built from primitives. Forward is positive Z.</summary>
    public sealed class PigeonWorld : MonoBehaviour
    {
        public Transform Bird
        {
            get;
            private set;
        }

        private Transform torso, neck, leftWing, rightWing, leftFoot, rightFoot;
        private bool initialized;
        public void Initialize(bool white, bool mayor, Plumage feather = Plumage.Blue)
        {
            if (initialized)
                return;
            initialized = true;
            CreateBird();
            if (white)
                feather = Plumage.White;
            foreach (var renderer in Bird.GetComponentsInChildren<Renderer>())
            {
                string part = renderer.gameObject.name;
                bool wing = part == "Folded wing", body = part == "Pear shaped breast" || part == "Soft chest", head = part == "Round head", dark = part == "Broad wing bar" || part == "Flight tip" || part == "Smooth tail";
                string hex = null;
                if (feather == Plumage.White && (wing || body || head))
                    hex = "EFE9D8";
                if (feather == Plumage.Brown)
                    hex = wing ? "BE9471" : body ? "9C755E" : head ? "785848" : dark ? "594638" : null;
                if (feather == Plumage.Checker)
                    hex = wing ? "586470" : body ? "788490" : dark ? "303B47" : null;
                if (feather == Plumage.Pied)
                    hex = wing || body ? "EEEDE3" : head || dark ? "303B47" : null;
                if (hex != null)
                    renderer.sharedMaterial = Material("Plumage " + feather, hex);
            }

            if (mayor)
            {
                var gold = Material("Mayor gold", "D4AA50");
                Shape("Mayor medallion", PrimitiveType.Sphere, Bird, new Vector3(0, 1.12f, .53f), new Vector3(.16f, .18f, .045f), gold);
            }

            // One mesh per animated pivot keeps the part animation while cutting ~40 renderers to 7.
            foreach (var pivot in new[]{Bird, torso, neck, leftWing, rightWing, leftFoot, rightFoot})
                MeshBaker.Bake(pivot, false);
        }

        private void CreateBird()
        {
            Bird = new GameObject("Pigeon").transform;
            Bird.SetParent(transform, false);
            torso = new GameObject("Breathing body").transform;
            torso.SetParent(Bird, false);
            var gray = Material("Blue grey feathers", "7D8B96");
            var wing = Material("Silver grey wings", "A2AEB4");
            var dark = Material("Charcoal flight feathers", "3F4D59");
            var head = Material("Slate head", "586B78");
            var green = Material("Emerald neck sheen", "3E7A70", .38f, .25f);
            var purple = Material("Violet breast sheen", "716278", .32f, .16f);
            var feet = Material("Rose feet", "B47878");
            var black = Material("Pupil and beak", "263039", .25f);
            var orange = Material("Amber iris", "DC8B40");
            var white = Material("Ivory cere", "E3E4D8");
            Shape("Pear shaped breast", PrimitiveType.Sphere, torso, new Vector3(0, .76f, -.08f), new Vector3(.82f, .92f, 1.15f), gray);
            Shape("Soft chest", PrimitiveType.Sphere, torso, new Vector3(0, .94f, .25f), new Vector3(.66f, .73f, .63f), gray);
            var tail = Shape("Smooth tail", PrimitiveType.Sphere, torso, new Vector3(0, .51f, -.84f), new Vector3(.46f, .095f, .92f), dark);
            tail.localRotation = Quaternion.Euler(12, 0, 0);
            neck = new GameObject("Head and neck").transform;
            neck.SetParent(torso, false);
            neck.localPosition = new Vector3(0, 1.04f, .29f);
            Shape("Violet collar", PrimitiveType.Sphere, neck, new Vector3(0, .065f, 0), new Vector3(.51f, .48f, .46f), purple);
            Shape("Green throat", PrimitiveType.Sphere, neck, new Vector3(0, .24f, .025f), new Vector3(.40f, .55f, .38f), green);
            Shape("Round head", PrimitiveType.Sphere, neck, new Vector3(0, .49f, .075f), new Vector3(.43f, .43f, .47f), head);
            var beak = Shape("Beak", PrimitiveType.Sphere, neck, new Vector3(0, .414f, .34f), new Vector3(.13f, .105f, .30f), black);
            beak.localRotation = Quaternion.Euler(16, 0, 0);
            Shape("Cere", PrimitiveType.Sphere, neck, new Vector3(0, .47f, .294f), new Vector3(.14f, .088f, .135f), white);
            foreach (int side in new[]{-1, 1})
            {
                Shape("Eye rim", PrimitiveType.Sphere, neck, new Vector3(side * .194f, .514f, .137f), new Vector3(.028f, .094f, .094f), gray);
                Shape("Amber eye", PrimitiveType.Sphere, neck, new Vector3(side * .21f, .514f, .14f), new Vector3(.023f, .072f, .072f), orange);
                Shape("Pupil", PrimitiveType.Sphere, neck, new Vector3(side * .221f, .514f, .145f), new Vector3(.015f, .041f, .044f), black);
                Shape("Eye light", PrimitiveType.Sphere, neck, new Vector3(side * .23f, .529f, .153f), new Vector3(.009f, .012f, .012f), white);
                var pivot = new GameObject(side < 0 ? "Left wing" : "Right wing").transform;
                pivot.SetParent(torso, false);
                pivot.localPosition = new Vector3(side * .28f, .98f, -.08f);
                Shape("Folded wing", PrimitiveType.Sphere, pivot, new Vector3(side * .095f, -.16f, -.22f), new Vector3(.24f, .57f, .97f), wing);
                Shape("Flight tip", PrimitiveType.Sphere, pivot, new Vector3(side * .13f, -.24f, -.54f), new Vector3(.18f, .30f, .59f), dark);
                for (int bar = 0; bar < 2; bar++)
                {
                    var stripe = Shape("Broad wing bar", PrimitiveType.Sphere, pivot, new Vector3(side * .205f, -.16f, -.2f - bar * .17f), new Vector3(.025f, .45f - bar * .075f, .09f), dark);
                    stripe.localRotation = Quaternion.Euler(-13, 0, 0);
                }

                var foot = new GameObject(side < 0 ? "Left foot" : "Right foot").transform;
                foot.SetParent(Bird, false);
                foot.localPosition = new Vector3(side * .20f, .06f, .10f);
                Limb("Leg", foot, new Vector3(0, .02f, 0), new Vector3(0, .38f, -.035f), .034f, feet);
                for (int toe = -1; toe <= 1; toe++)
                    Limb("Toe", foot, Vector3.zero, new Vector3(toe * .085f, -.015f, .19f - Mathf.Abs(toe) * .025f), .023f, feet);
                Limb("Back toe", foot, Vector3.zero, new Vector3(side * .025f, -.015f, -.105f), .022f, feet);
                if (side < 0)
                {
                    leftWing = pivot;
                    leftFoot = foot;
                }
                else
                {
                    rightWing = pivot;
                    rightFoot = foot;
                }
            }
        }

        public void Animate(float time, float speed, bool flying, bool eating, string activity = null)
        {
            if (!initialized)
                return;
            float walk = flying || eating ? 0 : Mathf.Clamp01(speed * 1.8f);
            float step = Mathf.Sin(time * 12);
            torso.localPosition = new Vector3(0, Mathf.Abs(step) * .025f * walk + Mathf.Sin(time * 2) * .009f, 0);
            torso.localRotation = Quaternion.Euler(flying ? 20 : eating ? 19 : 0, 0, 0);
            neck.localRotation = Quaternion.Euler(eating ? 51 + Mathf.Sin(time * 13) * 13 : 0, !flying && walk < .1f ? Mathf.Sin(time * .8f) * 13 : 0, 0);
            neck.localPosition = new Vector3(0, 1.04f - (eating ? .11f : 0), .29f + step * .055f * walk);
            float flap = flying ? 76 + Mathf.Sin(time * 17) * 49 : 0;
            leftWing.localRotation = Quaternion.Euler(0, flying ? -12 : 0, -flap);
            rightWing.localRotation = Quaternion.Euler(0, flying ? 12 : 0, flap);
            leftFoot.localRotation = Quaternion.Euler(flying ? -65 : step * 24 * walk, 0, 0);
            rightFoot.localRotation = Quaternion.Euler(flying ? -65 : -step * 24 * walk, 0, 0);
            leftFoot.localPosition = new Vector3(-.20f, .06f + Mathf.Max(0, step) * .085f * walk, .10f + step * .07f * walk);
            rightFoot.localPosition = new Vector3(.20f, .06f + Mathf.Max(0, -step) * .085f * walk, .10f - step * .07f * walk);
            if (!flying && activity == "羽繕い")
            {
                neck.localRotation = Quaternion.Euler(24 + Mathf.Sin(time * 4) * 9, 62 + Mathf.Sin(time * 1.7f) * 12, 0);
                rightWing.localRotation = Quaternion.Euler(0, 0, 12 + Mathf.Sin(time * 2) * 4);
            }
            else if (!flying && activity == "日向ぼっこ")
            {
                torso.localPosition += new Vector3(0, -.045f, 0);
                torso.localRotation = Quaternion.Euler(0, 0, -7);
                neck.localRotation = Quaternion.Euler(-9, Mathf.Sin(time * .5f) * 8, 5);
                leftWing.localRotation = Quaternion.Euler(0, 0, -16);
                rightWing.localRotation = Quaternion.Euler(0, 0, 16);
            }
        }

        // Names document each part's colour; materials are shared through MaterialCache.
        private static Material Material(string name, string hex, float smoothness = .13f, float metallic = 0) => MaterialCache.Get(hex, smoothness, metallic);
        private static Transform Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            var collider = obj.GetComponent<Collider>();
            if (collider != null)
                MaterialCache.Release(collider);
            return obj.transform;
        }

        private static void Limb(string name, Transform parent, Vector3 start, Vector3 end, float radius, Material material)
        {
            var limb = Shape(name, PrimitiveType.Cylinder, parent, (start + end) * .5f, new Vector3(radius * 2, Vector3.Distance(start, end) * .5f, radius * 2), material);
            limb.localRotation = Quaternion.FromToRotation(Vector3.up, end - start);
        }
    }
}
