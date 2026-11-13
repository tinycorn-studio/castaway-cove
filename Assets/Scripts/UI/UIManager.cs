using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cove
{
    public class UIManager : MonoBehaviour
    {
        public Camera Cam;

        public Canvas HudCanvas;
        public Canvas MenuCanvas;
        public Canvas ModalCanvas;

        public bool MenuOpen { get; private set; }
        public bool Paused { get; private set; }
        public bool ShopOpen { get; private set; }
        public bool QuestOpen { get; private set; }
        public bool WeaponOpen { get; private set; }
        public bool GrillOpen { get; private set; }
        public bool FishipediaOpen { get; private set; }
        public bool FortuneOpen { get; private set; }

        public bool AnyPanelOpen
        {
            get { return MenuOpen || Paused || ShopOpen || QuestOpen || WeaponOpen || GrillOpen || FishipediaOpen || FortuneOpen; }
        }

        public Text MoneyText;
        public Text GearText;
        public Text BagText;
        public Text PromptText;
        public Text BiteText;
        public Text ToastRootText;
        public GameObject ChargePanel;
        public RectTransform ChargeFill;
        public GameObject FishingPanel;
        public RectTransform TensionFill;
        public Image TensionFillImage;
        public RectTransform TensionMarker;
        public RectTransform ProgressFill;
        public RectTransform StaminaFill;
        public Image UnderwaterTint;
        public GameObject CatchCard;
        public Text CatchName;
        public Text CatchTier;
        public Text CatchWeight;
        public Text CatchValue;
        public Text CatchRare;
        public Text CatchFooter;
        public Image CatchAccent;
        public RectTransform ToastContainer;

        public GameObject MenuPanel;
        public GameObject MenuDim;
        public GameObject ModalDim;
        public Text MenuMoneyHint;
        public Button ContinueButton;
        public Button DifficultyButton;

        public GameObject PausePanel;
        public GameObject ShopPanel;
        public GameObject QuestPanel;
        public GameObject WeaponPanel;
        public GameObject GrillPanel;
        public Text GrillText;
        public GameObject FishipediaPanel;
        public Text FishipediaText;
        public Text AchievementsText;
        public GameObject FortunePanel;
        public Text FortuneText;

        public Text ShopMoneyText;
        public Text BaitItemDesc;
        public Button BaitBuyButton;
        public Text BagItemDesc;
        public Button BagBuyButton;
        public Text SellItemDesc;
        public Button SellButton;
        public Text SellButtonLabel;

        public Text WeaponText;
        public Text WeaponMoneyText;
        public Text HpText;
        public Text HungerText;
        public RectTransform HpFill;
        public RectTransform HungerFill;
        public GameObject Crosshair;
        public GameObject TargetPanel;
        public Text TargetNameText;
        public RectTransform TargetFill;
        public Image TargetFillImage;
        public Image DamageFlash;
        public GameObject DeathPanel;
        public Text PistolAmmoDesc;
        public Button PistolAmmoButton;
        public Text ShotgunAmmoDesc;
        public Button ShotgunAmmoButton;
        public Text DynamiteDesc;
        public Button DynamiteButton;
        public Button LavaCookButton;

        private class ShopRowUI
        {
            public Text title;
            public Text desc;
            public Text price;
            public Button buy;
            public Text buyLabel;
        }

        private readonly ShopRowUI[] rodRows = new ShopRowUI[3];
        private readonly ShopRowUI[] weaponRows = new ShopRowUI[3];

        public Text QuestSummary;
        public Text Quest1Title;
        public Text Quest1Progress;
        public Text Quest2Title;
        public Text Quest2Progress;
        public Text Quest3Title;
        public Text Quest3Progress;
        public Text Quest4Title;
        public Text Quest4Progress;
        public Text Quest5Title;
        public Text Quest5Progress;
        public Image Quest1Chip;
        public Image Quest2Chip;
        public Image Quest3Chip;
        public Image Quest4Chip;
        public Image Quest5Chip;

        public GameObject RadarPanel;
        public Text RadarText;

        public Text WorldStatus;

        private readonly List<GameObject> toasts = new List<GameObject>();
        private readonly List<Image> crosshairParts = new List<Image>();
        private Coroutine catchRoutine;
        private Coroutine damageRoutine;
        private float bitePulse;

        public void BuildUI(Camera cam)
        {
            Cam = cam;
            UITheme.Init();
            EnsureEventSystem();
            BuildHud();
            BuildMenu();
            BuildModal();
            RefreshHud();
            RefreshQuests();
        }

        private void EnsureEventSystem()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null) return;
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            es.transform.SetParent(transform, false);
        }

        private Canvas MakeCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Cam;
            canvas.planeDistance = 0.2f;
            canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static RectTransform MakePanel(Transform parent, string name, Color color, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, bool shadow = false)
        {
            var go = UITheme.Panel(parent, name, color, shadow);
            var rt = go.GetComponent<RectTransform>();
            UITheme.Place(rt, anchor, pivot, pos, size);
            return rt;
        }

        private static Text MakeText(Transform parent, string text, int size, Color color, TextAnchor anchor, bool shadow, Vector2 anchorPos, Vector2 pivot, Vector2 pos, Vector2 dims)
        {
            var t = UITheme.Label(parent, text, size, color, anchor, shadow);
            var rt = t.rectTransform;
            rt.anchorMin = anchorPos;
            rt.anchorMax = anchorPos;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = dims;
            return t;
        }

        private void BuildHud()
        {
            HudCanvas = MakeCanvas("HUD Canvas", 10);
            var root = HudCanvas.transform;

            var moneyChip = MakePanel(root, "Money Chip", Palette.Cream, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -18f), new Vector2(232f, 66f), true);
            MakeText(moneyChip, "MONEY", 12, Palette.Teal, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -10f), new Vector2(180f, 18f));
            MoneyText = MakeText(moneyChip, "$0", 30, Palette.Ink, TextAnchor.LowerLeft, false, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 8f), new Vector2(200f, 38f));
            MoneyText.fontStyle = FontStyle.Bold;

            var gearChip = MakePanel(root, "Gear Chip", new Color(1f, 0.96f, 0.87f, 0.94f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -92f), new Vector2(338f, 40f), true);
            GearText = MakeText(gearChip, "", 15, Palette.Ink, TextAnchor.MiddleLeft, false, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(320f, 30f));

            var bagChip = MakePanel(root, "Bag Chip", Palette.Cream, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -18f), new Vector2(158f, 66f), true);
            MakeText(bagChip, "BAG", 12, Palette.Teal, TextAnchor.UpperRight, false, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -10f), new Vector2(120f, 18f));
            BagText = MakeText(bagChip, "0 / 8", 26, Palette.Ink, TextAnchor.LowerRight, false, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 8f), new Vector2(130f, 34f));
            BagText.fontStyle = FontStyle.Bold;

            var weaponChip = MakePanel(root, "Weapon Chip", new Color(1f, 0.96f, 0.87f, 0.94f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -92f), new Vector2(238f, 40f), true);
            WeaponText = MakeText(weaponChip, "Fists", 15, Palette.Ink, TextAnchor.MiddleRight, false, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(220f, 30f));

            var hint = MakeText(root, "WASD move  ·  Shift sprint  ·  1 rod  ·  2 melee  ·  3 pistol  ·  4 shotgun  ·  5 dynamite  ·  R reload  ·  F eat  ·  E interact  ·  Tab log  ·  Esc pause",
                13, new Color(1f, 1f, 1f, 0.92f), TextAnchor.LowerLeft, true, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(22f, 16f), new Vector2(980f, 26f));
            hint.horizontalOverflow = HorizontalWrapMode.Overflow;

            PromptText = MakeText(root, "", 21, new Color(1f, 1f, 1f, 1f), TextAnchor.LowerCenter, true, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 108f), new Vector2(760f, 40f));
            PromptText.fontStyle = FontStyle.Bold;

            var chargeGo = MakePanel(root, "Cast Charge", new Color(0.1f, 0.2f, 0.24f, 0.55f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 160f), new Vector2(310f, 26f));
            ChargePanel = chargeGo.gameObject;
            var chargeBack = MakePanel(chargeGo, "Track", new Color(1f, 1f, 1f, 0.25f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(298f, 12f));
            var chargeFillGo = MakePanel(chargeBack, "Fill", Palette.Teal, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(292f, 8f));
            ChargeFill = chargeFillGo;
            UITheme.Fill(ChargeFill);
            MakeText(chargeGo, "CAST CHARGE", 11, Palette.Cream, TextAnchor.LowerCenter, true, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 6f), new Vector2(300f, 18f));
            ChargePanel.SetActive(false);

            var fishing = MakePanel(root, "Fishing Panel", new Color(0.09f, 0.2f, 0.24f, 0.62f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 146f), new Vector2(486f, 108f));
            FishingPanel = fishing.gameObject;

            MakeText(fishing, "TENSION", 12, Palette.Cream, TextAnchor.MiddleLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(90f, 18f));
            var tensionBack = MakePanel(fishing, "Tension Track", new Color(0.02f, 0.06f, 0.08f, 0.7f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(114f, -12f), new Vector2(352f, 18f));
            var tensionFillGo = MakePanel(tensionBack, "Fill", Palette.Gold, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(346f, 12f));
            TensionFill = tensionFillGo.GetComponent<RectTransform>();
            UITheme.Fill(TensionFill);
            TensionFillImage = tensionFillGo.GetComponent<Image>();
            var marker = MakePanel(tensionBack, "Safe Marker", Palette.Coral, new Vector2(0.55f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(4f, 22f));
            TensionMarker = marker;

            MakeText(fishing, "REEL", 12, Palette.Cream, TextAnchor.MiddleLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -52f), new Vector2(90f, 18f));
            var progressBack = MakePanel(fishing, "Reel Track", new Color(0.02f, 0.06f, 0.08f, 0.7f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(114f, -46f), new Vector2(352f, 18f));
            var progressFillGo = MakePanel(progressBack, "Fill", Palette.Teal, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(346f, 12f));
            ProgressFill = progressFillGo.GetComponent<RectTransform>();
            UITheme.Fill(ProgressFill);

            MakeText(fishing, "FISH STAMINA", 10, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -82f), new Vector2(96f, 14f));
            var staminaBack = MakePanel(fishing, "Stamina Track", new Color(0.02f, 0.06f, 0.08f, 0.7f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(114f, -80f), new Vector2(352f, 10f));
            var staminaFillGo = MakePanel(staminaBack, "Fill", Palette.Coral, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(346f, 6f));
            StaminaFill = staminaFillGo.GetComponent<RectTransform>();
            UITheme.Fill(StaminaFill);
            FishingPanel.SetActive(false);

            BiteText = MakeText(root, "!!  BITE  !!", 46, Palette.Coral, TextAnchor.MiddleCenter, true, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 84f), new Vector2(700f, 80f));
            BiteText.fontStyle = FontStyle.Bold;
            BiteText.gameObject.SetActive(false);

            BuildCatchCard(root);
            BuildToasts(root);

            var tintGo = new GameObject("Underwater Tint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tintGo.transform.SetParent(root, false);
            var tintRt = tintGo.GetComponent<RectTransform>();
            UITheme.Fill(tintRt);
            UnderwaterTint = tintGo.GetComponent<Image>();
            UnderwaterTint.color = new Color(0.08f, 0.35f, 0.47f, 0.3f);
            UnderwaterTint.raycastTarget = false;
            UnderwaterTint.gameObject.SetActive(false);

            var vitals = MakePanel(root, "Vitals", new Color(0.09f, 0.2f, 0.24f, 0.62f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(22f, 48f), new Vector2(300f, 56f));
            MakeText(vitals, "HP", 11, Palette.Cream, TextAnchor.MiddleLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -10f), new Vector2(28f, 16f));
            var hpBack = MakePanel(vitals, "HP Track", new Color(0.02f, 0.06f, 0.08f, 0.7f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -10f), new Vector2(210f, 12f));
            var hpGo = MakePanel(hpBack, "HP Fill", new Color(0.86f, 0.3f, 0.26f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(206f, 8f));
            HpFill = hpGo;
            UITheme.Fill(HpFill);
            MakeText(vitals, "FOOD", 10, new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -32f), new Vector2(32f, 14f));
            var hungerBack = MakePanel(vitals, "Hunger Track", new Color(0.02f, 0.06f, 0.08f, 0.7f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -32f), new Vector2(210f, 10f));
            var hungerGo = MakePanel(hungerBack, "Hunger Fill", new Color(0.95f, 0.72f, 0.3f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(206f, 6f));
            HungerFill = hungerGo;
            UITheme.Fill(HungerFill);
            HpText = MakeText(vitals, "100", 10, Palette.Cream, TextAnchor.MiddleRight, false, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -10f), new Vector2(44f, 14f));
            HungerText = MakeText(vitals, "100", 10, Palette.Cream, TextAnchor.MiddleRight, false, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -32f), new Vector2(44f, 14f));

            var cross = new GameObject("Crosshair", typeof(RectTransform));
            cross.transform.SetParent(root, false);
            var crossRt = cross.GetComponent<RectTransform>();
            UITheme.Place(crossRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 24f));
            MakePanel(cross.transform, "Dot", new Color(1f, 0.98f, 0.9f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4f, 4f));
            MakePanel(cross.transform, "Left", new Color(1f, 0.98f, 0.9f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-11f, 0f), new Vector2(7f, 1.5f));
            MakePanel(cross.transform, "Right", new Color(1f, 0.98f, 0.9f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(11f, 0f), new Vector2(7f, 1.5f));
            MakePanel(cross.transform, "Up", new Color(1f, 0.98f, 0.9f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 11f), new Vector2(1.5f, 7f));
            MakePanel(cross.transform, "Down", new Color(1f, 0.98f, 0.9f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -11f), new Vector2(1.5f, 7f));
            Crosshair = cross;
            Crosshair.SetActive(false);
            crosshairParts.AddRange(cross.GetComponentsInChildren<Image>());

            var target = MakePanel(root, "Target Panel", new Color(0.09f, 0.2f, 0.24f, 0.72f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(420f, 56f));
            TargetPanel = target.gameObject;
            TargetNameText = MakeText(target, "", 14, Palette.Cream, TextAnchor.MiddleLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -8f), new Vector2(388f, 18f));
            var targetBack = MakePanel(target, "Target Track", new Color(0.02f, 0.06f, 0.08f, 0.7f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 10f), new Vector2(388f, 12f));
            var targetFillGo = MakePanel(targetBack, "Target Fill", Palette.Coral, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(382f, 8f));
            TargetFill = targetFillGo;
            UITheme.Fill(TargetFill);
            TargetFillImage = targetFillGo.GetComponent<Image>();
            TargetPanel.SetActive(false);

            WorldStatus = MakeText(root, "", 13, new Color(1f, 1f, 1f, 0.75f), TextAnchor.LowerRight, true, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-22f, 16f), new Vector2(400f, 24f));

            var radar = MakePanel(root, "Radar", new Color(0.09f, 0.2f, 0.24f, 0.62f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-22f, 52f), new Vector2(320f, 150f));
            RadarPanel = radar.gameObject;
            MakeText(radar, "RADAR", 12, Palette.Gold, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -10f), new Vector2(292f, 18f));
            RadarText = MakeText(radar, "", 12, Palette.Cream, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -30f), new Vector2(292f, 110f));
            RadarText.lineSpacing = 1.15f;
            RadarPanel.SetActive(false);

            var damage = new GameObject("Damage Flash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            damage.transform.SetParent(root, false);
            var damageRt = damage.GetComponent<RectTransform>();
            UITheme.Fill(damageRt);
            DamageFlash = damage.GetComponent<Image>();
            DamageFlash.color = new Color(0.78f, 0.06f, 0.06f, 0f);
            DamageFlash.raycastTarget = false;

            var death = new GameObject("Death Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            death.transform.SetParent(root, false);
            var deathRt = death.GetComponent<RectTransform>();
            UITheme.Fill(deathRt);
            death.GetComponent<Image>().color = new Color(0.02f, 0.05f, 0.06f, 0.74f);
            death.GetComponent<Image>().raycastTarget = false;
            MakeText(death.transform, "KNOCKED OUT", 46, Palette.Coral, TextAnchor.MiddleCenter, true, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 22f), new Vector2(700f, 70f)).fontStyle = FontStyle.Bold;
            MakeText(death.transform, "You wake up at the dock in a moment…", 18, Palette.Cream, TextAnchor.MiddleCenter, true, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(700f, 30f));
            DeathPanel = death;
            DeathPanel.SetActive(false);
        }

        private void BuildCatchCard(Transform root)
        {
            var card = MakePanel(root, "Catch Card", Palette.Cream, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-34f, 40f), new Vector2(348f, 252f), true);
            CatchCard = card.gameObject;
            var accent = MakePanel(card, "Accent", Palette.Teal, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(348f, 10f));
            CatchAccent = accent.GetComponent<Image>();
            MakeText(card, "CAUGHT!", 13, Palette.Teal, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -22f), new Vector2(200f, 18f));
            CatchName = MakeText(card, "", 26, Palette.Ink, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -44f), new Vector2(308f, 44f));
            CatchName.fontStyle = FontStyle.Bold;
            CatchTier = MakeText(card, "", 12, Palette.Cream, TextAnchor.MiddleCenter, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -94f), new Vector2(110f, 24f));
            CatchWeight = MakeText(card, "", 16, Palette.Ink, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(142f, -94f), new Vector2(180f, 26f));
            CatchValue = MakeText(card, "", 32, Palette.Teal, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -128f), new Vector2(300f, 40f));
            CatchValue.fontStyle = FontStyle.Bold;
            CatchRare = MakeText(card, "RARE VARIANT  ·  5x VALUE", 12, Palette.Coral, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -178f), new Vector2(300f, 18f));
            CatchFooter = MakeText(card, "", 13, new Color(0.35f, 0.42f, 0.46f, 1f), TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -204f), new Vector2(300f, 20f));
            var tierBgGo = MakePanel(card, "Tier BG", Palette.Teal, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -94f), new Vector2(110f, 24f));
            tierBgGo.SetSiblingIndex(4);
            CatchTier.transform.SetAsLastSibling();
            CatchCard.SetActive(false);
        }

        private void BuildToasts(Transform root)
        {
            var container = new GameObject("Toasts", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            container.transform.SetParent(root, false);
            var rt = container.GetComponent<RectTransform>();
            UITheme.Place(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(560f, 200f));
            var layout = container.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 6f;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            var fitter = container.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ToastContainer = rt;
        }

        private void BuildMenu()
        {
            MenuCanvas = MakeCanvas("Menu Canvas", 20);
            var root = MenuCanvas.transform;
            var dim = new GameObject("Dim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dim.transform.SetParent(root, false);
            UITheme.Fill(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0.03f, 0.12f, 0.16f, 0.14f);
            dim.GetComponent<Image>().raycastTarget = false;
            MenuDim = dim;

            var panel = MakePanel(root, "Menu Panel", new Color(1f, 0.965f, 0.89f, 0.97f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(76f, 0f), new Vector2(540f, 566f), true);
            MenuPanel = panel.gameObject;

            MakeText(panel, "CASTAWAY COVE", 50, Palette.Ink, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -46f), new Vector2(470f, 64f)).fontStyle = FontStyle.Bold;
            MakeText(panel, "a cozy island fishing adventure", 18, Palette.Teal, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(44f, -112f), new Vector2(450f, 26f));
            var line = MakePanel(panel, "Rule", Palette.Coral, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(44f, -152f), new Vector2(90f, 4f));
            line.name = "Rule";

            var play = UITheme.Button(panel, "Play", () => GameBoot.I.StartGame(false), Palette.Teal, Palette.Cream, 24);
            var playRt = play.GetComponent<RectTransform>();
            UITheme.Place(playRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(44f, -190f), new Vector2(330f, 58f));

            ContinueButton = UITheme.Button(panel, "Continue", () => GameBoot.I.StartGame(true), new Color(0.93f, 0.85f, 0.68f, 1f), Palette.Ink, 24);
            var contRt = ContinueButton.GetComponent<RectTransform>();
            UITheme.Place(contRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(44f, -262f), new Vector2(330f, 58f));

            var quit = UITheme.Button(panel, "Quit", () => GameBoot.I.QuitGame(), Palette.Coral, Palette.Cream, 24);
            var quitRt = quit.GetComponent<RectTransform>();
            UITheme.Place(quitRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(44f, -334f), new Vector2(330f, 58f));

            DifficultyButton = UITheme.Button(panel, "Difficulty: Normal", () => GameBoot.I.ToggleDifficulty(), new Color(0.93f, 0.85f, 0.68f, 1f), Palette.Ink, 16);
            UITheme.Place(DifficultyButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(44f, -408f), new Vector2(330f, 42f));

            MenuMoneyHint = MakeText(panel, "", 14, new Color(0.36f, 0.44f, 0.47f, 1f), TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(44f, -466f), new Vector2(440f, 40f));
            MakeText(panel, "WASD move  |  mouse look  |  E interact\n1 rod  |  2/3/4 weapons  |  5 dynamite  |  LMB attack/cast  |  R reload  |  F eat  |  Tab log", 14, new Color(0.36f, 0.44f, 0.47f, 1f), TextAnchor.UpperLeft, false, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(44f, 16f), new Vector2(500f, 56f));
            MakeText(root, "v1.0  ·  offline single player", 12, new Color(1f, 1f, 1f, 0.65f), TextAnchor.UpperRight, true, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -140f), new Vector2(300f, 20f));
            MenuPanel.SetActive(false);
        }

        private void BuildModal()
        {
            ModalCanvas = MakeCanvas("Modal Canvas", 26);
            var root = ModalCanvas.transform;

            var dim = new GameObject("Dim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dim.transform.SetParent(root, false);
            UITheme.Fill(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0.02f, 0.09f, 0.12f, 0.45f);
            dim.GetComponent<Image>().raycastTarget = true;
            ModalDim = dim;

            BuildPause(root);
            BuildShop(root);
            BuildQuests(root);
            BuildWeapons(root);
            BuildGrill(root);
            BuildFishipedia(root);
            BuildFortune(root);
            ModalDim.SetActive(false);
        }

        private void BuildPause(Transform root)
        {
            var panel = MakePanel(root, "Pause Panel", new Color(1f, 0.965f, 0.89f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 470f), true);
            PausePanel = panel.gameObject;
            MakeText(panel, "PAUSED", 34, Palette.Ink, TextAnchor.UpperCenter, false, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(360f, 44f)).fontStyle = FontStyle.Bold;
            MakeText(panel, "The cove waits for you.", 15, Palette.Teal, TextAnchor.UpperCenter, false, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(340f, 24f));

            string[] labels = { "Resume", "Save Game", "Load Game", "Quit to Menu", "Quit Game" };
            Action[] actions =
            {
                () => GameBoot.I.SetPaused(false),
                () => GameBoot.I.ManualSave(),
                () => GameBoot.I.ManualLoad(),
                () => GameBoot.I.QuitToMenu(),
                () => GameBoot.I.QuitGame(),
            };
            for (int i = 0; i < labels.Length; i++)
            {
                bool accent = i == 0;
                bool danger = i == labels.Length - 1;
                Color bg = accent ? Palette.Teal : danger ? Palette.Coral : new Color(0.93f, 0.85f, 0.68f, 1f);
                Color fg = accent || danger ? Palette.Cream : Palette.Ink;
                var b = UITheme.Button(panel, labels[i], actions[i], bg, fg, 19);
                var rt = b.GetComponent<RectTransform>();
                UITheme.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 108f - i * 56f), new Vector2(320f, 46f));
            }
            MakeText(panel, "Esc to resume", 13, new Color(0.4f, 0.47f, 0.5f, 1f), TextAnchor.LowerCenter, false, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(300f, 20f));
            PausePanel.SetActive(false);
        }

        private void BuildShop(Transform root)
        {
            var panel = MakePanel(root, "Shop Panel", new Color(1f, 0.965f, 0.89f, 0.99f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 686f), true);
            ShopPanel = panel.gameObject;

            MakeText(panel, "COVE SHOP", 30, Palette.Ink, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -26f), new Vector2(300f, 40f)).fontStyle = FontStyle.Bold;
            ShopMoneyText = MakeText(panel, "$0", 24, Palette.Teal, TextAnchor.UpperRight, false, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-116f, -28f), new Vector2(180f, 34f));
            ShopMoneyText.fontStyle = FontStyle.Bold;
            var close = UITheme.Button(panel, "Close", () => GameBoot.I.CloseShop(), new Color(0.85f, 0.8f, 0.7f, 1f), Palette.Ink, 14);
            var closeRt = close.GetComponent<RectTransform>();
            UITheme.Place(closeRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -24f), new Vector2(78f, 34f));

            MakeText(panel, "Rods make casts longer, reels faster and lines more forgiving. Bait improves bite rate and the odds of rarer fish.", 13, new Color(0.4f, 0.47f, 0.5f, 1f), TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -72f), new Vector2(630f, 40f));

            for (int i = 0; i < 3; i++)
            {
                int tier = i;
                rodRows[i] = BuildShopRow(panel, -122f - i * 78f, GameData.Rods[i].name, () => GameBoot.I.BuyRod(tier));
            }
            var baitRow = BuildShopRow(panel, -122f - 3 * 78f, "Bait Pack", () => GameBoot.I.BuyBait());
            BaitItemDesc = baitRow.desc;
            BaitBuyButton = baitRow.buy;
            var bagRow = BuildShopRow(panel, -122f - 4 * 78f, "Bag Upgrade", () => GameBoot.I.BuyBag());
            BagItemDesc = bagRow.desc;
            BagBuyButton = bagRow.buy;
            var sellRow = BuildShopRow(panel, -122f - 5 * 78f, "Sell All Fish", () => GameBoot.I.SellAll());
            SellItemDesc = sellRow.desc;
            SellButton = sellRow.buy;
            SellButtonLabel = sellRow.buyLabel;

            MakeText(panel, "Buying and selling saves your progress automatically.", 12, new Color(0.45f, 0.5f, 0.52f, 1f), TextAnchor.LowerCenter, false, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(500f, 20f));
            ShopPanel.SetActive(false);
        }

        private ShopRowUI BuildShopRow(Transform parent, float y, string name, Action onClick)
        {
            var row = MakePanel(parent, name + " Row", new Color(0.97f, 0.91f, 0.78f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(636f, 72f));
            var ui = new ShopRowUI();
            ui.title = MakeText(row, name, 18, Palette.Ink, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -8f), new Vector2(300f, 24f));
            ui.title.fontStyle = FontStyle.Bold;
            ui.desc = MakeText(row, "", 12, new Color(0.36f, 0.43f, 0.46f, 1f), TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -33f), new Vector2(380f, 30f));
            ui.price = MakeText(row, "", 16, Palette.Teal, TextAnchor.UpperRight, false, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-146f, -14f), new Vector2(140f, 24f));
            ui.buy = UITheme.Button(row, "Buy", onClick, Palette.Teal, Palette.Cream, 14);
            var rt = ui.buy.GetComponent<RectTransform>();
            UITheme.Place(rt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(122f, 40f));
            ui.buyLabel = ui.buy.GetComponentInChildren<Text>();
            return ui;
        }

        private void BuildWeapons(Transform root)
        {            var panel = MakePanel(root, "Weapon Panel", new Color(1f, 0.965f, 0.89f, 0.99f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 620f), true);
            WeaponPanel = panel.gameObject;

            MakeText(panel, "WEAPON RACK", 30, Palette.Ink, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -26f), new Vector2(300f, 40f)).fontStyle = FontStyle.Bold;
            WeaponMoneyText = MakeText(panel, "$0", 24, Palette.Teal, TextAnchor.UpperRight, false, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-116f, -28f), new Vector2(180f, 34f));
            WeaponMoneyText.fontStyle = FontStyle.Bold;
            var close = UITheme.Button(panel, "Close", () => GameBoot.I.CloseWeapons(), new Color(0.85f, 0.8f, 0.7f, 1f), Palette.Ink, 14);
            var closeRt = close.GetComponent<RectTransform>();
            UITheme.Place(closeRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -24f), new Vector2(78f, 34f));

            MakeText(panel, "Weapons finish what the rod starts. Buy once, then press 2, 3 or 4 to equip. Ammo is spent per shot.", 13, new Color(0.4f, 0.47f, 0.5f, 1f), TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -72f), new Vector2(630f, 40f));

            weaponRows[0] = BuildShopRow(panel, -122f, "Knife", () => GameBoot.I.BuyWeapon(1));
            weaponRows[1] = BuildShopRow(panel, -200f, "Pistol", () => GameBoot.I.BuyWeapon(2));
            weaponRows[2] = BuildShopRow(panel, -278f, "Shotgun", () => GameBoot.I.BuyWeapon(3));

            var pistolAmmo = BuildShopRow(panel, -356f, "Pistol Ammo", () => GameBoot.I.BuyAmmo(2));
            PistolAmmoDesc = pistolAmmo.desc;
            PistolAmmoButton = pistolAmmo.buy;
            var shotgunAmmo = BuildShopRow(panel, -434f, "Shotgun Shells", () => GameBoot.I.BuyAmmo(3));
            ShotgunAmmoDesc = shotgunAmmo.desc;
            ShotgunAmmoButton = shotgunAmmo.buy;
            var dynamite = BuildShopRow(panel, -512f, "Dynamite", () => GameBoot.I.BuyDynamite());
            DynamiteDesc = dynamite.desc;
            DynamiteButton = dynamite.buy;

            MakeText(panel, "Buying weapons and ammo saves your progress automatically.", 12, new Color(0.45f, 0.5f, 0.52f, 1f), TextAnchor.LowerCenter, false, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(500f, 20f));
            WeaponPanel.SetActive(false);
        }

        private void BuildGrill(Transform root)
        {
            var panel = MakePanel(root, "Grill Panel", new Color(1f, 0.965f, 0.89f, 0.99f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 420f), true);
            GrillPanel = panel.gameObject;
            MakeText(panel, "GRILL", 30, Palette.Ink, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -26f), new Vector2(300f, 40f)).fontStyle = FontStyle.Bold;
            var close = UITheme.Button(panel, "Close", () => GameBoot.I.CloseGrill(), new Color(0.85f, 0.8f, 0.7f, 1f), Palette.Ink, 14);
            var closeRt = close.GetComponent<RectTransform>();
            UITheme.Place(closeRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -24f), new Vector2(78f, 34f));
            MakeText(panel, "Cooking raises a fish's value by 1.5x. There is a 15% chance to burn it down to 10% value. Cooked fish heal more when eaten.", 14, new Color(0.4f, 0.47f, 0.5f, 1f), TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -78f), new Vector2(550f, 60f));
            GrillText = MakeText(panel, "", 15, Palette.Ink, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -156f), new Vector2(550f, 80f));
            var cook = UITheme.Button(panel, "Cook all fish", () => GameBoot.I.CookAll(false), Palette.Teal, Palette.Cream, 16);
            UITheme.Place(cook.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-130f, 70f), new Vector2(240f, 46f));
            LavaCookButton = UITheme.Button(panel, "Lava cook (safe)", () => GameBoot.I.CookAll(true), new Color(0.93f, 0.55f, 0.25f, 1f), Palette.Cream, 14);
            UITheme.Place(LavaCookButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(130f, 70f), new Vector2(220f, 46f));
            MakeText(panel, "The lava vent is on Isle Five.", 12, new Color(0.45f, 0.5f, 0.52f, 1f), TextAnchor.LowerCenter, false, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(500f, 20f));
            GrillPanel.SetActive(false);
        }

        private void BuildFishipedia(Transform root)
        {
            var panel = MakePanel(root, "Fishipedia Panel", new Color(1f, 0.965f, 0.89f, 0.99f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 620f), true);
            FishipediaPanel = panel.gameObject;
            MakeText(panel, "FISHIPEDIA", 30, Palette.Ink, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -26f), new Vector2(300f, 40f)).fontStyle = FontStyle.Bold;
            MakeText(panel, "Every catch is logged here. Drip variants are the bright pink rarities.", 13, Palette.Teal, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -66f), new Vector2(600f, 22f));
            var close = UITheme.Button(panel, "Close", () => GameBoot.I.CloseFishipedia(), new Color(0.85f, 0.8f, 0.7f, 1f), Palette.Ink, 14);
            UITheme.Place(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -24f), new Vector2(78f, 34f));
            FishipediaText = MakeText(panel, "", 14, Palette.Ink, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -102f), new Vector2(610f, 300f));
            FishipediaText.lineSpacing = 1.1f;
            MakeText(panel, "ACHIEVEMENTS", 16, Palette.Teal, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -404f), new Vector2(300f, 22f)).fontStyle = FontStyle.Bold;
            AchievementsText = MakeText(panel, "", 13, new Color(0.36f, 0.43f, 0.46f, 1f), TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -432f), new Vector2(610f, 150f));
            FishipediaPanel.SetActive(false);
        }

        private void BuildFortune(Transform root)
        {
            var panel = MakePanel(root, "Fortune Panel", new Color(1f, 0.965f, 0.89f, 0.99f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 460f), true);
            FortunePanel = panel.gameObject;
            MakeText(panel, "REEL OF FORTUNE", 30, Palette.Ink, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -26f), new Vector2(400f, 40f)).fontStyle = FontStyle.Bold;
            var close = UITheme.Button(panel, "Close", () => GameBoot.I.CloseFortune(), new Color(0.85f, 0.8f, 0.7f, 1f), Palette.Ink, 14);
            UITheme.Place(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -24f), new Vector2(78f, 34f));
            FortuneText = MakeText(panel, "", 14, Palette.Ink, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -82f), new Vector2(570f, 130f));
            var gacha = UITheme.Button(panel, "Gacha spin (Drip / $100)", () => GameBoot.I.SpinGacha(), Palette.Teal, Palette.Cream, 15);
            UITheme.Place(gacha.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 174f), new Vector2(360f, 44f));
            var roulette = UITheme.Button(panel, "Roulette ($50)", () => GameBoot.I.PlayRoulette(), new Color(0.85f, 0.72f, 0.5f, 1f), Palette.Ink, 15);
            UITheme.Place(roulette.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-100f, 116f), new Vector2(200f, 44f));
            var slots = UITheme.Button(panel, "Slots ($25)", () => GameBoot.I.PlaySlots(), new Color(0.85f, 0.72f, 0.5f, 1f), Palette.Ink, 15);
            UITheme.Place(slots.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(100f, 116f), new Vector2(200f, 44f));
            MakeText(panel, "Drip fish are consumed by the gacha. The casino uses in-game money only.", 12, new Color(0.45f, 0.5f, 0.52f, 1f), TextAnchor.LowerCenter, false, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(560f, 20f));
            FortunePanel.SetActive(false);
        }

        private void BuildQuests(Transform root)
        {
            var panel = MakePanel(root, "Quest Panel", new Color(1f, 0.965f, 0.89f, 0.99f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 660f), true);
            QuestPanel = panel.gameObject;

            MakeText(panel, "ISLAND QUESTS", 30, Palette.Ink, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -28f), new Vector2(360f, 40f)).fontStyle = FontStyle.Bold;
            var close = UITheme.Button(panel, "Close", () => GameBoot.I.CloseQuests(), new Color(0.85f, 0.8f, 0.7f, 1f), Palette.Ink, 14);
            var closeRt = close.GetComponent<RectTransform>();
            UITheme.Place(closeRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -24f), new Vector2(78f, 34f));

            QuestSummary = MakeText(panel, "", 14, Palette.Teal, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -76f), new Vector2(520f, 22f));

            BuildQuestRow(panel, -96f, "1", out Quest1Title, out Quest1Progress, out Quest1Chip);
            BuildQuestRow(panel, -188f, "2", out Quest2Title, out Quest2Progress, out Quest2Chip);
            BuildQuestRow(panel, -280f, "3", out Quest3Title, out Quest3Progress, out Quest3Chip);
            BuildQuestRow(panel, -372f, "4", out Quest4Title, out Quest4Progress, out Quest4Chip);
            BuildQuestRow(panel, -464f, "5", out Quest5Title, out Quest5Progress, out Quest5Chip);

            MakeText(panel, "Defeat each island boss, collect its trophy and hand it in to open the next island.", 13, new Color(0.4f, 0.47f, 0.5f, 1f), TextAnchor.LowerCenter, false, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(560f, 34f));
            QuestPanel.SetActive(false);
        }

        private void BuildQuestRow(Transform parent, float y, string number, out Text title, out Text progress, out Image chip)
        {
            var row = MakePanel(parent, "Quest " + number, new Color(0.97f, 0.91f, 0.78f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(540f, 92f));
            var numPanel = MakePanel(row, "Number", Palette.Teal, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(48f, 48f));
            MakeText(numPanel, number, 22, Palette.Cream, TextAnchor.MiddleCenter, false, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f)).fontStyle = FontStyle.Bold;
            chip = MakePanel(row, "Status", Palette.Teal, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(112f, 28f)).GetComponent<Image>();
            var chipText = MakeText(chip.transform, "IN PROGRESS", 10, Palette.Cream, TextAnchor.MiddleCenter, false, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 22f));
            chipText.name = "Chip Text";
            title = MakeText(row, "", 19, Palette.Ink, TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(80f, -14f), new Vector2(330f, 28f));
            title.fontStyle = FontStyle.Bold;
            progress = MakeText(row, "", 14, new Color(0.36f, 0.43f, 0.46f, 1f), TextAnchor.UpperLeft, false, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(80f, -46f), new Vector2(330f, 26f));
        }

        private void Update()
        {
            if (BiteText != null && BiteText.gameObject.activeSelf)
            {
                bitePulse += Time.unscaledDeltaTime * 9f;
                float s = 1f + Mathf.Sin(bitePulse) * 0.06f;
                BiteText.rectTransform.localScale = new Vector3(s, s, 1f);
            }
        }

        public void RefreshHud()
        {
            var g = GameBoot.I != null ? GameBoot.I.State : null;
            if (g == null) return;
            if (MoneyText != null) MoneyText.text = GameData.Money(g.money);
            if (GearText != null)
            {
                string rod = GameData.Rods[Mathf.Clamp(g.rodTier, 0, GameData.Rods.Length - 1)].name;
                GearText.text = rod + "   ·   Bait " + g.bait;
            }
            if (BagText != null) BagText.text = g.bag.Count + " / " + g.bagCapacity;
            if (ShopMoneyText != null) ShopMoneyText.text = GameData.Money(g.money);
            if (WeaponMoneyText != null) WeaponMoneyText.text = GameData.Money(g.money);
            if (WeaponText != null && GameBoot.I.Combat != null) WeaponText.text = GameBoot.I.Combat.WeaponHudText();
            SetVitals(g.hp, 100f, g.hunger, 100f);
            RefreshShopRows();
            RefreshWeaponRows();
            if (GrillPanel != null && GrillPanel.activeSelf) RefreshGrill();
            if (FishipediaPanel != null && FishipediaPanel.activeSelf) RefreshFishipedia();
            if (FortunePanel != null && FortunePanel.activeSelf) RefreshFortune();
            if (QuestPanel != null && QuestPanel.activeSelf) RefreshQuests();
        }

        public void SetPrompt(string text)
        {
            if (PromptText == null) return;
            PromptText.text = text ?? "";
        }

        public void SetCharge(bool visible, float value)
        {
            if (ChargePanel == null) return;
            ChargePanel.SetActive(visible);
            if (ChargeFill != null)
            {
                float t = Mathf.Clamp01(value);
                ChargeFill.anchorMax = new Vector2(t, 1f);
                ChargeFill.offsetMin = new Vector2(0f, 0f);
                ChargeFill.offsetMax = new Vector2(0f, 0f);
                var img = ChargeFill.GetComponent<Image>();
                if (img != null) img.color = Color.Lerp(Palette.Teal, Palette.Coral, Mathf.Clamp01(t * 1.2f - 0.15f));
            }
        }

        public void ShowBite(bool show)
        {
            if (BiteText == null) return;
            BiteText.gameObject.SetActive(show);
            if (show) bitePulse = 0f;
        }

        public void SetFishing(bool visible, float tension, float progress, float safe)
        {
            if (FishingPanel == null) return;
            FishingPanel.SetActive(visible);
            if (!visible) return;
            if (TensionFill != null)
            {
                TensionFill.anchorMax = new Vector2(Mathf.Clamp01(tension), 1f);
                TensionFill.offsetMin = Vector2.zero;
                TensionFill.offsetMax = Vector2.zero;
            }
            if (TensionFillImage != null)
            {
                TensionFillImage.color = tension > safe ? Palette.Coral : Color.Lerp(Palette.Teal, Palette.Gold, Mathf.Clamp01(tension / Mathf.Max(0.01f, safe)));
            }
            if (TensionMarker != null)
            {
                float t = Mathf.Clamp01(safe);
                TensionMarker.anchorMin = new Vector2(t, 0.5f);
                TensionMarker.anchorMax = new Vector2(t, 0.5f);
                TensionMarker.anchoredPosition = Vector2.zero;
            }
            if (ProgressFill != null)
            {
                ProgressFill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
                ProgressFill.offsetMin = Vector2.zero;
                ProgressFill.offsetMax = Vector2.zero;
            }
        }

        public void SetFishStamina(float stamina)
        {
            if (StaminaFill == null) return;
            StaminaFill.anchorMax = new Vector2(Mathf.Clamp01(stamina), 1f);
            StaminaFill.offsetMin = Vector2.zero;
            StaminaFill.offsetMax = Vector2.zero;
        }

        public void Toast(string message)
        {
            if (ToastContainer == null) return;
            if (string.IsNullOrEmpty(message)) return;
            var panel = UITheme.Panel(ToastContainer, "Toast", new Color(0.07f, 0.2f, 0.24f, 0.86f));
            var le = panel.AddComponent<LayoutElement>();
            le.preferredHeight = 42f;
            le.preferredWidth = 540f;
            le.minHeight = 42f;
            var t = UITheme.Label(panel.transform, message, 16, new Color(1f, 0.98f, 0.92f, 1f), TextAnchor.MiddleCenter, false);
            UITheme.Stretch(t.rectTransform, 18f, 4f, 18f, 4f);
            toasts.Add(panel);
            while (toasts.Count > 3)
            {
                var old = toasts[0];
                toasts.RemoveAt(0);
                if (old != null) Destroy(old);
            }
            StartCoroutine(ToastLife(panel));
        }

        private IEnumerator ToastLife(GameObject go)
        {
            yield return new WaitForSecondsRealtime(3.4f);
            float t = 0f;
            var img = go.GetComponent<Image>();
            var texts = go.GetComponentsInChildren<Text>();
            while (t < 0.45f && go != null)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - t / 0.45f;
                if (img != null) { var c = img.color; c.a = 0.86f * a; img.color = c; }
                foreach (var tx in texts) { var c = tx.color; c.a = a; tx.color = c; }
                yield return null;
            }
            toasts.Remove(go);
            if (go != null) Destroy(go);
        }

        public void ShowCatch(CaughtFish fish, bool stored)
        {
            if (CatchCard == null) return;
            CatchCard.SetActive(true);
            CatchAccent.color = GameData.TierColor(fish.tier);
            CatchName.text = fish.species;
            CatchTier.text = GameData.TierLabelUpper(fish.tier);
            var tierBg = CatchTier.transform.parent.Find("Tier BG");
            if (tierBg != null) tierBg.GetComponent<Image>().color = GameData.TierColor(fish.tier);
            CatchWeight.text = fish.weight.ToString("0.0") + " kg";
            CatchValue.text = GameData.Money(fish.value);
            if (fish.drip)
            {
                CatchRare.text = "DRIP VARIANT  ·  8x VALUE";
                CatchRare.gameObject.SetActive(true);
            }
            else
            {
                CatchRare.text = "RARE VARIANT  ·  5x VALUE";
                CatchRare.gameObject.SetActive(fish.rareVariant);
            }
            CatchFooter.text = stored ? "Added to your bag" : "Still alive — finish it with a weapon";
            if (!string.IsNullOrEmpty(fish.killBonus)) CatchFooter.text += "  ·  " + fish.killBonus;
            if (catchRoutine != null) StopCoroutine(catchRoutine);
            catchRoutine = StartCoroutine(CatchLife());
        }

        private IEnumerator CatchLife()
        {
            yield return new WaitForSecondsRealtime(4.2f);
            if (CatchCard != null) CatchCard.SetActive(false);
            catchRoutine = null;
        }

        public void SetUnderwater(bool under)
        {
            if (UnderwaterTint != null) UnderwaterTint.gameObject.SetActive(under);
        }

        public void ShowMenu(bool show, bool hasSave)
        {
            MenuOpen = show;
            if (MenuPanel != null) MenuPanel.SetActive(show);
            if (MenuDim != null) MenuDim.SetActive(show);
            if (HudCanvas != null) HudCanvas.enabled = !show;
            if (MenuMoneyHint != null)
            {
                var saved = SaveSystem.Load(out string loadErr);
                MenuMoneyHint.text = saved != null
                    ? "Saved cove: " + GameData.Money(saved.money) + "  ·  " + GameData.Rods[Mathf.Clamp(saved.rodTier, 0, GameData.Rods.Length - 1)].name + "  ·  " + saved.catchCount + " fish caught"
                    : "No save yet — Play begins a fresh cove.";
            }
            if (ContinueButton != null)
            {
                ContinueButton.interactable = hasSave;
                var txt = ContinueButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = hasSave ? "Continue" : "Continue (no save)";
            }
            RefreshDifficulty();
            if (show) SetPrompt("");
        }

        public void RefreshDifficulty()
        {
            if (DifficultyButton == null) return;
            var txt = DifficultyButton.GetComponentInChildren<Text>();
            bool hard = GameBoot.I != null && GameBoot.I.State != null && GameBoot.I.State.hardMode;
            if (txt != null) txt.text = "Difficulty: " + (hard ? "Hard" : "Normal");
        }

        public void ShowPause(bool show)
        {
            Paused = show;
            if (PausePanel != null) PausePanel.SetActive(show);
            RefreshModalDim();
        }

        public void ShowShop(bool show)
        {
            ShopOpen = show;
            if (ShopPanel != null) ShopPanel.SetActive(show);
            RefreshModalDim();
            if (show) RefreshHud();
        }

        public void ShowQuests(bool show)
        {
            QuestOpen = show;
            if (QuestPanel != null) QuestPanel.SetActive(show);
            RefreshModalDim();
            if (show) RefreshQuests();
        }

        public void SetRadar(bool show, string text)
        {
            if (RadarPanel != null) RadarPanel.SetActive(show);
            if (RadarText != null && show) RadarText.text = text;
        }

        public void ShowWeapons(bool show)
        {
            WeaponOpen = show;
            if (WeaponPanel != null) WeaponPanel.SetActive(show);
            RefreshModalDim();
            if (show) RefreshHud();
        }

        public void ShowGrill(bool show)
        {
            GrillOpen = show;
            if (GrillPanel != null) GrillPanel.SetActive(show);
            RefreshModalDim();
            if (show) RefreshGrill();
        }

        public void RefreshGrill()
        {
            var g = GameBoot.I != null ? GameBoot.I.State : null;
            if (g == null || GrillText == null) return;
            int raw = 0;
            int cooked = 0;
            for (int i = 0; i < g.bag.Count; i++)
            {
                if (g.bag[i].cooked) cooked++;
                else raw++;
            }
            GrillText.text = "Bag: " + raw + " raw fish, " + cooked + " cooked.\nRaw value total: " + GameData.Money(BagValue(false)) + "\nCooked value total: " + GameData.Money(BagValue(true));
            if (LavaCookButton != null) LavaCookButton.gameObject.SetActive(g.island5);
        }

        public void ShowFishipedia(bool show)
        {
            FishipediaOpen = show;
            if (FishipediaPanel != null) FishipediaPanel.SetActive(show);
            RefreshModalDim();
            if (show) RefreshFishipedia();
        }

        public void RefreshFishipedia()
        {
            var g = GameBoot.I != null ? GameBoot.I.State : null;
            if (g == null) return;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < GameData.Species.Length; i++)
            {
                var sp = GameData.Species[i];
                bool found = g.discovered != null && g.discovered.Contains(sp.name);
                sb.AppendLine((found ? "[x] " : "[ ] ") + (found ? sp.name : "???") + "   " + GameData.TierLabel(sp.tier));
            }
            if (FishipediaText != null) FishipediaText.text = sb.ToString();
            if (AchievementsText != null)
            {
                if (g.achievements == null || g.achievements.Count == 0) AchievementsText.text = "No achievements yet.";
                else AchievementsText.text = string.Join("   ·   ", g.achievements.ToArray());
            }
        }

        public void ShowFortune(bool show)
        {
            FortuneOpen = show;
            if (FortunePanel != null) FortunePanel.SetActive(show);
            RefreshModalDim();
            if (show) RefreshFortune();
        }

        public void RefreshFortune()
        {
            var g = GameBoot.I != null ? GameBoot.I.State : null;
            if (g == null || FortuneText == null) return;
            int drip = 0;
            for (int i = 0; i < g.bag.Count; i++) if (g.bag[i].drip) drip++;
            FortuneText.text = "Money: " + GameData.Money(g.money) + "\nDrip fish in bag: " + drip + "\nCurrent skin: " + GameBoot.SkinName(g.skinIndex);
        }

        private int BagValue(bool cookedOnly)
        {
            var g = GameBoot.I != null ? GameBoot.I.State : null;
            if (g == null) return 0;
            int total = 0;
            for (int i = 0; i < g.bag.Count; i++)
            {
                if (cookedOnly && !g.bag[i].cooked) continue;
                total += g.bag[i].value;
            }
            return total;
        }

        private void RefreshModalDim()
        {
            if (ModalDim != null) ModalDim.SetActive(Paused || ShopOpen || QuestOpen || WeaponOpen || GrillOpen || FishipediaOpen || FortuneOpen);
        }

        public void SetVitals(float hp, float maxHp, float hunger, float maxHunger)
        {
            float h = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;
            float f = maxHunger > 0f ? Mathf.Clamp01(hunger / maxHunger) : 0f;
            if (HpFill != null)
            {
                HpFill.anchorMax = new Vector2(h, 1f);
                HpFill.offsetMin = Vector2.zero;
                HpFill.offsetMax = Vector2.zero;
            }
            if (HungerFill != null)
            {
                HungerFill.anchorMax = new Vector2(f, 1f);
                HungerFill.offsetMin = Vector2.zero;
                HungerFill.offsetMax = Vector2.zero;
            }
            if (HpText != null) HpText.text = Mathf.RoundToInt(hp).ToString();
            if (HungerText != null) HungerText.text = Mathf.RoundToInt(hunger).ToString();
        }

        public void SetWeapon(string text, bool showCrosshair, int index)
        {
            if (WeaponText != null) WeaponText.text = text;
            if (Crosshair != null) Crosshair.SetActive(showCrosshair);
        }

        public void SetCrosshairTarget(bool on)
        {
            Color c = on ? Palette.Coral : new Color(1f, 0.98f, 0.9f, 0.85f);
            for (int i = 0; i < crosshairParts.Count; i++)
                if (crosshairParts[i] != null) crosshairParts[i].color = c;
        }

        public void SetTargetBar(bool show, string name, float hp, float maxHp, bool boss)
        {
            if (TargetPanel == null) return;
            TargetPanel.SetActive(show);
            if (!show) return;
            TargetNameText.text = name;
            float t = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;
            TargetFill.anchorMax = new Vector2(t, 1f);
            TargetFill.offsetMin = Vector2.zero;
            TargetFill.offsetMax = Vector2.zero;
            TargetFillImage.color = boss ? new Color(0.92f, 0.32f, 0.22f, 1f) : new Color(0.95f, 0.55f, 0.3f, 1f);
        }

        public void FlashDamage()
        {
            if (DamageFlash == null) return;
            if (damageRoutine != null) StopCoroutine(damageRoutine);
            damageRoutine = StartCoroutine(DamageRoutine());
        }

        private IEnumerator DamageRoutine()
        {
            float t = 0f;
            while (t < 0.45f && DamageFlash != null)
            {
                t += Time.unscaledDeltaTime;
                float a = 0.38f * (1f - t / 0.45f);
                var c = DamageFlash.color;
                c.a = a;
                DamageFlash.color = c;
                yield return null;
            }
            if (DamageFlash != null)
            {
                var c = DamageFlash.color;
                c.a = 0f;
                DamageFlash.color = c;
            }
            damageRoutine = null;
        }

        public void ShowDeath(bool show)
        {
            if (DeathPanel != null) DeathPanel.SetActive(show);
        }

        public void RefreshShopRows()
        {
            var g = GameBoot.I != null ? GameBoot.I.State : null;
            if (g == null) return;

            for (int i = 0; i < rodRows.Length; i++)
            {
                var row = rodRows[i];
                if (row == null) continue;
                var def = GameData.Rods[i];
                row.title.text = def.name;
                row.desc.text = def.blurb + "  Cast " + def.castDistance.ToString("0") + " m · Reel " + def.reelSpeed.ToString("0.00") + "x · Tension " + def.tensionTolerance.ToString("0.00");
                if (i <= g.rodTier)
                {
                    row.price.text = i == 0 ? "Starting rod" : "";
                    row.buyLabel.text = i == g.rodTier ? "Equipped" : "Owned";
                    row.buy.interactable = false;
                }
                else if (i == g.rodTier + 1)
                {
                    row.price.text = GameData.Money(def.price);
                    row.buyLabel.text = "Buy Rod";
                    row.buy.interactable = g.money >= def.price;
                }
                else
                {
                    row.price.text = GameData.Money(def.price);
                    row.buyLabel.text = "Locked";
                    row.buy.interactable = false;
                }
            }            if (BaitItemDesc != null)
            {
                BaitItemDesc.text = "+" + GameData.BaitPackAmount + " bait. Faster bites and better odds of rare fish.  (you have " + g.bait + ")";
            }
            if (BaitBuyButton != null)
            {
                BaitBuyButton.interactable = g.money >= GameData.BaitPackPrice;
                var lbl = BaitBuyButton.GetComponentInChildren<Text>();
                if (lbl != null) lbl.text = "Buy · " + GameData.Money(GameData.BaitPackPrice);
            }
            if (BagItemDesc != null)
            {
                if (g.bagCapacity >= GameData.BagUpgradeCapacity)
                {
                    BagItemDesc.text = "Your bag holds " + g.bagCapacity + " fish. Fully upgraded.";
                    if (BagBuyButton != null)
                    {
                        BagBuyButton.interactable = false;
                        var lbl = BagBuyButton.GetComponentInChildren<Text>();
                        if (lbl != null) lbl.text = "Owned";
                    }
                }
                else
                {
                    BagItemDesc.text = "Woven bag: holds " + g.bagCapacity + " → " + GameData.BagUpgradeCapacity + " fish.";
                    if (BagBuyButton != null)
                    {
                        BagBuyButton.interactable = g.money >= GameData.BagUpgradePrice;
                        var lbl = BagBuyButton.GetComponentInChildren<Text>();
                        if (lbl != null) lbl.text = "Buy · " + GameData.Money(GameData.BagUpgradePrice);
                    }
                }
            }
            if (SellItemDesc != null)
            {
                int total = 0;
                for (int i = 0; i < g.bag.Count; i++) total += g.bag[i].value;
                if (g.bag.Count == 0) SellItemDesc.text = "No fish in your bag. Catch some at the shore or from the boat.";
                else SellItemDesc.text = "Sell all " + g.bag.Count + " fish for " + GameData.Money(total) + ".";
                if (SellButton != null)
                {
                    SellButton.interactable = g.bag.Count > 0;
                    SellButtonLabel.text = g.bag.Count > 0 ? "Sell All" : "Empty";
                }
            }
        }

        public void RefreshWeaponRows()
        {
            var g = GameBoot.I != null ? GameBoot.I.State : null;
            if (g == null) return;
            for (int i = 0; i < weaponRows.Length; i++)
            {
                var row = weaponRows[i];
                if (row == null) continue;
                int index = i + 1;
                var def = GameData.Weapons[index];
                bool owned = index == 1 ? g.knifeOwned : index == 2 ? g.pistolOwned : g.shotgunOwned;
                row.title.text = def.name;
                row.desc.text = def.blurb + (def.melee ? "  Damage " + def.damage : "  Damage " + def.damage + " · Mag " + def.magSize + " · Range " + def.range.ToString("0") + " m");
                if (owned)
                {
                    row.price.text = "Owned";
                    row.buyLabel.text = g.weaponIndex == index ? "Equipped" : "Equip";
                    row.buy.interactable = false;
                }
                else
                {
                    row.price.text = GameData.Money(def.price);
                    row.buyLabel.text = "Buy";
                    row.buy.interactable = g.money >= def.price;
                }
            }

            if (PistolAmmoDesc != null)
            {
                if (!g.pistolOwned) PistolAmmoDesc.text = "Buy the Pistol first.";
                else PistolAmmoDesc.text = "Pistol rounds: " + g.pistolMag + " loaded / " + g.pistolAmmo + " spare. Pack of " + GameData.PistolAmmoAmount + ".";
                if (PistolAmmoButton != null)
                {
                    PistolAmmoButton.interactable = g.pistolOwned && g.money >= GameData.PistolAmmoPrice;
                    var lbl = PistolAmmoButton.GetComponentInChildren<Text>();
                    if (lbl != null) lbl.text = "Buy · " + GameData.Money(GameData.PistolAmmoPrice);
                }
            }
            if (ShotgunAmmoDesc != null)
            {
                if (!g.shotgunOwned) ShotgunAmmoDesc.text = "Buy the Shotgun first.";
                else ShotgunAmmoDesc.text = "Shotgun shells: " + g.shotgunMag + " loaded / " + g.shotgunAmmo + " spare. Pack of " + GameData.ShotgunAmmoAmount + ".";
                if (ShotgunAmmoButton != null)
                {
                    ShotgunAmmoButton.interactable = g.shotgunOwned && g.money >= GameData.ShotgunAmmoPrice;
                    var lbl = ShotgunAmmoButton.GetComponentInChildren<Text>();
                    if (lbl != null) lbl.text = "Buy · " + GameData.Money(GameData.ShotgunAmmoPrice);
                }
            }
            if (DynamiteDesc != null)
            {
                DynamiteDesc.text = "Throwable · 60 area damage · you have " + g.dynamite + ".";
                if (DynamiteButton != null)
                {
                    DynamiteButton.interactable = g.money >= GameData.DynamitePrice;
                    var lbl = DynamiteButton.GetComponentInChildren<Text>();
                    if (lbl != null) lbl.text = "Buy · " + GameData.Money(GameData.DynamitePrice);
                }
            }
        }

        public void RefreshQuests()
        {
            var g = GameBoot.I != null ? GameBoot.I.State : null;
            if (g == null) return;
            bool q1 = g.bossKilled;
            bool q2 = g.piranhaKilled;
            bool q3 = g.pufferKilled;
            bool q4 = g.albatrossKilled;
            bool q5 = g.whaleKilled;

            if (QuestSummary != null)
            {
                QuestSummary.text = "Catches " + g.catchCount + "  ·  kills " + g.kills + "  ·  earnings " + GameData.Money(g.earnedTotal);
            }
            if (Quest1Title != null) Quest1Title.text = "Defeat the Spider Crab";
            if (Quest1Progress != null) Quest1Progress.text = q1 ? "Complete — boat keys and Isle Two are yours." : "Bait the crab near the shop, dodge its charge, punish the stun.";
            SetQuestChip(Quest1Chip, q1, "DONE");

            if (Quest2Title != null) Quest2Title.text = "Defeat the Giant Piranha (Isle Two)";
            if (Quest2Progress != null) Quest2Progress.text = q2 ? "Complete — Isle Three is open." : !g.island2 ? "Locked — finish the Spider Crab first." : "Challenge the altar on Isle Two.";
            SetQuestChip(Quest2Chip, q2, "DONE");

            if (Quest3Title != null) Quest3Title.text = "Defeat the Pufferfish (Isle Three)";
            if (Quest3Progress != null) Quest3Progress.text = q3 ? "Complete — Isle Four is open." : !g.island3 ? "Locked — finish the Giant Piranha first." : "Challenge the altar on Isle Three.";
            SetQuestChip(Quest3Chip, q3, "DONE");

            if (Quest4Title != null) Quest4Title.text = "Defeat the Albatross (Isle Four)";
            if (Quest4Progress != null) Quest4Progress.text = q4 ? "Complete — Isle Five is open." : !g.island4 ? "Locked — finish the Pufferfish first." : "Challenge the altar on Isle Four.";
            SetQuestChip(Quest4Chip, q4, "DONE");

            if (Quest5Title != null) Quest5Title.text = "Defeat the Mutated Whale (Isle Five)";
            if (Quest5Progress != null) Quest5Progress.text = q5 ? "Complete — you made it home." : !g.island5 ? "Locked — finish the Albatross first." : "Challenge the altar on Isle Five.";
            SetQuestChip(Quest5Chip, q5, "DONE");
        }

        private void SetQuestChip(Image chip, bool done, string label)
        {
            if (chip == null) return;
            chip.color = done ? new Color(0.36f, 0.65f, 0.4f, 1f) : new Color(0.85f, 0.72f, 0.5f, 1f);
            var txt = chip.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.text = done ? label : "IN PROGRESS";
                txt.fontSize = done ? 11 : 10;
            }
        }
    }
}
