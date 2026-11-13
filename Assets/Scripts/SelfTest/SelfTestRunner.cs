using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Cove
{
    [Serializable]
    public class SelfTestStep
    {
        public string step;
        public bool passed;
        public string detail;
        public float ms;
    }

    [Serializable]
    public class SelfTestResult
    {
        public int version = 1;
        public string game = "Castaway Cove";
        public bool passed;
        public List<SelfTestStep> steps = new List<SelfTestStep>();
        public int finalMoney;
        public int bagCount;
        public float totalMs;
        public List<string> errors = new List<string>();
    }

    public class SelfTestRunner : MonoBehaviour
    {
        private GameBoot game;
        private string path;
        private SelfTestResult result = new SelfTestResult();
        private readonly Stopwatch total = new Stopwatch();
        private bool finished;
        private int startFrame;
        private string saveBackup;
        private bool hadSaveBackup;

        public void Init(GameBoot g, string p)
        {
            game = g;
            path = p;
            try
            {
                saveBackup = SaveSystem.SavePath + ".selftest-backup";
                if (File.Exists(SaveSystem.SavePath))
                {
                    File.Copy(SaveSystem.SavePath, saveBackup, true);
                    hadSaveBackup = true;
                }
            }
            catch { }
            total.Start();
            startFrame = Time.frameCount;
            StartCoroutine(Watchdog());
            StartCoroutine(Run());
        }

        private IEnumerator Watchdog()
        {
            float t = 0f;
            while (!finished)
            {
                t += Time.unscaledDeltaTime;
                if (t > 90f) break;
                yield return null;
            }
            if (!finished)
            {
                result.errors.Add("Self-test watchdog timed out while state was " + StateName());
                Add("timeout", false, "Timed out before finishing; fishing state " + StateName(), total.ElapsedMilliseconds);
                Finish(false);
            }
        }

        private string StateName()
        {
            return game != null && game.Fishing != null ? game.Fishing.State.ToString() : "unknown";
        }

        private IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void Add(string step, bool passed, string detail, long ms)
        {
            result.steps.Add(new SelfTestStep { step = step, passed = passed, detail = detail, ms = ms });
            if (!passed) result.errors.Add(step + ": " + detail);
        }

        private IEnumerator Run()
        {
            yield return null;
            yield return null;

            float ms;

            var sw = Stopwatch.StartNew();
            bool spawnOk = game != null && game.Playing && game.Player != null && game.Player.Controller != null && game.MainCamera != null;
            Vector3 spawnPos = spawnOk ? game.Player.transform.position : Vector3.zero;
            yield return Wait(0.7f);
            if (spawnOk)
            {
                spawnOk = game.Player.transform.position.y > 0.2f;
                spawnPos = game.Player.transform.position;
            }
            ms = sw.ElapsedMilliseconds;
            Add("spawn", spawnOk, spawnOk ? "player spawned at " + Fmt(spawnPos) + " on the start island" : "player or world missing", (long)ms);

            sw = Stopwatch.StartNew();
            Vector3 before = game.Player.transform.position;
            game.Player.Teleport(game.World.StartSpawn.position, 0f);
            yield return Wait(0.5f);
            before = game.Player.transform.position;
            game.Player.ExternalInput = true;
            game.Player.ExternalMove = new Vector2(0f, 1f);
            game.Player.ExternalSprint = false;
            yield return Wait(1.1f);
            game.Player.ExternalInput = false;
            game.Player.ExternalMove = Vector2.zero;
            Vector3 after = game.Player.transform.position;
            float dist = Vector3.Distance(new Vector3(before.x, 0f, before.z), new Vector3(after.x, 0f, after.z));
            bool moveOk = dist >= 1.5f;
            ms = sw.ElapsedMilliseconds;
            Add("move", moveOk, "walked " + dist.ToString("0.00") + " m forward (need 1.5 m)", (long)ms);
            if (!moveOk) result.errors.Add("player did not move far enough");

            game.Player.Teleport(new Vector3(0f, 1.6f, 17f), 0f);
            game.Player.ForceLook(0f, 10f);
            yield return Wait(0.7f);
            game.Fishing.EquipRod(true);
            game.Fishing.UseReelOverride = true;
            game.Fishing.ReelHoldOverride = false;
            yield return Wait(0.25f);

            sw = Stopwatch.StartNew();
            int baitBefore = game.State.bait;
            game.Fishing.TestSetCharge(0f);
            game.Fishing.TestStartCharge();
            if (game.Fishing.State != FishingState.Charging)
            {
                game.Fishing.EquipRod(true);
                yield return null;
                game.Fishing.TestStartCharge();
            }
            yield return Wait(0.45f);
            game.Fishing.TestSetCharge(0.7f);
            game.Fishing.TestRelease();
            float waited = 0f;
            while (game.Fishing.State == FishingState.Flying && waited < 3f) { waited += Time.unscaledDeltaTime; yield return null; }
            while (game.Fishing.State == FishingState.Charging && waited < 3f) { waited += Time.unscaledDeltaTime; yield return null; }
            bool castOk = game.Fishing.State == FishingState.Waiting || game.Fishing.State == FishingState.Nibble || game.Fishing.State == FishingState.Bite;
            bool baitUsed = game.State.bait == baitBefore - 1;
            ms = sw.ElapsedMilliseconds;
            Add("cast", castOk, castOk ? "bobber landed on the water, " + game.State.bait + " bait left" : "cast did not land (state " + game.Fishing.State + ", " + game.Fishing.LastCastError + ", at " + game.Player.transform.position + ")", (long)ms);

            sw = Stopwatch.StartNew();
            game.Fishing.ForceBiteNow();
            yield return Wait(0.25f);
            bool biteShown = game.Fishing.State == FishingState.Bite && game.UI.BiteText != null && game.UI.BiteText.gameObject.activeSelf;
            bool hooked = game.Fishing.HookNow();
            yield return Wait(0.2f);
            bool hookOk = hooked && game.Fishing.State == FishingState.Reeling;
            ms = sw.ElapsedMilliseconds;
            Add("hook", biteShown && hookOk, biteShown && hookOk ? "bite alert shown, click hooked the fish" : "bite=" + biteShown + " hooked=" + hooked + " state " + game.Fishing.State, (long)ms);

            sw = Stopwatch.StartNew();
            float reelTime = 0f;
            bool snapped = false;
            while (game.Fishing.State == FishingState.Reeling && reelTime < 30f)
            {
                game.Fishing.ReelHoldOverride = game.Fishing.Tension < game.Fishing.SafeTension * 0.82f;
                reelTime += Time.unscaledDeltaTime;
                yield return null;
            }
            game.Fishing.ReelHoldOverride = false;
            snapped = game.Fishing.State == FishingState.Snapped;
            bool reelOk = game.Fishing.State == FishingState.Landed && !snapped;
            ms = sw.ElapsedMilliseconds;
            Add("reel", reelOk, reelOk ? "reel minigame completed in " + reelTime.ToString("0.0") + " s without snapping" : snapped ? "line snapped during the reel" : "reel stalled (state " + game.Fishing.State + ")", (long)ms);

            sw = Stopwatch.StartNew();
            bool caught = game.Fishing.LastCatch != null;
            int bagAfterCatch = game.State.bag.Count;
            var enemy = game.Combat != null ? game.Combat.CurrentFish : null;
            bool enemyAlive = enemy != null && !enemy.IsDead;
            bool catchOk = caught && enemyAlive && bagAfterCatch == 0 && game.UI.CatchCard != null && game.UI.CatchCard.activeSelf;
            ms = sw.ElapsedMilliseconds;
            string fishName = caught ? game.Fishing.LastCatch.species : "nothing";
            Add("catch", catchOk, catchOk ? "landed " + fishName + " (" + game.Fishing.LastCatch.weight.ToString("0.0") + " kg) alive and the catch card is showing" : "LastCatch=" + caught + " liveEnemy=" + enemyAlive + " bag=" + bagAfterCatch, (long)ms);

            sw = Stopwatch.StartNew();
            bool fightOk = false;
            if (enemy != null && !enemy.IsDead)
            {
                enemy.TestMode = true;
                yield return new WaitForFixedUpdate();
                yield return null;
                bool shot = game.Combat.TestFireAt(enemy);
                game.Combat.TestFinishTarget(enemy);
                yield return Wait(0.15f);
                fightOk = shot && enemy.IsDead;
                Add("fight", fightOk, fightOk ? "pistol shot damaged the landed fish and the follow-up killed it" : "shot=" + shot + " dead=" + enemy.IsDead + " hp=" + enemy.Hp, sw.ElapsedMilliseconds);
            }
            else
            {
                Add("fight", false, "no live landed fish to fight", sw.ElapsedMilliseconds);
            }

            sw = Stopwatch.StartNew();
            int bagBeforeCollect = game.State.bag.Count;
            if (enemy != null && enemy.IsDead) enemy.CollectForTest();
            yield return Wait(0.1f);
            bool collectOk = game.State.bag.Count == bagBeforeCollect + 1;
            ms = sw.ElapsedMilliseconds;
            Add("collect", collectOk, collectOk ? "collected the kill into the bag (" + game.State.bag.Count + " / " + game.State.bagCapacity + ")" : "bag did not gain the kill (bag " + game.State.bag.Count + ")", (long)ms);

            sw = Stopwatch.StartNew();
            bool bagOk = game.State.bag.Count <= game.State.bagCapacity && game.State.bag.Count == 1;
            ms = sw.ElapsedMilliseconds;
            Add("bag", bagOk, "bag holds " + game.State.bag.Count + " / " + game.State.bagCapacity + " fish", (long)ms);

            bool castActOk = castOk && hookOk && reelOk && catchOk;
            Add("act", castActOk, castActOk ? "cast, hooked, reeled and caught " + fishName + " using the full fishing loop" : "fishing action did not complete", 0L);

            sw = Stopwatch.StartNew();
            int valueBeforeSell = game.Fishing.LastCatch != null ? game.Fishing.LastCatch.value : 0;
            int moneyBeforeSell = game.State.money;
            game.SellAll();
            bool sellOk = game.State.bag.Count == 0 && game.State.money == moneyBeforeSell + valueBeforeSell && valueBeforeSell > 0;
            ms = sw.ElapsedMilliseconds;
            Add("sell", sellOk, sellOk ? "sold the catch for " + GameData.Money(valueBeforeSell) + ", money is now " + GameData.Money(game.State.money) : "sell failed (money " + GameData.Money(game.State.money) + ", bag " + game.State.bag.Count + ")", (long)ms);

            sw = Stopwatch.StartNew();
            int moneyBeforeBuy = game.State.money;
            int baitBeforeBuy = game.State.bait;
            game.BuyBait();
            bool buyOk = game.State.money == moneyBeforeBuy - GameData.BaitPackPrice && game.State.bait == baitBeforeBuy + GameData.BaitPackAmount;
            ms = sw.ElapsedMilliseconds;
            Add("buy", buyOk, buyOk ? "bought a bait pack for " + GameData.Money(GameData.BaitPackPrice) + "; " + game.State.bait + " bait and " + GameData.Money(game.State.money) + " left" : "buy failed (money " + game.State.money + ", bait " + game.State.bait + ")", (long)ms);

            sw = Stopwatch.StartNew();
            int savedMoney = game.State.money;
            string saveError;
            bool wroteSave = SaveSystem.Save(game, out saveError) && File.Exists(SaveSystem.SavePath);
            var loaded = SaveSystem.Load(out saveError);
            bool saveOk = wroteSave && loaded != null && loaded.money == savedMoney;
            ms = sw.ElapsedMilliseconds;
            Add("save", saveOk, saveOk ? "save.json written with money " + GameData.Money(savedMoney) + " at " + SaveSystem.SavePath : "save failed: " + saveError, (long)ms);

            sw = Stopwatch.StartNew();
            if (loaded != null)
            {
                game.State.money = savedMoney + 777;
                game.State.bag.Add(new CaughtFish { species = "Test Fish", tier = "common", weight = 1f, value = 10, color = "#FFFFFF" });
                game.ApplySave(loaded);
            }
            bool loadOk = loaded != null && game.State.money == savedMoney && game.State.bag.Count == 0;
            ms = sw.ElapsedMilliseconds;
            Add("load", loadOk, loadOk ? "reloaded save: money restored to " + GameData.Money(game.State.money) + ", bag empty" : "load did not restore the saved state", (long)ms);

            sw = Stopwatch.StartNew();
            game.UI.RefreshHud();
            game.UI.SetPrompt("E   Read the quest board");
            bool hudOk = game.UI.HudCanvas != null && game.UI.HudCanvas.enabled
                && game.UI.MoneyText != null && game.UI.MoneyText.gameObject.activeInHierarchy && game.UI.MoneyText.text == GameData.Money(game.State.money)
                && game.UI.BagText != null && game.UI.BagText.gameObject.activeInHierarchy
                && game.UI.WeaponText != null && game.UI.WeaponText.gameObject.activeInHierarchy
                && game.UI.HpFill != null && game.UI.HungerFill != null
                && game.UI.PromptText != null && game.UI.PromptText.gameObject.activeInHierarchy && game.UI.PromptText.text.Contains("quest board");
            ms = sw.ElapsedMilliseconds;
            Add("hud", hudOk, hudOk ? "HUD shows money " + game.UI.MoneyText.text + ", bag " + game.UI.BagText.text + ", weapon " + game.UI.WeaponText.text + ", HP and hunger bars, and interaction text \"" + game.UI.PromptText.text + "\"" : "HUD text, vitals or canvas missing", (long)ms);

            sw = Stopwatch.StartNew();
            int moneyBeforeBoss = game.State.money;
            game.TrySpawnBoss();
            var boss = game.Boss;
            bool bossSpawnedOk = boss != null;
            if (boss != null) boss.TakeDamage(boss.MaxHp, false, false, false, boss.transform.position + Vector3.up);
            yield return Wait(0.2f);
            var shell = game.BossShell;
            if (shell != null)
            {
                var shellInteract = shell.GetComponent<Interactable>();
                if (shellInteract != null) shellInteract.Interact();
            }
            yield return Wait(0.1f);
            bool bossOk = bossSpawnedOk && game.State.bossKilled && game.State.island2 && game.State.money == moneyBeforeBoss + 300;
            ms = sw.ElapsedMilliseconds;
            Add("boss", bossOk, bossOk ? "killed the Spider Crab, collected its shell, earned $300 and opened Isle Two" : "bossSpawned=" + bossSpawnedOk + " killed=" + game.State.bossKilled + " island2=" + game.State.island2 + " money=" + game.State.money, (long)ms);

            sw = Stopwatch.StartNew();
            bool chainOk = true;
            string chainDetail = "";
            for (int type = 1; type <= 4; type++)
            {
                game.TrySpawnIslandBoss(type);
                var islandBoss = game.ActiveIslandBoss;
                if (islandBoss == null)
                {
                    chainOk = false;
                    chainDetail += "boss " + type + " did not spawn; ";
                    continue;
                }
                islandBoss.TakeDamage(islandBoss.MaxHp, false, false, false, islandBoss.transform.position);
                yield return Wait(0.08f);
                var trophy = game.BossTrophy;
                if (trophy != null)
                {
                    var trophyInteract = trophy.GetComponent<Interactable>();
                    if (trophyInteract != null) trophyInteract.Interact();
                }
                yield return Wait(0.08f);
                if (!game.IslandBossDefeated(type))
                {
                    chainOk = false;
                    chainDetail += "boss " + type + " not defeated; ";
                }
            }
            bool chainUnlocks = game.State.island3 && game.State.island4 && game.State.island5 && game.State.whaleKilled;
            chainOk = chainOk && chainUnlocks;
            if (!chainUnlocks) chainDetail += "islands=(3:" + game.State.island3 + " 4:" + game.State.island4 + " 5:" + game.State.island5 + " whale:" + game.State.whaleKilled + ")";
            ms = sw.ElapsedMilliseconds;
            Add("bosses", chainOk, chainOk ? "defeated Piranha, Pufferfish, Albatross and Whale; Isles 3, 4 and 5 opened" : chainDetail, (long)ms);

            sw = Stopwatch.StartNew();
            game.State.bag.Add(new CaughtFish { species = "Test Cook", tier = "common", weight = 1f, value = 100, baseValue = 100 });
            game.CookAll(true);
            bool cookOk = game.State.bag.Count == 1 && game.State.bag[0].value == 150 && game.State.bag[0].cooked;
            game.SellAll();
            int skinBefore = game.State.skinIndex;
            int moneyBeforeServices = game.State.money;
            game.SpinGacha();
            bool gachaOk = game.State.skinIndex >= 0 && game.State.skinIndex != skinBefore && game.State.money == moneyBeforeServices - 100;
            game.BuyDynamite();
            bool dynamiteOk = game.State.dynamite == 1;
            bool difficultyBefore = game.State.hardMode;
            game.ToggleDifficulty();
            bool difficultyOk = game.State.hardMode != difficultyBefore;
            game.ToggleDifficulty();
            bool fishipediaOk = game.State.discovered != null && game.State.discovered.Contains("Sunny Sardine");
            bool servicesOk = cookOk && gachaOk && dynamiteOk && difficultyOk && fishipediaOk;
            ms = sw.ElapsedMilliseconds;
            Add("services", servicesOk, servicesOk ? "cooked a fish for 1.5x, spun a skin, bought dynamite, toggled difficulty and logged the Fishipedia" : "cook=" + cookOk + " gacha=" + gachaOk + " dynamite=" + dynamiteOk + " difficulty=" + difficultyOk + " fishipedia=" + fishipediaOk, (long)ms);

            result.finalMoney = game.State.money;
            result.bagCount = game.State.bag.Count;
            bool allOk = true;
            for (int i = 0; i < result.steps.Count; i++) if (!result.steps[i].passed) allOk = false;
            Finish(allOk);
        }

        private static string Fmt(Vector3 v)
        {
            return "(" + v.x.ToString("0.0") + ", " + v.y.ToString("0.0") + ", " + v.z.ToString("0.0") + ")";
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [System.Runtime.InteropServices.DllImport("msvcrt.dll", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        private static extern int _write(int fd, byte[] buffer, uint count);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nBytes, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);

        private static void WriteStdout(string text)
        {
            try
            {
                IntPtr handle = GetStdHandle(-11);
                if (handle == IntPtr.Zero || handle == new IntPtr(-1)) return;
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text + "\n");
                uint written;
                WriteFile(handle, bytes, (uint)bytes.Length, out written, IntPtr.Zero);
                try { _write(1, bytes, (uint)bytes.Length); } catch { }
            }
            catch { }
        }
        private void Finish(bool passed)
        {
            if (finished) return;
            finished = true;
            total.Stop();
            result.passed = passed;
            result.totalMs = total.ElapsedMilliseconds;
            string json = JsonUtility.ToJson(result, true);
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Self-test could not write result: " + e);
            }
            try
            {
                if (File.Exists(SaveSystem.SavePath)) File.Delete(SaveSystem.SavePath);
                if (hadSaveBackup && File.Exists(saveBackup)) File.Copy(saveBackup, SaveSystem.SavePath, true);
                if (File.Exists(saveBackup)) File.Delete(saveBackup);
            }
            catch { }
            UnityEngine.Debug.Log("SELF_TEST_RESULT: " + (passed ? "PASS" : "FAIL"));
            UnityEngine.Debug.Log(json);
            WriteStdout(json);
            StartCoroutine(QuitSoon(passed ? 0 : 1));
        }

        private IEnumerator QuitSoon(int code)
        {
            yield return new WaitForSecondsRealtime(0.35f);
            Application.Quit(code);
        }
    }
}
