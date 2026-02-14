using UnityEngine;

namespace Plugins.Auto_LOD_Generator.Editor.Core
{
    /// <summary>
    /// [설명]: LOD 및 HLOD 생성을 담당하는 서비스 인터페이스입니다.
    /// 로직의 추상화를 통해 다양한 단순화 알고리즘 및 생성 방식을 지원합니다.
    /// </summary>
    public interface ILODService
    {
        /// <summary>
        /// [설명]: 대상 오브젝트에 대해 LOD 그룹을 생성합니다.
        /// </summary>
        /// <param name="originalObject">LOD를 생성할 원본 오브젝트</param>
        /// <param name="qualityFactor">메쉬 단순화 품질 계수 (0.0 ~ 1.0)</param>
        /// <param name="savePath">생성된 메쉬 애셋을 저장할 경로</param>
        /// <param name="useCollider">메쉬 콜라이더 생성 여부</param>
        /// <param name="useHLOD">HLOD 시스템 통합 여부</param>
        void GenerateLOD(GameObject originalObject, float qualityFactor, string savePath, bool useCollider, bool useHLOD);
    }
}
