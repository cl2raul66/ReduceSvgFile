using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ReduceSvgFile
{
    public static class SvgOptimizer
    {
        private class PathCommand
        {
            public char Type { get; set; }
            public List<double> Arguments { get; set; } = new List<double>();
        }

        private class StringWriterWithEncoding : StringWriter
        {
            public StringWriterWithEncoding(Encoding encoding) : base()
            {
                SpecifiedEncoding = encoding;
            }

            public override Encoding Encoding => SpecifiedEncoding;
            private Encoding SpecifiedEncoding { get; }
        }

        /// <summary>
        /// Optimizes an SVG content string by removing editor metadata, unused elements/IDs, and rounding numbers.
        /// </summary>
        public static string Optimize(string svgContent, int precision)
        {
            if (string.IsNullOrWhiteSpace(svgContent))
                return svgContent;

            XDocument doc;
            try
            {
                doc = XDocument.Parse(svgContent, LoadOptions.PreserveWhitespace);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Failed to parse SVG content as XML. Details: " + ex.Message, ex);
            }

            var root = doc.Root;
            if (root == null || root.Name.LocalName != "svg")
            {
                throw new InvalidDataException("Root element is not <svg>.");
            }

            // 1. Remove all XML comments
            doc.DescendantNodes().OfType<XComment>().Remove();

            // 2. Clean up editor-specific namespaces on the root element
            CleanRootNamespaces(root);

            // 3. Convert inline CSS styles to attributes, remove editor-specific attributes and elements recursively
            CleanElement(root, precision);

            // 4. Optimization Loop: Dead Code Elimination (unused defs/IDs) and Group Collapsing
            bool changed;
            int iterations = 0;
            do
            {
                changed = false;
                changed |= RemoveUnusedIdsAndElements(doc);
                changed |= CollapseUselessGroups(root);
                iterations++;
            } while (changed && iterations < 10);

            // 5. Save the document to a minified string
            var settings = new XmlWriterSettings
            {
                Indent = false,
                NewLineOnAttributes = false,
                OmitXmlDeclaration = true,
                Encoding = new UTF8Encoding(false) // No BOM
            };

            using (var sw = new StringWriterWithEncoding(Encoding.UTF8))
            {
                using (var xw = XmlWriter.Create(sw, settings))
                {
                    doc.Save(xw);
                }
                return sw.ToString();
            }
        }

        private static void CleanRootNamespaces(XElement root)
        {
            var nsToRemove = new List<XAttribute>();
            foreach (var attr in root.Attributes())
            {
                if (attr.IsNamespaceDeclaration)
                {
                    string nsVal = attr.Value;
                    string prefix = attr.Name.LocalName;

                    // Remove redundant prefix-based SVG namespace declarations (e.g. xmlns:svg)
                    // keeping only the default xmlns="http://www.w3.org/2000/svg" (where prefix is "xmlns")
                    if (prefix != "xmlns" && nsVal == "http://www.w3.org/2000/svg")
                    {
                        nsToRemove.Add(attr);
                    }
                    else if (nsVal != "http://www.w3.org/2000/svg" &&
                             nsVal != "http://www.w3.org/1999/xlink" &&
                             nsVal != "http://www.w3.org/XML/1998/namespace")
                    {
                        nsToRemove.Add(attr);
                    }
                }
            }
            foreach (var attr in nsToRemove)
            {
                attr.Remove();
            }
        }

        private static void ConvertStyleToAttributes(XElement element)
        {
            var styleAttr = element.Attribute("style");
            if (styleAttr == null) return;

            var styles = styleAttr.Value.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var remainingStyles = new List<string>();

            foreach (var style in styles)
            {
                var parts = style.Split(':', 2);
                if (parts.Length == 2)
                {
                    string key = parts[0].Trim();
                    string val = parts[1].Trim();

                    if (IsValidXmlAttributeName(key))
                    {
                        element.SetAttributeValue(key, val);
                    }
                    else
                    {
                        remainingStyles.Add($"{key}:{val}");
                    }
                }
            }

            if (remainingStyles.Count > 0)
            {
                styleAttr.Value = string.Join(";", remainingStyles);
            }
            else
            {
                styleAttr.Remove();
            }
        }

        private static bool IsValidXmlAttributeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            char first = name[0];
            if (!char.IsLetter(first) && first != '_') return false;

            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.') return false;
            }
            return true;
        }

        private static void CleanElement(XElement element, int precision)
        {
            // Convert inline CSS style rules to separate attributes
            ConvertStyleToAttributes(element);

            // Remove editor-specific and non-SVG attributes
            var attributesToRemove = new List<XAttribute>();
            foreach (var attr in element.Attributes())
            {
                if (attr.IsNamespaceDeclaration)
                {
                    string nsVal = attr.Value;
                    if (nsVal != "http://www.w3.org/2000/svg" &&
                        nsVal != "http://www.w3.org/1999/xlink" &&
                        nsVal != "http://www.w3.org/XML/1998/namespace")
                    {
                        attributesToRemove.Add(attr);
                    }
                }
                else
                {
                    var ns = attr.Name.NamespaceName;
                    if (!string.IsNullOrEmpty(ns) &&
                        ns != "http://www.w3.org/2000/svg" &&
                        ns != "http://www.w3.org/1999/xlink" &&
                        ns != "http://www.w3.org/XML/1998/namespace")
                    {
                        attributesToRemove.Add(attr);
                    }
                }
            }

            foreach (var attr in attributesToRemove)
            {
                attr.Remove();
            }

            // Optimize and round numerical attributes
            foreach (var attr in element.Attributes().ToList())
            {
                string name = attr.Name.LocalName;
                string val = attr.Value;

                if (name == "d")
                {
                    attr.Value = OptimizePathData(val, precision);
                }
                else if (name == "transform" || name == "gradientTransform" || name == "patternTransform")
                {
                    attr.Value = OptimizeTransform(val, precision);
                }
                else if (name == "viewBox" || name == "points" || name == "stroke-dasharray")
                {
                    attr.Value = OptimizeNumericList(val, precision);
                }
                else if (name == "fill" || name == "stroke" || name == "stop-color" || name == "flood-color" || name == "lighting-color")
                {
                    attr.Value = OptimizeColor(val);
                }
                else if (name == "x" || name == "y" || name == "width" || name == "height" ||
                         name == "cx" || name == "cy" || name == "r" ||
                         name == "rx" || name == "ry" ||
                         name == "x1" || name == "y1" || name == "x2" || name == "y2" ||
                         name == "stroke-width" || name == "font-size" ||
                         name == "opacity" || name == "fill-opacity" || name == "stroke-opacity")
                {
                    attr.Value = OptimizeNumericAttribute(val, precision);
                }
            }

            // Recursively process child elements
            var children = element.Elements().ToList();
            foreach (var child in children)
            {
                var childNs = child.Name.NamespaceName;
                var childName = child.Name.LocalName;

                // Remove editor-specific elements entirely
                if (!string.IsNullOrEmpty(childNs) &&
                    childNs != "http://www.w3.org/2000/svg" &&
                    childNs != "http://www.w3.org/1999/xlink")
                {
                    child.Remove();
                    continue;
                }

                // Remove standard metadata, desc, and title elements (typically unnecessary in optimized SVGs)
                if (childName == "metadata" || childName == "desc" || childName == "title")
                {
                    child.Remove();
                    continue;
                }

                CleanElement(child, precision);
            }

            // Clean up empty containers
            if ((element.Name.LocalName == "g" || element.Name.LocalName == "defs") && !element.HasElements)
            {
                bool hasAttributes = element.Attributes().Any(a => !a.IsNamespaceDeclaration && a.Name.LocalName != "id");
                if (!hasAttributes)
                {
                    element.Remove();
                }
            }
        }

        private static bool RemoveUnusedIdsAndElements(XDocument doc)
        {
            var referencedIds = new HashSet<string>();
            var urlRegex = new Regex(@"url\s*\(\s*#([^)]+)\s*\)", RegexOptions.Compiled);

            // Collect all references
            foreach (var element in doc.Descendants())
            {
                foreach (var attr in element.Attributes())
                {
                    string val = attr.Value;
                    var matches = urlRegex.Matches(val);
                    foreach (Match match in matches)
                    {
                        referencedIds.Add(match.Groups[1].Value.Trim());
                    }

                    if ((attr.Name.LocalName == "href" || attr.Name.LocalName == "xlink:href") && val.StartsWith("#"))
                    {
                        referencedIds.Add(val.Substring(1).Trim());
                    }
                }
            }

            bool removedAny = false;
            var elementsToRemove = new List<XElement>();
            var attributesToRemove = new List<XAttribute>();

            var defElementTypes = new HashSet<string>
            {
                "linearGradient", "radialGradient", "clipPath", "mask", "filter", "pattern", "marker"
            };

            foreach (var element in doc.Descendants().ToList())
            {
                var idAttr = element.Attribute("id");
                if (idAttr != null)
                {
                    string idVal = idAttr.Value.Trim();
                    if (!referencedIds.Contains(idVal))
                    {
                        bool isDefElement = defElementTypes.Contains(element.Name.LocalName) ||
                                            element.Ancestors().Any(a => a.Name.LocalName == "defs");

                        if (isDefElement)
                        {
                            elementsToRemove.Add(element);
                        }
                        else
                        {
                            attributesToRemove.Add(idAttr);
                        }
                    }
                }
            }

            foreach (var attr in attributesToRemove)
            {
                attr.Remove();
                removedAny = true;
            }

            foreach (var element in elementsToRemove)
            {
                if (element.Parent != null)
                {
                    element.Remove();
                    removedAny = true;
                }
            }

            return removedAny;
        }

        private static bool CollapseUselessGroups(XElement element)
        {
            bool collapsedAny = false;

            // Process children first
            foreach (var child in element.Elements().ToList())
            {
                collapsedAny |= CollapseUselessGroups(child);
            }

            if (element.Name.LocalName == "g" && element.Parent != null)
            {
                // Group is useless if it has no attributes at all
                bool hasAttributes = element.Attributes().Any(a => !a.IsNamespaceDeclaration);
                if (!hasAttributes)
                {
                    var children = element.Nodes().ToList();
                    foreach (var child in children)
                    {
                        child.Remove();
                        element.AddBeforeSelf(child);
                    }
                    element.Remove();
                    collapsedAny = true;
                }
            }

            return collapsedAny;
        }

        private static string FormatNumber(double value, int precision)
        {
            double rounded = Math.Round(value, precision);
            if (rounded == 0.0) rounded = 0.0; // Remove negative zero

            string numStr = rounded.ToString("G", CultureInfo.InvariantCulture);

            // Shorten decimal notation: "0.5" -> ".5", "-0.5" -> "-.5"
            if (numStr.StartsWith("0.")) numStr = numStr.Substring(1);
            else if (numStr.StartsWith("-0.")) numStr = "-" + numStr.Substring(2);

            return numStr;
        }

        private static string OptimizeNumericAttribute(string valStr, int precision)
        {
            if (string.IsNullOrEmpty(valStr)) return valStr;

            string unit = "";
            string numPart = valStr;

            string[] units = { "px", "pt", "em", "ex", "%", "in", "cm", "mm", "pc" };
            foreach (var u in units)
            {
                if (valStr.EndsWith(u, StringComparison.OrdinalIgnoreCase))
                {
                    unit = valStr.Substring(valStr.Length - u.Length);
                    numPart = valStr.Substring(0, valStr.Length - u.Length);
                    break;
                }
            }

            if (double.TryParse(numPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
            {
                return FormatNumber(val, precision) + unit;
            }

            return valStr;
        }

        private static string OptimizeNumericList(string listStr, int precision)
        {
            if (string.IsNullOrEmpty(listStr)) return listStr;

            var parts = listStr.Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var roundedParts = new List<string>();

            foreach (var part in parts)
            {
                roundedParts.Add(OptimizeNumericAttribute(part, precision));
            }

            return string.Join(" ", roundedParts);
        }

        private static string OptimizeTransform(string transformStr, int precision)
        {
            if (string.IsNullOrEmpty(transformStr)) return transformStr;

            string pattern = @"[-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?";
            return Regex.Replace(transformStr, pattern, match =>
            {
                if (double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                {
                    return FormatNumber(value, precision);
                }
                return match.Value;
            });
        }

        private static string OptimizeColor(string color)
        {
            if (string.IsNullOrEmpty(color)) return color;

            // If it is a url reference, do not optimize as color (keeps original capitalization)
            if (color.Contains("url(", StringComparison.OrdinalIgnoreCase))
            {
                return color;
            }

            color = color.Trim().ToLowerInvariant();

            // rgb(...) format
            if (color.StartsWith("rgb("))
            {
                var match = Regex.Match(color, @"rgb\(\s*(\d+%?)\s*,\s*(\d+%?)\s*,\s*(\d+%?)\s*\)");
                if (match.Success)
                {
                    int r = ParseColorComponent(match.Groups[1].Value);
                    int g = ParseColorComponent(match.Groups[2].Value);
                    int b = ParseColorComponent(match.Groups[3].Value);
                    color = $"#{r:x2}{g:x2}{b:x2}";
                }
            }

            // Hex shortening: #aabbcc -> #abc
            if (color.StartsWith("#") && color.Length == 7)
            {
                if (color[1] == color[2] && color[3] == color[4] && color[5] == color[6])
                {
                    color = "#" + color[1] + color[3] + color[5];
                }
            }

            // Named colors mapping
            if (color == "black") return "#000";
            if (color == "white") return "#fff";
            if (color == "yellow") return "#ff0";
            if (color == "fuchsia" || color == "magenta") return "#f0f";
            if (color == "lime") return "#0f0";
            if (color == "aqua") return "#0ff";

            return color;
        }

        private static int ParseColorComponent(string comp)
        {
            comp = comp.Trim();
            if (comp.EndsWith("%"))
            {
                if (double.TryParse(comp.Substring(0, comp.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                {
                    return (int)Math.Clamp(Math.Round(val * 2.55), 0, 255);
                }
            }
            else
            {
                if (int.TryParse(comp, out int val))
                {
                    return Math.Clamp(val, 0, 255);
                }
            }
            return 0;
        }

        private static List<PathCommand> ParsePath(string pathData)
        {
            var commands = new List<PathCommand>();
            if (string.IsNullOrEmpty(pathData)) return commands;

            int i = 0;
            int len = pathData.Length;
            char currentCommand = '\0';

            while (i < len)
            {
                char c = pathData[i];

                // Skip spaces and commas
                if (char.IsWhiteSpace(c) || c == ',')
                {
                    i++;
                    continue;
                }

                // Check for command character (excluding scientific notation exponent letters)
                if (char.IsLetter(c) && c != 'e' && c != 'E')
                {
                    currentCommand = c;
                    commands.Add(new PathCommand { Type = c });
                    i++;
                    continue;
                }

                // Skip coordinate values if we don't have a command context (invalid SVG, but failsafe)
                if (currentCommand == '\0')
                {
                    i++;
                    continue;
                }

                var activeCmd = commands[commands.Count - 1];

                // Arc command flags logic
                bool isFlag = false;
                if (char.ToUpperInvariant(activeCmd.Type) == 'A')
                {
                    int argCount = activeCmd.Arguments.Count % 7;
                    if (argCount == 3 || argCount == 4)
                    {
                        isFlag = true;
                    }
                }

                if (isFlag)
                {
                    if (c == '0' || c == '1')
                    {
                        activeCmd.Arguments.Add(c - '0');
                        i++;
                    }
                    else
                    {
                        i++; // Skip malformed flag character
                    }
                }
                else
                {
                    // Parse standard double value
                    int start = i;
                    bool hasDecimal = false;
                    bool hasExponent = false;

                    if (c == '-' || c == '+')
                    {
                        i++;
                    }

                    while (i < len)
                    {
                        c = pathData[i];
                        if (char.IsDigit(c))
                        {
                            i++;
                        }
                        else if (c == '.' && !hasDecimal && !hasExponent)
                        {
                            hasDecimal = true;
                            i++;
                        }
                        else if ((c == 'e' || c == 'E') && !hasExponent)
                        {
                            hasExponent = true;
                            i++;
                            if (i < len && (pathData[i] == '-' || pathData[i] == '+'))
                            {
                                i++;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (i > start)
                    {
                        string numStr = pathData.Substring(start, i - start);
                        if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                        {
                            activeCmd.Arguments.Add(val);
                        }
                    }
                    else
                    {
                        i++; // Avoid infinite loop
                    }
                }
            }

            return commands;
        }

        private static string OptimizePathData(string pathData, int precision)
        {
            var commands = ParsePath(pathData);
            var sb = new StringBuilder();

            foreach (var cmd in commands)
            {
                sb.Append(cmd.Type);

                bool isArc = char.ToUpperInvariant(cmd.Type) == 'A';
                int argCount = cmd.Arguments.Count;

                for (int i = 0; i < argCount; i++)
                {
                    double val = cmd.Arguments[i];
                    bool isFlag = isArc && (i % 7 == 3 || i % 7 == 4);

                    string numStr;
                    if (isFlag)
                    {
                        numStr = ((int)Math.Round(val)).ToString();
                    }
                    else
                    {
                        numStr = FormatNumber(val, precision);
                    }

                    if (i > 0)
                    {
                        bool needSpace = true;
                        if (numStr[0] == '-')
                        {
                            needSpace = false;
                        }
                        else if (isArc && (i % 7 == 4))
                        {
                            needSpace = false; // sweep-flag after large-arc-flag (both are single digits)
                        }

                        if (needSpace) sb.Append(' ');
                        sb.Append(numStr);
                    }
                    else
                    {
                        sb.Append(numStr);
                    }
                }
            }

            return sb.ToString();
        }
    }
}
