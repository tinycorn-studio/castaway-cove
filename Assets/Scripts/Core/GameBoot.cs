using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Cove
{
    public class GameBoot : MonoBehaviour
    {
        public static GameBoot I;
        public GameState State = new GameState();
        public WorldBuilder World;
        public PlayerController Player;
        public FishingSystem Fishing;
        public PlayerCombat Combat;
        public UIManager UI;
        public AudioLibrary Audio;
        public Boat Boat;
        public CrabBoss Boss;
        public IslandBoss ActiveIslandBoss;
        public GameObject BossShell;
        public GameObject BossTrophy;
        public int ActiveIslandBossType;
        public bool bossSpawned;
        public System.Random Rng;

        public bool Playing;
        public bool Busy;        public GameObject PlayerObject;
        public Camera MainCamera;
        public Light MainLight;

        public int CurrentIslandIndex
        {
            get
            {
                if (World == null || Player == null || World.Islands == null || World.Islands.Count == 0) return 0;
                int best = 0;
                float bestDistance = float.MaxValue;
                for (int i = 0; i < World.Islands.Count; i++)
                {
                    float d = Vector3.Distance(Player.transform.position, World.Islands[i].center);
                    if (d < bestDistance) { bestDistance = d; best = i; }
                }
                return best;
            }
        }

        private bool selfTestMode;
        private bool screenshotMode;
        private string selfTestPath;
        private string screenshotPath;
        private string shotUi;
        private bool quest2Announced;
        private bool quest3Announced;
        private bool wasPaused;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string className, string windowName);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        private void Awake()
        {
            I = this;
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            DontDestroyOnLoad(gameObject);

            string[] args = Environment.GetCommandLineArgs();
            selfTestMode = HasFlag(args, "-selfTest") || HasFlag(args, "--selfTest");
            screenshotMode = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--self-test")
                {
                    selfTestMode = true;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-")) selfTestPath = args[i + 1];
                }
                else if (args[i].StartsWith("--self-test="))
                {
                    selfTestMode = true;
                    selfTestPath = args[i].Substring("--self-test=".Length);
                }
                else if (args[i] == "--screenshot" || args[i] == "-screenshot")
                {
                    screenshotMode = true;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-")) screenshotPath = args[i + 1];
                }
                else if (args[i].StartsWith("--screenshot="))
                {
                    screenshotMode = true;
                    screenshotPath = args[i].Substring("--screenshot=".Length);
                }
                else if (args[i] == "--shot-ui" && i + 1 < args.Length)
                {
                    shotUi = args[i + 1];
                }
            }
            if (selfTestMode && string.IsNullOrEmpty(selfTestPath))
                selfTestPath = Path.Combine(Application.persistentDataPath, "self-test.json");
            if (screenshotMode && string.IsNullOrEmpty(screenshotPath))
                screenshotPath = Path.Combine(Application.persistentDataPath, "screenshot.png");

            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);

            GameData.Init();
            State.ResetToDefaults();
            Rng = selfTestMode ? new System.Random(12345) : new System.Random(Environment.TickCount);

            BuildWorld();
            BuildAudio();
            BuildPlayer();
            BuildUI();
            Fishing = PlayerObject.AddComponent<FishingSystem>();
            Fishing.Player = Player;
            Fishing.Cam = MainCamera;
            Fishing.Audio = Audio;
            Fishing.Rng = Rng;

            Combat = PlayerObject.AddComponent<PlayerCombat>();
            Combat.Player = Player;
            Combat.Cam = MainCamera;
            Combat.Audio = Audio;

            if (selfTestMode)
            {
                StartGameInternal(false);
                Player.ExternalInput = false;
                var test = gameObject.AddComponent<SelfTestRunner>();
                test.Init(this, selfTestPath);
            }
            else if (screenshotMode)
            {
                UI.ShowMenu(true, SaveSystem.Exists());
                Playing = false;
                Player.inputEnabled = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                StartCoroutine(ScreenshotRoutine(screenshotPath));
            }
            else
            {
                UI.ShowMenu(true, SaveSystem.Exists());
                UI.ShowPause(false);
                Playing = false;
                Player.inputEnabled = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private static bool HasFlag(string[] args, string flag)
        {
            for (int i = 0; i < args.Length; i++) if (args[i] == flag) return true;
            return false;
        }

        private void BuildWorld()
        {
            var worldGo = new GameObject("World");
            worldGo.transform.SetParent(transform, false);
            World = worldGo.AddComponent<WorldBuilder>();
            World.worldFont = GetWorldFont();
            World.Build();
        }

        private Font GetWorldFont()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { f = null; }
            if (f == null)
            {
                try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { f = null; }
            }
            if (f == null) f = Font.CreateDynamicFontFromOSFont("Arial", 48);
            return f;
        }

        private void BuildAudio()
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(transform, false);
            Audio = go.AddComponent<AudioLibrary>();
            Audio.Init();
            Boat = World.Boat;
        }

        private void BuildPlayer()
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(transform, false);
            MainCamera = camGo.AddComponent<Camera>();
            MainCamera.fieldOfView = 70f;
            MainCamera.nearClipPlane = 0.06f;
            MainCamera.farClipPlane = 700f;
            MainCamera.clearFlags = CameraClearFlags.SolidColor;
            MainCamera.backgroundColor = Palette.Sky;
            MainCamera.allowHDR = false;
            camGo.AddComponent<AudioListener>();

            PlayerObject = new GameObject("Player");
            PlayerObject.transform.SetParent(transform, false);
            PlayerObject.AddComponent<CharacterController>();
            Player = PlayerObject.AddComponent<PlayerController>();
            Player.WaterLevel = World.WaterLevel;

            var pivot = new GameObject("Camera Pivot");
            pivot.transform.SetParent(PlayerObject.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            Player.CameraPivot = pivot.transform;
            camGo.transform.SetParent(pivot.transform, false);
            camGo.transform.localPosition = Vector3.zero;
            camGo.transform.localRotation = Quaternion.identity;
            Player.Cam = MainCamera;

            var interact = PlayerObject.AddComponent<PlayerInteraction>();
            interact.player = Player;

            PlayerObject.transform.position = World.StartSpawn.position;
            Player.ForceLook(0f, 0f);
        }

        private void BuildUI()
        {
            var uiGo = new GameObject("UI");
            uiGo.transform.SetParent(transform, false);
            UI = uiGo.AddComponent<UIManager>();
            UI.BuildUI(MainCamera);
        }

        private void StartGameInternal(bool loadSave)
        {
            if (loadSave)
            {
                var data = SaveSystem.Load(out string err);
                if (data != null)
                {
                    ApplySave(data);
                    UI.Toast("Welcome back to Castaway Cove.");
                }
                else
                {
                    NewGameState();
                    UI.Toast("No save found — starting a fresh cove.");
                }
            }
            else
            {
                NewGameState();
                UI.Toast("Welcome to Castaway Cove! Press 1 for your rod.");
            }

            UI.ShowMenu(false, SaveSystem.Exists());
            UI.ShowPause(false);
            UI.ShowShop(false);
            UI.ShowQuests(false);
            UI.ShowWeapons(false);
            UI.ShowGrill(false);
            UI.ShowFishipedia(false);
            UI.ShowFortune(false);
            Playing = true;
            Player.inputEnabled = true;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            UI.RefreshHud();
            if (Fishing != null) Fishing.EquipRod(false);
            if (Combat != null)
            {
                Combat.RefreshWeaponVisual();
                UI.SetVitals(State.hp, 100f, State.hunger, 100f);
            }
        }

        private void NewGameState()
        {
            bool keepHard = State.hardMode;
            State.ResetToDefaults();
            State.hardMode = keepHard;
            quest2Announced = false;
            quest3Announced = false;
            bossSpawned = false;
            BossShell = null;
            BossTrophy = null;
            ActiveIslandBossType = 0;
            if (Boss != null) { Destroy(Boss.gameObject); Boss = null; }
            if (ActiveIslandBoss != null) { Destroy(ActiveIslandBoss.gameObject); ActiveIslandBoss = null; }
            var fishEnemies = FindObjectsOfType<FishEnemy>();
            for (int i = 0; i < fishEnemies.Length; i++) if (fishEnemies[i] != null) Destroy(fishEnemies[i].gameObject);
            if (Player != null)
            {
                Player.transform.position = World.StartSpawn.position;
                Player.ForceLook(0f, 0f);
            }
            if (Boat != null)
            {
                Boat.transform.position = new Vector3(4.2f, 0.32f, 25.5f);
                Boat.Yaw = 8f;
                Boat.transform.rotation = Quaternion.Euler(0f, 8f, 0f);
                Boat.Occupied = false;
            }
            if (Player != null)
            {
                Player.InBoat = false;
                Player.Controller.enabled = true;
            }
            if (Combat != null)
            {
                Combat.CurrentFish = null;
                Combat.RefreshWeaponVisual();
            }
            if (UI != null) UI.SetVitals(State.hp, 100f, State.hunger, 100f);
        }

        public void StartGame(bool loadSave)
        {
            StartGameInternal(loadSave);
        }

        public void OpenShop()
        {
            if (!Playing) return;
            if (Fishing != null) Fishing.CancelFishing(false);
            UI.ShowQuests(false);
            UI.ShowWeapons(false);
            UI.ShowShop(true);
            UI.RefreshHud();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseShop()
        {
            UI.ShowShop(false);
            if (Playing && !UI.AnyPanelOpen) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }

        public void OpenQuests()
        {
            if (!Playing) return;
            if (Fishing != null) Fishing.CancelFishing(false);
            UI.ShowShop(false);
            UI.ShowWeapons(false);
            UI.ShowQuests(true);
            UI.RefreshQuests();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseQuests()
        {
            UI.ShowQuests(false);
            if (Playing && !UI.AnyPanelOpen) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }

        public void SetPaused(bool paused)
        {
            if (!Playing) return;
            UI.ShowPause(paused);
            if (paused)
            {
                if (Fishing != null) Fishing.CancelFishing(false);
                Player.inputEnabled = false;
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Player.inputEnabled = true;
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void ToggleDifficulty()
        {
            State.hardMode = !State.hardMode;
            UI.RefreshDifficulty();
            UI.Toast("Difficulty set to " + (State.hardMode ? "Hard: tougher creatures, more damage." : "Normal.") + " Applies to newly spawned creatures.");
        }

        public void ToggleBoat()
        {
            if (!Playing) return;
            if (Player == null || Boat == null) return;
            if (!Player.InBoat && !State.boatKeys)
            {
                UI.Toast("The rowboat is locked. Defeat the Spider Crab and hand its shell to Mira for the keys.");
                return;
            }
            if (Fishing != null) Fishing.CancelFishing(false);

            if (Player.InBoat)
            {
                Player.InBoat = false;
                Boat.Occupied = false;
                Player.Controller.enabled = true;
                Vector3 exit = Boat.transform.position + Boat.transform.right * 2.1f + Vector3.up * 0.4f;
                Player.transform.position = exit;
                Player.inputEnabled = true;
                UI.Toast("You slip back into the water. E near the boat to climb in again.");
            }
            else
            {
                if (Vector3.Distance(Player.transform.position, Boat.transform.position) > 5.5f)
                {
                    UI.Toast("Get closer to the rowboat first.");
                    return;
                }
                Player.InBoat = true;
                Boat.Occupied = true;
                Player.Controller.enabled = false;
                Player.transform.position = Boat.SeatPosition;
                Player.Yaw = Boat.Yaw;
                Player.ForceLook(Boat.Yaw, -4f);
                UI.Toast("WASD rows the boat. E hops out. You can fish from here.");
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void BuyRod(int target)
        {
            if (target < 0 || target >= GameData.Rods.Length) return;
            if (target <= State.rodTier)
            {
                UI.Toast("You already own that rod.");
                return;
            }
            if (target != State.rodTier + 1)
            {
                UI.Toast("Buy the previous rod first.");
                Audio.Play(Audio.Error, 0.6f, 1f);
                return;
            }
            int price = GameData.Rods[target].price;
            if (State.money < price)
            {
                UI.Toast("Not enough money for that rod.");
                Audio.Play(Audio.Error, 0.6f, 1f);
                return;
            }
            State.money -= price;
            State.rodTier = target;
            Audio.Play(Audio.Buy, 0.8f, 1f);
            UI.Toast("Bought the " + GameData.Rods[target].name + "! Longer casts and calmer reels.");
            UI.RefreshHud();
            AutoSave();
        }

        public void BuyRod()
        {
            BuyRod(State.rodTier + 1);
        }

        public void BuyBait()
        {
            if (State.money < GameData.BaitPackPrice)
            {
                UI.Toast("Not enough money for bait.");
                Audio.Play(Audio.Error, 0.6f, 1f);
                return;
            }
            State.money -= GameData.BaitPackPrice;
            State.bait += GameData.BaitPackAmount;
            Audio.Play(Audio.Buy, 0.8f, 1f);
            UI.Toast("Bought a bait pack. +" + GameData.BaitPackAmount + " bait.");
            UI.RefreshHud();
            AutoSave();
        }

        public void BuyBag()
        {
            if (State.bagCapacity >= GameData.BagUpgradeCapacity)
            {
                UI.Toast("Your bag is already fully upgraded.");
                return;
            }
            if (State.money < GameData.BagUpgradePrice)
            {
                UI.Toast("Not enough money for the bag upgrade.");
                Audio.Play(Audio.Error, 0.6f, 1f);
                return;
            }
            State.money -= GameData.BagUpgradePrice;
            State.bagCapacity = GameData.BagUpgradeCapacity;
            Audio.Play(Audio.Buy, 0.8f, 1f);
            UI.Toast("Bag upgraded! It now holds " + State.bagCapacity + " fish.");
            UI.RefreshHud();
            AutoSave();
        }

        public void SellAll()
        {
            if (State.bag.Count == 0)
            {
                UI.Toast("No fish to sell yet.");
                return;
            }
            int total = 0;
            for (int i = 0; i < State.bag.Count; i++) total += State.bag[i].value;
            int count = State.bag.Count;
            State.bag.Clear();
            State.money += total;
            State.earnedTotal += total;
            Audio.Play(Audio.Buy, 0.9f, 1f);
            UI.Toast("Sold " + count + " fish for " + GameData.Money(total) + ".");
            UI.RefreshHud();
            EvaluateQuests();
            AutoSave();
        }

        public void EvaluateQuests()
        {
            if (State.kills >= 3 && !State.catchQuestDone)
            {
                State.catchQuestDone = true;
                UI.Toast("Quest complete: three creatures killed! The cove is proud of you.");
            }
            if (!State.island2 && State.bossKilled)
            {
                State.island2 = true;
                quest2Announced = true;
                UI.Toast("Isle Two unlocked! Follow the current past the first marker.");
                UI.RefreshQuests();
                AutoSave();
            }
            if (!State.island3 && State.earnedTotal >= 500)
            {
                State.island3 = true;
                quest3Announced = true;
                UI.Toast("Isle Three unlocked! The far horizon is open.");
                UI.RefreshQuests();
                AutoSave();
            }
            UI.RefreshQuests();
        }

        public void RegisterKill(FishEnemy fish)
        {
            State.kills++;
            if (State.kills == 1) UnlockAchievement("First Kill");
            if (fish != null && !string.IsNullOrEmpty(fish.Fish.killBonus))
                UI.Toast("Kill bonus: " + fish.Fish.killBonus + "!");
            UI.RefreshHud();
            EvaluateQuests();
            AutoSave();
        }

        public void DiscoverFish(CaughtFish fish)
        {
            if (fish == null) return;
            if (State.discovered == null) State.discovered = new System.Collections.Generic.List<string>();
            if (!State.discovered.Contains(fish.species))
            {
                State.discovered.Add(fish.species);
                UI.Toast("Fishipedia: " + fish.species + " discovered.");
            }
            if (fish.drip) UnlockAchievement("Drip Collector");
        }

        public void UnlockAchievement(string name)
        {
            if (State.achievements == null) State.achievements = new System.Collections.Generic.List<string>();
            if (State.achievements.Contains(name)) return;
            State.achievements.Add(name);
            UI.Toast("Achievement: " + name);
        }

        public void CheckMoneyAchievement()
        {
            if (State.money >= 10000) UnlockAchievement("Rich! $10,000 on hand");
        }

        public void TrySpawnBoss()
        {
            if (bossSpawned || State.bossKilled)
            {
                UI.Toast(State.bossKilled ? "The Spider Crab is already beaten." : "The Spider Crab is already here.");
                return;
            }
            float h = MeshGen.IslandHeight(20f, 4.2f, 11f, 5f, 11);
            var go = new GameObject("Spider Crab");
            go.transform.SetParent(World.transform, false);
            Boss = go.AddComponent<CrabBoss>();
            Boss.Init(this, new Vector3(11f, h + 0.1f, 5f));
            bossSpawned = true;
            UI.Toast("Bait cast! The Spider Crab is coming. Dodge its charge, then punish the stun.");
            if (Audio != null) Audio.Play(Audio.BossRoar, 0.8f, 0.9f);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void SpawnBossShell(Vector3 pos)
        {
            var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.name = "Spider Crab Shell";
            shell.transform.SetParent(World.transform, false);
            shell.transform.position = new Vector3(pos.x, pos.y + 0.25f, pos.z);
            shell.transform.localScale = new Vector3(0.9f, 0.35f, 0.75f);
            shell.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("bossShellPickup", new Color32(0xE8, 0xC9, 0x9A, 0xFF), 0.35f);
            var col = shell.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            var interact = shell.AddComponent<Interactable>();
            interact.prompt = "E   Collect the Spider Crab Shell";
            interact.onInteract = () =>
            {
                CompleteBossQuest();
                Destroy(shell);
            };
            BossShell = shell;
        }

        public void CompleteBossQuest()
        {
            if (State.bossKilled) return;
            State.bossKilled = true;
            State.island2 = true;
            State.boatKeys = true;
            State.radarOwned = true;
            State.money += 300;
            State.earnedTotal += 300;
            quest2Announced = true;
            UnlockAchievement("Crab Hunter");
            UI.Toast("Spider Crab Shell handed in! $300, boat keys and a Radar. Isle Two is open.");
            if (Audio != null) Audio.Play(Audio.Fanfare, 0.9f, 1f);
            UI.RefreshHud();
            UI.RefreshQuests();
            AutoSave();
        }

        public static string IslandBossName(int type)
        {
            if (type == 1) return "Giant Piranha";
            if (type == 2) return "Pufferfish";
            if (type == 3) return "Albatross";
            return "Bowhead Whale";
        }

        public bool IslandBossDefeated(int type)
        {
            if (type == 1) return State.piranhaKilled;
            if (type == 2) return State.pufferKilled;
            if (type == 3) return State.albatrossKilled;
            return State.whaleKilled;
        }

        public bool IslandBossUnlocked(int type)
        {
            if (type == 1) return State.island2;
            if (type == 2) return State.island3;
            if (type == 3) return State.island4;
            return State.island5;
        }

        public void TrySpawnIslandBoss(int type)
        {
            if (type < 1 || type > 4) return;
            if (IslandBossDefeated(type))
            {
                UI.Toast(IslandBossName(type) + " is already beaten.");
                return;
            }
            if (!IslandBossUnlocked(type))
            {
                UI.Toast("Clear the previous island first.");
                return;
            }
            if (ActiveIslandBoss != null && !ActiveIslandBoss.IsDead)
            {
                UI.Toast(IslandBossName(type) + " is already here.");
                return;
            }
            var info = World.Islands[type];
            float lx = 6f;
            float lz = 5f;
            float h = MeshGen.IslandHeight(info.radius, info.height, lx, lz, info.seed);
            var go = new GameObject(IslandBossName(type));
            go.transform.SetParent(World.transform, false);
            go.transform.position = new Vector3(info.center.x + lx, h + 0.2f, info.center.z + lz);
            ActiveIslandBoss = go.AddComponent<IslandBoss>();
            ActiveIslandBoss.Init(this, go.transform.position, type);
            ActiveIslandBossType = type;
            UI.Toast("Bait cast! The " + IslandBossName(type) + " is coming. Dodge its charge and punish the stun.");
            if (Audio != null) Audio.Play(Audio.BossRoar, 0.8f, type == 4 ? 0.7f : 0.95f);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void SpawnBossTrophy(int type, Vector3 pos)
        {
            var trophy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            trophy.name = IslandBossName(type) + " Trophy";
            trophy.transform.SetParent(World.transform, false);
            trophy.transform.position = new Vector3(pos.x, pos.y + 0.45f, pos.z);
            trophy.transform.localScale = new Vector3(0.8f, 0.55f, 0.8f);
            Color c = type == 1 ? new Color32(0xB8, 0x3A, 0x32, 0xFF) : type == 2 ? new Color32(0xD9, 0x9A, 0x3A, 0xFF) : type == 3 ? new Color32(0xE8, 0xE5, 0xD8, 0xFF) : new Color32(0x5B, 0x7A, 0x8C, 0xFF);
            trophy.GetComponent<MeshRenderer>().sharedMaterial = WorldBuilder.MakeMat("bossTrophy", c, 0.35f);
            var col = trophy.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            var interact = trophy.AddComponent<Interactable>();
            interact.prompt = "E   Collect the " + IslandBossName(type) + " trophy";
            interact.onInteract = () =>
            {
                CompleteIslandBoss(type);
                Destroy(trophy);
            };
            BossTrophy = trophy;
        }

        public void CompleteIslandBoss(int type)
        {
            if (IslandBossDefeated(type)) return;
            if (type == 1) { State.piranhaKilled = true; State.island3 = true; State.money += 200; State.earnedTotal += 200; UI.Toast("Giant Piranha defeated! $200 and Isle Three are yours."); UnlockAchievement("Piranha Hunter"); }
            else if (type == 2) { State.pufferKilled = true; State.island4 = true; State.money += 300; State.earnedTotal += 300; UI.Toast("Pufferfish defeated! $300 and Isle Four are yours."); UnlockAchievement("Pufferfish Hunter"); }
            else if (type == 3) { State.albatrossKilled = true; State.island5 = true; State.money += 400; State.earnedTotal += 400; UI.Toast("Albatross defeated! $400 and Isle Five are yours."); UnlockAchievement("Albatross Hunter"); }
            else { State.whaleKilled = true; State.money += 1000; State.earnedTotal += 1000; UI.Toast("The Bowhead Whale is down. You are home — the cove is safe!"); UnlockAchievement("Homecoming"); }
            ActiveIslandBossType = 0;
            CheckMoneyAchievement();
            if (Audio != null) Audio.Play(Audio.Fanfare, 0.9f, 1f);
            UI.RefreshHud();
            UI.RefreshQuests();
            AutoSave();
        }

        public void OpenWeapons()
        {
            if (!Playing) return;
            if (Fishing != null) Fishing.CancelFishing(false);
            UI.ShowShop(false);
            UI.ShowQuests(false);
            UI.ShowWeapons(true);
            UI.RefreshHud();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseWeapons()
        {
            UI.ShowWeapons(false);
            if (Playing && !UI.AnyPanelOpen) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }

        public void BuyWeapon(int index)
        {
            if (index < 1 || index >= GameData.Weapons.Length) return;
            var def = GameData.Weapons[index];
            if ((index == 1 && State.knifeOwned) || (index == 2 && State.pistolOwned) || (index == 3 && State.shotgunOwned))
            {
                UI.Toast("You already own the " + def.name + ".");
                return;
            }
            if (State.money < def.price)
            {
                UI.Toast("Not enough money for the " + def.name + ".");
                Audio.Play(Audio.Error, 0.6f, 1f);
                return;
            }
            State.money -= def.price;
            if (index == 1) State.knifeOwned = true;
            if (index == 2) { State.pistolOwned = true; State.pistolMag = def.magSize; State.pistolAmmo = 16; }
            if (index == 3) { State.shotgunOwned = true; State.shotgunMag = def.magSize; State.shotgunAmmo = 12; }
            Audio.Play(Audio.Buy, 0.8f, 1f);
            UI.Toast("Bought the " + def.name + "! Press " + (index + 1) + " to equip it.");
            if (Combat != null) Combat.EquipWeapon(index);
            UI.RefreshHud();
            AutoSave();
        }

        public void BuyAmmo(int index)
        {
            int price = index == 2 ? GameData.PistolAmmoPrice : GameData.ShotgunAmmoPrice;
            int amount = index == 2 ? GameData.PistolAmmoAmount : GameData.ShotgunAmmoAmount;
            if (State.money < price)
            {
                UI.Toast("Not enough money for ammo.");
                Audio.Play(Audio.Error, 0.6f, 1f);
                return;
            }
            State.money -= price;
            if (index == 2) State.pistolAmmo += amount;
            else State.shotgunAmmo += amount;
            Audio.Play(Audio.Buy, 0.7f, 1f);
            UI.Toast("Bought " + amount + " rounds.");
            UI.RefreshHud();
            AutoSave();
        }

        public void BuyDynamite()
        {
            if (State.money < GameData.DynamitePrice)
            {
                UI.Toast("Not enough money for dynamite.");
                Audio.Play(Audio.Error, 0.6f, 1f);
                return;
            }
            State.money -= GameData.DynamitePrice;
            State.dynamite++;
            Audio.Play(Audio.Buy, 0.7f, 1f);
            UI.Toast("Bought a dynamite stick. Press 5 to throw it.");
            UI.RefreshHud();
            AutoSave();
        }

        public void OpenGrill()
        {            if (!Playing) return;
            if (Fishing != null) Fishing.CancelFishing(false);
            UI.ShowShop(false);
            UI.ShowQuests(false);
            UI.ShowWeapons(false);
            UI.ShowGrill(true);
            UI.RefreshHud();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseGrill()
        {
            UI.ShowGrill(false);
            if (Playing && !UI.AnyPanelOpen) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }

        public void CookAll(bool safe)
        {
            if (State.bag.Count == 0)
            {
                UI.Toast("No fish to cook. Catch some first.");
                return;
            }
            int cooked = 0;
            int burned = 0;
            for (int i = 0; i < State.bag.Count; i++)
            {
                var fish = State.bag[i];
                if (fish.cooked) continue;
                bool success = safe || Rng.NextDouble() < 0.85;
                if (success)
                {
                    fish.value = Mathf.Max(1, Mathf.RoundToInt(fish.value * 1.5f));
                    fish.cooked = true;
                    cooked++;
                }
                else
                {
                    fish.value = Mathf.Max(1, Mathf.RoundToInt(fish.value * 0.1f));
                    fish.cooked = true;
                    fish.burned = true;
                    burned++;
                }
            }
            if (cooked == 0 && burned == 0)
            {
                UI.Toast("Everything in your bag is already cooked.");
                return;
            }
            UI.Toast(safe ? "Lava-cooked " + cooked + " fish for 1.5x value." : "Cooked " + cooked + " fish" + (burned > 0 ? ", burned " + burned : "") + ".");
            if (Audio != null) Audio.Play(Audio.Buy, 0.7f, 1.2f);
            CheckMoneyAchievement();
            UI.RefreshHud();
            AutoSave();
        }

        public void OpenFishipedia()
        {
            if (!Playing) return;
            if (Fishing != null) Fishing.CancelFishing(false);
            UI.ShowShop(false);
            UI.ShowQuests(false);
            UI.ShowWeapons(false);
            UI.ShowGrill(false);
            UI.ShowFortune(false);
            UI.ShowFishipedia(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseFishipedia()
        {
            UI.ShowFishipedia(false);
            if (Playing && !UI.AnyPanelOpen) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }

        public void OpenFortune()
        {
            if (!Playing) return;
            if (Fishing != null) Fishing.CancelFishing(false);
            UI.ShowShop(false);
            UI.ShowQuests(false);
            UI.ShowWeapons(false);
            UI.ShowGrill(false);
            UI.ShowFishipedia(false);
            UI.ShowFortune(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseFortune()
        {
            UI.ShowFortune(false);
            if (Playing && !UI.AnyPanelOpen) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }

        public static string SkinName(int index)
        {
            if (index == 1) return "Coral";
            if (index == 2) return "Abyss";
            if (index == 3) return "Gold";
            if (index == 4) return "Mint";
            return "Default";
        }

        public void SpinGacha()
        {
            int dripIndex = -1;
            for (int i = 0; i < State.bag.Count; i++) if (State.bag[i].drip) { dripIndex = i; break; }
            if (dripIndex >= 0) State.bag.RemoveAt(dripIndex);
            else if (State.money >= 100) State.money -= 100;
            else
            {
                UI.Toast("The Reel needs a Drip fish or $100.");
                Audio.Play(Audio.Error, 0.6f, 1f);
                return;
            }
            int skin = Rng.Next(1, 5);
            State.skinIndex = skin;
            if (Combat != null) Combat.ApplySkin(skin);
            UnlockAchievement("Reel of Fortune");
            UI.Toast("Skin unlocked: " + SkinName(skin) + "!");
            if (Audio != null) Audio.Play(Audio.Fanfare, 0.8f, 1.1f);
            CheckMoneyAchievement();
            UI.RefreshHud();
            AutoSave();
        }

        public void PlayRoulette()
        {
            if (State.money < 50)
            {
                UI.Toast("Roulette costs $50.");
                Audio.Play(Audio.Error, 0.6f, 1f);
                return;
            }
            State.money -= 50;
            int roll = Rng.Next(0, 37);
            int win = roll == 0 ? 1750 : roll % 2 == 0 ? 100 : 0;
            State.money += win;
            if (win > 0) UnlockAchievement("High Roller");
            UI.Toast("Roulette " + roll + ": " + (win > 0 ? "won " + GameData.Money(win) : "lost the bet") + ".");
            CheckMoneyAchievement();
            UI.RefreshHud();
            AutoSave();
        }

        public void PlaySlots()
        {
            if (State.money < 25)
            {
                UI.Toast("Slots cost $25.");
                Audio.Play(Audio.Error, 0.6f, 1f);
                return;
            }
            State.money -= 25;
            double r = Rng.NextDouble();
            int win = r < 0.55 ? 0 : r < 0.90 ? 50 : r < 0.98 ? 75 : 250;
            State.money += win;
            if (win > 0) UnlockAchievement("High Roller");
            UI.Toast("Slots: " + (win > 0 ? "won " + GameData.Money(win) : "no match") + ".");
            CheckMoneyAchievement();
            UI.RefreshHud();
            AutoSave();
        }

        private void AutoSave()
        {
            if (selfTestMode) return;
            string err;
            SaveSystem.Save(this, out err);
        }

        public void ManualSave()
        {
            if (!Playing) return;
            string err;
            if (SaveSystem.Save(this, out err)) UI.Toast("Game saved.");
            else
            {
                UI.Toast("Could not save: " + err);
                Audio.Play(Audio.Error, 0.6f, 1f);
            }
        }

        public void ManualLoad()
        {
            if (Fishing != null) Fishing.CancelFishing(false);
            var data = SaveSystem.Load(out string err);
            if (data == null)
            {
                UI.Toast("No save to load yet.");
                Audio.Play(Audio.Error, 0.6f, 1f);
                return;
            }
            ApplySave(data);
            if (Playing) SetPaused(false);
            UI.ShowPause(false);
            UI.ShowShop(false);
            UI.ShowQuests(false);
            UI.RefreshHud();
            UI.Toast("Save loaded.");
            if (Playing) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }

        public void ApplySave(SaveData data)
        {
            State.money = data.money;
            State.rodTier = Mathf.Clamp(data.rodTier, 0, GameData.Rods.Length - 1);
            State.bait = Mathf.Max(0, data.bait);
            State.bagCapacity = Mathf.Max(8, data.bagCapacity);
            State.bag = data.bag != null ? new System.Collections.Generic.List<CaughtFish>(data.bag) : new System.Collections.Generic.List<CaughtFish>();
            State.catchCount = data.catchCount;
            State.earnedTotal = data.earnedTotal;
            State.island2 = data.island2;
            State.island3 = data.island3;
            State.island4 = data.island4;
            State.island5 = data.island5;
            State.catchQuestDone = data.catchQuestDone;
            State.boatKeys = data.boatKeys;
            State.radarOwned = data.radarOwned;
            State.piranhaKilled = data.piranhaKilled;
            State.pufferKilled = data.pufferKilled;
            State.albatrossKilled = data.albatrossKilled;
            State.whaleKilled = data.whaleKilled;
            State.discovered = data.discovered != null ? new System.Collections.Generic.List<string>(data.discovered) : new System.Collections.Generic.List<string>();
            State.achievements = data.achievements != null ? new System.Collections.Generic.List<string>(data.achievements) : new System.Collections.Generic.List<string>();
            State.skinIndex = data.skinIndex;
            State.hardMode = data.hardMode;
            State.dynamite = Mathf.Max(0, data.dynamite);
            State.hp = data.hp > 0f ? Mathf.Clamp(data.hp, 1f, 100f) : 100f;
            State.hunger = Mathf.Clamp(data.hunger, 0f, 100f);
            State.kills = Mathf.Max(0, data.kills);
            State.bossKilled = data.bossKilled;
            State.knifeOwned = data.knifeOwned;
            State.pistolOwned = data.pistolOwned;
            State.shotgunOwned = data.shotgunOwned;
            State.pistolAmmo = Mathf.Max(0, data.pistolAmmo);
            State.shotgunAmmo = Mathf.Max(0, data.shotgunAmmo);
            State.pistolMag = Mathf.Clamp(data.pistolMag, 0, GameData.Weapons[2].magSize);
            State.shotgunMag = Mathf.Clamp(data.shotgunMag, 0, GameData.Weapons[3].magSize);
            State.weaponIndex = Mathf.Clamp(data.weaponIndex, 0, GameData.Weapons.Length - 1);
            if (!Combat.OwnsWeapon(State.weaponIndex)) State.weaponIndex = 0;
            quest2Announced = data.island2;
            quest3Announced = data.island3;
            bossSpawned = false;
            BossShell = null;
            BossTrophy = null;
            ActiveIslandBossType = 0;
            if (Boss != null) { Destroy(Boss.gameObject); Boss = null; }
            if (ActiveIslandBoss != null) { Destroy(ActiveIslandBoss.gameObject); ActiveIslandBoss = null; }

            if (Player != null)
            {
                var pos = new Vector3(data.px, data.py, data.pz);
                if (pos.sqrMagnitude < 0.01f) pos = World.StartSpawn.position;
                Player.InBoat = false;
                Player.Controller.enabled = true;
                Player.Teleport(pos, data.yaw);
                Player.ForceLook(data.yaw, data.pitch);
            }
            if (Boat != null)
            {
                Boat.transform.position = new Vector3(data.boatX == 0f && data.boatZ == 0f ? 4.2f : data.boatX, 0.32f, data.boatZ == 0f ? 25.5f : data.boatZ);
                Boat.Yaw = data.boatYaw;
                Boat.transform.rotation = Quaternion.Euler(0f, data.boatYaw, 0f);
                Boat.Occupied = false;
            }
            if (Player != null && Boat != null && data.inBoat)
            {
                Player.InBoat = true;
                Boat.Occupied = true;
                Player.Controller.enabled = false;
                Player.transform.position = Boat.SeatPosition;
            }
            if (Combat != null)
            {
                Combat.CurrentFish = null;
                Combat.RefreshWeaponVisual();
            }
            if (UI != null)
            {
                UI.SetVitals(State.hp, 100f, State.hunger, 100f);
                UI.RefreshHud();
            }
        }

        public void QuitToMenu()
        {
            if (Playing) AutoSave();
            if (Fishing != null) Fishing.CancelFishing(false);
            Playing = false;
            Time.timeScale = 1f;
            UI.ShowPause(false);
            UI.ShowShop(false);
            UI.ShowQuests(false);
            UI.ShowWeapons(false);
            UI.ShowGrill(false);
            UI.ShowFishipedia(false);
            UI.ShowFortune(false);
            UI.ShowMenu(true, SaveSystem.Exists());
            Player.inputEnabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            UI.SetPrompt("");
        }

        public void QuitGame()
        {
            if (Playing) AutoSave();
            Application.Quit();
        }

        private void Update()
        {
            if (!Playing || UI == null) return;

            bool blocked = UI.AnyPanelOpen;
            if (UI.MenuOpen && !selfTestMode && !screenshotMode)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (UI.FishipediaOpen) CloseFishipedia();
                else if (!UI.AnyPanelOpen) OpenFishipedia();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (UI.ShopOpen) { UI.ShowShop(false); }
                else if (UI.QuestOpen) { UI.ShowQuests(false); }
                else if (UI.WeaponOpen) { UI.ShowWeapons(false); }
                else if (UI.GrillOpen) { UI.ShowGrill(false); }
                else if (UI.FishipediaOpen) { UI.ShowFishipedia(false); }
                else if (UI.FortuneOpen) { UI.ShowFortune(false); }
                else if (UI.Paused) { SetPaused(false); }
                else { SetPaused(true); }
            }

            if (!blocked || UI.Paused == false)
            {
                if (Input.GetKeyDown(KeyCode.F5)) ManualSave();
                if (Input.GetKeyDown(KeyCode.F9)) ManualLoad();
            }

            if (UI.Paused) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            else if (!UI.AnyPanelOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Fishing != null) Fishing.RodHidden = UI.AnyPanelOpen;
            bool under = MainCamera.transform.position.y < World.WaterLevel - 0.05f;
            UI.SetUnderwater(under);
            if (UI.WorldStatus != null && Time.frameCount % 30 == 0)
            {
                if (Time.frameCount % 120 == 0) CheckMoneyAchievement();
                if (State.radarOwned && UI.RadarPanel != null && Player != null)
                {
                    var sb = new System.Text.StringBuilder();
                    for (int i = 0; i < World.Islands.Count; i++)
                    {
                        var isl = World.Islands[i];
                        float dist = Vector3.Distance(Player.transform.position, isl.center);
                        bool open = i == 0 || (i == 1 && State.island2) || (i == 2 && State.island3) || (i == 3 && State.island4) || (i == 4 && State.island5);
                        sb.AppendLine(isl.name + "   " + Mathf.RoundToInt(dist) + " m   " + (open ? "OPEN" : "LOCKED"));
                    }
                    UI.SetRadar(true, sb.ToString());
                    UI.WorldStatus.text = "";
                }
                else
                {
                    UI.SetRadar(false, "");
                    string status = State.island5 ? "All islands open" : State.island4 ? "Isle Five needs the Albatross" : State.island3 ? "Isle Four needs the Pufferfish" : State.island2 ? "Isle Three needs the Giant Piranha" : "Isle Two needs the Spider Crab";
                    UI.WorldStatus.text = status;
                }
            }
        }

        private void OnApplicationQuit()
        {
            if (Playing && !selfTestMode && !screenshotMode) AutoSave();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && Playing && !selfTestMode) AutoSave();
        }

        private IEnumerator ScreenshotRoutine(string path)
        {
            yield return null;
            for (int i = 0; i < 24; i++) yield return null;

            try
            {
                var hwnd = FindWindow(null, Application.productName);
                if (hwnd != IntPtr.Zero)
                {
                    SetWindowPos(hwnd, new IntPtr(1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);
                }
            }
            catch { }

            if (shotUi == "menu")
            {
                for (int i = 0; i < 96; i++) yield return null;
                CaptureScreenshot(path);
                yield break;
            }

            StartGameInternal(false);
            Player.transform.position = new Vector3(0f, 1.6f, 24f);
            Player.ForceLook(182f, 1f);
            Fishing.EquipRod(true);
            UI.SetPrompt("");
            if (shotUi == "boat")
            {
                Player.Teleport(new Vector3(0.4f, 1.2f, 21.5f), 0f);
                Player.ForceLook(40f, -10f);
            }
            else if (shotUi == "shopworld")
            {
                float sh = MeshGen.IslandHeight(20f, 4.2f, 0f, 2.5f, 11);
                Player.Teleport(new Vector3(0f, sh + 0.4f, 2.5f), 0f);
                Player.ForceLook(180f, 2f);
            }
            else if (shotUi == "aerial")
            {
                Player.Controller.enabled = false;
                Player.transform.position = new Vector3(0f, 42f, 58f);
                Player.ForceLook(180f, 26f);
            }
            else if (shotUi == "shore")
            {
                Player.Teleport(new Vector3(13f, 1.2f, 9f), 0f);
                Player.ForceLook(205f, 4f);
            }
            else if (shotUi == "fish")
            {
                Player.ForceLook(182f, 2f);
                Fishing.EquipRod(false);
                Fishing.DebugPreviewFish("Moonray Bass");
            }
            else if (shotUi == "combat")
            {
                State.pistolOwned = true;
                State.pistolMag = 8;
                State.pistolAmmo = 16;
                Player.Teleport(new Vector3(0f, 1.6f, 18f), 0f);
                Player.ForceLook(0f, 0f);
                var preview = new CaughtFish
                {
                    species = "Moonray Bass",
                    tier = "rare",
                    weight = 3.2f,
                    value = 260,
                    baseValue = 260,
                    rareVariant = false,
                    color = "#C79BFF"
                };
                var enemy = Combat.SpawnCaughtFish(preview);
                if (enemy != null)
                {
                    enemy.TestMode = true;
                    enemy.transform.position = new Vector3(0f, 1.2f, 22.2f);
                }
                Combat.EquipWeapon(2);
                Player.ForceLook(0f, 14f);
            }
            else if (shotUi == "fist")
            {
                Player.Teleport(new Vector3(0f, 1.6f, 18f), 0f);
                var preview = new CaughtFish
                {
                    species = "Sunny Sardine",
                    tier = "common",
                    weight = 1.1f,
                    value = 45,
                    baseValue = 45,
                    rareVariant = false,
                    color = "#9BD4A0"
                };
                var enemy = Combat.SpawnCaughtFish(preview);
                if (enemy != null)
                {
                    enemy.TestMode = true;
                    enemy.transform.position = new Vector3(0f, 1.2f, 22.2f);
                }
                Combat.EquipWeapon(0);
                Player.ForceLook(0f, 14f);
            }
            else if (shotUi == "boss")
            {
                Player.Controller.enabled = false;
                Player.transform.position = new Vector3(0f, 42f, 50f);
                Player.ForceLook(180f, 28f);
                State.pistolOwned = true;
                State.pistolMag = 8;
                State.pistolAmmo = 16;
                TrySpawnBoss();
                Combat.EquipWeapon(2);
            }
            else if (shotUi == "weapons") OpenWeapons();
            else if (shotUi == "fishipedia") OpenFishipedia();
            else if (shotUi == "grill") OpenGrill();
            else if (shotUi == "fortune") OpenFortune();
            else if (shotUi == "piranha")
            {
                State.boatKeys = true;
                State.radarOwned = true;
                State.island2 = true;
                State.pistolOwned = true;
                State.pistolMag = 8;
                State.pistolAmmo = 16;
                var info = World.Islands[1];
                float bossGround = MeshGen.IslandHeight(info.radius, info.height, 6f, 5f, info.seed);
                Player.Controller.enabled = false;
                Player.transform.position = new Vector3(info.center.x + 6f, bossGround + 1.6f, info.center.z + 12f);
                Player.ForceLook(180f, 17f);
                TrySpawnIslandBoss(1);
                if (ActiveIslandBoss != null)
                {
                    ActiveIslandBoss.TestMode = true;
                    ActiveIslandBoss.transform.position = Player.transform.position + Player.transform.forward * 8f;
                }
                Combat.EquipWeapon(2);
            }
            else if (shotUi == "radar")
            {
                State.radarOwned = true;
                State.boatKeys = true;
                State.island2 = true;
                Player.Teleport(new Vector3(0f, 3.2f, 8f), 0f);
                Player.ForceLook(180f, 2f);
            }
            else if (shotUi == "shop") OpenShop();
            else if (shotUi == "quests") OpenQuests();
            else if (shotUi == "pause") SetPaused(true);
            UI.Toast("A calm morning at Castaway Cove.");
            for (int i = 0; i < 96; i++) yield return null;
            CaptureScreenshot(path);
        }

        private void CaptureScreenshot(string path)
        {
            try
            {
                var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
                rt.antiAliasing = 1;
                RenderTexture previous = MainCamera.targetTexture;
                MainCamera.targetTexture = rt;
                MainCamera.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                MainCamera.targetTexture = previous;
                RenderTexture.active = null;

                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Destroy(tex);
                rt.Release();
                Debug.Log("SCREENSHOT_SAVED " + path);
            }
            catch (Exception e)
            {
                Debug.LogError("Screenshot failed: " + e);
            }

            Application.Quit(0);
        }
    }
}
