using UnityEngine;

namespace Cove
{
    public class FishEnemy : CombatTarget
    {
        public CaughtFish Fish;
        public int HpValue;
        public int MaxHpValue;
        public bool Dead;
        public bool TestMode;
        public float MoveSpeed = 2.4f;
        public int ContactDamage = 9;
        public bool LastHitHeadshot;
        public bool LastHitMelee;
        public bool LastHitAirborne;
        public bool LastHitLongshot;

        private GameBoot game;
        private Transform visual;
        private BoxCollider box;
        private float attackTimer;
        private float hopPhase;
        private float groundY;
        private float stunTimer;
        private bool collected;
        private string bonusText;

        public override string TargetName { get { return Fish != null ? Fish.species : "Fish"; } }
        public override int Hp { get { return HpValue; } }
        public override int MaxHp { get { return MaxHpValue; } }
        public override bool IsDead { get { return Dead; } }
        public override bool IsAirborne { get { return transform.position.y > groundY + 0.45f; } }
        public override Transform AimPoint { get { return transform; } }

        public void Init(CaughtFish fish, System.Random rng, GameBoot g)
        {
            Fish = fish;
            game = g;
            int baseHp = fish.tier == "epic" ? 90 : fish.tier == "rare" ? 65 : fish.tier == "uncommon" ? 48 : 32;
            MaxHpValue = Mathf.RoundToInt(baseHp + fish.weight * 1.6f);
            if (fish.rareVariant) MaxHpValue = Mathf.RoundToInt(MaxHpValue * 1.4f);
            HpValue = MaxHpValue;
            if (g.State.hardMode)
            {
                MaxHpValue = Mathf.RoundToInt(MaxHpValue * 1.4f);
                HpValue = MaxHpValue;
                ContactDamage = Mathf.RoundToInt(ContactDamage * 1.3f);
            }
            BuildVisual();
            SnapToGround();
            attackTimer = 1.2f;
            hopPhase = (float)rng.NextDouble() * 6f;
        }

        private void BuildVisual()
        {
            string model = "Fish1_display";
            switch (Fish.species)
            {
                case "Coral Perch":
                case "Lagoon Snapper":
                case "Amber Tang":
                case "Moonray Bass":
                    model = "Fish2_display";
                    break;
                case "Stormback Eel":
                case "Golden Leviathan":
                    model = "Fish3_display";
                    break;
            }
            var prefab = CC0Library.Prefab(model);
            if (prefab == null) prefab = CC0Library.Prefab("Fish1_display");
            if (prefab != null)
            {
                var go = Instantiate(prefab, transform, false);
                go.name = "Fish Visual";
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                Bounds b = CC0Library.LocalBounds(go, transform);
                float target = Mathf.Clamp(0.55f + Fish.weight * 0.035f, 0.55f, 1.1f);
                float scale = b.size.y > 0.001f ? target / b.size.y : 1f;
                go.transform.localScale = Vector3.one * scale;
                Vector3 p = go.transform.localPosition;
                p.y = -b.min.y * scale + 0.14f;
                go.transform.localPosition = p;
                Color tint = Color.white;
                if (!string.IsNullOrEmpty(Fish.color)) ColorUtility.TryParseHtmlString(Fish.color, out tint);
                CC0Library.Tint(go, tint);
                visual = go.transform;
            }
            else
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(body.GetComponent<Collider>());
                body.transform.SetParent(transform, false);
                body.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                body.transform.localScale = new Vector3(0.32f, 0.42f, 0.7f);
                Color tint = Color.white;
                if (!string.IsNullOrEmpty(Fish.color)) ColorUtility.TryParseHtmlString(Fish.color, out tint);
                body.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("fishBody", tint, 0.4f);
                visual = body.transform;
            }

            box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            Bounds wb = CC0Library.LocalBounds(gameObject, transform);
            box.center = new Vector3(0f, 0.22f, 0f);
            box.size = new Vector3(Mathf.Max(0.35f, wb.size.x), Mathf.Max(0.28f, wb.size.y), Mathf.Max(0.55f, wb.size.z));
        }

        private void Update()
        {
            if (GameBoot.I == null || !GameBoot.I.Playing) return;
            SnapToGround();
            if (Dead || TestMode)
            {
                if (Dead) transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
                return;
            }

            var player = game.Player;
            if (player == null) return;
            Vector3 to = player.transform.position - transform.position;
            to.y = 0f;
            float dist = to.magnitude;

            if (stunTimer > 0f)
            {
                stunTimer -= Time.deltaTime;
                return;
            }

            if (dist < 13f)
            {
                Vector3 dir = to.sqrMagnitude > 0.001f ? to.normalized : transform.forward;
                transform.position += dir * MoveSpeed * Time.deltaTime;
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                hopPhase += Time.deltaTime * 9f;
                Vector3 p = transform.position;
                p.y = groundY + 0.18f + Mathf.Abs(Mathf.Sin(hopPhase)) * 0.22f;
                transform.position = p;
            }

            attackTimer -= Time.deltaTime;
            if (dist < 2.0f && attackTimer <= 0f)
            {
                attackTimer = 1.45f;
                if (player != null && game.Combat != null)
                {
                    game.Combat.TakeDamage(ContactDamage + (Fish.tier == "epic" ? 5 : 0));
                    if (game.Audio != null) game.Audio.Play(game.Audio.Nibble, 0.7f, 0.5f);
                    hopPhase = 6f;
                }
            }
        }

        private void SnapToGround()
        {
            RaycastHit hit;
            Vector3 from = transform.position + Vector3.up * 5f;
            if (Physics.Raycast(from, Vector3.down, out hit, 12f, ~0, QueryTriggerInteraction.Ignore))
                groundY = hit.point.y;
            else
                groundY = GameBoot.I.World.WaterLevel;
            Vector3 p = transform.position;
            if (Dead) p.y = groundY + 0.1f;
            else p.y = Mathf.Max(p.y, groundY + 0.14f);
            transform.position = p;
        }

        public override bool IsHeadshot(Vector3 point)
        {
            return point.y > transform.position.y + 0.2f;
        }

        public override void TakeDamage(int damage, bool headshot, bool melee, bool airborne, Vector3 hitPoint)
        {
            if (Dead) return;
            HpValue = Mathf.Max(0, HpValue - damage);
            LastHitHeadshot = headshot;
            LastHitMelee = melee;
            LastHitAirborne = airborne;
            if (melee) LastHitLongshot = false;
            if (visual != null) StartCoroutine(Flash());
            if (HpValue <= 0) Die();
        }

        private System.Collections.IEnumerator Flash()
        {
            if (visual == null) yield break;
            var renderers = visual.GetComponentsInChildren<Renderer>();
            var original = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) original[i] = renderers[i].material.color;
            for (int i = 0; i < renderers.Length; i++) renderers[i].material.color = Color.white;
            yield return new WaitForSeconds(0.07f);
            for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].material.color = original[i];
        }

        private void Die()
        {
            if (Dead) return;
            Dead = true;
            float mult = 1f;
            bonusText = "";
            if (LastHitHeadshot) { mult += 0.25f; bonusText += "Headshot "; }
            if (LastHitAirborne) { mult += 0.5f; bonusText += "Airborne "; }
            if (LastHitMelee) { mult += 0.05f; bonusText += "Melee "; }
            if (LastHitLongshot) { mult += 0.3f; bonusText += "Longshot "; }
            if (mult > 1.001f)
            {
                Fish.value = Mathf.RoundToInt(Fish.value * mult);
                Fish.killBonus = bonusText.Trim() + " x" + mult.ToString("0.00");
            }
            transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
            box.isTrigger = true;
            var interact = gameObject.AddComponent<Interactable>();
            interact.prompt = "E   Collect " + Fish.species;
            interact.onInteract = Collect;
            if (game.Audio != null) game.Audio.Play(game.Audio.Fanfare, 0.5f, 1.3f);
            game.RegisterKill(this);
        }

        public void Collect()
        {
            if (!Dead || collected) return;
            collected = true;
            var state = game.State;
            if (state.bag.Count >= state.bagCapacity)
            {
                game.UI.Toast("Your bag is full. Sell some fish first.");
                collected = false;
                return;
            }
            state.bag.Add(Fish);
            game.UI.ShowCatch(Fish, true);
            game.UI.Toast("Collected " + Fish.species + (string.IsNullOrEmpty(Fish.killBonus) ? "." : " (" + Fish.killBonus + ")"));
            game.UI.RefreshHud();
            game.EvaluateQuests();
            if (game.Audio != null) game.Audio.Play(game.Audio.Buy, 0.6f, 1.1f);
            Destroy(gameObject);
        }

        public void CollectForTest()
        {
            Collect();
        }

        public void TestPlaceInFront(Transform player, Camera cam)
        {
            Vector3 flat = cam.transform.forward;
            flat.y = 0f;
            flat.Normalize();
            Vector3 p = player.position + flat * 6f;
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 5f, Vector3.down, out hit, 12f, ~0, QueryTriggerInteraction.Ignore)) p.y = hit.point.y + 0.1f;
            transform.position = p;
        }
    }
}
