# Tutorial: Partículas de impacto + Selección de Skins

Guía paso a paso para dejar funcionando, dentro del editor de Unity, dos
herramientas pensadas para que las use cualquiera del equipo sin tocar
código:

- **Partículas de impacto**: efectos visuales (gol, fallo, pelota caída) que
  se crean y ajustan como un asset, con sliders normales del Inspector.
- **Skins del robot**: crear skins y elegirlas desde una pantalla del menú;
  la textura del robot cambia según lo elegido.

> Unity **6000.5.7f1**, render pipeline **URP**.
> Escenas: `Assets/Scenes/MainMenu.unity` y `Assets/Scenes/Jueguitos.unity`.

Todo lo que sigue es trabajo en el editor de Unity: crear assets y arrastrar
referencias en el Inspector. No hay que escribir ni una línea de código.

---

# PARTE A — Partículas de impacto

## 0. Archivos que ya están en el proyecto

| Archivo | Qué es |
|---|---|
| `Assets/Scripts/VFX/ImpactEffect.cs` | El asset que edita cualquier persona: sprite, color, cantidad, velocidad, tamaño, gravedad, sonido. |
| `Assets/Scripts/VFX/ImpactEffectPlayer.cs` | El motor que arma el efecto en pantalla a partir de esos valores. No hace falta ponerlo en la escena, se crea solo la primera vez que se usa. |

Ya están enganchados en el código en tres momentos del juego: **gol**
(`GoalDetector`), **pelota caída durante los jueguitos** (`Ground`) y
**tiro errado** (`GameFlowManager`). Lo único que falta es crear los assets
de efecto y arrastrarlos a esos tres lugares.

## A1. Crear la carpeta y los assets

1. En la ventana **Project**, clic derecho sobre `Assets` → `Create > Folder`.
   Nombrala **`VFX`**.
2. Adentro de `VFX`, clic derecho → `Create > K.I.C.K > Impact Effect`.
   Nombralo **`FX_Gol`**.
3. Repetí el paso 2 dos veces más:
   - **`FX_Fallo`** (tiro que no entra al arco).
   - **`FX_PelotaCaida`** (se te cae la pelota en los jueguitos).

Cada uno es un asset independiente: lo que cambies en uno no afecta a los
otros.

## A2. Conseguir un sprite para las partículas

Cada efecto necesita un dibujo (PNG con transparencia) que se repite por
cada partícula.

1. Si no tenés arte todavía, un círculo blanco simple con el borde suave
   (tipo "glow") ya funciona bien como partícula genérica — el color y el
   tamaño hacen la mayor parte del trabajo visual.
2. Guardá el/los PNG en `Assets/VFX/Sprites/`.
3. Seleccioná cada imagen en el Project y en el Inspector poné:
   - **Texture Type**: `Sprite (2D and UI)`
   - Aplicá (`Apply`).

Podés usar el mismo sprite para los tres efectos y diferenciarlos solo con
color y cantidad, o uno distinto para cada uno — lo que sea más fácil al
principio.

## A3. Completar los campos de cada asset

Seleccioná **`FX_Gol`** en el Project. En el Inspector vas a ver estos
campos (todos son sliders o casilleros normales, no hace falta saber nada
de partículas):

| Campo | Qué hace | Valor sugerido para el gol |
|---|---|---|
| **Particle Sprite** | el dibujo de cada partícula | tu PNG |
| **Color Over Lifetime** | el color a lo largo de la vida de la partícula (izquierda = recién nace, derecha = a punto de desaparecer) | amarillo/dorado opaco → mismo color transparente |
| **Burst Count** | cuántas partículas salen de una vez | 30 |
| **Spread Angle** | qué tan abierto es el cono de salida (0 = todas para el mismo lado, 180 = como una explosión) | 160 |
| **Speed Range** | velocidad mínima y máxima | 3 – 7 |
| **Size Range** | tamaño mínimo y máximo | 0.2 – 0.5 |
| **Lifetime Range** | cuánto tiempo (segundos) vive cada partícula | 0.4 – 0.9 |
| **Gravity Modifier** | cuánto caen por la gravedad (0 = flotan, 1 = caen como un objeto real) | 0.3 |
| **Start Rotation Random Degrees** | rotación inicial al azar, para que no se vean todas iguales | 180 |
| **Sfx** (opcional) | un sonido que se reproduce junto con el efecto | un "tin" o campanita, si tenés |

Ahora seleccioná **`FX_Fallo`** y **`FX_PelotaCaida`**, y ponéles algo más
discreto (por ejemplo, polvo):

| Campo | Valor sugerido |
|---|---|
| **Color Over Lifetime** | gris/marrón opaco → transparente |
| **Burst Count** | 10-12 |
| **Spread Angle** | 45 (más concentrado, como una salpicadura) |
| **Speed Range** | 1 – 3 |
| **Gravity Modifier** | 0.8 |

## A4. Conectar cada efecto donde tiene que disparar

En la escena **`Jueguitos`**:

1. Buscá en la jerarquía **cada objeto con el componente `Goal Detector`**
   (hay uno por zona del arco). En cada uno, arrastrá **`FX_Gol`** al campo
   **Goal Effect**.
2. Buscá el objeto con el componente **`Ground`** (el piso de la cancha).
   Arrastrá **`FX_PelotaCaida`** al campo **Drop Effect**.
3. Buscá el objeto con el componente **`Game Flow Manager`**. Arrastrá
   **`FX_Fallo`** al campo **Miss Effect**.

Si dejás alguno de estos campos vacío, simplemente no se reproduce nada ahí
— no rompe el juego.

## A5. Probar y ajustar

1. Dale Play en `Jueguitos`.
2. Hacé un gol → tiene que aparecer el estallido de partículas justo cuando
   la pelota entra.
3. Dejá caer la pelota en los jueguitos, o erra un tiro → tiene que
   aparecer el efecto de polvo.

**Lo más cómodo de este sistema**: no hace falta salir de Play para ajustar
un efecto. Mientras el juego corre, seleccioná el asset (por ejemplo
`FX_Gol`) en la ventana de Project y cambiá cualquier valor del Inspector.
El próximo gol ya sale con el valor nuevo, sin reiniciar nada, porque el
efecto se vuelve a armar cada vez que se dispara.

### Problemas comunes (partículas)

| Síntoma | Causa / solución |
|---|---|
| No aparece nada | El campo (`Goal Effect`/`Drop Effect`/`Miss Effect`) quedó vacío, o el asset no tiene `Particle Sprite` asignado. |
| Se ve un cuadrado rosa/magenta | El sprite no está importado como `Sprite (2D and UI)` (paso A2.3). |
| Las partículas son gigantes o minúsculas | Ajustá `Size Range` — son unidades del mundo, probá valores entre 0.1 y 1. |
| Salen para un solo lado en vez de "explotar" | Subí `Spread Angle` (160-180 = todos los lados). |
| Desaparecen de golpe en vez de esfumarse | En `Color Over Lifetime`, el punto de la derecha tiene que tener el alpha (transparencia) en 0. |
| El sonido no se escucha | Revisá que el campo `Sfx` tenga un `AudioClip` asignado y que el volumen de SFX no esté en 0 (menú de audio). |

---

# PARTE B — Selección de Skins

## 0. Archivos que ya están en el proyecto

| Archivo | Qué es |
|---|---|
| `Assets/Scripts/Skins/RobotSkin.cs` | ScriptableObject: **una** skin (textura + color + icono). |
| `Assets/Scripts/Skins/RobotSkinLibrary.cs` | ScriptableObject: la **lista** de todas las skins disponibles. |
| `Assets/Scripts/Skins/PlayerSkinPrefs.cs` | Guarda qué skin eligió el jugador y la comparte entre escenas. |
| `Assets/Scripts/Skins/RobotSkinController.cs` | Va en el robot. Aplica la skin guardada al arrancar. |
| `Assets/Scripts/Skins/SkinSelectionScreen.cs` | La pantalla de selección (arma los botones sola). |

## B1. Preparar las texturas

1. Guardá los PNG/JPG de las skins del cuerpo en `Assets/Skins/Textures/`
   (creá la carpeta si no existe).
2. Seleccioná cada textura y en el Inspector dejá:
   - **Texture Type**: `Default`
   - **sRGB (Color Texture)**: activado
   - Aplicá (`Apply`).
3. Para los **iconos** de los botones (lo que se ve en la pantalla de
   selección) necesitás *Sprites*. Pueden ser los mismos PNG u otros más
   chicos. Seleccionalos y poné:
   - **Texture Type**: `Sprite (2D and UI)`
   - `Apply`.

> `Body Texture` (lo que se ve en el robot) es una **Texture2D**.
> `Preview Icon` (lo que se ve en el botón) es un **Sprite**.

## B2. Crear las skins (assets `RobotSkin`)

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

## B3. Crear la librería (`RobotSkinLibrary`)

1. `Assets > Create > K.I.C.K > Robot Skin Library`.
2. Guardala como `Assets/Skins/RobotSkinLibrary.asset`.
3. En el Inspector, campo **Skins**: subí el tamaño de la lista y arrastrá
   cada asset `RobotSkin`.
4. **El orden importa**: es el orden de los botones en la pantalla y el
   número (índice) que se guarda en disco. La primera de la lista es la
   skin por defecto.

## B4. Poner el `RobotSkinController` en el robot

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

### Probar el cambio en el editor (sin la pantalla todavía)

1. Con el prefab del robot seleccionado, cambiá **Preview Index** (0, 1, 2…)
   en `Robot Skin Controller`.
2. Clic derecho sobre el título del componente > **Aplicar Skin De Preview**.
3. El robot debería cambiar de textura en el Scene View. Si funciona,
   seguimos con la pantalla.

## B5. Armar la pantalla de selección (UI)

En la escena **`MainMenu`**:

1. Si no hay Canvas, `GameObject > UI > Canvas` (se crea el EventSystem
   solo).
2. Dentro del Canvas: `GameObject > Create Empty`, nombralo **`SkinsPanel`**.
   - Ponelo con un `Image` de fondo si querés (`Add Component > Image`).
   - Estirá su `RectTransform` para cubrir la pantalla (selector de anclas,
     opción de las 4 flechitas hacia afuera).
3. Dentro de `SkinsPanel`: `GameObject > Create Empty` → **`SkinButtons`**.
   - `Add Component > Horizontal Layout Group` (o `Grid Layout Group` si vas
     a tener muchas skins).
   - Poné algo de `Spacing` (ej. 20).
4. Dentro de `SkinButtons` creá **UN** botón plantilla:
   - `GameObject > UI > Button - TextMeshPro`, nombralo
     **`SkinButtonTemplate`**.
   - Dentro del botón: `GameObject > UI > Image`, nombrala **exactamente**
     `Icon`.
   - El texto hijo (`Text (TMP)`) podés dejarlo (muestra el `Display Name`)
     o borrarlo.
   - **Importante**: el **Target Graphic** del `Button` tiene que ser la
     imagen de **fondo** del botón, **no** la imagen `Icon` (si no, el
     resaltado del seleccionado tiñe el dibujo).

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

## B6. Configurar `Skin Selection Screen`

1. Seleccioná **`SkinsPanel`** y `Add Component > Skin Selection Screen`.
2. Configuralo:

| Campo | Valor |
|---|---|
| **Library** | `RobotSkinLibrary.asset`. |
| **Button Container** | El objeto `SkinButtons`. |
| **Button Template** | El objeto `SkinButtonTemplate`. |
| **Icon Child Name** | `Icon` (dejalo así). |
| **Selected Color** | Color del botón elegido (ej. amarillo). |
| **Normal Color** | Color de los no elegidos (ej. blanco). |
| **Live Preview** | El `RobotSkinController` del robot que se ve en el menú (opcional, para ver el cambio al instante). |

3. Dejá `SkinsPanel` **desactivado** (checkbox arriba del Inspector), para
   que arranque oculto.

## B7. Botón "Skins" en el menú + botón "Volver"

1. En el menú principal, creá un botón `GameObject > UI > Button -
   TextMeshPro`, texto **"Skins"**.
2. En su componente `Button`, sección **On Click ()**: `+`
   - Objeto: arrastrá **`SkinsPanel`**.
   - Función: `SkinSelectionScreen > Open ()`.
3. En el botón **`BackButton`** de adentro del panel, **On Click ()**: `+`
   - Objeto: **`SkinsPanel`**.
   - Función: `SkinSelectionScreen > Close ()`.

## B8. Probar

1. Play en `MainMenu`.
2. Click en **Skins** → aparece un botón por skin, con su icono y nombre.
3. Click en una skin → se resalta y (si asignaste `Live Preview`) el robot
   cambia.
4. **Volver** → se cierra el panel.
5. Play en `Jueguitos` → el robot arranca con la skin elegida.
6. Parás Play, volvés a entrar → la elección se mantiene (queda guardada).

### Problemas comunes (skins)

| Síntoma | Causa / solución |
|---|---|
| No aparece ningún botón | Falta asignar `Library`, `Button Container` o `Button Template` en `Skin Selection Screen` (mirá la Console). |
| Los botones aparecen pero sin icono | La Image hija no se llama `Icon`, o la skin no tiene `Preview Icon` asignado. |
| El robot no cambia en el juego | El robot de `Jueguitos` no tiene `Robot Skin Controller`, o le falta la `Library`. |
| Cambia de color pero no de textura | La textura no está asignada en `Body Texture`, o el material del robot no usa `_BaseMap` (URP Lit). Probá una skin con `Override Material`. |
| Se pinta algo que no es el cuerpo | Llená `Target Renderers` a mano solo con los renderers del cuerpo. |
| El icono se ve teñido de amarillo al seleccionar | El `Target Graphic` del Button es la Image `Icon`; cambialo a la Image de fondo del botón. |

---

# Reset rápido (para testear desde cero)

- **Skins elegida**: `PlayerPrefs.DeleteKey("Robot_SkinIndex")`.
- **Todo junto**: en Unity, menú `Edit > Clear All PlayerPrefs` — borra
  también el volumen de audio, el puntaje más alto y la tabla de
  posiciones, así que usalo solo si querés arrancar de cero con todo.

# Checklist final

**Partículas**
- [ ] Carpeta `Assets/VFX/` creada, con al menos un sprite importado como Sprite.
- [ ] 3 assets `Impact Effect` creados (`FX_Gol`, `FX_Fallo`, `FX_PelotaCaida`).
- [ ] Cada uno con `Particle Sprite` y `Color Over Lifetime` configurados.
- [ ] `FX_Gol` asignado en cada `Goal Detector` del arco.
- [ ] `FX_PelotaCaida` asignado en `Ground`.
- [ ] `FX_Fallo` asignado en `Game Flow Manager`.

**Skins**
- [ ] Texturas importadas (Body como Default, iconos como Sprite).
- [ ] ≥ 2 assets `Robot Skin` creados.
- [ ] `RobotSkinLibrary.asset` con todas las skins en orden.
- [ ] `Robot Skin Controller` en `Player.prefab` con la Library asignada.
- [ ] `SkinsPanel` con `SkinButtons` (Layout) + `SkinButtonTemplate` (con `Icon`).
- [ ] `Skin Selection Screen` configurado; panel desactivado por defecto.
- [ ] Botón "Skins" → `Open()`, botón "Volver" → `Close()`.
