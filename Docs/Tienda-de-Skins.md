# Tutorial: Tienda de Skins (con puntos acumulados)

Guía paso a paso para dejar funcionando, dentro del editor de Unity, la
tienda donde el jugador compra skins del robot con los puntos que va
acumulando al jugar. Al final hay una sección con **ideas de skins**
inspiradas en el mundo del fútbol, para el artista 2D.

> Unity **6000.5.7f1**, render pipeline **URP**.
> Escena: `Assets/Scenes/MainMenu.unity`.

Todo lo que sigue es trabajo en el editor: crear archivos (assets) y
objetos de UI, y arrastrar referencias en el Inspector. No hay que escribir
código.

---

## 0. Cómo funciona (para entender qué estamos armando)

- Cada vez que **termina una partida** (ganes o pierdas), el puntaje de esa
  ronda se suma a una **billetera persistente** (`PlayerWallet`). Esto es
  distinto del *high score* o la tabla de posiciones: ahí se guarda la
  *mejor* partida; la billetera suma *todas*.
- Cada skin (`RobotSkin`) tiene un campo **Price**. Si es `0`, está
  disponible siempre. Si tiene un número, aparece **bloqueada** (con un
  candado) hasta que el jugador la compra.
- Al tocar una skin bloqueada, si hay saldo suficiente, se descuenta de la
  billetera y la skin queda **desbloqueada para siempre** (no hay que
  volver a comprarla).
- Todo esto ya está programado. Lo que falta es **armar la pantalla** en
  el editor y **crear los archivos de cada skin**.

Archivos de código ya existentes (no hay que tocarlos, solo usarlos desde
el Inspector):

| Archivo | Qué hace |
|---|---|
| `Assets/Scripts/Skins/RobotSkin.cs` | Un archivo = una skin (textura, color, precio, icono). |
| `Assets/Scripts/Skins/RobotSkinLibrary.cs` | La lista de todas las skins disponibles, en orden. |
| `Assets/Scripts/Skins/PlayerWallet.cs` | La billetera de puntos acumulados. |
| `Assets/Scripts/Skins/SkinUnlocks.cs` | Recuerda qué skins ya se compraron. |
| `Assets/Scripts/Skins/PlayerSkinPrefs.cs` | Recuerda cuál skin está elegida ahora. |
| `Assets/Scripts/Skins/RobotSkinController.cs` | Va en el robot: aplica la skin elegida. |
| `Assets/Scripts/Skins/SkinSelectionScreen.cs` | La pantalla/tienda: arma los botones sola. |

---

## 1. Crear las skins (son como "fichas" con los datos de cada color)

1. En la ventana **Project**, clic derecho sobre `Assets` → `Create >
   Folder`. Nombrala **`Skins`**.
2. Doble clic para entrar. Clic derecho adentro → `Create > K.I.C.K > Robot
   Skin Library`. Nombrala **`RobotSkinLibrary`**.
3. Clic derecho de nuevo → `Create > K.I.C.K > Robot Skin`. Repetí esto una
   vez por cada skin que quieras (ver la sección de ideas más abajo para
   nombres y colores).

### Completar cada skin

Seleccioná el archivo de la skin y completá en el Inspector:

| Campo | Qué es |
|---|---|
| **Display Name** | El nombre que ve el jugador (ej. "Local Clásico"). |
| **Price** | Costo en puntos. `0` = gratis desde el principio. |
| **Tint** | Color que tiñe al robot (si no tenés una textura todavía, usá solo esto). |
| **Body Texture** | Una imagen (PNG) para un diseño más elaborado (rayas, parches). Opcional. |
| **Preview Icon** | Un sprite chico para el botón de la tienda. Opcional. |

### Cargar las skins en la librería

4. Clic en `RobotSkinLibrary`. En el campo **Skins**, poné el tamaño (la
   cantidad de skins que hiciste) y arrastrá cada archivo de skin a su
   casillero, en el orden en que querés que aparezcan los botones.
   **La skin en la posición 0 es la que usa el robot por defecto**, así
   que conviene que sea una gratuita.

---

## 2. Controller en el robot (una sola vez)

1. Abrí `Assets/Animations/Player.prefab` (doble clic).
2. Seleccioná el objeto raíz del prefab.
3. `Add Component > Robot Skin Controller`.
4. **Library** → arrastrá `RobotSkinLibrary`. Dejá `Target Renderers` vacío.
5. Guardá (`Ctrl+S`) y salí del modo prefab (flecha `<` arriba de la Hierarchy).

---

## 3. Armar el panel de la tienda (escena `MainMenu`)

### 3.1 — El panel

1. Clic derecho sobre `Canvas` → `UI > Image`. Renombralo **`SkinsPanel`**.
2. Rect Transform → selector de anclas (el cuadradito con líneas, arriba a
   la izquierda del bloque) → elegí la opción que estira a toda la
   pantalla (4 flechas hacia afuera).

### 3.2 — El texto del saldo

3. Clic derecho sobre `SkinsPanel` → `UI > Text - TextMeshPro`. Renombralo
   **`BalanceText`**.

### 3.3 — La fila de botones

4. Clic derecho sobre `SkinsPanel` → `Create Empty`. Renombralo
   **`SkinButtons`**.
5. `Add Component > Horizontal Layout Group` (o `Grid Layout Group` si van
   a ser muchas skins).

### 3.4 — El botón plantilla

6. Clic derecho sobre `SkinButtons` → `UI > Button - TextMeshPro`.
   Renombralo **`SkinButtonTemplate`**.
7. Adentro de ese botón, agregá:
   - Una `Image` llamada **exactamente** `Icon`.
   - Un `Text - TextMeshPro` llamado **exactamente** `Price`.
   - Una `Image` llamada **exactamente** `Lock` (un overlay oscuro o un
     candado, lo que prefieran — se prende solo cuando la skin está
     bloqueada).
8. En el componente **Button** del propio `SkinButtonTemplate`, el campo
   **Target Graphic** tiene que apuntar a la Image de **fondo** del botón
   (no a `Icon` ni a `Lock`).

### 3.5 — Botón para volver

9. Clic derecho sobre `SkinsPanel` → `UI > Button - TextMeshPro`.
   Renombralo **`BackButton`**, texto "Volver".

Jerarquía final:

```
Canvas
├─ SkinsButton            (Button, en el menu principal)
└─ SkinsPanel
   ├─ BalanceText
   ├─ SkinButtons
   │  └─ SkinButtonTemplate
   │     ├─ Icon
   │     ├─ Text (TMP)
   │     ├─ Price
   │     └─ Lock
   └─ BackButton
```

---

## 4. Conectar todo

1. Clic en `SkinsPanel` → `Add Component > Skin Selection Screen`.
2. Completá:

| Campo | Valor |
|---|---|
| **Library** | `RobotSkinLibrary` |
| **Button Container** | `SkinButtons` |
| **Button Template** | `SkinButtonTemplate` |
| **Balance Text** | `BalanceText` |
| **Live Preview** | (opcional) el `Robot Skin Controller` del robot visible en el menú |

   Los campos `Icon Child Name` / `Price Child Name` / `Lock Child Name`
   no hace falta tocarlos: ya vienen con los nombres correctos.
3. Destildá el checkbox de arriba del Inspector de `SkinsPanel`, para que
   arranque oculto.

## 5. Botones de entrada y salida

- Si no existe, creá un botón **`SkinsButton`** en el menú (`Canvas > UI >
  Button - TextMeshPro`, texto "Skins").
- `SkinsButton` → **On Click ()** → `+` → arrastrá `SkinsPanel` → elegí
  `Skin Selection Screen > Open ()`.
- `BackButton` (el de adentro del panel) → **On Click ()** → `+` →
  arrastrá `SkinsPanel` → elegí `Skin Selection Screen > Close ()`.

---

## 6. Probar

1. Play en `MainMenu` → "Skins": las skins con `Price` en `0` se ven
   disponibles; las demás, con el candado puesto y el número de precio.
2. Jugá una partida en `Jueguitos` hasta perder. Volvé al menú → el
   `BalanceText` debería haber subido.
3. Tocá una skin bloqueada: si el saldo alcanza, se desbloquea, se
   descuenta y queda seleccionada.
4. Entrá a `Jueguitos` de nuevo: el robot tiene que aparecer con la skin
   elegida.

### Reset rápido (para testear desde cero)

- `PlayerPrefs.DeleteKey("Player_Points")` — borra el saldo.
- `PlayerPrefs.DeleteKey("Robot_UnlockedSkins")` — vuelve a bloquear todo.
- `PlayerPrefs.DeleteKey("Robot_SkinIndex")` — borra la skin elegida.
- O `Edit > Clear All PlayerPrefs` para borrar absolutamente todo (incluye
  volumen, puntajes, tutorial).

### Problemas comunes

| Síntoma | Causa / solución |
|---|---|
| No aparece ningún botón | Falta `Library`, `Button Container` o `Button Template` en `Skin Selection Screen` (mirá la Console). |
| El precio o el candado no se ven | Los hijos del botón no se llaman exactamente `Price` / `Lock`. |
| El candado se ve pero nunca se compra nada | Revisá que `PlayerWallet.Balance` tenga puntos — jugá una partida completa hasta el Game Over. |
| El saldo no sube | `GameManager.GameOver()` es lo que llama a `PlayerWallet.Add(score)` — confirmá que esa escena use ese `GameManager`. |
| El candado se ve manchado de amarillo al seleccionar | El `Target Graphic` del botón apunta a `Lock` o `Icon` en vez del fondo. |

---

# Ideas de skins inspiradas en el fútbol

Para no meternos en problemas de marca registrada, la idea **no es copiar
el escudo o el nombre de un club real**, sino usar **arquetipos clásicos
del uniforme de fútbol** como punto de partida creativo. Cada una de estas
es una combinación de colores + un patrón, con un nombre propio del juego.

Todas se pueden lograr solo con **Tint** (color liso) para arrancar, y
después mejorarse con una **Body Texture** (rayas, bastones, parches) para
la versión final.

## Camisetas "titular / suplente" (las más baratas, primera tanda)

| Nombre sugerido | Colores | Patrón (si hay textura) | Precio sugerido |
|---|---|---|---|
| **Local Clásico** | Blanco + un color fuerte de acento (franja al pecho) | Lisa con una franja diagonal | Gratis (posición 0) |
| **Visitante Oscuro** | Negro o azul marino + detalles en un color vivo | Lisa, logo/numero simple | Bajo |
| **Suplente Flúo** | Un color flúo (verde lima, naranja, amarillo) de cuerpo entero | Lisa | Bajo-medio |

## Estilos "a rayas / a bastones" (clásicos del fútbol sudamericano y europeo)

| Nombre sugerido | Colores | Patrón |
|---|---|---|
| **Rayado Celeste** | Celeste y blanco | Bastones verticales anchos |
| **Rayado Rojinegro** | Rojo y negro | Mitad y mitad (una manga de cada color) |
| **Franjas Doradas** | Blanco con una franja horizontal dorada/negra al pecho | Franja diagonal tipo banda presidencial |

## Arquero (colores que tradicionalmente NO se repiten con la cancha)

| Nombre sugerido | Colores |
|---|---|
| **Guante de Oro** | Amarillo flúo + negro |
| **Arquero Verde** | Verde botella + blanco |

## Selecciones / "seleccionado nacional" (inspiración en combinaciones típicas, sin usar escudos)

| Nombre sugerido | Colores |
|---|---|
| **Albiceleste** | Celeste y blanco a bastones + short negro |
| **Verdeamarela** | Amarillo + detalles verdes y azules |
| **Azzurra** | Azul liso de cuerpo entero |
| **Tricolor** | Verde, blanco y rojo en bloques |

## Retro / edición especial (para precios altos, de "coleccionista")

| Nombre sugerido | Idea |
|---|---|
| **Retro 90s** | Colores pastel + un patrón geométrico tipo años 90 (rombos, manchas) |
| **Edición Dorada** | El robot en un único color metálico dorado (usar `Override Material` con un material brillante en vez de solo `Tint`) |
| **Edición Campeón** | Una banda o estrellas doradas sobre el diseño "Local Clásico", como homenaje a las camisetas de campeón |

## Sugerencia de progresión de precios

Para que la tienda tenga sentido de progresión (barato al principio, caro
y aspiracional después):

| Categoría | Rango de precio sugerido |
|---|---|
| Titular / Suplente básicas | Gratis a 300 |
| Rayadas / Selecciones | 500 a 1200 |
| Arquero | 400 a 800 |
| Retro / Edición especial | 2000+ (la recompensa de largo plazo) |

Esto da una curva natural: con las primeras 2-3 partidas ya se desbloquea
algo, y las ediciones especiales quedan como objetivo para jugadores que
vuelven varias veces — exactamente el tipo de gancho de retención que
buscábamos sin usar dinero real.
