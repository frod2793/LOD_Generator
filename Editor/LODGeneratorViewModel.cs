using System;
using System.Collections.Generic;
using UnityEngine;
using Plugins.Auto_LOD_Generator.Editor.Core;

namespace Plugins.Auto_LOD_Generator.EditorScripts
{
    /// <summary>
    /// [설명]: LOD 작업을 위한 에디터 윈도우의 비즈니스 로직과 상태를 관리하는 뷰모델입니다.
    /// MVVM 패턴을 따르며, View(LODGroupWindow)와 Service(LODService) 사이의 가교 역할을 합니다.
    /// </summary>
    public class LODGeneratorViewModel
    {
        #region 내부 필드
        private readonly ILODService m_lodService;
        private string m_savePath = string.Empty;
        private float m_qualityFactor = 0.5f;
        private bool m_useCollider = false;
        private bool m_useHLOD = false;
        private List<GameObject> m_targetObjects = new List<GameObject>();
        private string m_statusMessage = "Ready";
        private float m_progress = 0f;
        #endregion

        #region 프로퍼티
        /// <summary>
        /// [설명]: 메쉬 애셋 저장 경로입니다.
        /// </summary>
        public string SavePath
        {
            get => m_savePath;
            set => m_savePath = value;
        }

        /// <summary>
        /// [설명]: 단순화 퀄리티 계수 (0.0 ~ 1.0)입니다.
        /// </summary>
        public float QualityFactor
        {
            get => m_qualityFactor;
            set => m_qualityFactor = Mathf.Clamp01(value);
        }

        /// <summary>
        /// [설명]: 메쉬 콜라이더 생성 여부입니다.
        /// </summary>
        public bool UseCollider
        {
            get => m_useCollider;
            set => m_useCollider = value;
        }

        /// <summary>
        /// [설명]: HLOD 통합 여부입니다.
        /// </summary>
        public bool UseHLOD
        {
            get => m_useHLOD;
            set => m_useHLOD = value;
        }

        /// <summary>
        /// [설명]: LOD를 생성할 대상 오브젝트 리스트입니다.
        /// </summary>
        public IReadOnlyList<GameObject> TargetObjects => m_targetObjects;

        /// <summary>
        /// [설명]: 현재 작업 상태 메시지입니다.
        /// </summary>
        public string StatusMessage => m_statusMessage;

        /// <summary>
        /// [설명]: 현재 작업 진행률 (0.0 ~ 1.0)입니다.
        /// </summary>
        public float Progress => m_progress;
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: 의존성 주입을 통해 서비스를 초기화합니다.
        /// </summary>
        /// <param name="lodService">LOD 생성 서비스 인스턴스</param>
        public LODGeneratorViewModel(ILODService lodService)
        {
            m_lodService = lodService;
        }
        #endregion

        #region 공개 메서드 (Commands)
        /// <summary>
        /// [설명]: 대상 리스트에 오브젝트를 추가합니다.
        /// </summary>
        public void AddTarget(GameObject obj)
        {
            if (obj == null || m_targetObjects.Contains(obj)) return;

            // 빈 슬롯(null)이 있으면 먼저 채움
            int emptyIndex = m_targetObjects.IndexOf(null);
            if (emptyIndex != -1)
            {
                m_targetObjects[emptyIndex] = obj;
            }
            else
            {
                m_targetObjects.Add(obj);
            }
        }

        /// <summary>
        /// [설명]: 대상 리스트를 초기화합니다.
        /// </summary>
        public void ClearTargets()
        {
            m_targetObjects.Clear();
        }

        /// <summary>
        /// [설명]: 설정된 옵션에 따라 모든 대상 오브젝트의 LOD를 생성합니다.
        /// </summary>
        public void GenerateLODs()
        {
            if (m_lodService == null)
            {
                Debug.LogError("[LODViewModel] 서비스가 초기화되지 않았습니다.");
                return;
            }

            if (string.IsNullOrEmpty(m_savePath))
            {
                Debug.LogWarning("[LODViewModel] 저장 경로가 설정되지 않았습니다.");
                return;
            }

            m_progress = 0f;
            int total = m_targetObjects.Count;
            int current = 0;

            foreach (var target in m_targetObjects)
            {
                if (target == null) continue;
                
                current++;
                m_statusMessage = $"Processing: {target.name} ({current}/{total})";
                m_progress = (float)current / total;

                try
                {
                    m_lodService.GenerateLOD(target, m_qualityFactor, m_savePath, m_useCollider, m_useHLOD);
                    Debug.Log($"[LODViewModel] {target.name}의 LOD 생성이 완료되었습니다.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LODViewModel] {target.name} 생성 실패: {e.Message}");
                }
            }

            m_statusMessage = "All tasks completed successfully.";
            m_progress = 1.0f;
        }
        #endregion
    }
}
