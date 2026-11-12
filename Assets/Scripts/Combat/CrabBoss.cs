using UnityEngine;

namespace Cove
{
    public class CrabBoss : CombatTarget
    {
        public int HpValue = 320;
        public int MaxHpValue = 320;
        public bool Dead;
        public int ContactDamage = 24;

        private GameBoot game;
        private Transform player;
        private float stateTimer;
        private int state;
        private Vector3 chargeDir;
        private float groundY;
        private Transform stunStars;
        private float bob;

        public override string TargetName { get { return "SPIDER CRAB"; } }
        public override int Hp { get { return HpValue; } }
        public override int MaxHp { get { return MaxHpValue; } }
        public override bool IsDead { get { return Dead; } }
        public override bool IsBoss { get { return true; } }
        public override Transform AimPoint { get { return transform; } }

        public void Init(GameBoot g, Vector3 pos)
        {
            game = g;
            player = g.Player != null ? g.Player.transform : null;
            transform.position = pos;
            BuildVisual();
            transform.localScale = Vector3.one * 1.45f;
            SnapToGround();
            state = 0;
            stateTimer = 1.2f;
        }

        private void BuildVisual()
        {
            Color shell = new Color32(0xB8, 0x4A, 0x3A, 0xFF);
            Color dark = new Color32(0x6E, 0x2A, 0x24, 0xFF);
            Color claw = new Color32(0xD9, 0x6A, 0x4A, 0xFF);

            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            body.transform.localScale = new Vector3(1.55f, 0.72f, 1.15f);
            body.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("crabShell", shell, 0.25f);

            var top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(top.GetComponent<Collider>());
            top.transform.SetParent(transform, false);
            top.transform.localPosition = new Vector3(0f, 0.92f, -0.04f);
            top.transform.localScale = new Vector3(1.1f, 0.42f, 0.9f);
            top.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("crabTop", dark, 0.3f);

            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    Destroy(leg.GetComponent<Collider>());
                    leg.transform.SetParent(transform, false);
                    float z = -0.42f + i * 0.3f;
                    leg.transform.localPosition = new Vector3(side * 0.78f, 0.34f, z);
                    leg.transform.localScale = new Vector3(0.1f, 0.42f, 0.1f);
                    leg.transform.localRotation = Quaternion.Euler(0f, 0f, side * 55f);
                    leg.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("crabLeg", dark, 0.3f);
                }

                var arm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Destroy(arm.GetComponent<Collider>());
                arm.transform.SetParent(transform, false);
                arm.transform.localPosition = new Vector3(side * 0.72f, 0.55f, 0.62f);
                arm.transform.localScale = new Vector3(0.18f, 0.38f, 0.18f);
                arm.transform.localRotation = Quaternion.Euler(52f, 0f, side * 18f);
                arm.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("crabArm", shell, 0.3f);

                var pincer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(pincer.GetComponent<Collider>());
                pincer.transform.SetParent(transform, false);
                pincer.transform.localPosition = new Vector3(side * 0.78f, 0.55f, 0.98f);
                pincer.transform.localScale = new Vector3(0.38f, 0.28f, 0.46f);
                pincer.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("crabClaw", claw, 0.25f);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(eye.GetComponent<Collider>());
                eye.transform.SetParent(transform, false);
                eye.transform.localPosition = new Vector3(side * 0.24f, 1.15f, 0.42f);
                eye.transform.localScale = Vector3.one * 0.14f;
                eye.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("crabEye", Color.white, 0.6f);
            }

            stunStars = new GameObject("Stun Stars").transform;
            stunStars.SetParent(transform, false);
            stunStars.localPosition = new Vector3(0f, 1.6f, 0f);
            for (int i = 0; i < 3; i++)
            {
                var star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(star.GetComponent<Collider>());
                star.transform.SetParent(stunStars, false);
                float a = i * Mathf.PI * 2f / 3f;
                star.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.35f, 0f, Mathf.Sin(a) * 0.35f);
                star.transform.localScale = Vector3.one * 0.1f;
                star.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("crabStar", new Color32(0xFF, 0xE0, 0x55, 0xFF), 0.2f);
            }
            stunStars.gameObject.SetActive(false);

            var col = gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.8f, 0f);
            col.height = 1.7f;
            col.radius = 0.95f;
        }

        private void Update()
        {
            if (game == null || !game.Playing) return;
            if (Dead) return;
            if (player == null) { player = game.Player != null ? game.Player.transform : null; return; }
            SnapToGround();
            bob += Time.deltaTime;

            Vector3 to = player.position - transform.position;
            to.y = 0f;
            float dist = to.magnitude;

            if (state == 0)
            {
                if (dist < 20f)
                {
                    state = 1;
                    stateTimer = 0.8f;
                    chargeDir = to.normalized;
                    if (game.UI != null) game.UI.Toast("The Spider Crab raises its claws!");
                    if (game.Audio != null) game.Audio.Play(game.Audio.BossRoar, 0.8f, 1f);
                }
            }
            else if (state == 1)
            {
                stateTimer -= Time.deltaTime;
                transform.rotation = Quaternion.LookRotation(chargeDir, Vector3.up);
                transform.position += Vector3.up * Mathf.Sin(bob * 30f) * 0.002f;
                if (stateTimer <= 0f)
                {
                    state = 2;
                    stateTimer = 0.55f;
                }
            }
            else if (state == 2)
            {
                stateTimer -= Time.deltaTime;
                Vector3 p = transform.position + chargeDir * 11f * Time.deltaTime;
                p.y = transform.position.y;
                transform.position = p;
                if (dist < 2.2f)
                {
                    if (game.Combat != null) game.Combat.TakeDamage(ContactDamage);
                }
                if (stateTimer <= 0f)
                {
                    state = 3;
                    stateTimer = 2.3f;
                    stunStars.gameObject.SetActive(true);
                    if (game.UI != null) game.UI.Toast("The crab is dazed! Attack now!");
                }
            }
            else if (state == 3)
            {
                stateTimer -= Time.deltaTime;
                stunStars.Rotate(Vector3.up, 320f * Time.deltaTime);
                if (stateTimer <= 0f)
                {
                    state = 0;
                    stateTimer = 0.8f;
                    stunStars.gameObject.SetActive(false);
                }
            }
        }

        private void SnapToGround()
        {
            RaycastHit hit;
            Vector3 from = transform.position + Vector3.up * 6f;
            if (Physics.Raycast(from, Vector3.down, out hit, 14f, ~0, QueryTriggerInteraction.Ignore))
                groundY = hit.point.y;
            else
                groundY = game.World.WaterLevel;
            Vector3 p = transform.position;
            p.y = groundY;
            transform.position = p;
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
            if (stunStars != null) stunStars.gameObject.SetActive(false);
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 90f);
            if (game.Audio != null) game.Audio.Play(game.Audio.BossRoar, 0.9f, 0.7f);
            game.RegisterKill(null);
            game.SpawnBossShell(transform.position);
            if (game.UI != null) game.UI.Toast("Spider Crab defeated! Take its shell to the shop keeper.");
            Destroy(gameObject, 2.5f);
        }
    }
}
