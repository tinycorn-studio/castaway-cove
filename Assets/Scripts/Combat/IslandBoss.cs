using UnityEngine;

namespace Cove
{
    public class IslandBoss : CombatTarget
    {
        public int BossType;
        public int HpValue;
        public int MaxHpValue;
        public bool Dead;
        public int ContactDamage;

        private GameBoot game;
        private Transform player;
        private float stateTimer;
        private int state;
        private Vector3 chargeDir;
        private float groundY;
        private Transform stunStars;
        private float bob;
        private float hover;
        private float moveSpeed;
        private float chargeTime;
        private float telegraphTime;
        private float stunTime;

        public override string TargetName
        {
            get
            {
                if (BossType == 1) return "GIANT PIRANHA";
                if (BossType == 2) return "PUFFERFISH";
                if (BossType == 3) return "ALBATROSS";
                return "BOWHEAD WHALE";
            }
        }

        public override int Hp { get { return HpValue; } }
        public override int MaxHp { get { return MaxHpValue; } }
        public override bool IsDead { get { return Dead; } }
        public override bool IsBoss { get { return true; } }
        public override Transform AimPoint { get { return transform; } }

        public void Init(GameBoot g, Vector3 pos, int type)
        {
            game = g;
            player = g.Player != null ? g.Player.transform : null;
            BossType = type;
            transform.position = pos;
            if (type == 1) { MaxHpValue = 240; ContactDamage = 18; moveSpeed = 10f; chargeTime = 0.45f; telegraphTime = 0.6f; stunTime = 2.0f; }
            else if (type == 2) { MaxHpValue = 320; ContactDamage = 24; moveSpeed = 8f; chargeTime = 0.7f; telegraphTime = 0.8f; stunTime = 2.4f; }
            else if (type == 3) { MaxHpValue = 300; ContactDamage = 20; moveSpeed = 13f; chargeTime = 0.5f; telegraphTime = 0.7f; stunTime = 2.2f; hover = 2.4f; }
            else { MaxHpValue = 700; ContactDamage = 32; moveSpeed = 8f; chargeTime = 0.7f; telegraphTime = 1.0f; stunTime = 2.6f; }
            HpValue = MaxHpValue;
            if (g.State.hardMode)
            {
                MaxHpValue = Mathf.RoundToInt(MaxHpValue * 1.4f);
                HpValue = MaxHpValue;
                ContactDamage = Mathf.RoundToInt(ContactDamage * 1.3f);
            }
            BuildVisual();
            SnapToGround();
            state = 0;
            stateTimer = 1.2f;
        }

        private void BuildVisual()
        {
            if (BossType == 1) BuildPiranha();
            else if (BossType == 2) BuildPuffer();
            else if (BossType == 3) BuildAlbatross();
            else BuildWhale();

            var col = gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, BossType == 3 ? 0.9f : 1.1f, 0f);
            col.height = BossType == 3 ? 2.0f : 2.4f;
            col.radius = BossType == 4 ? 1.8f : 1.0f;

            stunStars = new GameObject("Stun Stars").transform;
            stunStars.SetParent(transform, false);
            stunStars.localPosition = new Vector3(0f, BossType == 3 ? 1.9f : 2.4f, 0f);
            for (int i = 0; i < 3; i++)
            {
                var star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(star.GetComponent<Collider>());
                star.transform.SetParent(stunStars, false);
                float a = i * Mathf.PI * 2f / 3f;
                star.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.4f, 0f, Mathf.Sin(a) * 0.4f);
                star.transform.localScale = Vector3.one * 0.12f;
                star.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("bossStar", new Color32(0xFF, 0xE0, 0x55, 0xFF), 0.2f);
            }
            stunStars.gameObject.SetActive(false);
        }

        private void BuildPiranha()
        {
            var prefab = CC0Library.Prefab("Fish2_display") ?? CC0Library.Prefab("Fish3_display");
            if (prefab != null)
            {
                var go = Instantiate(prefab, transform, false);
                go.transform.localPosition = new Vector3(0f, 0.7f, 0f);
                go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                Bounds b = CC0Library.LocalBounds(go, transform);
                float s = b.size.y > 0.001f ? 1.5f / b.size.y : 1f;
                go.transform.localScale = Vector3.one * s;
                CC0Library.Tint(go, new Color32(0xB8, 0x3A, 0x32, 0xFF));
            }
            else
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(body.GetComponent<Collider>());
                body.transform.SetParent(transform, false);
                body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                body.transform.localScale = new Vector3(1.2f, 1.2f, 2.0f);
                body.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("piranhaBody", new Color32(0xB8, 0x3A, 0x32, 0xFF), 0.3f);
            }
        }

        private void BuildPuffer()
        {
            Color bodyColor = new Color32(0xD9, 0x9A, 0x3A, 0xFF);
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            body.transform.localScale = new Vector3(1.7f, 1.5f, 1.7f);
            body.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("pufferBody", bodyColor, 0.3f);

            for (int i = 0; i < 14; i++)
            {
                var spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(spike.GetComponent<Collider>());
                spike.transform.SetParent(transform, false);
                float a = i * Mathf.PI * 2f / 14f;
                float y = Mathf.Sin(i * 2.7f) * 0.55f;
                spike.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.8f, 1.0f + y, Mathf.Sin(a) * 0.8f);
                spike.transform.localScale = new Vector3(0.1f, 0.32f, 0.1f);
                spike.transform.localRotation = Quaternion.Euler(90f, -a * Mathf.Rad2Deg, 0f);
                spike.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("pufferSpike", new Color32(0xF2, 0xD9, 0x9A, 0xFF), 0.3f);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(eye.GetComponent<Collider>());
                eye.transform.SetParent(transform, false);
                eye.transform.localPosition = new Vector3(side * 0.45f, 1.35f, 0.78f);
                eye.transform.localScale = Vector3.one * 0.2f;
                eye.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("pufferEye", Color.white, 0.6f);
            }
        }

        private void BuildAlbatross()
        {
            Color white = new Color32(0xF2, 0xEF, 0xE5, 0xFF);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            body.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
            body.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("birdBody", white, 0.3f);

            for (int side = -1; side <= 1; side += 2)
            {
                var wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(wing.GetComponent<Collider>());
                wing.transform.SetParent(transform, false);
                wing.transform.localPosition = new Vector3(side * 1.1f, 0.3f, 0f);
                wing.transform.localScale = new Vector3(1.9f, 0.08f, 0.55f);
                wing.transform.localRotation = Quaternion.Euler(0f, 0f, side * -12f);
                wing.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("birdWing", new Color32(0xD9, 0xD9, 0xD0, 0xFF), 0.3f);
            }

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(transform, false);
            head.transform.localPosition = new Vector3(0f, 1.0f, 0.35f);
            head.transform.localScale = Vector3.one * 0.4f;
            head.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("birdHead", white, 0.3f);

            var beak = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(beak.GetComponent<Collider>());
            beak.transform.SetParent(transform, false);
            beak.transform.localPosition = new Vector3(0f, 0.98f, 0.62f);
            beak.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);
            beak.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            beak.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("birdBeak", new Color32(0xE8, 0xB2, 0x4A, 0xFF), 0.3f);
        }

        private void BuildWhale()
        {
            var prefab = CC0Library.Prefab("Shark_display");
            if (prefab != null)
            {
                var go = Instantiate(prefab, transform, false);
                go.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                Bounds b = CC0Library.LocalBounds(go, transform);
                float s = b.size.y > 0.001f ? 3.2f / b.size.y : 1f;
                go.transform.localScale = Vector3.one * s;
                CC0Library.Tint(go, new Color32(0x5B, 0x7A, 0x8C, 0xFF));
            }
            else
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(body.GetComponent<Collider>());
                body.transform.SetParent(transform, false);
                body.transform.localPosition = new Vector3(0f, 1.4f, 0f);
                body.transform.localScale = new Vector3(2.4f, 1.6f, 4.2f);
                body.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("whaleBody", new Color32(0x5B, 0x7A, 0x8C, 0xFF), 0.3f);
            }
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
                if (dist < 22f)
                {
                    state = 1;
                    stateTimer = telegraphTime;
                    chargeDir = to.normalized;
                    if (game.Audio != null) game.Audio.Play(game.Audio.BossRoar, 0.8f, BossType == 4 ? 0.7f : 1f);
                }
            }
            else if (state == 1)
            {
                stateTimer -= Time.deltaTime;
                transform.rotation = Quaternion.LookRotation(chargeDir, Vector3.up);
                transform.position += Vector3.up * Mathf.Sin(bob * 30f) * 0.003f;
                if (stateTimer <= 0f)
                {
                    state = 2;
                    stateTimer = chargeTime;
                }
            }
            else if (state == 2)
            {
                stateTimer -= Time.deltaTime;
                Vector3 p = transform.position + chargeDir * moveSpeed * Time.deltaTime;
                p.y = transform.position.y;
                transform.position = p;
                if (dist < (BossType == 4 ? 3.2f : 2.3f)) game.Combat.TakeDamage(ContactDamage);
                if (stateTimer <= 0f)
                {
                    state = 3;
                    stateTimer = stunTime;
                    if (stunStars != null) stunStars.gameObject.SetActive(true);
                }
            }
            else if (state == 3)
            {
                stateTimer -= Time.deltaTime;
                if (stunStars != null) stunStars.Rotate(Vector3.up, 320f * Time.deltaTime);
                if (stateTimer <= 0f)
                {
                    state = 0;
                    stateTimer = 0.8f;
                    if (stunStars != null) stunStars.gameObject.SetActive(false);
                }
            }
        }

        private void SnapToGround()
        {
            RaycastHit hit;
            Vector3 from = transform.position + Vector3.up * 8f;
            if (Physics.Raycast(from, Vector3.down, out hit, 18f, ~0, QueryTriggerInteraction.Ignore))
                groundY = hit.point.y;
            else
                groundY = game.World.WaterLevel;
            Vector3 p = transform.position;
            p.y = groundY + hover;
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
            game.SpawnBossTrophy(BossType, transform.position);
            Destroy(gameObject, 2.5f);
        }
    }
}
