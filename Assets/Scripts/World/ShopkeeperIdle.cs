using UnityEngine;

namespace Cove
{
    public class ShopkeeperIdle : MonoBehaviour
    {
        public float BobHeight = 0.022f;
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
                armRight.localRotation = armRightBase * Quaternion.Euler(Mathf.Sin(time * 1.4f) * 4f, 0f, -wave * 70f);
            if (armLeft != null)
                armLeft.localRotation = armLeftBase * Quaternion.Euler(-Mathf.Sin(time * 1.1f) * 3f, 0f, 0f);
            if (legLeft != null)
                legLeft.localRotation = legLeftBase * Quaternion.Euler(Mathf.Sin(time * 0.9f) * 1.5f, 0f, 0f);
            if (legRight != null)
                legRight.localRotation = legRightBase * Quaternion.Euler(-Mathf.Sin(time * 0.9f) * 1.5f, 0f, 0f);
        }
    }
}
