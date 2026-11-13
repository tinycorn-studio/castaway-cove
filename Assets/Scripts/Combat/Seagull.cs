using UnityEngine;

namespace Cove
{
    public class Seagull : CombatTarget
    {
        public int HpValue = 20;
        public bool Dead;

        private GameBoot game;
        private Vector3 center;
        private float radius = 18f;
        private float height = 14f;
        private float speed = 0.45f;
        private float phase;
        private Transform leftWing;
        private Transform rightWing;

        public override string TargetName { get { return "SEAGULL"; } }
        public override int Hp { get { return HpValue; } }
        public override int MaxHp { get { return 20; } }
        public override bool IsDead { get { return Dead; } }

        public void Init(GameBoot g, Vector3 orbitCenter, float startPhase)
        {
            game = g;
            center = orbitCenter;
            phase = startPhase;
            BuildVisual();
            UpdatePosition();
        }

        private void BuildVisual()
        {
            Color white = new Color32(0xF4, 0xF2, 0xEA, 0xFF);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(0.18f, 0.35f, 0.18f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("gullBody", white, 0.4f);

            leftWing = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            Destroy(leftWing.GetComponent<Collider>());
            leftWing.SetParent(transform, false);
            leftWing.localPosition = new Vector3(-0.35f, 0.05f, 0f);
            leftWing.localScale = new Vector3(0.6f, 0.03f, 0.22f);
            leftWing.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("gullWing", white, 0.4f);

            rightWing = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            Destroy(rightWing.GetComponent<Collider>());
            rightWing.SetParent(transform, false);
            rightWing.localPosition = new Vector3(0.35f, 0.05f, 0f);
            rightWing.localScale = new Vector3(0.6f, 0.03f, 0.22f);
            rightWing.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("gullWing", white, 0.4f);

            var beak = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(beak.GetComponent<Collider>());
            beak.transform.SetParent(transform, false);
            beak.transform.localPosition = new Vector3(0f, 0f, 0.3f);
            beak.transform.localScale = new Vector3(0.04f, 0.12f, 0.04f);
            beak.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            beak.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("gullBeak", new Color32(0xE8, 0xB2, 0x4A, 0xFF), 0.4f);

            var col = gameObject.AddComponent<CapsuleCollider>();
            col.center = Vector3.zero;
            col.height = 0.7f;
            col.radius = 0.35f;
            col.isTrigger = true;
        }

        private void Update()
        {
            if (game == null || !game.Playing || Dead) return;
            phase += Time.deltaTime * speed;
            UpdatePosition();
            float flap = Mathf.Sin(Time.time * 8f) * 18f;
            if (leftWing != null) leftWing.localRotation = Quaternion.Euler(0f, 0f, flap);
            if (rightWing != null) rightWing.localRotation = Quaternion.Euler(0f, 0f, -flap);
        }

        private void UpdatePosition()
        {
            Vector3 p = center + new Vector3(Mathf.Cos(phase) * radius, height + Mathf.Sin(phase * 2f) * 0.8f, Mathf.Sin(phase) * radius);
            Vector3 dir = new Vector3(-Mathf.Sin(phase), 0f, Mathf.Cos(phase));
            transform.position = p;
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        public override void TakeDamage(int damage, bool headshot, bool melee, bool airborne, Vector3 hitPoint)
        {
            if (Dead) return;
            HpValue = Mathf.Max(0, HpValue - damage);
            if (HpValue <= 0) Die();
        }

        private void Die()
        {
            if (Dead) return;
            Dead = true;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            game.State.money += 15;
            game.UnlockAchievement("Seagull Sniper");
            game.UI.Toast("Seagull shot: +$15.");
            game.RegisterKill(null);
            game.UI.RefreshHud();
            Destroy(gameObject, 1.5f);
        }
    }
}
