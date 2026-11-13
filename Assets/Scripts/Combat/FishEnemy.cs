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
        private GameObject highlightRing;

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
            Color tint = Color.white;
            if (!string.IsNullOrEmpty(Fish.color)) ColorUtility.TryParseHtmlString(Fish.color, out tint);
            string model = Fish.weight > 5f ? "fish-large" : "fish";
            var go = CC0Library.SpawnLocal(model, transform, Vector3.zero, 0f, Mathf.Clamp(0.8f + Fish.weight * 0.04f, 0.8f, 1.5f), CCFit.Height, true, tint);
            if (go != null)
            {
                visual = go.transform;
                float eyeSize = Mathf.Clamp(0.8f + Fish.weight * 0.04f, 0.8f, 1.5f);
                for (int side = -1; side <= 1; side += 2)
                {
                    var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(eye.GetComponent<Collider>());
                    eye.transform.SetParent(transform, false);
                    eye.transform.localPosition = new Vector3(side * 0.16f * eyeSize, 0.38f * eyeSize, 0.42f * eyeSize);
                    eye.transform.localScale = Vector3.one * (0.055f * eyeSize);
                    eye.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("fishEye", new Color32(0x1C, 0x22, 0x28, 0xFF), 0.6f);
                }
            }
            else
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(body.GetComponent<Collider>());
                body.transform.SetParent(transform, false);
                body.transform.localPosition = new Vector3(0f, 0.28f, 0f);
                body.transform.localScale = new Vector3(0.35f, 0.48f, 0.8f);
                body.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("fishBody", tint, 0.4f);
                var tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(tail.GetComponent<Collider>());
                tail.transform.SetParent(transform, false);
                tail.transform.localPosition = new Vector3(0f, 0.28f, -0.5f);
                tail.transform.localScale = new Vector3(0.06f, 0.32f, 0.3f);
                tail.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("fishTail", tint, 0.4f);
                visual = body.transform;
            }

            box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            Bounds wb = CC0Library.LocalBounds(gameObject, transform);
            box.center = new Vector3(0f, 0.22f, 0f);
            box.size = new Vector3(Mathf.Max(0.35f, wb.size.x), Mathf.Max(0.28f, wb.size.y), Mathf.Max(0.55f, wb.size.z));

            highlightRing = new GameObject("Target Ring");
            highlightRing.transform.SetParent(transform, false);
            highlightRing.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(seg.GetComponent<Collider>());
                seg.transform.SetParent(highlightRing.transform, false);
                seg.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.72f, 0f, Mathf.Sin(a) * 0.72f);
                seg.transform.localScale = new Vector3(0.24f, 0.02f, 0.07f);
                seg.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                seg.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("targetRing", new Color32(0xFF, 0xD1, 0x6A, 0xFF), 0.1f);
                seg.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            highlightRing.SetActive(false);
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

            if (highlightRing != null && highlightRing.activeSelf)
                highlightRing.transform.Rotate(0f, 140f * Time.deltaTime, 0f, Space.Self);
            if (visual != null)
                visual.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 7f + hopPhase) * 7f, 0f);

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

        public override void SetHighlight(bool on)
        {
            if (highlightRing != null) highlightRing.SetActive(on && !Dead);
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
            if (highlightRing != null) highlightRing.SetActive(false);
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
