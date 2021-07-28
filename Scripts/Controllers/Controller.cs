using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Gimbl
{
    // Avialable types.
    public enum ControllerTypes
    {
        LinearTreadmill,
        SimulatedLinearTreadmill,
        SphericalTreadmill,
        SimulatedSphericalTreadmill,
    }

    public abstract class ControllerObject : MonoBehaviour
    {
        public ActorObject Actor;
        public abstract void EditMenu();                            // Custom edit menu.
        public abstract void LinkSettings(string assetPath = "");   // Creates or links a settings file (ScriptableObject).

        // Handling of gamepads
        public Gamepad gamepad = new Gamepad();
        public string[] deviceNames = Gamepad.GetDeviceNames();

        // General buffer for inputs and smoothing.
        public class ValueBuffer
        {
            public float[] x;
            public float[] y;
            public float[] z;
            public Vector3 res = new Vector3();
            public int bufferSize;
            public int counter;
            private bool isCircular;
            public ValueBuffer(int reqBufferSize, bool reqIsCircular)
            {
                bufferSize = reqBufferSize;
                x = new float[bufferSize];
                y = new float[bufferSize];
                z = new float[bufferSize];
                counter = 0;
                isCircular = reqIsCircular;
            }
            public void Add(float newX, float newY, float newZ)
            {
                x[counter] = newX;
                y[counter] = newY;
                z[counter] = newZ;
                counter++;
                if (counter == bufferSize)
                {
                    if (isCircular) { counter = 0; }
                    else { counter = bufferSize-1; } //overwrites.
                }
            }
            public Vector3 Sum()
            {
                res.x = 0; res.y = 0; res.z = 0;
                for (int i = 0; i < GetBufferLimit(); i++)
                { res.x += x[i]; res.y += y[i]; res.z += z[i]; }
                return res;
            }
            public Vector3 Average()
            {
                res.x = 0; res.y = 0; res.z = 0;
                for (int i = 0; i < GetBufferLimit(); i++)
                { res.x += x[i]; res.y += y[i]; res.z += z[i]; }
                res.x /= bufferSize; res.y /= bufferSize; res.z /= bufferSize;
                return res;
            }
            public void Clear()
            {
                res.x = 0; res.y = 0; res.z = 0;
                for (int i = 0; i < GetBufferLimit(); i++)
                { x[i] = 0; y[i] = 0; z[i] = 0; }
                counter = 0;
            }

            private int GetBufferLimit()
            {
                if (isCircular) { return bufferSize; }
                else { return counter; }
            }
        }
        public ValueBuffer smoothBuffer;
        public ValueBuffer movement = new ValueBuffer(100, false); // Stores ball rotations.
        public int GetBufferSize(float setting)
        {
            int size = (int)(((float)setting / 1000) / (1f / Screen.currentResolution.refreshRate));
            if (size == 0) size = 1;
            return size;
        }

        public void InitiateController()
        {
            gameObject.transform.SetParent(GameObject.Find("Controllers").transform);
            // Create settings file.
            LinkSettings();
            //update main controller parent.
            UnityEditor.Undo.RegisterCreatedObjectUndo(gameObject, "Create Controller");
        }

        public void SaveController()
        {
            GameObject controller = this.gameObject;
            // get controller type and file extension.
            string sourceType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(string.Format("Assets/VRSettings/Controllers/{0}.asset", this.name)).ToString();
            string[] s = sourceType.Split('.');
            string extension = s[1];
            // File dialogue.
            string outputFile = UnityEditor.EditorUtility.SaveFilePanel("Save Controller settings as..",
             "",
             "",
             extension);
            if (outputFile.Length == 0) return;
            UnityEditor.AssetDatabase.SaveAssets();
            string sourcePath = System.IO.Path.Combine(Application.dataPath, string.Format("VRSettings/Controllers/{0}.asset", controller.name));
            UnityEditor.FileUtil.ReplaceFile(sourcePath, outputFile);
        }

        public void LoadController()
        {
            GameObject controller = this.gameObject;
            // get controller type and file extension.
            string sourceFile = string.Format("Assets/VRSettings/Controllers/{0}.asset", this.gameObject.name);
            string sourceType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(sourceFile).ToString();
            string[] s = sourceType.Split('.');
            string extension = s[1];
            // File Dialogue.
            string inputFile = UnityEditor.EditorUtility.OpenFilePanel("Import Setup", Application.dataPath, extension);
            if (inputFile.Length == 0) return;
            // Remove current settings file.
            string settingsFileAssetPath = string.Format("Assets/VRSettings/Controllers/{0}.asset", controller.name);
            UnityEditor.AssetDatabase.DeleteAsset(settingsFileAssetPath);
            // Copy new file to location.
            string newLoc = System.IO.Path.Combine(Application.dataPath, string.Format("VRSettings/Controllers/{0}.asset", controller.name));
            UnityEditor.FileUtil.CopyFileOrDirectory(inputFile, newLoc);
            UnityEditor.AssetDatabase.ImportAsset(settingsFileAssetPath);
            // Link to controller.
            controller.GetComponent<ControllerObject>().LinkSettings(settingsFileAssetPath);
        }

        public void DeleteController()
        {
            GameObject controller = this.gameObject;
            bool accept = UnityEditor.EditorUtility.DisplayDialog(string.Format("Remove Controller {0}?", controller.name),
                string.Format("Are you sure you want to delete Controller {0}?", controller.name), "Delete", "Cancel");
            if (accept)
            {
                // Not deleting scriptable object asset so delete it can be undone.
                UnityEditor.Undo.DestroyObjectImmediate(controller);
            }
        }

        public void ControllerMenuTitle(bool isActive, string type)
        {
            EditorGUILayout.BeginHorizontal();
            if (isActive && Actor != null) { EditorGUILayout.LabelField(string.Format("<color=#66CC00>{0}</color> - {1}", name,type), LayoutSettings.controllerLabel); }
            else { EditorGUILayout.LabelField(string.Format("<color=#EE0000>{0}</color> - {1}", name,type), LayoutSettings.controllerLabel); }
            EditorGUILayout.EndHorizontal();
        }

    }

}
