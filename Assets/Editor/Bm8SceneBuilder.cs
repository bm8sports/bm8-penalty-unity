using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class Bm8SceneBuilder
{
    private const string StylizedKeeperPath = "Assets/Art/Characters/goalkeeper-stylized-rig-and-animation/source/ThuMon/Goalkeeper_TPose.FBX";
    private const string RobotKeeperPath = "Assets/animo/AA_Soccer_Goalkeeper/Prefabs/Robot.prefab";
    private const string AaControllerFolder = "Assets/animo/AA_Soccer_Goalkeeper/Controller/";
    private const string RuntimeTestRequestKey = "BM8.KeeperRuntimeTest.Requested";

    private static readonly string[] RequiredKeeperControllers =
    {
        "AA_Soccer_Goal_Idel",
        "AA_Soccer_Goal_CatchBall_UP_Succ",
        "AA_Soccer_Goal_HitBall_UP_Fail",
        "AA_Soccer_Goal_CatchBall_L_Succ",
        "AA_Soccer_Goal_HitBall_L_Fail",
        "AA_Soccer_Goal_CatchBall_R_Succ",
        "AA_Soccer_Goal_HitBall_R_Fail",
        "AA_Soccer_Goal_CatchBall_F_Succ",
        "AA_Soccer_Goal_HitBall_F_Fail",
        "AA_Soccer_Goal_HoldBall_LD",
        "AA_Soccer_Goal_Down_LD",
        "AA_Soccer_Goal_HoldBall_RD",
        "AA_Soccer_Goal_Down_RD",
        "AA_Soccer_Goal_LHandHit_UL",
        "AA_Soccer_Goal_RHandHit_UR",
        "AA_Soccer_Goal_HitBall_TL_Succ",
        "AA_Soccer_Goal_HitBall_TL_Fail",
        "AA_Soccer_Goal_HitBall_TR_Succ",
        "AA_Soccer_Goal_HitBall_TR_Fail"
    };

    [MenuItem("BM8/Open Penalty Prototype Scene")]
    public static void OpenPrototypeScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/BM8PenaltyPrototype.unity", OpenSceneMode.Single);
        Debug.Log("BM8 penalty prototype scene opened.");
    }

    [MenuItem("BM8/Validate Goalkeeper Animation Setup")]
    public static void ValidateGoalkeeperAnimationSetup()
    {
        int issues = 0;
        string report = "BM8 goalkeeper animation validation:\n";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(StylizedKeeperPath) == null)
        {
            issues++;
            report += "- Missing uploaded stylized goalkeeper: " + StylizedKeeperPath + "\n";
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(RobotKeeperPath) == null)
        {
            issues++;
            report += "- Missing AA goalkeeper robot prefab: " + RobotKeeperPath + "\n";
        }

        for (int i = 0; i < RequiredKeeperControllers.Length; i++)
        {
            if (LoadKeeperController(RequiredKeeperControllers[i]) == null)
            {
                issues++;
                report += "- Missing controller: " + RequiredKeeperControllers[i] + "\n";
            }
        }

        Bm8PenaltyPrototype prototype = Object.FindAnyObjectByType<Bm8PenaltyPrototype>();
        if (prototype == null)
        {
            issues++;
            report += "- BM8PenaltyPrototype component not found in the open scene.\n";
        }
        else
        {
            SerializedObject serialized = new SerializedObject(prototype);
            issues += ValidateControllerProperty(serialized, "keeperIdleController", ref report);
            issues += ValidateControllerProperty(serialized, "keeperCatchForwardSuccessController", ref report);
            issues += ValidateControllerProperty(serialized, "keeperCatchForwardFailController", ref report);
            issues += ValidateControllerProperty(serialized, "keeperCatchUpSuccessController", ref report);
            issues += ValidateControllerProperty(serialized, "keeperCatchUpFailController", ref report);
            issues += ValidateControllerProperty(serialized, "keeperCatchLeftDownSuccessController", ref report);
            issues += ValidateControllerProperty(serialized, "keeperCatchLeftDownFailController", ref report);
            issues += ValidateControllerProperty(serialized, "keeperCatchRightDownSuccessController", ref report);
            issues += ValidateControllerProperty(serialized, "keeperCatchRightDownFailController", ref report);
            issues += ValidateControllerProperty(serialized, "keeperHitLeftSuccessController", ref report);
            issues += ValidateControllerProperty(serialized, "keeperHitLeftFailController", ref report);
            issues += ValidateControllerProperty(serialized, "keeperHitRightSuccessController", ref report);
            issues += ValidateControllerProperty(serialized, "keeperHitRightFailController", ref report);
            issues += ValidateControllerProperty(serialized, "keeperHitTopLeftSuccessController", ref report);
            issues += ValidateControllerProperty(serialized, "keeperHitTopLeftFailController", ref report);
            issues += ValidateControllerProperty(serialized, "keeperHitTopRightSuccessController", ref report);
            issues += ValidateControllerProperty(serialized, "keeperHitTopRightFailController", ref report);
        }

        GameObject keeper = GameObject.Find("Goalkeeper");
        if (keeper == null)
        {
            issues++;
            report += "- Goalkeeper object not found in the open scene.\n";
        }
        else
        {
            Animator animator = keeper.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                issues++;
                report += "- Goalkeeper has no Animator in its children.\n";
            }
            else if (!animator.isHuman)
            {
                issues++;
                report += "- Goalkeeper Animator is not Humanoid; AA animations may not retarget.\n";
            }
        }

        if (issues == 0)
        {
            Debug.Log(report + "- OK: all required goalkeeper animation assets and scene references are present.");
        }
        else
        {
            Debug.LogError(report + "- Issues found: " + issues);
        }
    }

    [MenuItem("BM8/Repair Goalkeeper Animation References")]
    public static void RepairGoalkeeperAnimationReferences()
    {
        Bm8PenaltyPrototype prototype = Object.FindAnyObjectByType<Bm8PenaltyPrototype>();
        if (prototype == null)
        {
            Debug.LogError("BM8PenaltyPrototype component not found. Open BM8PenaltyPrototype scene first.");
            return;
        }

        GameObject keeper = GameObject.Find("Goalkeeper");
        if (keeper == null)
        {
            Debug.LogError("Goalkeeper object not found. Open BM8PenaltyPrototype scene first.");
            return;
        }

        Animator animator = keeper.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = LoadKeeperController("AA_Soccer_Goal_Idel");
        }

        SerializedObject serialized = new SerializedObject(prototype);
        serialized.FindProperty("keeperAnimator").objectReferenceValue = animator;
        AssignKeeperControllers(serialized);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(prototype);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("BM8 goalkeeper animation references repaired. Run BM8/Validate Goalkeeper Animation Setup to verify.");
    }

    [MenuItem("BM8/Run Keeper Runtime Test")]
    public static void RunKeeperRuntimeTest()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Stop Play Mode before starting the BM8 keeper runtime test.");
            return;
        }

        OpenPrototypeScene();
        if (!ValidateKeeperDirectionContract())
        {
            return;
        }

        RepairGoalkeeperAnimationReferences();
        EditorPrefs.SetBool(RuntimeTestRequestKey, true);
        EditorApplication.isPlaying = true;
        Debug.Log("BM8 keeper runtime test requested. Unity will run TEST 9 and TEST TOP automatically.");
    }

    [MenuItem("BM8/Validate Keeper Direction Contract")]
    public static bool ValidateKeeperDirectionContract()
    {
        MethodInfo expectedController = typeof(Bm8PenaltyPrototype).GetMethod(
            "ExpectedAaKeeperControllerName",
            BindingFlags.Static | BindingFlags.NonPublic);
        if (expectedController == null)
        {
            Debug.LogError("BM8 keeper direction contract failed: ExpectedAaKeeperControllerName was not found.");
            return false;
        }

        int issues = 0;
        issues += ValidateExpectedController(expectedController, 0, 0, true, "AA_Soccer_Goal_RHandHit_UR");
        issues += ValidateExpectedController(expectedController, 2, 0, true, "AA_Soccer_Goal_LHandHit_UL");
        issues += ValidateExpectedController(expectedController, 0, 0, false, "AA_Soccer_Goal_HitBall_TR_Fail");
        issues += ValidateExpectedController(expectedController, 2, 0, false, "AA_Soccer_Goal_HitBall_TL_Fail");
        issues += ValidateExpectedController(expectedController, 0, 1, true, "AA_Soccer_Goal_CatchBall_R_Succ");
        issues += ValidateExpectedController(expectedController, 2, 1, true, "AA_Soccer_Goal_CatchBall_L_Succ");
        issues += ValidateExpectedController(expectedController, 0, 1, false, "AA_Soccer_Goal_HitBall_R_Fail");
        issues += ValidateExpectedController(expectedController, 2, 1, false, "AA_Soccer_Goal_HitBall_L_Fail");
        issues += ValidateExpectedController(expectedController, 0, 2, true, "AA_Soccer_Goal_HoldBall_RD");
        issues += ValidateExpectedController(expectedController, 2, 2, true, "AA_Soccer_Goal_HoldBall_LD");
        issues += ValidateExpectedController(expectedController, 0, 2, false, "AA_Soccer_Goal_Down_RD");
        issues += ValidateExpectedController(expectedController, 2, 2, false, "AA_Soccer_Goal_Down_LD");

        if (issues > 0)
        {
            Debug.LogError("BM8 keeper direction contract failed with " + issues + " issue(s). Goal-grid L/R must be mirrored to goalkeeper-avatar R/L.");
            return false;
        }

        Debug.Log("BM8 keeper direction contract passed.");
        return true;
    }

    private static int ValidateExpectedController(MethodInfo expectedController, int col, int row, bool save, string expected)
    {
        string actual = expectedController.Invoke(null, new object[] { col, row, save }) as string;
        if (actual == expected)
        {
            return 0;
        }

        Debug.LogError("BM8 keeper direction contract failed: col " + col + ", row " + row + ", save " + save + " expected " + expected + ", got " + (string.IsNullOrEmpty(actual) ? "<none>" : actual));
        return 1;
    }

    [InitializeOnLoad]
    private static class KeeperRuntimeTestRunner
    {
        private const double SceneReadyTimeoutSeconds = 15d;
        private const double StartDelaySeconds = 0.35d;
        private const double FullGridTimeoutSeconds = 90d;
        private const double TopGridTimeoutSeconds = 45d;
        private const double KeeperActionFreezeGraceSeconds = 0.58d;

        private enum RunnerState
        {
            Idle,
            WaitForScene,
            WaitForReady,
            WaitForFullGrid,
            WaitForTopGrid
        }

        private static RunnerState state = RunnerState.Idle;
        private static Bm8PenaltyPrototype prototype;
        private static double stageStartedAt;
        private static double savedStatusStartedAt = -1d;

        static KeeperRuntimeTestRunner()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += Update;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && EditorPrefs.GetBool(RuntimeTestRequestKey, false))
            {
                state = RunnerState.WaitForScene;
                stageStartedAt = EditorApplication.timeSinceStartup;
                prototype = null;
                savedStatusStartedAt = -1d;
            }

            if (change == PlayModeStateChange.EnteredEditMode && !EditorPrefs.GetBool(RuntimeTestRequestKey, false))
            {
                state = RunnerState.Idle;
                prototype = null;
                savedStatusStartedAt = -1d;
            }
        }

        private static void Update()
        {
            if (!EditorApplication.isPlaying || !EditorPrefs.GetBool(RuntimeTestRequestKey, false))
            {
                return;
            }

            if (state == RunnerState.Idle)
            {
                state = RunnerState.WaitForScene;
                stageStartedAt = EditorApplication.timeSinceStartup;
                savedStatusStartedAt = -1d;
            }

            if (state == RunnerState.WaitForScene)
            {
                prototype = Object.FindAnyObjectByType<Bm8PenaltyPrototype>();
                if (prototype != null)
                {
                    Time.timeScale = 1f;
                    state = RunnerState.WaitForReady;
                    stageStartedAt = EditorApplication.timeSinceStartup;
                    savedStatusStartedAt = -1d;
                    Debug.Log("BM8 keeper runtime test: scene ready; waiting for game startup.");
                    return;
                }

                if (ElapsedSeconds() > SceneReadyTimeoutSeconds)
                {
                    FailRuntimeTest("BM8PenaltyPrototype was not found after entering Play Mode.");
                }

                return;
            }

            if (state == RunnerState.WaitForReady)
            {
                if (ElapsedSeconds() >= StartDelaySeconds && IsReadyToStartRuntimeTest())
                {
                    prototype.RunKeeperZoneTest();
                    state = RunnerState.WaitForFullGrid;
                    stageStartedAt = EditorApplication.timeSinceStartup;
                    savedStatusStartedAt = -1d;
                    Debug.Log("BM8 keeper runtime test: running TEST 9.");
                    return;
                }

                if (ElapsedSeconds() > SceneReadyTimeoutSeconds)
                {
                    FailRuntimeTest("Game did not become ready for testing. Status: " + CurrentStatusText());
                }

                return;
            }

            if (state == RunnerState.WaitForFullGrid)
            {
                if (StatusEquals("Test complete"))
                {
                    prototype.RunTopKeeperZoneTest();
                    state = RunnerState.WaitForTopGrid;
                    stageStartedAt = EditorApplication.timeSinceStartup;
                    savedStatusStartedAt = -1d;
                    Debug.Log("BM8 keeper runtime test: TEST 9 complete; running TEST TOP.");
                    return;
                }

                if (KeeperActionRepeatingAfterSave("TEST 9"))
                {
                    return;
                }

                if (StatusContains("timeout") || StatusContains("watchdog") || StatusContains("mismatch") || StatusContains("motion"))
                {
                    FailRuntimeTest("TEST 9 reported a runtime problem. Status: " + CurrentStatusText() + ". Controller: " + CurrentKeeperControllerName() + ". Motion: " + CurrentKeeperMotionViolation());
                    return;
                }

                if (ElapsedSeconds() > FullGridTimeoutSeconds)
                {
                    FailRuntimeTest("TEST 9 did not complete within " + FullGridTimeoutSeconds + " seconds. Status: " + CurrentStatusText() + ". Controller: " + CurrentKeeperControllerName() + ". Motion: " + CurrentKeeperMotionViolation());
                }

                return;
            }

            if (state == RunnerState.WaitForTopGrid)
            {
                if (StatusEquals("Top test complete"))
                {
                    EditorPrefs.DeleteKey(RuntimeTestRequestKey);
                    Debug.Log("BM8 keeper runtime test passed: TEST 9 and TEST TOP completed.");
                    EditorApplication.isPlaying = false;
                    state = RunnerState.Idle;
                    prototype = null;
                    savedStatusStartedAt = -1d;
                    return;
                }

                if (KeeperActionRepeatingAfterSave("TEST TOP"))
                {
                    return;
                }

                if (StatusContains("timeout") || StatusContains("watchdog") || StatusContains("mismatch") || StatusContains("motion"))
                {
                    FailRuntimeTest("TEST TOP reported a runtime problem. Status: " + CurrentStatusText() + ". Controller: " + CurrentKeeperControllerName() + ". Motion: " + CurrentKeeperMotionViolation());
                    return;
                }

                if (ElapsedSeconds() > TopGridTimeoutSeconds)
                {
                    FailRuntimeTest("TEST TOP did not complete within " + TopGridTimeoutSeconds + " seconds. Status: " + CurrentStatusText() + ". Controller: " + CurrentKeeperControllerName() + ". Motion: " + CurrentKeeperMotionViolation());
                }
            }
        }

        private static bool KeeperActionRepeatingAfterSave(string label)
        {
            if (!StatusContains("SAVED -"))
            {
                savedStatusStartedAt = -1d;
                return false;
            }

            if (savedStatusStartedAt < 0d)
            {
                savedStatusStartedAt = EditorApplication.timeSinceStartup;
                return false;
            }

            if (EditorApplication.timeSinceStartup - savedStatusStartedAt <= KeeperActionFreezeGraceSeconds)
            {
                return false;
            }

            Animator animator = CurrentKeeperAnimator();
            float speed = animator != null ? animator.speed : 0f;
            bool enabled = animator != null && animator.enabled;
            if (!enabled || speed <= 0.01f)
            {
                return false;
            }

            FailRuntimeTest(label + " keeper action repeated after save. Status: " + CurrentStatusText() + ". Controller: " + CurrentKeeperControllerName() + ". Animator speed: " + speed.ToString("0.00") + ". Animator enabled: " + enabled);
            return true;
        }

        private static bool StatusContains(string value)
        {
            return CurrentStatusText().ToLowerInvariant().Contains(value.ToLowerInvariant());
        }

        private static bool StatusEquals(string value)
        {
            return CurrentStatusText().Equals(value, System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsReadyToStartRuntimeTest()
        {
            string status = CurrentStatusText();
            return status.Equals("Tap goal", System.StringComparison.OrdinalIgnoreCase)
                || status.Equals("Ready", System.StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(status);
        }

        private static string CurrentStatusText()
        {
            if (prototype != null && !string.IsNullOrWhiteSpace(prototype.StatusMessage))
            {
                return prototype.StatusMessage;
            }

            Text[] texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].name == "Status")
                {
                    return texts[i].text ?? "";
                }
            }

            return "";
        }

        private static string CurrentKeeperControllerName()
        {
            if (prototype == null || string.IsNullOrWhiteSpace(prototype.ActiveKeeperControllerName))
            {
                return "<none>";
            }

            return prototype.ActiveKeeperControllerName;
        }

        private static string CurrentKeeperMotionViolation()
        {
            if (prototype == null || string.IsNullOrWhiteSpace(prototype.KeeperMotionViolationMessage))
            {
                return "<none>";
            }

            return prototype.KeeperMotionViolationMessage;
        }

        private static Animator CurrentKeeperAnimator()
        {
            if (prototype == null)
            {
                return null;
            }

            FieldInfo animatorField = typeof(Bm8PenaltyPrototype).GetField("keeperAnimator", BindingFlags.Instance | BindingFlags.NonPublic);
            if (animatorField == null)
            {
                return null;
            }

            return animatorField.GetValue(prototype) as Animator;
        }

        private static double ElapsedSeconds()
        {
            return EditorApplication.timeSinceStartup - stageStartedAt;
        }

        private static void FailRuntimeTest(string message)
        {
            EditorPrefs.DeleteKey(RuntimeTestRequestKey);
            Debug.LogError("BM8 keeper runtime test failed: " + message);
            EditorApplication.isPlaying = false;
            state = RunnerState.Idle;
            prototype = null;
            savedStatusStartedAt = -1d;
        }
    }

    [MenuItem("BM8/Use Uploaded Stylized Goalkeeper")]
    public static void UseUploadedStylizedGoalkeeper()
    {
        GameObject stylizedKeeper = AssetDatabase.LoadAssetAtPath<GameObject>(StylizedKeeperPath);
        if (stylizedKeeper == null)
        {
            Debug.LogError("Uploaded stylized goalkeeper not found at: " + StylizedKeeperPath);
            return;
        }

        ReplaceGoalkeeperPrefab(stylizedKeeper);
    }

    [MenuItem("BM8/Replace Goalkeeper With Selected Humanoid")]
    public static void ReplaceGoalkeeperWithSelectedHumanoid()
    {
        GameObject selectedPrefab = Selection.activeObject as GameObject;
        if (selectedPrefab == null)
        {
            Debug.LogError("Select a rigged humanoid goalkeeper FBX/Prefab in Project, then run this menu again.");
            return;
        }

        ReplaceGoalkeeperPrefab(selectedPrefab);
    }

    private static void ReplaceGoalkeeperPrefab(GameObject selectedPrefab)
    {
        GameObject keeper = GameObject.Find("Goalkeeper");
        if (keeper == null)
        {
            Debug.LogError("Goalkeeper object not found. Open BM8PenaltyPrototype scene first.");
            return;
        }

        Transform existing = keeper.transform.Find("Visible FBX Keeper");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject visible = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
        if (visible == null)
        {
            visible = Object.Instantiate(selectedPrefab);
        }

        visible.name = "Visible FBX Keeper";
        visible.transform.SetParent(keeper.transform, false);
        visible.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        visible.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        visible.transform.localScale = Vector3.one * 1.05f;

        Animator animator = visible.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            animator = visible.AddComponent<Animator>();
        }

        animator.applyRootMotion = false;
        animator.runtimeAnimatorController = LoadKeeperController("AA_Soccer_Goal_Idel");

        Bm8PenaltyPrototype prototype = Object.FindAnyObjectByType<Bm8PenaltyPrototype>();
        if (prototype != null)
        {
            SerializedObject serialized = new SerializedObject(prototype);
            serialized.FindProperty("keeperAnimator").objectReferenceValue = animator;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(prototype);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Replaced goalkeeper with " + selectedPrefab.name + ". Check Rig settings if AA animations do not move it.");
    }

    [MenuItem("BM8/Rebuild Penalty Prototype Scene")]
    public static void Rebuild()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        RenderSettings.ambientLight = new Color(0.74f, 0.78f, 0.82f);
        RenderSettings.skybox = null;

        var controller = new GameObject("BM8 Penalty Prototype");
        var prototype = controller.AddComponent<Bm8PenaltyPrototype>();

        var cameraRig = new GameObject("Camera Rig").transform;
        cameraRig.position = new Vector3(0f, 2.72f, -6.35f);
        cameraRig.rotation = Quaternion.Euler(11.5f, 0f, 0f);

        var camera = new GameObject("Main Camera");
        camera.tag = "MainCamera";
        camera.transform.SetParent(cameraRig, false);
        var cameraComponent = camera.AddComponent<Camera>();
        cameraComponent.fieldOfView = 38f;
        cameraComponent.backgroundColor = new Color(0.07f, 0.13f, 0.2f);
        camera.AddComponent<AudioListener>();

        var sun = new GameObject("Match Light");
        var light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        sun.transform.rotation = Quaternion.Euler(52f, -28f, 0f);

        CreateField();
        var goal = CreateGoal();
        var ball = CreateBall();
        var player = CreatePlayer();
        var keeper = CreateKeeper();
        var ui = CreateUi(prototype);

        SerializedObject serialized = new SerializedObject(prototype);
        serialized.FindProperty("ball").objectReferenceValue = ball;
        serialized.FindProperty("player").objectReferenceValue = player;
        serialized.FindProperty("keeper").objectReferenceValue = keeper;
        serialized.FindProperty("keeperAnimator").objectReferenceValue = keeper.GetComponentInChildren<Animator>(true);
        AssignKeeperControllers(serialized);
        serialized.FindProperty("cameraRig").objectReferenceValue = cameraRig;
        serialized.FindProperty("statusText").objectReferenceValue = ui.status;
        serialized.FindProperty("scoreText").objectReferenceValue = ui.score;
        serialized.FindProperty("powerSlider").objectReferenceValue = ui.power;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = controller;
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/BM8PenaltyPrototype.unity");
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/BM8PenaltyPrototype.unity", true)
        };

        Debug.Log("BM8 penalty prototype scene rebuilt: " + goal.name);
    }

    private static void CreateField()
    {
        var field = GameObject.CreatePrimitive(PrimitiveType.Cube);
        field.name = "Pitch";
        field.transform.localScale = new Vector3(11f, 0.12f, 17f);
        field.transform.position = new Vector3(0f, -0.08f, 0.9f);
        SetMaterial(field, new Color(0.08f, 0.42f, 0.18f));

        CreateLine("Penalty Spot", new Vector3(0f, 0.02f, -2.9f), new Vector3(0.28f, 0.03f, 0.28f));
        CreateLine("Goal Line", new Vector3(0f, 0.03f, 4.95f), new Vector3(7.6f, 0.03f, 0.08f));
        CreateLine("Penalty Box Back", new Vector3(0f, 0.03f, 1.0f), new Vector3(7.6f, 0.03f, 0.08f));
        CreateLine("Penalty Box Left", new Vector3(-3.8f, 0.03f, 3.0f), new Vector3(0.08f, 0.03f, 4.0f));
        CreateLine("Penalty Box Right", new Vector3(3.8f, 0.03f, 3.0f), new Vector3(0.08f, 0.03f, 4.0f));
    }

    private static GameObject CreateGoal()
    {
        var goal = new GameObject("Goal Frame");
        CreatePost("Left Post", goal.transform, new Vector3(-3.05f, 1.25f, 5.05f), new Vector3(0.1f, 2.5f, 0.1f));
        CreatePost("Right Post", goal.transform, new Vector3(3.05f, 1.25f, 5.05f), new Vector3(0.1f, 2.5f, 0.1f));
        CreatePost("Crossbar", goal.transform, new Vector3(0f, 2.5f, 5.05f), new Vector3(6.2f, 0.1f, 0.1f));
        CreatePost("Net Back", goal.transform, new Vector3(0f, 1.25f, 5.55f), new Vector3(6.2f, 2.4f, 0.04f), new Color(0.78f, 0.86f, 0.9f, 0.38f));
        return goal;
    }

    private static Transform CreateBall()
    {
        var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Ball";
        ball.transform.position = new Vector3(0f, 0.22f, -2.9f);
        ball.transform.localScale = Vector3.one * 0.42f;
        SetMaterial(ball, Color.white);
        return ball.transform;
    }

    private static Transform CreatePlayer()
    {
        var player = new GameObject("Striker").transform;
        player.position = new Vector3(-1.1f, 0f, -4.75f);
        player.localScale = Vector3.one * 1.22f;
        CreateBody(player, "BM8 Striker", new Color(0.92f, 0.18f, 0.08f), new Color(0.04f, 0.055f, 0.07f), false);
        CreateVisibleFbxStriker(player);
        return player;
    }

    private static Transform CreateKeeper()
    {
        var keeper = new GameObject("Goalkeeper").transform;
        keeper.position = new Vector3(0f, 0f, 4.45f);
        keeper.localScale = Vector3.one * 1.12f;
        CreateBody(keeper, "Keeper", new Color(0.025f, 0.025f, 0.03f), new Color(0.02f, 0.08f, 0.13f), true);
        CreateVisibleFbxKeeper(keeper);
        return keeper;
    }

    private static void CreateVisibleFbxKeeper(Transform keeper)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StylizedKeeperPath);
        if (prefab == null)
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RobotKeeperPath);
        }

        if (prefab == null)
        {
            Debug.LogWarning("No goalkeeper prefab found.");
            return;
        }

        GameObject visible = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        visible.name = "Visible FBX Keeper";
        visible.transform.SetParent(keeper, false);
        visible.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        visible.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        visible.transform.localScale = Vector3.one * 1.05f;

        Animator animator = visible.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = LoadKeeperController("AA_Soccer_Goal_Idel");
        }
    }

    private static (Text status, Text score, Slider power) CreateUi(Bm8PenaltyPrototype prototype)
    {
        var canvasObject = new GameObject("Match UI");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        var status = CreateText(canvasObject.transform, "Status", "BM8 PENALTY", new Vector2(0.5f, 1f), new Vector2(0f, -24f), 18);
        var score = CreateText(canvasObject.transform, "Score", "Goals 0   Saves 0   Shots 0", new Vector2(0.5f, 1f), new Vector2(0f, -50f), 14);
        score.color = new Color(1f, 0.9f, 0.35f);
        var hint = CreateText(canvasObject.transform, "Hint", "", new Vector2(0.5f, 1f), new Vector2(0f, -90f), 14);
        hint.color = new Color(0.88f, 0.95f, 1f);

        var powerObject = new GameObject("Power Slider");
        powerObject.transform.SetParent(canvasObject.transform, false);
        var slider = powerObject.AddComponent<Slider>();
        slider.minValue = 0.35f;
        slider.maxValue = 1f;
        slider.value = 0.75f;
        var rect = powerObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 188f);
        rect.sizeDelta = new Vector2(260f, 24f);

        return (status, score, slider);
    }

    private static void CreateBody(Transform parent, string name, Color shirt, Color shortsColor, bool keeper)
    {
        Color skin = new Color(0.76f, 0.50f, 0.34f);
        Color hair = new Color(0.025f, 0.02f, 0.018f);
        Color sock = keeper ? new Color(0.86f, 0.92f, 0.98f) : new Color(0.93f, 0.1f, 0.05f);
        Color trim = keeper ? new Color(1f, 0.9f, 0.08f) : Color.white;
        Color panel = keeper ? new Color(0.12f, 0.52f, 0.95f) : new Color(0.08f, 0.08f, 0.085f);
        Color boot = new Color(0.015f, 0.015f, 0.016f);

        CreatePart(name + " Neck", parent, PrimitiveType.Capsule, new Vector3(0f, 1.55f, -0.01f), new Vector3(0.16f, 0.18f, 0.14f), skin);
        CreatePart(name + " Torso", parent, PrimitiveType.Capsule, new Vector3(0f, 1.12f, 0f), new Vector3(0.42f, 0.86f, 0.3f), shirt);
        CreatePart(name + " Chest Panel", parent, PrimitiveType.Cube, new Vector3(0f, 1.13f, -0.18f), new Vector3(0.34f, 0.46f, 0.026f), panel);
        CreatePart(name + " Shoulder Line", parent, PrimitiveType.Cube, new Vector3(0f, 1.48f, -0.02f), new Vector3(0.68f, 0.07f, 0.2f), shirt);
        CreatePart(name + " Collar", parent, PrimitiveType.Cube, new Vector3(0f, 1.53f, -0.16f), new Vector3(0.22f, 0.04f, 0.05f), trim);
        CreatePart(name + " Head", parent, PrimitiveType.Sphere, new Vector3(0f, 1.86f, 0f), new Vector3(0.23f, 0.3f, 0.22f), skin);
        CreatePart(name + " Hair", parent, PrimitiveType.Sphere, new Vector3(0f, 2.02f, -0.02f), new Vector3(0.24f, 0.12f, 0.22f), hair);
        CreatePart(name + " Back Hair", parent, PrimitiveType.Cube, new Vector3(0f, 1.93f, 0.16f), new Vector3(0.2f, 0.18f, 0.04f), hair);
        CreatePart(name + " Left Ear", parent, PrimitiveType.Sphere, new Vector3(-0.22f, 1.86f, 0f), new Vector3(0.045f, 0.075f, 0.035f), skin);
        CreatePart(name + " Right Ear", parent, PrimitiveType.Sphere, new Vector3(0.22f, 1.86f, 0f), new Vector3(0.045f, 0.075f, 0.035f), skin);
        CreatePart(name + " Shorts", parent, PrimitiveType.Cube, new Vector3(0f, 0.62f, 0f), new Vector3(0.48f, 0.26f, 0.28f), shortsColor);
        CreatePart(name + " Left Leg", parent, PrimitiveType.Capsule, new Vector3(-0.15f, 0.27f, 0f), new Vector3(0.11f, 0.66f, 0.11f), skin);
        CreatePart(name + " Right Leg", parent, PrimitiveType.Capsule, new Vector3(0.15f, 0.27f, 0f), new Vector3(0.11f, 0.66f, 0.11f), skin);
        CreatePart(name + " Left Sock", parent, PrimitiveType.Capsule, new Vector3(-0.15f, 0.04f, -0.01f), new Vector3(0.115f, 0.24f, 0.115f), sock);
        CreatePart(name + " Right Sock", parent, PrimitiveType.Capsule, new Vector3(0.15f, 0.04f, -0.01f), new Vector3(0.115f, 0.24f, 0.115f), sock);
        CreatePart(name + " Left Boot", parent, PrimitiveType.Cube, new Vector3(-0.15f, -0.09f, -0.08f), new Vector3(0.18f, 0.07f, 0.28f), boot);
        CreatePart(name + " Right Boot", parent, PrimitiveType.Cube, new Vector3(0.15f, -0.09f, -0.08f), new Vector3(0.18f, 0.07f, 0.28f), boot);
        CreatePart(name + " Left Arm", parent, PrimitiveType.Capsule, new Vector3(-0.39f, 1.14f, 0.01f), new Vector3(0.095f, 0.56f, 0.095f), shirt);
        CreatePart(name + " Right Arm", parent, PrimitiveType.Capsule, new Vector3(0.39f, 1.14f, 0.01f), new Vector3(0.095f, 0.56f, 0.095f), shirt);
        CreatePart(name + " Left Forearm", parent, PrimitiveType.Capsule, new Vector3(-0.42f, 0.82f, 0.01f), new Vector3(0.08f, 0.26f, 0.08f), skin);
        CreatePart(name + " Right Forearm", parent, PrimitiveType.Capsule, new Vector3(0.42f, 0.82f, 0.01f), new Vector3(0.08f, 0.26f, 0.08f), skin);
        CreatePart(name + " Left Hand", parent, PrimitiveType.Sphere, new Vector3(-0.42f, 0.66f, -0.02f), new Vector3(0.09f, 0.08f, 0.07f), skin);
        CreatePart(name + " Right Hand", parent, PrimitiveType.Sphere, new Vector3(0.42f, 0.66f, -0.02f), new Vector3(0.09f, 0.08f, 0.07f), skin);
        CreatePart(name + " Left Sleeve Trim", parent, PrimitiveType.Cube, new Vector3(-0.39f, 0.91f, -0.01f), new Vector3(0.13f, 0.035f, 0.12f), trim);
        CreatePart(name + " Right Sleeve Trim", parent, PrimitiveType.Cube, new Vector3(0.39f, 0.91f, -0.01f), new Vector3(0.13f, 0.035f, 0.12f), trim);
    }

    private static void CreateVisibleFbxStriker(Transform parent)
    {
        if (CreatePhotoBillboard(
            parent,
            "Visible Photo Character",
            "Assets/Art/Characters/striker-photo-cutout.png",
            new Vector3(0f, 1.16f, -0.27f),
            new Vector2(1.34f, 2.38f)))
        {
            return;
        }

        const string modelPath = "Assets/ThirdParty/KenneyAnimatedCharacters/Model/characterMedium.fbx";
        AssetDatabase.Refresh();

        var source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (source == null)
        {
            Debug.LogWarning("BM8 visible FBX striker model is missing: " + modelPath);
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        instance.name = "Visible FBX Character";
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        instance.transform.localScale = Vector3.one * 0.0125f;

        var kitMaterial = CreateOrUpdateMaterial(
            "Assets/ThirdParty/KenneyAnimatedCharacters/BM8_Striker_OrangeBlack.mat",
            new Color(0.92f, 0.18f, 0.08f));
        foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterial = kitMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        CreatePart("BM8 Visible Chest Panel", instance.transform, PrimitiveType.Cube, new Vector3(0f, 92f, -16f), new Vector3(24f, 42f, 2.2f), new Color(0.035f, 0.04f, 0.045f));
        CreatePart("BM8 Visible Collar", instance.transform, PrimitiveType.Cube, new Vector3(0f, 121f, -16.5f), new Vector3(15f, 3.5f, 2.4f), Color.white);
    }

    private static void AssignKeeperControllers(SerializedObject serialized)
    {
        SetController(serialized, "keeperIdleController", "AA_Soccer_Goal_Idel");
        SetController(serialized, "keeperCatchForwardSuccessController", "AA_Soccer_Goal_CatchBall_F_Succ");
        SetController(serialized, "keeperCatchForwardFailController", "AA_Soccer_Goal_CatchBall_F_Fail");
        SetController(serialized, "keeperCatchUpSuccessController", "AA_Soccer_Goal_CatchBall_UP_Succ");
        SetController(serialized, "keeperCatchUpFailController", "AA_Soccer_Goal_CatchBall_UP_Fail");
        SetController(serialized, "keeperCatchLeftDownSuccessController", "AA_Soccer_Goal_CatchBall_LD_Succ");
        SetController(serialized, "keeperCatchLeftDownFailController", "AA_Soccer_Goal_CatchBall_LD_Fail");
        SetController(serialized, "keeperCatchRightDownSuccessController", "AA_Soccer_Goal_CatchBall_RD_Succ");
        SetController(serialized, "keeperCatchRightDownFailController", "AA_Soccer_Goal_CatchBall_RD_Fail");
        SetController(serialized, "keeperHitLeftSuccessController", "AA_Soccer_Goal_HitBall_L_Succ");
        SetController(serialized, "keeperHitLeftFailController", "AA_Soccer_Goal_HitBall_L_Fail");
        SetController(serialized, "keeperHitRightSuccessController", "AA_Soccer_Goal_HitBall_R_Succ");
        SetController(serialized, "keeperHitRightFailController", "AA_Soccer_Goal_HitBall_R_Fail");
        SetController(serialized, "keeperHitTopLeftSuccessController", "AA_Soccer_Goal_HitBall_TL_Succ");
        SetController(serialized, "keeperHitTopLeftFailController", "AA_Soccer_Goal_HitBall_TL_Fail");
        SetController(serialized, "keeperHitTopRightSuccessController", "AA_Soccer_Goal_HitBall_TR_Succ");
        SetController(serialized, "keeperHitTopRightFailController", "AA_Soccer_Goal_HitBall_TR_Fail");
    }

    private static void SetController(SerializedObject serialized, string propertyName, string controllerName)
    {
        serialized.FindProperty(propertyName).objectReferenceValue = LoadKeeperController(controllerName);
    }

    private static int ValidateControllerProperty(SerializedObject serialized, string propertyName, ref string report)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue == null)
        {
            report += "- Scene reference missing: " + propertyName + "\n";
            return 1;
        }

        return 0;
    }

    private static RuntimeAnimatorController LoadKeeperController(string controllerName)
    {
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AaControllerFolder + controllerName + ".Controller");
    }

    private static bool CreatePhotoBillboard(Transform parent, string name, string texturePath, Vector3 localPosition, Vector2 size)
    {
        AssetDatabase.Refresh();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            Debug.LogWarning("BM8 photo character texture is missing: " + texturePath);
            return false;
        }

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = localPosition;
        quad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        quad.transform.localScale = new Vector3(size.x, size.y, 1f);

        var collider = quad.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        string materialPath = texturePath.Replace(".png", ".mat");
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Unlit/Transparent"));
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.shader = Shader.Find("Unlit/Transparent");
        material.mainTexture = texture;
        material.color = Color.white;
        material.renderQueue = 3000;
        EditorUtility.SetDirty(material);

        var renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return true;
    }

    private static Material CreateOrUpdateMaterial(string path, Color color)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreatePart(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        SetMaterial(part, color);
        return part;
    }

    private static void CreateLine(string name, Vector3 position, Vector3 scale)
    {
        var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = name;
        line.transform.position = position;
        line.transform.localScale = scale;
        SetMaterial(line, Color.white);
    }

    private static void CreatePost(string name, Transform parent, Vector3 position, Vector3 scale)
    {
        CreatePost(name, parent, position, scale, Color.white);
    }

    private static void CreatePost(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
    {
        var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = name;
        post.transform.SetParent(parent, false);
        post.transform.position = position;
        post.transform.localScale = scale;
        SetMaterial(post, color);
    }

    private static Button CreateButton(Transform parent, string label, Vector2 anchor, Vector2 offset)
    {
        var buttonObject = new GameObject(label + " Button");
        buttonObject.transform.SetParent(parent, false);
        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);
        var button = buttonObject.AddComponent<Button>();
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = offset;
        rect.sizeDelta = new Vector2(112f, 42f);

        var text = CreateText(buttonObject.transform, "Label", label, new Vector2(0.5f, 0.5f), Vector2.zero, 18);
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.rectTransform.sizeDelta = rect.sizeDelta;
        return button;
    }

    private static Text CreateText(Transform parent, string name, string value, Vector2 anchor, Vector2 offset, int size)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = offset;
        rect.sizeDelta = new Vector2(520f, 42f);
        return text;
    }

    private static void SetMaterial(GameObject target, Color color)
    {
        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        target.GetComponent<Renderer>().sharedMaterial = material;
    }
}
