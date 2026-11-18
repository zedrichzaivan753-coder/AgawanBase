// Docs/tools/MdToHtml.cs
//
// Print-ready HTML for the Week 13 submission.
//
// The three Week 13 documents are written in Markdown, and the module wants a PDF set. Rather
// than hand-maintaining two copies of each document, this converter renders the Markdown subset
// these documents actually use into one self-contained HTML file per document, styled for A4 with
// @media print, so File > Print > Save as PDF produces the submission PDF.
//
// It is deliberately NOT part of the Unity project: it sits in Docs/tools/, outside Assets/, so
// Unity never compiles it and it cannot break the build. Run it from the editor with
// execute_script (entry point: Execute, no arguments).
//
// Supported: h1-h4, fenced code, tables with a separator row, bullet and numbered lists
// (including indented continuation lines), horizontal rules, **bold**, *italic*, `code`, the
// <!--INLINE:file.svg--> marker that splices a diagram into a document, and (from Week 14)
// ![caption](path) images, which are inlined into the HTML as base64 data URIs.
//
// The image support exists because Week 13 shipped an HTML set that referenced no PNG at all: the
// screenshots had to be pasted into the documents by hand. A printed PDF must carry its own
// pictures, so every image is embedded in the file rather than linked beside it.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public static class MdToHtml
{
    // One entry per week folder, with the documents in it. Adding a week is one more line here.
    static readonly string[] Weeks = { "Week_13", "Week_14", "Week_15", "Week_16" };

    static readonly string[][] Documents =
    {
        new string[]
        {
            "Abrera_Zedrich_SA5.md",
            "Abrera_Zedrich_PT5.md",
            "Abrera_Zedrich_Week13_Reflection.md"
        },
        new string[]
        {
            "Abrera_Zedrich_SA6.md",
            "Abrera_Zedrich_PT6.md",
            "Abrera_Zedrich_Week14_Reflection.md"
        },
        new string[]
        {
            "Abrera_Zedrich_AS3_Milestone.md",
            "Abrera_Zedrich_Week15_Reflection.md"
        },
        new string[]
        {
            "Abrera_Zedrich_SA6.md",
            "Abrera_Zedrich_AS3.md",
            "Abrera_Zedrich_Week16_Reflection.md"
        }
    };

    public static void Execute()
    {
        string root = Directory.GetCurrentDirectory();

        for (int w = 0; w < Weeks.Length; w++)
        {
            string week = Path.Combine(root, @"Docs\Zedrich Abrera", Weeks[w]);
            string evidence = Path.Combine(week, "Evidence");

            if (!Directory.Exists(week))
            {
                Debug.Log("[MdToHtml] not found: " + week);
                continue;
            }

            string label = Weeks[w].Replace('_', ' ');   // "Week_14" -> "Week 14"

            for (int d = 0; d < Documents[w].Length; d++)
            {
                Convert(week, evidence, Documents[w][d], label);
            }
        }
    }

    // ------------------------------------------------------------------ one document

    static void Convert(string folder, string evidenceDir, string mdName, string weekLabel)
    {
        string mdPath = Path.Combine(folder, mdName);
        if (!File.Exists(mdPath))
        {
            Debug.Log("[MdToHtml] missing " + mdPath);
            return;
        }

        string text = File.ReadAllText(mdPath, Encoding.UTF8)
                              .Replace("\r\n", "\n")
                              .Replace("\r", "\n");
        string[] lines = text.Split('\n');

        StringBuilder body = new StringBuilder();
        string title = null;
        int i = 0;

        while (i < lines.Length)
        {
            string raw = lines[i];
            string t = raw.Trim();

            if (t.Length == 0) { i++; continue; }

            // ---- inline SVG marker -------------------------------------------------
            if (t.StartsWith("<!--INLINE:") && t.EndsWith("-->"))
            {
                string file = t.Substring(11, t.Length - 11 - 3).Trim();
                string svgPath = Path.Combine(evidenceDir, file);

                body.AppendLine("<figure class=\"diagram\">");
                if (File.Exists(svgPath))
                {
                    string svg = File.ReadAllText(svgPath, Encoding.UTF8).Trim();
                    if (svg.StartsWith("<?xml"))
                    {
                        int close = svg.IndexOf("?>", StringComparison.Ordinal);
                        if (close >= 0) svg = svg.Substring(close + 2).Trim();
                    }
                    if (svg.StartsWith("<!DOCTYPE"))
                    {
                        int close = svg.IndexOf('>');
                        if (close >= 0) svg = svg.Substring(close + 1).Trim();
                    }
                    body.AppendLine(svg);
                }
                else
                {
                    body.AppendLine("<p class=\"warn\">Diagram not found: " + Escape(file) + "</p>");
                }
                body.AppendLine("</figure>");
                i++;
                continue;
            }

            // ---- fenced code -------------------------------------------------------
            if (t.StartsWith("```"))
            {
                i++;
                StringBuilder code = new StringBuilder();
                while (i < lines.Length && !lines[i].Trim().StartsWith("```"))
                {
                    code.AppendLine(lines[i]);
                    i++;
                }
                if (i < lines.Length) i++;
                body.AppendLine("<pre><code>" + Escape(code.ToString().TrimEnd('\n')) + "</code></pre>");
                continue;
            }

            // ---- rule --------------------------------------------------------------
            if (t == "---")
            {
                body.AppendLine("<hr/>");
                i++;
                continue;
            }

            // ---- headings ----------------------------------------------------------
            if (t.StartsWith("#"))
            {
                int level = 0;
                while (level < t.Length && t[level] == '#') level++;
                if (level >= 1 && level <= 4 && t.Length > level && t[level] == ' ')
                {
                    string content = t.Substring(level + 1).Trim();
                    if (level == 1 && title == null) title = Plain(content);
                    body.AppendLine("<h" + level + ">" + Inline(content) + "</h" + level + ">");
                    i++;
                    continue;
                }
            }

            // ---- table -------------------------------------------------------------
            if (t.StartsWith("|"))
            {
                List<string> rows = new List<string>();
                while (i < lines.Length && lines[i].Trim().StartsWith("|"))
                {
                    rows.Add(lines[i].Trim());
                    i++;
                }

                body.AppendLine("<table>");

                int first = 0;
                if (rows.Count > 0 && !IsSeparator(rows[0]))
                {
                    body.AppendLine("<thead><tr>");
                    foreach (string cell in Cells(rows[0]))
                        body.AppendLine("<th>" + Inline(cell) + "</th>");
                    body.AppendLine("</tr></thead>");
                    first = 1;
                    if (rows.Count > 1 && IsSeparator(rows[1])) first = 2;
                }
                else if (rows.Count > 1)
                {
                    first = 2;
                }

                body.AppendLine("<tbody>");
                for (int r = first; r < rows.Count; r++)
                {
                    if (IsSeparator(rows[r])) continue;
                    body.AppendLine("<tr>");
                    foreach (string cell in Cells(rows[r]))
                        body.AppendLine("<td>" + Inline(cell) + "</td>");
                    body.AppendLine("</tr>");
                }
                body.AppendLine("</tbody></table>");
                continue;
            }

            // ---- bullet list -------------------------------------------------------
            if (t.StartsWith("- "))
            {
                body.AppendLine("<ul>");
                bool open = false;
                while (i < lines.Length)
                {
                    string lineRaw = lines[i];
                    string lt = lineRaw.Trim();

                    if (lt.Length == 0) break;

                    if (lt.StartsWith("- "))
                    {
                        if (open) body.AppendLine("</li>");
                        body.Append("<li>" + Inline(lt.Substring(2).Trim()));
                        open = true;
                        i++;
                        continue;
                    }

                    // indented continuation of the current item
                    if (open && (lineRaw.StartsWith("  ") || lineRaw.StartsWith("\t")) && !StartsBlock(lt))
                    {
                        body.Append(" " + Inline(lt));
                        i++;
                        continue;
                    }

                    break;
                }
                if (open) body.AppendLine("</li>");
                body.AppendLine("</ul>");
                continue;
            }

            // ---- numbered list -----------------------------------------------------
            if (Regex.IsMatch(t, @"^\d+\.\s"))
            {
                body.AppendLine("<ol>");
                bool open = false;
                while (i < lines.Length)
                {
                    string lineRaw = lines[i];
                    string lt = lineRaw.Trim();

                    if (lt.Length == 0) break;

                    Match m = Regex.Match(lt, @"^\d+\.\s+(.*)$");
                    if (m.Success)
                    {
                        if (open) body.AppendLine("</li>");
                        body.Append("<li>" + Inline(m.Groups[1].Value.Trim()));
                        open = true;
                        i++;
                        continue;
                    }

                    if (open && (lineRaw.StartsWith("  ") || lineRaw.StartsWith("\t")) && !StartsBlock(lt))
                    {
                        body.Append(" " + Inline(lt));
                        i++;
                        continue;
                    }

                    break;
                }
                if (open) body.AppendLine("</li>");
                body.AppendLine("</ol>");
                continue;
            }

            // ---- image: ![caption](path) --------------------------------------------
            if (t.StartsWith("!["))
            {
                Match img = Regex.Match(t, @"^!\[(.*?)\]\((.+?)\)\s*$");
                if (img.Success)
                {
                    body.AppendLine(Figure(img.Groups[1].Value, img.Groups[2].Value, folder));
                    i++;
                    continue;
                }
            }

            // ---- paragraph ---------------------------------------------------------
            StringBuilder para = new StringBuilder();
            while (i < lines.Length)
            {
                string lt = lines[i].Trim();
                if (lt.Length == 0) break;
                if (para.Length > 0 && StartsBlock(lt)) break;
                if (para.Length > 0) para.Append(' ');
                para.Append(lt);
                i++;
            }
            if (para.Length > 0)
            {
                body.AppendLine("<p>" + Inline(para.ToString()) + "</p>");
            }
            else
            {
                i++;   // never spin
            }
        }

        if (string.IsNullOrEmpty(title)) title = Path.GetFileNameWithoutExtension(mdName);

        string html = Page(title, mdName, body.ToString(), weekLabel);
        string outPath = Path.Combine(folder, Path.GetFileNameWithoutExtension(mdName) + ".html");
        File.WriteAllText(outPath, html, new UTF8Encoding(false));
        Debug.Log("[MdToHtml] wrote " + outPath + "  (" + html.Length + " chars)");
    }

    // ------------------------------------------------------------------ images

    /// <summary>
    /// ![caption](Evidence/file.png) -> a &lt;figure&gt; whose image is a base64 data URI.
    ///
    /// Inlined on purpose: a Week 13 lesson, where the HTML referenced no picture and the
    /// screenshots had to be pasted in by hand. A data URI means the HTML is the document - move
    /// the Evidence folder away and it still shows every screenshot, and printing it to PDF keeps
    /// them.
    /// </summary>
    static string Figure(string caption, string relativePath, string documentFolder)
    {
        string path = Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(documentFolder, relativePath.Replace('/', Path.DirectorySeparatorChar));

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<figure class=\"shot\">");

        if (!File.Exists(path))
        {
            sb.AppendLine("<p class=\"warn\">Image not found: " + Escape(relativePath) + "</p>");
            sb.AppendLine("</figure>");
            return sb.ToString();
        }

        byte[] bytes = File.ReadAllBytes(path);
        string mime = Mime(Path.GetExtension(path));

        // System.Convert, not Convert: this class has its own Convert() method, and the bare
        // name binds to that method group instead of the type (CS0023).
        sb.AppendLine("<img src=\"data:" + mime + ";base64," + System.Convert.ToBase64String(bytes) +
                      "\" alt=\"" + Escape(caption) + "\"/>");

        if (!string.IsNullOrEmpty(caption))
        {
            sb.AppendLine("<figcaption>" + Inline(caption) + "</figcaption>");
        }

        sb.AppendLine("</figure>");
        return sb.ToString();
    }

    static string Mime(string extension)
    {
        switch (extension.ToLowerInvariant())
        {
            case ".jpg":
            case ".jpeg": return "image/jpeg";
            case ".svg": return "image/svg+xml";
            case ".gif": return "image/gif";
            default: return "image/png";
        }
    }

    // ------------------------------------------------------------------ helpers

    static bool StartsBlock(string t)
    {
        if (t.Length == 0) return true;
        if (t.StartsWith("#")) return true;
        if (t.StartsWith("|")) return true;
        if (t.StartsWith("```")) return true;
        if (t == "---") return true;
        if (t.StartsWith("- ")) return true;
        if (t.StartsWith("<!--INLINE:")) return true;
        if (t.StartsWith("![")) return true;
        if (Regex.IsMatch(t, @"^\d+\.\s")) return true;
        return false;
    }

    static bool IsSeparator(string row)
    {
        string r = row.Trim();
        if (r.Length == 0) return false;
        bool dash = false;
        foreach (char c in r)
        {
            if (c == '-') { dash = true; continue; }
            if (c == '|' || c == ':' || c == ' ') continue;
            return false;
        }
        return dash;
    }

    static List<string> Cells(string row)
    {
        string r = row.Trim();
        if (r.StartsWith("|")) r = r.Substring(1);
        if (r.EndsWith("|")) r = r.Substring(0, r.Length - 1);

        List<string> list = new List<string>();
        foreach (string cell in r.Split('|')) list.Add(cell.Trim());
        return list;
    }

    static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    /// <summary>Escapes, then applies code / bold / italic. Code spans are protected first so a
    /// literal asterisk inside `match.*` can never be read as emphasis.</summary>
    static string Inline(string s)
    {
        List<string> code = new List<string>();

        s = Escape(s);
        s = Regex.Replace(s, "`([^`]+)`", delegate (Match m)
        {
            code.Add(m.Groups[1].Value);
            return "\u0001" + (code.Count - 1) + "\u0002";
        });
        s = Regex.Replace(s, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
        s = Regex.Replace(s, @"(?<!\*)\*([^*\n]+?)\*(?!\*)", "<em>$1</em>");
        s = Regex.Replace(s, "\u0001(\\d+)\u0002", delegate (Match m)
        {
            return "<code>" + code[int.Parse(m.Groups[1].Value)] + "</code>";
        });

        return s;
    }

    static string Plain(string s)
    {
        return s.Replace("`", "").Replace("**", "").Replace("*", "").Replace("&", "&amp;");
    }

    // ------------------------------------------------------------------ page shell

    static string Page(string title, string sourceName, string body, string weekLabel)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\"/>");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>");
        sb.AppendLine("<title>" + Escape(title) + "</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(@"
@page { size: A4; margin: 16mm 14mm 16mm 14mm; }

html { -webkit-text-size-adjust: 100%; }

body {
    margin: 0 auto;
    padding: 8mm 4mm 16mm 4mm;
    max-width: 186mm;
    background: #ffffff;
    color: #16191d;
    font-family: ""Segoe UI"", ""Helvetica Neue"", Arial, sans-serif;
    font-size: 10.5pt;
    line-height: 1.52;
}

h1, h2, h3, h4 { color: #12243a; page-break-after: avoid; break-after: avoid; }

h1 {
    font-size: 19pt;
    line-height: 1.2;
    margin: 0 0 4pt 0;
    padding-bottom: 6pt;
    border-bottom: 2.5pt solid #12243a;
}

h2 {
    font-size: 13.5pt;
    margin: 20pt 0 7pt 0;
    padding-bottom: 3pt;
    border-bottom: 0.75pt solid #c9ced6;
}

h3 { font-size: 11.5pt; margin: 14pt 0 5pt 0; color: #26405c; }
h4 { font-size: 10.5pt; margin: 11pt 0 4pt 0; color: #26405c; }

p { margin: 0 0 8pt 0; orphans: 3; widows: 3; }

strong { color: #101820; }

hr {
    border: none;
    border-top: 0.75pt solid #d5dae1;
    margin: 13pt 0;
}

ul, ol { margin: 0 0 9pt 0; padding-left: 17pt; }
li { margin: 0 0 3.5pt 0; }

table {
    width: 100%;
    border-collapse: collapse;
    margin: 5pt 0 12pt 0;
    font-size: 8.9pt;
    line-height: 1.38;
}

th, td {
    border: 0.7pt solid #b9c0c9;
    padding: 3.2pt 4.6pt;
    text-align: left;
    vertical-align: top;
}

th { background: #e9edf3; color: #12243a; font-weight: 600; }

tbody tr:nth-child(even) td { background: #f7f9fb; }

tr { page-break-inside: avoid; page-break-after: auto; }
thead { display: table-header-group; }

code {
    font-family: ""Cascadia Mono"", Consolas, ""Courier New"", monospace;
    font-size: 8.7pt;
    background: #f1f4f8;
    border: 0.5pt solid #e0e5ec;
    border-radius: 2pt;
    padding: 0 2pt;
    word-break: break-word;
}

pre {
    background: #f6f8fa;
    border: 0.7pt solid #d5dae1;
    border-left: 2.5pt solid #12243a;
    border-radius: 2pt;
    padding: 6pt 8pt;
    margin: 6pt 0 12pt 0;
    white-space: pre-wrap;
    word-wrap: break-word;
    page-break-inside: avoid;
}

pre code {
    background: none;
    border: none;
    padding: 0;
    font-size: 8.5pt;
    line-height: 1.45;
}

figure.diagram {
    margin: 8pt 0 12pt 0;
    padding: 6pt;
    border: 0.7pt solid #d5dae1;
    border-radius: 3pt;
    background: #ffffff;
    page-break-inside: avoid;
}

figure.diagram svg { display: block; width: 100%; height: auto; }

figure.shot {
    margin: 8pt 0 13pt 0;
    padding: 5pt;
    border: 0.7pt solid #d5dae1;
    border-radius: 3pt;
    background: #fbfcfd;
    page-break-inside: avoid;
    break-inside: avoid;
}

figure.shot img {
    display: block;
    width: 100%;
    height: auto;
    border: 0.5pt solid #c9ced6;
    border-radius: 2pt;
}

figure.shot figcaption {
    margin-top: 4pt;
    font-size: 8.6pt;
    color: #4c5661;
    line-height: 1.4;
}

p.warn { color: #8a1f1f; font-weight: 600; }

footer.docnote {
    margin-top: 22pt;
    padding-top: 6pt;
    border-top: 0.75pt solid #d5dae1;
    color: #5c6672;
    font-size: 8pt;
}

@media print {
    body { max-width: none; padding: 0; font-size: 10pt; }
    a { color: inherit; text-decoration: none; }
    h1 { font-size: 18pt; }
    table { font-size: 8.6pt; }
}
");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.Append(body);
        sb.AppendLine("<footer class=\"docnote\">AgawanBase &middot; Zedrich Zaivan B. Abrera &middot; MOBILE DEV 002, Module 5, "
                      + Escape(weekLabel) + " &middot; source: "
                      + Escape(sourceName) + "</footer>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }
}
