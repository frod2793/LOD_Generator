#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.HLODSystem;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;


namespace Plugins.Auto_LOD_Generator.EditorScripts
{
    public class LODGroupWindow : EditorWindow
    {
        #region 필드 및 변수
        private Texture2D _icon;
        private bool _objectSelected;
        private bool _isHlodSelected;
        private float _hSliderValue;
        private string _objPath;
        private List<GameObject> _objectsToSimplify;
        private List<GameObject> _objectsToHlod;

        private ReorderableList _reorderableList;

        private const string IconPath = "LOD_Generator/Editor/icon.png";

        private bool _isColider;
        private bool _isHlod;

        public string savePath = "Assets/";
        private float _minuswidth = 7f;
        private Vector2 _scrollPosition = Vector2.zero;
        #endregion

        #region Unity 에디터 이벤트
        private void OnEnable()
        {
            MonoScript script = MonoScript.FromScriptableObject(this);
            string scriptPath = AssetDatabase.GetAssetPath(script);

            if (!string.IsNullOrEmpty(scriptPath))
            {
                string scriptDirectory = Path.GetDirectoryName(scriptPath);
                string iconRelativePath = "icon.png";
                string fullIconPath = Path.Combine(scriptDirectory, iconRelativePath).Replace('\\', '/');
                _icon = AssetDatabase.LoadAssetAtPath<Texture2D>(fullIconPath);

                if (_icon == null)
                {
                    Debug.LogWarning($"LODGroupWindow 아이콘 로드 실패: {fullIconPath}. 아이콘 파일 위치를 확인하세요.");
                }
            }
            else
            {
                Debug.LogError("LODGroupWindow 스크립트 경로를 찾을 수 없습니다.");
            }

            _hSliderValue = 1f;
            _objectSelected = false;
            _objectsToSimplify = new List<GameObject>();
            var window = GetWindow<LODGroupWindow>(true, "LOD Generator", true);
            window.Focus();

            minSize = new Vector2(600, 800f);
            maxSize = new Vector2(1200, 1600f);

            _reorderableList = new ReorderableList(_objectsToSimplify, typeof(GameObject), true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Drag and Drop Objects"),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    _objectsToSimplify[index] = (GameObject)EditorGUI.ObjectField(
                        new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                        _objectsToSimplify[index], typeof(GameObject), true);
                },
                onAddCallback = list => { _objectsToSimplify.Add(null); }
            };
        }

        private void OnGUI()
        {
            _scrollPosition = GUILayout.BeginScrollView(
                _scrollPosition,
                false,
                true,
                GUILayout.Width(position.width),
                GUILayout.Height(position.height)
            );

            GUILayout.BeginVertical();

            SelectPath_Btn_GUI();
            GUILayout.Space(10);

            Toggle_Colider_GUI();
            GUILayout.Space(10);

            // 오브젝트가 선택된 경우에만 LOD 설정 및 생성 버튼 표시
            if (_objectSelected)
            {
                ObjectSelected_GUI();
                GUILayout.Space(10);
            }

            Select_Object_from_File_Btn_GUI();
            GUILayout.Space(10);

            List_Clear_Btn_GUI();
            GUILayout.Space(20);

            Drag_And_Drop_GUI();
            GUILayout.Space(10);

            GetHLOD_GameObjects();
            GUILayout.Space(10);


            GUILayout.EndVertical();

            GUILayout.EndScrollView();
        }

        private void OnFocus()
        {
            // 창이 포커스를 얻을 때마다 최상단으로 가져옴
            Repaint();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
        #endregion

        #region UI 메서드
        /// <summary>
        /// 저장경로 선택 버튼
        /// </summary>
        private void SelectPath_Btn_GUI()
        {
            GUILayout.BeginVertical();
            GUILayout.Box(_icon, GUILayout.Height(140f), GUILayout.Width(140f));
            if (GUILayout.Button("Select Save Path", GUILayout.Height(20f),
                    GUILayout.Width(position.width - _minuswidth)))
            {
                var selectedPath = EditorUtility.OpenFolderPanel("Select Save Path", Application.dataPath, savePath);
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    savePath = selectedPath.Replace(Application.dataPath, "Assets");
                }
                else
                {
                    Debug.LogError("The selected path must be within the Assets directory.");
                }
            }

            savePath = EditorGUILayout.TextField("Save Path:", savePath, GUILayout.Height(20f),
                GUILayout.Width(position.width - _minuswidth));

            GUILayout.EndVertical();
        }

        /// <summary>
        /// HLOD 또는 콜라이더 활성화 토글
        /// </summary>
        private void Toggle_Colider_GUI()
        {
            GUILayout.Space(20);
            _isColider = EditorGUILayout.Toggle("Use Colider", _isColider);
            GUILayout.Space(20);
            _isHlod = EditorGUILayout.Toggle("Use HLOD", _isHlod);
        }

        /// <summary>
        /// 오브젝트가 선택된후 LOD 생성 버튼 과 퀄리티 조절 슬라이더
        /// </summary>
        private void ObjectSelected_GUI()
        {
            GUILayout.Space(20);
            EditorGUILayout.LabelField("Quality Factor: ", GUILayout.Height(20f),
                GUILayout.Width(position.width - _minuswidth));
            var textFieldVal = float.Parse(EditorGUILayout.TextField(
                _hSliderValue.ToString(CultureInfo.InvariantCulture), GUILayout.Height(20f),
                GUILayout.Width(position.width)));

            if (textFieldVal >= 0 && textFieldVal <= 1)
            {
                _hSliderValue = textFieldVal;
            }
            else
            {
                Debug.LogError("Quality factor number must be between 0 and 1");
            }

            _hSliderValue = GUILayout.HorizontalScrollbar(_hSliderValue, 0.01f, 0f, 1f, GUILayout.Height(20f),
                GUILayout.Width(position.width - _minuswidth));
            GUILayout.Space(20);

            if (GUILayout.Button("Generate", GUILayout.Height(20f), GUILayout.Width(position.width - _minuswidth)))
            {
                foreach (var obj in _objectsToSimplify)
                {
                    LODGenerator.Generator(obj, _hSliderValue, savePath, _isColider, _isHlod);
                }
            }
        }

        private void Select_Object_from_File_Btn_GUI()
        {
            GUILayout.Space(20);
            if (GUILayout.Button("Select Object from File", GUILayout.Height(20f),
                    GUILayout.Width(position.width - _minuswidth)))
            {
                _objPath = EditorUtility.OpenFilePanel("Select an FBX object", Application.dataPath, "fbx")
                    .Replace(Application.dataPath, "");

                if (_objPath.Length != 0)
                {
                    _objectSelected = true;
                    var obj = AssetDatabase.LoadAssetAtPath("Assets/" + _objPath, typeof(GameObject)) as GameObject;
                    if (obj != null)
                    {
                        _objectsToSimplify.Add(obj);
                    }
                }
            }
        }

        /// <summary>
        /// 리스트 초기화 버튼
        /// </summary>
        private void List_Clear_Btn_GUI()
        {
            GUILayout.Space(20);
            if (GUILayout.Button("List Clear", GUILayout.Height(20f), GUILayout.Width(position.width - _minuswidth)))
            {
                _objectsToSimplify.Clear();
                _objectSelected = false;
                if (_objectsToHlod != null)
                    _objectsToHlod.Clear();
            }
        }

        /// <summary>
        /// LOD 를 만들 오브젝트를 드래그앤 드랍으로 가져오기
        /// </summary>
        private void Drag_And_Drop_GUI()
        {
            GUILayout.BeginVertical();

            // 리스트 표시
            _reorderableList.DoLayoutList();

            // 드래그 앤 드롭 처리
            var evt = Event.current;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                // 드래그된 오브젝트가 GameObject인지 확인
                bool hasValidObjects = DragAndDrop.objectReferences.Any(obj => obj is GameObject);

                // 유효한 오브젝트가 있을 경우만 드래그 허용
                DragAndDrop.visualMode = hasValidObjects ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

                if (evt.type == EventType.DragPerform && hasValidObjects)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is GameObject gameObject)
                        {
                            // 중복 오브젝트 확인
                            if (!_objectsToSimplify.Contains(gameObject))
                            {
                                _objectsToSimplify.Add(gameObject);
                                _objectSelected = true;
                            }
                        }
                    }
                }

                Event.current.Use();
            }

            GUILayout.EndVertical();
        }
        #endregion

        #region HLOD 관련 메서드
        /// <summary>
        /// HLOD 오브젝트 가져오기 버튼과 리스트 보여주기
        /// </summary>
        private void GetHLOD_GameObjects()
        {
            HLODEditor hlodEditor = CreateInstance<HLODEditor>();

            if (GUILayout.Button("Get HLOD obj", GUILayout.Height(20f), GUILayout.Width(position.width - _minuswidth)))
            {
                _objectsToHlod = hlodEditor.GetHLOD_GameObjects();
            }

            // 가져온 HLOD 오브젝트들을 리스트 보여주기
            if (_objectsToHlod != null)
            {
                foreach (var obj in _objectsToHlod)
                {
                    EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                }

                if (_objectsToHlod.Count > 0)
                {
                    _isHlodSelected = true;
                }
            }

            if (_isHlodSelected)
            {
                if (GUILayout.Button("텍스처 권한 자동 설정", GUILayout.Height(20f), 
                        GUILayout.Width(position.width - _minuswidth)))
                {
                    if (_objectsToHlod != null && _objectsToHlod.Count > 0)
                    {
                        int totalCount = 0;
                        foreach (var obj in _objectsToHlod)
                        {
                            EnableAllTexturesReadWrite(obj);
                        }
                        EditorUtility.DisplayDialog("완료", "모든 텍스처의 Read/Write 권한이 설정되었습니다.", "확인");
                    }
                }
                
                if (GUILayout.Button("GenerateHLod", GUILayout.Height(20f),
                        GUILayout.Width(position.width - _minuswidth)))
                {
                    // _objectsToHlod 리스트가 null이 아니고, 오브젝트가 하나 이상 있는지 확인
                    if (_objectsToHlod != null && _objectsToHlod.Count > 0)
                    {
                        // _objectsToHlod 리스트에 있는 각 오브젝트에 대해 HLOD 생성
                        foreach (var obj in _objectsToHlod)
                        {
                            CoroutineRunner.RunCoroutine(GenerateHLODWithDelay(hlodEditor, obj));
                        }
                    }
                    else
                    {
                        // HLOD 오브젝트가 없을 경우 에러 메시지 출력
                        Debug.LogError("No HLOD objects to generate.");
                    }
                }
            }
        }

        /// <summary>
        /// HLOD 가 생성될시 중복 생성호출을 방지하기 위해 코루틴으로 딜레이를 줌
        /// </summary>
        /// <param name="hlodEditor"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        private IEnumerator GenerateHLODWithDelay(HLODEditor hlodEditor, GameObject obj)
        {
            // HLOD 생성 전 텍스처 권한 자동 설정
            EnableAllTexturesReadWrite(obj);

            // 텍스처 리임포트 완료를 위한 데이터베이스 새로고침
            AssetDatabase.Refresh();

            // 텍스처 리임포트가 완료될 시간 확보 (지연 시간)
            yield return new EditorWaitForSeconds(2.0f);

            // try/catch를 yield return 밖으로 이동
            try
            {
                hlodEditor.GenerateHLOD(obj); // HLOD 오브젝트 생성
            }
            catch (Exception ex)
            {
                Debug.LogError($"HLOD 생성 중 오류 발생: {ex.Message}");
                yield break; // 오류 발생 시 코루틴 종료
            }

            // try/catch 블록 외부로 yield return 이동
            yield return new WaitUntil(() => HLODCreator.isCreating == false);
        }
        #endregion

        #region 텍스처 처리 메서드
     

        // 텍스처 Read/Write 활성화 메서드
        private void EnableReadWrite()
        {
            Object[] selectedTextures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);

            if (selectedTextures.Length == 0)
            {
                EditorUtility.DisplayDialog("알림", "텍스처를 먼저 선택해주세요.", "확인");
                return;
            }

            int count = 0;
            foreach (Texture2D texture in selectedTextures)
            {
                string path = AssetDatabase.GetAssetPath(texture);
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);

                if (!importer.isReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                    count++;
                }
            }

            EditorUtility.DisplayDialog("완료", $"{count}개 텍스처의 Read/Write 활성화 완료", "확인");
        }

       private void EnableAllTexturesReadWrite(GameObject rootObject)
{
    List<Material> allMaterials = new List<Material>();
    HashSet<string> processedTexturePaths = new HashSet<string>();
    int count = 0;

    // 모든 MeshRenderer에서 재질 수집
    MeshRenderer[] meshRenderers = rootObject.GetComponentsInChildren<MeshRenderer>(true);
    foreach (MeshRenderer renderer in meshRenderers)
    {
        if (renderer.sharedMaterials != null)
        {
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat != null && !allMaterials.Contains(mat))
                {
                    allMaterials.Add(mat);
                }
            }
        }
    }

    // 각 재질의 텍스처 가져오기 및 처리
    foreach (Material material in allMaterials)
    {
        Shader shader = material.shader;
        if (shader == null) continue;

        int propertyCount = ShaderUtil.GetPropertyCount(shader);

        for (int i = 0; i < propertyCount; i++)
        {
            try
            {
                if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    string propertyName = ShaderUtil.GetPropertyName(shader, i);
                    Texture texture = material.GetTexture(propertyName);

                    if (texture is Texture2D texture2D)
                    {
                        string assetPath = AssetDatabase.GetAssetPath(texture2D);
                        if (!string.IsNullOrEmpty(assetPath) && !processedTexturePaths.Contains(assetPath))
                        {
                            processedTexturePaths.Add(assetPath);
                            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                            
                            if (importer != null)
                            {
                                // 패키지 내 텍스처인 경우 처리 방법이 다를 수 있음
                                bool isPackageTexture = assetPath.StartsWith("Packages/");
                                
                                if (!importer.isReadable)
                                {
                                    try
                                    {
                                        importer.isReadable = true;
                                        importer.SaveAndReimport();
                                        count++;
                                        Debug.Log($"텍스처 '{assetPath}'의 Read/Write 활성화 완료");
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.LogError($"텍스처 '{assetPath}'의 Read/Write 활성화 실패: {ex.Message}");
                                        
                                        // 패키지 내 텍스처인 경우 사용자에게 안내
                                        if (isPackageTexture)
                                        {
                                            Debug.LogWarning($"패키지 내 텍스처 '{assetPath}'는 프로젝트 Assets 폴더로 복사 후 사용하세요.");
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.Log($"텍스처 '{assetPath}'는 이미 Read/Write 활성화됨");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"텍스처 처리 중 오류 발생: {ex.Message}");
            }
        }
    }

    if (count > 0)
    {
        Debug.Log($"총 {count}개 텍스처의 Read/Write 권한이 자동으로 활성화되었습니다.");
        // 텍스처 임포트 완료를 위한 데이터베이스 새로고침
        AssetDatabase.Refresh();
    }
}
        #endregion
    }

    #region 유틸리티 클래스
    public class EditorWaitForSeconds : CustomYieldInstruction
    {
        private double _targetTime;

        public EditorWaitForSeconds(float seconds)
        {
            _targetTime = EditorApplication.timeSinceStartup + seconds;
        }

        public override bool keepWaiting
        {
            get { return EditorApplication.timeSinceStartup < _targetTime; }
        }
    }
    #endregion
}
#endif