#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Plugins.Auto_LOD_Generator.EditorScripts
{
    public class AutoLOD : UnityEditor.Editor
    {
       
        [MenuItem("AutoLOD/Create a LOD Group on the object", false, -1)]
        public static void AutoLODGenerator()
        {
            ShowLODGroupWindow();
        }
        
        private static void ShowLODGroupWindow()
        {
            var popUp = CreateInstance<LODGroupWindow>();
            popUp.position = new Rect(Screen.width / 2.0f, Screen.height / 2.0f, 250, 100);
            popUp.minSize = new Vector2(600, 800); 
            popUp.maxSize = new Vector2(1200, 1600); 
            popUp.ShowPopup();
        }
    }
}
#endif