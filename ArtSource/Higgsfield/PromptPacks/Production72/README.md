# IanGrowing — Production72

사용자가 제공한 72개 프롬프트의 원문 버전이다. 기존 Phase 3.3 원문/선택 결과는 덮어쓰지 않는다.

- `MASTER_STYLE.txt`, `NEGATIVE_DIRECTION.txt`: 공통 원문.
- `001.txt`–`072.txt`: 번호별 본문 원문. `BODIES.txt`는 순서 검증용 원문 묶음.
- `manifest.json`: 제목, Batch, 장소 의존성, 이안 등장 여부, 후보/생성 ID/URL, 검수 이력, 선택 원본 SHA-256, Unity 출력 경로.
- `assets.csv`: 레퍼런스의 제작 역할과 비율/스타일 계약.
- `IAN_IDENTITY.txt`, `IAN_WEEKDAY_SEQUENCE.txt`: 보너스 캐릭터 기준과 미제출 일상 시퀀스.

실제 제출 프롬프트는 `../../Prompts/p72_*.txt`다. MASTER + 해당 원문 + 기존 공통 STYLE FORMULA + NEGATIVE를 그대로 포함한다. 시트 분리/방향/픽셀 격자/위치 보존 지시는 뒤에 추가하며, 원문을 몰래 수정하지 않는다.

## 실행

```sh
# 메타데이터/로컬 프롬프트만 준비. 생성 API를 호출하지 않는다.
python3 Tools/Higgsfield/production72.py prepare 1 15 41 59 60 63 67 71

# 명시한 후보만 유료 생성. 후보마다 독립 요청 1회, 자동 재시도 없음.
python3 Tools/Higgsfield/generate_pack.py p72_001_a p72_001_b --workers 2

# 두 후보를 실제로 열어 검수한 뒤 선택. 레퍼런스 선택이며 Game Ready 승인이 아니다.
python3 Tools/Higgsfield/production72.py select 1 a --note '구체적인 검수 결과'

# 위 장소를 고정한 이후에만 변형을 준비할 수 있다.
python3 Tools/Higgsfield/production72.py prepare 2
python3 Tools/Higgsfield/production72.py report
python3 Tools/Higgsfield/production72.py audit
```

후보가 모두 부적합하면 실패 원인을 기록하고 원인 하나를 고친 새 revision에 후보 2개를 준비한다. 기존 제출 프롬프트/참조/결과는 불변이다. 위 명령의 후보 ID는 예시이며 현재 활성 revision은 manifest를 확인한다.

## 생성 순서

1. Batch 1: **01, 02, 15, 16, 18, 41, 42, 59, 60, 63, 67, 71**.
2. Batch 2: 21–26. 21을 선택한 뒤 22–26이 해당 이미지를 재사용한다.
3. Batch 3: 09–14, 27–34, 35–40. 장소별 기준 이미지부터 고정한다.
4. Batch 4: 49–58. 은행/투자/부동산 기준 이미지를 재사용한다.
5. Batch 5: 63–72. **Batch 1에서 생성한 63/67/71은 재제출하지 않는다.**
6. 나머지 원문은 `supplement` 대기열이다. 자동 생성하지 않는다.

이안 등장 장면은 공식 원본을 마지막 참조 이미지로 연결한다. 장소 변형은 첫 참조 이미지가 고정된 장소다. 한 번 변형 생성에 사용한 장소는 다른 후보로 자동 교체할 수 없다.

## 검수 / Unity

픽셀 에지, 2.5D 카메라, 인간/문/차량 크기, 통로, 이안 정체성, 브랜드/글자, 절제된 블랙코미디를 확인한다. 부드러운 일러스트를 픽셀 아트 통과로 기록하지 않는다. 읽을 수 있는 생성 글자는 런타임에 가져오지 않는다.

RAW → 선택 레퍼런스 → 개별 오브젝트 분리/픽셀 정리 → Game Ready QA → Unity Point/40 PPU/무압축 → Prefab/Atlas → Game View. Scene 전체 PNG는 런타임 배경으로 넣지 않는다. 선택 시트는 방향별 애니메이션 완성을 의미하지 않는다.
