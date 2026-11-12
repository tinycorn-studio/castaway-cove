using System.Collections.Generic;
using UnityEngine;

namespace Cove
{
    public enum CCFit
    {
        Height,
        Footprint
    }

    public static class CC0Library
    {
        private static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

        public static bool Available
        {
            get { return Prefab("tree_palm") != null; }
        }

        public static GameObject Prefab(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            GameObject go;
            if (cache.TryGetValue(name, out go) && go != null) return go;
            go = Resources.Load<GameObject>("CC0/" + name);
            cache[name] = go;
            return go;
        }

        public static GameObject SpawnLocal(string name, Transform parent, Vector3 localPos, float yaw, float target, CCFit mode = CCFit.Height, bool alignBase = true, Color? tint = null)
        {
            var prefab = Prefab(name);
            if (prefab == null) return null;

            var go = Object.Instantiate(prefab, parent, false);
            go.name = name;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one;

            float s = ScaleFor(go, parent, target, mode);
            go.transform.localScale = Vector3.one * s;
            if (tint.HasValue) Tint(go, tint.Value);

            Bounds b = LocalBounds(go, parent);
            Vector3 p = localPos;
            if (alignBase) p.y = localPos.y - b.min.y;
            go.transform.localPosition = p;
            return go;
        }

        public static GameObject SpawnHeld(string name, Transform parent, Vector3 localPos, Quaternion localRot, float targetHeight, Color tint)
        {
            var prefab = Prefab(name);
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, parent, false);
            go.name = name + " (held)";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = localRot;
            go.transform.localScale = Vector3.one;
            float s = ScaleFor(go, parent, targetHeight, CCFit.Height);
            go.transform.localScale = Vector3.one * s;
            Tint(go, tint);
            Bounds b = LocalBounds(go, parent);
            go.transform.localPosition = localPos;
            var anim = go.GetComponent<Animation>();
            if (anim != null) anim.Play();
            return go;
        }

        public static float ScaleFor(GameObject go, Transform parent, float target, CCFit mode)
        {
            Bounds b = LocalBounds(go, parent);
            float current = mode == CCFit.Height ? b.size.y : Mathf.Max(b.size.x, b.size.z);
            if (current < 0.00001f) return 1f;
            return target / current;
        }

        public static void SetTopLocalY(GameObject go, Transform parent, float topY)
        {
            Bounds b = LocalBounds(go, parent);
            Vector3 p = go.transform.localPosition;
            p.y += topY - b.max.y;
            go.transform.localPosition = p;
        }

        public static Bounds LocalBounds(GameObject go, Transform space)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            Bounds result = new Bounds(Vector3.zero, Vector3.zero);
            bool has = false;
            foreach (var r in renderers)
            {
                Bounds wb = r.bounds;
                Vector3 c = wb.center;
                Vector3 e = wb.extents;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = c + new Vector3(
                        (i & 1) == 0 ? -e.x : e.x,
                        (i & 2) == 0 ? -e.y : e.y,
                        (i & 4) == 0 ? -e.z : e.z);
                    Vector3 p = space.InverseTransformPoint(corner);
                    if (!has) { result = new Bounds(p, Vector3.zero); has = true; }
                    else result.Encapsulate(p);
                }
            }
            return result;
        }

        public static BoxCollider AddBoxCollider(GameObject go, float margin = 0f)
        {
            Bounds b = LocalBounds(go, go.transform);
            var col = go.AddComponent<BoxCollider>();
            col.center = b.center;
            col.size = b.size + Vector3.one * margin;
            return col;
        }

        public static void Tint(GameObject go, Color color)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var mats = renderers[i].materials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] != null && mats[m].HasProperty("_Color")) mats[m].SetColor("_Color", color);
                }
            }
        }

        public static void HideChild(GameObject go, string childName)
        {
            var t = go.transform.Find(childName);
            if (t != null) t.gameObject.SetActive(false);
        }

        public static GameObject SpawnWorld(string name, Transform parent, Vector3 worldPos, float yaw, float target, CCFit mode = CCFit.Height, bool alignBase = true, Color? tint = null)
        {
            var prefab = Prefab(name);
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, parent, false);
            go.name = name;
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one;
            Bounds b = WorldBounds(go);
            float current = mode == CCFit.Height ? b.size.y : Mathf.Max(b.size.x, b.size.z);
            float s = current < 0.00001f ? 1f : target / current;
            go.transform.localScale = Vector3.one * s;
            if (tint.HasValue) Tint(go, tint.Value);
            b = WorldBounds(go);
            if (alignBase) go.transform.position += new Vector3(0f, worldPos.y - b.min.y, 0f);
            return go;
        }

        public static Bounds WorldBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            Bounds b = new Bounds(go.transform.position, Vector3.zero);
            bool has = false;
            foreach (var r in renderers)
            {
                if (!has) { b = r.bounds; has = true; }
                else b.Encapsulate(r.bounds);
            }
            return b;
        }
    }

    public class IdleBob : MonoBehaviour
    {
        public float Height = 0.02f;
        public float Speed = 1.6f;
        public float SwayDegrees = 3f;
        public float Phase;

        private Vector3 basePos;
        private Quaternion baseRot;
        private float t;

        private void Start()
        {
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
            t = Phase;
        }

        private void Update()
        {
            t += Time.deltaTime * Speed;
            transform.localPosition = basePos + Vector3.up * (Mathf.Sin(t) * Height);
            transform.localRotation = baseRot * Quaternion.Euler(0f, Mathf.Sin(t * 0.55f) * SwayDegrees, 0f);
        }
    }
}
