# Resort Empire – postavičky (hostia, pracovníci, VIP) a bubliny

Priečinok `ResortEmpire` vlož do `Assets/`. Spojí sa s tým, čo tam už máš (Sprites, UI).

## Čo je vnútri
- `Sprites/Characters/Guests` – 6 bežných hostí (Tourist s kufrom, Sunhat, Backpacker, Grandpa, Swimmer, Striped).
- `Sprites/Characters/Workers` – 6 pracovníkov. Oblečenie podľa budovy:
  Staff = nepriradený, Receptionist = Reception, Chef = Restaurant, Bartender = Beach Bar,
  Lifeguard = Pool, Housekeeper = Bungalows.
- `Sprites/Characters/VIP` – 8 VIP hostí (RockKing, FilmStar, PopDancer, Spy, Professor, RockSinger, BellRapper, SneakerStar).
- `Sprites/Bubbles` – bubliny nad hlavou: potreby (Rooms, Food, Drinks, Relax, OceanView), Happy, Angry, Waiting, Love, Alert, Vip.
- `Data/CharacterCatalog.json` – zoznam animácií, počet snímok a odporúčané FPS.

## Ako je sheet usporiadaný
Každá postavička = 1 PNG, už rozrezané (Sprite Mode = Multiple), pivot je pri nohách.
- **Riadky = smer:** SE, SW, NW, NE (SE = pohyb po mriežke v smere +x, SW = +y, NW = −x, NE = −y).
- **Stĺpce = snímky:** Idle_0–1, Walk_0–3 a jedna akcia_0–1
  (hostia **Cheer** = spokojný, pracovníci **Work** = obsluhuje, VIP **Wave** = máva).

Pixels Per Unit = 256 (postavičky sú kreslené v 2× rozlíšení, takže sú ostré, ale veľkosťou sedia k budovám).

## Animácia v Unity (najjednoduchšie)
1. V Project okne rozbaľ sheet (šípka pri PNG).
2. Označ napr. `Guest_Tourist_SE_Walk_0` až `_3` a pretiahni ich do Scene.
3. Unity sa spýta, kam uložiť animáciu – ulož ako `Tourist_Walk_SE.anim`. Vytvorí aj Animator.
4. Rýchlosť: v okne Animation nastav Samples = 8 (Walk), 2 (Idle), 4 (Cheer/Work/Wave).

Bublina = samostatný SpriteRenderer ako dieťa postavičky, posunutý nad hlavu (približne Y = 0.65).
