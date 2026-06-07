using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using ArenaCraft;

namespace Unity.AI.Assistant.PlayModeTest
{
    [InitializeOnLoad]
    internal static class PlayModeTestRunner
    {
        private const string StateKey = "PlayModeTest.State";
        private const string ResultKey = "PlayModeTest.Result";
        private const string ScriptPathKey = "PlayModeTest.ScriptPath";
        private const string SentinelLog = "PLAY_MODE_TEST_COMPLETE";

        private static readonly int WaitFrames = SessionState.GetInt("PlayModeTest.WaitFrames", 10);
        private static readonly float TestTimeout = SessionState.GetFloat("PlayModeTest.TestTimeout", 10.0f);

        private static List<string> _capturedLogs = new List<string>();
        private const int MaxCapturedLogs = 100;

        static PlayModeTestRunner()
        {
            string state = SessionState.GetString(StateKey, "Idle");
            switch (state)
            {
                case "Idle": break;
                case "WaitingForCompile":
                    EditorApplication.delayCall += () => {
                        SessionState.SetString(StateKey, "EnteringPlayMode");
                        EditorApplication.isPlaying = true;
                    };
                    break;
                case "EnteringPlayMode":
                    if (EditorApplication.isPlaying) {
                        SessionState.SetString(StateKey, "InPlayMode");
                        EditorApplication.update += WaitFramesThenRun;
                    }
                    break;
                case "InPlayMode":
                    if (EditorApplication.isPlaying) EditorApplication.update += WaitFramesThenRun;
                    break;
                case "Done":
                    EditorApplication.delayCall += SelfDestruct;
                    break;
            }
        }

        private static int _frameCount = 0;
        private static bool _setupDone = false;
        private static bool _testDone = false;
        private static double _testStartTime = 0;
        private static Vector3 _p1StartPos;
        private static Keyboard _keyboard;

        private static void WaitFramesThenRun()
        {
            _frameCount++;
            if (_frameCount < WaitFrames) return;
            if (_testDone) return;

            if (!_setupDone)
            {
                _setupDone = true;
                Application.logMessageReceived += OnLogMessage;
                _testStartTime = EditorApplication.timeSinceStartup;
                Setup();
                return;
            }

            float elapsed = (float)(EditorApplication.timeSinceStartup - _testStartTime);
            bool timedOut = elapsed >= TestTimeout;
            bool complete = Tick(elapsed);

            if (complete || timedOut) FinishTest(timedOut && !complete, timedOut ? "Timeout" : null);
        }

        private static void Setup()
        {
            Debug.Log("[Test] Setup: Checking Players and Input");
            var p1 = GameObject.Find("RPGHeroHP");
            if (p1 != null) {
                _p1StartPos = p1.transform.position;
                var input = p1.GetComponent<PlayerInputProvider>();
                if (input != null) Debug.Log("[Test] P1 Input Asset: " + (input.controls != null ? input.controls.name : "NULL"));
            }

            _keyboard = InputSystem.AddDevice<Keyboard>();
            Debug.Log("[Test] Keyboard device added for simulation");
        }

        private static bool Tick(float elapsed)
        {
            // Simulate W key press for 1 second
            if (elapsed > 1.0f && elapsed < 2.0f)
            {
                InputSystem.QueueDeltaStateEvent(_keyboard.wKey, 1.0f);
            }
            else if (elapsed >= 2.0f)
            {
                InputSystem.QueueDeltaStateEvent(_keyboard.wKey, 0.0f);
            }

            if (elapsed >= 4.0f)
            {
                var p1 = GameObject.Find("RPGHeroHP");
                if (p1 != null)
                {
                    float dist = Vector3.Distance(_p1StartPos, p1.transform.position);
                    Debug.Log("[Test] P1 Movement distance: " + dist);
                    if (dist < 0.01f) Debug.LogWarning("[Test] P1 did not move!");
                }
                return true;
            }
            return false;
        }

        private static void FinishTest(bool isError, string msg)
        {
            _testDone = true;
            EditorApplication.update -= WaitFramesThenRun;
            Application.logMessageReceived -= OnLogMessage;

            var result = new TestResult { success = !isError, error = msg, logs = _capturedLogs.ToArray() };
            SessionState.SetString(ResultKey, JsonUtility.ToJson(result));
            SessionState.SetString(StateKey, "Done");
            EditorApplication.isPlaying = false;
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            if (_capturedLogs.Count < MaxCapturedLogs) _capturedLogs.Add("[" + type + "] " + message);
        }

        private static void SelfDestruct()
        {
            string path = SessionState.GetString(ScriptPathKey, "");
            if (AssetDatabase.AssetPathExists(path)) AssetDatabase.DeleteAsset(path);
            SessionState.EraseString(StateKey);
            SessionState.EraseString(ScriptPathKey);
        }

        [System.Serializable] private class TestResult { public bool success; public string error; public string[] logs; }
    }
}
