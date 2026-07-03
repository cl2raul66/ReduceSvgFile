# reducesvg

`reducesvg` es una herramienta de línea de comandos (CLI) rápida, ligera y autónoma escrita en C# (.NET) diseñada para optimizar y reducir al máximo el tamaño de los archivos SVG. 

Es especialmente útil para limpiar los archivos SVG exportados por editores vectoriales como **Inkscape** o **Adobe Illustrator**, eliminando metadatos innecesarios, redondeando coordenadas y eliminando definiciones huérfanas sin alterar la apariencia visual.

---

## Características Principales

*   **Conversión de Estilos**: Transforma reglas CSS inline (`style="..."`) en atributos SVG de presentación directa (como `fill` y `stroke`), reduciendo el tamaño del marcado.
*   **Limpieza de Metadatos de Editores**: Remueve comentarios XML, elementos no estándar y namespaces de editores (`sodipodi:*`, `inkscape:*`, etc.).
*   **Eliminación de Código Muerto (DCE)**: Detecta y elimina IDs no referenciados, así como elementos definidos dentro de `<defs>` (degradados, filtros, máscaras, patrones) que no se utilicen en el documento.
*   **Colapso de Grupos Redundantes**: Disuelve los elementos de grupo `<g>` que queden sin atributos o estilos tras la limpieza, moviendo sus elementos hijos al elemento padre.
*   **Optimización y Redondeo Numérico**: 
    *   Optimiza los datos de rutas (`d`) redondeando las coordenadas decimales a una precisión configurable y removiendo espacios y comas redundantes.
    *   Redondea dimensiones y transformaciones (`transform`, `viewBox`, `points`, `cx`, `cy`, `r`, `width`, `height`, etc.).
*   **Acortamiento de Colores**: Traduce formatos de color largos como `rgb(255, 255, 255)` o `#ffffff` a formatos hexadecimales cortos como `#fff`.
*   **Minificación XML**: Exporta el archivo resultante en una única línea de XML plano, omitiendo declaraciones y sangrías innecesarias.

---

## Compilación e Instalación

Para compilar `reducesvg` como un ejecutable único y autónomo (que incluye el runtime de .NET integrado para que no dependa de librerías externas), ejecuta el siguiente comando en el directorio del proyecto:

```bash
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true
```

Esto generará el ejecutable `ReduceSvgFile.exe` en:
`bin\Release\net10.0\win-x64\publish\`

Puedes copiar este archivo en cualquier directorio de tu sistema agregado al `PATH` (por ejemplo, `C:\Users\tu_usuario\Tools\`) y renombrarlo como `reducesvg.exe` para usarlo de forma global.

---

## Instrucciones de Uso

La herramienta se ejecuta desde la terminal con la siguiente estructura:

```bash
reducesvg <selector_archivo> [opciones]
```

### Ejemplos Prácticos

1.  **Optimizar un único archivo en la misma carpeta**:
    ```bash
    reducesvg archivo.svg
    ```
    *Genera un archivo optimizado llamado `archivo_out.svg` en la misma carpeta.*

2.  **Optimizar todos los SVG de un directorio**:
    ```bash
    reducesvg * -src "C:\Ruta\De\Tus\SVG"
    ```
    *Busca todos los archivos `.svg` en esa carpeta y genera versiones optimizadas añadiendo `_out` (ej. `logo_out.svg`).*

3.  **Establecer un directorio de salida personalizado (sin renombrar archivos)**:
    ```bash
    reducesvg * -src "C:\Origen" -out "C:\Destino"
    ```
    *Optimiza todos los SVG del directorio origen y los guarda en la carpeta destino manteniendo su nombre original (sin el sufijo `_out`).*

4.  **Ajustar la precisión decimal y ver estadísticas de reducción**:
    ```bash
    reducesvg archivo.svg -p 2 -v
    ```
    *Usa una precisión de 2 decimales para las coordenadas y muestra en consola el porcentaje de reducción en bytes obtenido.*

---

## Opciones y Parámetros

| Parámetro | Descripción |
| :--- | :--- |
| `-src <ruta>` | Especifica el directorio de origen (por defecto es el directorio actual). |
| `-out <ruta>` | Especifica el directorio de salida. Los archivos conservarán su nombre original en esta ruta (sin el sufijo `_out`). |
| `-p`, `--precision <num>` | Cambia la precisión de redondeo de coordenadas decimales (por defecto es `3`). |
| `-v`, `--verbose` | Activa el modo detallado en consola para imprimir estadísticas de tamaño antes y después de optimizar. |
| `-h`, `--help` | Muestra el menú de ayuda en la terminal con ejemplos de uso. |

---

## Patrocinio

Si encuentras útil esta herramienta y deseas apoyar su mantenimiento y desarrollo continuo, considera patrocinar el proyecto a través de:

*   **GitHub Sponsors**: [![GitHub Sponsors](https://img.shields.io/badge/Sponsor-GitHub%20Sponsors-ea4aaa?style=flat&logo=github)](https://github.com/sponsors/cl2raul66)

*(¡Cualquier apoyo es enormemente apreciado!)*