using System.Collections.Generic;
using System.Globalization;
using Plugins.Auto_LOD_Generator.Editor;
using Unity.HLODSystem;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Plugins.Auto_LOD_Generator.Editor
{
    public class LODGroupWindow : EditorWindow
    {
        private Texture _icon;
        private bool _objectSelected;
        private float _hSliderValue;
        private string _objPath;
        private List<GameObject> _objectsToSimplify;
        private ReorderableList _reorderableList;
        private const string _iconPath = "Assets/LOD_Generator/Editor/icon.png";

        private bool _isColider;
        private bool _isHLOD;
        
        public string SavePath = "Assets/";
        private float minuswidth = 7f;
        
        
        private void OnEnable()
        {
            _hSliderValue = 1f;
            _icon = (Texture)AssetDatabase.LoadAssetAtPath(_iconPath, typeof(Texture));
            _objectSelected = false;
            _objectsToSimplify = new List<GameObject>();
            GetWindow(typeof(LODGroupWindow));
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
            GUILayout.BeginArea(new Rect(0f, 0f, position.width, position.height));
            GUILayout.BeginHorizontal();

            SelectPath_Btn_GUI();


            Toggle_Colider_GUI();

            if (_objectSelected)
            {
                ObjectSelected_GUI();
            }

            Select_Object_from_File_Btn_GUI();

            Select_Object_from_Scene_GUI();

            List_Clear_Btn_GUI();

            Drag_And_Drop_GUI();

            GUILayout.EndVertical();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void Drag_And_Drop_GUI()
        {
            GUILayout.Space(20);
            GUILayout.BeginVertical(GUILayout.Width(position.width), GUILayout.Height(position.height - 200));
            _reorderableList.DoLayoutList();

            var evt = Event.current;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is GameObject gameObject)
                        {
                            _objectsToSimplify.Add(gameObject);
                            _objectSelected = true;
                        }
                    }
                }

                Event.current.Use();
            }
        }

        private void List_Clear_Btn_GUI()
        {
            GUILayout.Space(20);
            if (GUILayout.Button("List Clear", GUILayout.Height(20f), GUILayout.Width(position.width - minuswidth)))
            {
                _objectsToSimplify.Clear();
                _objectSelected = false;
            }
        }

        private void Select_Object_from_Scene_GUI()
        {
            GUILayout.Space(20);
            if (GUILayout.Button("Select Object from Scene", GUILayout.Height(20f),
                    GUILayout.Width(position.width - minuswidth)))
            {
                var selectedObjects = Selection.gameObjects;
                if (selectedObjects.Length > 0)
                {
                    _objectSelected = true;
                    _objectsToSimplify.AddRange(selectedObjects);
                    _objPath = selectedObjects[0].name;
                }
                else
                {
                    Debug.LogError("No object selected in the scene.");
                }
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

        private void Toggle_Colider_GUI()
        {
            GUILayout.Space(20);
            _isColider = EditorGUILayout.Toggle("Use Colider", _isColider);
            GUILayout.Space(20);
            _isHLOD = EditorGUILayout.Toggle("Use HLOD", _isHLOD);
        }

        private void SelectPath_Btn_GUI()
        { 
 ;
            GUILayout.BeginVertical();
            GUILayout.Box(_icon, GUILayout.Height(140f), GUILayout.Width(140f));
            if (GUILayout.Button("Select Save Path", GUILayout.Height(20f), GUILayout.Width(position.width - minuswidth)))
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
        }

        private void ObjectSelected_GUI()
        {
            GUILayout.Space(20);
            EditorGUILayout.LabelField("Quality Factor: ", GUILayout.Height(20f), GUILayout.Width(position.width - minuswidth));
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

            if (GUILayout.Button("Generate", GUILayout.Height(20f), GUILayout.Width(position.width-minuswidth)))
            {
                foreach (var obj in _objectsToSimplify)
                {
                    LODGenerator.Generator(obj, _hSliderValue, SavePath, _isColider,_isHLOD);
                }
            }
        }
    }
}