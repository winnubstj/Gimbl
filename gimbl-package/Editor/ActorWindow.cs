using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using Gimbl;
using PathCreation;

public class ActorWindow : EditorWindow
{
    #region Menu Variables.
    Vector2 scrollPosition = Vector2.zero;
    public delegate void CreateFunc<T>(MenuSettings<T> settings) where T : UnityEngine.Object;



    // Stores menu settings.
    public class MenuSettings<T> where T : UnityEngine.Object
    {
        public string typeName;
        public bool[] show = { false, false, false, false, false };
        public string name = "";
        public int selectedInstanceId;
        public Rect editRect = new Rect(); // stores location editing window.
        private T _selectedObj;
        public T selectedObj
        {
            get {return _selectedObj;}
            set
            {
                if (!UnityEngine.Object.ReferenceEquals(value,_selectedObj))
                {
                    _selectedObj = value;
                    if (value!=null) { selectedInstanceId = value.GetInstanceID(); }
                    else { selectedInstanceId = 0; }
                }
            }
        }
    }
    // Need to make non-generic inherited class for having serializble variables (otherwise menu changes on run)
    [System.Serializable] public class ActorMenuSettings : MenuSettings<ActorObject> { }
    [System.Serializable] public class ControllerMenuSettings : MenuSettings<ControllerOutput> { }
    [System.Serializable] public class PathMenuSettings : MenuSettings<PathCreator> { }
    [SerializeField] private ActorMenuSettings actSettings = new ActorMenuSettings() { typeName = "Actor" };
    [SerializeField] private ControllerMenuSettings contSettings = new ControllerMenuSettings() { typeName = "Controller" };
    [SerializeField] private PathMenuSettings pathSettings = new PathMenuSettings(){ typeName = "Path" };

    // Actor specific variables.
    private string[] actorModels;
    private int selectedModel = 0;
    private bool trackCam = true;

    // Controller specific Variables.
    private ControllerTypes contType = ControllerTypes.LinearTreadmill;

    #endregion

    #region Window Setup.
    private static EditorWindow currentWindow;
    public static void ShowWindow()
    {
        currentWindow = GetWindow<ActorWindow>("Actors",true, typeof(MainWindow));
        currentWindow.Show();
    }
    private void OnEnable()
    {
        // Get actor models.
        Resources.LoadAll<GameObject>("Actors/Mouse");
        UnityEngine.Object[] data = Resources.LoadAll<GameObject>("Actors/Prefabs");
        actorModels = data.Select(x => x.name).ToArray();
        actorModels = actorModels.Union(new string[] { "None" }).ToArray();

    }
    #endregion
    private void OnGUI()
    {


        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition,GUILayout.Height(position.height), GUILayout.Width(position.width));
        #region ActorMenu
        EditorGUILayout.BeginVertical(LayoutSettings.mainBox.style);
        EditorGUILayout.LabelField("Actors", LayoutSettings.sectionLabel);

        // Select and delete actor.
        EditorGUILayout.BeginHorizontal(LayoutSettings.editWidth);
            SelectMenu(actSettings);
            if (GUILayout.Button("Delete", LayoutSettings.buttonOp)) actSettings.selectedObj.DeleteActor();
        EditorGUILayout.EndHorizontal();


        // Edit Actor.
        if (actSettings.selectedObj != null)
        {
            actSettings.selectedObj.EditMenu();
        }

        // Create Actor.
        if (EditorApplication.isPlaying) GUI.enabled = false;
        actSettings.show[0] = EditorGUILayout.Foldout(actSettings.show[0], "Create");
        if (actSettings.show[0])
        {
            EditorGUILayout.BeginVertical(LayoutSettings.subBox.style);
            EditorGUILayout.LabelField("Create Actor", EditorStyles.boldLabel);
            actSettings.name = EditorGUILayout.TextField("Actor Name: ", actSettings.name, LayoutSettings.editFieldOp);
            selectedModel = EditorGUILayout.Popup("Model: ", selectedModel, actorModels, LayoutSettings.editFieldOp);
            trackCam = EditorGUILayout.Toggle("Add Tracking Cam: ",trackCam);
            CreateButton(actSettings);
            EditorGUILayout.EndVertical();
        }
        GUI.enabled = true;
        EditorGUILayout.EndVertical();
        #endregion

        #region Controller
        EditorGUILayout.BeginVertical(LayoutSettings.mainBox.style);
        EditorGUILayout.LabelField("Controllers", LayoutSettings.sectionLabel);

        // Select and delete.
        EditorGUILayout.BeginHorizontal(LayoutSettings.editWidth);
            SelectMenu(contSettings);
            if (GUILayout.Button("Delete", LayoutSettings.buttonOp)) contSettings.selectedObj.master.DeleteController();
        EditorGUILayout.EndHorizontal();

        // Edit.
        contSettings.show[0] = EditorGUILayout.Foldout(contSettings.show[0], "Edit");
        if (contSettings.show[0])
        {
            if (contSettings.selectedObj != null)
            {
                EditorGUILayout.BeginVertical(LayoutSettings.subBox.style);
                // Custom edit menu.
                contSettings.selectedObj.master.EditMenu();
                // Save and load options.
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal(LayoutSettings.editFieldOp); GUILayout.FlexibleSpace();
                if (GUILayout.Button("Save Controller Settings", GUILayout.Width(250))) contSettings.selectedObj.master.SaveController();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                if (EditorApplication.isPlaying) GUI.enabled = false;
                EditorGUILayout.BeginHorizontal(LayoutSettings.editFieldOp); GUILayout.FlexibleSpace();
                if (GUILayout.Button("Load Controller Settings", GUILayout.Width(250))) contSettings.selectedObj.master.LoadController();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;
                EditorGUILayout.EndVertical();

                
            }
        }
        // Create.
        if (EditorApplication.isPlaying) GUI.enabled = false;
        contSettings.show[1] = EditorGUILayout.Foldout(contSettings.show[1], "Create");
        if (contSettings.show[1])
        {
            EditorGUILayout.BeginVertical(LayoutSettings.subBox.style);
            EditorGUILayout.LabelField("Create Controller", EditorStyles.boldLabel);
            contSettings.name = EditorGUILayout.TextField("Controller Name: ", contSettings.name, LayoutSettings.editFieldOp);
            contType = (ControllerTypes)EditorGUILayout.EnumPopup("Type: ",contType, LayoutSettings.editFieldOp);
            CreateButton(contSettings);
            EditorGUILayout.EndVertical();
        }
        GUI.enabled = true;
        EditorGUILayout.EndVertical();
        #endregion

        #region Paths
        EditorGUILayout.BeginVertical(LayoutSettings.mainBox.style);
        EditorGUILayout.LabelField("Paths", LayoutSettings.sectionLabel);

        // Select and delete.
        EditorGUILayout.BeginHorizontal(LayoutSettings.editWidth);
            SelectMenu(pathSettings);
            if (GUILayout.Button("Delete", LayoutSettings.buttonOp)) DeletePath();
        EditorGUILayout.EndHorizontal();

        // Create.
        if (EditorApplication.isPlaying) GUI.enabled = false;
        pathSettings.show[0] = EditorGUILayout.Foldout(pathSettings.show[0], "Create");
        if (pathSettings.show[0])
        {
            EditorGUILayout.BeginVertical(LayoutSettings.subBox.style);
            EditorGUILayout.LabelField("Create Path", EditorStyles.boldLabel);
            pathSettings.name = EditorGUILayout.TextField("Path Name: ", pathSettings.name, LayoutSettings.editFieldOp);
            CreateButton(pathSettings);
            EditorGUILayout.EndVertical();
        }
        GUI.enabled = true;
        EditorGUILayout.EndVertical();
        #endregion
        GUILayout.EndScrollView();
    }

    // Menu Functions.
    private void SelectMenu<T>(MenuSettings<T> settings) where T : UnityEngine.Object
    {
        // Object cant be found (possible serialization on run)
        if (settings.selectedObj == null)
        {
            T obj = null;
            // Check if instance ID is valid
            if (settings.selectedInstanceId != 0)
            {
                try { obj = (T)EditorUtility.InstanceIDToObject(settings.selectedInstanceId); }
                catch (System.InvalidCastException) { obj = null; } // catches changed instanceID on restart.
            }
            // Otherwise find first on list.
            if (obj==null){ obj = FindObjectOfType<T>();}
                if (obj != null) { settings.selectedObj = obj; }
        }
        settings.selectedObj = (T)EditorGUILayout.ObjectField(settings.selectedObj, typeof(T), true);
    }

    private void CreateButton<T>(MenuSettings<T> settings) where T:UnityEngine.Object
    {
        EditorGUILayout.BeginHorizontal();
            T[] objs = FindObjectsOfType<T>();
            string[] names = objs.Select(x => x.name).ToArray();
            string msg = "";
            if (ArrayUtility.Contains(names, settings.name)) { msg = "Duplicate name"; GUI.enabled = false; }
            if (settings.name == "") { msg = "Empty Name"; GUI.enabled = false; }
            EditorGUILayout.LabelField(msg, GUILayout.Width(197));
            if (GUILayout.Button("Create", LayoutSettings.buttonOp))
            {
                GameObject obj = new GameObject(settings.name);
                //Controller.
                if (typeof(T)==typeof(ControllerOutput))
                {
                    //Create Controller. 
                    ControllerObject cont = (ControllerObject)obj.AddComponent(System.Type.GetType(string.Format("Gimbl.{0}", contType.ToString())));
                    cont.InitiateController();
                    //Create general Output Object and link.
                    ControllerOutput contOut = obj.AddComponent<ControllerOutput>();
                    contOut.master = cont;
                    // Select created.
                    settings.selectedObj = contOut as T;
                }
                //Actor.
                if (typeof(T) == typeof(ActorObject))
                {
                    ActorObject act = obj.AddComponent<ActorObject>();
                    act.InitiateActor(actorModels[selectedModel], trackCam);
                    settings.selectedObj = act as T;
                }
                //Path.
                if (typeof(T) == typeof(PathCreator))
                {
                    CreatePath(settings as MenuSettings<PathCreator>, obj);
                }
                settings.name = "";
        }
        EditorGUILayout.EndHorizontal();
    }

    // Path Manipulation Functions
    private void DeletePath()
    {
        GameObject obj = pathSettings.selectedObj.gameObject;
        bool accept = EditorUtility.DisplayDialog(string.Format("Remove Path {0}?", obj.name),
            string.Format("Are you sure you want to delete Path {0}?", obj.name), "Delete", "Cancel");
        if (accept)
        {
            // Not deleting scriptable object asset so delete it can be undone.
            Undo.DestroyObjectImmediate(obj);
        }
    }

    private void CreatePath(MenuSettings<PathCreator> settings,GameObject obj)
    {
        obj.transform.SetParent(GameObject.Find("Paths").transform);
        PathCreator path = obj.AddComponent<PathCreator>();
        path.bezierPath.Space = PathSpace.xz;
        path.bezierPath.ControlPointMode = BezierPath.ControlMode.Automatic;
        Undo.RegisterCreatedObjectUndo(obj, "Create Path");
        settings.selectedObj = path;
    }

}