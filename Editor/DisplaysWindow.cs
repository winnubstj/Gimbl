using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using Gimbl;

public class DisplaysWindow : EditorWindow
{
    #region Menu Variables.
    Vector2 scrollPosition = Vector2.zero;
    public delegate void CreateFunc<T>(MenuSettings<T> settings) where T : UnityEngine.Object;
    public enum DisplayType
    {
        Monitor,
    }
    [System.Serializable]
    public class MenuSettings<T>
    {
        public string typeName;
        public bool[] show = { false, false, false, false, false };
        public string name = "";
        public T selected;
    }

    [System.Serializable]
    public class DisplayMenu : MenuSettings<DisplayObject> { }
    string[] displayModels;
    private int selectedModel = 0;
    Gimbl.DisplayType dispType = Gimbl.DisplayType.Monitor;
    SerializedObject serializedObject;

    // Display variables.
    private DisplayMenu dispSettings = new DisplayMenu() { typeName = "Display" };

    [SerializeField]
    public FullScreenViewManager fullScreenManager;
    #endregion

    #region Window Setup
    private static EditorWindow currentWindow;
    public static void ShowWindow()
    {
        if (currentWindow == null) currentWindow = GetWindow<DisplaysWindow>("Displays", true, typeof(MainWindow));
    }
    private void OnEnable()
    {
        TagLayerEditor.TagsAndLayers.AddTag("VRDisplay");
        // Get Display 3d models (as prefabs in Resources/Displays).
        UnityEngine.Object[] data = Resources.LoadAll<GameObject>("Displays");
        displayModels = data.Select(x => x.name).ToArray();
        // Get fullscreen info.
        fullScreenManager = new FullScreenViewManager();
    }
    #endregion


    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(position.height), GUILayout.Width(position.width));
        #region Display Menu
        EditorGUILayout.BeginVertical(LayoutSettings.mainBox.style);
        EditorGUILayout.LabelField("Displays", LayoutSettings.sectionLabel);

        // Select and delete.
        EditorGUILayout.BeginHorizontal();
        SelectMenu(dispSettings);
        if (GUILayout.Button("Delete", LayoutSettings.buttonOp)) DeleteDisplay();
        EditorGUILayout.EndHorizontal();

        // Edit.
        if (dispSettings.selected != null)
        {
            // Blank screen.
            EditorGUILayout.BeginHorizontal();
            if (dispSettings.selected.currentBrightness > 0)
            {
                if (GUILayout.Button("Blank Display")) { dispSettings.selected.currentBrightness = 0; }
            }
            else
            {
                if (GUILayout.Button("Show Display")) { dispSettings.selected.currentBrightness = dispSettings.selected.settings.brightness; }
            }
            EditorGUILayout.EndHorizontal();
        }

        // Edit.
        dispSettings.show[0] = EditorGUILayout.Foldout(dispSettings.show[0], "Edit");
        if (dispSettings.show[0])
        {
            if (dispSettings.selected != null)
            {
                EditorGUILayout.BeginVertical(LayoutSettings.subBox.style);
                serializedObject = new SerializedObject(dispSettings.selected.settings);
                float prevHeight = dispSettings.selected.settings.heightInVR;
                float prevBrightness = dispSettings.selected.settings.brightness;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("isActive"), true,LayoutSettings.editFieldOp);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("brightness"), true, LayoutSettings.editFieldOp);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("heightInVR"), true, LayoutSettings.editFieldOp);
                serializedObject.ApplyModifiedProperties();
                if (prevHeight != dispSettings.selected.settings.heightInVR){ dispSettings.selected.transform.localPosition = new Vector3(0, dispSettings.selected.settings.heightInVR, 0); }
                if (prevBrightness != dispSettings.selected.settings.brightness) { dispSettings.selected.currentBrightness = dispSettings.selected.settings.brightness; }
                EditorGUILayout.EndVertical();
            }
        }

        // Create.
        if (EditorApplication.isPlaying) GUI.enabled = false;
        dispSettings.show[1] = EditorGUILayout.Foldout(dispSettings.show[1], "Create");
        if (dispSettings.show[1])
        {
            EditorGUILayout.BeginVertical(LayoutSettings.subBox.style);
            EditorGUILayout.LabelField("Create Display", EditorStyles.boldLabel);
            dispSettings.name = EditorGUILayout.TextField("Display Name: ", dispSettings.name, LayoutSettings.editFieldOp);
            selectedModel = EditorGUILayout.Popup("Model: ", selectedModel, displayModels, LayoutSettings.editFieldOp);
            dispType = (Gimbl.DisplayType)EditorGUILayout.EnumPopup("Type: ", dispType, LayoutSettings.editFieldOp);
            CreateButton(dispSettings, new CreateFunc<DisplayObject>(CreateDisplay));
            EditorGUILayout.EndVertical();
        }
        GUI.enabled = true;
        EditorGUILayout.EndVertical();
        #endregion

        #region Screen Control.
        EditorGUILayout.BeginVertical(LayoutSettings.mainBox.style);
        EditorGUILayout.LabelField("Camera Mapping", LayoutSettings.sectionLabel);

        fullScreenManager.OnGUIRefreshMonitorPositions();
        fullScreenManager.OnGUICameraObjectFields();
        if (EditorApplication.isPlaying) GUI.enabled = false;
        fullScreenManager.OnGUIShowFullScreenViews();
        GUI.enabled = true;
        EditorGUILayout.EndVertical();
        #endregion.
        EditorGUILayout.EndScrollView();
    }

    // MenuFunctions.

    private void SelectMenu<T>(MenuSettings<T> settings) where T : UnityEngine.Object
    {
        T obj = FindObjectOfType<T>();
        if (settings.selected == null && obj != null) settings.selected = obj;
        settings.selected = (T)EditorGUILayout.ObjectField(settings.selected, typeof(T), true);
    }
    private void CreateButton<T>(MenuSettings<T> settings, CreateFunc<T> func) where T : UnityEngine.Object
    {
        EditorGUILayout.BeginHorizontal();
        T[] objs = FindObjectsOfType<T>();
        string[] names = objs.Select(x => x.name).ToArray();
        string msg = "";
        if (ArrayUtility.Contains(names, settings.name)) { msg = "Duplicate name"; GUI.enabled = false; }
        if (settings.name == "") { msg = "Empty Name"; GUI.enabled = false; }
        EditorGUILayout.LabelField(msg, GUILayout.Width(197));
        if (GUILayout.Button("Create", LayoutSettings.buttonOp)) func(settings);
        EditorGUILayout.EndHorizontal();
    }

    // Object Manipulation Functions

    private void DeleteDisplay()
    {
        GameObject obj = dispSettings.selected.gameObject;
        bool accept = EditorUtility.DisplayDialog(string.Format("Remove Display {0}?", obj.name),
            string.Format("Are you sure you want to delete Display {0}?", obj.name), "Delete", "Cancel");
        if (accept)
        {
            Undo.DestroyObjectImmediate(obj);
        }
    }
    private void CreateDisplay<T>(MenuSettings<T> settings) where T : UnityEngine.Component
    {
        //Load 3d model.
        UnityEngine.Object modelObj = Resources.Load(String.Format("Displays/{0}", displayModels[selectedModel]));
        GameObject obj = Instantiate(modelObj) as GameObject;
        obj.name = settings.name;
        DisplayObject disp = obj.AddComponent<DisplayObject>();
        obj.tag = "VRDisplay";
        // Create settings.
        DisplaySettings settingsObj = CreateInstance<DisplaySettings>();
        AssetDatabase.CreateAsset(settingsObj, string.Format("Assets/VRSettings/Displays/{0}.asset", obj.name));
        disp.settings = settingsObj;
        // Continue depending on display type.
        switch (dispType)
        {
            case Gimbl.DisplayType.Monitor:
                // Go through screens surfaces.
                MeshRenderer[] meshes = obj.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer mesh in meshes)
                {
                    // Turn off collider.
                    mesh.GetComponent<MeshCollider>().enabled = false;
                    // Create camera.
                    GameObject cam = new GameObject(String.Format("Camera: {0}", mesh.name));
                    cam.transform.SetParent(mesh.transform.parent);
                    cam.transform.localPosition = new Vector3(0, 0, 0);
                    //Setup projection script.
                    Camera camComp = cam.AddComponent<Camera>();
                    camComp.nearClipPlane = 0.3f;
                    camComp.targetDisplay = 8;
                    camComp.clearFlags = CameraClearFlags.Skybox;
                    camComp.backgroundColor = Color.black;
                    PerspectiveProjection proj = cam.AddComponent<PerspectiveProjection>();
                    proj.projectionScreen = mesh.gameObject;
                    proj.setNearClipPlane = false;
                    mesh.enabled = false;
                }
                break;
        }
        Undo.RegisterCreatedObjectUndo(obj, "Create Display");
        settings.selected = disp as T;
        settings.name = "";
    }
}