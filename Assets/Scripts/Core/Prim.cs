using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// Tiny helper for building the low-poly scene from Unity primitives at runtime.
    /// Real Asset-Store / Blender meshes (GDD 7.x) can be dropped in later by
    /// swapping the MeshFilter/MeshRenderer on the generated objects; until then
    /// these flat-shaded primitives give the intended low-poly, warm-earth look.
    /// </summary>
    public static class Prim
    {
        private static Shader _shader;

        private static Shader LitShader
        {
            get
            {
                if (_shader == null)
                {
                    // URP lit shader; fall back to the built-in standard if absent.
                    _shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (_shader == null) _shader = Shader.Find("Standard");
                    if (_shader == null) _shader = Shader.Find("Sprites/Default");
                }
                return _shader;
            }
        }

        public static Material NewMaterial(Color color, float smoothness = 0.1f)
        {
            var mat = new Material(LitShader);
            // URP Lit uses _BaseColor; Standard uses _Color. Set both so either works.
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            mat.color = color;
            return mat;
        }

        public static GameObject Create(PrimitiveType type, string name, Transform parent,
            Vector3 position, Vector3 scale, Color color, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!collider)
            {
                var c = go.GetComponent<Collider>();
                if (c != null) Object.Destroy(c);
            }
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            if (r != null) r.sharedMaterial = NewMaterial(color);
            return go;
        }
    }
}
