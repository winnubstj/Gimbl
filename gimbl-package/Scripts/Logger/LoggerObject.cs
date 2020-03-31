using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using UnityEditor;

namespace Gimbl
{
    public class LoggerObject : MonoBehaviour
    {

        public class MQTTLogFile
        {
            public string filePath;
        }

        // Start info.
        private class StartMsg
        {
            public string time;
            public string project;
            public string scene;
        }
        private StartMsg startMsg = new StartMsg();

        //Log variables.
        public LogFile logFile;
        public LoggerSettings settings;

        public void OnEnable()
        {
            // Logger object start mqtt client to ensure its ready.
            GameObject.FindObjectOfType<Gimbl.MQTTClient>().Connect(false);
            StartLog();
        }
        private void StartLog()
        {
            MQTTChannel<MQTTLogFile> logFileChannel = new MQTTChannel<MQTTLogFile>("Gimbl/Session/LogFile");
            logFileChannel.Event.AddListener(OnLogFileName);
            if (EditorPrefs.GetBool("Gimbl_externalLog", false)==false)
            {
                // Check if log file already exists.
                string filePath = Path.Combine(Application.dataPath, "Logs",
                    string.Format("{0}-{1}.json", settings.outputFile, settings.sessionId));
                if (CheckFileExists(filePath)) { return; }
                // Create log file.
                logFile = new LogFile(filePath);
                //Update counter.
                settings.sessionId++;
                UnityEditor.EditorUtility.SetDirty(settings);
            }
            else
            {
                while (logFile == null) { };
                if (CheckFileExists(logFile.filePath)) { return; };
            }
        }
        private void Start()
        {
            //StartLog();
            // Log start message.
            startMsg.time = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            startMsg.scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            startMsg.project = PlayerSettings.productName;
            logFile.Log("Info", startMsg);
            // Setup Listener.
            MQTTRawChannel channel = new MQTTRawChannel("Log/");
            channel.Event.AddListener(OnMessage);
            // Start Times. 
            logFile.stopwatch.Start();
        }

        private void OnApplicationQuit()
        {
            // Close stream.
            logFile.Close();

        }
        private bool CheckFileExists(string filePth)
        {
            if (System.IO.File.Exists(filePth))
            {
                UnityEngine.Debug.LogError(string.Format("Log File:{0} already exists", Path.GetFileName(filePth)));
                UnityEditor.EditorApplication.isPlaying = false;
                settings.sessionId++;
                return true;
            }
            else { return false; }
        }
        private void OnMessage(string msg) { logFile.LogJson("Log",msg); }
        private void OnLogFileName(MQTTLogFile msg) { if (logFile == null) { logFile = new LogFile(msg.filePath); } }
    }
    public class LogFile
    {
        public string filePath;
        private bool hasWritten = false;
        public StreamWriter stream;
        public System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        //Constructor.
        public LogFile(string path)
        {
            filePath = path;
            stream = new StreamWriter(string.Format("{0}~",path), true, System.Text.Encoding.ASCII, 65536);
            stream.Write("[");
        }
        public void Log(string msg)
        {
            if (stream.BaseStream != null)
            {
                lock (stream)
                {
                    WriteHeader(msg);
                    stream.Write("{}}");
                }
            }
        }

        public void Log<T>(string msg,T data)
        {
            if (stream.BaseStream != null)
            {
                lock (stream)
                {
                    WriteHeader(msg);
                    stream.Write(JsonUtility.ToJson(data).ToString());
                    stream.Write("}");
                }
            }
        }

        public void LogJson(string header,string jsonStr)
        {
            if (stream.BaseStream != null)
            {
                lock (stream)
                {
                    WriteHeader("Log");
                    stream.Write(jsonStr);
                    stream.Write("}");
                }
            }
        }
        private void WriteHeader(string msg)
        {
            if (hasWritten) { stream.Write(",\n"); } else { hasWritten = true; }
            stream.Write("{\"time\":");
            stream.Write(((float)stopwatch.ElapsedTicks/10000f).ToString());
            stream.Write(",\"msg\":\"");
            stream.Write(msg);
            stream.Write("\",\"data\":");
        }
        public void Close()
        {
            // Close writing.
            stream.Write("]");
            stream.Close();
            // Remove temporary flag from log file "~" so that we can import the log file.
            System.IO.File.Move(string.Format("{0}~", filePath),filePath);
            if (EditorPrefs.GetBool("Gimbl_externalLog", false) == false)
            {
                UnityEditor.AssetDatabase.ImportAsset(string.Format("Assets/Logs/{0}", Path.GetFileName(filePath)));
            }
        }
    }
}