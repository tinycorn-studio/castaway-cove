using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cove
{
    [Serializable]
    public class FishSpecies
    {
        public string name;
        public string tier;
        public int baseValue;
        public float minKg;
        public float maxKg;
        public float weight;
        public string color;
    }

    [Serializable]
    public class FishTableJson
    {
        public FishSpecies[] species;
    }

    [Serializable]
    public class CaughtFish
    {
        public string species;
        public string tier;
        public float weight;
        public int value;
        public int baseValue;
        public bool rareVariant;
        public string color;
        public string killBonus;
    }

    [Serializable]
    public class RodDef
    {
        public string name;
        public int price;
        public float castDistance;
        public float reelSpeed;
        public float tensionTolerance;
        public string blurb;
    }

    [Serializable]
    public class GameState
    {
        public int money;
        public int rodTier;
        public int bait = 10;
        public int bagCapacity = 8;
        public List<CaughtFish> bag = new List<CaughtFish>();
        public int catchCount;
        public int earnedTotal;
        public bool island2;
        public bool island3;
        public bool catchQuestDone;

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

        public void ResetToDefaults()
        {
            money = 0;
            rodTier = 0;
            bait = 10;
            bagCapacity = 8;
            bag = new List<CaughtFish>();
            catchCount = 0;
            earnedTotal = 0;
            island2 = false;
            island3 = false;
            catchQuestDone = false;
            hp = 100f;
            hunger = 100f;
            kills = 0;
            bossKilled = false;
            knifeOwned = false;
            pistolOwned = false;
            shotgunOwned = false;
            pistolAmmo = 16;
            shotgunAmmo = 0;
            pistolMag = 8;
            shotgunMag = 5;
            weaponIndex = 0;
        }
    }

    [Serializable]
    public class WeaponDef
    {
        public string name;
        public int price;
        public int damage;
        public float range;
        public float cooldown;
        public int pellets;
        public int magSize;
        public float reload;
        public bool melee;
        public string blurb;
    }

    public static class GameData
    {
        public static FishSpecies[] Species = new FishSpecies[0];

        public static readonly RodDef[] Rods =
        {
            new RodDef { name = "Bamboo Rod", price = 0, castDistance = 12f, reelSpeed = 1f, tensionTolerance = 0.55f, blurb = "Simple and sturdy. A good start." },
            new RodDef { name = "Sturdy Rod", price = 150, castDistance = 16f, reelSpeed = 1.25f, tensionTolerance = 0.65f, blurb = "Casts farther, reels faster, forgives more." },
            new RodDef { name = "Master Rod", price = 500, castDistance = 20f, reelSpeed = 1.5f, tensionTolerance = 0.75f, blurb = "For island legends. Long casts, calm hands." },
        };

        public const int BaitPackPrice = 25;
        public const int BaitPackAmount = 10;
        public const int BagUpgradePrice = 120;
        public const int BagUpgradeCapacity = 14;
        public const float RareVariantChance = 0.03f;
        public const int RareVariantMultiplier = 5;
        public const int PistolAmmoPrice = 12;
        public const int PistolAmmoAmount = 16;
        public const int ShotgunAmmoPrice = 18;
        public const int ShotgunAmmoAmount = 12;

        public static readonly WeaponDef[] Weapons =
        {
            new WeaponDef { name = "Fists", price = 0, damage = 10, range = 2.4f, cooldown = 0.45f, pellets = 1, magSize = 0, reload = 0f, melee = true, blurb = "Free and reliable. Get close and swing." },
            new WeaponDef { name = "Knife", price = 45, damage = 22, range = 2.7f, cooldown = 0.32f, pellets = 1, magSize = 0, reload = 0f, melee = true, blurb = "Fast melee finisher. No ammo needed." },
            new WeaponDef { name = "Pistol", price = 140, damage = 18, range = 45f, cooldown = 0.3f, pellets = 1, magSize = 8, reload = 1.1f, melee = false, blurb = "Accurate sidearm. Headshots and airborne kills pay more." },
            new WeaponDef { name = "Shotgun", price = 320, damage = 13, range = 16f, cooldown = 0.8f, pellets = 6, magSize = 5, reload = 1.6f, melee = false, blurb = "Close-range burst. Six pellets per shell." },
        };

        private const string FishJson = @"
{
  ""species"": [
    { ""name"": ""Sunny Sardine"",  ""tier"": ""common"",   ""baseValue"": 45,  ""minKg"": 0.2, ""maxKg"": 1.1, ""weight"": 26, ""color"": ""#9BD4A0"" },
    { ""name"": ""Dune Minnow"",    ""tier"": ""common"",   ""baseValue"": 55,  ""minKg"": 0.3, ""maxKg"": 1.4, ""weight"": 22, ""color"": ""#A8D8A0"" },
    { ""name"": ""Coral Perch"",    ""tier"": ""common"",   ""baseValue"": 70,  ""minKg"": 0.6, ""maxKg"": 2.6, ""weight"": 18, ""color"": ""#8FCFB0"" },
    { ""name"": ""Lagoon Snapper"", ""tier"": ""uncommon"", ""baseValue"": 110, ""minKg"": 1.0, ""maxKg"": 4.0, ""weight"": 13, ""color"": ""#6FC3FF"" },
    { ""name"": ""Amber Tang"",     ""tier"": ""uncommon"", ""baseValue"": 130, ""minKg"": 1.4, ""maxKg"": 5.2, ""weight"": 9,  ""color"": ""#7FB8E8"" },
    { ""name"": ""Moonray Bass"",   ""tier"": ""rare"",     ""baseValue"": 200, ""minKg"": 2.0, ""maxKg"": 7.5, ""weight"": 5,  ""color"": ""#C79BFF"" },
    { ""name"": ""Stormback Eel"",  ""tier"": ""rare"",     ""baseValue"": 240, ""minKg"": 2.4, ""maxKg"": 9.0, ""weight"": 3,  ""color"": ""#B08BFF"" },
    { ""name"": ""Golden Leviathan"",""tier"": ""epic"",     ""baseValue"": 480, ""minKg"": 5.0, ""maxKg"": 18.0,""weight"": 2,  ""color"": ""#FFC24B"" }
  ]
}";

        private static readonly Dictionary<string, Color> tierColors = new Dictionary<string, Color>
        {
            { "common", new Color32(0x7B, 0xC7, 0x8C, 0xFF) },
            { "uncommon", new Color32(0x63, 0xB4, 0xF2, 0xFF) },
            { "rare", new Color32(0xB9, 0x8C, 0xEF, 0xFF) },
            { "epic", new Color32(0xF2, 0xB2, 0x3E, 0xFF) },
        };

        public static void Init()
        {
            if (Species != null && Species.Length > 0) return;
            var table = JsonUtility.FromJson<FishTableJson>(FishJson);
            Species = table.species;
        }

        public static Color TierColor(string tier)
        {
            Color c;
            if (tier != null && tierColors.TryGetValue(tier, out c)) return c;
            return new Color32(0x7B, 0xC7, 0x8C, 0xFF);
        }

        public static string TierLabel(string tier)
        {
            if (string.IsNullOrEmpty(tier)) return "Common";
            return char.ToUpperInvariant(tier[0]) + tier.Substring(1);
        }

        public static string TierLabelUpper(string tier)
        {
            return TierLabel(tier).ToUpperInvariant();
        }

        public static CaughtFish RollFish(System.Random rng, bool baitUsed)
        {
            float[] weights = new float[Species.Length];
            float total = 0f;
            for (int i = 0; i < Species.Length; i++)
            {
                float w = Species[i].weight;
                if (baitUsed)
                {
                    if (Species[i].tier == "common") w *= 0.55f;
                    else if (Species[i].tier == "uncommon") w *= 1.35f;
                    else if (Species[i].tier == "rare") w *= 2.1f;
                    else w *= 2.6f;
                }
                weights[i] = w;
                total += w;
            }

            float pick = (float)rng.NextDouble() * total;
            FishSpecies sp = Species[0];
            for (int i = 0; i < Species.Length; i++)
            {
                pick -= weights[i];
                if (pick <= 0f) { sp = Species[i]; break; }
            }

            float t = (float)rng.NextDouble();
            float weightKg = Mathf.Lerp(sp.minKg, sp.maxKg, t);
            float sizeFactor = Mathf.Lerp(0.8f, 1.5f, t);
            bool rare = rng.NextDouble() < RareVariantChance;
            int value = Mathf.Max(5, Mathf.RoundToInt(sp.baseValue * sizeFactor));
            if (rare) value *= RareVariantMultiplier;

            return new CaughtFish
            {
                species = sp.name,
                tier = sp.tier,
                weight = Mathf.Round(weightKg * 10f) / 10f,
                value = value,
                baseValue = value,
                rareVariant = rare,
                color = sp.color,
            };
        }

        public static string Money(int v)
        {
            return "$" + v.ToString("N0");
        }
    }
}
