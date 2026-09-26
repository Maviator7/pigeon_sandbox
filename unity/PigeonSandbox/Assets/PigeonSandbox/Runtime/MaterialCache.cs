using System.Collections.Generic;
using UnityEngine;

namespace PigeonSandbox
{
    // One shared Standard material per color and surface, reused by facilities, pigeons and visitors.
    public static class MaterialCache
    {
        static readonly Dictionary<(string hex, float smoothness, float metallic), Material> materials = new Dictionary<(string, float, float), Material>();
        public static int Count => materials.Count;
        public static Material Get(string hex, float smoothness = .12f, float metallic = 0)
        {
            var key = (hex, smoothness, metallic);
            if (materials.TryGetValue(key, out var material) && material != null)
                return material;
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            material = new Material(Shader.Find("Standard"))
            {name = "Pigeon Sandbox " + hex, color = color};
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", metallic);
            materials[key] = material;
            return material;
        }

        // Destroy works only in Play mode; Editor checks build worlds in edit mode.
        public static void Release(Object target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }
    }
}
