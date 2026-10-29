# SortByRawName (Jellyfin 12.2)

Jellyfin 12는 한글 제목을 로마자로 바꾼 뒤 그 값으로 작품 목록을 정렬합니다. 그래서 `아`는 A, `바`는 B, `차`는 C 근처에 붙습니다.

이 플러그인은 그 로마자 변환을 끄고, 이미 저장된 정렬 제목을 원문으로 다시 씁니다. 정렬 순서는 숫자, 영어, 한글 가나다입니다.

Jellyfin 12.2용으로 빌드했습니다. 서버가 12.2.0 이상이어야 로드됩니다.

## 저장소로 설치

1. 관리자 대시보드에서 플러그인 → 저장소로 이동합니다.
2. 저장소를 추가합니다. 이름은 `SortByRawName`, 저장소 URL은 아래 주소입니다.

   `https://raw.githubusercontent.com/DEPFRONN/jellyfin-sortbyrawname/main/manifest.json`

3. 카탈로그에서 SortByRawName 1.0.0.0을 설치합니다.
4. 젤리핀을 재시작합니다.
5. 대시보드의 예약 작업에서 **원문 제목으로 정렬 다시 적용**이 끝날 때까지 기다린 뒤, 작품 목록을 새로고침합니다.

## 수동 설치

1. 예전에 쓰던 SortByRawName 플러그인이 있으면 플러그인 폴더에서 지웁니다. 12.2에서는 동작하지 않고, 같이 두면 서버 시작을 방해할 수 있습니다.
2. 아래 폴더에 있는 `SortByRawName.dll`과 `meta.json`을 복사합니다. 다시 빌드할 때는 `dotnet build src/SortByRawName/SortByRawName.csproj -c Release` 를 실행합니다.

   `src/SortByRawName/bin/Release/net10.0/`
3. 젤리핀 데이터 폴더의 `plugins/SortByRawName/`에 넣습니다. Docker라면 보통 `/config/plugins/SortByRawName/` 입니다.
4. 젤리핀을 재시작합니다.
5. 대시보드의 예약 작업에서 **원문 제목으로 정렬 다시 적용**이 끝날 때까지 기다립니다. 시작 때 한 번 실행됩니다. 끝나기 전에 목록을 보면 이전 순서가 남아 있을 수 있습니다.
6. 브라우저에서 작품 목록을 새로고침합니다.

오른쪽 A–Z 색인은 한글 첫 글자를 로마자 칸에 넣지 않습니다. 한글 작품은 목록 뒤쪽, 가나다 순으로 모입니다.

## 플러그인 없이 환경 변수만 쓸 때

`JELLYFIN_TRANSLITERATOR_ID`를 빈 문자열로 두고 서버를 다시 시작한 뒤, 작품 정보를 다시 저장해야 합니다. 변수를 비우지 않고 빼 버리면 기본값(로마자 변환)이 그대로입니다. NAS 설정 화면은 빈 값을 지우는 경우가 많아서, 그 경우에는 이 플러그인을 쓰는 편이 맞습니다.
