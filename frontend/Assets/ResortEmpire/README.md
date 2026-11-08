# Resort Empire – sprite sheety pre Unity

## Import
Priečinok `ResortEmpire` (aj s `ResortEmpire.meta`) presuň do `Assets/`.
Každý PNG je **sprite sheet** (Sprite Mode = Multiple) a je už rozrezaný: po rozkliknutí šípky pri PNG
v Project okne uvidíš jednotlivé sprity s menami. Pixels Per Unit 128, pivoty nastavené.

- `Sprites/Buildings/<Budova>.png` – riadky Lv1, Lv2, Lv3 a stĺpce R0–R3 (napr. `Reception_Lv2_R1`)
- `Sprites/Decor/<Názov>.png`, `Sprites/Transport/<Názov>.png` – R0–R3
- `Sprites/Terrain/Terrain.png` – Deep / Shallow / Sand / Grass × R0–R3
- `Sprites/Paths/Path_<Štýl>.png` – 16 kusov podľa susedov (`Path_Stone_00` až `_15`)
- `Data/SpriteCatalog.json` – footprinty, rotácie, levely, pivoty

## Nastavenie scény
1. Tilemap → Isometric, Grid **Cell Size = (1, 0.5, 1)**.
2. Transparency Sort Mode = **Custom Axis (0, 1, 0)** (URP 2D: v Renderer 2D Data).
3. Sorting Layers: Ground, Paths, Objects. SpriteRenderer → Sprite Sort Point = **Pivot**.

## Pivot a výška
Pivot = stred footprintu na úrovni vody. Na piesku posuň objekt o **Y +0.0735**, na tráve o **Y +0.147**.
Pri R1 a R3 sa footprint prehodí (2×4 → 4×2).

## Maska chodníka
1 = sused −x, 2 = +x, 4 = −y, 8 = +y. Súčet = číslo spritu (`03` rovný kus, `15` križovatka).
