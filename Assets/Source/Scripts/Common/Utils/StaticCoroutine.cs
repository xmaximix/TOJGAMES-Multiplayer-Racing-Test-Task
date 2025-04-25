using System.Collections;
using UnityEngine;

namespace TojGamesTask.Common.Utils
{
    public class StaticCoroutine
    {
        private static StaticCoroutineBehaviour runner;
        private static void InitializeRunner()
        {
            if (runner != null) return;

            var go = new GameObject("_StaticCoroutineRunner");
            go.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(go);

            runner = go.AddComponent<StaticCoroutineBehaviour>();
        }

        public static Coroutine StartCoroutine(IEnumerator routine)
        {
            InitializeRunner();
            return runner.StartCoroutine(routine);
        }

        public static void StopCoroutine(Coroutine coroutine)
        {
            if (runner == null || coroutine == null) return;
            runner.StopCoroutine(coroutine);
        }

        public static void StopAllCoroutines()
        {
            if (runner == null) return;
            runner.StopAllCoroutines();
        }

        private class StaticCoroutineBehaviour : MonoBehaviour
        {
        }
    }
}