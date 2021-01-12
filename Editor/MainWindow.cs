using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Gimbl;

public class MainWindow : EditorWindow
{
    private SerializedObject Logger;
    private SerializedProperty outputPath;
    private SerializedProperty outputFile;
    Vector2 scrollPosition = Vector2.zero;

    private MQTTClient _client;
    private LoggerObject _logger;
    private static MainWindow window;

    private bool foldBlink = false;
    private int duration;
    private int fadeTime;

    [System.Serializable]
    private class MsgMenuSettings
    {
        public bool isFold = false;
        public bool sendFrameMsg = false;
    }
    [SerializeField] private MsgMenuSettings msgSettings = new MsgMenuSettings();

    [System.Serializable]
    private class SessionMenuSettings
    {
        public bool isFold = false;
        public bool externalStart = false;
        public bool externalLog = false;
    }
    [SerializeField] private SessionMenuSettings sessionSettings = new SessionMenuSettings();

    [MenuItem("Window/Gimbl")]
    public static void ShowWindow()
    {
        if (window == null)
        {
            window = GetWindow<MainWindow>(" Settings", true );
            Texture2D tex = Resources.Load("Icons/mouse", typeof(Texture2D)) as Texture2D;
            window.titleContent.image = tex;
            Rect pos = window.position;
            pos.x = Screen.currentResolution.width * 0.75f;
            pos.y = Screen.currentResolution.height * 0.5f;
            pos.width = 385;
            pos.height = 520;
            window.position = pos;
            // Call tab windows.
            ActorWindow.ShowWindow();
            DisplaysWindow.ShowWindow();
            // Refocus to main.
            window.Focus();
        }
    }
    private void OnEnable()
    {
        InitializeScene();
    }
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(position.height), GUILayout.Width(position.width));
        #region Logging.
        EditorGUILayout.BeginVertical(LayoutSettings.mainBox.style);
        Logger = new SerializedObject(FindObjectOfType<LoggerObject>().settings);
        outputPath = Logger.FindProperty("outputPath");
        outputFile = Logger.FindProperty("outputFile");

        EditorGUILayout.LabelField("Logging", LayoutSettings.sectionLabel);
        if (EditorApplication.isPlaying) GUI.enabled = false;
        // file/
        EditorGUILayout.PropertyField(outputFile, new GUIContent("Session Name: "), false, GUILayout.Width(300));
        EditorGUILayout.PropertyField(Logger.FindProperty("sessionId"), new GUIContent("Session Id: "), false, GUILayout.Width(300));
        Logger.ApplyModifiedProperties();
        EditorGUILayout.EndVertical();
        #endregion

        #region MQTT
        // MQTT Settings.
        if (EditorApplication.isPlaying) GUI.enabled = false; // disable on play.
        EditorGUILayout.BeginVertical(LayoutSettings.mainBox.style);
        EditorGUILayout.LabelField("MQTT", LayoutSettings.sectionLabel);
        _client.ip = EditorGUILayout.TextField("ip: ", _client.ip, GUILayout.Width(300));
        _client.port = int.Parse(EditorGUILayout.TextField("port: ", _client.port.ToString(), GUILayout.Width(300)));
        if (GUI.changed)
        {
            EditorPrefs.SetString("JaneliaVR_MQTT_IP", _client.ip);
            EditorPrefs.SetInt("JaneliaVR_MQTT_Port", _client.port);
        }
        if (GUILayout.Button("Test Connection"))
        {
            _client.Connect(true);
            _client.Disconnect();
        }
        //Messaging settings.
        msgSettings.isFold = EditorGUILayout.Foldout(msgSettings.isFold, "Messaging");
        if (msgSettings.isFold)
        {
            EditorGUILayout.BeginVertical(LayoutSettings.subBox.style);
            bool newSendFrame = EditorGUILayout.Toggle("Send Frame Messages", msgSettings.sendFrameMsg, LayoutSettings.editFieldOp);
            if (newSendFrame != msgSettings.sendFrameMsg) { msgSettings.sendFrameMsg = newSendFrame; EditorPrefs.SetBool("Gimbl_sendFrameMsg", newSendFrame); }
            EditorGUILayout.EndVertical();
        }
        //Session settings.
        sessionSettings.isFold = EditorGUILayout.Foldout(sessionSettings.isFold, "External Control");
        if (sessionSettings.isFold)
        {
            EditorGUILayout.BeginVertical(LayoutSettings.subBox.style);
            bool newExternalStart = EditorGUILayout.Toggle("External Start Trigger", sessionSettings.externalStart, LayoutSettings.editFieldOp);
            if (newExternalStart != sessionSettings.externalStart) { sessionSettings.externalStart= newExternalStart; EditorPrefs.SetBool("Gimbl_externalStart", newExternalStart); }
            bool newExternalLog = EditorGUILayout.Toggle("External Log Naming", sessionSettings.externalLog, LayoutSettings.editFieldOp);
            if (newExternalLog != sessionSettings.externalLog) { sessionSettings.externalLog = newExternalLog; EditorPrefs.SetBool("Gimbl_externalLog", newExternalLog); }
            EditorGUILayout.EndVertical();
        }

        GUI.enabled = true;
        EditorGUILayout.EndVertical();


        #endregion


        EditorGUILayout.BeginVertical(LayoutSettings.mainBox.style);
        EditorGUILayout.LabelField("General", LayoutSettings.sectionLabel);

        #region Teleport.
        foldBlink = EditorGUILayout.Foldout(foldBlink, "Teleport");
        if (foldBlink)
        {
            EditorGUILayout.BeginVertical(LayoutSettings.subBox.style);
            EditorGUILayout.BeginHorizontal(LayoutSettings.editFieldOp);
                int newDuration = EditorGUILayout.IntField("Dark Duration: ", PlayerPrefs.GetInt("Gimbl_BlinkDuration",2000));
                EditorGUILayout.LabelField("(ms)", GUILayout.Width(50));
            if (newDuration != duration) { PlayerPrefs.SetInt("Gimbl_BlinkDuration", newDuration); duration = newDuration; }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal(LayoutSettings.editFieldOp);
                int newFadeTime = EditorGUILayout.IntField("Fade Time: ", PlayerPrefs.GetInt("Gimbl_BlinkFadeTime",3000));
            EditorGUILayout.LabelField("(ms)", GUILayout.Width(50)) ;
                if (newFadeTime != fadeTime) { PlayerPrefs.SetInt("Gimbl_BlinkFadeTime", newFadeTime); fadeTime = newFadeTime; }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        #endregion
        EditorGUILayout.EndVertical();

        #region Export/Import.
        EditorGUILayout.BeginVertical(LayoutSettings.mainBox.style);
        EditorGUILayout.LabelField("Setup",LayoutSettings.sectionLabel);
        if (GUILayout.Button("Export Setup")) {  ExportSetup(); }
        if (GUILayout.Button("Import Setup")) { ImportSetup();}
        EditorGUILayout.EndVertical();
        #endregion

        EditorGUILayout.EndScrollView();
    }

    //IntializeScene. Check for excistence of key gameobjects and set reference variables.
    private void InitializeScene()
    {
        // Create settings folders.
        if (!AssetDatabase.IsValidFolder("Assets/Logs")) AssetDatabase.CreateFolder("Assets", "Logs");
        if (!AssetDatabase.IsValidFolder("Assets/VRSettings")) AssetDatabase.CreateFolder("Assets", "VRSettings");
        if (!AssetDatabase.IsValidFolder("Assets/VRSettings/PathCreator")) AssetDatabase.CreateFolder("Assets/VRSettings", "PathCreator");
        if (!AssetDatabase.IsValidFolder("Assets/VRSettings/Controllers")) AssetDatabase.CreateFolder("Assets/VRSettings", "Controllers");
        if (!AssetDatabase.IsValidFolder("Assets/VRSettings/Displays")) AssetDatabase.CreateFolder("Assets/VRSettings", "Displays");
        if (!AssetDatabase.IsValidFolder("Assets/VRSettings/Actors")) AssetDatabase.CreateFolder("Assets/VRSettings", "Actors");

        // Check for main default objects.
        GameObject obj;
        string[] DefaultObjects = { "Actors", "Controllers", "MQTT Client","Paths","Logger" };
        foreach (string objName in DefaultObjects)
        {
            if (!GameObject.Find(objName))
            {
                Debug.Log(string.Format("Creating Object: {0}..", objName));
                obj = new GameObject(objName);
                // Special circumstances.
                switch (objName)
                {
                    case "MQTT Client":
                        obj.AddComponent<Gimbl.MQTTClient>();
                        break;
                    case "Logger":
                        _logger = obj.AddComponent<Gimbl.LoggerObject>();
                        Gimbl.LoggerSettings asset = CreateInstance<Gimbl.LoggerSettings>();
                        AssetDatabase.CreateAsset(asset, "Assets/VRSettings/LoggerSettings.asset");
                        _logger.settings = asset;
                        break;
                }
                // Things have changed. Mark scene for save.
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
            else obj = GameObject.Find(objName);
            switch (objName)
            {  
                // Assign for controls.
                case "MQTT Client":
                    _client = obj.GetComponent<Gimbl.MQTTClient>();
                    obj.hideFlags = HideFlags.HideInHierarchy;
                    break;
                case "Paths":
                    break;
                case "Controllers":
                    obj.hideFlags = HideFlags.None;
                    break;
                case "Logger":
                    _logger = obj.GetComponent<Gimbl.LoggerObject>();
                    _logger.settings = (LoggerSettings)AssetDatabase.LoadAssetAtPath("Assets/VRSettings/LoggerSettings.asset", typeof(LoggerSettings));
                    obj.hideFlags = HideFlags.HideInHierarchy;
                    break;
                case "Actors":
                    break;
            }
        }
        // Set client IP settings from stored.
        _client.ip = EditorPrefs.GetString("JaneliaVR_MQTT_IP");
        if (_client.ip == "") _client.ip = "127.0.0.1";
        _client.port = EditorPrefs.GetInt("JaneliaVR_MQTT_Port");
        if (_client.port == 0) _client.port = 1883;
        // Set default properties.
        msgSettings.sendFrameMsg = EditorPrefs.GetBool("Gimbl_sendFrameMsg",false);
        sessionSettings.externalStart = EditorPrefs.GetBool("Gimbl_externalStart", false);
        sessionSettings.externalLog = EditorPrefs.GetBool("Gimbl_externalLog", false);
        // Check for global display settings path creator.
        if (UnityEditor.AssetDatabase.FindAssets("t:GlobalDisplaySettings").Length == 0)
        {
            PathCreation.GlobalDisplaySettings asset = CreateInstance<PathCreation.GlobalDisplaySettings>();
            AssetDatabase.CreateAsset(asset, "Assets/VRSettings/PathCreator/GlobalDisplaySettings.asset");
        }

    }

    private void ExportSetup()
    {
        // Actors and Controllers are stored first as assets.
        // these are then bundles into a package together with the required settings files (scriptableObjects)

        // File dialogue.
        string[] s = Application.dataPath.Split('/');
        string projectName = s[s.Length - 2];
        string outputFile = EditorUtility.SaveFilePanel("Save Setup as..",
            "",
            projectName,
            "gimblsetup");
        if (outputFile.Length == 0) return;
        // Create temporary asset where everything will be stored.
        PrefabUtility.SaveAsPrefabAsset(GameObject.Find("Actors"), "Assets/tempActors.prefab");
        PrefabUtility.SaveAsPrefabAsset(GameObject.Find("Controllers"), "Assets/tempControllers.prefab");
        // Create Package.
        string[] assetBundle = new string[] { "Assets/tempActors.prefab", "Assets/tempControllers.prefab","Assets/VRSettings/Displays/savedFullScreenViews.asset" };
        AssetDatabase.ExportPackage(assetBundle, outputFile, ExportPackageOptions.IncludeDependencies);
        // Remove tmeporary assets.
        AssetDatabase.DeleteAsset("Assets/tempActors.prefab");
        AssetDatabase.DeleteAsset("Assets/tempControllers.prefab");

    }

    private void ImportSetup()
    {
        // Import dialogue (Continue: Yes/No)
        bool choice =  EditorUtility.DisplayDialog("Erase current setup?",
        "Importing this setup will remove all current Actors,Controllers and Displays", "Continue", "Cancel");
        if (!choice) return;
        // File Dialogue.
        string inputFile = EditorUtility.OpenFilePanel("Import Setup", Application.dataPath, "gimblsetup");
        if (inputFile.Length == 0) return;
        // Remove Actors and Controllers (repopulate..)
        DestroyImmediate(GameObject.Find("Actors"));
        DestroyImmediate(GameObject.Find("Controllers"));
        //Import package.
        AssetDatabase.ImportPackage(inputFile, false);
        //Instantiate Actors.
        Object actObj = AssetDatabase.LoadAssetAtPath("Assets/tempActors.prefab",typeof(Object));
        GameObject actors = Instantiate(actObj) as GameObject;
        actors.name = "Actors";
        //Instantiate Controllers..
        Object contObj = AssetDatabase.LoadAssetAtPath("Assets/tempControllers.prefab", typeof(Object));
        GameObject controllers = Instantiate(contObj) as GameObject;
        controllers.name = "Controllers";
        // Load Camera setup. (stored in Assets/VRSettings/Displays/savedFullScreenViews.asset).
        DisplaysWindow win = (DisplaysWindow)GetWindow(typeof(DisplaysWindow));
        win.fullScreenManager.LoadCameras();
        // Set render layers.
        foreach (ActorObject act in actors.GetComponentsInChildren<ActorObject>())
        {
            //Create layer.
            if (LayerMask.NameToLayer(act.name)==-1)
            {
                TagLayerEditor.TagsAndLayers.AddLayer(act.name);
            }
            GameObject model = act.GetComponentInChildren<MeshRenderer>().gameObject;
            //Set Layer.
            model.layer = LayerMask.NameToLayer(act.name);
            // Set Culling mask.
            if (act.gameObject.GetComponentInChildren<DisplayObject>() != null)
            {
                foreach (Camera cam in act.gameObject.GetComponentInChildren<DisplayObject>().GetComponentsInChildren<Camera>())
                {
                    cam.cullingMask = -1; // show everything
                    cam.cullingMask &= ~(1 << LayerMask.NameToLayer(act.name));
                }
            }
        }
        // Remove tmeporary assets.
        AssetDatabase.DeleteAsset("Assets/tempActors.prefab");
        AssetDatabase.DeleteAsset("Assets/tempControllers.prefab");
    }
}