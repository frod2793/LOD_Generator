#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Plugins.Auto_LOD_Generator.EditorScripts
{
    /// <summary>
    /// [설명]: LOD Generator의 프리미엄 UI 스타일 및 컬러 토큰을 정의하는 정적 클래스입니다.
    /// </summary>
    public static class LODGeneratorStyles
    {
        #region 컬러 토큰
        public static readonly Color k_PrimaryColor = new Color(0.2f, 0.6f, 1.0f);
        public static readonly Color k_SecondaryColor = new Color(0.15f, 0.15f, 0.15f);
        public static readonly Color k_AccentColor = new Color(0.1f, 0.8f, 0.4f);
        public static readonly Color k_BackgroundColor = new Color(0.12f, 0.12f, 0.12f);
        public static readonly Color k_BorderColor = new Color(0.25f, 0.25f, 0.25f);
        public static readonly Color k_TextColor = new Color(0.9f, 0.9f, 0.9f);
        public static readonly Color k_MutedTextColor = new Color(0.6f, 0.6f, 0.6f);
        #endregion

        #region 커스텀 GUIStyle 프로퍼티
        public static GUIStyle CardStyle { get; private set; }
        public static GUIStyle HeaderStyle { get; private set; }
        public static GUIStyle TitleStyle { get; private set; }
        public static GUIStyle SubtitleStyle { get; private set; }
        public static GUIStyle PremiumButtonStyle { get; private set; }
        public static GUIStyle ProgressBarStyle { get; private set; }
        public static GUIStyle ProgressBarFillStyle { get; private set; }
        public static GUIStyle DragAndDropAreaStyle { get; private set; }
        public static GUIStyle InfoBoxStyle { get; private set; }
        #endregion

        #region 초기화
        public static void InitializeStyles()
        {
            // GUI.skin은 OnGUI 내부에서만 안전하게 접근 가능합니다.
            if (CardStyle != null || GUI.skin == null) return;

            // 카드 스타일 (섹션 컨테이너)
            CardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(15, 15, 15, 15),
                margin = new RectOffset(10, 10, 10, 10),
                normal = { background = CreateTexture(2, 2, k_SecondaryColor) }
            };

            // 헤더 스타일
            HeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                normal = { textColor = k_PrimaryColor },
                margin = new RectOffset(0, 0, 0, 10)
            };

            // 타이틀 스타일
            TitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = k_TextColor },
                margin = new RectOffset(0, 0, 5, 5)
            };

            // 서브타이틀 스타일
            SubtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                normal = { textColor = k_MutedTextColor },
                margin = new RectOffset(0, 0, 0, 5)
            };

            // 프리미엄 버튼 스타일
            PremiumButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                fixedHeight = 36,
                normal = { textColor = Color.white, background = CreateTexture(2, 2, k_PrimaryColor) },
                hover = { background = CreateTexture(2, 2, k_PrimaryColor * 1.2f) },
                active = { background = CreateTexture(2, 2, k_PrimaryColor * 0.8f) },
                margin = new RectOffset(5, 5, 5, 5)
            };

            // 프로그레스 바 배경
            ProgressBarStyle = new GUIStyle(GUI.skin.box)
            {
                fixedHeight = 6,
                normal = { background = CreateTexture(2, 2, k_BorderColor) },
                margin = new RectOffset(0, 0, 10, 10)
            };

            // 프로그레스 바 채우기
            ProgressBarFillStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = CreateTexture(2, 2, k_AccentColor) }
            };

            // 드래그 앤 드롭 영역 스타일
            DragAndDropAreaStyle = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = k_TextColor },
                padding = new RectOffset(5, 5, 5, 5),
                margin = new RectOffset(5, 5, 5, 5)
            };

            // 정보 박스 스타일
            InfoBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                fontSize = 12,
                normal = { textColor = k_TextColor }
            };
        }
        #endregion

        #region 텍스처 생성 유틸리티
        private static Texture2D CreateTexture(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = color;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
        #endregion
    }
}
#endif
