# Projekt: generowanie pustyni z obrazu w skali szarosci

Ten projekt tworzy prosta scene pustyni w Unity. Najwazniejsza czesc to generator terenu, ktory bierze obraz w skali szarosci i zamienia go na siatke 3D. Do tego dochodzi gorace oswietlenie, mgla piaskowa, efekt falowania powietrza i prosty kontroler FPS do chodzenia po terenie.

## Co robi projekt

W scenie znajduje sie obiekt `TerrainGenerator`. W Inspectorze mozna przypisac obraz wysokosci, ustawic rozmiar pustyni, wysokosc, rozdzielczosc siatki, poziom iso, material piasku i collider. Po kliknieciu `Generate Terrain` generator tworzy dziecko o nazwie `Generated Desert Terrain`.

Wygenerowany obiekt ma:

- `MeshFilter`
- `MeshRenderer`
- opcjonalny `MeshCollider`
- material pustynnego piasku

Do sceny dodany jest tez prosty streamer `InfiniteDesertTerrainStreamer`. Kopiuje wygenerowany kafel terenu wokol gracza i przed kamera, zeby podczas chodzenia nie bylo widac konca platformy.

Scena demo jest w pliku `Assets/Scenes/DesertDemo.unity`.

## Jak obraz w skali szarosci wplywa na teren

Generator odczytuje jasnosc pikseli z tekstury:

- biale miejsca daja wyzszy teren
- czarne miejsca daja niski teren
- szare miejsca daja wartosci posrednie

Dla kazdego punktu w siatce program sprawdza, czy punkt jest ponizej wysokosci wynikajacej z obrazu. Jesli jest ponizej, traktuje go jako "pelny". Jesli jest powyzej, traktuje go jako "pusty". Granica miedzy tymi stanami staje sie powierzchnia terenu.

## Co znaczy marching cubes w tym projekcie

W tym projekcie uzyty jest uproszczony pomysl marching cubes. Program dzieli przestrzen na male kostki, liczy wartosc pola w rogach kostek i szuka miejsca, gdzie pole przechodzi przez poziom `isoLevel`.

Zamiast duzej tabeli przypadkow dla marching cubes, kazda kostka jest dzielona na kilka prostszych bryl. Dzieki temu kod jest krotszy i latwiejszy do wytlumaczenia. Efekt jest ten sam w sensie ogolnej idei: z pola wartosci powstaje powierzchnia z trojkatow.

## Dodane skrypty

`DesertTerrainGenerator.cs`

Glowny komponent w Inspectorze. Przechowuje ustawienia, uruchamia generowanie, tworzy dziecko z meshem i przypina material oraz collider.

`DesertScalarField.cs`

Odpowiada za odczyt obrazu w skali szarosci. Zamienia jasnosc tekstury na wysokosc terenu i zwraca wartosc pola dla punktu 3D.

`DesertMarchingCubes.cs`

Buduje mesh. Przechodzi po siatce voxelowej, znajduje przeciecia z poziomem iso i dodaje trojkaty do siatki.

`GeneratedDesertTerrain.cs`

Prosty znacznik dla obiektow wygenerowanych przez generator. Dzieki temu generator wie, ktore stare obiekty moze usunac przy ponownym generowaniu.

`DesertTerrainTile.cs`

Przechowuje wspolrzedne kafla terenu. Streamer uzywa tego, zeby wiedziec, ktory kafel juz istnieje.

`InfiniteDesertTerrainStreamer.cs`

Tworzy kopie wygenerowanego kafla terenu. Zostawia bezpieczny obszar pod graczem, dodaje wiecej kafli w kierunku patrzenia i ruchu, a dalekie kafle usuwa albo wylacza.

`DesertTerrainGeneratorEditor.cs`

Dodaje przyciski `Generate Terrain` i `Clear Generated Terrain` w Inspectorze.

`SimpleDesertFpsController.cs`

Bardzo prosty kontroler FPS. Obsluguje chodzenie, skok, myszke i sprawdzanie ziemi.

`DesertHeatHazeOverlay.cs`

Tworzy przed kamera prosty ekranowy overlay z materialem falowania. To daje efekt goracego powietrza.

`DesertSunGlare.cs`

Ustawia prosty obiekt odblasku slonca tak, zeby byl zwrocony do kamery.

`DesertLensFlare.cs`

Buduje kilka krazkow odblasku slonca. To nie jest realistyczny system optyczny, tylko prosty efekt wizualny do sceny pustyni.

`DesertSceneSetup.cs`

Skrypt edytorowy z menu `Pustynja/Setup Desert Demo Scene`. Tworzy materialy, tekstury proceduralne, volume, swiatlo, gracza i scene demo.

## Jak uzyc generatora w Unity

1. Otworz scene `Assets/Scenes/DesertDemo.unity`.
2. Zaznacz obiekt `TerrainGenerator`.
3. W polu `Grayscale Source Image` przypisz teksture w skali szarosci.
4. Ustaw `Terrain Size`, `Height Multiplier`, `Resolution` i `Iso Level`.
5. Wybierz material piasku w polu `Desert Material`.
6. Zostaw wlaczone `Generate Collider`, jesli gracz ma chodzic po terenie.
7. Kliknij `Generate Terrain`.

Mozna tez kliknac prawym przyciskiem na komponencie i wybrac `Generate Terrain`, bo metoda ma `ContextMenu`.

## Jak powstaje mesh

Najpierw generator tworzy siatke punktow 3D. Dla kazdego punktu pyta `DesertScalarField`, jaka jest tam wartosc pola. Potem kazda kostka w siatce jest dzielona na mniejsze czesci. Jesli w jednej czesci sa punkty pelne i puste, to znaczy, ze powierzchnia przechodzi przez srodek.

Program wylicza miejsca przeciecia na krawedziach, uklada je w kolejnosci i dodaje trojkaty do listy. Na koncu Unity dostaje gotowy `Mesh`, a skrypt przelicza normalne, tangenty i granice mesha.

## Material pustyni i srodowisko

Material `DesertSand.mat` uzywa URP Lit. Ma cieply kolor piasku i proceduralna teksture `ProceduralSand.png`, ktora powtarza sie po powierzchni terenu.

Scena ma proceduralny skybox w cieplych kolorach, mocne slonce kierunkowe, mgle w `RenderSettings` i `Hot Desert Global Volume`. Volume dodaje cieply filtr koloru, lekki bloom, winiete, balans bieli i bardzo delikatna aberracje chromatyczna.

## Jak dziala efekt goraca

Efekt goraca sklada sie z dwoch prostych elementow.

Pierwszy to shader `HeatHazeOverlay.shader`. Kamera ma przed soba maly quad, ktory probkuje kolor sceny i lekko przesuwa UV falami sinusoidalnymi. Przez to obraz wyglada, jakby powietrze drgalo od temperatury.

Drugi element to piaskowa atmosfera. Obiekt `Sand Dust Atmosphere` ma `ParticleSystem`, ktory tworzy delikatny pyl. Do tego dochodzi mgla w kolorze piasku.

## Jak dziala kontroler FPS

`SimpleDesertFpsController` uzywa `CharacterController`. Ruch WASD lub strzalkami jest liczony w kierunku kamery/gracza. Myszka obraca gracza w poziomie i kamere w pionie. Spacja wykonuje skok.

Skrypt nie ma sprintu, kucania, broni ani innych systemow. Jest specjalnie uproszczony, zeby pasowal do projektu z generowaniem terenu.

## Jak dziala wykrywanie ziemi

Kontroler nie ufa tylko `CharacterController.isGrounded`. Zamiast tego robi `SphereCast` pod dolna czescia kapsuly. Sprawdza najblizsze stabilne trafienie, ignoruje wlasne collidery i odrzuca zbyt strome powierzchnie.

Jest tez krotki `coyoteTime`. To znaczy, ze po zejsciu z krawedzi gracz ma jeszcze mala chwile na skok. Dzieki temu chodzenie po nierownym mesh'u jest przyjemniejsze.

## Najwazniejsze ustawienia w Inspectorze

`Grayscale Source Image` to obraz, ktory steruje wysokoscia.

`Terrain Size` to rozmiar pustyni w osi X i Z.

`Height Multiplier` to maksymalna wysokosc terenu.

`Resolution` to gestosc voxelowej siatki. Wieksza wartosc daje dokladniejszy mesh, ale generuje wiecej trojkatow.

`Iso Level` przesuwa granice powierzchni. Zwykle zostaje przy `0`.

`Make Edges Tileable` lekko miesza krawedzie obrazu, zeby teren mogl byc kopiowany jako kafle bez bardzo widocznej krawedzi.

`Desert Material` to material przypisany do wygenerowanego mesha.

`Sand Texture` to opcjonalna tekstura piasku.

`Generate Collider` dodaje `MeshCollider`, zeby mozna bylo chodzic po terenie.

`Clear Old Generated Terrain` usuwa poprzedni wygenerowany teren przed stworzeniem nowego.

W komponencie `InfiniteDesertTerrainStreamer` najwazniejsze sa:

`Target` to gracz.

`Target Camera` to kamera gracza.

`Safety Radius` mowi, ile kafli zawsze zostaje wokol gracza.

`Forward Tiles` mowi, jak daleko teren ma sie generowac przed graczem.

`Side Tiles` mowi, jak szeroko teren ma sie generowac po bokach kierunku patrzenia.

## Szybka instrukcja od zera

1. W Unity wybierz menu `Pustynja/Setup Desert Demo Scene`.
2. Otworzy sie i zapisze scena `DesertDemo`.
3. Zaznacz `TerrainGenerator`.
4. Kliknij `Generate Terrain`, jesli chcesz przebudowac teren.
5. Uruchom Play Mode.
6. Kliknij w Game View, zeby zablokowac kursor.
7. Chodzisz klawiszami WASD, skaczesz spacja, patrzysz myszka.

## Najwazniejszy przeplyw kodu

`DesertTerrainGenerator.GenerateTerrain()` sprawdza ustawienia i tworzy `DesertScalarField`.

`DesertScalarField` czyta jasnosc tekstury i odpowiada na pytanie: czy punkt jest pod powierzchnia, czy nad nia.

`DesertMarchingCubes.BuildMesh()` przechodzi po voxelach, znajduje przeciecia z poziomem iso i sklada trojkaty.

Generator tworzy dziecko `Generated Desert Terrain`, dodaje `MeshFilter`, `MeshRenderer`, opcjonalnie `MeshCollider` i przypisuje material.

`InfiniteDesertTerrainStreamer` bierze ten wygenerowany mesh jako wzor. Tworzy z niego kolejne kafle w miejscach, gdzie gracz moze zaraz dojsc albo spojrzec.

Kontroler FPS potem chodzi po colliderach wygenerowanych kafli.
