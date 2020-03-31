using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Gimbl
{
    [System.Serializable]
    public partial class ActorObject : MonoBehaviour
    {
        public bool isActive = true; // for disabling movement.

        // Idle detection
        private System.Diagnostics.Stopwatch idleStopWatch = new System.Diagnostics.Stopwatch();
        private Vector3 prevPos;
        private float idleTime = 0;
        private IdleMessage idleMsg = new IdleMessage();
        private MQTTChannel<IdleMessage> idleChan;
        public class IdleMessage
        {
            public string name;
            public int idleTime;
        }

        // For Logging.
        public class ActorLogMessage
        {
            public string name;
            public int[] position = new int[3];
            public int[] heading = new int[3];
        }
        public ActorLogMessage actLogMsg = new ActorLogMessage();

        public class PathLogMsg
        {
            public string name = "";
            public string pathName = "";
            public int position = 0;
        }
        public PathLogMsg pathMsg = new PathLogMsg();

        // Linked Display Object.
        [SerializeField] private DisplayObject _display;
        public DisplayObject display
        {
            get { return _display; }
            set {
                if (value!=_display)
                {
                    // parent if new displayObject
                    if (value!=null) { value.ParentToActor(this); }
                    // If previous display excisted -> unparent
                    if (_display!=null) { _display.Unparent(); }
                    _display = value;
                }   
                }
        }

        LogFile logger;
        [SerializeField] public ActorSettings settings;
        [SerializeField] private ControllerOutput _controller;
        [SerializeField] private AudioListener listener;
        //Check that only one controller can be linked to one actor.
        [SerializeField]
        public ControllerOutput controller
        {
            get { return _controller; }
            set
            {
                if (_controller!=value)
                {
                    //change values.
                    if (_controller != null) _controller.master.Actor = null; //abandon.
                    _controller = value;
                    if (value != null)
                    {
                        value.master.Actor = this;
                        // make sure other actors are no longer coupled.
                        foreach (ActorObject act in FindObjectsOfType<ActorObject>())
                        {
                            if (act.controller == value && act != this)
                            {
                                Debug.LogWarning(string.Format("Switched Controller {0} from {1} to {2}", value.gameObject.name, act.gameObject.name, this.gameObject.name));
                                act._controller = null; // stops looping.
                            }
                        }
                    }
                    if (!EditorApplication.isPlaying) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                }
            }
        }

        public void Start() {
            logger = FindObjectOfType<LoggerObject>().logFile;
            actLogMsg.name = name;
            // Setup idle monitor.
            idleChan = new MQTTChannel<IdleMessage>("Gimbl/Idle/");
            idleStopWatch.Start();
            prevPos = transform.position; }

        public void LateUpdate()
        {
            // Log Absolute Position.
            int roundFct = 1000;
            actLogMsg.position[0] = (int)(gameObject.transform.position.x * roundFct);
            actLogMsg.position[1] = (int)(gameObject.transform.position.y * roundFct);
            actLogMsg.position[2] = (int)(gameObject.transform.position.z * roundFct);
            actLogMsg.heading[0] = (int)(gameObject.transform.rotation.x * roundFct); 
            actLogMsg.heading[1] = (int)(gameObject.transform.rotation.y * roundFct);
            actLogMsg.heading[2] = (int)(gameObject.transform.rotation.z * roundFct);
            logger.Log("Position", actLogMsg);

            // Log position on path (if applicable).
            if (controller != null)
            {
                if (controller.master.GetType() == typeof(LinearTreadmill) || controller.master.GetType() == typeof(SimulatedLinearTreadmill))
                {
                    if (((LinearTreadmill)controller.master).path != null)
                    {
                        pathMsg.name = name;
                        pathMsg.pathName = ((LinearTreadmill)controller.master).path.name;
                        pathMsg.position = (int)(((LinearTreadmill)controller.master).path.path.GetClosestDistanceAlongPath(gameObject.transform.position) * 1000);
                        logger.Log("Path Position", pathMsg);
                    }
                }
            }

            // Check idle.
            if (idleStopWatch.ElapsedMilliseconds > 2000)
            {
                idleStopWatch.Restart();
                if (Vector3.Distance(prevPos, transform.position) < 0.5)
                {
                    idleTime += 2000;
                    if (idleTime > (settings.idleTimeOut * 1000 * 60))
                    {
                        prevPos = transform.position;
                        idleTime = 0;
                        // Send signal.
                        idleMsg.name = name;
                        idleMsg.idleTime = (int)(settings.idleTimeOut * 60000);
                        idleChan.Send(idleMsg);
                        logger.Log("Idle!");
                    }
                }
                else
                {
                    prevPos = transform.position;
                    idleTime = 0;
                }
            }

        }

        public void InitiateActor(string modelStr, bool trackCam)
        {
            gameObject.transform.SetParent(GameObject.Find("Actors").transform);
            ActorSettings asset = ScriptableObject.CreateInstance<ActorSettings>();
            AssetDatabase.CreateAsset(asset, string.Format("Assets/VRSettings/Actors/{0}.asset", gameObject.name));
            settings = asset;
            // Add Audio Listener.
            listener = gameObject.AddComponent<AudioListener>();
            listener.enabled = false;
            // Add Character Controller.
            CharacterController charObj = gameObject.AddComponent<CharacterController>();
            charObj.slopeLimit = 45;
            charObj.stepOffset = 0.000001f;
            charObj.skinWidth = 0.05f;
            charObj.minMoveDistance = 0.001f;
            charObj.center = new Vector3(0, 0.55f, 0);
            charObj.radius = 0.5f;
            charObj.height = 0.1f;
            // Set render layer.
            TagLayerEditor.TagsAndLayers.AddLayer(gameObject.name);
            // Model.
            if (modelStr != "None")
            {
                UnityEngine.Object modelObj = Resources.Load(string.Format("Actors/Prefabs/{0}", modelStr));
                GameObject model = Instantiate(modelObj) as GameObject;
                model.name = string.Format("Model {0}", modelStr);
                model.transform.SetParent(gameObject.transform);
                model.layer = LayerMask.NameToLayer(gameObject.name);
            }
            // Tracking Cam.
            if (trackCam)
            {
                // Find currently targetted displays.
                List<int> usedDisplays = new List<int>();
                List<int> availableDisplays = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7 };
                TagLayerEditor.TagsAndLayers.AddTag("TrackCam");
                foreach (GameObject trackObj in GameObject.FindGameObjectsWithTag("TrackCam"))
                {
                    usedDisplays.Add(trackObj.GetComponent<Camera>().targetDisplay);
                }
                int[] displays = availableDisplays.Except(usedDisplays).ToArray();
                int nextDisp = 7; // default
                if (displays.Length > 0) { nextDisp = displays[0]; }
                // Create display.
                GameObject cam = new GameObject(string.Format("Track Cam: {0}", settings.name));
                Camera camComp = cam.AddComponent<Camera>();
                cam.transform.parent = gameObject.transform;
                cam.transform.localPosition = new Vector3(0, 1, -1.3f);
                cam.transform.eulerAngles = new Vector3(20, 0, 0);
                camComp.clearFlags = CameraClearFlags.Skybox;
                camComp.backgroundColor = Color.black;
                // Set target display.
                cam.tag = "TrackCam";
                camComp.targetDisplay = nextDisp;

            }
            // Update.
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Actor");
        }
        public void DeleteActor()
        {
            bool accept = EditorUtility.DisplayDialog(string.Format("Remove Actor {0}?", name),
                string.Format("Are you sure you want to delete Actor {0}?", name), "Delete", "Cancel");
            if (accept)
            {
                // Not deleting scriptable object asset so delete it can be undone.
                TagLayerEditor.TagsAndLayers.RemoveLayer(name);
                // unparent attached displays.
                PerspectiveProjection cam = GetComponentInChildren<PerspectiveProjection>();
                if (cam != null) cam.transform.parent.transform.SetParent(null);
                Undo.DestroyObjectImmediate(gameObject);
            }
        }

        public void EditMenu()
        {
            EditorGUILayout.BeginVertical(LayoutSettings.subBox.style);
            // Controller.
            EditorGUILayout.BeginHorizontal();
            if (controller != null)
                EditorGUILayout.LabelField("<color=#66CC00>Controller: </color>", LayoutSettings.linkFieldStyle, LayoutSettings.linkFieldLayout);
            else
                EditorGUILayout.LabelField("<color=#EE0000>Controller: </color>", LayoutSettings.linkFieldStyle, LayoutSettings.linkFieldLayout);
            controller = (ControllerOutput)EditorGUILayout.ObjectField(controller, typeof(ControllerOutput), true, LayoutSettings.linkObjectLayout);
            EditorGUILayout.EndHorizontal();
            // Display.
            EditorGUILayout.BeginHorizontal();
            if (display != null)
                EditorGUILayout.LabelField("<color=#66CC00>Display: </color>", LayoutSettings.linkFieldStyle, LayoutSettings.linkFieldLayout);
            else
                EditorGUILayout.LabelField("<color=#EE0000>Display: </color>", LayoutSettings.linkFieldStyle, LayoutSettings.linkFieldLayout);
            display = (DisplayObject)EditorGUILayout.ObjectField(display, typeof(DisplayObject), true, LayoutSettings.linkObjectLayout);
            EditorGUILayout.EndHorizontal();
            // Is Audio Listener.
            bool newActiveListener = EditorGUILayout.Toggle("Audio Listener: ", listener.enabled);
            // If turned on disable other listener.
            if (newActiveListener)
            {
                foreach(AudioListener list in FindObjectsOfType<AudioListener>())
                {
                    list.enabled = false;
                }
            }
            listener.enabled = newActiveListener;
            // Display.
            EditorGUILayout.BeginHorizontal();
            settings.idleTimeOut = EditorGUILayout.FloatField("Idle Time-out (mins.): ", settings.idleTimeOut);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    }

}

