# LOD Generator (v1.0.7)

LOD Generator는 유니티 엔진에서 고품질의 LOD(Level of Detail) 및 HLOD(Hierarchical Level of Detail)를 효율적으로 생성하고 관리할 수 있도록 설계된 도구입니다.

## � 저장소 및 설치
- **GitHub Repository**: [https://github.com/frod2793/LOD_Generator.git](https://github.com/frod2793/LOD_Generator.git)
- **설치 방법**: 유니티의 패키지 매니저(Package Manager)에서 `Add package from git URL...`을 통해 위 URL을 입력하여 설치할 수 있습니다.

---

## 🛠 사용 방법 (Usage Guide)

### 1. 도구 실행
- 유니티 상단 메뉴에서 `AutoLOD` > `Create a LOD Group on the object`를 클릭하여 **LOD Generator** 윈도우를 엽니다.

### 2. 초기 설정
1. **Save Path 설정**: 에셋이 저장될 기본 경로를 지정합니다. (`Select Save Path` 버튼 클릭)
2. **옵션 구성**:
   - `Use Collider`: 생성된 LOD 오브젝트에 메쉬 콜라이더를 포함할지 여부를 결정합니다.
   - `USE HLOD`: 계층형 LOD(HLOD) 생성 프로세스를 활성화합니다.

### 3. LOD 생성 흐름
1. **오브젝트 등록**: 씬(Scene)에서 생성하고자 하는 오브젝트를 드래그 앤 드롭하여 목록에 추가합니다.
2. **LOD 생성**: `Generate LOD` 버튼을 클릭합니다.
   - 원본 오브젝트 하위에 `[ObjectName]_LODGroup`이 생성됩니다.
   - 지정한 경로에 단순화된 메쉬 에셋들이 자동 저장됩니다.

### 4. HLOD 생성 흐름 (`USE HLOD` 체크 시)
1. **기본 LOD 생성**: 먼저 위의 LOD 생성 과정을 완료합니다.
2. **HLOD 오브젝트 수집**: 생성된 LOD 그룹 하위의 `HLOD` 컴포넌트를 가진 오브젝트들을 `Get HLOD` 버튼으로 수집합니다.
3. **HLOD 베이크**: `Generate HLOD` 버튼을 눌러 최종 HLOD를 생성합니다.
   - 베이크가 완료되면 `LOD Group`의 최하위 단계에 HLOD 메쉬가 할당됩니다.

---

## 📌 주요 특징
- **자동화된 워크플로우**: 복잡한 설정 없이 드래그 앤 드롭만으로 LOD 그룹 구성 가능
- **HLOD 최적화**: 대규모 환경에서 렌더링 부하를 줄이기 위한 계층형 LOD 지원
- **표준화된 파일 구조**: 생성된 모든 에셋은 `LOD`, `HLOD` 폴더로 자동 분류되어 관리됩니다.

## ⚠️ 요구 사항
- **Unity Version**: 2021.3+ (LTS 권장)
- **필수 구성**: 씬에 `MeshRenderer`와 `MeshFilter`가 있는 오브젝트가 배치되어 있어야 합니다.