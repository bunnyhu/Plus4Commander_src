# Plus4Commander — Használati útmutató

A **Plus4 Commander** egy Commodore Plus/4 (és 16) lemezkép- (`.d64`), magnószalagkép- (`.t64`) és fájlböngésző segédprogram, amely a fájlkezelők logikáját ötvözi beépített Plus/4 elemző és megjelenítő eszközökkel. Gyors előnézetet nyújt a Commodore fájlok (`.PRG`, `.SEQ`, `.USR`, `.REL`) tartalmába, automatikusan felismerve azok típusát.

A program nem igényel telepítést, önállóan futtatható, és a Commodore karakterkészletet is beépítve tartalmazza.

---

## 1. Felhasználói felület és navigáció

A program két fő részből áll: a **bal oldali fájllistából** (PC könyvtárak és virtuális lemez-/szalagképek) és a **jobb oldali elemző/előnézeti panelből**.

### Felső navigációs és műveleti sáv
* **Meghajtóválasztó (`C:\`, `D:\`, ...) és Gyökér gomb (`\`):** Gyors váltás a meghajtók között, a program meghajtónként megjegyzi az utoljára használt mappát.
* **Elérési út címsor:** A mező szerkeszthető, a program azonnal odaugrik. Ha `.d64` vagy `.t64` fájlt illeszted be, rögtön megnyitja a tartalmát.
* **Export (`F5`):** Képfájlból (`.d64` / `.t64`) kiexportálja a kijelölt fájlt.
* **Mount+Load (`F6`):** Képfájlon belül állva betölti a képfájlt és a kijelölt fájlt a YAPE emulátorba. PC mappában egy képfájlon fájlon állva automatikus indítással (autostart) csatolja a YAPE emulátorba.
* **Quick Run (`F7`):** Ha BASIC program van kijelölve, azonnal elindítja a YAPE emulátorban.
* **About (`F1`):** A program névjegye, itt állítható be a YAPE emulátor elérési útja is.
* **Exit:** Bezárja az alkalmazást.

### Bal oldali fájlböngésző és lemezfejléc
* **Fejléc:** PC módban a meghajtó nevét, címkéjét és fájlrendszerét mutatja. D64/T64 képfájlon belül a lemez vagy szalag eredeti Commodore címét, azonosítóját (`Disk ID`) és DOS verzióját jeleníti meg.
* **Fájllista:**
  * PC mappában egy `.d64` vagy `.t64` fájlon állva előnézetként megjeleníti a teljes könyvtárat (`DIRECTORY`), a fájlok számát és a blokkinformációkat.
  * Mappából vagy képfájlból való visszalépés: `[..]` vagy `Backspace`.
* **Lábléc:** PC módban a szabad/összes tárhelyet és az elemek számát, D64 esetén a szabad/összes blokkot (`BLOCKS FREE`), T64 esetén pedig a foglalt blokkokat és a fájlszámot mutatja.

---

## 2. Fájlműveletek és helyi menü (jobb egérgomb)

A bal oldali fájllistán jobb egérgombbal kattintva elérhetők a fájl- és képfájlkezelő műveletek:

* **Másolás (`Ctrl+C`) és Beillesztés (`Ctrl+V`):** Commodore fájlok másolása vágólapon keresztül — akár PC mappa és képfájl között, akár két külön futó **Plus4 Commander** ablak között is (pl. egyik `.d64`-ből a másikba). Ha a célhelyen már létezik azonos nevű fájl, a program rákérdez a felülírásra.
* **Fájl átnevezése (`F2`):** Működik PC mappában és D64/T64 képfájlon belül is (Commodore fájloknál max. 16 karakteres névvel).
* **Lemez-/szalagcímke átnevezése:** D64/T64 képfájlon belül, vagy PC mappában egy képfájlon állva átírható a belső címke (D64: max. 16 karakter + 2 karakteres Disk ID; T64: max. 24 karakter).
* **Fájl törlése képfájlból (`Del`):** Megerősítés után törli a kijelölt fájlt a megnyitott `.d64` vagy `.t64` képfájlból, és felszabadítja a helyét.
* **Üres D64 / T64 létrehozása:** PC mappában állva új, üres lemez- vagy szalagképfájl hozható létre tetszőleges címkével és (D64 esetén) Disk ID-val.

### Előnézeti panel vágólapkezelés
* **Szöveges nézetekben (`Basic`, `Asm`, `HEX`, `Text`):** A szöveges mezőn jobb egérgombbal kattintva kijelölhető és vágólapra másolható a forráskód vagy dump.
* **Grafikus nézetekben (`Image`, `Font`):** A képre jobb egérgombbal kattintva (vagy rákattintás után `Ctrl+C`-vel) a renderelt kép vágólapra másolható eredeti (`Copy Image`) vagy 2×-es éles nagyítással (`Copy Image (2x)`).

---

## 3. Nézetválasztó gombok és speciális vezérlők

A jobb oldali előnézeti ablak feletti gombsorral bármikor felülbírálható az automatikus felismerés:

| Gomb | Funkció |
| :--- | :--- |
| **Auto** | Visszaállítja az automatikus tartalomfelismerést (`Space` billentyűvel is aktiválható). |
| **HEX** | 16 bájtos sorokba rendezett hexadecimális és ASCII nézet. |
| **Basic** | Commodore BASIC V3.5 programok listázása eredeti Commodore grafikus karakterekkel. |
| **Asm** | 6502 / 7501 gépi kódú disassembler (cím, nyers bájtok, utasítások). |
| **Text** | Szöveges megjelenítés (`SEQ`, `USR`, illetve `REL` fájloknál rekordonként sorszámozva). |
| **Image** | Plus/4 grafikus képek (320×200) kirajzolása. |
| **Font** | Egyedi karakterkészletek (256 darab 8×8-as karakter) megjelenítése 16×16-os rácsban. |
| **Pal: Auto / Bott / Mult / Raw** | Palettaforrás választása kép nézetben (automatikus felismerés, klasszikus Botticelli `$1BE8/$1BE9`, nibble-fordított `MULT` vagy nyers alapértelmezett színek). |
| **Color / HiRes** | Kép nézetben vált a színes (Multicolor) és a nagyfelbontású de monokróm (HiRes) mód között. |
| **◄ Font / Font ►** | Karakterkészlet nézetben 1-1 bájttal eltolja a megjelenítés kezdőpontját, ha a fájl nem szabványos fejléccel rendelkezik. |

---

## 4. Automatikus előnézet (`Auto` mód)

Egy Commodore fájl kijelölésekor a program automatikusan a legvalószínűbb nézetet választja ki:

1. **Szöveges / adatbázis fájl (`Text`):** `SEQ`, `USR` és `REL` fájloknál (utóbbinál D64-en belül a rekordhosszt is felismeri és sorokra tördeli).
2. **Karakterkészlet (`Font`):** Kb. 2 KB-os (2048–2060 bájtos) fájloknál.
3. **BASIC V3.5 program (`Basic`):** Szabványos (`$1001`) vagy grafikus módú (`$4001`) BASIC programoknál.
4. **Grafikus kép (`Image`):** 7000–12500 bájt közötti képernyő- és bitképfájloknál.
5. **Gépi kód (`Asm`):** Minden egyéb bináris programnál.

### Nagy fájlok gyors előnézete
A gyors böngészés érdekében a **HEX** nézet alapból az első 1024 bájtot, az **Asm** nézet pedig az első 400 utasítást mutatja. Ennél nagyobb fájloknál az ablak alján megjelenik a **Show All / Show Full Disasm** gomb (vagy a szövegre duplán kattintva is betölthető a teljes tartalom).

---

## 5. Billentyűparancsok

| Billentyű | Művelet |
| :--- | :--- |
| **`↑` / `↓`** | Lépegetés a fájlok és mappák között |
| **`Enter`** | Belépés mappába vagy `.d64` / `.t64` képfájlba |
| **`Backspace`** | Visszalépés a szülő mappába vagy kilépés a képfájlból |
| **`←` / `→`** | Váltás az előnézeti módok (`Auto`, `HEX`, `Basic`, `Asm`, `Text`, `Image`, `Font`) között |
| **`Space`** | Visszaállás automatikus (`Auto`) előnézeti módra |
| **`Ctrl+C`** | Kijelölt Commodore fájl (vagy grafikus előnézeti kép) másolása vágólapra |
| **`Ctrl+V`** | Vágólapon lévő Commodore fájl beillesztése (PC mappába vagy képfájlba) |
| **`F2`** | Kijelölt fájl átnevezése |
| **`Del`** | Kijelölt fájl törlése a D64/T64 képfájlból |
| **`F5`** | Kijelölt fájl exportálása a D64/T64 képfájlból PC-re |
| **`F6`** | Képfájl mountolás és fájl betöltés YAPE-ben |
| **`F7`** | BASIC fájl gyorsindítás YAPE-ben |
| **`F1`** | Névjegy (`About`) ablak megnyitása |
| **`Esc`** | Szerkesztés megszakítása a felső címsorban |

---

* **Forráskód és letöltés:** [https://github.com/bunnyhu/Plus4Commander](https://github.com/bunnyhu/Plus4Commander)