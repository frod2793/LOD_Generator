using System;
using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem.Simplifier
{
    public abstract class SimplifierBase : ISimplifier
    {
        private SerializableDynamicObject m_options;
        public SimplifierBase(SerializableDynamicObject simplifierOptions)
        {
            m_options = simplifierOptions;
        }

        public IEnumerator Simplify(HLODBuildInfo buildInfo)
        {
            if (m_options == null)
                yield break;

            for (int i = 0; i < buildInfo.WorkingObjects.Count; ++i)
            {
                Utils.WorkingMesh mesh = buildInfo.WorkingObjects[i].Mesh;

                int triangleCount = mesh.triangles.Length / 3;
                float simplifyMaxPolygonCount = Convert.ToSingle(m_options["SimplifyMaxPolygonCount"]);
                float simplifyPolygonRatio = Convert.ToSingle(m_options["SimplifyPolygonRatio"]);
                float simplifyMinPolygonCount = Convert.ToSingle(m_options["SimplifyMinPolygonCount"]);

                float maxQuality = Mathf.Min(simplifyMaxPolygonCount / (float)triangleCount, simplifyPolygonRatio);
                float minQuality = Mathf.Max(simplifyMinPolygonCount / (float)triangleCount, 0.0f);

                var ratio = maxQuality * Mathf.Pow(simplifyPolygonRatio, buildInfo.Distances[i]);
                ratio = Mathf.Max(ratio, minQuality);

                yield return GetSimplifiedMesh(mesh, ratio, (m) =>
                {
                    buildInfo.WorkingObjects[i].SetMesh(m);
                });
            }            
        }

        public void SimplifyImmidiate(HLODBuildInfo buildInfo)
        {
            IEnumerator routine = Simplify(buildInfo);
            CustomCoroutine coroutine = new CustomCoroutine(routine);
            while (coroutine.MoveNext())
            {
            }
        }

        protected abstract IEnumerator GetSimplifiedMesh(Utils.WorkingMesh origin, float quality, Action<Utils.WorkingMesh> resultCallback);

        protected static void OnGUIBase(SerializableDynamicObject simplifierOptions)
        {
            if (simplifierOptions == null) return;
            EditorGUI.indentLevel += 1;

            if (simplifierOptions["SimplifyPolygonRatio"] == null)
                simplifierOptions["SimplifyPolygonRatio"] = 0.8f;
            if (simplifierOptions["SimplifyMinPolygonCount"] == null)
                simplifierOptions["SimplifyMinPolygonCount"] = 10;
            if (simplifierOptions["SimplifyMaxPolygonCount"] == null)
                simplifierOptions["SimplifyMaxPolygonCount"] = 500;

            simplifierOptions["SimplifyPolygonRatio"] = EditorGUILayout.Slider("Polygon Ratio", Convert.ToSingle(simplifierOptions["SimplifyPolygonRatio"]), 0.0f, 1.0f);
            EditorGUILayout.LabelField("Triangle Range");
            EditorGUI.indentLevel += 1;
            simplifierOptions["SimplifyMinPolygonCount"] = EditorGUILayout.IntSlider("Min", Convert.ToInt32(simplifierOptions["SimplifyMinPolygonCount"]), 10, 100);
            simplifierOptions["SimplifyMaxPolygonCount"] = EditorGUILayout.IntSlider("Max", Convert.ToInt32(simplifierOptions["SimplifyMaxPolygonCount"]), 10, 5000);
            EditorGUI.indentLevel -= 1;

            EditorGUI.indentLevel -= 1;
        }
    }
}
