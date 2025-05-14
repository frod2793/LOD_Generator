#if UNITY_EDITOR
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


namespace Plugins.Auto_LOD_Generator.EditorScripts
{
    public class LODGroupWindow : EditorWindow
    {
        private Texture2D _icon;
        private bool _objectSelected;
        private bool _isHLODSelected;
        private float _hSliderValue;
        private string _objPath;
        private List<GameObject> _objectsToSimplify;
        private List<GameObject> _objectsToHLOD;

        private UnityEditorInternal.ReorderableList _reorderableList;

        private const string _iconPath = "LOD_Generator/Editor/icon.png";

        private bool _isColider;
        private bool _isHLOD;

        public string SavePath = "Assets/";
        private float minuswidth = 7f;


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
            // --- 아이콘 로드 로직 끝 ---

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

        private Vector2 _scrollPosition = Vector2.zero; // 스크롤 위치를 저장할 변수 추가

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


            Texture_ReadWrite_GUI();
            GUILayout.Space(10);
            
            GUILayout.EndVertical();

            GUILayout.EndScrollView();
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

            GUILayout.EndVertical(); // 누락된 EndVertical 추가
        }

        /// <summary>
        /// HLOD 오브젝트 가져오기 버튼과 리스트 보여주기
        /// </summary>
        private void GetHLOD_GameObjects()
        {
            HLODEditor hlodEditor = CreateInstance<HLODEditor>();

            if (GUILayout.Button("Get HLOD obj", GUILayout.Height(20f), GUILayout.Width(position.width - minuswidth)))
            {
                _objectsToHLOD = hlodEditor.GetHLOD_GameObjects();
            }

            // 가져온 HLOD 오브젝트들을 리스트 보여주기
            if (_objectsToHLOD != null)
            {
                foreach (var obj in _objectsToHLOD)
                {
                    EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                }

                if (_objectsToHLOD.Count > 0)
                {
                    _isHLODSelected = true;
                }
            }

            if (_isHLODSelected)
            {
                if (GUILayout.Button("GenerateHLod", GUILayout.Height(20f),
                        GUILayout.Width(position.width - minuswidth)))
                {
                    // _objectsToHLOD 리스트가 null이 아니고, 오브젝트가 하나 이상 있는지 확인
                    if (_objectsToHLOD != null && _objectsToHLOD.Count > 0)
                    {
                        // _objectsToHLOD 리스트에 있는 각 오브젝트에 대해 HLOD 생성
                        foreach (var obj in _objectsToHLOD)
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
            hlodEditor.GenerateHLOD(obj); // HLOD 오브젝트 생성
            yield return new WaitUntil(() => HLODCreator.isCreating == false); // HLOD 생성이 완료될 때까지 대기
        }

        /// <summary>
        /// 리스트 초기화 버튼
        /// </summary>
        private void List_Clear_Btn_GUI()
        {
            GUILayout.Space(20);
            if (GUILayout.Button("List Clear", GUILayout.Height(20f), GUILayout.Width(position.width - minuswidth)))
            {
                _objectsToSimplify.Clear();
                _objectSelected = false;
                if (_objectsToHLOD != null)
                    _objectsToHLOD.Clear();
            }
        }

        private void Select_Object_from_File_Btn_GUI()
        {
            GUILayout.Space(20);
            if (GUILayout.Button("Select Object from File", GUILayout.Height(20f),
                    GUILayout.Width(position.width - minuswidth)))
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
        ///  HLOD 또는 콜라이더 활성화 토글
        /// </summary>
        private void Toggle_Colider_GUI()
        {
            GUILayout.Space(20);
            _isColider = EditorGUILayout.Toggle("Use Colider", _isColider);
            GUILayout.Space(20);
            _isHLOD = EditorGUILayout.Toggle("Use HLOD", _isHLOD);
        }

        /// <summary>
        /// 저장경로 선택 버튼
        /// </summary>
        private void SelectPath_Btn_GUI()
        {
            GUILayout.BeginVertical();
            GUILayout.Box(_icon, GUILayout.Height(140f), GUILayout.Width(140f));
            if (GUILayout.Button("Select Save Path", GUILayout.Height(20f),
                    GUILayout.Width(position.width - minuswidth)))
            {
                var selectedPath = EditorUtility.OpenFolderPanel("Select Save Path", Application.dataPath, SavePath);
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    SavePath = selectedPath.Replace(Application.dataPath, "Assets");
                }
                else
                {
                    Debug.LogError("The selected path must be within the Assets directory.");
                }
            }

            SavePath = EditorGUILayout.TextField("Save Path:", SavePath, GUILayout.Height(20f),
                GUILayout.Width(position.width - minuswidth));
    
            GUILayout.EndVertical(); // 누락된 EndVertical 추가
        }

        /// <summary>
        /// 오브젝트가 선택된후 LOD 생성 버튼 과 퀄리티 조절 슬라이더
        /// </summary>
        private void ObjectSelected_GUI()
        {
            GUILayout.Space(20);
            EditorGUILayout.LabelField("Quality Factor: ", GUILayout.Height(20f),
                GUILayout.Width(position.width - minuswidth));
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
                GUILayout.Width(position.width - minuswidth));
            GUILayout.Space(20);

            if (GUILayout.Button("Generate", GUILayout.Height(20f), GUILayout.Width(position.width - minuswidth)))
            {
                foreach (var obj in _objectsToSimplify)
                {
                    LODGenerator.Generator(obj, _hSliderValue, SavePath, _isColider, _isHLOD);
                }
            }
        }
        // LODGroupWindow 클래스 내부에 다음 메서드를 추가하세요
        private void Texture_ReadWrite_GUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("텍스처 Read/Write 활성화", EditorStyles.boldLabel);
    
            if (GUILayout.Button("선택된 텍스처 Read/Write 활성화", GUILayout.Height(20f), GUILayout.Width(position.width - minuswidth)))
            {
                EnableReadWrite();
            }
        }

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
    }
    
}
#endif