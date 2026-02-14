#if UNITY_EDITOR
using System;
using UnityEngine;
using Plugins.Auto_LOD_Generator.Editor.Core;

namespace Plugins.Auto_LOD_Generator.EditorScripts
{
    /// <summary>
    /// [설명]: LOD 생성을 위한 레거시 지원 및 정적 유틸리티 클래스입니다.
    /// 내부적으로 LODService를 사용하여 로직을 수행합니다.
    /// </summary>
    public static class LODGenerator
    {
        /// <summary>
        /// [설명]: 정적 메서드를 통해 LOD 그룹을 생성합니다. (레거시 지원용)
        /// </summary>
        /// <param name="originalObject">원본 오브젝트</param>
        /// <param name="qualityFactor">품질 계수 (0~1)</param>
        /// <param name="path">저장 경로</param>
        /// <param name="isCollider">콜라이더 생성 여부</param>
        /// <param name="isHLOD">HLOD 사용 여부</param>
        public static void Generator(GameObject originalObject, float qualityFactor, string path, bool isCollider, bool isHLOD)
        {
            if (originalObject == null) throw new ArgumentNullException(nameof(originalObject));

            // 새 아키텍처의 서비스를 사용하여 실행
            ILODService service = new LODService();
            service.GenerateLOD(originalObject, qualityFactor, path, isCollider, isHLOD);
        }
    }
}
#endif
// Refresh trigger
