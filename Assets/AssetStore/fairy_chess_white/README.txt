페어리 체스 도트 기물 (흰색) — 18종
=====================================
- fairy_chess_white_sheet.png : 스프라이트 시트 (96x96, 6열 x 3행, 칸 16x32, 여백 없음)
- fairy_chess_white_sheet.json: 각 칸 좌표 + 한글 이름 (TexturePacker JSON-Hash 형식, Phaser/Pixi 호환)
- pieces/      : 기물별 PNG (16x32 원본 크기, 샘플과 동일 규격)
- pieces_x4/   : 4배 확대본 (미리보기/문서용)
- slice_sheet.py : 시트를 다시 잘라 PNG로 저장하는 스크립트
    python slice_sheet.py fairy_chess_white_sheet.png            -> pieces/
    python slice_sheet.py fairy_chess_white_sheet.png --scale 4  -> 확대본
- 팔레트(샘플과 동일 4색): #eaf0d8  #96a2b3  #596070  #1f1f29

엔진에서 슬라이스할 때
- Unity : Sprite Mode = Multiple, Slice = Grid By Cell Size 16x32, Pivot = Bottom,
          Filter Mode = Point, Compression = None
- Godot : Sprite2D hframes=6, vframes=3 (또는 AtlasTexture 16x32), Texture Filter = Nearest
- 기준선: 받침이 있는 기물은 모두 맨 아래 줄(y=31)에 바닥이 닿도록 정렬되어 있음

순서 (시트 좌상단부터)
01  아마존      amazon
02  챈슬러      chancellor
03  아치비숍     archbishop
04  페르즈      ferz
05  알필       alfil
06  켄타우로스    centaur
07  제브라      zebra
08  다바바      dabbaba
09  지라프      giraffe
10  와지르      wazir
11  캐논       cannon
12  나이트라이더   nightrider
13  만        mann
14  그래스호퍼    grasshopper
15  캐멀       camel
16  월드비스트    wildebeest
17  드래곤킹     dragon_king
18  드래곤호스    dragon_horse
