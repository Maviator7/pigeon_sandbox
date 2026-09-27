using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PigeonSandbox
{
    // Merges the primitive parts under a transform into one mesh drawn with the shared vertex-colour material.
    // Models are still authored as primitives; baking turns ~5-40 draw calls per object into one.
    public static class MeshBaker
    {
        static Material shared;
        static readonly List<MeshRenderer> renderers = new List<MeshRenderer>();
        static readonly List<Vector3> positions = new List<Vector3>(), normals = new List<Vector3>(), scratchVectors = new List<Vector3>();
        static readonly List<Color> colors = new List<Color>();
        static readonly List<Vector2> metallic = new List<Vector2>();
        static readonly List<int> triangles = new List<int>(), scratchIndices = new List<int>();
        public static int MeshesBaked
        {
            get;
            private set;
        }

        public static Material Material
        {
            get
            {
                if (shared == null)
                    shared = new Material(Shader.Find("PigeonSandbox/Vertex Color Lit"))
                    {name = "Pigeon Sandbox baked"};
                return shared;
            }
        }

        // recursive=false bakes only direct children, leaving animated child pivots (and their parts) intact.
        public static void Bake(Transform root, bool recursive)
        {
            renderers.Clear();
            if (recursive)
                root.GetComponentsInChildren(true, renderers);
            else
                foreach (Transform child in root)
                    if (child.TryGetComponent(out MeshRenderer renderer))
                        renderers.Add(renderer);
            if (root.TryGetComponent(out MeshRenderer own))
                renderers.Remove(own);
            if (renderers.Count == 0)
                return;
            positions.Clear();
            normals.Clear();
            colors.Clear();
            metallic.Clear();
            triangles.Clear();
            var toRoot = root.worldToLocalMatrix;
            foreach (var renderer in renderers)
            {
                var source = renderer.GetComponent<MeshFilter>().sharedMesh;
                var material = renderer.sharedMaterial;
                var matrix = toRoot * renderer.transform.localToWorldMatrix;
                var normalMatrix = matrix.inverse.transpose;
                var color = material.color;
                color.a = material.GetFloat("_Glossiness");
                var surface = new Vector2(material.GetFloat("_Metallic"), 0);
                int offset = positions.Count;
                source.GetVertices(scratchVectors);
                foreach (var vertex in scratchVectors)
                    positions.Add(matrix.MultiplyPoint3x4(vertex));
                source.GetNormals(scratchVectors);
                foreach (var normal in scratchVectors)
                    normals.Add(normalMatrix.MultiplyVector(normal).normalized);
                for (int i = 0; i < source.vertexCount; i++)
                {
                    colors.Add(color);
                    metallic.Add(surface);
                }

                for (int sub = 0; sub < source.subMeshCount; sub++)
                {
                    source.GetTriangles(scratchIndices, sub);
                    foreach (int index in scratchIndices)
                        triangles.Add(offset + index);
                }
            }

            var mesh = new Mesh{name = root.name + " (baked)", indexFormat = positions.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16};
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetUVs(0, metallic);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            foreach (var renderer in renderers)
                MaterialCache.Release(renderer.gameObject);
            // Unity objects override ==, so avoid ?? when looking up components.
            if (!root.TryGetComponent(out MeshFilter filter))
                filter = root.gameObject.AddComponent<MeshFilter>();
            if (filter.sharedMesh != null)
                MaterialCache.Release(filter.sharedMesh);
            filter.sharedMesh = mesh;
            if (!root.TryGetComponent(out MeshRenderer target))
                target = root.gameObject.AddComponent<MeshRenderer>();
            target.sharedMaterial = Material;
            MeshesBaked++;
        }

        // Baked meshes are owned by their object; release them with it.
        public static void Release(GameObject target)
        {
            if (target == null)
                return;
            foreach (var filter in target.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh != null && filter.sharedMesh.name.EndsWith(" (baked)"))
                    MaterialCache.Release(filter.sharedMesh);
            MaterialCache.Release(target);
        }
    }
}
