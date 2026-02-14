#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Plugins.Auto_LOD_Generator.Editor.Core;
using Unity.HLODSystem;
using Unity.HLODSystem.Utils;
using Object = UnityEngine.Object;

namespace Plugins.Auto_LOD_Generator.EditorScripts
{
    /// <summary>
    /// [설명]: 프리미엄 UI가 적용된 LOD Generator 에디터 윈도우입니다.
    /// 카드형 레이아웃과 반응형 UI를 통해 사용자 경험을 극대화합니다.
    /// </summary>
    public class LODGroupWindow : EditorWindow
    {
        #region 에디터 설정
        private const string k_IconFileName = "icon.png";
        private const float k_MinWindowWidth = 800f;
        private const float k_MinWindowHeight = 850f;
        #endregion

        #region 내부 변수
        private Texture2D m_icon;
        private Vector2 m_scrollPosition = Vector2.zero;
        private ReorderableList m_reorderableList;
        private LODGeneratorViewModel m_viewModel;
        
        // UI 보조 상태
        private bool m_isHlodSelected;
        private List<GameObject> m_objectsToHlod;
        #endregion

        #region 유니티 생명주기
        private void OnEnable()
        {
            InitializeViewModel();
            LoadIcon();
            SetupReorderableList();

            minSize = new Vector2(k_MinWindowWidth, k_MinWindowHeight);
        }

        private void OnGUI()
        {
            LODGeneratorStyles.InitializeStyles();
            
            if (m_viewModel == null)
            {
                InitializeViewModel();
            }

            DrawHeader();

            m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
            GUILayout.BeginVertical(GUILayout.ExpandHeight(true));

            DrawStatusPanel();
            DrawPathSection();
            DrawSettingsSection();
            DrawTargetListSection();
            DrawHLODSection();

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            DrawFooter();
        }

        private void OnFocus() => Repaint();
        private void OnInspectorUpdate() => Repaint();
        #endregion

        #region 초기화 및 바인딩 로직
        private void InitializeViewModel()
        {
            ILODService lodService = new LODService();
            m_viewModel = new LODGeneratorViewModel(lodService);
        }

        private void LoadIcon()
        {
            MonoScript script = MonoScript.FromScriptableObject(this);
            string scriptPath = AssetDatabase.GetAssetPath(script);

            if (!string.IsNullOrEmpty(scriptPath))
            {
                string scriptDirectory = Path.GetDirectoryName(scriptPath);
                string fullIconPath = Path.Combine(scriptDirectory, k_IconFileName).Replace('\\', '/');
                m_icon = AssetDatabase.LoadAssetAtPath<Texture2D>(fullIconPath);
            }
        }

        private void SetupReorderableList()
        {
            List<GameObject> uiList = m_viewModel.TargetObjects as List<GameObject>;

            m_reorderableList = new ReorderableList(uiList, typeof(GameObject), true, false, true, true)
            {
                drawHeaderCallback = rect => GUI.Label(rect, "대상 오브젝트 리스트 (Target Objects)", EditorStyles.miniBoldLabel),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    rect.y += 2;
                    uiList[index] = (GameObject)EditorGUI.ObjectField(
                        new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                        uiList[index], typeof(GameObject), true);
                },
                onAddCallback = list => { uiList.Add(null); },
                elementHeight = EditorGUIUtility.singleLineHeight + 4
            };
        }
        #endregion

        #region UI 컴포넌트 드로잉
        private void DrawHeader()
        {
            GUILayout.BeginHorizontal(LODGeneratorStyles.CardStyle);
            if (m_icon != null)
            {
                GUILayout.Label(m_icon, GUILayout.Width(64), GUILayout.Height(64));
            }
            GUILayout.BeginVertical();
            GUILayout.Label("LOD Generator (LOD 생성기)", LODGeneratorStyles.HeaderStyle);
            GUILayout.Label("Automated Mesh Simplification & HLOD Integration System (자동 메쉬 단순화 및 HLOD 통합 시스템)", LODGeneratorStyles.SubtitleStyle);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawStatusPanel()
        {
            GUILayout.BeginVertical(LODGeneratorStyles.CardStyle);
            GUILayout.Label("System Status (시스템 상태)", LODGeneratorStyles.TitleStyle);
            
            string statusPrefix = "Status (상태): ";
            GUILayout.Label(statusPrefix + m_viewModel.StatusMessage, EditorStyles.miniLabel);
            
            // 커스텀 프로그레스 바
            Rect progressRect = GUILayoutUtility.GetRect(0, 8, LODGeneratorStyles.ProgressBarStyle);
            GUI.BeginGroup(progressRect, LODGeneratorStyles.ProgressBarStyle);
            GUI.Box(new Rect(0, 0, progressRect.width * m_viewModel.Progress, progressRect.height), "", LODGeneratorStyles.ProgressBarFillStyle);
            GUI.EndGroup();
            
            GUILayout.EndVertical();
        }

        private void DrawPathSection()
        {
            GUILayout.BeginVertical(LODGeneratorStyles.CardStyle);
            GUILayout.Label("Output Settings (출력 설정)", LODGeneratorStyles.TitleStyle);
            
            GUILayout.BeginHorizontal();
            m_viewModel.SavePath = EditorGUILayout.TextField("Save Assets At (애셋 저장 경로):", m_viewModel.SavePath);
            if (GUILayout.Button("Browse (찾아보기)", GUILayout.Width(110)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select Save Path (LOD 저장 경로 선택)", Application.dataPath, m_viewModel.SavePath);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        m_viewModel.SavePath = selectedPath.Replace(Application.dataPath, "Assets");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Warning", "Path must be inside Assets folder.", "OK");
                    }
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawSettingsSection()
        {
            GUILayout.BeginVertical(LODGeneratorStyles.CardStyle);
            GUILayout.Label("Generation Parameters (생성 파라미터)", LODGeneratorStyles.TitleStyle);
            
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            m_viewModel.QualityFactor = EditorGUILayout.Slider("LOD Quality (LOD 품질) (0-1):", m_viewModel.QualityFactor, 0f, 1f);
            GUILayout.EndVertical();
            
            GUILayout.BeginVertical(GUILayout.Width(280));
            m_viewModel.UseCollider = EditorGUILayout.ToggleLeft("Create Mesh Colliders (메쉬 콜라이더 생성)", m_viewModel.UseCollider);
            m_viewModel.UseHLOD = EditorGUILayout.ToggleLeft("Integrate HLOD System (HLOD 시스템 통합)", m_viewModel.UseHLOD);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            if (m_viewModel.TargetObjects.Count > 0)
            {
                if (GUILayout.Button("GENERATE LODS (LOD 생성 시작)", LODGeneratorStyles.PremiumButtonStyle))
                {
                    m_viewModel.GenerateLODs();
                    AssetDatabase.Refresh();
                }
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Button("ADD OBJECTS TO START (시작하려면 오브젝트를 추가하세요)", LODGeneratorStyles.PremiumButtonStyle);
                GUI.enabled = true;
            }
            GUILayout.EndVertical();
        }

        private void DrawTargetListSection()
        {
            GUILayout.BeginVertical(LODGeneratorStyles.CardStyle);
            GUILayout.Label("Source Models (소스 모델)", LODGeneratorStyles.TitleStyle);
            
            m_reorderableList.DoLayoutList();

            DrawDragAndDropArea();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add from FBX File (FBX 파일에서 추가)", EditorStyles.miniButtonLeft))
            {
                string path = EditorUtility.OpenFilePanel("Select FBX (FBX 오브젝트 선택)", Application.dataPath, "fbx");
                if (!string.IsNullOrEmpty(path))
                {
                    string relativePath = path.Replace(Application.dataPath, "Assets");
                    GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);
                    if (obj != null) m_viewModel.AddTarget(obj);
                }
            }
            if (GUILayout.Button("Clear List (리스트 초기화)", EditorStyles.miniButtonRight))
            {
                m_viewModel.ClearTargets();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawDragAndDropArea()
        {
            Event evt = Event.current;
            
            // 드래그 중 여부 확인
            bool isDragging = DragAndDrop.objectReferences.Length > 0;
            
            // 사용자의 요청에 따라 "DROP HERE!"를 전면에 배치
            string label = isDragging ? "DROP NOW! (지금 놓으세요!)" : "DROP HERE! (여기에 놓으세요!)";
            GUILayout.Box(label, LODGeneratorStyles.DragAndDropAreaStyle, GUILayout.Height(60), GUILayout.ExpandWidth(true));
            
            Rect dropArea = GUILayoutUtility.GetLastRect();
            bool isDraggingOver = dropArea.Contains(evt.mousePosition);

            if (isDraggingOver)
            {
                switch (evt.type)
                {
                    case EventType.DragUpdated:
                        bool hasValidObjects = DragAndDrop.objectReferences.Any(obj => obj is GameObject);
                        DragAndDrop.visualMode = hasValidObjects ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                        evt.Use();
                        break;

                    case EventType.DragPerform:
                        DragAndDrop.AcceptDrag();
                        foreach (var draggedObject in DragAndDrop.objectReferences)
                        {
                            if (draggedObject is GameObject gameObject)
                            {
                                m_viewModel.AddTarget(gameObject);
                            }
                        }
                        evt.Use();
                        break;
                }
            }

            // 마우스 이동이나 드래그 상태 변화에 따른 즉각적인 UI 갱신 유도
            if (evt.type == EventType.MouseMove || evt.type == EventType.DragUpdated)
            {
                Repaint();
            }
        }

        private void DrawHLODSection()
        {
            GUILayout.BeginVertical(LODGeneratorStyles.CardStyle);
            GUILayout.Label("HLOD Utility (HLOD 유틸리티)", LODGeneratorStyles.TitleStyle);

            if (GUILayout.Button("Fetch Active HLOD GameObjects (활성 HLOD 오브젝트 가져오기)", EditorStyles.miniButton))
            {
                HLODEditor hlodEditor = CreateInstance<HLODEditor>();
                m_objectsToHlod = hlodEditor.GetHLOD_GameObjects();
                m_isHlodSelected = m_objectsToHlod != null && m_objectsToHlod.Count > 0;
                DestroyImmediate(hlodEditor);
            }

            if (m_isHlodSelected && m_objectsToHlod != null)
            {
                GUILayout.Space(5);
                foreach (var obj in m_objectsToHlod)
                {
                    if (obj != null) EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Enable Read/Write (Textures) (읽기/쓰기 권한 활성화 (텍스처))", EditorStyles.miniButtonLeft))
                {
                    EnableAllTexturesReadWriteForList(m_objectsToHlod);
                }
                if (GUILayout.Button("GENERATE HLOD (HLOD 생성)", EditorStyles.miniButtonRight))
                {
                    ExecuteHLODGeneration();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void DrawFooter()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close Window (창 닫기)", EditorStyles.toolbarButton))
            {
                Close();
            }
            GUILayout.EndHorizontal();
        }
        #endregion

        #region 비즈니스 로직 (View 전용)
        private void ExecuteHLODGeneration()
        {
            if (m_objectsToHlod == null || m_objectsToHlod.Count == 0) return;

            HLODEditor hlodEditor = CreateInstance<HLODEditor>();
            foreach (var obj in m_objectsToHlod)
            {
                if (obj == null) continue;
                CoroutineRunner.RunCoroutine(GenerateHLODWithDelay(hlodEditor, obj));
            }
        }

        private IEnumerator GenerateHLODWithDelay(HLODEditor hlodEditor, GameObject obj)
        {
            EnableAllTexturesReadWrite(obj);
            AssetDatabase.Refresh();

            yield return new EditorWaitForSeconds(2.0f);

            try
            {
                hlodEditor.GenerateHLOD(obj);
            }
            catch (Exception ex)
            {
                Debug.LogError($"HLOD 생성 실패: {ex.Message}");
                yield break;
            }

            yield return new WaitUntil(() => HLODCreator.isCreating == false);
        }

        private void EnableAllTexturesReadWriteForList(List<GameObject> targets)
        {
            foreach (var obj in targets)
            {
                if (obj != null) EnableAllTexturesReadWrite(obj);
            }
            EditorUtility.DisplayDialog("완료", "모든 텍스처의 Read/Write 권한 설정이 완료되었습니다.", "확인");
        }

        private void EnableAllTexturesReadWrite(GameObject rootObject)
        {
            if (rootObject == null) return;

            HashSet<string> processedPaths = new HashSet<string>();
            MeshRenderer[] renderers = rootObject.GetComponentsInChildren<MeshRenderer>(true);

            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials == null) continue;

                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null || mat.shader == null) continue;

                    int propCount = ShaderUtil.GetPropertyCount(mat.shader);
                    for (int i = 0; i < propCount; i++)
                    {
                        if (ShaderUtil.GetPropertyType(mat.shader, i) != ShaderUtil.ShaderPropertyType.TexEnv) continue;

                        string propName = ShaderUtil.GetPropertyName(mat.shader, i);
                        Texture tex = mat.GetTexture(propName);

                        if (tex is Texture2D tex2D)
                        {
                            string path = AssetDatabase.GetAssetPath(tex2D);
                            if (string.IsNullOrEmpty(path) || processedPaths.Contains(path)) continue;

                            processedPaths.Add(path);
                            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                            if (importer != null && !importer.isReadable)
                            {
                                importer.isReadable = true;
                                importer.SaveAndReimport();
                            }
                        }
                    }
                }
            }
            AssetDatabase.Refresh();
        }
        #endregion
    }

    #region 유틸리티
    public class EditorWaitForSeconds : CustomYieldInstruction
    {
        private readonly double m_targetTime;
        public override bool keepWaiting => EditorApplication.timeSinceStartup < m_targetTime;

        public EditorWaitForSeconds(float seconds)
        {
            m_targetTime = EditorApplication.timeSinceStartup + seconds;
        }
    }
    #endregion
}
#endif
