using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using Unity.HLODSystem;
using Object = UnityEngine.Object;

namespace Plugins.Auto_LOD_Generator.Editor.Core
{
    /// <summary>
    /// [설명]: UnityMeshSimplifier를 활용한 LOD 생성 서비스 구현체입니다.
    /// </summary>
    public class LODService : ILODService
    {
        #region 상수 및 설정
        private const int k_LodCount = 4;
        private readonly float[] m_qualityFactors = { 1.0f, 0.6f, 0.4f };
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 원본 오브젝트를 바탕으로 LOD 그룹을 생성하고 메쉬를 단순화합니다.
        /// </summary>
        public void GenerateLOD(GameObject originalObject, float qualityFactor, string savePath, bool useCollider, bool useHLOD)
        {
            if (originalObject == null)
            {
                throw new ArgumentNullException(nameof(originalObject));
            }

            // 새로운 부모 생성 및 LODGroup 추가
            GameObject newParent = new GameObject($"{originalObject.name} LOD Group");
            
            // 트랜스폼 일치 (위치, 회전, 스케일 상속)
            newParent.transform.position = originalObject.transform.position;
            newParent.transform.rotation = originalObject.transform.rotation;
            newParent.transform.localScale = originalObject.transform.localScale;

            LODGroup lodGroup = newParent.AddComponent<LODGroup>();
            LOD[] lods = new LOD[k_LodCount];

            if (useHLOD)
            {
                HandleHLODGeneration(originalObject, qualityFactor, savePath, useCollider, newParent, lodGroup, lods);
            }
            else
            {
                HandleStandardLODGeneration(originalObject, qualityFactor, savePath, useCollider, newParent, lodGroup, lods);
            }
        }
        #endregion

        #region 내부 비즈니스 로직
        /// <summary>
        /// [설명]: 표준 LOD 그룹 생성 로직을 처리합니다.
        /// </summary>
        private void HandleStandardLODGeneration(GameObject originalObject, float qualityFactor, string savePath, bool useCollider, GameObject newParent, LODGroup lodGroup, LOD[] lods)
        {
            // 오브젝트별 전용 LOD 폴더 경로 설정
            string lodPath = $"{savePath}/{originalObject.name}/LOD";

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < k_LodCount; i++)
                {
                    GameObject newGameObj = Object.Instantiate(originalObject, newParent.transform);
                    newGameObj.name = $"{originalObject.name}_LOD{i}";

                    float currentQuality = (i == 0) ? 1.0f : qualityFactor * m_qualityFactors[i - 1];
                    ProcessRecursiveMeshSimplification(newGameObj, currentQuality, $"LOD{i}", lodPath);

                    Renderer[] renderers = newGameObj.GetComponentsInChildren<Renderer>();
                    lods[i] = new LOD(0.5f / (i + 1), renderers);

                    if (i == 0 && useCollider)
                    {
                        GenerateCollisionObject(originalObject, newParent.transform);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();
            newParent.transform.SetParent(originalObject.transform.parent);
        }

        /// <summary>
        /// [설명]: HLOD 시스템이 통합된 LOD 생성을 처리합니다.
        /// </summary>
        private void HandleHLODGeneration(GameObject originalObject, float qualityFactor, string savePath, bool useCollider, GameObject newParent, LODGroup lodGroup, LOD[] lods)
        {
            // 1. HLOD 오브젝트를 LOD Group의 자실으로 생성
            GameObject hlObject = new GameObject($"{originalObject.name} HLOD");
            hlObject.transform.SetParent(newParent.transform, false);
            
            HLOD hlod = hlObject.AddComponent<HLOD>();

            // 2. HLOD 베이크를 위한 소스 오브젝트를 HLOD 하위에 생성 (기본 레이어 설정 등 포함)
            GameObject sourceForHlod = Object.Instantiate(originalObject, hlObject.transform);
            sourceForHlod.name = $"{originalObject.name}_Source";

            // 필수 타입 자동 설정 (기본값)
            var spaceSplitterTypes = Unity.HLODSystem.SpaceManager.SpaceSplitterTypes.GetTypes();
            var simplifierTypes = Unity.HLODSystem.Simplifier.SimplifierTypes.GetTypes();
            var batcherTypes = Unity.HLODSystem.BatcherTypes.GetTypes();
            var streamingTypes = Unity.HLODSystem.Streaming.StreamingBuilderTypes.GetTypes();

            if (spaceSplitterTypes.Any()) hlod.SpaceSplitterType = spaceSplitterTypes.First();
            if (simplifierTypes.Any()) hlod.SimplifierType = simplifierTypes.First();
            if (batcherTypes.Any()) hlod.BatcherType = (batcherTypes.Count() > 1) ? batcherTypes.Skip(1).First() : batcherTypes.First();
            if (streamingTypes.Any()) hlod.StreamingType = streamingTypes.First();

            // HLOD 출력 경로 지정 ([savePath]/[ObjectName]/HLOD)
            string hlodPath = $"{savePath}/{originalObject.name}/HLOD/";
            var options = hlod.StreamingOptions;

            if (options != null)
            {
                options["OutputDirectory"] = hlodPath;
            }

            // 3. 표준 LOD 생성 수행 (지정된 폴더 구조 사용)
            HandleStandardLODGeneration(originalObject, qualityFactor, savePath, useCollider, newParent, lodGroup, lods);

            // 4. HLOD 베이크 코루틴 비동기 실행 (생성된 hlod 컴포넌트 기준)
            Unity.HLODSystem.Utils.CoroutineRunner.RunCoroutine(HLODCreator.Create(hlod));
        }

        /// <summary>
        /// [설명]: 계층 구조 내의 모든 메쉬를 재귀적으로 단순화합니다.
        /// </summary>
        private void ProcessRecursiveMeshSimplification(GameObject root, float qualityFactor, string lodName, string path)
        {
            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;

                // LOD0(원본)인 경우 애셋화만 진행하고 단순화는 건너뜀
                if (qualityFactor >= 1.0f)
                {
                    // 원본 메쉬를 새로운 애셋으로 복제하여 저장 (LOD 간 독립성 확보)
                    mf.sharedMesh = SaveCopiedMesh(mf.sharedMesh, root.name, mf.gameObject.name, lodName, path);
                    continue;
                }

                var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();
                meshSimplifier.Initialize(mf.sharedMesh);
                meshSimplifier.SimplifyMesh(qualityFactor);

                Mesh destMesh = meshSimplifier.ToMesh();
                string assetPath = $"{path}/{root.name}/{lodName}/{mf.gameObject.name}_Mesh.asset";
                
                SaveMeshAsset(destMesh, assetPath);

                Mesh loadedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                if (loadedMesh != null)
                {
                    mf.sharedMesh = loadedMesh;
                }
            }

            // 기존 메쉬 콜라이더 제거 (LOD0 제외)
            if (qualityFactor < 1.0f)
            {
                RemoveExistingColliders(root);
            }
        }

        private Mesh SaveCopiedMesh(Mesh original, string rootName, string objName, string lodName, string path)
        {
            Mesh copy = Object.Instantiate(original);
            string assetPath = $"{path}/{rootName}/{lodName}/{objName}_Original.asset";
            SaveMeshAsset(copy, assetPath);
            return AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        }

        private void SaveMeshAsset(Mesh mesh, string assetPath)
        {
            string directory = System.IO.Path.GetDirectoryName(assetPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // 이미 존재하는 애셋이면 덮어쓰기 위해 삭제 후 생성
            if (AssetDatabase.LoadAssetAtPath<Mesh>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            AssetDatabase.CreateAsset(mesh, assetPath);
        }

        private void RemoveExistingColliders(GameObject target)
        {
            MeshCollider collider = target.GetComponent<MeshCollider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private void GenerateCollisionObject(GameObject originalObject, Transform parent)
        {
            GameObject colObj = Object.Instantiate(originalObject, parent);
            colObj.name = $"{originalObject.name}_Collider";
            
            // 콜라이더 생성 로직 (재귀)
            ProcessColliderGeneration(colObj);
        }

        private void ProcessColliderGeneration(GameObject target)
        {
            if (!target.TryGetComponent(out MeshCollider collider))
            {
                target.AddComponent<MeshCollider>();
            }

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null) Object.DestroyImmediate(renderer);

            MeshFilter filter = target.GetComponent<MeshFilter>();
            if (filter != null) Object.DestroyImmediate(filter);

            foreach (Transform child in target.transform)
            {
                ProcessColliderGeneration(child.gameObject);
            }
        }

        #endregion
    }
}
