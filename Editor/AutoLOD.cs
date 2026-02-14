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
            var popUp = EditorWindow.GetWindow<LODGroupWindow>(true, "LOD Generator", true);
            float width = 800;
            float height = 800;
            popUp.position = new Rect((Screen.currentResolution.width - width) / 2, (Screen.currentResolution.height - height) / 2, width, height);
            popUp.minSize = new Vector2(width, height); 
            popUp.maxSize = new Vector2(4000, 4000); 
            popUp.Show();
        }
    }
}
#endif