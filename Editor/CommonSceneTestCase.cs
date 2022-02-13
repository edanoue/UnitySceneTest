// Copyright Edanoue, Inc. MIT License - see LICENSE.md

#nullable enable

using System.Collections;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using NUnit.Framework.Internal;
using Edanoue.SceneTest.Interfaces;

namespace Edanoue.SceneTest
{
    public static class CommonSceneTestCase
    {
        private static class Log
        {
            public static void Info(string msg)
            {
                UnityEngine.Debug.Log($"[{nameof(SceneTest)}] {msg}");
            }
            public static void Warning(string msg)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(SceneTest)}] {msg}");
            }
        }

        public static IEnumerator RunTest(string sceneAbsPath, TestSuite fixture)
        {
            // 指定されたシーンを読み込む
            yield return LoadTestSceneAsync(sceneAbsPath);

            // ロードされている全てのシーン内から TestCase を収集する
            ISceneTestCaseCollecter caseCollecter = new SceneTestCaseCollecter();
            if (!caseCollecter.Collect())
            {
                Log.Warning($"Not founded any {nameof(ISceneTestCase)} implemented components. skipped testing.");
                yield break;
            }

            // テストランナーを生成する
            ISceneTestRunner runner = new SceneTestRunner(caseCollecter);

            // Suite の PropertieBag にアクセスする
            var testSuiteProperties = fixture.Properties;

            // Timeout を取得する
            // Default は 10 秒としておく
            float timeoutSec = 10f;

            // Timeout が Suite に存在したら
            if (testSuiteProperties.ContainsKey(PropertyNames.Timeout))
            {
                // こちらでも 設定する
                // ms が指定されているので, s に変換する
                var timeoutMs = (int)testSuiteProperties.Get(PropertyNames.Timeout);
                timeoutSec = (float)timeoutMs / 1000f;
                // こちら側は 0.1 秒だけ短くしておく
                timeoutSec = UnityEngine.Mathf.Max(0.01f, timeoutSec - 0.1f);
            }

            // テストを実行する
            yield return runner.RunAll(new RunnerOptions()
            {
                GlobalTimeoutSeconds = timeoutSec
            });

            // 指定されたシーンのアンロードを行う
            yield return UnloadTestSceneAsync(sceneAbsPath);

            // テストレポートの表示 を行う
            string reportsStr = "";
            reportsStr += "==========================\n";
            reportsStr += "        Test Report       \n";
            reportsStr += "==========================\n";

            // テスト結果を取得する
            foreach (var test in caseCollecter.TestCases)
            {
                var report = test.Result;

                reportsStr += $"{report.Name}: {report.ResultState}\n";
                reportsStr += $"msg: {report.Message}\n";
                /*
                foreach (var pair in report.CustomInfos)
                {
                    reportsStr += $"{pair.Key}: {pair.Value}\n";
                }
                */
                reportsStr += $"duration: {report.Duration}\n";
                reportsStr += $"--------------------------\n";

                /*

                // Custon Info に ゲームオブジェクト情報も入れておく
                if (test is MonoBehaviour mb)
                {
                    // report.CustomInfos.Add("GameObject", mb.gameObject.name);
                }

                // Custom Info に タイムアウト情報も入れておく
                {
                    var globalTimeoutSec = _timeoutSeconds;
                    var localTimeoutSec = Mathf.Max(test.Options.LocalTimeoutSeconds, 0.001f);

                    if (globalTimeoutSec > localTimeoutSec)
                    {
                        // report.CustomInfos.Add("timeoutSec", $"{localTimeoutSec} (local)");
                    }
                    else
                    {
                        // report.CustomInfos.Add("timeoutSec", $"{globalTimeoutSec} (global)");
                    }
                }
                */
            }

            Log.Info(reportsStr);

        }

        private static bool IsLoadedTestScene(string scenePath)
        {
            // シーンが読み込まれているかどうかを確認するため, パスから Scene を取得する
            var scene = SceneManager.GetSceneByPath(scenePath);

            // この scene が有効な場合はすでにロードされている
            return scene.IsValid();
        }

        private static IEnumerator UnloadTestSceneAsync(string scenePath)
        {
            // まだシーンが読み込まれていない場合は処理をスキップする
            if (!IsLoadedTestScene(scenePath))
            {
                Log.Warning($"Already unloaded scene: {scenePath}. skip unload");
                yield break;
            }

            // 加算ロードしたテスト用のシーンを破棄する
            yield return SceneManager.UnloadSceneAsync(scenePath);

            if (!IsLoadedTestScene(scenePath))
            {
                Log.Info($"Unloaded test scene: {scenePath}");
            }
        }

        private static IEnumerator LoadTestSceneAsync(string scenePath)
        {
            // すでにシーンがロード済みの場合は処理をスキップする
            if (IsLoadedTestScene(scenePath))
            {
                Log.Warning($"Already loaded scene: {scenePath}. skip load");
                yield break;
            }

            // テスト用のシーンを加算ロードする
            var loadSceneParameters = new LoadSceneParameters(LoadSceneMode.Additive);

            // シーンロードを行う
            // この際 Active Scene は InitTestScene のまま
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, loadSceneParameters);

            if (IsLoadedTestScene(scenePath))
            {
                Log.Info($"Loaded test scene: {scenePath}");
            }
        }
    }

}
