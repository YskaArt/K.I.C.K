# Tutorial: Scoreboard + Selección de Skins

Guía paso a paso para dejar funcionando, dentro del editor de Unity, dos
sistemas que ya tienen todo el código escrito:

- **Scoreboard**: tabla de los 10 mejores puntajes, accesible desde el menú.
- **Skins del robot**: crear skins y elegirlas desde una pantalla del menú;
  la textura del robot cambia según lo elegido.

> Unity **6000.5.7f1**, render pipeline **URP**.
> Escenas: `Assets/Scenes/MainMenu.unity` y `Assets/Scenes/Jueguitos.unity`.

Todo lo que sigue es trabajo en el editor: crear GameObjects de UI y arrastrar
referencias en el Inspector. No hay que tocar código.

---

## 0. Archivos que ya están en el proyecto

Skins (`Assets/Scripts/Skins/`):

| Archivo | Qué es |
|---|---|
| `RobotSkin.cs` | ScriptableObject: **una** skin (textura + color + icono). |
| `RobotSkinLibrary.cs` | ScriptableObject: la **lista** de skins. |
| `PlayerSkinPrefs.cs` | Guarda el índice elegido (`PlayerPrefs`) y lo comparte entre escenas. |
| `RobotSkinController.cs` | Va en el robot. Aplica la skin guardada. |
| `SkinSelectionScreen.cs` | La pantalla de selección (arma los botones sola). |

Scoreboard (`Assets/Scripts/`):

| Archivo | Qué es |
|---|---|
| `Scoreboard.cs` | Clase estática: guarda/ordena el top 10 en `PlayerPrefs`. |
| `ScoreboardScreen.cs` | La pantalla del menú (arma las filas sola). |
| `GameManager.cs` | Ya modificado: al terminar la partida llama `Scoreboard.Submit(score)`. |

Si Unity muestra errores de compilación, resolvelos antes de seguir (Console
sin errores rojos).

---

# PARTE A — Sistema de Skins

## A1. Preparar las texturas

1. Meté los PNG/JPG de las skins del cuerpo en `Assets/Skins/Textures/`
   (creá la carpeta si no existe).
2. Seleccioná cada textura y en el Inspector dejá:
   - **Texture Type**: `Default`
   - **sRGB (Color Texture)**: activado
   - Aplicá (`Apply`).
3. Para los **iconos** de los botones necesitás *Sprites*. Pueden ser los
   mismos PNG u otros más chicos. Seleccionalos y poné:
   - **Texture Type**: `Sprite (2D and UI)`
   - `Apply`.

> `Body Texture` (lo que se ve en el robot) es una **Texture2D**.
> `Preview Icon` (lo que se ve en el botón) es un **Sprite**. Pueden salir del
> mismo archivo si lo importás como Sprite, o de dos archivos distintos.

## A2. Crear las skins (assets `RobotSkin`)

Por cada skin:

1. `Assets > Create > K.I.C.K > Robot Skin`.
2. Guardala en `Assets/Skins/` con un nombre claro (`Skin_Rojo`, `Skin_Oro`…).
3. Completá en el Inspector:

| Campo | Valor |
|---|---|
| **Display Name** | Nombre visible (`Rojo`, `Oro`, `Camuflaje`…). |
| **Preview Icon** | El Sprite del icono (opcional pero recomendado). |
| **Body Texture** | La Texture2D que se aplica al robot. |
| **Tint** | Color multiplicador. Dejalo **blanco** para no alterar la textura. |
| **Override Material** | Dejar **desactivado** (uso normal). |
| **Custom Material** | Vacío (solo si activás Override Material). |

> **Skin de solo color**: dejá `Body Texture` vacío y cambiá `Tint`.
> **Skin con material propio** (otro shader, metalizado, etc.): activá
> `Override Material` y asigná `Custom Material`.

Hacé al menos **2 skins** para poder probar el cambio.

## A3. Crear la librería (`RobotSkinLibrary`)

1. `Assets > Create > K.I.C.K > Robot Skin Library`.
2. Guardala como `Assets/Skins/RobotSkinLibrary.asset`.
3. En el Inspector, campo **Skins**: subí el tamaño de la lista y arrastrá
   cada asset `RobotSkin`.
4. **El orden importa**: es el orden de los botones en la pantalla y el número
   (índice) que se guarda en disco. La primera de la lista es la skin por
   defecto (índice 0).

## A4. Poner el `RobotSkinController` en el robot

El robot es el prefab `Assets/Animations/Player.prefab` (se usa tanto en el
menú como en el juego).

1. Doble clic en `Assets/Animations/Player.prefab` para abrirlo en modo prefab.
2. Seleccioná el GameObject raíz del prefab.
3. `Add Component > Robot Skin Controller`.
4. Configuralo:

| Campo | Valor |
|---|---|
| **Library** | Arrastrá `RobotSkinLibrary.asset`. |
| **Target Renderers** | **Dejar vacío** → agarra todos los `Renderer` hijos del robot. |
| **Apply Saved On Start** | Activado. |
| **Preview Index** | 0 (solo se usa para previsualizar en el editor). |

5. Guardá el prefab (`Ctrl+S` o salir del modo prefab).

> Si en alguna escena el robot **no** es ese prefab, repetí los pasos 2–4
> sobre ese objeto en la escena.
>
> Si al cambiar de skin se pintan cosas que no son el cuerpo (un trail, un
> efecto), llená `Target Renderers` a mano solo con los `Renderer` del cuerpo.

## A5. Probar el cambio en el editor (sin UI todavía)

1. Abrí el prefab del robot o ponelo en una escena.
2. En el `RobotSkinController`, cambiá **Preview Index** (0, 1, 2…).
3. Clic derecho sobre el título del componente `RobotSkinController` >
   **Aplicar Skin De Preview**.
4. El robot debería cambiar de textura en el Scene View.

Si funciona, seguimos con la pantalla.

## A6. Armar la pantalla de selección (UI)

En la escena **`MainMenu`**:

1. Si no hay Canvas, `GameObject > UI > Canvas` (y se crea el EventSystem).
2. Dentro del Canvas: `GameObject > Create Empty`, nombralo **`SkinsPanel`**.
   - Ponelo con un `Image` de fondo si querés (`Add Component > Image`).
   - Estirá su `RectTransform` para cubrir la pantalla (anchors *stretch*).
3. Dentro de `SkinsPanel`: `GameObject > Create Empty` → **`SkinButtons`**.
   - `Add Component > Horizontal Layout Group` (o `Grid Layout Group` si vas a
     tener muchas).
   - En el Layout Group activá `Child Force Expand` según te guste; poné algo
     de `Spacing` (ej. 20).
   - Opcional: `Add Component > Content Size Fitter`.
4. Dentro de `SkinButtons` creá **UN** botón plantilla:
   - `GameObject > UI > Button - TextMeshPro`, nombralo **`SkinButtonTemplate`**.
   - Dentro del botón: `GameObject > UI > Image`, nombrala **exactamente**
     `Icon`. Centrala y dale el tamaño que quieras (ej. 120×120).
   - El texto hijo (`Text (TMP)`) podés dejarlo (mostrará el `Display Name`) o
     borrarlo.
   - **Importante**: el **Target Graphic** del `Button` debe ser la imagen de
     **fondo** del botón, **no** la imagen `Icon`. (Si el target graphic fuera
     el icono, el resaltado del seleccionado te teñiría el dibujo.)

Jerarquía esperada:

```
Canvas
└─ SkinsPanel            (Image de fondo + SkinSelectionScreen)
   ├─ SkinButtons        (Horizontal/Grid Layout Group)
   │  └─ SkinButtonTemplate   (Button)
   │     ├─ Icon              (Image)   ← nombre EXACTO "Icon"
   │     └─ Text (TMP)        (opcional)
   └─ BackButton         (Button)  → cierra el panel
```

## A7. Configurar `SkinSelectionScreen`

1. Seleccioná **`SkinsPanel`** y `Add Component > Skin Selection Screen`.
2. Configuralo:

| Campo | Valor |
|---|---|
| **Library** | `RobotSkinLibrary.asset`. |
| **Button Container** | El objeto `SkinButtons`. |
| **Button Template** | El objeto `SkinButtonTemplate`. |
| **Icon Child Name** | `Icon` (dejalo así si nombraste la Image `Icon`). |
| **Selected Color** | Color del botón elegido (ej. amarillo). |
| **Normal Color** | Color de los no elegidos (ej. blanco). |
| **Live Preview** | El `RobotSkinController` del robot que se ve en el menú (opcional, para ver el cambio al instante). |

3. Dejá `SkinsPanel` **desactivado** en la jerarquía (checkbox arriba del
   Inspector), para que arranque oculto. Se prende con el botón del menú.

> Al activarse el panel, `SkinSelectionScreen` clona `SkinButtonTemplate` una
> vez por skin, le pone el icono y el nombre, y engancha el click. La plantilla
> original queda oculta automáticamente.

## A8. Botón "Skins" en el menú + botón "Volver"

1. En el menú principal, al lado de *Play*/*Exit*, creá un botón
   `GameObject > UI > Button - TextMeshPro`, texto **"Skins"**.
2. En su componente `Button`, sección **On Click ()**: `+`
   - Objeto: arrastrá **`SkinsPanel`**.
   - Función: `SkinSelectionScreen > Open ()`.
3. En el botón **`BackButton`** de adentro del panel, **On Click ()**: `+`
   - Objeto: **`SkinsPanel`**.
   - Función: `SkinSelectionScreen > Close ()`.

## A9. Probar

1. Play en `MainMenu`.
2. Click en **Skins** → aparece un botón por skin.
3. Click en una skin → se resalta y (si hay `Live Preview`) el robot cambia.
4. **Volver** → cerrás el panel.
5. Play en `Jueguitos` → el robot arranca con la skin elegida.
6. Parás Play, volvés a entrar → la elección se mantiene (está en `PlayerPrefs`).

### Problemas comunes (skins)

| Síntoma | Causa / solución |
|---|---|
| No aparece ningún botón | Falta asignar `Library`, `Button Container` o `Button Template` en `SkinSelectionScreen` (mirá la Console). |
| Los botones aparecen pero sin icono | La Image hija no se llama `Icon`, o `Icon Child Name` no coincide, o la skin no tiene `Preview Icon`. |
| El robot no cambia en el juego | El robot de `Jueguitos` no tiene `RobotSkinController`, o le falta la `Library`. |
| Cambia de color pero no de textura | La textura no está asignada en `Body Texture`, o el material del robot no usa `_BaseMap` (URP Lit). Probá una skin con `Override Material`. |
| Se pinta algo que no es el cuerpo | Llená `Target Renderers` a mano solo con los renderers del cuerpo. |
| El icono se ve teñido de amarillo al seleccionar | El `Target Graphic` del Button es la Image `Icon`; cambialo a la Image de fondo del botón. |

---

# PARTE B — Scoreboard

## B1. Verificar el registro de puntajes

Ya está hecho en `GameManager.GameOver()`: llama `Scoreboard.Submit(score)`.
No hay que tocar nada acá, salvo el paso opcional B6.

## B2. Armar el panel de puntajes (UI)

En la escena **`MainMenu`**:

1. Dentro del Canvas: `GameObject > Create Empty` → **`ScoreboardPanel`**
   (con `Image` de fondo, `RectTransform` en *stretch*).
2. Dentro: un título `GameObject > UI > Text - TextMeshPro` con "MEJORES PUNTAJES".
3. Dentro: `GameObject > Create Empty` → **`ScoreRows`**
   - `Add Component > Vertical Layout Group` (poné `Spacing` ~8,
     `Child Force Expand > Width` activado).
   - Opcional: `Add Component > Content Size Fitter` (`Vertical: Preferred`).
4. Dentro de `ScoreRows` creá **UNA** fila plantilla:
   - `GameObject > Create Empty` → **`ScoreRowTemplate`**, con
     `Add Component > Horizontal Layout Group`.
   - Adentro, 3 textos `GameObject > UI > Text - TextMeshPro` nombrados
     **exactamente**: `Rank`, `Score`, `Date`.
5. Opcional: un texto **`EmptyMessage`** ("Sin puntajes todavía") como hijo del
   panel, fuera de `ScoreRows`.
6. Opcional: un botón **`ClearButton`** ("Borrar").
7. Un botón **`BackButton`** ("Volver").

Jerarquía esperada:

```
Canvas
└─ ScoreboardPanel        (Image + ScoreboardScreen)
   ├─ Title (TMP)
   ├─ ScoreRows           (Vertical Layout Group)
   │  └─ ScoreRowTemplate (Horizontal Layout Group)
   │     ├─ Rank  (TMP)   ← nombres EXACTOS
   │     ├─ Score (TMP)
   │     └─ Date  (TMP)
   ├─ EmptyMessage (TMP)  (opcional)
   ├─ ClearButton         (opcional)
   └─ BackButton
```

> Si te da fiaca hacer 3 textos: dejá **un solo** `TMP Text` dentro de
> `ScoreRowTemplate` con cualquier nombre. El script detecta que no hay
> `Rank`/`Score`/`Date` y escribe todo en una línea (`" 1.     1200    09/09/2026"`).

## B3. Configurar `ScoreboardScreen`

1. Seleccioná **`ScoreboardPanel`** y `Add Component > Scoreboard Screen`.
2. Configuralo:

| Campo | Valor |
|---|---|
| **Row Container** | El objeto `ScoreRows`. |
| **Row Template** | El objeto `ScoreRowTemplate`. |
| **Empty Message** | El texto `EmptyMessage` (opcional). |
| **Clear Button** | El botón `ClearButton` (opcional). |
| **Date Format** | `dd/MM/yyyy` (o `dd/MM HH:mm`, lo que prefieras). |

3. Dejá `ScoreboardPanel` **desactivado** en la jerarquía.

## B4. Botón "Puntajes" en el menú + "Volver"

1. Botón `GameObject > UI > Button - TextMeshPro`, texto **"Puntajes"**.
2. **On Click ()**: objeto `ScoreboardPanel`, función
   `ScoreboardScreen > Open ()`.
3. Botón `BackButton` dentro del panel → **On Click ()**: objeto
   `ScoreboardPanel`, función `ScoreboardScreen > Close ()`.
4. (Si pusiste `ClearButton`) no hace falta enganchar nada: `ScoreboardScreen`
   ya lo conecta solo a `Scoreboard.Clear()`.

## B5. (Opcional) Mostrar el puesto en el Game Over

En la escena **`Jueguitos`**:

1. En el panel de Game Over agregá un `TMP Text` (ej. debajo del score final).
2. Seleccioná el objeto con el componente **`GameManager`**.
3. Arrastrá ese texto al campo **Rank Text**.
4. Al perder mostrará `Puesto #N` si el puntaje entró al top 10, o nada si no.

## B6. Probar

1. Jugá una partida completa en `Jueguitos` hasta el Game Over.
2. Volvé al menú (`MainMenu`).
3. **Puntajes** → tiene que aparecer la fila con tu puntaje y la fecha.
4. Jugá otra con más/menos puntos → se reordena, quedan máximo 10.
5. **Borrar** (si lo pusiste) → queda vacío y aparece `EmptyMessage`.

### Problemas comunes (scoreboard)

| Síntoma | Causa / solución |
|---|---|
| No aparece ninguna fila | Falta `Row Container` o `Row Template` en `ScoreboardScreen` (Console). O nunca se registró un puntaje > 0. |
| Aparece la fila pero sin datos | Los textos hijos no se llaman **exactamente** `Rank`, `Score`, `Date`. |
| La plantilla se ve siempre (fila vacía de más) | Es normal si la dejaste activa; el script la desactiva al abrir el panel. Dejala como hijo de `Row Container`. |
| El puntaje no se guarda | La partida no llega a `GameManager.GameOver()`, o `score` es 0 (`Submit` ignora ≤ 0). |
| Se ve `01/01/0001` | La fecha guardada es inválida; borrá la tabla con `ClearButton` y volvé a jugar. |

---

# Cómo funciona por dentro (resumen)

### Skins
- `PlayerSkinPrefs.SelectedIndex` guarda el índice en `PlayerPrefs`
  (clave `Robot_SkinIndex`) y dispara el evento `Changed`.
- `SkinSelectionScreen.Select(i)` setea ese índice.
- `RobotSkinController` escucha `Changed` y, en `Start`, aplica la skin
  guardada. Cambia la textura con un `MaterialPropertyBlock`
  (`_BaseMap` / `_BaseColor`), así **no crea instancias de material** ni
  modifica los assets. Con `Override Material` sí reemplaza el material.

### Scoreboard
- `Scoreboard` guarda una lista JSON en `PlayerPrefs`
  (clave `Scoreboard_v1`), ordenada de mayor a menor, recortada a 10.
- `Submit(score)` inserta, ordena, recorta y devuelve el puesto (o -1).
- `ScoreboardScreen` se reconstruye en `OnEnable` y cada vez que
  `Scoreboard.Changed` se dispara.

### Reset manual de datos (para testear)
- Skins: `PlayerPrefs.DeleteKey("Robot_SkinIndex")`.
- Scoreboard: botón `Borrar`, o `PlayerPrefs.DeleteKey("Scoreboard_v1")`.
- Todo junto: `Edit > Clear All PlayerPrefs` (menú de Unity) borra **todo**
  (incluye volumen de audio, high score, etc.).

---

# Checklist final

**Skins**
- [ ] Texturas importadas (Body como Default, iconos como Sprite).
- [ ] ≥ 2 assets `RobotSkin` creados.
- [ ] `RobotSkinLibrary.asset` con todas las skins en orden.
- [ ] `RobotSkinController` en `Player.prefab` con la Library asignada.
- [ ] `SkinsPanel` con `SkinButtons` (Layout) + `SkinButtonTemplate` (con `Icon`).
- [ ] `SkinSelectionScreen` configurado; panel desactivado por defecto.
- [ ] Botón "Skins" → `Open()`, botón "Volver" → `Close()`.

**Scoreboard**
- [ ] `ScoreboardPanel` con `ScoreRows` (Layout) + `ScoreRowTemplate` (`Rank`/`Score`/`Date`).
- [ ] `ScoreboardScreen` configurado; panel desactivado por defecto.
- [ ] Botón "Puntajes" → `Open()`, botón "Volver" → `Close()`.
- [ ] (Opcional) `Rank Text` asignado en `GameManager`.
- [ ] (Opcional) `ClearButton` asignado.
