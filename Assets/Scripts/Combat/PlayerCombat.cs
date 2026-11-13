using System.Collections.Generic;
using UnityEngine;

namespace Cove
{
    public class PlayerCombat : MonoBehaviour
    {
        public PlayerController Player;
        public Camera Cam;
        public AudioLibrary Audio;
        public FishEnemy CurrentFish;

        private GameObject weaponRoot;
        private GameObject[] weaponVisuals = new GameObject[4];
        private readonly List<Renderer> weaponParts = new List<Renderer>();
        private Vector3 weaponRootBasePos;
        private Quaternion weaponRootBaseRot;
        private float punchTimer;
        private float nextAttack;
        private float invuln;
        private float hudTimer;
        private float targetTimer;
        private bool reloading;
        private float reloadTimer;
        private int reloadWeapon = -1;
        private CombatTarget hudTarget;
        private CombatTarget highlightedTarget;

        private void Start()
        {
            BuildWeaponVisuals();
            if (State != null && State.skinIndex > 0) ApplySkin(State.skinIndex);
            RefreshWeaponVisual();
        }

        private GameState State
        {
            get { return GameBoot.I != null ? GameBoot.I.State : null; }
        }

        private void Update()
        {
            var game = GameBoot.I;
            if (game == null || !game.Playing) return;
            bool blocked = game.UI != null && game.UI.AnyPanelOpen;

            invuln -= Time.deltaTime;
            if (!blocked)
            {
                HandleInput();
                TickHunger();
            }
            TickReload();
            RefreshWeaponVisual();

            hudTimer -= Time.deltaTime;
            if (hudTimer <= 0f && game.UI != null)
            {
                hudTimer = 0.12f;
                game.UI.SetVitals(State.hp, 100f, State.hunger, 100f);
                game.UI.SetWeapon(WeaponHudText(), !game.Fishing.RodEquipped && !blocked, State.weaponIndex);
            }

            targetTimer -= Time.deltaTime;
            if (targetTimer <= 0f && game.UI != null)
            {
                targetTimer = 0.1f;
                CombatTarget aimed = null;
                RaycastHit aimHit;
                if (Physics.Raycast(Cam.transform.position, Cam.transform.forward, out aimHit, 35f, ~0, QueryTriggerInteraction.Collide))
                    aimed = aimHit.collider.GetComponentInParent<CombatTarget>();
                if (aimed == null)
                    aimed = CombatRegistry.Nearest(transform.position, 20f, transform.forward);
                if (aimed != null && aimed.IsDead) aimed = null;
                hudTarget = aimed;
                if (highlightedTarget != null && highlightedTarget != aimed) highlightedTarget.SetHighlight(false);
                highlightedTarget = aimed;
                if (highlightedTarget != null) highlightedTarget.SetHighlight(true);
                if (hudTarget != null)
                    game.UI.SetTargetBar(true, hudTarget.TargetName, hudTarget.Hp, hudTarget.MaxHp, hudTarget.IsBoss);
                else
                    game.UI.SetTargetBar(false, "", 0f, 0f, false);
                game.UI.SetCrosshairTarget(hudTarget != null);
            }

            if (punchTimer > 0f && weaponRoot != null)
            {
                punchTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(punchTimer / 0.22f);
                float jab = Mathf.Sin(t * Mathf.PI);
                weaponRoot.transform.localPosition = weaponRootBasePos + new Vector3(0f, -0.02f, 0.2f) * jab;
                weaponRoot.transform.localRotation = weaponRootBaseRot * Quaternion.Euler(10f * jab, 0f, 0f);
            }
            else if (weaponRoot != null)
            {
                weaponRoot.transform.localPosition = weaponRootBasePos;
                weaponRoot.transform.localRotation = weaponRootBaseRot;
            }
        }

        private void HandleInput()
        {
            var game = GameBoot.I;
            if (Input.GetKeyDown(KeyCode.Alpha2)) EquipWeapon(State.knifeOwned ? 1 : 0);
            if (Input.GetKeyDown(KeyCode.Alpha3)) EquipWeapon(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) EquipWeapon(3);
            if (Input.GetKeyDown(KeyCode.R)) StartReload();
            if (Input.GetKeyDown(KeyCode.F)) EatFish();
            if (Input.GetKeyDown(KeyCode.Alpha5)) ThrowDynamite();

            if (game.Fishing == null || game.Fishing.RodEquipped) return;
            if (game.Fishing.State != FishingState.Idle) return;
            if (Input.GetMouseButtonDown(0)) TryAttack();
        }

        private void TickHunger()
        {
            State.hunger = Mathf.Max(0f, State.hunger - 0.22f * Time.deltaTime);
            if (State.hunger <= 0f)
            {
                State.hp = Mathf.Max(0f, State.hp - 0.9f * Time.deltaTime);
                if (State.hp <= 0f) Die();
            }
        }

        private void TickReload()
        {
            if (!reloading) return;
            if (reloadWeapon != State.weaponIndex)
            {
                reloading = false;
                return;
            }
            reloadTimer -= Time.deltaTime;
            if (reloadTimer > 0f) return;
            reloading = false;
            var def = GameData.Weapons[reloadWeapon];
            int mag = GetMag(reloadWeapon);
            int reserve = GetReserve(reloadWeapon);
            int take = Mathf.Min(def.magSize - mag, reserve);
            SetMag(reloadWeapon, mag + take);
            SetReserve(reloadWeapon, reserve - take);
            if (GameBoot.I.UI != null)
            {
                GameBoot.I.UI.RefreshHud();
                GameBoot.I.UI.Toast("Reloaded " + def.name + ".");
            }
        }

        public void HandleWeaponKeys()
        {
        }

        public bool OwnsWeapon(int index)
        {
            if (index == 0) return true;
            if (index == 1) return State.knifeOwned;
            if (index == 2) return State.pistolOwned;
            if (index == 3) return State.shotgunOwned;
            return false;
        }

        public bool EquipWeapon(int index)
        {
            if (index < 0 || index >= GameData.Weapons.Length) return false;
            var game = GameBoot.I;
            if (!OwnsWeapon(index))
            {
                if (game.UI != null) game.UI.Toast("Buy the " + GameData.Weapons[index].name + " at the weapon rack first.");
                return false;
            }
            if (game.Fishing != null) game.Fishing.EquipRod(false);
            State.weaponIndex = index;
            if (reloading && reloadWeapon != index) reloading = false;
            RefreshWeaponVisual();
            if (game.UI != null)
            {
                game.UI.RefreshHud();
                game.UI.Toast(GameData.Weapons[index].name + " equipped.");
            }
            return true;
        }

        public void EquipBestWeapon()
        {
            if (State.shotgunOwned) EquipWeapon(3);
            else if (State.pistolOwned) EquipWeapon(2);
            else if (State.knifeOwned) EquipWeapon(1);
            else EquipWeapon(0);
        }

        private void TryAttack()
        {
            var def = GameData.Weapons[State.weaponIndex];
            if (Time.time < nextAttack) return;
            if (reloading) return;
            nextAttack = Time.time + def.cooldown;
            if (def.melee) MeleeAttack(def);
            else RangedAttack(def);
        }

        public void AttackNow()
        {
            nextAttack = 0f;
            TryAttack();
        }

        private void MeleeAttack(WeaponDef def)
        {
            Vector3 center = transform.position + Vector3.up * 1.1f + transform.forward * (def.range * 0.55f);
            Collider[] hits = Physics.OverlapSphere(center, def.range * 0.55f, ~0, QueryTriggerInteraction.Collide);
            CombatTarget target = null;
            float best = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                var t = hits[i].GetComponentInParent<CombatTarget>();
                if (t == null || t.IsDead) continue;
                float d = (t.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; target = t; }
            }
            if (Audio != null) Audio.Play(Audio.Punch, 0.65f, 1f);
            punchTimer = 0.22f;
            if (target != null)
            {
                bool head = target.IsHeadshot(target.transform.position + Vector3.up * 0.4f);
                target.TakeDamage(def.damage, head, true, target.IsAirborne, target.transform.position);
                if (Audio != null) Audio.Play(Audio.Hit, 0.7f, 1f);
                HitSpark(target.transform.position + Vector3.up * 0.35f);
            }
            Recoil(0.4f);
        }

        private void RangedAttack(WeaponDef def)
        {
            int mag = GetMag(State.weaponIndex);
            if (mag <= 0)
            {
                StartReload();
                if (Audio != null) Audio.Play(Audio.Error, 0.35f, 1.5f);
                return;
            }
            SetMag(State.weaponIndex, mag - 1);
            if (GameBoot.I.UI != null) GameBoot.I.UI.RefreshHud();
            if (Audio != null) Audio.Play(Audio.Shot, 0.85f, State.weaponIndex == 3 ? 0.8f : 1.1f);
            MuzzleFlash();

            int pellets = Mathf.Max(1, def.pellets);
            bool drewTracer = false;
            for (int p = 0; p < pellets; p++)
            {
                Vector3 dir = Cam.transform.forward;
                if (pellets > 1)
                {
                    float spread = 0.045f;
                    dir += Cam.transform.right * Random.Range(-spread, spread) + Cam.transform.up * Random.Range(-spread, spread);
                    dir.Normalize();
                }
                Vector3 origin = Cam.transform.position + dir * 0.2f;
                RaycastHit[] hits = Physics.RaycastAll(origin, dir, def.range, ~0, QueryTriggerInteraction.Collide);
                float nearest = float.MaxValue;
                RaycastHit chosen = new RaycastHit();
                bool have = false;
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider == null) continue;
                    if (hits[i].collider.transform.IsChildOf(transform)) continue;
                    if (hits[i].distance < nearest)
                    {
                        nearest = hits[i].distance;
                        chosen = hits[i];
                        have = true;
                    }
                }
                Vector3 end = have ? chosen.point : origin + dir * def.range;
                if (!drewTracer)
                {
                    DrawTracer(origin, end);
                    drewTracer = true;
                }
                if (have)
                {
                    var t = chosen.collider.GetComponentInParent<CombatTarget>();
                    if (t != null && !t.IsDead)
                    {
                        var fishTarget = t as FishEnemy;
                        if (fishTarget != null) fishTarget.LastHitLongshot = nearest > 18f;
                        t.TakeDamage(def.damage, t.IsHeadshot(chosen.point), false, t.IsAirborne, chosen.point);
                        if (Audio != null) Audio.Play(Audio.Hit, 0.5f, 1.2f);
                        break;
                    }
                }
            }
            Recoil(State.weaponIndex == 3 ? 1.8f : 0.7f);
        }

        private void Recoil(float amount)
        {
            Player.Pitch = Mathf.Clamp(Player.Pitch - amount, -80f, 80f);
            if (Player.CameraPivot != null) Player.CameraPivot.localRotation = Quaternion.Euler(Player.Pitch, 0f, 0f);
        }

        public void StartReload()
        {
            var def = GameData.Weapons[State.weaponIndex];
            if (def.melee || def.magSize <= 0) return;
            if (reloading) return;
            if (GetMag(State.weaponIndex) >= def.magSize) return;
            if (GetReserve(State.weaponIndex) <= 0)
            {
                if (GameBoot.I.UI != null) GameBoot.I.UI.Toast("No spare ammo. Buy more at the weapon rack.");
                if (Audio != null) Audio.Play(Audio.Error, 0.35f, 1.4f);
                return;
            }
            reloading = true;
            reloadWeapon = State.weaponIndex;
            reloadTimer = def.reload;
            if (Audio != null) Audio.Play(Audio.Reload, 0.6f, 1f);
            if (GameBoot.I.UI != null) GameBoot.I.UI.Toast("Reloading…");
        }

        public int GetMag(int index)
        {
            if (index == 2) return State.pistolMag;
            if (index == 3) return State.shotgunMag;
            return 0;
        }

        private void SetMag(int index, int value)
        {
            if (index == 2) State.pistolMag = Mathf.Max(0, value);
            else if (index == 3) State.shotgunMag = Mathf.Max(0, value);
        }

        public int GetReserve(int index)
        {
            if (index == 2) return State.pistolAmmo;
            if (index == 3) return State.shotgunAmmo;
            return 0;
        }

        private void SetReserve(int index, int value)
        {
            if (index == 2) State.pistolAmmo = Mathf.Max(0, value);
            else if (index == 3) State.shotgunAmmo = Mathf.Max(0, value);
        }

        public string WeaponHudText()
        {
            var def = GameData.Weapons[State.weaponIndex];
            string text = def.melee ? def.name : def.name + "  " + GetMag(State.weaponIndex) + " / " + GetReserve(State.weaponIndex);
            return text + "   ·   Dyn " + State.dynamite;
        }

        public void TakeDamage(int amount)
        {
            if (invuln > 0f || !GameBoot.I.Playing) return;
            if (State.hp <= 0f) return;
            State.hp = Mathf.Max(0f, State.hp - amount);
            invuln = 0.75f;
            if (Audio != null) Audio.Play(Audio.Hurt, 0.7f, 1f);
            if (GameBoot.I.UI != null)
            {
                GameBoot.I.UI.FlashDamage();
                GameBoot.I.UI.SetVitals(State.hp, 100f, State.hunger, 100f);
            }
            if (State.hp <= 0f) Die();
        }

        private void Die()
        {
            if (!GameBoot.I.Playing) return;
            State.hp = 0f;
            if (Audio != null) Audio.Play(Audio.Death, 0.8f, 1f);
            if (GameBoot.I.UI != null) GameBoot.I.UI.ShowDeath(true);
            StartCoroutine(Respawn());
        }

        private System.Collections.IEnumerator Respawn()
        {
            yield return new WaitForSecondsRealtime(1.6f);
            var game = GameBoot.I;
            int loss = Mathf.RoundToInt(State.money * 0.1f);
            State.money = Mathf.Max(0, State.money - loss);
            State.hp = 60f;
            State.hunger = 50f;
            Player.InBoat = false;
            Player.Controller.enabled = true;
            Player.Teleport(game.World.StartSpawn.position + Vector3.up * 0.4f, 0f);
            if (game.Boat != null) { game.Boat.Occupied = false; }
            if (game.UI != null)
            {
                game.UI.ShowDeath(false);
                game.UI.Toast("You were knocked out. Lost " + GameData.Money(loss) + " and woke up at the dock.");
                game.UI.RefreshHud();
            }
            invuln = 2f;
            if (game.Fishing != null) game.Fishing.CancelFishing(false);
        }
        private void ThrowDynamite()
        {
            if (State.dynamite <= 0)
            {
                if (GameBoot.I.UI != null) GameBoot.I.UI.Toast("No dynamite. Buy some at the weapon rack.");
                return;
            }
            State.dynamite--;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.name = "Dynamite";
            go.transform.position = Cam.transform.position + Cam.transform.forward * 0.7f;
            go.transform.localScale = Vector3.one * 0.2f;
            go.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("dynamite", new Color32(0xB8, 0x2A, 0x2A, 0xFF), 0.3f);
            var rb = go.AddComponent<Rigidbody>();
            rb.velocity = Cam.transform.forward * 9f + Vector3.up * 4f;
            var stick = go.AddComponent<DynamiteStick>();
            stick.Init(GameBoot.I, 1.6f);
            if (Audio != null) Audio.Play(Audio.Click, 0.5f, 1.3f);
            if (GameBoot.I.UI != null) GameBoot.I.UI.Toast("Dynamite out!");
            if (GameBoot.I.UI != null) GameBoot.I.UI.RefreshHud();
        }

        public void EatFish()
        {
            var game = GameBoot.I;
            if (State.bag.Count == 0)
            {
                if (game.UI != null) game.UI.Toast("No fish to eat. Catch one first.");
                return;
            }
            int idx = State.bag.Count - 1;
            CaughtFish f = State.bag[idx];
            State.bag.RemoveAt(idx);
            float heal = f.cooked && !f.burned ? 30f : f.burned ? 8f : 18f;
            float food = f.cooked && !f.burned ? 40f : f.burned ? 10f : 28f;
            State.hp = Mathf.Min(100f, State.hp + heal + Mathf.Min(20f, f.value * 0.02f));
            State.hunger = Mathf.Min(100f, State.hunger + food);
            if (Audio != null) Audio.Play(Audio.Buy, 0.6f, 1.1f);
            if (game.UI != null)
            {
                game.UI.Toast("Ate " + f.species + " — health and hunger restored.");
                game.UI.SetVitals(State.hp, 100f, State.hunger, 100f);
                game.UI.RefreshHud();
            }
        }

        public FishEnemy SpawnCaughtFish(CaughtFish fish)
        {
            if (fish == null) return null;
            Vector3 forward = Cam.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = transform.forward;
            forward.Normalize();
            Vector3 pos = Player.transform.position + forward * 1.9f;
            RaycastHit hit;
            Vector3 rayFrom = pos + Vector3.up * 6f;
            if (Physics.Raycast(rayFrom, Vector3.down, out hit, 20f, ~0, QueryTriggerInteraction.Ignore))
                pos.y = hit.point.y + 0.25f;
            else
                pos.y = GameBoot.I.World.WaterLevel + 0.12f;

            var go = new GameObject("Fish (alive)");
            go.transform.SetParent(GameBoot.I.World.transform, false);
            go.transform.position = pos;
            var enemy = go.AddComponent<FishEnemy>();
            enemy.Init(fish, GameBoot.I.Rng, GameBoot.I);
            CurrentFish = enemy;
            if (GameBoot.I.Fishing != null) GameBoot.I.Fishing.EquipRod(false);
            EquipBestWeapon();
            if (GameBoot.I.UI != null)
            {
                GameBoot.I.UI.Toast("The " + fish.species + " is still alive! Finish it with a weapon.");
                GameBoot.I.UI.SetPrompt("LMB attack  ·  R reload  ·  F eat  ·  E collect");
            }
            return enemy;
        }

        public bool TestFireAt(CombatTarget target)
        {
            if (target == null || target.IsDead) return false;
            State.pistolOwned = true;
            State.pistolMag = 8;
            State.pistolAmmo = 16;
            EquipWeapon(2);
            Vector3 flat = target.transform.position - Player.transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f) flat = Player.transform.forward;
            Vector3 p = Cam.transform.position + flat.normalized * 6f;
            p.y = Cam.transform.position.y - 0.2f;
            target.transform.position = p;
            Physics.SyncTransforms();
            Vector3 aimAt = target.transform.position + Vector3.up * 0.22f;
            Vector3 to = aimAt - Cam.transform.position;
            float yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
            Player.ForceLook(yaw, pitch);
            int before = target.Hp;
            AttackNow();
            return target.Hp < before;
        }

        public void TestFinishTarget(CombatTarget target)
        {
            if (target != null && !target.IsDead)
                target.TakeDamage(target.MaxHp, false, false, false, target.transform.position + Vector3.up * 0.4f);
        }

        private void BuildWeaponVisuals()
        {
            weaponRoot = new GameObject("Weapon Visuals");
            weaponRoot.transform.SetParent(Cam.transform, false);
            weaponRoot.transform.localPosition = new Vector3(0.3f, -0.27f, 0.68f);
            weaponRoot.transform.localRotation = Quaternion.Euler(2f, -16f, 0f);
            weaponRootBasePos = weaponRoot.transform.localPosition;
            weaponRootBaseRot = weaponRoot.transform.localRotation;

            var fists = new GameObject("Fists");
            fists.transform.SetParent(weaponRoot.transform, false);
            fists.transform.localPosition = new Vector3(0.02f, -0.04f, 0.04f);
            AddPrimitive(fists.transform, PrimitiveType.Capsule, new Vector3(0f, -0.09f, 0.06f), new Vector3(0.08f, 0.16f, 0.08f), new Color32(0xC9, 0x93, 0x6A, 0xFF), new Vector3(72f, 0f, 0f));
            AddPrimitive(fists.transform, PrimitiveType.Sphere, new Vector3(0f, 0.02f, 0.24f), new Vector3(0.16f, 0.14f, 0.16f), new Color32(0xD9, 0xA4, 0x7A, 0xFF), Vector3.zero);
            AddPrimitive(fists.transform, PrimitiveType.Sphere, new Vector3(-0.08f, 0.04f, 0.28f), new Vector3(0.07f, 0.055f, 0.09f), new Color32(0xD9, 0xA4, 0x7A, 0xFF), Vector3.zero);
            weaponVisuals[0] = fists;

            var knife = new GameObject("Knife");
            knife.transform.SetParent(weaponRoot.transform, false);
            knife.transform.localPosition = new Vector3(0.02f, -0.02f, 0.06f);
            AddPart(knife.transform, new Vector3(0f, 0f, 0f), new Vector3(0.035f, 0.035f, 0.18f), new Color32(0x3B, 0x2E, 0x26, 0xFF), new Vector3(0f, 0f, 0f));
            AddPart(knife.transform, new Vector3(0f, 0.01f, 0.24f), new Vector3(0.03f, 0.012f, 0.34f), new Color32(0xD8, 0xD9, 0xDC, 0xFF), new Vector3(0f, 0f, -6f));
            AddPart(knife.transform, new Vector3(0f, -0.02f, -0.1f), new Vector3(0.055f, 0.11f, 0.09f), new Color32(0xC9, 0x93, 0x6A, 0xFF), new Vector3(8f, 0f, 0f));
            weaponVisuals[1] = knife;

            var pistol = new GameObject("Pistol");
            pistol.transform.SetParent(weaponRoot.transform, false);
            pistol.transform.localPosition = new Vector3(0.02f, -0.02f, 0.1f);
            pistol.transform.localScale = Vector3.one * 0.85f;
            AddPart(pistol.transform, new Vector3(0f, 0f, 0.04f), new Vector3(0.07f, 0.11f, 0.2f), new Color32(0x33, 0x38, 0x3E, 0xFF), Vector3.zero);
            AddPart(pistol.transform, new Vector3(0f, -0.09f, -0.06f), new Vector3(0.06f, 0.14f, 0.08f), new Color32(0x6A, 0x4B, 0x33, 0xFF), new Vector3(12f, 0f, 0f));
            AddPart(pistol.transform, new Vector3(0f, 0.02f, 0.18f), new Vector3(0.04f, 0.04f, 0.1f), new Color32(0x22, 0x26, 0x2A, 0xFF), Vector3.zero);
            AddPart(pistol.transform, new Vector3(0f, -0.13f, -0.09f), new Vector3(0.06f, 0.12f, 0.08f), new Color32(0xC9, 0x93, 0x6A, 0xFF), new Vector3(18f, 0f, 0f));
            weaponVisuals[2] = pistol;

            var shotgun = new GameObject("Shotgun");
            shotgun.transform.SetParent(weaponRoot.transform, false);
            shotgun.transform.localPosition = new Vector3(0.02f, -0.02f, 0.1f);
            shotgun.transform.localScale = Vector3.one * 0.72f;
            AddPart(shotgun.transform, new Vector3(0f, 0f, 0.16f), new Vector3(0.07f, 0.1f, 0.42f), new Color32(0x5B, 0x3D, 0x28, 0xFF), Vector3.zero);
            AddPart(shotgun.transform, new Vector3(0f, 0.015f, 0.48f), new Vector3(0.055f, 0.055f, 0.36f), new Color32(0x2B, 0x2E, 0x31, 0xFF), Vector3.zero);
            AddPart(shotgun.transform, new Vector3(0f, -0.05f, -0.18f), new Vector3(0.06f, 0.13f, 0.16f), new Color32(0x4A, 0x32, 0x22, 0xFF), new Vector3(16f, 0f, 0f));
            AddPart(shotgun.transform, new Vector3(0f, -0.14f, -0.14f), new Vector3(0.06f, 0.12f, 0.08f), new Color32(0xC9, 0x93, 0x6A, 0xFF), new Vector3(18f, 0f, 0f));
            weaponVisuals[3] = shotgun;
        }

        private void AddPart(Transform parent, Vector3 pos, Vector3 scale, Color color, Vector3 euler)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("weaponPart", color, 0.35f);
            weaponParts.Add(go.GetComponent<MeshRenderer>());
        }

        private void AddPrimitive(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Color color, Vector3 euler)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("weaponPart", color, 0.35f);
            weaponParts.Add(go.GetComponent<MeshRenderer>());
        }

        public void ApplySkin(int index)
        {
            Color c;
            if (index == 1) c = new Color32(0xFF, 0x7A, 0x59, 0xFF);
            else if (index == 2) c = new Color32(0x3A, 0x5A, 0x8C, 0xFF);
            else if (index == 3) c = new Color32(0xF2, 0xB2, 0x3E, 0xFF);
            else if (index == 4) c = new Color32(0x6F, 0xD9, 0xB0, 0xFF);
            else return;
            for (int i = 0; i < weaponParts.Count; i++)
            {
                if (weaponParts[i] == null) continue;
                var mat = weaponParts[i].material;
                if (mat != null && mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            }
        }

        public void RefreshWeaponVisual()
        {
            if (weaponRoot == null) return;
            bool rod = GameBoot.I != null && GameBoot.I.Fishing != null && GameBoot.I.Fishing.RodEquipped;
            bool blocked = GameBoot.I != null && GameBoot.I.UI != null && GameBoot.I.UI.AnyPanelOpen;
            bool show = !rod && !blocked && GameBoot.I != null && GameBoot.I.Playing;
            weaponRoot.SetActive(show);
            for (int i = 0; i < weaponVisuals.Length; i++)
                if (weaponVisuals[i] != null) weaponVisuals[i].SetActive(i == State.weaponIndex);
        }

        private void DrawTracer(Vector3 from, Vector3 to)
        {
            var go = new GameObject("Tracer");
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = 0.02f;
            line.endWidth = 0.008f;
            line.useWorldSpace = true;
            line.material = WorldBuilder.MakeMat("tracer", new Color32(0xFF, 0xE7, 0x8A, 0xFF), 0.1f);
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Destroy(go, 0.07f);
        }

        private void MuzzleFlash()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(Cam.transform, false);
            go.transform.localPosition = new Vector3(0.34f, -0.28f, 0.78f);
            go.transform.localScale = Vector3.one * 0.06f;
            go.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("muzzle", new Color32(0xFF, 0xD8, 0x6A, 0xFF), 0.1f);
            Destroy(go, 0.05f);
        }

        private void HitSpark(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.12f;
            go.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("hitSpark", new Color32(0xFF, 0xD8, 0x6A, 0xFF), 0.1f);
            Destroy(go, 0.12f);
        }
    }
}
