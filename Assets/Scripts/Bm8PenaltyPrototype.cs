using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class Bm8PenaltyPrototype : MonoBehaviour
{
    private static readonly bool UseAaAnimatedKeeper = true;
    private static readonly bool UseGeneratedKeeperSpriteSheet = false;
    private static readonly bool UseArcadeVideoCamera = true;
    private const float ShotWatchdogSeconds = 14f;
    private const double ShotWatchdogWallSeconds = 14d;
    private const float KeeperTestShotTimeoutSeconds = 15f;
    private const string UploadedStylizedKeeperPath = "Assets/Art/Characters/goalkeeper-stylized-rig-and-animation/source/ThuMon/Goalkeeper_TPose.FBX";
    private const string Bm8KeeperBaseTexturePath = "Assets/Art/Characters/goalkeeper-stylized-rig-and-animation/source/ThuMon/textures/Goalkeeper_Base_color.png";
    private const string AaGoalkeeperControllerFolder = "Assets/animo/AA_Soccer_Goalkeeper/Controller/";

    [Header("Scene Objects")]
    [SerializeField] private Transform ball;
    [SerializeField] private Transform player;
    [SerializeField] private Transform keeper;
    [SerializeField] private Transform cameraRig;
    [SerializeField] private Text statusText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Slider powerSlider;

    [Header("Imported Goalkeeper Animation")]
    [SerializeField] private Animator keeperAnimator;
    [SerializeField] private RuntimeAnimatorController keeperIdleController;
    [SerializeField] private RuntimeAnimatorController keeperCatchForwardSuccessController;
    [SerializeField] private RuntimeAnimatorController keeperCatchForwardFailController;
    [SerializeField] private RuntimeAnimatorController keeperCatchUpSuccessController;
    [SerializeField] private RuntimeAnimatorController keeperCatchUpFailController;
    [SerializeField] private RuntimeAnimatorController keeperCatchLeftDownSuccessController;
    [SerializeField] private RuntimeAnimatorController keeperCatchLeftDownFailController;
    [SerializeField] private RuntimeAnimatorController keeperCatchRightDownSuccessController;
    [SerializeField] private RuntimeAnimatorController keeperCatchRightDownFailController;
    [SerializeField] private RuntimeAnimatorController keeperHitLeftSuccessController;
    [SerializeField] private RuntimeAnimatorController keeperHitLeftFailController;
    [SerializeField] private RuntimeAnimatorController keeperHitRightSuccessController;
    [SerializeField] private RuntimeAnimatorController keeperHitRightFailController;
    [SerializeField] private RuntimeAnimatorController keeperHitTopLeftSuccessController;
    [SerializeField] private RuntimeAnimatorController keeperHitTopLeftFailController;
    [SerializeField] private RuntimeAnimatorController keeperHitTopRightSuccessController;
    [SerializeField] private RuntimeAnimatorController keeperHitTopRightFailController;

    private Vector3 ballStart;
    private Vector3 ballBaseScale;
    private Vector3 playerStart;
    private Vector3 keeperStart;
    private Transform strikerLeftLeg;
    private Transform strikerRightLeg;
    private Transform strikerLeftArm;
    private Transform strikerRightArm;
    private Transform strikerTorso;
    private Transform strikerHead;
    private Transform strikerVisibleModel;
    private Transform keeperVisibleModel;
    private Transform keeperLeftArm;
    private Transform keeperRightArm;
    private Transform keeperLeftLeg;
    private Transform keeperRightLeg;
    private Transform keeperTorso;
    private Transform keeperHead;
    private Transform keeperLeftGlove;
    private Transform keeperRightGlove;
    private Transform keeperSprite;
    private Renderer keeperSpriteRenderer;
    private Material keeperSpriteMaterial;
    private TrailRenderer ballTrail;
    private Transform ballShadow;
    private Material ballShadowMaterial;
    private Transform saveImpactFlash;
    private Material saveImpactMaterial;
    private Transform saveShockwave;
    private Material saveShockwaveMaterial;
    private Transform saveContactStreak;
    private Material saveContactStreakMaterial;
    private Transform resultGoalFlash;
    private Material resultGoalFlashMaterial;
    private Transform goalNetImpact;
    private Material goalNetImpactMaterial;
    private float resultGoalFlashUntil;
    private Color resultGoalFlashColor = Color.white;
    private RectTransform goalGrid;
    private int aimCol = 1;
    private int aimRow = 1;
    private int keeperCol = 1;
    private int keeperRow = 1;
    private readonly bool[] shotHistoryGoals = new bool[5];
    private readonly bool[] shotHistoryResolved = new bool[5];
    private bool shooting;
    private int goals;
    private int saves;
    private int shotCount;
    private int goalStreak;
    private int saveStreak;
    private float aimX;
    private float aimY = 2.1f;
    private float saveReboundSide = 1f;
    private bool usingKeeperSpriteSheet;
    private bool arcadeSceneFixed;
    private bool forceKeeperTestShot;
    private int forcedKeeperCol = 1;
    private int forcedKeeperRow = 1;
    private bool forcedKeeperSave = true;
    private bool showDebugControls;
    private bool keeperTestMode;
    private int keeperTestShotIndex;
    private int keeperTestShotTotal;
    private float shootingStartedRealtime;
    private double shootingStartedWallClock;
    private Vector3 importedKeeperAnchorLocalPosition = new Vector3(0f, 0f, -0.02f);
    private Quaternion importedKeeperAnchorLocalRotation = Quaternion.Euler(0f, 180f, 0f);
    private Vector3 importedKeeperAnchorLocalScale = Vector3.one * 1.05f;
    private Vector3 importedKeeperActionOffsetLocal;
    private Quaternion importedKeeperActionRotationOffset = Quaternion.identity;
    private float importedKeeperActionT;
    private Vector3 importedKeeperReadyBoundsCenterLocal;
    private bool importedKeeperBoundsCaptured;
    private string resultBanner = "";
    private string resultMultiplierText = "";
    private string resultStreakText = "";
    private float resultBannerUntil;
    private float resultBannerStartedAt;
    private Color resultBannerColor = Color.white;
    private float targetPulseStartedAt;
    private float targetLockStartedAt;
    private float targetLockUntil;
    private float kickFlashUntil;
    private Vector3 kickFlashWorld;
    private float shotSpeedLineUntil;
    private float shotSpeedLineStartedAt;
    private float shotSpeedLineSide;
    private float saveDustStartedAt;
    private float saveDustUntil;
    private Vector3 saveDustWorld;
    private float saveDustSide;
    private float saveContactSparkStartedAt;
    private float saveContactSparkUntil;
    private Vector3 saveContactSparkWorld;

    private void Awake()
    {
        ballStart = ball.position;
        ballBaseScale = ball.localScale;
        playerStart = player.position;
        keeperStart = keeper.position;
        CacheBodyParts();
        RemoveStrikerArmNumbers();
        ApplyRealisticCharacterDesign();
        HideStrikerControlRigWhenFbxExists();
        HideProceduralStrikerForArcadeCamera();
        if (UseAaAnimatedKeeper)
        {
            DisableKeeperSpriteSheet();
            EnsureUploadedStylizedKeeperInEditor();
            EnsureImportedKeeperActive();
        }
        else if (UseGeneratedKeeperSpriteSheet)
        {
            DisableImportedRobotKeeper();
            SetupKeeperSpriteSheet();
        }
        else
        {
            DisableImportedRobotKeeper();
            DisableKeeperSpriteSheet();
            EnsureProceduralKeeperVisible();
        }
        HideKeeperControlRigWhenFbxExists();
        EnsureKeeperGloves();
        HideKeeperMarkerGlovesWhenImportedKeeperIsActive();
        SetupBallTrail();
        EnsureBallShadow();
        EnsureSaveImpactFlash();
        EnsureSaveShockwave();
        EnsureSaveContactStreak();
        EnsureResultGoalFlash();
        EnsureGoalNetImpact();
        HideSolidGoalNetBackdrop();
        EnsureArcadeBackdrop();
        CreateNineTargetGrid();
        HideLegacyTextOverlay();
        UpdateScore();
        SetStatus(UseAaAnimatedKeeper ? "Tap goal" : "Tap goal");
    }

    private void Update()
    {
        if (!arcadeSceneFixed)
        {
            HideSolidGoalNetBackdrop();
            EnsureArcadeBackdrop();
            arcadeSceneFixed = true;
        }

        UpdateGoalGridOverlay();
        UpdateResultGoalFlash();
        UpdateArcadeBackdropPulse();
        RunShotWatchdog();

        if (shooting && Time.realtimeSinceStartup - shootingStartedRealtime > ShotWatchdogSeconds)
        {
            ResetShot();
            SetStatus("Shot timeout reset");
            return;
        }

        if (shooting)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.F10))
        {
            showDebugControls = !showDebugControls;
            SetStatus(showDebugControls ? "Debug controls" : "Tap goal");
            return;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            StartCoroutine(TestAllKeeperZones());
            return;
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            StartCoroutine(TestTopKeeperZones());
            return;
        }

        if (Input.GetMouseButtonDown(0) && TryShootFromMouse(Input.mousePosition))
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Keypad7) || Input.GetKeyDown(KeyCode.Alpha7))
        {
            ShootAt(0, 0);
        }
        else if (Input.GetKeyDown(KeyCode.Keypad8) || Input.GetKeyDown(KeyCode.Alpha8))
        {
            ShootAt(1, 0);
        }
        else if (Input.GetKeyDown(KeyCode.Keypad9) || Input.GetKeyDown(KeyCode.Alpha9))
        {
            ShootAt(2, 0);
        }
        else if (Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            ShootAt(0, 1);
        }
        else if (Input.GetKeyDown(KeyCode.Keypad5) || Input.GetKeyDown(KeyCode.Alpha5))
        {
            ShootAt(1, 1);
        }
        else if (Input.GetKeyDown(KeyCode.Keypad6) || Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            ShootAt(2, 1);
        }
        else if (Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Alpha1))
        {
            ShootAt(0, 2);
        }
        else if (Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Space))
        {
            ShootAt(1, 2);
        }
        else if (Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.Alpha3))
        {
            ShootAt(2, 2);
        }
    }

    private void LateUpdate()
    {
        RunShotWatchdog();
        if (UseAaAnimatedKeeper)
        {
            AnchorImportedKeeperVisibleModel();
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!UseAaAnimatedKeeper || keeperAnimator == null || !keeperAnimator.isHuman)
        {
            return;
        }

        if (!shooting)
        {
            Vector3 leftReady = keeper.TransformPoint(new Vector3(-0.34f, 1.18f, -0.34f));
            Vector3 rightReady = keeper.TransformPoint(new Vector3(0.34f, 1.18f, -0.34f));
            ApplyKeeperHandIk(leftReady, rightReady, 0.78f);
            return;
        }

        keeperAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
        keeperAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
        keeperAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
        keeperAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
    }

    private void ApplyKeeperHandIk(Vector3 leftTarget, Vector3 rightTarget, float weight)
    {
        float clamped = Mathf.Clamp01(weight);
        keeperAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, clamped);
        keeperAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, clamped * 0.55f);
        keeperAnimator.SetIKPosition(AvatarIKGoal.LeftHand, leftTarget);
        keeperAnimator.SetIKRotation(AvatarIKGoal.LeftHand, KeeperHandLookRotation(leftTarget));

        keeperAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, clamped);
        keeperAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, clamped * 0.55f);
        keeperAnimator.SetIKPosition(AvatarIKGoal.RightHand, rightTarget);
        keeperAnimator.SetIKRotation(AvatarIKGoal.RightHand, KeeperHandLookRotation(rightTarget));
    }

    private Quaternion KeeperHandLookRotation(Vector3 target)
    {
        Vector3 aim = target - keeper.position;
        if (aim.sqrMagnitude < 0.0001f)
        {
            return keeper.rotation;
        }

        return Quaternion.LookRotation(aim.normalized, Vector3.up);
    }

    private void OnGUI()
    {
        if (!arcadeSceneFixed)
        {
            HideSolidGoalNetBackdrop();
            EnsureArcadeBackdrop();
            arcadeSceneFixed = true;
        }

        DrawArcadeHud();
        DrawGoalFramePulse();
        DrawIdleGoalGridGlow();
        DrawReadyAimGuide();
        DrawReadyBallAura();
        DrawShootingActionOverlay();
        DrawTargetLockOverlay();
        DrawShotSpeedLines();
        DrawSaveDropDust();
        DrawSaveContactSpark();
        DrawKickFlash();
        DrawRuntimeTestButton();
        RunShotWatchdog();

        if (shooting || !TryGetGoalGuiRect(out Rect goalRect))
        {
            return;
        }

        Event current = Event.current;
        if (current == null || current.type != EventType.MouseDown || current.button != 0 || !goalRect.Contains(current.mousePosition))
        {
            return;
        }

        float normalizedX = Mathf.InverseLerp(goalRect.xMin, goalRect.xMax, current.mousePosition.x);
        float normalizedY = Mathf.InverseLerp(goalRect.yMin, goalRect.yMax, current.mousePosition.y);
        int col = Mathf.Clamp(Mathf.FloorToInt(normalizedX * 3f), 0, 2);
        int row = Mathf.Clamp(Mathf.FloorToInt(normalizedY * 3f), 0, 2);
        ShootAt(col, row);
        current.Use();
    }

    private void DrawArcadeHud()
    {
        float width = Screen.width;
        float height = Screen.height;
        float topPad = Mathf.Max(8f, height * 0.018f);
        float topBarHeight = Mathf.Clamp(height * 0.16f, 74f, 108f);
        float bottomBarHeight = Mathf.Clamp(height * 0.11f, 62f, 84f);
        float bottomBarY = height - bottomBarHeight - Mathf.Max(16f, height * 0.045f);
        GUIStyle title = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(Mathf.Clamp(height * 0.04f, 15f, 22f)),
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        GUIStyle small = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(Mathf.Clamp(height * 0.032f, 11f, 15f)),
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.88f, 0.2f) }
        };
        GUIStyle panelText = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(Mathf.Clamp(height * 0.034f, 12f, 16f)),
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        FillGuiRect(new Rect(0f, 0f, width, topBarHeight), new Color(0.02f, 0.025f, 0.035f, 0.78f));
        FillGuiRect(new Rect(width * 0.018f, topPad, width * 0.15f, Mathf.Max(28f, topBarHeight * 0.34f)), new Color(0.1f, 0.04f, 0.035f, 0.9f));
        GUI.Label(new Rect(width * 0.018f, topPad, width * 0.15f, Mathf.Max(28f, topBarHeight * 0.34f)), "BM8 PENALTY", panelText);
        string status = statusText != null && !string.IsNullOrWhiteSpace(statusText.text) ? statusText.text : "Ready";
        status = status.Replace('\n', ' ').Replace('\r', ' ').Trim();
        if (status.Length > 24)
        {
            status = status.Substring(0, 24);
        }
        GUI.Label(new Rect(width * 0.34f, topPad + 6f, width * 0.32f, topBarHeight * 0.26f), status.ToUpperInvariant(), title);
        string scoreLine = keeperTestMode
            ? "TEST " + keeperTestShotIndex + " / " + keeperTestShotTotal
            : "GOALS " + goals + "   SAVES " + saves + "   SHOTS " + shotCount;
        GUI.Label(new Rect(width * 0.34f, topPad + topBarHeight * 0.36f, width * 0.32f, topBarHeight * 0.22f), scoreLine, small);
        DrawShotHistoryLights(width, topPad, topBarHeight);

        float chipStart = width * 0.27f;
        string[] chips = { "x2", "x4", "x12", "x20", "x32" };
        for (int i = 0; i < chips.Length; i++)
        {
            float x = chipStart + i * width * 0.11f;
            float chipY = topPad + topBarHeight * 0.66f;
            bool selectedChip = i == TargetMultiplierIndex();
            float pulse = selectedChip ? Mathf.Sin((Time.time - targetPulseStartedAt) * 12f) * 0.5f + 0.5f : 0f;
            float chipSize = selectedChip ? Mathf.Lerp(14f, 22f, pulse) : 11f;
            Color chipColor = i % 2 == 0 ? new Color(1f, 0.2f, 0.18f, 0.95f) : new Color(1f, 0.86f, 0.18f, 0.95f);
            if (selectedChip)
            {
                FillGuiRect(new Rect(x - chipSize * 0.85f, chipY - chipSize * 0.85f, chipSize * 2.7f, chipSize * 2.7f), new Color(1f, 0.92f, 0.16f, Mathf.Lerp(0.12f, 0.34f, pulse)));
                chipColor = new Color(1f, 0.98f, 0.32f, 1f);
            }
            FillGuiRect(new Rect(x - (chipSize - 11f) * 0.5f, chipY - (chipSize - 11f) * 0.5f, chipSize, chipSize), chipColor);
            GUI.Label(new Rect(x - 15f, chipY + 12f, 44f, 18f), chips[i], selectedChip ? panelText : small);
        }

        Rect bottom = new Rect(width * 0.18f, bottomBarY, width * 0.64f, bottomBarHeight);
        FillGuiRect(bottom, new Color(0.12f, 0.045f, 0.025f, 0.92f));
        GUI.Label(new Rect(bottom.x + 18f, bottom.y + 6f, 120f, 20f), "BET", small);
        GUI.Label(new Rect(bottom.x + 18f, bottom.y + bottom.height * 0.42f, 120f, 24f), "100.00", panelText);
        GUI.Label(new Rect(bottom.x + bottom.width * 0.38f, bottom.y + bottom.height * 0.3f, bottom.width * 0.24f, 28f), shooting ? "ACTION" : "TAP GOAL", panelText);
        GUI.Label(new Rect(bottom.x + bottom.width - 160f, bottom.y + 6f, 138f, 20f), "DEMO BALANCE", small);
        GUI.Label(new Rect(bottom.x + bottom.width - 160f, bottom.y + bottom.height * 0.42f, 138f, 24f), "1,000.00", panelText);

        if (!string.IsNullOrEmpty(resultBanner) && Time.time < resultBannerUntil)
        {
            float age = Mathf.Max(0f, Time.time - resultBannerStartedAt);
            float life = Mathf.Clamp01((resultBannerUntil - Time.time) / Mathf.Max(0.1f, resultBannerUntil - resultBannerStartedAt));
            float pop = Mathf.Sin(Mathf.Clamp01(age / 0.22f) * Mathf.PI);
            float pulse = Mathf.Sin(Time.time * 22f) * 0.5f + 0.5f;
            Color flash = new Color(resultBannerColor.r, resultBannerColor.g, resultBannerColor.b, Mathf.Lerp(0.22f, 0.46f, Mathf.Max(pulse, pop)) * life);
            FillGuiRect(new Rect(0f, height * 0.31f, width, height * 0.2f), flash);
            DrawResultBurst(age, life, width, height);
            GUIStyle bannerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(Mathf.Clamp(height * Mathf.Lerp(0.09f, 0.13f, pop), 36f, 82f)),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(0f, height * 0.29f - pop * 10f, width, height * 0.22f), resultBanner, bannerStyle);
            if (!string.IsNullOrEmpty(resultMultiplierText))
            {
                GUIStyle multiplierStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.RoundToInt(Mathf.Clamp(height * Mathf.Lerp(0.032f, 0.052f, pop), 16f, 34f)),
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, 0.92f, 0.2f, Mathf.Clamp01(life + 0.15f)) }
                };
                GUI.Label(new Rect(0f, height * 0.48f + pop * 8f, width, height * 0.08f), resultMultiplierText, multiplierStyle);
            }

            if (!string.IsNullOrEmpty(resultStreakText))
            {
                GUIStyle streakStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.RoundToInt(Mathf.Clamp(height * 0.03f, 14f, 24f)),
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, 1f, 1f, Mathf.Clamp01(life + 0.05f)) }
                };
                GUI.Label(new Rect(0f, height * 0.545f + pop * 5f, width, height * 0.06f), resultStreakText, streakStyle);
            }
        }
    }

    private void DrawShotHistoryLights(float width, float topPad, float topBarHeight)
    {
        float startX = width * 0.72f;
        float y = topPad + topBarHeight * 0.24f;
        float size = Mathf.Clamp(width * 0.012f, 9f, 15f);
        for (int i = 0; i < shotHistoryResolved.Length; i++)
        {
            bool active = i == shotCount % shotHistoryResolved.Length && shooting;
            float pulse = active ? Mathf.Sin(Time.time * 14f) * 0.5f + 0.5f : 0f;
            Color color = shotHistoryResolved[i]
                ? shotHistoryGoals[i] ? new Color(1f, 0.22f, 0.12f, 0.96f) : new Color(0.1f, 0.62f, 1f, 0.96f)
                : new Color(0.38f, 0.38f, 0.42f, 0.72f);
            if (active)
            {
                color = Color.Lerp(color, new Color(1f, 0.92f, 0.18f, 1f), pulse);
                FillGuiRect(new Rect(startX + i * size * 2.1f - size * 0.55f, y - size * 0.55f, size * 2.1f, size * 2.1f), new Color(1f, 0.88f, 0.18f, 0.16f + 0.22f * pulse));
            }

            FillGuiRect(new Rect(startX + i * size * 2.1f, y, size, size), color);
        }
    }

    private void DrawResultBurst(float age, float life, float width, float height)
    {
        float burst = Mathf.Sin(Mathf.Clamp01(age / 0.56f) * Mathf.PI);
        if (burst <= 0.001f)
        {
            return;
        }

        Vector2 center = new Vector2(width * 0.5f, height * 0.4f);
        for (int i = 0; i < 24; i++)
        {
            float seed = i * 12.9898f;
            float angle = i * Mathf.PI * 2f / 24f + Mathf.Sin(seed) * 0.18f;
            float distance = Mathf.Lerp(34f, Mathf.Min(width, height) * 0.26f, burst) * Mathf.Lerp(0.72f, 1.18f, Mathf.Repeat(seed, 1f));
            float size = Mathf.Lerp(10f, 3f, burst);
            Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.62f) * distance;
            Color color = Color.Lerp(resultBannerColor, new Color(1f, 0.95f, 0.2f), i % 2 == 0 ? 0.65f : 0.2f);
            color.a = Mathf.Lerp(0.8f, 0.08f, burst) * life;
            FillGuiRect(new Rect(pos.x - size * 0.5f, pos.y - size * 0.5f, size * 1.8f, size * 0.55f), color);
        }
    }

    private void DrawShootingActionOverlay()
    {
        if (!shooting || !string.IsNullOrEmpty(resultBanner) && Time.time < resultBannerUntil)
        {
            return;
        }

        float age = Mathf.Max(0f, Time.realtimeSinceStartup - shootingStartedRealtime);
        float width = Screen.width;
        float height = Screen.height;
        float charge = Mathf.Clamp01(age / 1.25f);
        float pulse = Mathf.Sin(Time.time * 14f) * 0.5f + 0.5f;
        float sideAlpha = Mathf.Lerp(0.06f, 0.22f, Mathf.Max(charge, pulse * 0.55f));
        FillGuiRect(new Rect(0f, 0f, width * 0.16f, height), new Color(0f, 0f, 0f, sideAlpha));
        FillGuiRect(new Rect(width * 0.84f, 0f, width * 0.16f, height), new Color(0f, 0f, 0f, sideAlpha));
        FillGuiRect(new Rect(0f, 0f, width, height * 0.075f), new Color(0f, 0f, 0f, sideAlpha * 0.72f));
        FillGuiRect(new Rect(0f, height * 0.925f, width, height * 0.075f), new Color(0f, 0f, 0f, sideAlpha * 0.72f));

        for (int i = 0; i < 7; i++)
        {
            float y = height * (0.24f + i * 0.078f);
            float phase = Mathf.Repeat(Time.time * 0.92f + i * 0.13f, 1f);
            float x = Mathf.Lerp(-width * 0.18f, width * 1.05f, phase);
            float lineWidth = Mathf.Lerp(width * 0.2f, width * 0.42f, charge);
            Color color = new Color(1f, 0.84f, 0.12f, Mathf.Lerp(0.04f, 0.16f, pulse) * (0.65f + charge * 0.35f));
            FillGuiRect(new Rect(x, y, lineWidth, 2.5f), color);
            FillGuiRect(new Rect(width - x - lineWidth, height - y, lineWidth, 2.5f), color);
        }
    }

    private void DrawShotSpeedLines()
    {
        if (Time.time >= shotSpeedLineUntil)
        {
            return;
        }

        float age = Mathf.Max(0f, Time.time - shotSpeedLineStartedAt);
        float life = Mathf.Clamp01((shotSpeedLineUntil - Time.time) / Mathf.Max(0.1f, shotSpeedLineUntil - shotSpeedLineStartedAt));
        float width = Screen.width;
        float height = Screen.height;
        float sweep = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / 0.32f));
        Color color = new Color(1f, 0.95f, 0.32f, 0.22f * life);

        for (int i = 0; i < 9; i++)
        {
            float y = height * (0.2f + i * 0.075f);
            float x = Mathf.Lerp(-width * 0.15f, width * 0.74f, sweep) + i * 17f * shotSpeedLineSide;
            float lineWidth = Mathf.Lerp(width * 0.28f, width * 0.08f, i / 8f);
            float thickness = Mathf.Lerp(5f, 1.8f, i / 8f);
            FillGuiRect(new Rect(x, y, lineWidth, thickness), color);
            FillGuiRect(new Rect(width - x - lineWidth, height - y, lineWidth, thickness), color);
        }
    }

    private void DrawSaveDropDust()
    {
        if (Time.time >= saveDustUntil || Camera.main == null)
        {
            return;
        }

        Vector3 screen = Camera.main.WorldToScreenPoint(saveDustWorld);
        if (screen.z <= 0f)
        {
            return;
        }

        float age = Mathf.Max(0f, Time.time - saveDustStartedAt);
        float life = Mathf.Clamp01((saveDustUntil - Time.time) / Mathf.Max(0.1f, saveDustUntil - saveDustStartedAt));
        float burst = Mathf.Sin(Mathf.Clamp01(age / 0.36f) * Mathf.PI);
        Vector2 center = new Vector2(screen.x, Screen.height - screen.y);
        for (int i = 0; i < 10; i++)
        {
            float seed = i * 9.173f;
            float spread = Mathf.Lerp(8f, 62f, burst) * Mathf.Lerp(0.72f, 1.18f, Mathf.Repeat(seed, 1f));
            float x = center.x + saveDustSide * spread + Mathf.Sin(seed) * 18f * burst;
            float y = center.y + Mathf.Cos(seed) * 8f * burst;
            float width = Mathf.Lerp(18f, 6f, burst);
            float height = Mathf.Lerp(7f, 2f, burst);
            FillGuiRect(new Rect(x - width * 0.5f, y - height * 0.5f, width, height), new Color(0.88f, 0.78f, 0.52f, Mathf.Lerp(0.5f, 0.04f, burst) * life));
        }
    }

    private void DrawSaveContactSpark()
    {
        if (Time.time >= saveContactSparkUntil || Camera.main == null)
        {
            return;
        }

        Vector3 screen = Camera.main.WorldToScreenPoint(saveContactSparkWorld);
        if (screen.z <= 0f)
        {
            return;
        }

        float age = Mathf.Max(0f, Time.time - saveContactSparkStartedAt);
        float life = Mathf.Clamp01((saveContactSparkUntil - Time.time) / Mathf.Max(0.1f, saveContactSparkUntil - saveContactSparkStartedAt));
        float pop = Mathf.Sin(Mathf.Clamp01(age / 0.18f) * Mathf.PI);
        Vector2 center = new Vector2(screen.x, Screen.height - screen.y);
        float length = Mathf.Lerp(18f, 76f, pop);
        float thickness = Mathf.Lerp(7f, 2.5f, pop);
        Color gold = new Color(1f, 0.94f, 0.18f, Mathf.Lerp(0.85f, 0.12f, pop) * life);
        Color white = new Color(1f, 1f, 1f, Mathf.Lerp(0.75f, 0.08f, pop) * life);
        FillGuiRect(new Rect(center.x - length * 0.5f, center.y - thickness * 0.5f, length, thickness), gold);
        FillGuiRect(new Rect(center.x - thickness * 0.5f, center.y - length * 0.5f, thickness, length), white);
        FillGuiRect(new Rect(center.x - length * 0.36f, center.y - thickness * 0.5f - length * 0.22f, length * 0.72f, thickness), gold);
        FillGuiRect(new Rect(center.x - length * 0.36f, center.y - thickness * 0.5f + length * 0.22f, length * 0.72f, thickness), gold);
    }

    private void DrawIdleGoalGridGlow()
    {
        if (shooting || !TryGetGoalGuiRect(out Rect goalRect))
        {
            return;
        }

        float cellWidth = goalRect.width / 3f;
        float cellHeight = goalRect.height / 3f;
        float pulse = Mathf.Sin(Time.time * 3.2f) * 0.5f + 0.5f;
        Color line = new Color(1f, 0.86f, 0.18f, Mathf.Lerp(0.08f, 0.18f, pulse));
        Color selected = new Color(1f, 0.92f, 0.18f, Mathf.Lerp(0.06f, 0.14f, pulse));
        Rect selectedCell = new Rect(goalRect.x + aimCol * cellWidth, goalRect.y + aimRow * cellHeight, cellWidth, cellHeight);
        FillGuiRect(new Rect(selectedCell.x + 4f, selectedCell.y + 4f, selectedCell.width - 8f, selectedCell.height - 8f), selected);

        for (int i = 1; i < 3; i++)
        {
            float x = goalRect.x + cellWidth * i;
            float y = goalRect.y + cellHeight * i;
            FillGuiRect(new Rect(x - 1f, goalRect.y, 2f, goalRect.height), line);
            FillGuiRect(new Rect(goalRect.x, y - 1f, goalRect.width, 2f), line);
        }
    }

    private void DrawGoalFramePulse()
    {
        if (!TryGetGoalGuiRect(out Rect goalRect))
        {
            return;
        }

        float active = shooting ? 1f : 0.35f;
        if (!string.IsNullOrEmpty(resultBanner) && Time.time < resultBannerUntil)
        {
            active = 1f;
        }

        float pulse = Mathf.Sin(Time.time * (shooting ? 10f : 4f)) * 0.5f + 0.5f;
        float thickness = Mathf.Lerp(3f, 7f, pulse) * active;
        float alpha = Mathf.Lerp(0.16f, 0.54f, pulse) * active;
        Color color = new Color(1f, 0.92f, 0.18f, alpha);
        Rect outer = new Rect(goalRect.x - 5f, goalRect.y - 5f, goalRect.width + 10f, goalRect.height + 10f);
        FillGuiRect(new Rect(outer.x, outer.y, outer.width, thickness), color);
        FillGuiRect(new Rect(outer.x, outer.yMax - thickness, outer.width, thickness), color);
        FillGuiRect(new Rect(outer.x, outer.y, thickness, outer.height), color);
        FillGuiRect(new Rect(outer.xMax - thickness, outer.y, thickness, outer.height), color);
    }

    private void DrawReadyBallAura()
    {
        if (shooting || Camera.main == null || !string.IsNullOrEmpty(resultBanner) && Time.time < resultBannerUntil)
        {
            return;
        }

        Vector3 screen = Camera.main.WorldToScreenPoint(ball.position);
        if (screen.z <= 0f)
        {
            return;
        }

        float pulse = Mathf.Sin(Time.time * 4.6f) * 0.5f + 0.5f;
        Vector2 center = new Vector2(screen.x, Screen.height - screen.y);
        float size = Mathf.Lerp(44f, 78f, pulse);
        Color ring = new Color(1f, 0.88f, 0.18f, Mathf.Lerp(0.14f, 0.34f, pulse));
        Color glow = new Color(1f, 0.2f, 0.1f, Mathf.Lerp(0.04f, 0.14f, pulse));
        FillGuiRect(new Rect(center.x - size * 0.5f, center.y - size * 0.11f, size, size * 0.22f), glow);
        FillGuiRect(new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, 3f), ring);
        FillGuiRect(new Rect(center.x - size * 0.5f, center.y + size * 0.5f - 3f, size, 3f), ring);
        FillGuiRect(new Rect(center.x - size * 0.5f, center.y - size * 0.5f, 3f, size), ring);
        FillGuiRect(new Rect(center.x + size * 0.5f - 3f, center.y - size * 0.5f, 3f, size), ring);
    }

    private void DrawReadyAimGuide()
    {
        if (shooting || Camera.main == null || !string.IsNullOrEmpty(resultBanner) && Time.time < resultBannerUntil || !TryGetGoalGuiRect(out Rect goalRect))
        {
            return;
        }

        Vector3 ballScreen = Camera.main.WorldToScreenPoint(ball.position + new Vector3(0f, 0.12f, 0f));
        if (ballScreen.z <= 0f)
        {
            return;
        }

        float cellWidth = goalRect.width / 3f;
        float cellHeight = goalRect.height / 3f;
        Vector2 from = new Vector2(ballScreen.x, Screen.height - ballScreen.y);
        Vector2 to = new Vector2(goalRect.x + (aimCol + 0.5f) * cellWidth, goalRect.y + (aimRow + 0.5f) * cellHeight);
        float pulse = Mathf.Sin(Time.time * 5.5f) * 0.5f + 0.5f;
        Color color = new Color(1f, 0.86f, 0.16f, Mathf.Lerp(0.1f, 0.28f, pulse));
        for (int i = 1; i <= 9; i++)
        {
            float t = i / 10f;
            Vector2 p = Vector2.Lerp(from, to, t);
            float size = Mathf.Lerp(5f, 9f, Mathf.Repeat(t + Time.time * 0.8f, 1f));
            FillGuiRect(new Rect(p.x - size * 0.5f, p.y - size * 0.5f, size, size), color);
        }
    }

    private void DrawTargetLockOverlay()
    {
        if (Time.time >= targetLockUntil || !TryGetGoalGuiRect(out Rect goalRect))
        {
            return;
        }

        float age = Mathf.Max(0f, Time.time - targetLockStartedAt);
        float life = Mathf.Clamp01((targetLockUntil - Time.time) / Mathf.Max(0.1f, targetLockUntil - targetLockStartedAt));
        float cellWidth = goalRect.width / 3f;
        float cellHeight = goalRect.height / 3f;
        Rect cell = new Rect(goalRect.x + aimCol * cellWidth, goalRect.y + aimRow * cellHeight, cellWidth, cellHeight);
        float pulse = Mathf.Sin(age * 24f) * 0.5f + 0.5f;
        float inset = Mathf.Lerp(10f, 2f, Mathf.Sin(Mathf.Clamp01(age / 0.18f) * Mathf.PI));
        Rect glow = new Rect(cell.x + inset, cell.y + inset, cell.width - inset * 2f, cell.height - inset * 2f);
        Color fill = new Color(1f, 0.82f, 0.08f, Mathf.Lerp(0.08f, 0.26f, pulse) * life);
        Color border = new Color(1f, 0.96f, 0.25f, Mathf.Lerp(0.42f, 0.92f, pulse) * life);
        FillGuiRect(glow, fill);
        FillGuiRect(new Rect(glow.x, glow.y, glow.width, 4f), border);
        FillGuiRect(new Rect(glow.x, glow.yMax - 4f, glow.width, 4f), border);
        FillGuiRect(new Rect(glow.x, glow.y, 4f, glow.height), border);
        FillGuiRect(new Rect(glow.xMax - 4f, glow.y, 4f, glow.height), border);
    }

    private void DrawKickFlash()
    {
        if (Time.time >= kickFlashUntil || Camera.main == null)
        {
            return;
        }

        Vector3 screen = Camera.main.WorldToScreenPoint(kickFlashWorld);
        if (screen.z <= 0f)
        {
            return;
        }

        float remaining = Mathf.Clamp01((kickFlashUntil - Time.time) / 0.18f);
        float pop = Mathf.Sin(remaining * Mathf.PI);
        float size = Mathf.Lerp(18f, 62f, pop);
        Rect rect = new Rect(screen.x - size * 0.5f, Screen.height - screen.y - size * 0.5f, size, size);
        FillGuiRect(rect, new Color(1f, 0.92f, 0.14f, Mathf.Lerp(0.05f, 0.42f, remaining)));
        FillGuiRect(new Rect(rect.x + size * 0.43f, rect.y - size * 0.2f, size * 0.14f, size * 1.4f), new Color(1f, 1f, 1f, Mathf.Lerp(0.04f, 0.32f, remaining)));
        FillGuiRect(new Rect(rect.x - size * 0.2f, rect.y + size * 0.43f, size * 1.4f, size * 0.14f), new Color(1f, 1f, 1f, Mathf.Lerp(0.04f, 0.32f, remaining)));
    }

    private void DrawRuntimeTestButton()
    {
        if (!showDebugControls)
        {
            return;
        }

        Rect buttonRect = new Rect(Screen.width - 114f, 82f, 92f, 30f);
        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.92f, 0.16f) },
            hover = { textColor = Color.white },
            active = { textColor = Color.white }
        };

        Color previousColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.12f, 0.04f, 0.035f, 0.96f);
        GUI.enabled = !shooting;
        if (GUI.Button(buttonRect, "TEST 9", style))
        {
            RunKeeperZoneTest();
        }

        Rect topTestRect = new Rect(Screen.width - 114f, 116f, 92f, 30f);
        if (GUI.Button(topTestRect, "TEST TOP", style))
        {
            RunTopKeeperZoneTest();
        }
        GUI.enabled = true;

        Rect resetRect = new Rect(Screen.width - 114f, 150f, 92f, 30f);
        if (GUI.Button(resetRect, "RESET", style))
        {
            ResetShot();
        }
        GUI.backgroundColor = previousColor;
    }

    private void RunShotWatchdog()
    {
        if (!shooting)
        {
            return;
        }

        double started = shootingStartedWallClock > 0.001d ? shootingStartedWallClock : WallClockSeconds();
        if (WallClockSeconds() - started > ShotWatchdogWallSeconds)
        {
            ResetShot();
            SetStatus("Shot watchdog reset");
        }
    }

    private static void FillGuiRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void ShowResultBanner(string message, Color color)
    {
        resultBanner = message;
        resultMultiplierText = TargetMultiplierLabel() + (message == "GOAL" ? " HIT" : " SAVED");
        resultStreakText = message == "GOAL" ? StreakLabel(goalStreak, "GOAL STREAK") : StreakLabel(saveStreak, "SAVE STREAK");
        resultBannerColor = color;
        resultBannerStartedAt = Time.time;
        resultBannerUntil = Time.time + 1.7f;
        ShowResultGoalFlash(color);
    }

    private static string StreakLabel(int streak, string label)
    {
        return streak >= 2 ? streak + "x " + label : "";
    }

    private void RecordShotHistory(bool goal)
    {
        int index = (shotCount - 1) % shotHistoryResolved.Length;
        if (index == 0)
        {
            for (int i = 0; i < shotHistoryResolved.Length; i++)
            {
                shotHistoryResolved[i] = false;
                shotHistoryGoals[i] = false;
            }
        }

        shotHistoryResolved[index] = true;
        shotHistoryGoals[index] = goal;
    }

    private string TargetMultiplierLabel()
    {
        string[] labels = { "x2", "x4", "x12", "x20", "x32" };
        return labels[Mathf.Clamp(TargetMultiplierIndex(), 0, labels.Length - 1)];
    }

    private int TargetMultiplierIndex()
    {
        if (aimRow == 0)
        {
            return aimCol == 0 ? 0 : aimCol == 1 ? 2 : 4;
        }

        if (aimRow == 1)
        {
            return aimCol == 1 ? 2 : aimCol == 0 ? 1 : 3;
        }

        return aimCol == 1 ? 1 : aimCol == 0 ? 0 : 4;
    }

    private bool TryShootFromMouse(Vector3 mousePosition)
    {
        if (!TryGetGoalScreenBounds(out Vector2 screenMin, out Vector2 screenMax))
        {
            return false;
        }

        if (mousePosition.x < screenMin.x || mousePosition.x > screenMax.x || mousePosition.y < screenMin.y || mousePosition.y > screenMax.y)
        {
            return false;
        }

        float normalizedX = Mathf.InverseLerp(screenMin.x, screenMax.x, mousePosition.x);
        float normalizedY = Mathf.InverseLerp(screenMin.y, screenMax.y, mousePosition.y);
        int col = Mathf.Clamp(Mathf.FloorToInt(normalizedX * 3f), 0, 2);
        int row = Mathf.Clamp(2 - Mathf.FloorToInt(normalizedY * 3f), 0, 2);
        ShootAt(col, row);
        return true;
    }

    private bool TryGetGoalGuiRect(out Rect goalRect)
    {
        goalRect = default;
        if (!TryGetGoalScreenBounds(out Vector2 screenMin, out Vector2 screenMax))
        {
            return false;
        }

        goalRect = new Rect(screenMin.x, Screen.height - screenMax.y, screenMax.x - screenMin.x, screenMax.y - screenMin.y);
        return goalRect.width > 1f && goalRect.height > 1f;
    }

    private bool TryGetGoalScreenBounds(out Vector2 screenMin, out Vector2 screenMax)
    {
        screenMin = Vector2.zero;
        screenMax = Vector2.zero;
        if (Camera.main == null)
        {
            return false;
        }

        Vector3 leftBottom = Camera.main.WorldToScreenPoint(new Vector3(-3.05f, 0.32f, 5.05f));
        Vector3 rightTop = Camera.main.WorldToScreenPoint(new Vector3(3.05f, 2.5f, 5.05f));
        if (leftBottom.z < 0f || rightTop.z < 0f)
        {
            return false;
        }

        screenMin = new Vector2(Mathf.Min(leftBottom.x, rightTop.x), Mathf.Min(leftBottom.y, rightTop.y));
        screenMax = new Vector2(Mathf.Max(leftBottom.x, rightTop.x), Mathf.Max(leftBottom.y, rightTop.y));
        return true;
    }


    public void AimLeft()
    {
        SetAimGrid(0, 1);
        SetStatus("Target: left middle");
    }

    public void AimCenter()
    {
        SetAimGrid(1, 1);
        SetStatus("Target: center middle");
    }

    public void AimRight()
    {
        SetAimGrid(2, 1);
        SetStatus("Target: right middle");
    }

    public void ShootTopLeft()
    {
        ShootAt(0, 0);
    }

    public void ShootTopCenter()
    {
        ShootAt(1, 0);
    }

    public void ShootTopRight()
    {
        ShootAt(2, 0);
    }

    public void ShootMiddleLeft()
    {
        ShootAt(0, 1);
    }

    public void ShootMiddleCenter()
    {
        ShootAt(1, 1);
    }

    public void ShootMiddleRight()
    {
        ShootAt(2, 1);
    }

    public void ShootBottomLeft()
    {
        ShootAt(0, 2);
    }

    public void ShootBottomCenter()
    {
        ShootAt(1, 2);
    }

    public void ShootBottomRight()
    {
        ShootAt(2, 2);
    }

    public void Shoot()
    {
        if (!shooting)
        {
            StartCoroutine(ShootRoutine());
        }
    }

    private void ShootAt(float x, float y)
    {
        aimX = x;
        aimY = y;
        SetStatus("9-grid target locked");
        Shoot();
    }

    private void ShootAt(int col, int row)
    {
        SetAimGrid(col, row);
        SetStatus("9-grid target locked");
        Shoot();
    }

    public void RunKeeperZoneTest()
    {
        if (!shooting)
        {
            StartCoroutine(TestAllKeeperZones());
        }
    }

    public void RunTopKeeperZoneTest()
    {
        if (!shooting)
        {
            StartCoroutine(TestTopKeeperZones());
        }
    }

    private void SetAimGrid(int col, int row)
    {
        aimCol = Mathf.Clamp(col, 0, 2);
        aimRow = Mathf.Clamp(row, 0, 2);
        aimX = GridX(aimCol);
        aimY = GridY(aimRow);
        targetPulseStartedAt = Time.time;
        targetLockStartedAt = Time.time;
        targetLockUntil = Time.time + 0.78f;
    }

    public void ResetShot()
    {
        StopAllCoroutines();
        shooting = false;
        forceKeeperTestShot = false;
        ClearImportedKeeperActionOffset();
        ball.position = ballStart;
        ball.rotation = Quaternion.identity;
        ball.localScale = ballBaseScale;
        UpdateBallShadow();
        if (ballTrail != null)
        {
            ballTrail.Clear();
        }
        HideSaveImpactFlash();
        HideSaveShockwave();
        HideSaveContactStreak();
        HideResultGoalFlash();
        HideGoalNetImpact();

        player.position = playerStart;
        player.rotation = Quaternion.identity;
        keeper.position = keeperStart;
        keeper.rotation = Quaternion.identity;
        ClearImportedKeeperActionOffset();
        ResetPose();
        cameraRig.position = ReadyCameraPosition();
        cameraRig.rotation = ReadyCameraRotation();
        SetStatus("Tap goal");
    }

    private IEnumerator ShootRoutine()
    {
        shooting = true;
        shootingStartedRealtime = Time.realtimeSinceStartup;
        shootingStartedWallClock = WallClockSeconds();
        ball.position = ballStart;
        ball.rotation = Quaternion.identity;
        ball.localScale = ballBaseScale;
        UpdateBallShadow();
        if (ballTrail != null)
        {
            ballTrail.Clear();
        }
        HideSaveImpactFlash();
        HideSaveShockwave();
        HideSaveContactStreak();
        HideResultGoalFlash();
        HideGoalNetImpact();

        player.position = playerStart;
        player.rotation = Quaternion.identity;
        keeper.position = keeperStart;
        keeper.rotation = Quaternion.identity;
        ResetPose();

        float power = powerSlider != null ? powerSlider.value : 0.75f;
        SetStatus("Run up");

        Vector3 runTarget = new Vector3(-0.35f, playerStart.y, -2.15f);
        yield return RunUp(playerStart, runTarget, 0.92f);

        SetStatus("Strike");
        yield return PlantAndKick(0.58f);

        Vector3 target = new Vector3(aimX, Mathf.Clamp(aimY + power * 0.25f, 0.85f, 2.42f), 5.15f);
        if (forceKeeperTestShot)
        {
            keeperCol = forcedKeeperCol;
            keeperRow = forcedKeeperRow;
        }
        else
        {
            PickKeeperGrid();
        }

        Vector3 keeperTarget = KeeperDiveTarget(keeperCol, keeperRow);
        bool save = forceKeeperTestShot ? forcedKeeperSave : ShouldKeeperSave(power);
        string keeperAction = KeeperActionName(keeperCol, keeperRow);
        SetStatus("Keeper " + keeperAction + " " + GridName(keeperCol, keeperRow));
        PlayKeeperDiveAnimation(save);
        float keeperDuration = save ? keeperRow == 0 ? 1.38f : 1.08f : 0.92f;
        StartCoroutine(DiveKeeper(keeperTarget, keeperDuration, save));
        saveReboundSide = keeperCol == 1 ? (aimCol == 0 ? -1f : aimCol == 2 ? 1f : UnityEngine.Random.value < 0.5f ? -1f : 1f) : Mathf.Sign(keeperCol - 1f);
        yield return FlyBall(ballStart, target, save, save ? keeperRow == 0 ? 1.22f : 1.08f : 0.9f);

        shotCount++;
        if (save)
        {
            saves++;
            saveStreak++;
            goalStreak = 0;
            RecordShotHistory(false);
            ShowResultBanner("SAVED", new Color(0.1f, 0.55f, 1f));
        }
        else
        {
            goals++;
            goalStreak++;
            saveStreak = 0;
            RecordShotHistory(true);
            ShowResultBanner("GOAL", new Color(1f, 0.2f, 0.12f));
        }

        UpdateScore();
        SetStatus(save ? "SAVED - " + keeperAction + " " + GridName(keeperCol, keeperRow) : "GOAL");
        StartCoroutine(ResultCameraPunch(save, saveReboundSide, save ? 0.48f : 0.42f));
        if (save && !UseAaAnimatedKeeper)
        {
            keeper.position = keeperStart;
            keeper.rotation = Quaternion.identity;
            ResetPose();
        }
        yield return new WaitForSeconds(save ? keeperRow == 0 ? 1.65f : 1.35f : 0.9f);
        yield return ReturnAllToReady(save ? 0.42f : 0.28f);
        SetStatus("Tap goal");
        shooting = false;
    }

    private IEnumerator TestAllKeeperZones()
    {
        if (shooting)
        {
            yield break;
        }

        int previousGoals = goals;
        int previousSaves = saves;
        int previousShotCount = shotCount;
        keeperTestMode = true;
        keeperTestShotIndex = 0;
        keeperTestShotTotal = 9;
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                keeperTestShotIndex++;
                SetAimGrid(col, row);
                forceKeeperTestShot = true;
                forcedKeeperCol = col;
                forcedKeeperRow = row;
                forcedKeeperSave = true;
                SetStatus("TEST " + KeeperActionName(col, row) + " " + GridName(col, row));
                Shoot();
                float shotWaitStarted = Time.realtimeSinceStartup;
                while (shooting)
                {
                    if (Time.realtimeSinceStartup - shotWaitStarted > KeeperTestShotTimeoutSeconds)
                    {
                        ResetShot();
                        SetStatus("TEST timeout " + GridName(col, row));
                        break;
                    }

                    yield return null;
                }

                yield return new WaitForSecondsRealtime(0.2f);
            }
        }

        forceKeeperTestShot = false;
        keeperTestMode = false;
        goals = previousGoals;
        saves = previousSaves;
        shotCount = previousShotCount;
        UpdateScore();
        SetStatus("Test complete");
    }

    private IEnumerator TestTopKeeperZones()
    {
        if (shooting)
        {
            yield break;
        }

        int previousGoals = goals;
        int previousSaves = saves;
        int previousShotCount = shotCount;
        keeperTestMode = true;
        keeperTestShotIndex = 0;
        keeperTestShotTotal = 3;
        for (int col = 0; col < 3; col++)
        {
            keeperTestShotIndex++;
            SetAimGrid(col, 0);
            forceKeeperTestShot = true;
            forcedKeeperCol = col;
            forcedKeeperRow = 0;
            forcedKeeperSave = true;
            SetStatus("TEST TOP " + GridName(col, 0));
            Shoot();
            float shotWaitStarted = Time.realtimeSinceStartup;
            while (shooting)
            {
                if (Time.realtimeSinceStartup - shotWaitStarted > KeeperTestShotTimeoutSeconds)
                {
                    ResetShot();
                    SetStatus("TEST TOP timeout " + GridName(col, 0));
                    break;
                }

                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.2f);
        }

        forceKeeperTestShot = false;
        keeperTestMode = false;
        goals = previousGoals;
        saves = previousSaves;
        shotCount = previousShotCount;
        UpdateScore();
        SetStatus("Top test complete");
    }

    private IEnumerator RunUp(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float stride = Mathf.Sin(t * Mathf.PI * 6.4f);
            float counterStride = -stride;
            float footSnap = Mathf.Sign(stride) * Mathf.Pow(Mathf.Abs(stride), 0.65f);
            float lean = Mathf.Lerp(7f, 15f, t);
            player.position = Vector3.Lerp(from, to, Smooth(t))
                + new Vector3(0.04f * stride, Mathf.Abs(stride) * 0.055f, 0f);
            player.rotation = Quaternion.Euler(lean, 5f * stride, -3.2f * stride);
            SetLocalPosition(strikerTorso, new Vector3(0f, 1.05f + Mathf.Abs(stride) * 0.07f, 0f));
            SetLocalPosition(strikerHead, new Vector3(0.04f * stride, 1.82f + Mathf.Abs(stride) * 0.06f, 0.02f));
            SetLocalPosition(strikerLeftLeg, new Vector3(-0.22f, 0.2f, 0.1f * stride));
            SetLocalPosition(strikerRightLeg, new Vector3(0.22f, 0.2f, -0.1f * stride));
            SetLocalPosition(strikerLeftArm, new Vector3(-0.5f, 1.08f, -0.09f * stride));
            SetLocalPosition(strikerRightArm, new Vector3(0.5f, 1.08f, 0.09f * stride));
            SetLocalRotation(strikerTorso, -lean * 0.55f, 0f, -2.5f * stride);
            SetLocalRotation(strikerHead, lean * 0.25f, -8f * Mathf.Sign(aimX), 2f * stride);
            SetLocalRotation(strikerLeftLeg, 52f * footSnap, 0f, -5f * stride);
            SetLocalRotation(strikerRightLeg, 52f * counterStride, 0f, 5f * stride);
            SetLocalRotation(strikerLeftArm, -62f * stride, 0f, 18f + 12f * stride);
            SetLocalRotation(strikerRightArm, -62f * counterStride, 0f, -18f + 12f * counterStride);
            AnimateVisibleStriker(lean, stride, 0f);
            yield return null;
        }
    }

    private IEnumerator PlantAndKick(float duration)
    {
        float elapsed = 0f;
        Quaternion from = Quaternion.identity;
        Quaternion to = Quaternion.Euler(13f, Mathf.Sign(aimX) * 7f, -Mathf.Sign(aimX) * 12f);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float windUp = Mathf.Clamp01(t / 0.42f);
            float strike = Mathf.Clamp01((t - 0.22f) / 0.42f);
            float follow = Mathf.Clamp01((t - 0.62f) / 0.38f);
            float snap = Mathf.SmoothStep(0f, 1f, strike);
            float impact = Mathf.Sin(Mathf.Clamp01(Mathf.InverseLerp(0.36f, 0.54f, t)) * Mathf.PI);
            float bodyTurn = Mathf.Sin(t * Mathf.PI);
            float aimSide = Mathf.Abs(aimX) < 0.1f ? 0f : Mathf.Sign(aimX);

            player.position = Vector3.Lerp(new Vector3(-0.35f, playerStart.y, -2.15f), new Vector3(-0.12f, playerStart.y, -2.48f), Smooth(t))
                + new Vector3(-aimSide * 0.025f * impact, -0.035f * impact, -0.035f * impact);
            player.rotation = Quaternion.Slerp(from, to, bodyTurn);
            SetLocalPosition(strikerTorso, new Vector3(0f, 1.02f - 0.04f * snap - 0.045f * impact, 0.08f * snap + 0.045f * impact));
            SetLocalPosition(strikerHead, new Vector3(aimSide * 0.04f, 1.8f - 0.025f * impact, 0.04f + 0.04f * follow + 0.035f * impact));
            SetLocalPosition(strikerRightLeg, new Vector3(0.22f + aimSide * 0.035f * impact, 0.18f - 0.035f * impact, Mathf.Lerp(-0.2f, 0.32f, snap) + 0.12f * impact));
            SetLocalPosition(strikerLeftLeg, new Vector3(-0.26f, 0.2f, Mathf.Lerp(0.1f, -0.08f, snap)));
            SetLocalPosition(strikerLeftArm, new Vector3(-0.55f, 1.1f, 0.08f + 0.08f * bodyTurn));
            SetLocalPosition(strikerRightArm, new Vector3(0.55f, 1.08f, -0.1f + 0.18f * snap - 0.04f * impact));
            SetLocalRotation(strikerTorso, Mathf.Lerp(-24f, 24f, snap) - 10f * follow - 5f * impact, aimSide * (16f * snap + 4f * impact), -aimSide * 16f * bodyTurn);
            SetLocalRotation(strikerHead, -6f + 10f * follow, aimSide * 18f, aimSide * 3f);
            SetLocalRotation(strikerRightLeg, Mathf.Lerp(-112f, 124f, snap) - 28f * follow + 18f * impact, aimSide * (10f + 8f * impact), aimSide * 8f);
            SetLocalRotation(strikerLeftLeg, Mathf.Lerp(28f, -46f, windUp) + 18f * follow, 0f, -aimSide * 12f);
            SetLocalRotation(strikerLeftArm, Mathf.Lerp(40f, 78f, bodyTurn), 0f, 34f + aimSide * 12f);
            SetLocalRotation(strikerRightArm, Mathf.Lerp(-86f, -18f, snap), 0f, -34f + aimSide * 9f);
            AnimateVisibleStriker(Mathf.Lerp(-12f, 22f, snap), bodyTurn * aimSide, follow);
            ApplyBallImpactScale(impact * 0.55f);
            if (impact > 0.82f && Time.time >= kickFlashUntil)
            {
                kickFlashWorld = ball.position + new Vector3(aimSide * 0.05f, 0.08f, -0.08f);
                kickFlashUntil = Time.time + 0.18f;
            }
            cameraRig.position = ReadyCameraPosition() + new Vector3(aimSide * impact * 0.035f, impact * 0.02f, impact * 0.05f);
            cameraRig.rotation = Quaternion.Euler(6.5f - impact * 0.8f, aimSide * impact * 0.55f, 0f);
            ball.Rotate(new Vector3(620f, 240f, aimSide * 140f) * Time.deltaTime, Space.World);
            yield return null;
        }

        ball.localScale = ballBaseScale;
    }

    private IEnumerator DiveKeeper(Vector3 to, float duration, bool saved)
    {
        if (UseAaAnimatedKeeper && keeperAnimator != null)
        {
            ClearImportedKeeperActionOffset();
            Vector3 rootFrom = keeperStart;
            Vector3 rootTo = AaKeeperRootTarget(saved);
            Quaternion rotationFrom = Quaternion.identity;
            Quaternion rotationTo = AaKeeperRootRotation(saved);
            float rootHoldT = saved ? 0.82f : 1f;
            float holdElapsed = 0f;
            while (holdElapsed < duration)
            {
                holdElapsed += Time.unscaledDeltaTime;
                float actionT = Mathf.Clamp01(holdElapsed / duration);
                float moveT = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 0.52f, Mathf.Min(actionT, rootHoldT)));
                keeper.position = Vector3.Lerp(rootFrom, rootTo, moveT);
                keeper.rotation = Quaternion.Slerp(rotationFrom, rotationTo, moveT);
                AnchorImportedKeeperVisibleModel();
                yield return null;
            }

            ClearImportedKeeperActionOffset();
            keeper.position = rootTo;
            keeper.rotation = rotationTo;
            AnchorImportedKeeperVisibleModel();
            yield break;
        }

        Vector3 from = keeper.position;
        Quaternion startRotation = keeper.rotation;
        float side = Mathf.Abs(to.x) < 0.1f ? 0f : Mathf.Sign(to.x);
        bool standingBlock = IsStandingBlockRow(keeperRow);
        bool topRow = keeperRow == 0;
        bool middleRow = keeperRow == 1;
        bool bottomRow = keeperRow == 2;
        bool centerDive = side == 0f;
        KeeperSaveProfile profile = KeeperSaveProfile.For(keeperCol, keeperRow);
        Quaternion endRotation = standingBlock ? Quaternion.Euler(-5f, side * 8f, -side * 6f) : KeeperEndRotation(side, profile.rowHeight, bottomRow, centerDive);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float coil = Mathf.Sin(Mathf.Clamp01(t / 0.12f) * Mathf.PI);
            float crouch = Mathf.Sin(Mathf.Clamp01(t / 0.18f) * Mathf.PI);
            float launch = Smooth(Mathf.Clamp01((t - 0.02f) / profile.launchDuration));
            float settle = Smooth(Mathf.Clamp01((t - 0.66f) / 0.28f));
            float hang = Mathf.Sin(Mathf.Clamp01((t - 0.02f) / 0.74f) * Mathf.PI);
            float reach = Mathf.Sin(Mathf.Clamp01(t * 2.55f) * Mathf.PI * 0.5f);
            float contactLock = 1f - Mathf.Abs(Mathf.Clamp01((t - 0.32f) / 0.14f) * 2f - 1f);
            float palmPunch = Mathf.Sin(Mathf.Clamp01((t - 0.39f) / 0.18f) * Mathf.PI);
            float gloveReach = Smooth(Mathf.Clamp01(t / 0.22f));
            float gloveSnap = Smooth(Mathf.Clamp01((t - 0.4f) / 0.1f));
            float push = launch;
            if (standingBlock)
            {
                float move = Smooth(Mathf.Clamp01(t / 0.24f));
                float jumpAmount = topRow ? 1f : middleRow ? 0.45f : 0.12f;
                float jump = Mathf.Sin(Mathf.Clamp01(t / 0.82f) * Mathf.PI) * jumpAmount;
                float handReach = Smooth(Mathf.Clamp01(t / 0.22f));
                float headSnap = Mathf.Sin(Mathf.Clamp01((t - 0.18f) / 0.24f) * Mathf.PI);
                Vector3 topPosition = Vector3.Lerp(from, to, move);
                topPosition.x = Mathf.Clamp(topPosition.x, -0.52f, 0.52f);
                topPosition.y = keeperStart.y + (topRow ? 0.58f : middleRow ? 0.18f : 0.04f) * jump;
                topPosition.z = keeperStart.z - 0.04f * move;
                keeper.position = topPosition;
                keeper.rotation = Quaternion.Euler(-9f * jump - 12f * headSnap, side * 5f * move, -side * 3f * jump);
                Vector3 topContactWorld = StandingBlockContactWorld();
                Vector3 leftReady = keeper.position + keeper.rotation * new Vector3(-0.34f, 1.18f, -0.13f);
                Vector3 rightReady = keeper.position + keeper.rotation * new Vector3(0.34f, 1.18f, -0.13f);
                Vector3 leftContact = topContactWorld + new Vector3(-0.2f, topRow ? -0.08f : 0.01f, 0.02f);
                Vector3 rightContact = topContactWorld + new Vector3(0.2f, topRow ? -0.08f : 0.01f, 0.02f);
                SetGloveWorld(keeperLeftGlove, Vector3.Lerp(leftReady, leftContact, handReach), profile.gloveScale + 0.08f * jump);
                SetGloveWorld(keeperRightGlove, Vector3.Lerp(rightReady, rightContact, handReach), profile.gloveScale + 0.08f * jump);
                SetLocalPosition(keeperTorso, new Vector3(-side * 0.04f * headSnap, 1.14f + 0.1f * jump, 0.02f));
                SetLocalPosition(keeperHead, new Vector3(side * 0.03f, 1.9f + 0.08f * jump, -0.04f - 0.08f * headSnap));
                SetLocalRotation(keeperTorso, -10f * jump, side * 10f * move, -side * 4f * jump);
                SetLocalRotation(keeperHead, -18f * headSnap, side * 12f * move, 0f);
                SetLocalPosition(keeperLeftArm, new Vector3(-0.42f, 1.28f + 0.42f * handReach, -0.06f - 0.08f * headSnap));
                SetLocalPosition(keeperRightArm, new Vector3(0.42f, 1.28f + 0.42f * handReach, -0.06f - 0.08f * headSnap));
                SetLocalRotation(keeperLeftArm, -152f * handReach, 0f, 28f);
                SetLocalRotation(keeperRightArm, -152f * handReach, 0f, -28f);
                if (keeperVisibleModel != null)
                {
                    keeperVisibleModel.localPosition = new Vector3(0f, 0.03f + 0.04f * jump, -0.02f);
                    keeperVisibleModel.localRotation = Quaternion.Euler(-10f * jump - 16f * headSnap, 180f + side * 3f, -side * 2f * jump);
                }
                AnimateKeeperSprite(t, true, false, false, side);
                yield return null;
                continue;
            }

            Vector3 travel = Vector3.Lerp(from, to, launch);
            Vector3 divePosition = travel + new Vector3(side * profile.airSideDrift * hang - side * 0.08f * coil, hang * profile.lift - crouch * 0.16f - profile.lowDive * 0.22f * launch, profile.forwardDrive * launch - 0.16f * settle);
            divePosition.x = Mathf.Clamp(divePosition.x, -2.18f, 2.18f);
            divePosition.y = Mathf.Clamp(divePosition.y, keeperStart.y - 0.12f, keeperStart.y + 1.42f);
            keeper.position = divePosition;
            keeper.rotation = Quaternion.Slerp(startRotation, endRotation, push);
            bool leftDive = side < 0f;
            bool rightDive = side > 0f;
            float activeLeft = leftDive || centerDive ? 1f : 0f;
            float activeRight = rightDive || centerDive ? 1f : 0f;
            Vector3 palmWorld = SavePalmWorld();

            SetLocalPosition(keeperTorso, new Vector3(side * profile.torsoSide * reach - side * 0.08f * coil, profile.torsoY + 0.1f * hang - 0.1f * crouch, 0.04f - profile.lowDive * 0.18f * push));
            SetLocalPosition(keeperHead, new Vector3(side * profile.headSide * reach, profile.headY + profile.headLift * hang, 0.14f - profile.lowDive * 0.08f * push));
            SetLocalPosition(keeperLeftArm, new Vector3(-0.5f - (leftDive ? profile.sideReach : 0.14f) * reach + (centerDive ? profile.centerHandSpread * reach : 0f), 1.08f + (leftDive || centerDive ? profile.activeArmY : profile.passiveArmY) * reach + 0.16f * palmPunch * activeLeft, -0.02f - 0.22f * palmPunch * activeLeft - profile.lowDive * 0.14f * reach));
            SetLocalPosition(keeperRightArm, new Vector3(0.5f + (rightDive ? profile.sideReach : 0.14f) * reach - (centerDive ? profile.centerHandSpread * reach : 0f), 1.08f + (rightDive || centerDive ? profile.activeArmY : profile.passiveArmY) * reach + 0.16f * palmPunch * activeRight, -0.02f - 0.22f * palmPunch * activeRight - profile.lowDive * 0.14f * reach));
            Vector3 glovePunchOffset = new Vector3(saveReboundSide * profile.punchSide * gloveSnap, profile.punchUp * palmPunch, -profile.punchForward * gloveSnap);
            if (centerDive)
            {
                Vector3 leftReady = keeper.position + keeper.rotation * new Vector3(-0.48f, 1.08f, -0.18f);
                Vector3 rightReady = keeper.position + keeper.rotation * new Vector3(0.48f, 1.08f, -0.18f);
                float spread = profile.contactSpread;
                Vector3 leftContact = palmWorld + new Vector3(-spread, profile.contactYOffset, 0f);
                Vector3 rightContact = palmWorld + new Vector3(spread, profile.contactYOffset, 0f);
                SetGloveWorld(keeperLeftGlove, Vector3.Lerp(leftReady, Vector3.Lerp(leftContact, leftContact + glovePunchOffset, gloveSnap), gloveReach), profile.gloveScale + 0.12f * Mathf.Max(contactLock, palmPunch));
                SetGloveWorld(keeperRightGlove, Vector3.Lerp(rightReady, Vector3.Lerp(rightContact, rightContact + glovePunchOffset, gloveSnap), gloveReach), profile.gloveScale + 0.12f * Mathf.Max(contactLock, palmPunch));
            }
            else if (leftDive)
            {
                Vector3 leftReady = keeper.position + keeper.rotation * new Vector3(-0.48f, 1.08f, -0.18f);
                Vector3 rightTrail = keeper.position + keeper.rotation * new Vector3(0.18f, 1.2f, -0.12f);
                SetGloveWorld(keeperLeftGlove, Vector3.Lerp(leftReady, Vector3.Lerp(palmWorld, palmWorld + glovePunchOffset, gloveSnap), gloveReach), profile.gloveScale + 0.12f * Mathf.Max(contactLock, palmPunch));
                SetGloveWorld(keeperRightGlove, rightTrail, 0.24f);
            }
            else
            {
                Vector3 rightReady = keeper.position + keeper.rotation * new Vector3(0.48f, 1.08f, -0.18f);
                Vector3 leftTrail = keeper.position + keeper.rotation * new Vector3(-0.18f, 1.2f, -0.12f);
                SetGloveWorld(keeperRightGlove, Vector3.Lerp(rightReady, Vector3.Lerp(palmWorld, palmWorld + glovePunchOffset, gloveSnap), gloveReach), profile.gloveScale + 0.12f * Mathf.Max(contactLock, palmPunch));
                SetGloveWorld(keeperLeftGlove, leftTrail, 0.24f);
            }
            SetLocalPosition(keeperLeftLeg, new Vector3(-0.24f - side * 0.1f * reach, 0.18f - 0.05f * crouch - profile.lowDive * 0.12f * push, -0.12f * push - profile.lowDive * 0.12f));
            SetLocalPosition(keeperRightLeg, new Vector3(0.24f - side * 0.1f * reach, 0.18f - 0.05f * crouch - profile.lowDive * 0.12f * push, 0.12f * push - profile.lowDive * 0.12f));
            SetLocalRotation(keeperTorso, (topRow ? -58f : middleRow ? -28f : 32f) * launch - 8f * coil, side * (topRow ? 30f : 42f) * reach, -side * (topRow ? 58f : middleRow ? 88f : 108f) * reach + side * 18f * coil);
            SetLocalRotation(keeperHead, topRow ? -18f : middleRow ? -6f : 20f, side * (topRow ? 16f : 32f), side * (topRow ? 6f : 14f));
            SetLocalRotation(keeperLeftArm, KeeperArmPitch(leftDive || centerDive, profile.rowHeight) * reach - 58f * palmPunch * activeLeft, 0f, (leftDive ? (topRow ? 138f : middleRow ? 166f : 178f) : centerDive ? (topRow ? 44f : middleRow ? 68f : 26f) : 26f) * reach + 26f * palmPunch * activeLeft);
            SetLocalRotation(keeperRightArm, KeeperArmPitch(rightDive || centerDive, profile.rowHeight) * reach - 58f * palmPunch * activeRight, 0f, (rightDive ? (topRow ? -138f : middleRow ? -166f : -178f) : centerDive ? (topRow ? -44f : middleRow ? -68f : -26f) : -26f) * reach - 26f * palmPunch * activeRight);
            SetLocalRotation(keeperLeftLeg, (topRow ? 42f : middleRow ? 62f : 82f) * push, 0f, (-44f - side * (middleRow ? 26f : 12f)) * push);
            SetLocalRotation(keeperRightLeg, (topRow ? -30f : middleRow ? -44f : -64f) * push, 0f, (44f - side * (middleRow ? 26f : 12f)) * push);
            AnimateKeeperSprite(t, topRow, middleRow, bottomRow, side);
            AnimateVisibleKeeper((topRow ? -28f : bottomRow ? 22f : -12f) * reach - 6f * coil, -side * (topRow ? 48f : middleRow ? 68f : 86f) * reach + side * 14f * coil, reach);
            yield return null;
        }

        if (standingBlock)
        {
            keeper.position = keeperStart;
            keeper.rotation = Quaternion.identity;
            ResetPose();
        }
    }

    private IEnumerator ReturnKeeperToCenter(float duration)
    {
        Vector3 from = keeper.position;
        Quaternion rotationFrom = keeper.rotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Smooth(Mathf.Clamp01(elapsed / duration));
            keeper.position = Vector3.Lerp(from, keeperStart, t);
            keeper.rotation = Quaternion.Slerp(rotationFrom, Quaternion.identity, t);
            BlendKeeperPoseToReady(t);
            yield return null;
        }

        keeper.position = keeperStart;
        keeper.rotation = Quaternion.identity;
        ResetPose();
    }

    private IEnumerator ReturnAllToReady(float duration)
    {
        Vector3 ballFrom = ball.position;
        Vector3 ballScaleFrom = ball.localScale;
        Quaternion ballRotationFrom = ball.rotation;
        Vector3 playerFrom = player.position;
        Quaternion playerRotationFrom = player.rotation;
        Vector3 keeperFrom = keeper.position;
        Quaternion keeperRotationFrom = keeper.rotation;
        Vector3 importedOffsetFrom = importedKeeperActionOffsetLocal;
        Quaternion importedRotationFrom = importedKeeperActionRotationOffset;
        Vector3 cameraFrom = cameraRig.position;
        Quaternion cameraRotationFrom = cameraRig.rotation;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Smooth(Mathf.Clamp01(elapsed / duration));
            ball.position = Vector3.Lerp(ballFrom, ballStart, t);
            ball.rotation = Quaternion.Slerp(ballRotationFrom, Quaternion.identity, t);
            ball.localScale = Vector3.Lerp(ballScaleFrom, ballBaseScale, t);
            UpdateBallShadow();
            player.position = Vector3.Lerp(playerFrom, playerStart, t);
            player.rotation = Quaternion.Slerp(playerRotationFrom, Quaternion.identity, t);
            keeper.position = Vector3.Lerp(keeperFrom, keeperStart, t);
            keeper.rotation = Quaternion.Slerp(keeperRotationFrom, Quaternion.identity, t);
            importedKeeperActionOffsetLocal = Vector3.Lerp(importedOffsetFrom, Vector3.zero, t);
            importedKeeperActionRotationOffset = Quaternion.Slerp(importedRotationFrom, Quaternion.identity, t);
            AnchorImportedKeeperVisibleModel();
            cameraRig.position = Vector3.Lerp(cameraFrom, ReadyCameraPosition(), t);
            cameraRig.rotation = Quaternion.Slerp(cameraRotationFrom, ReadyCameraRotation(), t);
            BlendKeeperPoseToReady(t);
            yield return null;
        }

        ball.position = ballStart;
        ball.rotation = Quaternion.identity;
        ball.localScale = ballBaseScale;
        UpdateBallShadow();
        if (ballTrail != null)
        {
            ballTrail.Clear();
        }
        HideSaveImpactFlash();
        HideSaveShockwave();
        HideSaveContactStreak();
        HideResultGoalFlash();
        HideGoalNetImpact();

        player.position = playerStart;
        player.rotation = Quaternion.identity;
        keeper.position = keeperStart;
        keeper.rotation = Quaternion.identity;
        ClearImportedKeeperActionOffset();
        cameraRig.position = ReadyCameraPosition();
        cameraRig.rotation = ReadyCameraRotation();
        ResetPose();
    }

    private IEnumerator ResultCameraPunch(bool saved, float reboundSide, float duration)
    {
        if (cameraRig == null)
        {
            yield break;
        }

        Vector3 from = cameraRig.position;
        Quaternion rotationFrom = cameraRig.rotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float punch = Mathf.Sin(t * Mathf.PI);
            float settle = Mathf.SmoothStep(0f, 1f, t);
            float shake = Mathf.Sin(t * Mathf.PI * 18f) * punch * (saved ? 0.026f : 0.018f);
            float side = saved ? reboundSide : Mathf.Sign(Mathf.Abs(aimX) < 0.1f ? reboundSide : aimX);

            Vector3 offset = new Vector3(
                side * (shake + punch * (saved ? 0.1f : 0.06f)),
                punch * (saved ? 0.045f : 0.075f),
                punch * (saved ? 0.22f : 0.36f));
            cameraRig.position = from + offset;
            cameraRig.rotation = rotationFrom * Quaternion.Euler(
                punch * (saved ? -1.15f : -1.75f),
                side * punch * (saved ? 1.45f : 0.85f),
                side * punch * (saved ? 0.85f : 0.38f));

            if (settle > 0.86f)
            {
                float restore = Mathf.InverseLerp(0.86f, 1f, settle);
                cameraRig.position = Vector3.Lerp(cameraRig.position, from, restore);
                cameraRig.rotation = Quaternion.Slerp(cameraRig.rotation, rotationFrom, restore);
            }

            yield return null;
        }
    }

    private void ForceReadyReset()
    {
        ball.position = ballStart;
        ball.rotation = Quaternion.identity;
        ball.localScale = ballBaseScale;
        UpdateBallShadow();
        if (ballTrail != null)
        {
            ballTrail.Clear();
        }
        HideSaveImpactFlash();
        HideSaveShockwave();
        HideSaveContactStreak();
        HideResultGoalFlash();

        player.position = playerStart;
        player.rotation = Quaternion.identity;
        keeper.position = keeperStart;
        keeper.rotation = Quaternion.identity;
        cameraRig.position = ReadyCameraPosition();
        cameraRig.rotation = ReadyCameraRotation();
        ResetPose();
    }

    private IEnumerator FlyBall(Vector3 from, Vector3 to, bool saved, float duration)
    {
        float elapsed = 0f;
        float reboundSide = saveReboundSide;
        bool standingBlockSave = saved && !UseAaAnimatedKeeper && IsStandingBlockRow(keeperRow);
        Vector3 contact = UseAaAnimatedKeeper ? AaKeeperContactWorld() : standingBlockSave ? StandingBlockContactWorld() : SavePalmWorld();
        float contactTime = UseAaAnimatedKeeper ? AaContactTime() : 0.32f;
        float loadTime = Mathf.Clamp01(contactTime + (UseAaAnimatedKeeper ? 0.035f : 0.06f));
        float punchTime = Mathf.Clamp01(loadTime + (UseAaAnimatedKeeper ? AaPunchWindow() * 0.86f : 0.24f));
        if (UseAaAnimatedKeeper && keeperRow == 0)
        {
            contact += new Vector3(0f, 0.14f, -0.08f);
            loadTime = Mathf.Clamp01(contactTime + 0.08f);
            punchTime = Mathf.Clamp01(loadTime + 0.16f);
        }
        Vector3 palmLoad = contact + (standingBlockSave ? new Vector3(0f, -0.05f, -0.02f) : AaPalmLoadOffset());
        Vector3 deflect = standingBlockSave
            ? new Vector3(
                Mathf.Clamp(contact.x + reboundSide * UnityEngine.Random.Range(0.65f, 1.1f), -1.65f, 1.65f),
                Mathf.Clamp(contact.y + UnityEngine.Random.Range(0.74f, 1.08f), 1.55f, 3.35f),
                UnityEngine.Random.Range(-3.05f, -2.45f))
            : UseAaAnimatedKeeper
            ? AaDeflectWorld(contact, reboundSide)
            : new Vector3(
                Mathf.Clamp(contact.x + reboundSide * UnityEngine.Random.Range(2.35f, 3.35f), -3.7f, 3.7f),
                Mathf.Clamp(contact.y + UnityEngine.Random.Range(0.62f, 1.3f), 1.05f, 3.35f),
                UnityEngine.Random.Range(-3.45f, -2.35f));
        Vector3 drop = new Vector3(
            Mathf.Clamp(deflect.x + reboundSide * UnityEngine.Random.Range(keeperRow == 0 ? 0.7f : 0.38f, keeperRow == 0 ? 1.25f : 0.95f), -3.8f, 3.8f),
            keeperRow == 0 ? 0.24f : 0.28f,
            UnityEngine.Random.Range(keeperRow == 0 ? -3.65f : -3.25f, keeperRow == 0 ? -2.85f : -2.45f));
        float lastT = 0f;
        shotSpeedLineStartedAt = Time.time;
        shotSpeedLineUntil = Time.time + 0.56f;
        shotSpeedLineSide = Mathf.Abs(to.x) < 0.1f ? 1f : Mathf.Sign(to.x);
        bool saveDustTriggered = false;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (saved && t >= contactTime && lastT < contactTime)
            {
                saveContactSparkWorld = contact;
                saveContactSparkStartedAt = Time.time;
                saveContactSparkUntil = Time.time + 0.34f;
                yield return null;
            }
            lastT = t;
            Vector3 position;
            float side = Mathf.Abs(to.x) < 0.1f ? 0f : Mathf.Sign(to.x);
            if (saved)
            {
                if (t < contactTime)
                {
                    float inT = t / contactTime;
                    float shotT = EaseOut(inT, UseAaAnimatedKeeper ? 3.05f : 2.2f);
                    position = Vector3.Lerp(from, contact, shotT);
                    position.y += Mathf.Sin(inT * Mathf.PI) * AaShotArcHeight(saved);
                }
                else if (t < loadTime)
                {
                    float loadT = (t - contactTime) / (loadTime - contactTime);
                    position = Vector3.Lerp(contact, palmLoad, Smooth(loadT));
                    position.x += Mathf.Sin(loadT * Mathf.PI) * reboundSide * 0.035f;
                }
                else if (t < punchTime)
                {
                    float outT = (t - loadTime) / (punchTime - loadTime);
                    position = Vector3.Lerp(palmLoad, deflect, EaseOut(outT, keeperRow == 0 ? 3.55f : 2.85f));
                    position.y += Mathf.Sin(outT * Mathf.PI) * (UseAaAnimatedKeeper ? AaDeflectArcHeight() : 1.36f);
                }
                else
                {
                    float fallT = (t - punchTime) / (1f - punchTime);
                    position = Vector3.Lerp(deflect, drop, EaseOut(fallT, keeperRow == 0 ? 1.45f : 1.18f));
                    position.y += Mathf.Sin(fallT * Mathf.PI) * (keeperRow == 0 ? 0.12f : 0.22f);
                    if (!saveDustTriggered && fallT > 0.56f)
                    {
                        saveDustTriggered = true;
                        saveDustWorld = new Vector3(position.x, 0.12f, position.z);
                        saveDustSide = reboundSide;
                        saveDustStartedAt = Time.time;
                        saveDustUntil = Time.time + 0.58f;
                    }
                }
            }
            else
            {
                float shotT = EaseOut(t, 2.15f);
                position = Vector3.Lerp(from, to, shotT);
                position.x += Mathf.Sin(t * Mathf.PI) * side * 0.32f;
                position.y += Mathf.Sin(t * Mathf.PI) * 1.36f;
            }

            ball.position = position;
            UpdateBallShadow();
            float contactFlash = saved ? Mathf.Sin(Mathf.Clamp01(Mathf.InverseLerp(contactTime - 0.025f, punchTime, t)) * Mathf.PI) : 0f;
            float impactBurst = saved ? Mathf.Sin(Mathf.Clamp01(Mathf.InverseLerp(contactTime - 0.015f, contactTime + 0.1f, t)) * Mathf.PI) : 0f;
            float presentationImpact = Mathf.Max(contactFlash, impactBurst);
            ApplyBallImpactScale(presentationImpact);
            UpdateSaveImpactFlash(saved ? presentationImpact : 0f, contact);
            UpdateSaveShockwave(saved ? impactBurst : 0f, contact);
            UpdateSaveContactStreak(saved ? presentationImpact : 0f, contact, reboundSide);
            float goalNetImpact = saved ? 0f : Mathf.Sin(Mathf.Clamp01(Mathf.InverseLerp(0.72f, 0.98f, t)) * Mathf.PI);
            UpdateGoalNetImpact(goalNetImpact, new Vector3(position.x, position.y, 4.92f), side);
            float saveSpinBoost = keeperRow == 0 ? 1.22f : 1f;
            ball.Rotate(new Vector3(saved ? 1760f * saveSpinBoost : 920f, saved ? -720f * saveSpinBoost : 260f, side * 220f + reboundSide * (saved ? 620f * saveSpinBoost : 0f)) * Time.unscaledDeltaTime, Space.World);
            Vector3 cameraBase = ShotCameraPosition(t, presentationImpact, saved, reboundSide);
            float saveHitShake = saved ? presentationImpact * (keeperRow == 0 ? 0.2f : 0.16f) : 0f;
            float shake = Mathf.Sin(t * Mathf.PI * 30f) * Mathf.Sin(t * Mathf.PI) * 0.04f + saveHitShake;
            cameraRig.position = cameraBase + new Vector3(shake * reboundSide, shake * 0.45f, saved ? presentationImpact * 0.13f : 0f);
            cameraRig.rotation = ShotCameraRotation(t, presentationImpact, reboundSide);
            yield return null;
        }

        HideSaveImpactFlash();
        HideSaveShockwave();
        HideSaveContactStreak();
        ball.localScale = ballBaseScale;
        HideGoalNetImpact();
        UpdateBallShadow();
    }

    private void CacheBodyParts()
    {
        strikerLeftLeg = player.Find("BM8 Striker Left Leg");
        strikerRightLeg = player.Find("BM8 Striker Right Leg");
        strikerLeftArm = player.Find("BM8 Striker Left Arm");
        strikerRightArm = player.Find("BM8 Striker Right Arm");
        strikerTorso = player.Find("BM8 Striker Torso");
        strikerHead = player.Find("BM8 Striker Head");
        strikerVisibleModel = player.Find("Visible FBX Character");
        keeperVisibleModel = FindKeeperVisibleModel();
        if (keeperAnimator == null && keeperVisibleModel != null)
        {
            keeperAnimator = keeperVisibleModel.GetComponentInChildren<Animator>();
        }
        keeperLeftArm = keeper.Find("Keeper Left Arm");
        keeperRightArm = keeper.Find("Keeper Right Arm");
        keeperLeftLeg = keeper.Find("Keeper Left Leg");
        keeperRightLeg = keeper.Find("Keeper Right Leg");
        keeperTorso = keeper.Find("Keeper Torso");
        keeperHead = keeper.Find("Keeper Head");
    }

    private void RemoveStrikerArmNumbers()
    {
        RemoveArmNumberDecals(strikerLeftArm);
        RemoveArmNumberDecals(strikerRightArm);
    }

    private Transform FindKeeperVisibleModel()
    {
        Transform stylized = keeper.Find("Visible FBX Keeper Stylized");
        if (stylized != null)
        {
            return stylized;
        }

        Transform visible = keeper.Find("Visible FBX Keeper");
        if (visible != null)
        {
            return visible;
        }

        return keeper.Find("Visible Photo Keeper");
    }

    private void DisableImportedRobotKeeper()
    {
        if (keeperVisibleModel == null)
        {
            return;
        }

        keeperVisibleModel.gameObject.SetActive(false);
        keeperAnimator = null;
        keeperVisibleModel = null;
    }

    private void EnsureImportedKeeperActive()
    {
        if (keeperVisibleModel == null)
        {
            keeperVisibleModel = FindKeeperVisibleModel();
        }

        if (keeperVisibleModel == null)
        {
            return;
        }

        keeperVisibleModel.gameObject.SetActive(true);
        keeperVisibleModel.localPosition = new Vector3(0f, 0f, -0.02f);
        keeperVisibleModel.localRotation = Quaternion.Euler(0f, 180f, 0f);
        keeperVisibleModel.localScale = Vector3.one * 1.05f;
        FitImportedKeeperToGoal();
        CaptureImportedKeeperAnchor();
        AnchorImportedKeeperVisibleModel();
        ApplyBm8KeeperKit();

        keeperAnimator = keeperVisibleModel.GetComponentInChildren<Animator>(true);
        if (keeperAnimator != null)
        {
            keeperAnimator.applyRootMotion = false;
            keeperAnimator.enabled = true;
            PlayKeeperController(keeperIdleController);
        }
    }

    private void EnsureUploadedStylizedKeeperInEditor()
    {
#if UNITY_EDITOR
        Transform currentVisible = FindKeeperVisibleModel();
        bool alreadyStylized = currentVisible != null && currentVisible.name.Contains("Stylized");
        if (alreadyStylized)
        {
            keeperVisibleModel = currentVisible;
            return;
        }

        GameObject stylizedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UploadedStylizedKeeperPath);
        if (stylizedPrefab == null)
        {
            keeperVisibleModel = currentVisible;
            return;
        }

        DestroyKeeperVisibleModels();

        GameObject visible = (GameObject)PrefabUtility.InstantiatePrefab(stylizedPrefab);
        if (visible == null)
        {
            visible = Instantiate(stylizedPrefab);
        }

        visible.name = "Visible FBX Keeper Stylized";
        visible.transform.SetParent(keeper, false);
        visible.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        visible.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        visible.transform.localScale = Vector3.one * 1.05f;

        keeperVisibleModel = visible.transform;
        CaptureImportedKeeperAnchor();
        ApplyBm8KeeperKit();
        keeperAnimator = visible.GetComponentInChildren<Animator>(true);
        if (keeperAnimator == null)
        {
            keeperAnimator = visible.AddComponent<Animator>();
        }
#endif
    }

    private void DestroyKeeperVisibleModels()
    {
        string[] visibleNames =
        {
            "Visible FBX Keeper",
            "Visible FBX Keeper Stylized",
            "Visible Photo Keeper"
        };

        for (int i = 0; i < visibleNames.Length; i++)
        {
            Transform child = keeper.Find(visibleNames[i]);
            if (child == null)
            {
                continue;
            }

#if UNITY_EDITOR
            DestroyImmediate(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }
    }

    private void FitImportedKeeperToGoal()
    {
        if (keeperVisibleModel == null)
        {
            return;
        }

        Bounds bounds;
        if (!TryGetVisibleBounds(keeperVisibleModel, out bounds) || bounds.size.y < 0.001f)
        {
            return;
        }

        const float targetHeight = 2.12f;
        float scale = targetHeight / bounds.size.y;
        keeperVisibleModel.localScale *= scale;

        if (TryGetVisibleBounds(keeperVisibleModel, out bounds))
        {
            Vector3 lift = new Vector3(0f, 0.02f - bounds.min.y, 0f);
            keeperVisibleModel.position += lift;
        }
    }

    private void ApplyBm8KeeperKit()
    {
        if (keeperVisibleModel == null)
        {
            return;
        }

        RemoveKeeperKitPanels();

#if UNITY_EDITOR
        Texture2D bm8Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Bm8KeeperBaseTexturePath);
        if (bm8Texture != null)
        {
            foreach (Renderer renderer in keeperVisibleModel.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null)
                    {
                        continue;
                    }

                    material.mainTexture = bm8Texture;
                    material.color = Color.white;
                    if (material.HasProperty("_BaseMap"))
                    {
                        material.SetTexture("_BaseMap", bm8Texture);
                    }
                    if (material.HasProperty("_BaseColor"))
                    {
                        material.SetColor("_BaseColor", Color.white);
                    }
                }
            }
        }
#endif
    }

    private void CaptureImportedKeeperAnchor()
    {
        if (keeperVisibleModel == null)
        {
            return;
        }

        importedKeeperAnchorLocalPosition = keeperVisibleModel.localPosition;
        importedKeeperAnchorLocalRotation = keeperVisibleModel.localRotation;
        importedKeeperAnchorLocalScale = keeperVisibleModel.localScale;

        Bounds bounds;
        if (TryGetVisibleBounds(keeperVisibleModel, out bounds))
        {
            importedKeeperReadyBoundsCenterLocal = keeper.InverseTransformPoint(bounds.center);
            importedKeeperBoundsCaptured = true;
        }
    }

    private void AnchorImportedKeeperVisibleModel()
    {
        if (keeperVisibleModel == null)
        {
            return;
        }

        Vector3 readyMotion = !shooting ? ImportedKeeperReadyMotionOffset() : Vector3.zero;
        Quaternion readyRotation = !shooting ? ImportedKeeperReadyMotionRotation() : Quaternion.identity;
        keeperVisibleModel.localPosition = importedKeeperAnchorLocalPosition + importedKeeperActionOffsetLocal + readyMotion;
        keeperVisibleModel.localRotation = importedKeeperAnchorLocalRotation * importedKeeperActionRotationOffset * readyRotation;
        keeperVisibleModel.localScale = importedKeeperAnchorLocalScale;
        ClampImportedKeeperVisibleBounds();
    }

    private static Vector3 ImportedKeeperReadyMotionOffset()
    {
        float breathe = Mathf.Sin(Time.time * 2.4f);
        float weight = Mathf.Sin(Time.time * 1.35f);
        return new Vector3(weight * 0.025f, Mathf.Abs(breathe) * 0.018f, breathe * 0.008f);
    }

    private static Quaternion ImportedKeeperReadyMotionRotation()
    {
        float weight = Mathf.Sin(Time.time * 1.35f);
        float breathe = Mathf.Sin(Time.time * 2.4f);
        return Quaternion.Euler(breathe * 0.65f, weight * 0.35f, -weight * 0.9f);
    }

    private void ClearImportedKeeperActionOffset()
    {
        importedKeeperActionOffsetLocal = Vector3.zero;
        importedKeeperActionRotationOffset = Quaternion.identity;
        importedKeeperActionT = 0f;
    }

    private void ApplyImportedKeeperActionOffset(float t)
    {
        importedKeeperActionT = Mathf.Clamp01(t);
        float side = keeperCol == 0 ? -1f : keeperCol == 2 ? 1f : 0f;
        float visualSide = side;
        bool top = keeperRow == 0;
        bool middle = keeperRow == 1;
        bool bottom = keeperRow == 2;

        float coil = Mathf.Sin(Mathf.Clamp01(Mathf.InverseLerp(0f, 0.2f, t)) * Mathf.PI);
        float launch = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.1f, 0.43f, t));
        float recover = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.66f, 1f, t));
        float hold = launch * (1f - recover);
        float snap = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
        float contactPunch = Mathf.Sin(Mathf.Clamp01(Mathf.InverseLerp(0.34f, 0.62f, t)) * Mathf.PI);
        float readStep = Smooth(Mathf.Clamp01(Mathf.InverseLerp(0f, 0.18f, t)));
        float anticipation = readStep * (1f - Smooth(Mathf.Clamp01(Mathf.InverseLerp(0.2f, 0.38f, t))));
        float readDrop = Mathf.Sin(Mathf.Clamp01(Mathf.InverseLerp(0.02f, 0.24f, t)) * Mathf.PI) * (1f - Smooth(Mathf.Clamp01(Mathf.InverseLerp(0.22f, 0.42f, t))));
        if (top)
        {
            coil = Mathf.Sin(Mathf.Clamp01(Mathf.InverseLerp(0f, 0.18f, t)) * Mathf.PI);
            launch = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 0.34f, t));
            recover = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.76f, 1f, t));
            hold = launch * (1f - recover);
            contactPunch = Mathf.Sin(Mathf.Clamp01(Mathf.InverseLerp(0.34f, 0.5f, t)) * Mathf.PI);
            anticipation = readStep * (1f - Smooth(Mathf.Clamp01(Mathf.InverseLerp(0.18f, 0.34f, t))));
            readDrop = Mathf.Sin(Mathf.Clamp01(Mathf.InverseLerp(0.01f, 0.2f, t)) * Mathf.PI) * (1f - Smooth(Mathf.Clamp01(Mathf.InverseLerp(0.2f, 0.34f, t))));
        }

        float x = -visualSide * 0.22f * anticipation - visualSide * 0.06f * readDrop - visualSide * 0.12f * coil + visualSide * (top ? 0.78f : middle ? 1.02f : 1.18f) * hold + visualSide * 0.1f * contactPunch;
        float y = -0.26f * anticipation - 0.16f * readDrop - 0.14f * coil + (top ? 0.36f : middle ? 0.16f : -0.34f) * hold + 0.08f * snap;
        float z = 0.12f * anticipation + 0.04f * readDrop - 0.08f * coil + (top ? -0.1f : middle ? -0.3f : -0.56f) * hold - 0.1f * contactPunch;
        if (top)
        {
            x = -visualSide * 0.1f * anticipation - visualSide * 0.045f * readDrop - visualSide * 0.05f * coil + visualSide * 0.52f * hold + visualSide * 0.035f * contactPunch;
            y = -0.2f * anticipation - 0.14f * readDrop - 0.08f * coil + 0.72f * hold + 0.05f * snap;
            z = 0.08f * anticipation + 0.03f * readDrop - 0.05f * coil - 0.14f * hold - 0.045f * contactPunch;
        }
        if (side == 0f)
        {
            x = 0f;
            y = -0.24f * anticipation - 0.16f * readDrop - 0.12f * coil + (top ? 0.68f : middle ? 0.12f : -0.18f) * hold + 0.07f * snap;
            z = 0.1f * anticipation + 0.04f * readDrop - 0.1f * coil + (top ? -0.18f : middle ? -0.48f : -0.56f) * hold - 0.1f * contactPunch;
        }

        importedKeeperActionOffsetLocal = new Vector3(x, y, z);
        float pitch = 14f * anticipation + 10f * readDrop - 7f * coil + (top ? -8f : bottom ? 20f : -8f) * hold + (bottom ? 10f : -4f) * contactPunch;
        float roll = side == 0f ? 0f : visualSide * 9f * anticipation + visualSide * 5f * readDrop - visualSide * ((top ? 10f : middle ? 38f : 48f) * hold + (top ? 3f : 8f) * contactPunch);
        if (top)
        {
            pitch = 10f * anticipation + 8f * readDrop - 5f * coil - 8f * hold - 3f * contactPunch;
            roll = side == 0f ? 0f : visualSide * 5f * anticipation + visualSide * 3f * readDrop - visualSide * (3.5f * hold + 1.5f * contactPunch);
        }
        importedKeeperActionRotationOffset = Quaternion.Euler(pitch, 0f, roll);
    }

    private void PoseImportedKeeperTopTipHands()
    {
        if (!shooting || keeperRow != 0 || keeperAnimator == null || !keeperAnimator.isHuman)
        {
            return;
        }

        Transform leftHand = keeperAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
        Transform rightHand = keeperAnimator.GetBoneTransform(HumanBodyBones.RightHand);
        if (leftHand == null || rightHand == null)
        {
            return;
        }

        float reachIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.1f, 0.42f, importedKeeperActionT));
        float reachOut = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.78f, 1f, importedKeeperActionT));
        float reach = reachIn * (1f - reachOut);
        if (reach <= 0.001f)
        {
            return;
        }

        float side = keeperCol == 0 ? -1f : keeperCol == 2 ? 1f : 0f;
        Vector3 contact = AaKeeperContactWorld() + new Vector3(0f, 0.16f, -0.08f);
        Vector3 leftTarget = contact + new Vector3(side < 0f ? -0.04f : -0.22f, side < 0f ? 0.02f : -0.06f, -0.02f);
        Vector3 rightTarget = contact + new Vector3(side > 0f ? 0.04f : 0.22f, side > 0f ? 0.02f : -0.06f, -0.02f);
        if (side == 0f)
        {
            leftTarget = contact + new Vector3(-0.16f, 0f, -0.02f);
            rightTarget = contact + new Vector3(0.16f, 0f, -0.02f);
        }

        leftHand.position = Vector3.Lerp(leftHand.position, leftTarget, reach);
        rightHand.position = Vector3.Lerp(rightHand.position, rightTarget, reach);
        Vector3 leftAim = contact - leftHand.position;
        Vector3 rightAim = contact - rightHand.position;
        if (leftAim.sqrMagnitude > 0.0001f)
        {
            leftHand.rotation = Quaternion.Slerp(leftHand.rotation, Quaternion.LookRotation(leftAim.normalized, Vector3.up), reach * 0.68f);
        }
        if (rightAim.sqrMagnitude > 0.0001f)
        {
            rightHand.rotation = Quaternion.Slerp(rightHand.rotation, Quaternion.LookRotation(rightAim.normalized, Vector3.up), reach * 0.68f);
        }
    }

    private void ClampImportedKeeperVisibleBounds()
    {
        if (!importedKeeperBoundsCaptured || !TryGetVisibleBounds(keeperVisibleModel, out Bounds bounds))
        {
            return;
        }

        Vector3 correction = Vector3.zero;
        const float goalMinX = -2.78f;
        const float goalMaxX = 2.78f;
        const float goalMinY = 0.02f;
        const float goalMaxY = 2.92f;
        float goalMinZ = keeperStart.z - 1.22f;
        float goalMaxZ = keeperStart.z + 0.82f;

        if (bounds.min.x < goalMinX)
        {
            correction.x += goalMinX - bounds.min.x;
        }
        else if (bounds.max.x > goalMaxX)
        {
            correction.x -= bounds.max.x - goalMaxX;
        }

        if (bounds.min.y < goalMinY)
        {
            correction.y += goalMinY - bounds.min.y;
        }
        else if (bounds.max.y > goalMaxY)
        {
            correction.y -= bounds.max.y - goalMaxY;
        }

        if (bounds.min.z < goalMinZ)
        {
            correction.z += goalMinZ - bounds.min.z;
        }
        else if (bounds.max.z > goalMaxZ)
        {
            correction.z -= bounds.max.z - goalMaxZ;
        }

        Vector3 centerLocal = keeper.InverseTransformPoint(bounds.center);
        float centerDriftX = centerLocal.x - importedKeeperReadyBoundsCenterLocal.x;
        float centerDriftY = centerLocal.y - importedKeeperReadyBoundsCenterLocal.y;
        float maxCenterDriftX = shooting && keeperRow == 0 ? 1.18f : 2.2f;
        float maxCenterDriftY = shooting && keeperRow == 0 ? 1.15f : 1.35f;
        if (Mathf.Abs(centerDriftX) > maxCenterDriftX)
        {
            correction.x -= Mathf.Sign(centerDriftX) * (Mathf.Abs(centerDriftX) - maxCenterDriftX);
        }
        if (Mathf.Abs(centerDriftY) > maxCenterDriftY)
        {
            correction.y -= Mathf.Sign(centerDriftY) * (Mathf.Abs(centerDriftY) - maxCenterDriftY);
        }

        if (correction.sqrMagnitude > 0.000001f)
        {
            keeperVisibleModel.position += correction;
        }
    }

    private void RemoveKeeperKitPanels()
    {
        string[] panelNames =
        {
            "BM8 Keeper Black Torso Panel",
            "BM8 Keeper Black Shorts Panel",
            "BM8 Keeper Chest Panel",
            "BM8 Keeper Left Side Stripe",
            "BM8 Keeper Right Side Stripe"
        };

        for (int i = 0; i < panelNames.Length; i++)
        {
            Transform panel = keeper.Find(panelNames[i]);
            if (panel == null)
            {
                continue;
            }

#if UNITY_EDITOR
            DestroyImmediate(panel.gameObject);
#else
            Destroy(panel.gameObject);
#endif
        }
    }

    private static bool TryGetVisibleBounds(Transform root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bounds = new Bounds(root.position, Vector3.zero);
        bool found = false;
        foreach (Renderer renderer in renderers)
        {
            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        return found;
    }

    private void SetupKeeperSpriteSheet()
    {
        string path = Path.Combine(Application.dataPath, "Art/Characters/keeper-sprite-sheet.png");
        if (!File.Exists(path))
        {
            return;
        }

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
        {
            Destroy(texture);
            return;
        }

        texture.wrapMode = TextureWrapMode.Clamp;
        GameObject spriteObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        spriteObject.name = "BM8 Keeper Sprite Sheet";
        spriteObject.transform.SetParent(keeper, false);
        spriteObject.transform.localPosition = new Vector3(0f, 1.28f, -0.52f);
        spriteObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        spriteObject.transform.localScale = new Vector3(1.35f, 1.95f, 1f);

        Collider collider = spriteObject.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Shader shader = Shader.Find("BM8/ChromaKeySprite");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Transparent");
        }

        keeperSpriteMaterial = new Material(shader);
        keeperSpriteMaterial.mainTexture = texture;
        keeperSpriteRenderer = spriteObject.GetComponent<Renderer>();
        if (keeperSpriteRenderer != null)
        {
            keeperSpriteRenderer.sharedMaterial = keeperSpriteMaterial;
            keeperSpriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            keeperSpriteRenderer.receiveShadows = false;
        }

        keeperSprite = spriteObject.transform;
        usingKeeperSpriteSheet = true;
        HideProceduralKeeperRenderersForSprite();
        SetKeeperSpriteFrame(0, 0, false);
    }

    private void DisableKeeperSpriteSheet()
    {
        usingKeeperSpriteSheet = false;
        if (keeperSprite != null)
        {
            Destroy(keeperSprite.gameObject);
            keeperSprite = null;
        }

        Transform existingSprite = keeper.Find("BM8 Keeper Sprite Sheet");
        if (existingSprite != null)
        {
            Destroy(existingSprite.gameObject);
        }

        keeperSpriteRenderer = null;
        keeperSpriteMaterial = null;
    }

    private void EnsureProceduralKeeperVisible()
    {
        foreach (Renderer renderer in keeper.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.name.StartsWith("Keeper", System.StringComparison.Ordinal))
            {
                renderer.enabled = true;
            }
        }
    }

    private void HideProceduralKeeperRenderersForSprite()
    {
        foreach (Renderer renderer in keeper.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == keeperSpriteRenderer || renderer.name.Contains("Glove"))
            {
                continue;
            }

            if (renderer.name.StartsWith("Keeper", System.StringComparison.Ordinal))
            {
                renderer.enabled = false;
            }
        }
    }

    private void ApplyRealisticCharacterDesign()
    {
        ApplyStrikerDesign();
        ApplyKeeperDesign();
        RemoveStrikerArmNumbers();
    }

    private void HideStrikerControlRigWhenFbxExists()
    {
        if (strikerVisibleModel == null)
        {
            return;
        }

        foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.transform == strikerVisibleModel || renderer.transform.IsChildOf(strikerVisibleModel))
            {
                renderer.enabled = true;
                continue;
            }

            if (renderer.name.StartsWith("BM8 Striker", System.StringComparison.Ordinal))
            {
                renderer.enabled = false;
            }
        }
    }

    private void HideProceduralStrikerForArcadeCamera()
    {
        if (!UseArcadeVideoCamera)
        {
            return;
        }

        foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = false;
        }
    }

    private void HideKeeperControlRigWhenFbxExists()
    {
        if (keeperVisibleModel == null)
        {
            return;
        }

        foreach (var renderer in keeper.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.transform == keeperVisibleModel || renderer.transform.IsChildOf(keeperVisibleModel))
            {
                renderer.enabled = true;
                continue;
            }

            if (renderer.name.StartsWith("Keeper", System.StringComparison.Ordinal))
            {
                renderer.enabled = false;
            }
        }
    }

    private void AnimateVisibleStriker(float pitch, float stride, float kickFollow)
    {
        if (strikerVisibleModel == null)
        {
            return;
        }

        float bob = Mathf.Abs(stride) * 0.06f;
        strikerVisibleModel.localPosition = new Vector3(0.035f * stride, bob, -0.02f + 0.06f * kickFollow);
        strikerVisibleModel.localRotation = Quaternion.Euler(pitch, 180f + 5f * stride, -3f * stride);
    }

    private void AnimateVisibleKeeper(float pitch, float roll, float reach)
    {
        if (UseAaAnimatedKeeper)
        {
            return;
        }

        if (usingKeeperSpriteSheet)
        {
            return;
        }

        if (keeperVisibleModel == null)
        {
            return;
        }

        keeperVisibleModel.localPosition = new Vector3(0f, 0.02f + 0.05f * reach, -0.02f);
        keeperVisibleModel.localRotation = Quaternion.Euler(pitch, 180f, roll);
    }

    private void AnimateKeeperSprite(float t, bool topRow, bool middleRow, bool bottomRow, float side)
    {
        if (!usingKeeperSpriteSheet || keeperSprite == null)
        {
            return;
        }

        bool mirror = side < 0f;
        if (t < 0.18f)
        {
            SetKeeperSpriteFrame(1, 0, mirror);
            return;
        }

        if (bottomRow)
        {
            SetKeeperSpriteFrame(t < 0.58f ? 1 : 4, 1, mirror);
            return;
        }

        if (middleRow)
        {
            SetKeeperSpriteFrame(t < 0.46f ? 2 : 3, 1, mirror);
            return;
        }

        SetKeeperSpriteFrame(t < 0.48f ? 2 : 3, 0, mirror);
    }

    private void SetKeeperSpriteFrame(int col, int row, bool mirror)
    {
        if (keeperSpriteMaterial == null || keeperSprite == null)
        {
            return;
        }

        const float columns = 5f;
        const float rows = 2f;
        col = Mathf.Clamp(col, 0, 4);
        row = Mathf.Clamp(row, 0, 1);
        keeperSpriteMaterial.mainTextureScale = new Vector2((mirror ? -1f : 1f) / columns, 1f / rows);
        keeperSpriteMaterial.mainTextureOffset = new Vector2((mirror ? col + 1f : col) / columns, (1f - row) / rows);
    }

    private void PlayKeeperDiveAnimation(bool save)
    {
        if (!UseAaAnimatedKeeper && IsStandingBlockRow(keeperRow))
        {
            PlayKeeperController(keeperIdleController);
            return;
        }

        RuntimeAnimatorController controller = UseAaAnimatedKeeper ? LoadAaKeeperController(AaKeeperControllerName(save)) : null;
        if (controller == null && keeperRow == 0)
        {
            if (keeperCol == 0)
            {
                controller = save ? keeperHitTopLeftSuccessController : keeperHitTopLeftFailController;
            }
            else if (keeperCol == 2)
            {
                controller = save ? keeperHitTopRightSuccessController : keeperHitTopRightFailController;
            }
            else
            {
                controller = save ? LoadAaKeeperController("AA_Soccer_Goal_HitBall_UP_Succ") : LoadAaKeeperController("AA_Soccer_Goal_HitBall_UP_Fail");
            }
        }
        else if (controller == null && keeperRow == 2)
        {
            if (keeperCol == 0)
            {
                controller = save ? keeperCatchLeftDownSuccessController : keeperCatchLeftDownFailController;
            }
            else if (keeperCol == 2)
            {
                controller = save ? keeperCatchRightDownSuccessController : keeperCatchRightDownFailController;
            }
            else
            {
                controller = save ? keeperCatchForwardSuccessController : keeperCatchForwardFailController;
            }
        }
        else if (controller == null)
        {
            if (keeperCol == 0)
            {
                controller = save ? keeperHitLeftSuccessController : keeperHitLeftFailController;
            }
            else if (keeperCol == 2)
            {
                controller = save ? keeperHitRightSuccessController : keeperHitRightFailController;
            }
            else
            {
                controller = save ? keeperCatchForwardSuccessController : keeperCatchForwardFailController;
            }
        }

        PlayKeeperController(controller);
    }

    private string AaKeeperControllerName(bool save)
    {
        string suffix = save ? "Succ" : "Fail";
        if (keeperRow == 0)
        {
            if (keeperCol == 0)
            {
                return "AA_Soccer_Goal_HitBall_TL_" + suffix;
            }

            if (keeperCol == 2)
            {
                return "AA_Soccer_Goal_HitBall_TR_" + suffix;
            }

            return "AA_Soccer_Goal_HitBall_UP_" + suffix;
        }

        if (keeperRow == 2)
        {
            if (keeperCol == 0)
            {
                return "AA_Soccer_Goal_CatchBall_LD_" + suffix;
            }

            if (keeperCol == 2)
            {
                return "AA_Soccer_Goal_CatchBall_RD_" + suffix;
            }

            return "AA_Soccer_Goal_CatchBall_F_" + suffix;
        }

        if (keeperCol == 0)
        {
            return "AA_Soccer_Goal_HitBall_L_" + suffix;
        }

        if (keeperCol == 2)
        {
            return "AA_Soccer_Goal_HitBall_R_" + suffix;
        }

        return "AA_Soccer_Goal_HitBall_F_" + suffix;
    }

    private static RuntimeAnimatorController LoadAaKeeperController(string controllerName)
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AaGoalkeeperControllerFolder + controllerName + ".Controller");
#else
        return null;
#endif
    }

    private void PlayKeeperController(RuntimeAnimatorController controller)
    {
        if (keeperAnimator == null || controller == null)
        {
            return;
        }

        keeperAnimator.speed = 1f;
        keeperAnimator.runtimeAnimatorController = controller;
        keeperAnimator.Rebind();
        keeperAnimator.Update(0f);
    }

    private Vector3 AaKeeperRootTarget(bool saved)
    {
        if (!saved)
        {
            float missSide = keeperCol == 1 ? 0f : Mathf.Sign(keeperCol - 1f) * 0.42f;
            return keeperStart + new Vector3(missSide, 0f, -0.08f);
        }

        float side = keeperCol == 0 ? -1f : keeperCol == 2 ? 1f : 0f;
        if (keeperRow == 0)
        {
            return keeperStart + new Vector3(side * 0.72f, 0.36f, -0.12f);
        }

        if (keeperRow == 2)
        {
            return keeperStart + new Vector3(side * 1.08f, 0f, -0.38f);
        }

        return keeperStart + new Vector3(side * 1.16f, side == 0f ? 0.02f : 0.12f, -0.22f);
    }

    private Quaternion AaKeeperRootRotation(bool saved)
    {
        if (!saved)
        {
            return Quaternion.identity;
        }

        float side = keeperCol == 0 ? -1f : keeperCol == 2 ? 1f : 0f;
        if (keeperRow == 0)
        {
            return Quaternion.Euler(-4f, side * 5f, -side * 7f);
        }

        if (keeperRow == 2)
        {
            return Quaternion.Euler(4f, side * 9f, -side * 12f);
        }

        return Quaternion.Euler(-2f, side * 10f, -side * 16f);
    }

    private struct KeeperSaveProfile
    {
        public float rowHeight;
        public float lowDive;
        public float lift;
        public float forwardDrive;
        public float airSideDrift;
        public float launchDuration;
        public float torsoY;
        public float headY;
        public float torsoSide;
        public float headSide;
        public float headLift;
        public float activeArmY;
        public float passiveArmY;
        public float sideReach;
        public float centerHandSpread;
        public float contactSpread;
        public float contactYOffset;
        public float punchSide;
        public float punchUp;
        public float punchForward;
        public float gloveScale;

        public static KeeperSaveProfile For(int col, int row)
        {
            bool center = col == 1;
            if (row == 0)
            {
                return new KeeperSaveProfile
                {
                    rowHeight = 1f,
                    lowDive = 0f,
                    lift = center ? 0.96f : 0.88f,
                    forwardDrive = -0.18f,
                    airSideDrift = center ? 0.16f : 0.34f,
                    launchDuration = 0.36f,
                    torsoY = 1.16f,
                    headY = 1.78f,
                    torsoSide = center ? 0.02f : 0.05f,
                    headSide = center ? 0.02f : 0.08f,
                    headLift = 0.08f,
                    activeArmY = center ? 0.96f : 0.84f,
                    passiveArmY = 0.34f,
                    sideReach = center ? 0.58f : 0.74f,
                    centerHandSpread = 0.28f,
                    contactSpread = center ? 0.07f : 0f,
                    contactYOffset = center ? 0.02f : 0f,
                    punchSide = center ? 0.24f : 0.38f,
                    punchUp = 0.14f,
                    punchForward = 0.62f,
                    gloveScale = center ? 0.32f : 0.35f
                };
            }

            if (row == 1)
            {
                return new KeeperSaveProfile
                {
                    rowHeight = 0.52f,
                    lowDive = 0f,
                    lift = center ? 0.42f : 0.34f,
                    forwardDrive = center ? -0.2f : -0.32f,
                    airSideDrift = center ? 0.2f : 0.46f,
                    launchDuration = 0.32f,
                    torsoY = 0.96f,
                    headY = 1.58f,
                    torsoSide = center ? 0.04f : 0.13f,
                    headSide = center ? 0.08f : 0.2f,
                    headLift = 0.015f,
                    activeArmY = center ? 0.26f : 0.14f,
                    passiveArmY = -0.08f,
                    sideReach = center ? 0.54f : 1f,
                    centerHandSpread = 0.34f,
                    contactSpread = center ? 0.1f : 0f,
                    contactYOffset = center ? 0.01f : 0f,
                    punchSide = center ? 0.32f : 0.48f,
                    punchUp = 0.1f,
                    punchForward = 0.58f,
                    gloveScale = center ? 0.31f : 0.36f
                };
            }

            return new KeeperSaveProfile
            {
                rowHeight = 0f,
                lowDive = 1f,
                lift = center ? 0.08f : 0.04f,
                forwardDrive = center ? -0.34f : -0.48f,
                airSideDrift = center ? 0.16f : 0.36f,
                launchDuration = 0.3f,
                torsoY = 0.74f,
                headY = 1.28f,
                torsoSide = center ? 0.03f : 0.07f,
                headSide = center ? 0.07f : 0.18f,
                headLift = 0.005f,
                activeArmY = center ? -0.32f : -0.52f,
                passiveArmY = -0.28f,
                sideReach = center ? 0.46f : 0.84f,
                centerHandSpread = 0.24f,
                contactSpread = center ? 0.08f : 0f,
                contactYOffset = -0.015f,
                punchSide = center ? 0.28f : 0.42f,
                punchUp = 0.04f,
                punchForward = 0.5f,
                gloveScale = center ? 0.31f : 0.35f
            };
        }
    }

    private void ApplyStrikerDesign()
    {
        Color shirt = new Color(0.92f, 0.18f, 0.08f);
        Color chest = new Color(0.08f, 0.08f, 0.085f);
        Color shorts = new Color(0.04f, 0.055f, 0.07f);
        Color skin = new Color(0.76f, 0.50f, 0.34f);
        Color socks = new Color(0.93f, 0.1f, 0.05f);
        Color hair = new Color(0.025f, 0.02f, 0.018f);
        Color boot = new Color(0.015f, 0.015f, 0.016f);

        player.localScale = Vector3.one * 1.22f;

        StyleBodyPart(strikerTorso, new Vector3(0f, 1.12f, 0f), new Vector3(0.42f, 0.86f, 0.3f), shirt);
        StyleBodyPart(strikerHead, new Vector3(0f, 1.86f, 0f), new Vector3(0.23f, 0.3f, 0.22f), skin);
        StyleBodyPart(player.Find("BM8 Striker Shorts"), new Vector3(0f, 0.62f, 0f), new Vector3(0.48f, 0.26f, 0.28f), shorts);
        StyleBodyPart(strikerLeftLeg, new Vector3(-0.15f, 0.27f, 0f), new Vector3(0.11f, 0.66f, 0.11f), skin);
        StyleBodyPart(strikerRightLeg, new Vector3(0.15f, 0.27f, 0f), new Vector3(0.11f, 0.66f, 0.11f), skin);
        StyleBodyPart(strikerLeftArm, new Vector3(-0.39f, 1.14f, 0.01f), new Vector3(0.095f, 0.56f, 0.095f), shirt);
        StyleBodyPart(strikerRightArm, new Vector3(0.39f, 1.14f, 0.01f), new Vector3(0.095f, 0.56f, 0.095f), shirt);

        EnsureDetailPart(player, "BM8 Striker Neck", PrimitiveType.Capsule, new Vector3(0f, 1.55f, -0.01f), new Vector3(0.16f, 0.18f, 0.14f), skin);
        EnsureDetailPart(player, "BM8 Striker Shoulder Line", PrimitiveType.Cube, new Vector3(0f, 1.48f, -0.02f), new Vector3(0.68f, 0.07f, 0.2f), shirt);
        EnsureDetailPart(player, "BM8 Striker Chest Panel", PrimitiveType.Cube, new Vector3(0f, 1.13f, -0.18f), new Vector3(0.34f, 0.46f, 0.026f), chest);
        EnsureDetailPart(player, "BM8 Striker Collar", PrimitiveType.Cube, new Vector3(0f, 1.53f, -0.16f), new Vector3(0.22f, 0.04f, 0.05f), Color.white);
        EnsureDetailPart(player, "BM8 Striker Hair", PrimitiveType.Sphere, new Vector3(0f, 2.02f, -0.02f), new Vector3(0.24f, 0.12f, 0.22f), hair);
        EnsureDetailPart(player, "BM8 Striker Back Hair", PrimitiveType.Cube, new Vector3(0f, 1.93f, 0.16f), new Vector3(0.2f, 0.18f, 0.04f), hair);
        EnsureDetailPart(player, "BM8 Striker Left Ear", PrimitiveType.Sphere, new Vector3(-0.22f, 1.86f, 0f), new Vector3(0.045f, 0.075f, 0.035f), skin);
        EnsureDetailPart(player, "BM8 Striker Right Ear", PrimitiveType.Sphere, new Vector3(0.22f, 1.86f, 0f), new Vector3(0.045f, 0.075f, 0.035f), skin);
        EnsureDetailPart(player, "BM8 Striker Left Sock", PrimitiveType.Capsule, new Vector3(-0.15f, 0.04f, -0.01f), new Vector3(0.115f, 0.24f, 0.115f), socks);
        EnsureDetailPart(player, "BM8 Striker Right Sock", PrimitiveType.Capsule, new Vector3(0.15f, 0.04f, -0.01f), new Vector3(0.115f, 0.24f, 0.115f), socks);
        EnsureDetailPart(player, "BM8 Striker Left Boot", PrimitiveType.Cube, new Vector3(-0.15f, -0.09f, -0.08f), new Vector3(0.18f, 0.07f, 0.28f), boot);
        EnsureDetailPart(player, "BM8 Striker Right Boot", PrimitiveType.Cube, new Vector3(0.15f, -0.09f, -0.08f), new Vector3(0.18f, 0.07f, 0.28f), boot);
        EnsureDetailPart(player, "BM8 Striker Left Forearm", PrimitiveType.Capsule, new Vector3(-0.42f, 0.82f, 0.01f), new Vector3(0.08f, 0.26f, 0.08f), skin);
        EnsureDetailPart(player, "BM8 Striker Right Forearm", PrimitiveType.Capsule, new Vector3(0.42f, 0.82f, 0.01f), new Vector3(0.08f, 0.26f, 0.08f), skin);
        EnsureDetailPart(player, "BM8 Striker Left Hand", PrimitiveType.Sphere, new Vector3(-0.42f, 0.66f, -0.02f), new Vector3(0.09f, 0.08f, 0.07f), skin);
        EnsureDetailPart(player, "BM8 Striker Right Hand", PrimitiveType.Sphere, new Vector3(0.42f, 0.66f, -0.02f), new Vector3(0.09f, 0.08f, 0.07f), skin);
        EnsureDetailPart(player, "BM8 Striker Left Sleeve Trim", PrimitiveType.Cube, new Vector3(-0.39f, 0.91f, -0.01f), new Vector3(0.13f, 0.035f, 0.12f), Color.white);
        EnsureDetailPart(player, "BM8 Striker Right Sleeve Trim", PrimitiveType.Cube, new Vector3(0.39f, 0.91f, -0.01f), new Vector3(0.13f, 0.035f, 0.12f), Color.white);
    }

    private void ApplyKeeperDesign()
    {
        Color shirt = new Color(0.025f, 0.025f, 0.03f);
        Color red = new Color(0.78f, 0.015f, 0.025f);
        Color shorts = new Color(0.018f, 0.018f, 0.022f);
        Color skin = new Color(0.78f, 0.52f, 0.34f);
        Color socks = new Color(0.08f, 0.08f, 0.09f);
        Color gloveYellow = new Color(1f, 0.9f, 0.08f);
        Color glovePalm = new Color(0.08f, 0.08f, 0.085f);
        Color white = Color.white;

        Color hair = new Color(0.025f, 0.02f, 0.018f);
        Color boot = new Color(0.015f, 0.015f, 0.016f);

        keeper.localScale = Vector3.one * 1.16f;

        StyleBodyPart(keeperTorso, new Vector3(0f, 1.16f, 0f), new Vector3(0.52f, 0.92f, 0.34f), shirt);
        StyleBodyPart(keeperHead, new Vector3(0f, 1.9f, -0.02f), new Vector3(0.26f, 0.34f, 0.24f), skin);
        StyleBodyPart(keeper.Find("Keeper Shorts"), new Vector3(0f, 0.63f, 0f), new Vector3(0.54f, 0.3f, 0.32f), shorts);
        StyleBodyPart(keeperLeftLeg, new Vector3(-0.16f, 0.27f, 0f), new Vector3(0.125f, 0.68f, 0.125f), skin);
        StyleBodyPart(keeperRightLeg, new Vector3(0.16f, 0.27f, 0f), new Vector3(0.125f, 0.68f, 0.125f), skin);
        StyleBodyPart(keeperLeftArm, new Vector3(-0.46f, 1.15f, 0.01f), new Vector3(0.12f, 0.54f, 0.12f), shirt);
        StyleBodyPart(keeperRightArm, new Vector3(0.46f, 1.15f, 0.01f), new Vector3(0.12f, 0.54f, 0.12f), shirt);

        EnsureDetailPart(keeper, "Keeper Neck", PrimitiveType.Capsule, new Vector3(0f, 1.55f, -0.01f), new Vector3(0.16f, 0.18f, 0.14f), skin);
        EnsureDetailPart(keeper, "Keeper Shoulder Line", PrimitiveType.Cube, new Vector3(0f, 1.51f, -0.02f), new Vector3(0.82f, 0.1f, 0.24f), shirt);
        EnsureDetailPart(keeper, "Keeper Chest Panel", PrimitiveType.Cube, new Vector3(0f, 1.17f, -0.19f), new Vector3(0.3f, 0.52f, 0.03f), red);
        EnsureDetailPart(keeper, "Keeper Left Red Side Panel", PrimitiveType.Cube, new Vector3(-0.31f, 1.1f, -0.19f), new Vector3(0.1f, 0.7f, 0.032f), red);
        EnsureDetailPart(keeper, "Keeper Right Red Side Panel", PrimitiveType.Cube, new Vector3(0.31f, 1.1f, -0.19f), new Vector3(0.1f, 0.7f, 0.032f), red);
        EnsureDetailPart(keeper, "Keeper Left Shoulder Flash", PrimitiveType.Cube, new Vector3(-0.31f, 1.49f, -0.2f), new Vector3(0.22f, 0.055f, 0.035f), red);
        EnsureDetailPart(keeper, "Keeper Right Shoulder Flash", PrimitiveType.Cube, new Vector3(0.31f, 1.49f, -0.2f), new Vector3(0.22f, 0.055f, 0.035f), red);
        EnsureDetailPart(keeper, "Keeper White Left Piping", PrimitiveType.Cube, new Vector3(-0.39f, 1.09f, -0.21f), new Vector3(0.024f, 0.76f, 0.034f), white);
        EnsureDetailPart(keeper, "Keeper White Right Piping", PrimitiveType.Cube, new Vector3(0.39f, 1.09f, -0.21f), new Vector3(0.024f, 0.76f, 0.034f), white);
        EnsureDetailPart(keeper, "Keeper Collar", PrimitiveType.Cube, new Vector3(0f, 1.55f, -0.17f), new Vector3(0.28f, 0.046f, 0.06f), white);
        EnsureDetailPart(keeper, "Keeper Hair", PrimitiveType.Sphere, new Vector3(0f, 2.07f, -0.02f), new Vector3(0.28f, 0.12f, 0.24f), hair);
        EnsureDetailPart(keeper, "Keeper Hair Front", PrimitiveType.Cube, new Vector3(0f, 2.035f, -0.18f), new Vector3(0.24f, 0.08f, 0.05f), hair);
        EnsureDetailPart(keeper, "Keeper Back Hair", PrimitiveType.Cube, new Vector3(0f, 1.96f, 0.16f), new Vector3(0.22f, 0.18f, 0.045f), hair);
        EnsureDetailPart(keeper, "Keeper Left Ear", PrimitiveType.Sphere, new Vector3(-0.225f, 1.89f, 0f), new Vector3(0.048f, 0.078f, 0.036f), skin);
        EnsureDetailPart(keeper, "Keeper Right Ear", PrimitiveType.Sphere, new Vector3(0.225f, 1.89f, 0f), new Vector3(0.048f, 0.078f, 0.036f), skin);
        EnsureDetailPart(keeper, "Keeper Left Eye", PrimitiveType.Sphere, new Vector3(-0.074f, 1.925f, -0.2f), new Vector3(0.03f, 0.03f, 0.018f), Color.black);
        EnsureDetailPart(keeper, "Keeper Right Eye", PrimitiveType.Sphere, new Vector3(0.074f, 1.925f, -0.2f), new Vector3(0.03f, 0.03f, 0.018f), Color.black);
        EnsureDetailPart(keeper, "Keeper Left Brow", PrimitiveType.Cube, new Vector3(-0.075f, 1.965f, -0.205f), new Vector3(0.07f, 0.014f, 0.012f), hair);
        EnsureDetailPart(keeper, "Keeper Right Brow", PrimitiveType.Cube, new Vector3(0.075f, 1.965f, -0.205f), new Vector3(0.07f, 0.014f, 0.012f), hair);
        EnsureDetailPart(keeper, "Keeper Nose", PrimitiveType.Sphere, new Vector3(0f, 1.875f, -0.215f), new Vector3(0.038f, 0.048f, 0.028f), new Color(0.7f, 0.44f, 0.29f));
        EnsureDetailPart(keeper, "Keeper Mouth", PrimitiveType.Cube, new Vector3(0f, 1.81f, -0.214f), new Vector3(0.095f, 0.018f, 0.012f), new Color(0.16f, 0.04f, 0.035f));
        EnsureDetailPart(keeper, "Keeper Beard", PrimitiveType.Cube, new Vector3(0f, 1.785f, -0.205f), new Vector3(0.13f, 0.045f, 0.018f), new Color(0.06f, 0.035f, 0.025f));
        EnsureDetailPart(keeper, "Keeper Left Sock", PrimitiveType.Capsule, new Vector3(-0.16f, 0.04f, -0.01f), new Vector3(0.12f, 0.25f, 0.12f), socks);
        EnsureDetailPart(keeper, "Keeper Right Sock", PrimitiveType.Capsule, new Vector3(0.16f, 0.04f, -0.01f), new Vector3(0.12f, 0.25f, 0.12f), socks);
        EnsureDetailPart(keeper, "Keeper Left Boot", PrimitiveType.Cube, new Vector3(-0.16f, -0.09f, -0.09f), new Vector3(0.19f, 0.075f, 0.3f), boot);
        EnsureDetailPart(keeper, "Keeper Right Boot", PrimitiveType.Cube, new Vector3(0.16f, -0.09f, -0.09f), new Vector3(0.19f, 0.075f, 0.3f), boot);
        EnsureDetailPart(keeper, "Keeper Left Knee Pad", PrimitiveType.Sphere, new Vector3(-0.16f, 0.37f, -0.1f), new Vector3(0.12f, 0.075f, 0.04f), shorts);
        EnsureDetailPart(keeper, "Keeper Right Knee Pad", PrimitiveType.Sphere, new Vector3(0.16f, 0.37f, -0.1f), new Vector3(0.12f, 0.075f, 0.04f), shorts);
        EnsureDetailPart(keeper, "Keeper Left Sleeve Trim", PrimitiveType.Cube, new Vector3(-0.46f, 0.94f, -0.03f), new Vector3(0.16f, 0.042f, 0.13f), white);
        EnsureDetailPart(keeper, "Keeper Right Sleeve Trim", PrimitiveType.Cube, new Vector3(0.46f, 0.94f, -0.03f), new Vector3(0.16f, 0.042f, 0.13f), white);
        EnsureDetailPart(keeper, "Keeper Left Glove Palm", PrimitiveType.Cube, new Vector3(-0.43f, 1.13f, -0.32f), new Vector3(0.18f, 0.12f, 0.035f), glovePalm);
        EnsureDetailPart(keeper, "Keeper Right Glove Palm", PrimitiveType.Cube, new Vector3(0.43f, 1.13f, -0.32f), new Vector3(0.18f, 0.12f, 0.035f), glovePalm);
        EnsureTextDecal(keeper, "Keeper BM Text", "bm", new Vector3(-0.035f, 1.2f, -0.226f), 0.17f, white);
        EnsureTextDecal(keeper, "Keeper 8 Text", "8", new Vector3(0.105f, 1.2f, -0.226f), 0.19f, red);
    }

    private static void StyleBodyPart(Transform part, Vector3 localPosition, Vector3 localScale, Color color)
    {
        if (part == null)
        {
            return;
        }

        part.localPosition = localPosition;
        part.localScale = localScale;
        SetMaterialColor(part, color);
    }

    private static void EnsureDetailPart(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject part = existing != null ? existing.gameObject : GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;
        SetMaterialColor(part.transform, color);
    }

    private static void EnsureTextDecal(Transform parent, string name, string text, Vector3 localPosition, float size, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject decal = existing != null ? existing.gameObject : new GameObject(name);
        decal.name = name;
        decal.transform.SetParent(parent, false);
        decal.transform.localPosition = localPosition;
        decal.transform.localRotation = Quaternion.identity;
        decal.transform.localScale = Vector3.one;

        TextMesh mesh = decal.GetComponent<TextMesh>();
        if (mesh == null)
        {
            mesh = decal.AddComponent<TextMesh>();
        }

        mesh.text = text;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.characterSize = size;
        mesh.fontSize = 96;
        mesh.color = color;
    }

    private static void SetMaterialColor(Transform part, Color color)
    {
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Material material = new Material(Shader.Find("Standard"));
        renderer.sharedMaterial = material;
        material.color = color;
    }

    private static void RemoveArmNumberDecals(Transform arm)
    {
        if (arm == null)
        {
            return;
        }

        foreach (Transform child in arm.GetComponentsInChildren<Transform>(true))
        {
            if (child == arm)
            {
                continue;
            }

            if (IsArmNumberDecal(child))
            {
                Destroy(child.gameObject);
            }
        }
    }

    private static bool IsArmNumberDecal(Transform candidate)
    {
        string childName = candidate.name.ToLowerInvariant();
        if (childName == "8" || childName.Contains("number 8") || childName.Contains("sleeve 8") || childName.Contains("arm 8"))
        {
            return true;
        }

        TextMesh meshText = candidate.GetComponent<TextMesh>();
        if (meshText != null && meshText.text.Trim() == "8")
        {
            return true;
        }

        Text uiText = candidate.GetComponent<Text>();
        return uiText != null && uiText.text.Trim() == "8";
    }

    private void EnsureKeeperGloves()
    {
        keeperLeftGlove = keeper.Find("Keeper Left Glove");
        if (keeperLeftGlove == null)
        {
            keeperLeftGlove = CreateKeeperGlove("Keeper Left Glove", new Vector3(-0.43f, 1.13f, -0.18f));
        }

        keeperRightGlove = keeper.Find("Keeper Right Glove");
        if (keeperRightGlove == null)
        {
            keeperRightGlove = CreateKeeperGlove("Keeper Right Glove", new Vector3(0.43f, 1.13f, -0.18f));
        }
    }

    private void HideKeeperMarkerGlovesWhenImportedKeeperIsActive()
    {
        if (!UseAaAnimatedKeeper || keeperVisibleModel == null)
        {
            return;
        }

        DisableRenderer(keeperLeftGlove);
        DisableRenderer(keeperRightGlove);
    }

    private static void DisableRenderer(Transform target)
    {
        if (target == null)
        {
            return;
        }

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
    }

    private Transform CreateKeeperGlove(string name, Vector3 localPosition)
    {
        GameObject glove = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glove.name = name;
        glove.transform.SetParent(keeper, false);
        glove.transform.localPosition = localPosition;
        glove.transform.localScale = Vector3.one * 0.26f;
        Renderer renderer = glove.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(1f, 0.9f, 0.08f);
            renderer.sharedMaterial = material;
            renderer.enabled = !UseAaAnimatedKeeper;
        }

        return glove.transform;
    }

    private void SetupBallTrail()
    {
        ballTrail = ball.GetComponent<TrailRenderer>();
        if (ballTrail == null)
        {
            ballTrail = ball.gameObject.AddComponent<TrailRenderer>();
        }

        ballTrail.time = 0.58f;
        ballTrail.startWidth = 0.26f;
        ballTrail.endWidth = 0.018f;
        ballTrail.minVertexDistance = 0.025f;
        ballTrail.numCapVertices = 8;
        ballTrail.numCornerVertices = 6;
        ballTrail.alignment = LineAlignment.View;
        ballTrail.textureMode = LineTextureMode.Stretch;
        ballTrail.widthCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.42f, 0.64f),
            new Keyframe(1f, 0.06f));
        ballTrail.material = new Material(Shader.Find("Sprites/Default"));
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 1f, 1f), 0f),
                new GradientColorKey(new Color(1f, 0.88f, 0.16f), 0.34f),
                new GradientColorKey(new Color(1f, 0.18f, 0.08f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.98f, 0f),
                new GradientAlphaKey(0.72f, 0.38f),
                new GradientAlphaKey(0f, 1f)
            });
        ballTrail.colorGradient = gradient;
        ballTrail.Clear();
    }

    private void EnsureBallShadow()
    {
        Transform existing = transform.Find("BM8 Ball Shadow");
        if (existing == null)
        {
            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shadow.name = "BM8 Ball Shadow";
            ballShadow = shadow.transform;
            ballShadow.SetParent(transform, false);

            Collider collider = shadow.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            Renderer renderer = shadow.GetComponent<Renderer>();
            if (renderer != null)
            {
                ballShadowMaterial = new Material(Shader.Find("Sprites/Default"));
                ballShadowMaterial.color = new Color(0f, 0f, 0f, 0.34f);
                renderer.sharedMaterial = ballShadowMaterial;
            }
        }
        else
        {
            ballShadow = existing;
            Renderer renderer = ballShadow.GetComponent<Renderer>();
            ballShadowMaterial = renderer != null ? renderer.sharedMaterial : null;
        }

        UpdateBallShadow();
    }

    private void UpdateBallShadow()
    {
        if (ballShadow == null || ball == null)
        {
            return;
        }

        float height = Mathf.Max(0f, ball.position.y - 0.22f);
        float fade = Mathf.Clamp01(1f - height / 3.2f);
        float size = Mathf.Lerp(0.34f, 0.82f, fade);
        ballShadow.position = new Vector3(ball.position.x, 0.045f, ball.position.z + 0.03f);
        ballShadow.rotation = Quaternion.Euler(90f, 0f, 0f);
        ballShadow.localScale = new Vector3(size * 1.35f, size * 0.52f, 1f);

        if (ballShadowMaterial != null)
        {
            ballShadowMaterial.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.06f, 0.34f, fade));
        }
    }

    private void EnsureSaveImpactFlash()
    {
        Transform existing = transform.Find("BM8 Save Impact Flash");
        saveImpactFlash = existing;
        if (saveImpactFlash == null)
        {
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Quad);
            flash.name = "BM8 Save Impact Flash";
            saveImpactFlash = flash.transform;
            saveImpactFlash.SetParent(transform, false);
            Collider collider = flash.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        Renderer renderer = saveImpactFlash.GetComponent<Renderer>();
        if (renderer != null)
        {
            saveImpactMaterial = new Material(Shader.Find("Sprites/Default"));
            saveImpactMaterial.color = new Color(1f, 0.9f, 0.08f, 0f);
            renderer.sharedMaterial = saveImpactMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        HideSaveImpactFlash();
    }

    private void UpdateSaveImpactFlash(float intensity, Vector3 contact)
    {
        if (saveImpactFlash == null)
        {
            return;
        }

        float visible = Mathf.Clamp01(intensity);
        if (visible <= 0.001f)
        {
            HideSaveImpactFlash();
            return;
        }

        saveImpactFlash.gameObject.SetActive(true);
        saveImpactFlash.position = contact + new Vector3(0f, 0f, -0.08f);
        if (cameraRig != null)
        {
            Vector3 toCamera = saveImpactFlash.position - cameraRig.position;
            if (toCamera.sqrMagnitude > 0.0001f)
            {
                saveImpactFlash.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
            }
        }

        float size = Mathf.Lerp(0.24f, keeperRow == 0 ? 0.88f : 0.68f, visible);
        saveImpactFlash.localScale = new Vector3(size, size, 1f);
        if (saveImpactMaterial != null)
        {
            saveImpactMaterial.color = new Color(1f, 0.95f, 0.16f, Mathf.Lerp(0.2f, 0.92f, visible));
        }
    }

    private void HideSaveImpactFlash()
    {
        if (saveImpactFlash != null)
        {
            saveImpactFlash.gameObject.SetActive(false);
        }
    }

    private void EnsureSaveShockwave()
    {
        Transform existing = transform.Find("BM8 Save Shockwave");
        saveShockwave = existing;
        if (saveShockwave == null)
        {
            GameObject shockwave = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shockwave.name = "BM8 Save Shockwave";
            saveShockwave = shockwave.transform;
            saveShockwave.SetParent(transform, false);
            Collider collider = shockwave.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        Renderer renderer = saveShockwave.GetComponent<Renderer>();
        if (renderer != null)
        {
            saveShockwaveMaterial = new Material(Shader.Find("Sprites/Default"));
            saveShockwaveMaterial.mainTexture = CreateShockwaveTexture();
            saveShockwaveMaterial.color = new Color(1f, 0.96f, 0.42f, 0f);
            renderer.sharedMaterial = saveShockwaveMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        HideSaveShockwave();
    }

    private static Texture2D CreateShockwaveTexture()
    {
        const int size = 96;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        Color clear = new Color(1f, 1f, 1f, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                float outer = Mathf.SmoothStep(0.74f, 0.86f, distance);
                float inner = 1f - Mathf.SmoothStep(0.88f, 0.99f, distance);
                float alpha = outer * inner;
                texture.SetPixel(x, y, alpha > 0.001f ? new Color(1f, 1f, 1f, alpha) : clear);
            }
        }

        texture.Apply(false, true);
        return texture;
    }

    private void UpdateSaveShockwave(float intensity, Vector3 contact)
    {
        if (saveShockwave == null)
        {
            return;
        }

        float visible = Mathf.Clamp01(intensity);
        if (visible <= 0.001f)
        {
            HideSaveShockwave();
            return;
        }

        saveShockwave.gameObject.SetActive(true);
        saveShockwave.position = contact + new Vector3(0f, 0f, -0.12f);
        if (cameraRig != null)
        {
            Vector3 toCamera = saveShockwave.position - cameraRig.position;
            if (toCamera.sqrMagnitude > 0.0001f)
            {
                saveShockwave.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
            }
        }

        float size = Mathf.Lerp(0.6f, keeperRow == 0 ? 1.72f : 1.42f, 1f - visible * 0.28f);
        saveShockwave.localScale = new Vector3(size, size, 1f);
        if (saveShockwaveMaterial != null)
        {
            saveShockwaveMaterial.color = new Color(1f, 0.96f, 0.42f, Mathf.Lerp(0.08f, 0.62f, visible));
        }
    }

    private void HideSaveShockwave()
    {
        if (saveShockwave != null)
        {
            saveShockwave.gameObject.SetActive(false);
        }
    }

    private void EnsureSaveContactStreak()
    {
        Transform existing = transform.Find("BM8 Save Contact Streak");
        saveContactStreak = existing;
        if (saveContactStreak == null)
        {
            GameObject streak = GameObject.CreatePrimitive(PrimitiveType.Quad);
            streak.name = "BM8 Save Contact Streak";
            saveContactStreak = streak.transform;
            saveContactStreak.SetParent(transform, false);

            Collider collider = streak.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        Renderer renderer = saveContactStreak.GetComponent<Renderer>();
        if (renderer != null)
        {
            saveContactStreakMaterial = new Material(Shader.Find("Sprites/Default"));
            saveContactStreakMaterial.color = new Color(1f, 0.98f, 0.62f, 0f);
            renderer.sharedMaterial = saveContactStreakMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        HideSaveContactStreak();
    }

    private void UpdateSaveContactStreak(float intensity, Vector3 contact, float reboundSide)
    {
        if (saveContactStreak == null)
        {
            return;
        }

        float visible = Mathf.Clamp01(intensity);
        if (visible <= 0.001f)
        {
            HideSaveContactStreak();
            return;
        }

        saveContactStreak.gameObject.SetActive(true);
        saveContactStreak.position = contact + new Vector3(reboundSide * 0.15f, keeperRow == 0 ? 0.05f : 0.02f, -0.14f);
        if (cameraRig != null)
        {
            Vector3 toCamera = saveContactStreak.position - cameraRig.position;
            if (toCamera.sqrMagnitude > 0.0001f)
            {
                saveContactStreak.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, -reboundSide * 16f);
            }
        }

        float length = Mathf.Lerp(0.36f, keeperRow == 0 ? 1.28f : 1.04f, visible);
        float thickness = Mathf.Lerp(0.045f, keeperRow == 0 ? 0.13f : 0.105f, visible);
        saveContactStreak.localScale = new Vector3(length, thickness, 1f);
        if (saveContactStreakMaterial != null)
        {
            saveContactStreakMaterial.color = new Color(1f, 0.98f, 0.58f, Mathf.Lerp(0.16f, 0.94f, visible));
        }
    }

    private void HideSaveContactStreak()
    {
        if (saveContactStreak != null)
        {
            saveContactStreak.gameObject.SetActive(false);
        }
    }

    private void EnsureGoalNetImpact()
    {
        Transform existing = transform.Find("BM8 Goal Net Impact");
        goalNetImpact = existing;
        if (goalNetImpact == null)
        {
            GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Quad);
            impact.name = "BM8 Goal Net Impact";
            goalNetImpact = impact.transform;
            goalNetImpact.SetParent(transform, false);

            Collider collider = impact.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        Renderer renderer = goalNetImpact.GetComponent<Renderer>();
        if (renderer != null)
        {
            goalNetImpactMaterial = new Material(Shader.Find("Sprites/Default"));
            goalNetImpactMaterial.mainTexture = CreateShockwaveTexture();
            goalNetImpactMaterial.color = new Color(1f, 0.88f, 0.18f, 0f);
            renderer.sharedMaterial = goalNetImpactMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        HideGoalNetImpact();
    }

    private void UpdateGoalNetImpact(float intensity, Vector3 impactPoint, float side)
    {
        if (goalNetImpact == null)
        {
            return;
        }

        float visible = Mathf.Clamp01(intensity);
        if (visible <= 0.001f)
        {
            HideGoalNetImpact();
            return;
        }

        goalNetImpact.gameObject.SetActive(true);
        goalNetImpact.position = new Vector3(
            Mathf.Clamp(impactPoint.x, -2.45f, 2.45f),
            Mathf.Clamp(impactPoint.y, 0.95f, 2.65f),
            4.72f);
        goalNetImpact.rotation = Quaternion.Euler(0f, 180f, side * Mathf.Lerp(-8f, 14f, visible));
        float size = Mathf.Lerp(0.42f, 1.55f, visible);
        goalNetImpact.localScale = new Vector3(size * 1.22f, size, 1f);
        UpdateGoalNetWarp(visible, impactPoint);
        if (goalNetImpactMaterial != null)
        {
            goalNetImpactMaterial.color = new Color(1f, 0.86f, 0.18f, Mathf.Lerp(0.12f, 0.72f, visible));
        }
    }

    private void HideGoalNetImpact()
    {
        if (goalNetImpact != null)
        {
            goalNetImpact.gameObject.SetActive(false);
        }

        ResetGoalNetWarp();
    }

    private void UpdateGoalNetWarp(float intensity, Vector3 impactPoint)
    {
        Transform backdrop = transform.Find("BM8 Arcade Backdrop");
        if (backdrop == null)
        {
            return;
        }

        float visible = Mathf.Clamp01(intensity);
        for (int i = 0; i <= 6; i++)
        {
            Transform line = backdrop.Find("Net Vertical " + i);
            if (line == null)
            {
                continue;
            }

            float baseX = Mathf.Lerp(-3.05f, 3.05f, i / 6f);
            float pull = Mathf.Clamp01(1f - Mathf.Abs(baseX - impactPoint.x) / 2.4f);
            float wave = Mathf.Sin(Time.time * 38f + i * 0.72f) * 0.018f * visible;
            line.position = new Vector3(baseX + wave, 1.42f, 5.18f + 0.28f * visible * pull);
            line.localScale = new Vector3(0.01f, 2.18f + 0.2f * visible * pull, 0.01f);
        }

        for (int i = 0; i <= 5; i++)
        {
            Transform line = backdrop.Find("Net Horizontal " + i);
            if (line == null)
            {
                continue;
            }

            float baseY = Mathf.Lerp(0.38f, 2.48f, i / 5f);
            float pull = Mathf.Clamp01(1f - Mathf.Abs(baseY - impactPoint.y) / 1.05f);
            float wave = Mathf.Sin(Time.time * 42f + i * 0.61f) * 0.012f * visible;
            line.position = new Vector3(wave, baseY, 5.17f + 0.32f * visible * pull);
            line.localScale = new Vector3(6.12f + 0.34f * visible * pull, 0.01f, 0.01f);
        }
    }

    private void ResetGoalNetWarp()
    {
        Transform backdrop = transform.Find("BM8 Arcade Backdrop");
        if (backdrop == null)
        {
            return;
        }

        for (int i = 0; i <= 6; i++)
        {
            Transform line = backdrop.Find("Net Vertical " + i);
            if (line == null)
            {
                continue;
            }

            float x = Mathf.Lerp(-3.05f, 3.05f, i / 6f);
            line.position = new Vector3(x, 1.42f, 5.18f);
            line.localScale = new Vector3(0.01f, 2.18f, 0.01f);
        }

        for (int i = 0; i <= 5; i++)
        {
            Transform line = backdrop.Find("Net Horizontal " + i);
            if (line == null)
            {
                continue;
            }

            float y = Mathf.Lerp(0.38f, 2.48f, i / 5f);
            line.position = new Vector3(0f, y, 5.17f);
            line.localScale = new Vector3(6.12f, 0.01f, 0.01f);
        }
    }

    private void EnsureResultGoalFlash()
    {
        Transform existing = transform.Find("BM8 Result Goal Flash");
        resultGoalFlash = existing;
        if (resultGoalFlash == null)
        {
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Quad);
            flash.name = "BM8 Result Goal Flash";
            resultGoalFlash = flash.transform;
            resultGoalFlash.SetParent(transform, false);

            Collider collider = flash.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        resultGoalFlash.position = new Vector3(0f, 1.52f, 5.02f);
        resultGoalFlash.rotation = Quaternion.Euler(0f, 180f, 0f);
        resultGoalFlash.localScale = new Vector3(5.5f, 1.75f, 1f);

        Renderer renderer = resultGoalFlash.GetComponent<Renderer>();
        if (renderer != null)
        {
            resultGoalFlashMaterial = new Material(Shader.Find("Sprites/Default"));
            resultGoalFlashMaterial.color = new Color(1f, 1f, 1f, 0f);
            renderer.sharedMaterial = resultGoalFlashMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        HideResultGoalFlash();
    }

    private void ShowResultGoalFlash(Color color)
    {
        if (resultGoalFlash == null)
        {
            EnsureResultGoalFlash();
        }

        resultGoalFlashColor = color;
        resultGoalFlashUntil = Time.time + 0.94f;
        UpdateResultGoalFlash();
    }

    private void UpdateResultGoalFlash()
    {
        if (resultGoalFlash == null || resultGoalFlashMaterial == null)
        {
            return;
        }

        float remaining = resultGoalFlashUntil - Time.time;
        if (remaining <= 0f)
        {
            HideResultGoalFlash();
            return;
        }

        resultGoalFlash.gameObject.SetActive(true);
        float t = Mathf.Clamp01(remaining / 0.94f);
        float pulse = Mathf.Sin(t * Mathf.PI);
        float alpha = Mathf.Lerp(0.04f, 0.38f, pulse);
        resultGoalFlash.localScale = new Vector3(Mathf.Lerp(5.25f, 6.1f, pulse), Mathf.Lerp(1.62f, 2.12f, pulse), 1f);
        resultGoalFlashMaterial.color = new Color(resultGoalFlashColor.r, resultGoalFlashColor.g, resultGoalFlashColor.b, alpha);
    }

    private void HideResultGoalFlash()
    {
        if (resultGoalFlash != null)
        {
            resultGoalFlash.gameObject.SetActive(false);
        }
    }

    private void ApplyBallImpactScale(float intensity)
    {
        if (ball == null)
        {
            return;
        }

        float hit = Mathf.Clamp01(intensity);
        if (hit <= 0.001f)
        {
            ball.localScale = ballBaseScale;
            return;
        }

        float squash = Mathf.Lerp(1f, 0.82f, hit);
        float stretch = Mathf.Lerp(1f, 1.18f, hit);
        ball.localScale = new Vector3(ballBaseScale.x * stretch, ballBaseScale.y * squash, ballBaseScale.z * stretch);
    }

    private void EnsureArcadeBackdrop()
    {
        Transform backdrop = transform.Find("BM8 Arcade Backdrop");
        if (backdrop == null)
        {
            backdrop = new GameObject("BM8 Arcade Backdrop").transform;
            backdrop.SetParent(transform, false);
        }

        EnsureWorldBox(backdrop, "Back Wall", new Vector3(0f, 1.92f, 5.32f), new Vector3(8.7f, 3.35f, 0.08f), new Color(0.035f, 0.055f, 0.085f));
        EnsureWorldBox(backdrop, "Red Left Panel", new Vector3(-3.25f, 1.72f, 5.26f), new Vector3(1.65f, 2.75f, 0.1f), new Color(0.52f, 0.055f, 0.045f));
        EnsureWorldBox(backdrop, "Blue Center Panel", new Vector3(0f, 1.72f, 5.25f), new Vector3(2.25f, 2.75f, 0.1f), new Color(0.035f, 0.24f, 0.42f));
        EnsureWorldBox(backdrop, "Red Right Panel", new Vector3(3.25f, 1.72f, 5.26f), new Vector3(1.65f, 2.75f, 0.1f), new Color(0.52f, 0.055f, 0.045f));
        EnsureWorldBox(backdrop, "Top Light Band", new Vector3(0f, 3.55f, 5.22f), new Vector3(8.8f, 0.18f, 0.12f), new Color(0.62f, 0.52f, 0.12f));
        EnsureWorldBox(backdrop, "Left Ad Board", new Vector3(-4.35f, 0.55f, 3.8f), new Vector3(0.12f, 0.72f, 2.2f), new Color(0.03f, 0.03f, 0.035f));
        EnsureWorldBox(backdrop, "Right Ad Board", new Vector3(4.35f, 0.55f, 3.8f), new Vector3(0.12f, 0.72f, 2.2f), new Color(0.03f, 0.03f, 0.035f));

        for (int i = 0; i < 9; i++)
        {
            float x = Mathf.Lerp(-3.55f, 3.55f, i / 8f);
            EnsureWorldBox(backdrop, "Chase Light " + i, new Vector3(x, 3.42f, 5.08f), new Vector3(0.32f, 0.09f, 0.08f), i % 2 == 0 ? new Color(1f, 0.18f, 0.12f) : new Color(1f, 0.84f, 0.14f));
        }

        for (int i = 0; i <= 6; i++)
        {
            float x = Mathf.Lerp(-3.05f, 3.05f, i / 6f);
            EnsureWorldBox(backdrop, "Net Vertical " + i, new Vector3(x, 1.42f, 5.18f), new Vector3(0.01f, 2.18f, 0.01f), new Color(0.34f, 0.44f, 0.54f));
        }

        for (int i = 0; i <= 5; i++)
        {
            float y = Mathf.Lerp(0.38f, 2.48f, i / 5f);
            EnsureWorldBox(backdrop, "Net Horizontal " + i, new Vector3(0f, y, 5.17f), new Vector3(6.12f, 0.01f, 0.01f), new Color(0.34f, 0.44f, 0.54f));
        }
    }

    private void UpdateArcadeBackdropPulse()
    {
        Transform backdrop = transform.Find("BM8 Arcade Backdrop");
        if (backdrop == null)
        {
            return;
        }

        float time = Time.time;
        for (int i = 0; i < 9; i++)
        {
            Transform light = backdrop.Find("Chase Light " + i);
            if (light == null)
            {
                continue;
            }

            float x = Mathf.Lerp(-3.55f, 3.55f, i / 8f);
            float pulse = Mathf.Sin(time * 6.4f - i * 0.78f) * 0.5f + 0.5f;
            float hot = Mathf.SmoothStep(0.18f, 1f, pulse);
            light.position = new Vector3(x, 3.42f + hot * 0.035f, 5.08f);
            light.localScale = new Vector3(Mathf.Lerp(0.24f, 0.42f, hot), Mathf.Lerp(0.06f, 0.14f, hot), 0.08f);
            SetExistingMaterialColor(light, Color.Lerp(new Color(0.28f, 0.03f, 0.025f), i % 2 == 0 ? new Color(1f, 0.16f, 0.1f) : new Color(1f, 0.9f, 0.18f), hot));
        }

        Transform band = backdrop.Find("Top Light Band");
        if (band != null)
        {
            float bandPulse = Mathf.Sin(time * 3.2f) * 0.5f + 0.5f;
            SetExistingMaterialColor(band, Color.Lerp(new Color(0.26f, 0.19f, 0.055f), new Color(0.86f, 0.72f, 0.18f), bandPulse));
        }
    }

    private static void SetExistingMaterialColor(Transform part, Color color)
    {
        Renderer renderer = part != null ? part.GetComponent<Renderer>() : null;
        if (renderer == null || renderer.sharedMaterial == null)
        {
            return;
        }

        renderer.sharedMaterial.color = color;
    }

    private void HideSolidGoalNetBackdrop()
    {
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            string objectName = renderer.gameObject.name;
            string parentName = renderer.transform.parent != null ? renderer.transform.parent.name : "";
            Bounds bounds = renderer.bounds;
            bool largeGoalBackPlate = renderer.transform.position.z > 4.9f && bounds.size.x > 4.8f && bounds.size.y > 1.2f;
            if (objectName.Contains("Net Back") || objectName.Contains("Goal Net") || parentName.Contains("Net Back") || largeGoalBackPlate)
            {
                renderer.enabled = false;
            }
        }
    }

    private static void EnsureWorldBox(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject box = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.position = position;
        box.transform.rotation = Quaternion.identity;
        box.transform.localScale = scale;
        Collider collider = box.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        SetMaterialColor(box.transform, color);
    }

    private void CreateNineTargetGrid()
    {
        EnsureEventSystem();

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Match UI");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        canvas.sortingOrder = 100;

        Transform oldGrid = canvas.transform.Find("Goal 9 Grid");
        if (oldGrid != null)
        {
            Destroy(oldGrid.gameObject);
        }

        HideLegacyControl(canvas.transform, "Left Button");
        HideLegacyControl(canvas.transform, "Center Button");
        HideLegacyControl(canvas.transform, "Right Button");
        HideLegacyControl(canvas.transform, "Shoot Button");
        HideLegacyControl(canvas.transform, "Reset Button");
        DestroyRuntimeControl(canvas.transform, "Test All Keeper Zones Button");
        DestroyRuntimeControl(canvas.transform, "Test Top Keeper Zones Button");
        SetLegacyText(canvas.transform, "Hint", "");

        GameObject gridObject = new GameObject("Goal 9 Grid");
        gridObject.transform.SetParent(canvas.transform, false);
        RectTransform grid = gridObject.AddComponent<RectTransform>();
        grid.anchorMin = new Vector2(0.5f, 0.5f);
        grid.anchorMax = new Vector2(0.5f, 0.5f);
        grid.anchoredPosition = new Vector2(0f, 0f);
        grid.sizeDelta = new Vector2(360f, 145f);
        goalGrid = grid;

        Image frame = gridObject.AddComponent<Image>();
        frame.color = new Color(0f, 0f, 0f, 0.04f);
        frame.raycastTarget = false;

        string[] labels =
        {
            "TL", "TC", "TR",
            "ML", "MC", "MR",
            "BL", "BC", "BR"
        };
        float[] xs = { -2.25f, 0f, 2.25f };
        float[] ys = { 2.35f, 1.65f, 1.0f };

        int index = 0;
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                float x = xs[col];
                float y = ys[row];
                Button button = CreateRuntimeTargetButton(gridObject.transform, labels[index], col, row);
                int targetCol = col;
                int targetRow = row;
                button.onClick.AddListener(() => ShootAt(targetCol, targetRow));
                index++;
            }
        }

        gridObject.transform.SetAsLastSibling();
        UpdateGoalGridOverlay();
    }

    private void HideLegacyTextOverlay()
    {
        if (statusText != null)
        {
            statusText.enabled = false;
        }

        if (scoreText != null)
        {
            scoreText.enabled = false;
        }
    }

    private static Button CreateRuntimeTargetButton(Transform parent, string label, int col, int row)
    {
        GameObject buttonObject = new GameObject("Target " + label);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.045f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.045f);
        colors.highlightedColor = new Color(1f, 0.92f, 0.25f, 0.24f);
        colors.pressedColor = new Color(1f, 0.82f, 0.08f, 0.42f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(148f, 70f);
        rect.anchoredPosition = new Vector2((col - 1) * 156f, (1 - row) * 78f);

        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(buttonObject.transform, false);
        Text text = textObject.AddComponent<Text>();
        text.text = "";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 1;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }

    private void UpdateGoalGridOverlay()
    {
        if (goalGrid == null || Camera.main == null)
        {
            return;
        }

        RectTransform canvasRect = goalGrid.parent as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        Vector3 leftBottom = Camera.main.WorldToScreenPoint(new Vector3(-3.05f, 0.32f, 5.05f));
        Vector3 rightTop = Camera.main.WorldToScreenPoint(new Vector3(3.05f, 2.5f, 5.05f));
        if (leftBottom.z < 0f || rightTop.z < 0f)
        {
            return;
        }

        Vector2 screenMin = new Vector2(Mathf.Min(leftBottom.x, rightTop.x), Mathf.Min(leftBottom.y, rightTop.y));
        Vector2 screenMax = new Vector2(Mathf.Max(leftBottom.x, rightTop.x), Mathf.Max(leftBottom.y, rightTop.y));
        Vector2 screenCenter = (screenMin + screenMax) * 0.5f;
        Vector2 screenSize = screenMax - screenMin;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenCenter, null, out Vector2 localCenter);
        float width = Mathf.Clamp(screenSize.x * canvasRect.rect.width / Screen.width, 260f, 560f);
        float height = Mathf.Clamp(screenSize.y * canvasRect.rect.height / Screen.height, 115f, 260f);

        goalGrid.anchoredPosition = localCenter;
        goalGrid.sizeDelta = new Vector2(width, height);

        for (int i = 0; i < goalGrid.childCount; i++)
        {
            RectTransform cell = goalGrid.GetChild(i) as RectTransform;
            if (cell == null)
            {
                continue;
            }

            int col = i % 3;
            int row = i / 3;
            cell.sizeDelta = new Vector2(width / 3f - 4f, height / 3f - 4f);
            cell.anchoredPosition = new Vector2((col - 1) * width / 3f, (1 - row) * height / 3f);
        }
    }

    private void PickKeeperGrid()
    {
        float readChance = UseAaAnimatedKeeper ? 0.62f : 0.5f;
        if (UnityEngine.Random.value < readChance)
        {
            keeperCol = aimCol;
            keeperRow = aimRow;
            return;
        }

        keeperCol = UnityEngine.Random.Range(0, 3);
        keeperRow = UnityEngine.Random.Range(0, 3);
        if (keeperCol == aimCol && keeperRow == aimRow)
        {
            keeperCol = (keeperCol + 1) % 3;
        }
    }

    private bool ShouldKeeperSave(float power)
    {
        if (keeperCol != aimCol || keeperRow != aimRow)
        {
            return false;
        }

        float chance = UseAaAnimatedKeeper ? 0.72f : 0.56f;
        if (aimRow == 0)
        {
            chance += 0.06f;
        }
        else if (aimRow == 2)
        {
            chance += UseAaAnimatedKeeper ? -0.06f : 0.08f;
        }

        chance += Mathf.InverseLerp(0.35f, 1f, power) * (UseAaAnimatedKeeper ? 0.06f : 0.12f);
        return UnityEngine.Random.value < Mathf.Clamp01(chance);
    }

    private static float GridX(int col)
    {
        return Mathf.Lerp(-2.25f, 2.25f, Mathf.Clamp01(col / 2f));
    }

    private static float GridY(int row)
    {
        return row == 0 ? 2.35f : row == 1 ? 1.65f : 1.0f;
    }

    private Vector3 KeeperDiveTarget(int col, int row)
    {
        float x = GridX(col) * (col == 1 ? 0.18f : IsStandingBlockRow(row) ? 0.2f : 0.82f);
        float y = keeperStart.y + (row == 0 ? 0.58f : row == 1 ? 0.18f : -0.02f);
        float z = keeperStart.z + (col == 1 ? 0.22f : row == 2 ? -0.42f : -0.1f);
        return new Vector3(x, y, z);
    }

    private static bool IsStandingBlockRow(int row)
    {
        return row <= 1;
    }

    private static Quaternion KeeperEndRotation(float side, float rowHeight, bool bottomRow, bool centerDive)
    {
        if (centerDive)
        {
            if (rowHeight > 0.8f)
            {
                return Quaternion.Euler(-64f, 0f, 0f);
            }

            if (bottomRow)
            {
                return Quaternion.Euler(34f, 0f, 0f);
            }

            return Quaternion.Euler(-12f, 0f, 0f);
        }

        if (bottomRow)
        {
            return Quaternion.Euler(22f, 0f, -side * 124f);
        }

        if (rowHeight > 0.8f)
        {
            return Quaternion.Euler(-50f, 0f, -side * 96f);
        }

        return Quaternion.Euler(-14f, 0f, -side * 116f);
    }

    private static float KeeperArmPitch(bool activeArm, float rowHeight)
    {
        if (!activeArm)
        {
            return rowHeight > 0.8f ? -82f : rowHeight > 0.25f ? -48f : -24f;
        }

        if (rowHeight > 0.8f)
        {
            return -176f;
        }

        if (rowHeight > 0.25f)
        {
            return -118f;
        }

        return -36f;
    }

    private static string GridName(int col, int row)
    {
        string vertical = row == 0 ? "T" : row == 1 ? "M" : "B";
        string horizontal = col == 0 ? "L" : col == 1 ? "C" : "R";
        return vertical + horizontal;
    }

    private static string KeeperActionName(int col, int row)
    {
        if (row == 0)
        {
            return col == 1 ? "HIGH CATCH" : "TOP TIP";
        }

        if (row == 2)
        {
            return col == 1 ? "LOW BLOCK" : "LOW SAVE";
        }

        return col == 1 ? "BODY CATCH" : "SIDE PUNCH";
    }

    private Vector3 SavePalmWorld()
    {
        Vector3 humanoidPalm;
        if (TryGetHumanoidSavePalm(out humanoidPalm))
        {
            return humanoidPalm;
        }

        float x = GridX(keeperCol) * (keeperCol == 1 ? 0.24f : 0.98f);
        float y = GridY(keeperRow) + (keeperRow == 0 ? 0.34f : keeperRow == 1 ? 0.1f : -0.1f);
        float z = keeperRow == 0 ? 3.56f : keeperRow == 1 ? 3.7f : 3.86f;
        return new Vector3(x, y, z);
    }

    private Vector3 AaKeeperContactWorld()
    {
        float xScale = keeperCol == 1 ? 0.12f : keeperRow == 0 ? 0.68f : keeperRow == 2 ? 0.74f : 0.82f;
        float x = GridX(keeperCol) * xScale;
        float y = GridY(keeperRow) + (keeperRow == 0 ? 0.42f : keeperRow == 1 ? 0.06f : -0.06f);
        float z = keeperStart.z - (keeperRow == 2 ? 0.72f : 0.86f);
        return new Vector3(x, y, z);
    }

    private float AaContactTime()
    {
        if (keeperRow == 0)
        {
            return keeperCol == 1 ? 0.3f : 0.35f;
        }

        if (keeperRow == 2)
        {
            return keeperCol == 1 ? 0.26f : 0.29f;
        }

        return keeperCol == 1 ? 0.27f : 0.31f;
    }

    private float AaPunchWindow()
    {
        if (keeperRow == 0)
        {
            return keeperCol == 1 ? 0.15f : 0.18f;
        }

        if (keeperRow == 2)
        {
            return keeperCol == 1 ? 0.16f : 0.18f;
        }

        return keeperCol == 1 ? 0.13f : 0.17f;
    }

    private Vector3 AaPalmLoadOffset()
    {
        float side = keeperCol == 1 ? saveReboundSide : Mathf.Sign(keeperCol - 1f);
        if (keeperRow == 0)
        {
            return new Vector3(side * 0.035f, 0.015f, -0.055f);
        }

        float y = keeperRow == 0 ? -0.06f : keeperRow == 2 ? -0.025f : -0.04f;
        return new Vector3(-side * 0.08f, y, -0.06f);
    }

    private Vector3 AaDeflectWorld(Vector3 contact, float reboundSide)
    {
        float sidePush = keeperRow == 0 ? UnityEngine.Random.Range(1.65f, 2.18f) : keeperRow == 2 ? UnityEngine.Random.Range(2.15f, 2.85f) : UnityEngine.Random.Range(2.55f, 3.35f);
        float upPush = keeperRow == 0 ? UnityEngine.Random.Range(0.36f, 0.62f) : keeperRow == 2 ? UnityEngine.Random.Range(0.22f, 0.5f) : UnityEngine.Random.Range(0.72f, 1.08f);
        float zPush = keeperRow == 0 ? UnityEngine.Random.Range(-3.95f, -3.25f) : keeperRow == 2 ? UnityEngine.Random.Range(-3.45f, -2.85f) : UnityEngine.Random.Range(-3.7f, -2.8f);
        return new Vector3(
            Mathf.Clamp(contact.x + reboundSide * sidePush, -3.75f, 3.75f),
            Mathf.Clamp(contact.y + upPush, 0.72f, 3.55f),
            zPush);
    }

    private float AaShotArcHeight(bool saved)
    {
        if (!UseAaAnimatedKeeper)
        {
            return saved ? 0.74f : 1.36f;
        }

        if (keeperRow == 0)
        {
            return 0.5f;
        }

        if (keeperRow == 2)
        {
            return 0.18f;
        }

        return 0.34f;
    }

    private float AaDeflectArcHeight()
    {
        if (keeperRow == 0)
        {
            return 0.46f;
        }

        if (keeperRow == 2)
        {
            return 0.34f;
        }

        return 1.05f;
    }

    private Vector3 StandingBlockContactWorld()
    {
        float x = GridX(keeperCol) * (keeperCol == 1 ? 0.12f : 0.2f);
        float y = keeperRow == 0 ? GridY(0) + 0.18f : keeperRow == 1 ? GridY(1) + 0.16f : GridY(2) + 0.18f;
        return new Vector3(x, y, 3.5f);
    }

    private bool TryGetHumanoidSavePalm(out Vector3 palm)
    {
        palm = Vector3.zero;
        if (!UseAaAnimatedKeeper || keeperAnimator == null || !keeperAnimator.isHuman)
        {
            return false;
        }

        Transform leftHand = keeperAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
        Transform rightHand = keeperAnimator.GetBoneTransform(HumanBodyBones.RightHand);
        if (leftHand == null && rightHand == null)
        {
            return false;
        }

        if (keeperCol == 0 && leftHand != null)
        {
            palm = leftHand.position;
        }
        else if (keeperCol == 2 && rightHand != null)
        {
            palm = rightHand.position;
        }
        else if (leftHand != null && rightHand != null)
        {
            palm = Vector3.Lerp(leftHand.position, rightHand.position, 0.5f);
        }
        else
        {
            palm = leftHand != null ? leftHand.position : rightHand.position;
        }

        palm += new Vector3(0f, keeperRow == 0 ? 0.1f : keeperRow == 2 ? -0.06f : 0.02f, -0.06f);
        return true;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static void HideLegacyControl(Transform parent, string name)
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == name)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private static void DestroyRuntimeControl(Transform parent, string name)
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == name)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private static void SetLegacyText(Transform parent, string name, string value)
    {
        Transform target = parent.Find(name);
        if (target == null)
        {
            return;
        }

        Text text = target.GetComponent<Text>();
        if (text != null)
        {
            text.text = value;
        }
    }

    private void ResetPose()
    {
        SetLocalRotation(strikerLeftLeg, 0f, 0f, 0f);
        SetLocalRotation(strikerRightLeg, 0f, 0f, 0f);
        SetLocalRotation(strikerLeftArm, 0f, 0f, 0f);
        SetLocalRotation(strikerRightArm, 0f, 0f, 0f);
        SetLocalRotation(strikerTorso, 0f, 0f, 0f);
        SetLocalRotation(strikerHead, 0f, 0f, 0f);
        SetLocalPosition(strikerTorso, new Vector3(0f, 1.12f, 0f));
        SetLocalPosition(strikerHead, new Vector3(0f, 1.86f, 0f));
        SetLocalPosition(strikerLeftLeg, new Vector3(-0.15f, 0.27f, 0f));
        SetLocalPosition(strikerRightLeg, new Vector3(0.15f, 0.27f, 0f));
        SetLocalPosition(strikerLeftArm, new Vector3(-0.39f, 1.14f, 0.01f));
        SetLocalPosition(strikerRightArm, new Vector3(0.39f, 1.14f, 0.01f));
        SetLocalPosition(strikerVisibleModel, new Vector3(0f, 0f, -0.02f));
        if (strikerVisibleModel != null)
        {
            strikerVisibleModel.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }

        AnchorImportedKeeperVisibleModel();
        if (usingKeeperSpriteSheet && keeperSprite != null)
        {
            keeperSprite.localPosition = new Vector3(0f, 1.28f, -0.52f);
            keeperSprite.localRotation = Quaternion.Euler(0f, 180f, 0f);
            keeperSprite.localScale = new Vector3(1.35f, 1.95f, 1f);
            SetKeeperSpriteFrame(0, 0, false);
        }
        PlayKeeperController(keeperIdleController);

        SetLocalRotation(keeperLeftArm, 0f, 0f, 0f);
        SetLocalRotation(keeperRightArm, 0f, 0f, 0f);
        SetLocalRotation(keeperLeftLeg, 0f, 0f, 0f);
        SetLocalRotation(keeperRightLeg, 0f, 0f, 0f);
        SetLocalRotation(keeperTorso, 0f, 0f, 0f);
        SetLocalRotation(keeperHead, 0f, 0f, 0f);
        SetLocalPosition(keeperTorso, new Vector3(0f, 1.16f, 0f));
        SetLocalPosition(keeperHead, new Vector3(0f, 1.9f, -0.02f));
        SetLocalPosition(keeperLeftLeg, new Vector3(-0.16f, 0.27f, 0f));
        SetLocalPosition(keeperRightLeg, new Vector3(0.16f, 0.27f, 0f));
        SetLocalPosition(keeperLeftArm, new Vector3(-0.46f, 1.15f, 0.01f));
        SetLocalPosition(keeperRightArm, new Vector3(0.46f, 1.15f, 0.01f));
        SetLocalPosition(keeperLeftGlove, new Vector3(-0.43f, 1.13f, -0.18f));
        SetLocalPosition(keeperRightGlove, new Vector3(0.43f, 1.13f, -0.18f));
        if (keeperLeftGlove != null)
        {
            keeperLeftGlove.localScale = Vector3.one * 0.26f;
        }

        if (keeperRightGlove != null)
        {
            keeperRightGlove.localScale = Vector3.one * 0.26f;
        }
    }

    private void BlendKeeperPoseToReady(float t)
    {
        SetLocalRotation(keeperLeftArm, 0f, 0f, 0f);
        SetLocalRotation(keeperRightArm, 0f, 0f, 0f);
        SetLocalRotation(keeperLeftLeg, 0f, 0f, 0f);
        SetLocalRotation(keeperRightLeg, 0f, 0f, 0f);
        SetLocalRotation(keeperTorso, 0f, 0f, 0f);
        SetLocalRotation(keeperHead, 0f, 0f, 0f);
        SetLocalPosition(keeperTorso, Vector3.Lerp(LocalOrDefault(keeperTorso, new Vector3(0f, 1.16f, 0f)), new Vector3(0f, 1.16f, 0f), t));
        SetLocalPosition(keeperHead, Vector3.Lerp(LocalOrDefault(keeperHead, new Vector3(0f, 1.9f, -0.02f)), new Vector3(0f, 1.9f, -0.02f), t));
        SetLocalPosition(keeperLeftLeg, Vector3.Lerp(LocalOrDefault(keeperLeftLeg, new Vector3(-0.16f, 0.27f, 0f)), new Vector3(-0.16f, 0.27f, 0f), t));
        SetLocalPosition(keeperRightLeg, Vector3.Lerp(LocalOrDefault(keeperRightLeg, new Vector3(0.16f, 0.27f, 0f)), new Vector3(0.16f, 0.27f, 0f), t));
        SetLocalPosition(keeperLeftArm, Vector3.Lerp(LocalOrDefault(keeperLeftArm, new Vector3(-0.46f, 1.15f, 0.01f)), new Vector3(-0.46f, 1.15f, 0.01f), t));
        SetLocalPosition(keeperRightArm, Vector3.Lerp(LocalOrDefault(keeperRightArm, new Vector3(0.46f, 1.15f, 0.01f)), new Vector3(0.46f, 1.15f, 0.01f), t));
        SetLocalPosition(keeperLeftGlove, Vector3.Lerp(LocalOrDefault(keeperLeftGlove, new Vector3(-0.43f, 1.13f, -0.18f)), new Vector3(-0.43f, 1.13f, -0.18f), t));
        SetLocalPosition(keeperRightGlove, Vector3.Lerp(LocalOrDefault(keeperRightGlove, new Vector3(0.43f, 1.13f, -0.18f)), new Vector3(0.43f, 1.13f, -0.18f), t));
        SetLocalPosition(keeperVisibleModel, Vector3.Lerp(LocalOrDefault(keeperVisibleModel, new Vector3(0f, 0f, -0.02f)), new Vector3(0f, 0f, -0.02f), t));
    }

    private static void SetLocalPosition(Transform target, Vector3 position)
    {
        if (target != null)
        {
            target.localPosition = position;
        }
    }

    private static double WallClockSeconds()
    {
        return (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds;
    }

    private static void SetGloveWorld(Transform target, Vector3 position, float scale)
    {
        if (target == null)
        {
            return;
        }

        target.position = position;
        target.localScale = Vector3.one * scale;
    }

    private static Vector3 LocalOrDefault(Transform target, Vector3 fallback)
    {
        return target != null ? target.localPosition : fallback;
    }

    private static void SetLocalRotation(Transform target, float x, float y, float z)
    {
        if (target != null)
        {
            target.localRotation = Quaternion.Euler(x, y, z);
        }
    }

    private static float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private static float EaseOut(float t, float power)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, power);
    }

    private static Vector3 ReadyCameraPosition()
    {
        return UseArcadeVideoCamera ? new Vector3(0f, 1.95f, -4.85f) : new Vector3(0f, 3.05f, -7.1f);
    }

    private static Quaternion ReadyCameraRotation()
    {
        return UseArcadeVideoCamera ? Quaternion.Euler(6.5f, 0f, 0f) : Quaternion.Euler(13.5f, 0f, 0f);
    }

    private Vector3 ShotCameraPosition(float t, float impact, bool saved, float reboundSide)
    {
        Vector3 ready = ReadyCameraPosition();
        Vector3 follow = new Vector3(aimX * 0.2f, saved ? 2.2f : 2.1f, saved ? -4.08f : -4.42f);
        float pushIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, saved ? 0.44f : 0.58f, t));
        Vector3 camera = Vector3.Lerp(ready, follow, pushIn);
        camera.x += reboundSide * impact * (saved ? 0.22f : 0.1f);
        camera.y += impact * (saved ? 0.1f : 0.05f);
        camera.z += impact * (saved ? 0.62f : 0.34f);
        return camera;
    }

    private Quaternion ShotCameraRotation(float t, float impact, float reboundSide)
    {
        float pushIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.5f, t));
        float basePitch = Mathf.Lerp(6.5f, 9.25f, pushIn);
        float shotLift = Mathf.Sin(t * Mathf.PI) * 2.15f;
        float yaw = aimX * 1.45f + reboundSide * impact * 2.2f;
        return Quaternion.Euler(basePitch + shotLift - impact * 2.45f, yaw, reboundSide * impact * 0.85f);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void UpdateScore()
    {
        if (scoreText != null)
        {
            scoreText.text = "Goals " + goals + "   Saves " + saves + "   Shots " + shotCount;
        }
    }
}
