using System.Collections;
using System.Collections.Generic;

using UnityEditor;

namespace PixelShenanigans.EditorUtilities
{
    [InitializeOnLoad]
    public class CoroutineEditorUtility
    {
        static int currentExecute = 0;
        private static List<IEnumerator> Coroutines = new List<IEnumerator>();

        static CoroutineEditorUtility()
        {
            EditorApplication.update += ExecuteCoroutine;
        }

        public static IEnumerator StartCoroutine(IEnumerator coroutine)
        {
            Coroutines.Add(coroutine);
            return coroutine;
        }

        static void ExecuteCoroutine()
        {
            if (Coroutines.Count <= 0)
            {
                return;
            }

            currentExecute = (currentExecute + 1) % Coroutines.Count;

            bool finish = !Coroutines[currentExecute].MoveNext();

            if (finish)
            {
                Coroutines.RemoveAt(currentExecute);
            }
        }
    }
}