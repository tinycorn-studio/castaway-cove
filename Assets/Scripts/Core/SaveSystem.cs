using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Cove
{
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int money;
        public int rodTier;
        public int bait = 10;
        public int bagCapacity = 8;
        public List<CaughtFish> bag = new List<CaughtFish>();
        public int catchCount;
        public int earnedTotal;
        public bool island2;
        public bool island3;
        public bool island4;
        public bool island5;
        public bool catchQuestDone;
        public bool boatKeys;
        public bool radarOwned;
        public bool piranhaKilled;
        public bool pufferKilled;
        public bool albatrossKilled;
        public bool whaleKilled;
        public List<string> discovered = new List<string>();
        public List<string> achievements = new List<string>();
        public int skinIndex = -1;
        public bool hardMode;
        public int dynamite;
        public float hp = 100f;
        public float hunger = 100f;
        public int kills;
        public bool bossKilled;
        public bool knifeOwned;
        public bool pistolOwned;
        public bool shotgunOwned;
        public int pistolAmmo = 16;
        public int shotgunAmmo;
        public int pistolMag = 8;
        public int shotgunMag = 5;
        public int weaponIndex;
        public float px, py, pz, yaw, pitch;
        public float boatX, boatZ, boatYaw;
        public bool inBoat;
    }

    public static class SaveSystem
    {
        public static string SavePath
        {
            get { return Path.Combine(Application.persistentDataPath, "save.json"); }
        }

        public static bool Exists()
        {
            try { return File.Exists(SavePath); }
            catch { return false; }
        }

        public static bool Save(GameBoot game, out string error)
        {
            error = null;
            try
            {
                var g = game.State;
                var data = new SaveData
                {
                    money = g.money,
                    rodTier = g.rodTier,
                    bait = g.bait,
                    bagCapacity = g.bagCapacity,
                    bag = new List<CaughtFish>(g.bag),
                    catchCount = g.catchCount,
                    earnedTotal = g.earnedTotal,
                    island2 = g.island2,
                    island3 = g.island3,
                    island4 = g.island4,
                    island5 = g.island5,
                    catchQuestDone = g.catchQuestDone,
                    boatKeys = g.boatKeys,
                    radarOwned = g.radarOwned,
                    piranhaKilled = g.piranhaKilled,
                    pufferKilled = g.pufferKilled,
                    albatrossKilled = g.albatrossKilled,
                    whaleKilled = g.whaleKilled,
                    discovered = new List<string>(g.discovered),
                    achievements = new List<string>(g.achievements),
                    skinIndex = g.skinIndex,
                    hardMode = g.hardMode,
                    dynamite = g.dynamite,
                    hp = g.hp,
                    hunger = g.hunger,
                    kills = g.kills,
                    bossKilled = g.bossKilled,
                    knifeOwned = g.knifeOwned,
                    pistolOwned = g.pistolOwned,
                    shotgunOwned = g.shotgunOwned,
                    pistolAmmo = g.pistolAmmo,
                    shotgunAmmo = g.shotgunAmmo,
                    pistolMag = g.pistolMag,
                    shotgunMag = g.shotgunMag,
                    weaponIndex = g.weaponIndex,
                };
                if (game.Player != null)
                {
                    Vector3 p = game.Player.transform.position;
                    data.px = p.x; data.py = p.y; data.pz = p.z;
                    data.yaw = game.Player.Yaw;
                    data.pitch = game.Player.Pitch;
                }
                if (game.Boat != null)
                {
                    Vector3 b = game.Boat.transform.position;
                    data.boatX = b.x; data.boatZ = b.z; data.boatYaw = game.Boat.Yaw;
                    data.inBoat = game.Player != null && game.Player.InBoat;
                }
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        public static SaveData Load(out string error)
        {
            error = null;
            try
            {
                if (!File.Exists(SavePath)) return null;
                string text = File.ReadAllText(SavePath);
                var data = JsonUtility.FromJson<SaveData>(text);
                if (data == null || data.version < 1) return null;
                if (data.bag == null) data.bag = new List<CaughtFish>();
                return data;
            }
            catch (Exception e)
            {
                error = e.Message;
                return null;
            }
        }

        public static void Delete()
        {
            try { if (File.Exists(SavePath)) File.Delete(SavePath); } catch { }
        }
    }
}
