# 기능과 검증 범위

포트폴리오 설명은 아래 실제 구현을 기준으로 작성합니다.

| 기능 | 구현 | 확인 방식 |
| --- | --- | --- |
| 실 배치·면적·번호 점검 | `InspectionEngine.Inspect` | 미배치·면적 0·중복 번호 예제 검사 |
| 문·창 Mark 점검 | `InspectionEngine.Inspect` | 빈 값·중복·분류별 구분·공백/대소문자 검사 |
| 빈 시트 점검 | `InspectionEngine.Inspect` | 뷰 없음·일람표 전용·예정 시트 검사 |
| CSV 보고서 | `ReportWriter.WriteCsv` | BOM·행 수·따옴표·줄바꿈·수식 시작 문자 검사 |
| 모델 정보 저장·읽기 | `ReportWriter.WriteSnapshot/ReadSnapshot` | JSON 왕복 후 점검 결과 비교 |
| WPF 분류 필터 | `InspectionWindow` | 실제 화면 인스턴스에서 전체/실/문/창/시트 건수 확인 |
| Revit 모델 수집 | `InspectCommand.Collect` | 설치된 Revit 2026 API로 컴파일; 호스트 실행 미검증 |
| Revit 요소 위치 보기 | `InspectCommand.Execute` | 선택·시트 열기·미배치 안내 코드 구현; 호스트 실행 미검증 |
| addin 설치 | `scripts/install-addin.ps1` | 시험 폴더의 manifest 경로·DLL 구성 확인 |

## 이번 실행 결과

- Release 빌드: 경고 0개, 오류 0개
- 검사 프로그램: 13/13 통과
- 샘플 결과: 검토 8건, CSV·JSON 저장
- 화면: 같은 점검 엔진을 사용하는 WPF Demo에서 캡처, 화면에 샘플 데이터임을 표시

Revit 호스트 동작과 실제 모델 검토는 이후 실행 확인이 필요합니다.
