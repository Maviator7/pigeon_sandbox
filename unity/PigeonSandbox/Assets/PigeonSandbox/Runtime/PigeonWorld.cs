using UnityEngine;

namespace PigeonSandbox
{
    /// <summary>Asset-free, smooth pigeon and a quiet park. Forward is positive Z.</summary>
    public sealed class PigeonWorld : MonoBehaviour
    {
        public Transform Bird { get; private set; }
        private Transform torso, neck, leftWing, rightWing, leftFoot, rightFoot;
        private Material seedMaterial;
        private bool initialized;

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            var grass = Material("Sage lawn", "849780");
            var sand = Material("Warm limestone", "D6CFB9");
            var stone = Material("Stone edge", "B1B7A1");
            var wood = Material("Oak", "967456");
            var metal = Material("Forest green steel", "354E46");
            var leaves = Material("Soft foliage", "627E67");
            var leavesLight = Material("Sunlit foliage", "8DAB7A");
            var bark = Material("Tree bark", "736250");
            seedMaterial = Material("Grain", "E6BD71");

            Shape("Lawn", PrimitiveType.Cylinder, transform, new Vector3(0, -.24f, 0), new Vector3(28, .2f, 28), grass);
            Shape("Promenade border", PrimitiveType.Cylinder, transform, new Vector3(0, -.13f, 0), new Vector3(16.4f, .1f, 16.4f), stone);
            Shape("Quiet walking court", PrimitiveType.Cylinder, transform, new Vector3(0, -.06f, 0), new Vector3(16, .06f, 16), sand);
            for (int i = 0; i < 7; i++)
            {
                float a = (i * 47 + 22) * Mathf.Deg2Rad;
                var position = new Vector3(Mathf.Cos(a) * 10.6f, 0, Mathf.Sin(a) * 10.6f);
                var tree = new GameObject("Park tree").transform;
                tree.SetParent(transform, false);
                tree.localPosition = position;
                Shape("Trunk", PrimitiveType.Cylinder, tree, new Vector3(0, 1.6f, 0), new Vector3(.38f, 1.65f, .38f), bark);
                Shape("Crown", PrimitiveType.Sphere, tree, new Vector3(0, 3.55f, 0), new Vector3(2.6f, 3.1f, 2.5f), i % 2 == 0 ? leaves : leavesLight);
                Shape("Crown lobe", PrimitiveType.Sphere, tree, new Vector3(.65f, 3.35f, .2f), new Vector3(1.8f, 2.2f, 1.8f), leaves);
            }
            Bench(new Vector3(-5.9f, 0, 6.7f), -35, wood, metal);
            Bench(new Vector3(5.7f, 0, 6.7f), 35, wood, metal);
            var basin = new GameObject("Bird bath").transform;
            basin.SetParent(transform, false);
            basin.localPosition = new Vector3(-8.5f, 0, -2.7f);
            Shape("Bath base", PrimitiveType.Cylinder, basin, new Vector3(0, .18f, 0), new Vector3(2.4f, .18f, 2.4f), stone);
            Shape("Still water", PrimitiveType.Cylinder, basin, new Vector3(0, .365f, 0), new Vector3(2.05f, .015f, 2.05f), Material("Water", "85AFAD", .55f));
            CreateBird();
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
            foreach (int side in new[] { -1, 1 })
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
                if (side < 0) { leftWing = pivot; leftFoot = foot; }
                else { rightWing = pivot; rightFoot = foot; }
            }
        }

        public void Animate(float time, float speed, bool flying, bool eating)
        {
            if (!initialized) return;
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
        }

        public GameObject CreateSeed(Vector3 pos)
        {
            var seed = Shape("Grain", PrimitiveType.Sphere, transform, new Vector3(pos.x, .038f, pos.z), new Vector3(.08f, .045f, .12f), seedMaterial);
            seed.localRotation = Quaternion.Euler(0, pos.x * 137 + pos.z * 73, 0);
            return seed.gameObject;
        }

        private void Bench(Vector3 position, float yaw, Material wood, Material metal)
        {
            var bench = new GameObject("Resting bench").transform;
            bench.SetParent(transform, false);
            bench.localPosition = position;
            bench.localRotation = Quaternion.Euler(0, yaw, 0);
            for (int i = 0; i < 3; i++)
            {
                Shape("Seat slat", PrimitiveType.Cube, bench, new Vector3(0, .67f, i * .19f), new Vector3(2.4f, .1f, .16f), wood);
                Shape("Back slat", PrimitiveType.Cube, bench, new Vector3(0, .96f + i * .19f, .47f), new Vector3(2.4f, .15f, .08f), wood);
            }
            foreach (int side in new[] { -1, 1 })
            {
                Shape("Bench leg", PrimitiveType.Cube, bench, new Vector3(side * .85f, .32f, .19f), new Vector3(.09f, .64f, .5f), metal);
                Shape("Back support", PrimitiveType.Cube, bench, new Vector3(side * .85f, 1.02f, .52f), new Vector3(.07f, .8f, .07f), metal);
            }
        }

        private static Material Material(string name, string hex, float smoothness = .13f, float metallic = 0)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            var material = new Material(Shader.Find("Standard")) { name = name, color = color };
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", metallic);
            return material;
        }

        private static Transform Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            var collider = obj.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return obj.transform;
        }

        private static void Limb(string name, Transform parent, Vector3 start, Vector3 end, float radius, Material material)
        {
            var limb = Shape(name, PrimitiveType.Cylinder, parent, (start + end) * .5f, new Vector3(radius * 2, Vector3.Distance(start, end) * .5f, radius * 2), material);
            limb.localRotation = Quaternion.FromToRotation(Vector3.up, end - start);
        }
    }
}
