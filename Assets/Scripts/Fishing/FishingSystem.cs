using UnityEngine;

namespace Cove
{
    public enum FishingState
    {
        Idle,
        Charging,
        Flying,
        Waiting,
        Nibble,
        Bite,
        Reeling,
        Landed,
        Snapped
    }

    public class FishingSystem : MonoBehaviour
    {
        public PlayerController Player;
        public Camera Cam;
        public AudioLibrary Audio;
        public System.Random Rng;

        public FishingState State { get; private set; }
        public bool RodEquipped { get; private set; }
        public float Charge { get; private set; }
        public float Tension { get; private set; }
        public float Progress { get; private set; }
        public float SafeTension { get; private set; }
        public bool BaitUsed { get; private set; }
        public CaughtFish LastCatch { get; private set; }

        public bool UseReelOverride;
        public bool ReelHoldOverride;
        public bool RodHidden;
        public string LastCastError = "";

        private GameObject bobber;
        private LineRenderer line;
        private GameObject rodVisual;
        private Transform bobberDot;

        private float chargeTimer;
        private float flightT;
        private Vector3 flightStart;
        private Vector3 castTarget;
        private float waitTimer;
        private float nibbleTimer;
        private float biteTimer;
        private float tension;
        private float progress;
        private float stamina;
        private float pullTimer;
        private float pullTime;
        private float stateTimer;
        private float reelTickTimer;
        private float bobT;
        private bool nibblePlayed;
        private GameObject heldFish;
        private float heldFishTimer;

        private void Start()
        {
            BuildRodVisual();
        }

        private void BuildRodVisual()
        {
            rodVisual = new GameObject("Fishing Rod");
            rodVisual.transform.SetParent(Cam.transform, false);
            rodVisual.transform.localPosition = new Vector3(0.43f, -0.46f, 0.42f);
            rodVisual.transform.localRotation = Quaternion.Euler(60f, 30f, 0f);

            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(shaft.GetComponent<Collider>());
            shaft.transform.SetParent(rodVisual.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 0.31f, 0f);
            shaft.transform.localScale = new Vector3(0.014f, 0.32f, 0.014f);
            shaft.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("rodShaft", new Color32(0x8A, 0x6A, 0x45, 0xFF), 0.2f);

            var grip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(grip.GetComponent<Collider>());
            grip.transform.SetParent(rodVisual.transform, false);
            grip.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            grip.transform.localScale = new Vector3(0.04f, 0.12f, 0.04f);
            grip.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("rodGrip", new Color32(0x3B, 0x2E, 0x26, 0xFF), 0.1f);

            var reel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(reel.GetComponent<Collider>());
            reel.transform.SetParent(rodVisual.transform, false);
            reel.transform.localPosition = new Vector3(0.05f, 0.11f, 0.03f);
            reel.transform.localScale = Vector3.one * 0.05f;
            reel.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("rodReel", new Color32(0xD9, 0xD4, 0xC8, 0xFF), 0.6f);

            rodVisual.SetActive(false);
        }

        public void EquipRod(bool equip)
        {
            if (RodEquipped == equip) return;
            RodEquipped = equip;
            if (rodVisual != null) rodVisual.SetActive(equip && !RodHidden);
            if (!equip) CancelFishing(false);
            if (GameBoot.I != null && GameBoot.I.UI != null)
                GameBoot.I.UI.Toast(equip ? "Fishing rod equipped. Hold left mouse to charge a cast." : "Rod put away.");
        }

        public void ToggleRod()
        {
            EquipRod(!RodEquipped);
        }

        private void Update()
        {
            var game = GameBoot.I;
            if (game == null || !game.Playing) return;
            if (game.UI == null) return;
            if (Cam == null) return;

            bool blocked = game.UI.AnyPanelOpen;

            if (!blocked && Input.GetKeyDown(KeyCode.Alpha1))
            {
                ToggleRod();
            }

            if (blocked)
            {
                if (State == FishingState.Charging) CancelFishing(false);
                return;
            }

            bool lmbDown = !UseReelOverride && Input.GetMouseButtonDown(0);
            bool lmbHeld = UseReelOverride ? ReelHoldOverride : Input.GetMouseButton(0);

            bobT += Time.deltaTime;
            if (heldFish != null)
            {
                heldFishTimer -= Time.deltaTime;
                if (heldFishTimer <= 0f) ClearHeldFish();
            }
            if (rodVisual != null) rodVisual.SetActive(RodEquipped && !RodHidden);

            switch (State)
            {
                case FishingState.Idle:
                    if (RodEquipped && lmbDown) TryBeginCharge();
                    break;

                case FishingState.Charging:
                    if (!RodEquipped) { CancelFishing(false); break; }
                    chargeTimer += Time.deltaTime;
                    Charge = Mathf.Clamp01(chargeTimer / 1.15f);
                    game.UI.SetCharge(true, Charge);
                    if (!UseReelOverride && Input.GetMouseButtonUp(0))
                    {
                        ReleaseCast();
                    }
                    break;

                case FishingState.Flying:
                    flightT += Time.deltaTime;
                    float ft = Mathf.Clamp01(flightT / 0.42f);
                    if (bobber != null)
                    {
                        Vector3 p = Vector3.Lerp(flightStart, castTarget, ft);
                        p.y = Mathf.Lerp(flightStart.y, GameBoot.I.World.WaterLevel, ft) + Mathf.Sin(ft * Mathf.PI) * 2.6f;
                        bobber.transform.position = p;
                    }
                    UpdateLine();
                    if (ft >= 1f)
                    {
                        if (bobber != null) bobber.transform.position = new Vector3(castTarget.x, GameBoot.I.World.WaterLevel + 0.04f, castTarget.z);
                        EnterWaiting();
                    }
                    break;

                case FishingState.Waiting:
                    if (!UseReelOverride && Input.GetMouseButtonDown(1)) { CancelFishing(true); break; }
                    UpdateBobberIdle(0f);
                    waitTimer -= Time.deltaTime;
                    if (!nibblePlayed && nibbleTimer > 0f)
                    {
                        nibbleTimer -= Time.deltaTime;
                        if (nibbleTimer <= 0f)
                        {
                            nibblePlayed = true;
                            Audio.Play(Audio.Nibble, 0.8f, 1f);
                            game.UI.SetFishing(true, 0f, 0f, 0f);
                            game.UI.Toast("Something is nibbling…");
                        }
                    }
                    if (waitTimer <= 0f)
                    {
                        EnterBite();
                    }
                    break;

                case FishingState.Nibble:
                    UpdateBobberIdle(Mathf.Sin(bobT * 22f) * 0.09f);
                    nibbleTimer -= Time.deltaTime;
                    if (nibbleTimer <= 0f) EnterBite();
                    break;

                case FishingState.Bite:
                    if (!UseReelOverride && Input.GetMouseButtonDown(1)) { CancelFishing(true); break; }
                    UpdateBobberIdle(Mathf.Sin(bobT * 26f) * 0.15f);
                    biteTimer -= Time.deltaTime;
                    if (biteTimer <= 0f)
                    {
                        game.UI.ShowBite(false);
                        game.UI.Toast("It slipped away. Watch for the next bite.");
                        EnterWaiting();
                    }
                    else if (lmbDown)
                    {
                        Hook();
                    }
                    break;

                case FishingState.Reeling:
                    UpdateReel(lmbHeld, blocked);
                    break;

                case FishingState.Landed:
                case FishingState.Snapped:
                    stateTimer -= Time.deltaTime;
                    if (bobber != null)
                    {
                        bobber.transform.position = new Vector3(bobber.transform.position.x, GameBoot.I.World.WaterLevel + 0.04f, bobber.transform.position.z);
                    }
                    UpdateLine();
                    if (stateTimer <= 0f)
                    {
                        CleanupBobber();
                        State = FishingState.Idle;
                    }
                    break;
            }
        }

        private bool TryBeginCharge()
        {
            LastCastError = "";
            if (!TryAimWater(out Vector3 target))
            {
                if (string.IsNullOrEmpty(LastCastError)) LastCastError = "unknown";
                GameBoot.I.UI.Toast("Face the open water to cast.");
                Audio.Play(Audio.Error, 0.5f, 1f);
                return false;
            }
            ClearHeldFish();
            State = FishingState.Charging;
            chargeTimer = 0f;
            Charge = 0f;
            castTarget = target;
            GameBoot.I.UI.SetCharge(true, 0f);
            Audio.Play(Audio.Cast, 0.35f, 1.4f);
            return true;
        }

        private bool TryAimWater(out Vector3 target)
        {
            target = Vector3.zero;
            Vector3 origin = Cam.transform.position;
            Vector3 dir = Cam.transform.forward;
            if (dir.y > -0.03f) { LastCastError = "not aiming down"; return false; }
            float waterY = GameBoot.I.World.WaterLevel;
            float t = (waterY - origin.y) / dir.y;
            if (t < 2f || t > 55f) { LastCastError = "water too close or too far"; return false; }
            Vector3 hit = origin + dir * t;
            if (GameBoot.I.World.IsInsideIsland(hit, 0.92f)) { LastCastError = "aiming at land"; return false; }
            target = hit;
            return true;
        }

        private void ReleaseCast()
        {
            GameBoot.I.UI.SetCharge(false, 0f);
            float charge = Mathf.Clamp01(chargeTimer / 1.15f);
            float rodRange = GameData.Rods[GameBoot.I.State.rodTier].castDistance;
            Vector3 origin = Cam.transform.position;
            Vector3 aim = Cam.transform.forward;
            Vector3 flatAim = new Vector3(aim.x, 0f, aim.z).normalized;
            if (flatAim.sqrMagnitude < 0.01f) flatAim = Cam.transform.forward;
            float dist = Mathf.Lerp(4f, rodRange, Mathf.Clamp01(charge));
            waterAim(origin, flatAim, dist, out castTarget);

            BaitUsed = false;
            if (GameBoot.I.State.bait > 0)
            {
                GameBoot.I.State.bait--;
                BaitUsed = true;
                GameBoot.I.UI.RefreshHud();
            }

            CreateBobber();
            flightStart = RodTip();
            flightT = 0f;
            if (bobber != null) bobber.transform.position = flightStart;
            State = FishingState.Flying;
            Audio.Play(Audio.Cast, 0.6f, 0.85f + charge * 0.4f);
        }

        private void waterAim(Vector3 origin, Vector3 flatDir, float dist, out Vector3 target)
        {
            float waterY = GameBoot.I.World.WaterLevel;
            float step = 0.4f;
            float last = dist;
            for (float d = 1f; d <= dist; d += step)
            {
                Vector3 p = origin + flatDir * d;
                if (GameBoot.I.World.IsInsideIsland(new Vector3(p.x, waterY, p.z), 0.9f))
                {
                    last = Mathf.Max(2f, d - 1.2f);
                    break;
                }
            }
            target = origin + flatDir * last;
            target.y = waterY;
        }

        private void CreateBobber()
        {
            CleanupBobber();
            bobber = new GameObject("Bobber");
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(bobber.transform, false);
            body.transform.localScale = Vector3.one * 0.16f;
            body.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("bobberRed", new Color32(0xE8, 0x53, 0x3F, 0xFF), 0.3f);

            var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(cap.GetComponent<Collider>());
            cap.transform.SetParent(bobber.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.07f, 0f);
            cap.transform.localScale = new Vector3(0.1f, 0.09f, 0.1f);
            cap.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("bobberWhite", Color.white, 0.3f);
            bobberDot = bobber.transform;

            var lineObj = new GameObject("Fishing Line");
            lineObj.transform.SetParent(transform);
            line = lineObj.AddComponent<LineRenderer>();
            line.positionCount = 3;
            line.startWidth = 0.022f;
            line.endWidth = 0.012f;
            line.useWorldSpace = true;
            line.material = WorldBuilder.MakeMat("line", new Color32(0xFF, 0xF8, 0xE6, 0xFF), 0.1f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
        }

        private void CleanupBobber()
        {
            if (bobber != null) Destroy(bobber);
            if (line != null) Destroy(line.gameObject);
            bobber = null;
            line = null;
            bobberDot = null;
        }

        private Vector3 RodTip()
        {
            return Cam.transform.position + Cam.transform.forward * 0.95f + Cam.transform.right * 0.24f - Cam.transform.up * 0.24f;
        }

        private void UpdateLine()
        {
            if (line == null || bobber == null) return;
            Vector3 tip = RodTip();
            Vector3 end = bobber.transform.position + Vector3.up * 0.06f;
            Vector3 mid = Vector3.Lerp(tip, end, 0.5f) + Vector3.down * 0.35f;
            line.SetPosition(0, tip);
            line.SetPosition(1, mid);
            line.SetPosition(2, end);
        }

        private void UpdateBobberIdle(float dip)
        {
            if (bobber == null) return;
            float y = GameBoot.I.World.WaterLevel + 0.05f + Mathf.Sin(bobT * 2.1f) * 0.025f + dip;
            bobber.transform.position = new Vector3(bobber.transform.position.x, y, bobber.transform.position.z);
            UpdateLine();
        }

        private void EnterWaiting()
        {
            State = FishingState.Waiting;
            float baseWait = (float)(1.6 + Rng.NextDouble() * 2.6);
            waitTimer = BaitUsed ? baseWait * 0.55f : baseWait;
            nibbleTimer = waitTimer * 0.55f;
            nibblePlayed = false;
            biteTimer = 0f;
            GameBoot.I.UI.SetFishing(false, 0f, 0f, 0f);
            GameBoot.I.UI.ShowBite(false);
            GameBoot.I.UI.SetPrompt("Waiting for a bite…  (right click to reel in)");
            if (!UseReelOverride && Input.GetMouseButtonDown(1)) CancelFishing(true);
        }

        private void EnterBite()
        {
            if (!UseReelOverride && Input.GetMouseButton(1)) { CancelFishing(true); return; }
            State = FishingState.Bite;
            biteTimer = BaitUsed ? 1.7f : 1.35f;
            Audio.Play(Audio.Bite, 0.9f, 1f);
            GameBoot.I.UI.ShowBite(true);
            GameBoot.I.UI.SetPrompt("Click now to hook the fish!");
        }

        public void ForceBiteNow()
        {
            if (State == FishingState.Waiting || State == FishingState.Nibble || State == FishingState.Flying)
            {
                if (State == FishingState.Flying) flightT = 10f;
                EnterBite();
            }
        }

        private void Hook()
        {
            if (State != FishingState.Bite) return;
            State = FishingState.Reeling;
            tension = 0.18f;
            progress = 0f;
            stamina = 1f;
            pullTimer = 0.9f;
            pullTime = 0f;
            GameBoot.I.UI.ShowBite(false);
            Audio.Play(Audio.Hook, 0.8f, 1f);
            GameBoot.I.UI.SetFishing(true, tension, progress, SafeTension);
            GameBoot.I.UI.SetPrompt("Hold left mouse to reel. Release to ease the line.");
            SafeTension = GameData.Rods[GameBoot.I.State.rodTier].tensionTolerance;
        }

        public bool HookNow()
        {
            if (State != FishingState.Bite) return false;
            Hook();
            return true;
        }

        private void UpdateReel(bool holding, bool blocked)
        {
            var rod = GameData.Rods[GameBoot.I.State.rodTier];
            SafeTension = rod.tensionTolerance;
            float dt = Time.deltaTime;

            pullTimer -= dt;
            if (pullTime > 0f)
            {
                pullTime -= dt;
            }
            else if (pullTimer <= 0f)
            {
                pullTime = 0.4f + (float)Rng.NextDouble() * 0.4f;
                pullTimer = 0.9f + (float)Rng.NextDouble() * 1.3f;
                Audio.Play(Audio.Nibble, 0.4f, 0.6f);
            }
            bool pulling = pullTime > 0f;

            if (holding)
            {
                float rise = 0.5f + (1f - rod.tensionTolerance) * 0.55f;
                tension += rise * dt;
                if (pulling) tension += 0.35f * dt;
                progress += 0.32f * rod.reelSpeed * (1.1f - 0.4f * stamina) * dt;
                stamina = Mathf.Clamp01(stamina - 0.16f * dt);
                Audio.PlayReelTick();
                if (bobber != null)
                {
                    Vector3 bp = bobber.transform.position;
                    bobber.transform.position = new Vector3(bp.x + Mathf.Sin(bobT * 30f) * 0.006f, GameBoot.I.World.WaterLevel + 0.03f, bp.z);
                }
            }
            else
            {
                tension -= 0.8f * dt;
                progress -= 0.03f * dt;
                stamina = Mathf.Clamp01(stamina + 0.03f * dt);
            }

            tension = Mathf.Clamp01(tension);
            progress = Mathf.Clamp01(progress);
            Tension = tension;
            Progress = progress;
            UpdateLine();

            GameBoot.I.UI.SetFishing(true, tension, progress, SafeTension);
            GameBoot.I.UI.SetFishStamina(stamina);

            if (tension >= 0.999f)
            {
                Snap();
                return;
            }
            if (progress >= 0.999f)
            {
                LandFish();
                return;
            }
            if (!UseReelOverride && Input.GetMouseButtonDown(1))
            {
                CancelFishing(true);
            }
        }

        private void Snap()
        {
            Audio.Play(Audio.Snap, 0.9f, 1f);
            GameBoot.I.UI.Toast("Snap! The line broke. Use shorter reels to keep tension down.");
            GameBoot.I.UI.SetFishing(false, 0f, 0f, 0f);
            GameBoot.I.UI.SetPrompt("");
            State = FishingState.Snapped;
            stateTimer = 1.2f;
            tension = 0f;
            progress = 0f;
        }

        private void LandFish()
        {
            var game = GameBoot.I;
            var fish = GameData.RollFish(Rng, BaitUsed, game.CurrentIslandIndex + 1);
            LastCatch = fish;
            ClearHeldFish();
            Audio.Play(Audio.Fanfare, 0.8f, 1f);
            GameBoot.I.UI.SetFishing(false, 0f, 0f, 0f);
            GameBoot.I.UI.SetPrompt("");

            game.State.catchCount++;
            if (game.State.catchCount == 1) game.UnlockAchievement("First Catch");
            game.DiscoverFish(fish);
            game.UI.RefreshHud();
            game.UI.ShowCatch(fish, false);
            if (game.Combat != null) game.Combat.SpawnCaughtFish(fish);
            if (game.Audio != null) game.Audio.Play(game.Audio.Hook, 0.5f, 1.3f);

            State = FishingState.Landed;
            stateTimer = 1.0f;
        }

        public void CancelFishing(bool showMessage)
        {
            if (State == FishingState.Idle) return;
            ClearHeldFish();
            CleanupBobber();
            GameBoot.I.UI.SetCharge(false, 0f);
            GameBoot.I.UI.ShowBite(false);
            GameBoot.I.UI.SetFishing(false, 0f, 0f, 0f);
            GameBoot.I.UI.SetPrompt("");
            State = FishingState.Idle;
            if (showMessage) GameBoot.I.UI.Toast("You reeled in your line.");
        }

        private void ShowHeldFish(CaughtFish fish)
        {
            ClearHeldFish();
            if (Cam == null) return;
            string baseModel = "Fish1";
            switch (fish.species)
            {
                case "Coral Perch":
                case "Lagoon Snapper":
                case "Amber Tang":
                case "Moonray Bass":
                    baseModel = "Fish2";
                    break;
                case "Stormback Eel":
                case "Golden Leviathan":
                    baseModel = "Fish3";
                    break;
            }
            string model = CC0Library.Prefab(baseModel + "_display") != null ? baseModel + "_display" : baseModel;
            Color tint = Color.white;
            if (!string.IsNullOrEmpty(fish.color)) ColorUtility.TryParseHtmlString(fish.color, out tint);
            float height = Mathf.Clamp(0.18f + fish.weight * 0.015f, 0.18f, 0.45f);
            var go = CC0Library.SpawnHeld(model, Cam.transform, new Vector3(0.12f, -0.12f, 1.05f), Quaternion.Euler(0f, 90f, 0f), height, tint);
            if (go == null) return;
            var bob = go.AddComponent<IdleBob>();
            bob.Height = 0.012f;
            bob.Speed = 2.2f;
            bob.SwayDegrees = 9f;
            heldFish = go;
            heldFishTimer = 4.2f;
        }

        public void DebugPreviewFish(string species)
        {
            ShowHeldFish(new CaughtFish
            {
                species = species,
                tier = "rare",
                weight = 3.2f,
                value = 250,
                rareVariant = false,
                color = "#C79BFF"
            });
        }
        private void ClearHeldFish()
        {
            if (heldFish != null) Destroy(heldFish);
            heldFish = null;
        }
        public void TestStartCharge()
        {
            if (RodEquipped) TryBeginCharge();
        }

        public void TestRelease()
        {
            if (State == FishingState.Charging) ReleaseCast();
        }

        public void TestSetCharge(float value)
        {
            if (State == FishingState.Charging) chargeTimer = value * 1.15f;
        }
    }
}
