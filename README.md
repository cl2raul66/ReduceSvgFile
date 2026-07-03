# reducesvg

`reducesvg` is a fast, lightweight, and self-contained C# command-line tool (CLI) designed to optimize and minify SVG files.

It is particularly useful for cleaning up SVG files exported by vector editors like **Inkscape** or **Adobe Illustrator** by stripping out unnecessary metadata, rounding coordinate precision, and removing orphaned definitions without altering visual rendering.

---

## Key Features

*   **Style-to-Attribute Conversion**: Converts inline CSS styles (`style="..."`) into direct SVG presentation attributes (such as `fill` and `stroke`), reducing overall markup size.
*   **Editor Metadata Cleanup**: Removes XML comments, non-standard editor elements, and namespaces (`sodipodi:*`, `inkscape:*`, etc.).
*   **Dead Code Elimination (DCE)**: Detects and removes unused element IDs as well as unused definitions in `<defs>` (gradients, filters, masks, patterns) that are not referenced in the document.
*   **Group Collapsing**: Dissolves redundant `<g>` groups that have no attributes or styles after cleanup, promoting child nodes to their parent element.
*   **Path & Numeric Coordinate Rounding**:
    *   Optimizes path data (`d`) by rounding decimal coordinates to a configurable precision and removing redundant spaces or commas.
    *   Rounds numeric attributes in shapes and transforms (`transform`, `viewBox`, `points`, `cx`, `cy`, `r`, `width`, `height`, etc.).
*   **Color Shortening**: Minifies long color definitions like `rgb(255, 255, 255)` or `#ffffff` to short hex codes like `#fff`.
*   **Minified XML Serialization**: Saves the resulting document on a single flat line, omitting unnecessary XML declarations, newlines, and indentations.

---

## Compilation & Installation

To compile `reducesvg` as a single, self-contained executable (which includes the .NET runtime so it runs without external runtime dependencies), run the following command in the project directory:

```bash
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true
```

This generates `ReduceSvgFile.exe` under:
`bin\Release\net10.0\win-x64\publish\`

You can copy this file into any directory on your system added to your environment `PATH` (e.g., `C:\Users\your_user\Tools\`) and rename it to `reducesvg.exe` for global access.

---

## Usage Instructions

Run the tool from your command line using the following structure:

```bash
reducesvg <file_selector> [options]
```

### Practical Examples

1.  **Optimize a single file in-place**:
    ```bash
    reducesvg logo.svg
    ```
    *Creates an optimized file named `logo_out.svg` in the same directory.*

2.  **Optimize all SVGs in a directory**:
    ```bash
    reducesvg * -src "C:\Path\To\Your\SVGs"
    ```
    *Finds all `.svg` files in that folder and generates optimized versions with an `_out` suffix (e.g., `logo_out.svg`).*

3.  **Specify a custom output directory (without renaming files)**:
    ```bash
    reducesvg * -src "C:\Source" -out "C:\Destination"
    ```
    *Optimizes all SVGs in the source directory and saves them in the destination folder maintaining their original filenames (no `_out` suffix).*

4.  **Set decimal precision and output compression stats**:
    ```bash
    reducesvg logo.svg -p 2 -v
    ```
    *Rounds coordinates to 2 decimal places and prints original vs. optimized file size comparisons in the console.*

---

## Options & Arguments

| Option | Description |
| :--- | :--- |
| `-src <path>` | Specify the source directory (defaults to current directory). |
| `-out <path>` | Specify the output directory. Output files will retain their original names (no `_out` suffix). |
| `-p`, `--precision <num>` | Set decimal coordinate precision (default is `3`). |
| `-v`, `--verbose` | Print conversion and compression statistics. |
| `-h`, `--help` | Show the help menu with usage guidelines. |