#if TEST_FRAMEWORK
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines;
using Unity.EditorCoroutines.Editor;

using UnityEngine.Networking;

namespace edu.ucf.gaim
{
    [Serializable]
    public struct LogMessage
    {
        public string msg;
        public string src;
        public string st;
        public string type;
    }

    public struct CFSettings
    {
        public string type;
        public string course;
        public string host;
        public string secret;
        public string assignment;
        public string term;
        public string submission;
        public string slug;
        public string curBranch;
    }
    // ensure class initializer is called whenever scripts recompile
    [InitializeOnLoadAttribute]
    public static class GaiMBridge
    {
        public static long epochTicks = new DateTime(1970, 1, 1).Ticks;
        public static CFSettings gaimSettings;
        public static string output = "";
        public static string stack = "";
        public static string secret = "";
        public static string repo = "";
        public static bool enabled = false;

        public struct LogMessages
        {
            public string ts;
            public List<LogMessage> log;
        }
        public static LogMessages logMessages = new LogMessages { log = new List<LogMessage>() };
        public static int logCount = 0;
        static LogMessage lm;

        /// <summary>
        /// This function is called when this object becomes enabled and active
        /// </summary>
        static GaiMBridge()
        {
            string[] s = Application.dataPath.Split('/');
            string projectName = s[s.Length - 2];
            string settings = EditorPrefs.GetString("settings-" + projectName);
            var ucfDir = Application.dataPath.Substring(0, Application.dataPath.Length - 7) + "/.ucf";
            char sep = Path.DirectorySeparatorChar;
            if (!!Directory.Exists(ucfDir))
            {
                return;
            }
            if (settings == null || settings == "")
            {
                StreamReader sr;
                if (!Directory.Exists(ucfDir) &&
                    File.Exists(ucfDir + sep + "vsc.json"))
                {
                    enabled = true;
                    if (!gaimSettings.Equals(null) && gaimSettings.host == null)
                    {
                        sr = new StreamReader(ucfDir + sep + "vsc.json", false);
                        settings = sr.ReadToEnd();
                        sr.Close();
                        EditorPrefs.SetString("settings-" + projectName, settings);
                        gaimSettings = JsonUtility.FromJson<CFSettings>(settings);
                    }
                    gaimSettings.host = "https://plato.mrl.ai:8081";
                    secret = gaimSettings.secret;
                }
                else
                {
                    return;
                }
            }
            else
            {
                gaimSettings = JsonUtility.FromJson<CFSettings>(settings);
                gaimSettings.host = "https://plato.mrl.ai:8081";
            }
            UnityEditor.EditorApplication.playModeStateChanged += LogPlayModeState;
            Application.logMessageReceived += HandleAppLog;
            EditorApplication.playModeStateChanged += LogPlayModeState;
            EditorApplication.wantsToQuit += Quit;
            HandleLog("Reload/Start", "", "9");
            File.WriteAllText(Application.dataPath.Substring(0, Application.dataPath.Length - 7) + "/.ucf/.running", "Running");
        }
        static bool Quit()
        {
            if (enabled)
            {
                HandleLog("Application Quit", "", "9");
                File.Delete(Application.dataPath.Substring(0, Application.dataPath.Length - 7) + "/.ucf/.running");
                lm = new LogMessage
                {
                    st = "",
                    src = "unity",
                    msg = "Application Quit",
                    type = "9"
                };
                logMessages.log.Clear();
                logMessages.log.Add(lm);
                var url = gaimSettings.host + "/git/event";
                var request = new UnityWebRequest(url, "POST");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(logMessages));
                request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                // request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                // Debug.Log("secret: " + secret);
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("secret", secret);
                request.SetRequestHeader("repo", repo);
                request.SendWebRequest();
            }
            return true;
        }
        // register an event handler when the class is initialized

        private static void LogPlayModeState(PlayModeStateChange state)
        {
            var state_str = "";
            switch (state)
            {
                case PlayModeStateChange.EnteredEditMode:
                    state_str = "EnteredEditMode";
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    state_str = "EnteredPlayMode";
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    state_str = "ExitEditMode";
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    state_str = "ExitPlayMode";
                    break;

            }
            HandleLog("PlayModeState", "6", state_str);
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void DidReloadScripts()
        {
            HandleLog("{\"compiled\":true}", "", "compiled");
            File.Delete(Application.dataPath.Substring(0, Application.dataPath.Length - 7) + "/.ucf/.compilerError");
        }
        private static String prettyPrintErrors()
        {
            string str = "";
            foreach (var msg in logMessages.log)
            {
                if (msg.type == LogType.Error.ToString())
                    str += msg.msg + "\n\r";
            }
            return str;
        }

        static IEnumerator Post(string url, string bodyJsonString)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            // request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            // Debug.Log("secret: " + secret);
            request.SetRequestHeader("Content-Type", "application/json");
            if (secret != null)
            {
                request.SetRequestHeader("secret", secret);
            }
            if (repo != null)
            {
                request.SetRequestHeader("repo", repo);
            }
            yield return request.SendWebRequest();
        }
        // keep a copy of the executing script
        private static EditorCoroutine coroutine;

        static IEnumerator EditorAttempt(float waitTime)
        {
            yield return new EditorWaitForSeconds(waitTime);
            var errorText = prettyPrintErrors();
            EditorCoroutineUtility.StartCoroutineOwnerless(Post(gaimSettings.host + "/git/event", JsonUtility.ToJson(logMessages)));
            if (logMessages.log.Count > 0 && errorText.Length > 0)
            {
                File.WriteAllText(Application.dataPath.Substring(0, Application.dataPath.Length - 7) + "/.ucf/.compilerError", errorText);
            }
            else
            {
                File.Delete(Application.dataPath.Substring(0, Application.dataPath.Length - 7) + "/.ucf/.compilerError");
            }
            logMessages.log.Clear();
        }
        // 
        static void HandleAppLog(string logString, string stackTrace, LogType type)
        {
            HandleLog(logString, stackTrace, type.ToString());
        }
        public static string prevMsg = "";
        public static int repeated = 0;

        static void HandleLog(string logString, string stackTrace, string type = "")
        {
            if (logString != prevMsg)
            {
                output = logString;
                prevMsg = output;
                stack = stackTrace;
                lm = new LogMessage
                {
                    st = stack,
                    src = "unity",
                    msg = output,
                    type = type
                };
                if (repeated > 0)
                {
                    lm.msg = lm.msg + repeated.ToString();
                }
                logMessages.log.Add(lm);
                if (logCount == 0)
                {
                    logMessages.ts = DateTime.UtcNow.ToString();
                    coroutine = EditorCoroutineUtility.StartCoroutineOwnerless(EditorAttempt(0.5f));
                }
                else
                {
                    EditorCoroutineUtility.StopCoroutine(coroutine);
                    coroutine = EditorCoroutineUtility.StartCoroutineOwnerless(EditorAttempt(0.5f));
                }
            }
            else
            {
                repeated++;
            }
            // sw.Close();
        }
    }
}
#endif
