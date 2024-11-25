using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Plugins.Auto_LOD_Generator.Editor
{
    public class LODGenerator : MonoBehaviour
    {
        //lod 그룹에 포함되지않는  오브젝트  1개 생성
        public static void Generator([JetBrains.Annotations.NotNull] GameObject originalObject, float qualityFactor, string path,bool isColider)
        {
            const int count = 4;

            if (originalObject == null) throw new ArgumentNullException(nameof(originalObject));

            var newParent = new GameObject(originalObject.name + " LOD Group");
            newParent.AddComponent<LODGroup>();

            var lods = new LOD[count]; // Adjusted the size of the array

            var qualityFactors = new List<float>()
            {
                1f,
                0.6f,
                0.4f
            };

            for (var i = 0; i < count; i++)
            {
                var newGameObj = Instantiate(originalObject, newParent.transform);
                newGameObj.name = originalObject.name + "_LOD" + i;

                ProcessGameObject(newGameObj, i == 0 ? 1 : qualityFactor * qualityFactors[i - 1],
                    originalObject.name + "_LOD" + i, path);

                var renderers = newGameObj.GetComponentsInChildren<Renderer>();

                lods[i] = new LOD(0.5F / (i + 1), renderers); // Adjusted the index

                if (i == 0&&isColider)
                {
                    var newGameObj_col = Instantiate(originalObject, newParent.transform);
                    newGameObj_col.name = originalObject.name + "_Colider";
                    ProcessGameObject_col(newGameObj_col);
                }
            }

            newParent.GetComponent<LODGroup>().SetLODs(lods);
            newParent.GetComponent<LODGroup>().RecalculateBounds();
            newParent.transform.SetParent(originalObject.transform.parent);
        }

        private static void ProcessGameObject(GameObject gameObject, float qualityFactor, string name, string path)
        {
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                // Use the saved mesh
                var originalMesh = meshFilter.sharedMesh;
                var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();
                meshSimplifier.Initialize(originalMesh);
                meshSimplifier.SimplifyMesh(qualityFactor);

                if (!IsNullOrEmpty(gameObject.GetComponent<MeshCollider>()))
                {
                    DestroyImmediate(gameObject.GetComponent<MeshCollider>());
                }
                
                if (!IsNullOrEmpty(gameObject.GetComponent<MeshCollider>()))
                {
                    DestroyImmediate(gameObject.GetComponent<MeshCollider>());
                }

                
                var destMesh = meshSimplifier.ToMesh();
                meshFilter.sharedMesh = destMesh;

                var assetPath = path + "/" + name + "/" + gameObject.name + "_SimplifiedMesh.asset";
                SaveMeshToAsset(destMesh, assetPath, name);

                // Load the saved mesh asset and apply it
                var loadedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                if (loadedMesh != null)
                {
                    meshFilter.sharedMesh = loadedMesh;
                }
                else
                {
                    Debug.LogError($"Failed to load the saved mesh asset at path: {assetPath}");
                }
            }

            foreach (Transform child in gameObject.transform)
            {
                ProcessGameObject(child.gameObject, qualityFactor, name, path);
            }
        }

        private static void SaveMeshToAsset(Mesh mesh, string assetPath, string name)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                Debug.LogError("Asset path is invalid.");
                return;
            }

            // Ensure the directory exists
            var directory = System.IO.Path.GetDirectoryName(assetPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                Debug.LogError($"Directory path is invalid. Asset path: {assetPath}");
                return;
            }

            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Create a temporary GameObject to hold the mesh
            var tempGameObject = new GameObject(mesh.name);
            var meshFilter = tempGameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = tempGameObject.AddComponent<MeshRenderer>();

            // Clean up the temporary GameObject
            GameObject.DestroyImmediate(tempGameObject);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Save the mesh asset
            AssetDatabase.CreateAsset(mesh, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ProcessGameObject_col(GameObject gameObject)
        {
            // 1. MeshCollider 처리
            if (!gameObject.TryGetComponent(out MeshCollider meshCollider))
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }

            // 2. MeshRenderer와 MeshFilter 처리
            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                DestroyImmediate(meshRenderer);
            }

            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                DestroyImmediate(meshFilter);
            }

            // 3. 자식 GameObject 처리 (루프 기반으로 변경하여 스택 오버플로우 방지)
            foreach (Transform child in gameObject.transform)
            {
                ProcessGameObject_col(child.gameObject);
            }
        }

        
        private static bool IsNullOrEmpty(Object value)
        {
            return ReferenceEquals(value,null);
        }
        
    }
}