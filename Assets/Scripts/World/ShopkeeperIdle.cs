using UnityEngine;

namespace Cove
{
    public class ShopkeeperIdle : MonoBehaviour
    {
        public float BobHeight = 0.08f;
        public float Speed = 1.4f;
        public float WaveEvery = 7f;

        private Transform torso;
        private Transform head;
        private Transform armLeft;
        private Transform armRight;
        private Transform legLeft;
        private Transform legRight;
        private Vector3 basePosition;
        private Quaternion torsoBase;
        private Quaternion headBase;
        private Quaternion armLeftBase;
        private Quaternion armRightBase;
        private Quaternion legLeftBase;
        private Quaternion legRightBase;
        private float time;
        private Transform bubble;
        private float npcHeight = 1.7f;

        private void Start()
        {
            basePosition = transform.localPosition;
            torso = FindDeep(transform, "torso");
            head = FindDeep(transform, "head");
            armLeft = FindDeep(transform, "arm-left");
            armRight = FindDeep(transform, "arm-right");
            legLeft = FindDeep(transform, "leg-left");
            legRight = FindDeep(transform, "leg-right");
            if (torso != null) torsoBase = torso.localRotation;
            if (head != null) headBase = head.localRotation;
            if (armLeft != null) armLeftBase = armLeft.localRotation;
            if (armRight != null) armRightBase = armRight.localRotation;
            if (legLeft != null) legLeftBase = legLeft.localRotation;
            if (legRight != null) legRightBase = legRight.localRotation;
            BuildBubble();
        }

        private void BuildBubble()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                npcHeight = Mathf.Max(1f, b.size.y);
            }

            var go = new GameObject("Greeting Bubble");
            go.transform.SetParent(transform.parent, false);
            bubble = go.transform;

            var panel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(panel.GetComponent<Collider>());
            panel.transform.SetParent(bubble, false);
            panel.transform.localScale = new Vector3(0.58f, 0.36f, 0.1f);
            panel.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("greetingBubble", new Color32(0xFF, 0xF4, 0xD8, 0xFF), 0.3f);

            var tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(tail.GetComponent<Collider>());
            tail.transform.SetParent(bubble, false);
            tail.transform.localPosition = new Vector3(-0.1f, -0.22f, 0f);
            tail.transform.localScale = new Vector3(0.14f, 0.14f, 0.06f);
            tail.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tail.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("greetingTail", new Color32(0xFF, 0xF4, 0xD8, 0xFF), 0.3f);

            var textGo = new GameObject("Greeting Text");
            textGo.transform.SetParent(bubble, false);
            textGo.transform.localPosition = new Vector3(0f, 0f, -0.05f);
            var tm = textGo.AddComponent<TextMesh>();
            tm.text = "HI!";
            tm.fontSize = 48;
            tm.characterSize = 0.065f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color32(0x25, 0x38, 0x4A, 0xFF);
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            if (f != null)
            {
                tm.font = f;
                textGo.GetComponent<MeshRenderer>().sharedMaterial = f.material;
            }
            bubble.gameObject.SetActive(false);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private void Update()
        {
            time += Time.deltaTime * Speed;
            float bob = Mathf.Sin(time * 1.7f) * BobHeight;
            transform.localPosition = basePosition + Vector3.up * bob;
            transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(time * 0.45f) * 4f, 0f);

            if (torso != null)
                torso.localRotation = torsoBase * Quaternion.Euler(0f, Mathf.Sin(time * 0.8f) * 4f, Mathf.Sin(time * 1.2f) * 1.5f);
            if (head != null)
                head.localRotation = headBase * Quaternion.Euler(Mathf.Sin(time * 1.5f) * 3f, Mathf.Sin(time * 0.55f) * 9f, 0f);

            float wavePhase = Mathf.Repeat(Time.time, WaveEvery);
            float wave = 0f;
            if (wavePhase > WaveEvery - 1.5f)
                wave = Mathf.Sin((wavePhase - (WaveEvery - 1.5f)) / 1.5f * Mathf.PI);

            if (armRight != null)
                armRight.localRotation = armRightBase * Quaternion.Euler(Mathf.Sin(time * 1.4f) * 6f, 0f, -wave * 85f);
            if (armLeft != null)
                armLeft.localRotation = armLeftBase * Quaternion.Euler(-Mathf.Sin(time * 1.1f) * 4f, 0f, 0f);
            if (legLeft != null)
                legLeft.localRotation = legLeftBase * Quaternion.Euler(Mathf.Sin(time * 0.9f) * 2f, 0f, 0f);
            if (legRight != null)
                legRight.localRotation = legRightBase * Quaternion.Euler(-Mathf.Sin(time * 0.9f) * 2f, 0f, 0f);

            if (bubble != null)
            {
                bubble.gameObject.SetActive(true);
                Vector3 toCam = Vector3.zero;
                var cam = Camera.main;
                if (cam != null)
                {
                    toCam = cam.transform.position - transform.position;
                    toCam.y = 0f;
                    if (toCam.sqrMagnitude > 0.01f) toCam.Normalize();
                }
                bubble.position = transform.position + Vector3.up * (npcHeight + 0.5f + Mathf.Sin(Time.time * 3.5f) * 0.06f) + toCam * 1.8f;
                float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.06f;
                bubble.localScale = Vector3.one * pulse;
                if (cam != null) bubble.rotation = Quaternion.LookRotation(cam.transform.position - bubble.position);
            }
        }
    }
}
