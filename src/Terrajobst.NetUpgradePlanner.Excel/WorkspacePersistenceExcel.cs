using System.Diagnostics;
using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Terrajobst.NetUpgradePlanner;

public static class WorkspacePersistenceExcel
{
    public static async Task SaveAsync(Workspace workspace, string path)
    {
        using var stream = File.Create(path);
        await SaveAsync(workspace, stream);
    }

    public static async Task SaveAsync(Workspace workspace, Stream stream)
    {
        using var memoryStream = new MemoryStream();

        using (var spreadsheet = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook))
        {
            spreadsheet.AddWorkbookPart();
            Debug.Assert(spreadsheet.WorkbookPart is not null);
            spreadsheet.WorkbookPart.Workbook = new Workbook();
            AddStylesheet(spreadsheet.WorkbookPart);

            AddAssembliesSheet(spreadsheet, workspace);
            AddDependenciesSheet(spreadsheet, workspace);
            AddProblemsSheet(spreadsheet, workspace);
            AddUsedApis(spreadsheet, workspace);
        }

        memoryStream.Position = 0;
        await memoryStream.CopyToAsync(stream);
    }

    private static void AddStylesheet(WorkbookPart workbookPart)
    {
        var cellstyle = new CellStyle { Name = "Normal", FormatId = 0U, BuiltinId = 0U };
        var border = new Border(new LeftBorder(), new RightBorder(), new TopBorder(), new BottomBorder(), new DiagonalBorder());

        var fill1 = new Fill(new PatternFill { PatternType = PatternValues.None });
        var fill2 = new Fill(new PatternFill { PatternType = PatternValues.Gray125 });

        var format1 = new CellFormat { FontId = 0U };
        var format2 = new CellFormat { FontId = 1U, ApplyFont = true };

        var textFont = new Font(
            new FontSize { Val = 11D },
            new Color { Theme = 1U },
            new FontName { Val = "Calibri" },
            new FontFamilyNumbering { Val = 2 },
            new FontScheme { Val = FontSchemeValues.Minor });

        var hyperlinkFont = new Font(
            new Underline(),
            new FontSize { Val = 11D },
            new Color { Theme = 10U },
            new FontName { Val = "Calibri" },
            new FontFamilyNumbering { Val = 2 },
            new FontScheme { Val = FontSchemeValues.Minor });

        var stylesheet = new Stylesheet
        {
            Fonts = new Fonts(textFont, hyperlinkFont),
            CellFormats = new CellFormats(format1, format2),
            Fills = new Fills(fill1, fill2),
            CellStyles = new CellStyles(cellstyle),
            Borders = new Borders(border),
        };

        workbookPart.AddNewPart<WorkbookStylesPart>();
        Debug.Assert(workbookPart.WorkbookStylesPart is not null);
        workbookPart.WorkbookStylesPart.Stylesheet = stylesheet;
    }

    private static void AddAssembliesSheet(SpreadsheetDocument spreadsheet, Workspace workspace)
    {
        var assembliesSheet = spreadsheet.AddWorksheet("Assemblies");

        var headers = new[]
        {
            "Assembly",
            "Target Framework",
            "Desired Framework",
            "Desired Platforms",
            "Score"
        };

        assembliesSheet.AddRow(headers);

        var analyzedAssemblies = workspace.Report is null
            ? new Dictionary<AssemblySetEntry, AnalyzedAssembly>()
            : workspace.Report.AnalyzedAssemblies.ToDictionary(a => a.Entry);

        foreach (var assembly in workspace.AssemblySet.Entries)
        {
            analyzedAssemblies.TryGetValue(assembly, out var analyzedAssembly);

            var name = assembly.Name;
            var targetFramework = assembly.TargetFramework ?? string.Empty;
            var desiredFramework = workspace.AssemblyConfiguration.GetDesiredFramework(assembly);
            var desiredPlatforms = workspace.AssemblyConfiguration.GetDesiredPlatforms(assembly).ToDisplayString();
            var score = analyzedAssembly is null ? (object?)null : Math.Round(analyzedAssembly.Score * 100, 2, MidpointRounding.AwayFromZero);

            assembliesSheet.AddRow(name, targetFramework, desiredFramework, desiredPlatforms, score);
        }

        assembliesSheet.FormatAsTable(1, workspace.AssemblySet.Entries.Count + 1, 1, headers);
        assembliesSheet.ApplyConditionalFormatting(2, workspace.AssemblySet.Entries.Count, 5, 1);
        assembliesSheet.SetColumnWidths(
            70, // Assembly
            20, // Target Framework
            20, // Desired Framework
            20, // Desired Platforms
            10  // Score
        );
    }

    private static void AddDependenciesSheet(SpreadsheetDocument spreadsheet, Workspace workspace)
    {
        if (workspace.Report is null)
            return;

        var problemsSheet = spreadsheet.AddWorksheet("Dependencies");

        var headers = new[]
        {
            "Assembly",
            "Dependency"
        };

        problemsSheet.AddRow(headers);

        var rowCount = 1;

        foreach (var assembly in workspace.AssemblySet.Entries)
        {
            foreach (var dependency in assembly.Dependencies)
            {
                var assemblyName = assembly.Name;

                problemsSheet.AddRow(
                    assemblyName,
                    dependency
                );

                rowCount++;
            }
        }

        problemsSheet.FormatAsTable(1, rowCount, 1, headers);
        problemsSheet.SetColumnWidths(
            70,  // Assembly
            70   // Dependency
        );
    }

    private static void AddProblemsSheet(SpreadsheetDocument spreadsheet, Workspace workspace)
    {
        if (workspace.Report is null)
            return;

        var problemsSheet = spreadsheet.AddWorksheet("Problems");

        var headers = new[]
        {
            "Assembly",
            "Severity",
            "Category",
            "Text",
            "Details",
            "Namespace",
            "Type",
            "Member",
        };

        problemsSheet.AddRow(headers);

        var rowCount = 1;

        foreach (var assembly in workspace.Report.AnalyzedAssemblies)
        {
            foreach (var problem in assembly.Problems)
            {
                var assemblyName = assembly.Entry.Name;
                var severity = problem.ProblemId.Severity.ToString();
                var category = problem.ProblemId.Category.ToString();
                var url = problem.ProblemId.Url;
                var text = problem.ProblemId.Text;
                var textObj = string.IsNullOrEmpty(url)
                    ? (object)text
                    : new HyperlinkCell(text, new Uri(url), null);
                var details = problem.Details;

                var namespaceName = problem.Api?.GetNamespaceName() ?? string.Empty;
                var typeName = problem.Api?.GetTypeName() ?? string.Empty;
                var memberName = problem.Api?.GetMemberName() ?? string.Empty;

                problemsSheet.AddRow(
                    assemblyName,
                    severity,
                    category,
                    textObj,
                    details,
                    namespaceName,
                    typeName,
                    memberName
                );

                rowCount++;
            }
        }

        problemsSheet.FormatAsTable(1, rowCount, 1, headers);
        problemsSheet.SetColumnWidths(
            70,  // Assembly
            10,  // Severity
            20,  // Category
            100, // Text
            100, // Details
            35,  // Namespace
            30,  // Type
            100  // Member
        );
    }

    private static void AddUsedApis(SpreadsheetDocument spreadsheet, Workspace workspace)
    {
        if (workspace.Report is null)
            return;

        var problemsSheet = spreadsheet.AddWorksheet("Used APIs");

        var headers = new[]
        {
            "Assembly",
            "Kind",
            "Namespace",
            "Type",
            "Member",
        };

        problemsSheet.AddRow(headers);

        var rowCount = 1;

        var catalog = workspace.Report.Catalog;
        var apiByGuid = catalog.GetAllApis().ToDictionary(a => a.Guid);

        foreach (var assembly in workspace.AssemblySet.Entries)
        {
            foreach (var apiGuid in assembly.UsedApis)
            {
                if (!apiByGuid.TryGetValue(apiGuid, out var api))
                    continue;

                var assemblyName = assembly.Name;
                var kind = api.Kind.ToString();
                var namespaceName = api.GetNamespaceName();
                var typeName = api.GetTypeName();
                var memberName = api.GetMemberName();

                problemsSheet.AddRow(
                    assemblyName,
                    kind,
                    namespaceName,
                    typeName,
                    memberName
                );

                rowCount++;
            }
        }

        problemsSheet.FormatAsTable(1, rowCount, 1, headers);
        problemsSheet.SetColumnWidths(
            70,  // Assembly
            15,  // Kind
            35,  // Namespace
            30,  // Type
            100  // Member
        );
    }

    public static Worksheet AddWorksheet(this SpreadsheetDocument spreadsheet, string name)
    {
        Debug.Assert(spreadsheet.WorkbookPart is not null);

        var sheets = spreadsheet.WorkbookPart.Workbook.GetFirstChild<Sheets>();
        if (sheets is null)
        {
            sheets = new Sheets();
            spreadsheet.WorkbookPart.Workbook.AppendChild(sheets);
        }

        var worksheetPart = spreadsheet.WorkbookPart.AddNewPart<WorksheetPart>();
        worksheetPart.Worksheet = new Worksheet();

        worksheetPart.Worksheet.Save();

        sheets.AppendChild(new Sheet()
        {
            Id = spreadsheet.WorkbookPart.GetIdOfPart(worksheetPart),
            SheetId = new UInt32Value((uint)sheets.Count() + 1),
            Name = name
        });

        return worksheetPart.Worksheet;
    }

    public static Table FormatAsTable(this Worksheet worksheet, int rowStart, int rowCount, int columnStart, params string[] headers)
    {
        if (rowCount == 1)
            rowCount++;

        var range = ComputeRange(rowStart, rowCount, columnStart, headers.Length);

        var sheetViews = worksheet.GetFirstChild<SheetViews>();
        if (sheetViews is null)
            sheetViews = worksheet.InsertAt(new SheetViews(), 0);

        var sheetView = sheetViews.AppendChild(new SheetView());
        sheetView.WorkbookViewId = 0;

        Debug.Assert(worksheet.WorksheetPart is not null);
        var tableDefinitionPart = worksheet.WorksheetPart.AddNewPart<TableDefinitionPart>();

        var tablePart = new TablePart
        {
            Id = worksheet.WorksheetPart.GetIdOfPart(tableDefinitionPart)
        };

        var tableParts = worksheet.GetFirstChild<TableParts>();
        if (tableParts is null)
            tableParts = worksheet.AppendChild(new TableParts());

        tableParts.AppendChild(tablePart);

        var tableId = GetNextTableId(worksheet);
        var table = new Table()
        {
            Id = tableId,
            Name = tableId.ToString(CultureInfo.InvariantCulture),
            DisplayName = "Table" + tableId.ToString(CultureInfo.InvariantCulture),
            Reference = range
        };

        tableDefinitionPart.Table = table;

        var columnCount = (uint)headers.Length;
        var tc = tableDefinitionPart.Table.AppendChild(new TableColumns() { Count = columnCount });
        for (uint i = 0; i < columnCount; i++)
        {
            var column = new TableColumn()
            {
                Id = i + 1,
                Name = headers[i]
            };

            tc.AppendChild(column);
        }

        tableDefinitionPart.Table.AutoFilter = new AutoFilter
        {
            Reference = range
        };

        var styleInfo = tableDefinitionPart.Table.AppendChild(new TableStyleInfo());
        styleInfo.Name = "TableStyleMedium2";
        styleInfo.ShowFirstColumn = false;
        styleInfo.ShowRowStripes = true;
        styleInfo.ShowLastColumn = false;
        styleInfo.ShowColumnStripes = false;

        return tableDefinitionPart.Table;
    }

    public static void ApplyConditionalFormatting(this Worksheet worksheet, int rowStart, int rowCount, int columnStart, int columnCount)
    {
        var range = ComputeRange(rowStart, rowCount, columnStart, columnCount);

        var conditionalFormatting1 = new ConditionalFormatting
        {
            SequenceOfReferences = new ListValue<StringValue>
            {
                InnerText = range
            }
        };

        var conditionalFormattingRule1 = new ConditionalFormattingRule
        {
            Type = ConditionalFormatValues.ColorScale,
            Priority = 1
        };

        var colorScale1 = new ColorScale();
        var conditionalFormatValueObject1 = new ConditionalFormatValueObject { Type = ConditionalFormatValueObjectValues.Number, Val = "0" };
        var conditionalFormatValueObject2 = new ConditionalFormatValueObject { Type = ConditionalFormatValueObjectValues.Percentile, Val = "50" };
        var conditionalFormatValueObject3 = new ConditionalFormatValueObject { Type = ConditionalFormatValueObjectValues.Number, Val = "100" };
        var color1 = new Color { Rgb = "FFF8696B" };
        var color2 = new Color { Rgb = "FFFFEB84" };
        var color3 = new Color { Rgb = "FF63BE7B" };

        colorScale1.Append(conditionalFormatValueObject1);
        colorScale1.Append(conditionalFormatValueObject2);
        colorScale1.Append(conditionalFormatValueObject3);
        colorScale1.Append(color1);
        colorScale1.Append(color2);
        colorScale1.Append(color3);

        conditionalFormattingRule1.Append(colorScale1);

        conditionalFormatting1.Append(conditionalFormattingRule1);

        // If we don't have this after SheetData, it corrupts the file if we have added hyperlinks before
        worksheet.InsertAfter(conditionalFormatting1, worksheet.Descendants<SheetData>().First());
    }

    public static void AddRow(this Worksheet ws, params object?[] data)
        => ws.AddRow((IEnumerable<object?>)data);

    public static void AddRow(this Worksheet ws, IEnumerable<object?> data)
    {
        var sd = ws.GetFirstChild<SheetData>();
        if (sd is null)
        {
            sd = ws.AppendChild(new SheetData());
        }

        var row = sd.AppendChild(new Row());

        foreach (var item in data)
        {
            if (item is null)
            {
                row.AppendChild(new Cell());
            }
            else if (item is HyperlinkCell hyperlinkCell)
            {
                var cell = CreateTextCell(hyperlinkCell.DisplayString);

                if (hyperlinkCell.StyleIndex.HasValue)
                {
                    cell.StyleIndex = (UInt32Value)hyperlinkCell.StyleIndex.Value;
                }

                row.AppendChild(cell);

                Debug.Assert(ws.WorksheetPart is not null);
                var hlRelationship = ws.WorksheetPart.AddHyperlinkRelationship(hyperlinkCell.Url, true);

                var hyperlink = new Hyperlink
                {
                    Reference = GetCellRefence(sd, row),
                    Id = hlRelationship.Id
                };

                var hyperlinks = ws.Descendants<Hyperlinks>().FirstOrDefault() ?? ws.AppendChild(new Hyperlinks());

                hyperlinks.Append(hyperlink);
            }
            else if (item is double number)
            {
                row.AppendChild(CreateNumberCell(number.ToString(CultureInfo.InvariantCulture)));
            }
            else if (item is IFormattable f)
            {
                var text = f.ToString(null, CultureInfo.InvariantCulture);
                row.AppendChild(CreateTextCell(text));
            }
            else
            {
                var text = item.ToString() ?? string.Empty;
                row.AppendChild(CreateTextCell(text));
            }
        }
    }

    private static string GetCellRefence(SheetData sd, Row row)
    {
        var rowCount = sd.Descendants<Row>().TakeWhile(r => r != row).Count() + 1;

        // Column needs to be 0-based for the GetColumnName method
        var columnCount = row.Descendants<Cell>().Count() - 1;

        return string.Format(CultureInfo.InvariantCulture, "{0}{1}", GetColumnName(columnCount), rowCount);
    }

    private static string GetColumnName(int index)
    {
        const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        var sb = new StringBuilder();

        if (index >= Alphabet.Length)
        {
            sb.Append(Alphabet[(index / Alphabet.Length) - 1]);
        }

        sb.Append(Alphabet[index % Alphabet.Length]);

        return sb.ToString();
    }

    public static void SetColumnWidths(this Worksheet ws, params double[] columnWidths)
    {
        AddColumnWidth(ws, (IEnumerable<double>)columnWidths);
    }

    public static void AddColumnWidth(this Worksheet ws, IEnumerable<double> columnWidths)
    {
        var columns = new Columns();

        uint pos = 1;
        foreach (var width in columnWidths)
        {
            var column = new Column
            {
                Min = (UInt32Value)pos,
                Max = (UInt32Value)pos,
                Width = width,
                BestFit = true,
                CustomWidth = true
            };
            columns.Append(column);

            pos++;
        }

        var sd = ws.GetFirstChild<SheetData>();

        if (sd is not null)
        {
            ws.InsertBefore<Columns>(columns, sd);
        }
        else
        {
            ws.Append(columns);
        }
    }

    public static Cell CreateTextCell(string text)
    {
        var cell = new Cell
        {
            DataType = CellValues.InlineString,
        };

        var inlineString = new InlineString
        {
            Text = new Text(text)
        };

        cell.AppendChild(inlineString);

        return cell;
    }

    public static Cell CreateNumberCell(string value)
    {
        return new Cell
        {
            CellValue = new CellValue(value)
        };
    }

    private static string ComputeRange(int rowStart, int rowCount, int columnStart, int columnCount)
    {
        if (columnStart + columnCount > 26)
            throw new NotSupportedException("Too many columns");

        return string.Format(CultureInfo.InvariantCulture, "{0}{1}:{2}{3}", (char)(((uint)'A') + columnStart - 1), rowStart, (char)(((uint)'A') + columnStart + columnCount - 2), rowStart + rowCount - 1);
    }

    private static uint GetNextTableId(Worksheet w)
    {
        var result = 0L;

        Debug.Assert(w.WorksheetPart is not null);
        var spreadsheet = (SpreadsheetDocument)w.WorksheetPart.OpenXmlPackage;

        Debug.Assert(spreadsheet.WorkbookPart is not null);
        foreach (var wp in spreadsheet.WorkbookPart.WorksheetParts)
        {
            foreach (var part in wp.GetPartsOfType<TableDefinitionPart>())
            {
                var value = part.Table?.Id;

                if (value is not null && value.HasValue)
                    result = Math.Max(result, value.Value);
            }
        }

        return (uint)(result + 1);
    }

    private sealed class HyperlinkCell
    {
        public HyperlinkCell(string displayString, Uri url, uint? styleIndex)
        {
            DisplayString = displayString;
            Url = url;
            StyleIndex = styleIndex;
        }

        public string DisplayString { get; }

        public Uri Url { get; }

        public uint? StyleIndex { get; }
    }
}
