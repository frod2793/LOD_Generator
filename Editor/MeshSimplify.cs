using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Plugins.Auto_LOD_Generator.EditorScripts
{
    /// <summary>
    /// [설명]: 개별 메쉬 단순화를 위한 유틸리티 클래스입니다.
    /// </summary>
    public static class MeshSimplify
    {
        /// <summary>
        /// [설명]: 제공된 오브젝트의 메쉬를 단순화하여 새로운 오브젝트를 생성합니다.
        /// </summary>
        /// <param name="originalObject">단순화할 원본 오브젝트</param>
        /// <param name="qualityFactor">단순화 품질 (0.0 ~ 1.0)</param>
        /// <param name="nameSuffix">생성될 오브젝트 이름의 접미사</param>
        public static void Simplify([NotNull] GameObject originalObject, float qualityFactor, string nameSuffix)
        {
            if (originalObject == null) throw new ArgumentNullException(nameof(originalObject));

            MeshFilter filter = originalObject.GetComponent<MeshFilter>();
            MeshRenderer renderer = originalObject.GetComponent<MeshRenderer>();

            if (filter == null || renderer == null)
            {
                Debug.LogWarning($"[MeshSimplify] {originalObject.name}에 MeshFilter 또는 MeshRenderer가 없습니다.");
                return;
            }

            Mesh originalMesh = filter.sharedMesh;
            Material originalMaterial = renderer.sharedMaterial;
            Transform originalTransform = originalObject.transform;
            
            var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();
            meshSimplifier.Initialize(originalMesh);
            meshSimplifier.SimplifyMesh(qualityFactor);
            
            Mesh destMesh = meshSimplifier.ToMesh();
            
            GameObject newGameObj = new GameObject($"{originalObject.name}{nameSuffix}");
            newGameObj.AddComponent<MeshFilter>().sharedMesh = destMesh;
            newGameObj.AddComponent<MeshRenderer>().sharedMaterial = originalMaterial;
            
            newGameObj.transform.SetPositionAndRotation(originalTransform.position, originalTransform.rotation);
            newGameObj.transform.localScale = originalTransform.localScale;
        }
    }
}
