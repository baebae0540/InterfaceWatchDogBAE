using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using P = DocumentFormat.OpenXml.Presentation;
using D = DocumentFormat.OpenXml.Drawing;

namespace PptGenerator;

public class SlideBuilder
{
    private readonly PresentationDocument _doc;
    private readonly SlideLayoutPart _layoutPart;
    private uint _slideId = 256;
    private int _relId = 100;

    public const long SlideWidth = 12192000;
    public const long SlideHeight = 6858000;
    public const long Emu = 914400;
    public const long EmuCm = 360000;

    public SlideBuilder(PresentationDocument doc)
    {
        _doc = doc;
        _layoutPart = InitPresentation();
    }

    private SlideLayoutPart InitPresentation()
    {
        var presentationPart = _doc.AddPresentationPart();

        var slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>("rId1");

        var slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>("rId1");
        slideLayoutPart.SlideLayout = new P.SlideLayout(
            new CommonSlideData(new ShapeTree(
                new P.NonVisualGroupShapeProperties(
                    new P.NonVisualDrawingProperties { Id = 1U, Name = "" },
                    new P.NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new D.TransformGroup()))),
            new P.ColorMapOverride(new D.MasterColorMapping()))
        { Type = P.SlideLayoutValues.Blank };

        // SlideLayout → SlideMaster 역참조 (PowerPoint 필수 요구)
        slideLayoutPart.AddPart(slideMasterPart);

        var colorMap = new P.ColorMap
        {
            Background1 = D.ColorSchemeIndexValues.Light1,
            Text1 = D.ColorSchemeIndexValues.Dark1,
            Background2 = D.ColorSchemeIndexValues.Light2,
            Text2 = D.ColorSchemeIndexValues.Dark2,
            Accent1 = D.ColorSchemeIndexValues.Accent1,
            Accent2 = D.ColorSchemeIndexValues.Accent2,
            Accent3 = D.ColorSchemeIndexValues.Accent3,
            Accent4 = D.ColorSchemeIndexValues.Accent4,
            Accent5 = D.ColorSchemeIndexValues.Accent5,
            Accent6 = D.ColorSchemeIndexValues.Accent6,
            Hyperlink = D.ColorSchemeIndexValues.Hyperlink,
            FollowedHyperlink = D.ColorSchemeIndexValues.FollowedHyperlink
        };

        slideMasterPart.SlideMaster = new P.SlideMaster(
            new CommonSlideData(new ShapeTree(
                new P.NonVisualGroupShapeProperties(
                    new P.NonVisualDrawingProperties { Id = 1U, Name = "" },
                    new P.NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new D.TransformGroup()))),
            colorMap,
            new P.SlideLayoutIdList(
                new P.SlideLayoutId { Id = 2147483649U, RelationshipId = "rId1" }));

        var themePart = slideMasterPart.AddNewPart<ThemePart>("rId2");
        themePart.Theme = CreateTheme();

        presentationPart.AddPart(themePart, "rId2");

        presentationPart.Presentation = new Presentation(
            new P.SlideMasterIdList(
                new P.SlideMasterId { Id = 2147483648U, RelationshipId = "rId1" }),
            new SlideIdList(),
            new SlideSize { Cx = (int)SlideWidth, Cy = (int)SlideHeight, Type = SlideSizeValues.Custom },
            new NotesSize { Cx = (int)SlideHeight, Cy = (int)SlideWidth }
        );

        return slideLayoutPart;
    }

    private static D.Theme CreateTheme()
    {
        return new D.Theme(
            new D.ThemeElements(
                new D.ColorScheme(
                    new D.Dark1Color(new D.SystemColor { Val = D.SystemColorValues.WindowText, LastColor = "000000" }),
                    new D.Light1Color(new D.SystemColor { Val = D.SystemColorValues.Window, LastColor = "FFFFFF" }),
                    new D.Dark2Color(new D.RgbColorModelHex { Val = "1C2028" }),
                    new D.Light2Color(new D.RgbColorModelHex { Val = "F5F6FA" }),
                    new D.Accent1Color(new D.RgbColorModelHex { Val = "2563EB" }),
                    new D.Accent2Color(new D.RgbColorModelHex { Val = "16A34A" }),
                    new D.Accent3Color(new D.RgbColorModelHex { Val = "DC2626" }),
                    new D.Accent4Color(new D.RgbColorModelHex { Val = "8B5CF6" }),
                    new D.Accent5Color(new D.RgbColorModelHex { Val = "F59E0B" }),
                    new D.Accent6Color(new D.RgbColorModelHex { Val = "06B6D4" }),
                    new D.Hyperlink(new D.RgbColorModelHex { Val = "2563EB" }),
                    new D.FollowedHyperlinkColor(new D.RgbColorModelHex { Val = "8B5CF6" }))
                { Name = "InterfaceWatchDog" },
                new D.FontScheme(
                    new D.MajorFont(
                        new D.LatinFont { Typeface = "맑은 고딕" },
                        new D.EastAsianFont { Typeface = "맑은 고딕" },
                        new D.ComplexScriptFont { Typeface = "맑은 고딕" }),
                    new D.MinorFont(
                        new D.LatinFont { Typeface = "맑은 고딕" },
                        new D.EastAsianFont { Typeface = "맑은 고딕" },
                        new D.ComplexScriptFont { Typeface = "맑은 고딕" }))
                { Name = "InterfaceWatchDog" },
                new D.FormatScheme(
                    new D.FillStyleList(
                        new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor }),
                        new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor }),
                        new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor })),
                    new D.LineStyleList(
                        new D.Outline(new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor })) { Width = 9525 },
                        new D.Outline(new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor })) { Width = 9525 },
                        new D.Outline(new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor })) { Width = 9525 }),
                    new D.EffectStyleList(
                        new D.EffectStyle(new D.EffectList()),
                        new D.EffectStyle(new D.EffectList()),
                        new D.EffectStyle(new D.EffectList())),
                    new D.BackgroundFillStyleList(
                        new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor }),
                        new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor }),
                        new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor })))
                { Name = "InterfaceWatchDog" }))
        { Name = "InterfaceWatchDog" };
    }

    public SlidePart AddSlide()
    {
        var presentationPart = _doc.PresentationPart!;
        var slidePart = presentationPart.AddNewPart<SlidePart>($"rId{_relId++}");
        slidePart.AddPart(_layoutPart, "rId1");

        slidePart.Slide = new Slide(new CommonSlideData(new ShapeTree(
            new P.NonVisualGroupShapeProperties(
                new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                new P.NonVisualGroupShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            new GroupShapeProperties(new D.TransformGroup())
        )));

        var slideIdList = presentationPart.Presentation.SlideIdList!;
        slideIdList.Append(new SlideId
        {
            Id = _slideId++,
            RelationshipId = presentationPart.GetIdOfPart(slidePart)
        });

        return slidePart;
    }

    public static P.Shape CreateTextBox(
        long x, long y, long cx, long cy,
        string text,
        int fontSize = 1800,
        string fontColor = "000000",
        bool bold = false,
        string fontFamily = "맑은 고딕",
        D.TextAlignmentTypeValues? align = null,
        D.TextAnchoringTypeValues? anchor = null)
    {
        var shape = new P.Shape();
        shape.NonVisualShapeProperties = new P.NonVisualShapeProperties(
            new P.NonVisualDrawingProperties { Id = GetNextShapeId(), Name = "TextBox" },
            new P.NonVisualShapeDrawingProperties(new D.ShapeLocks { NoGrouping = true }),
            new ApplicationNonVisualDrawingProperties());

        shape.ShapeProperties = new P.ShapeProperties(
            new D.Transform2D(
                new D.Offset { X = x, Y = y },
                new D.Extents { Cx = cx, Cy = cy }),
            new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Rectangle });

        var actualAlign = align ?? D.TextAlignmentTypeValues.Left;
        var actualAnchor = anchor ?? D.TextAnchoringTypeValues.Top;
        var paragraph = CreateParagraph(text, fontSize, fontColor, bold, fontFamily, actualAlign);

        shape.TextBody = new P.TextBody(
            new D.BodyProperties { Anchor = actualAnchor, Wrap = D.TextWrappingValues.Square },
            new D.ListStyle(),
            paragraph);

        return shape;
    }

    public static P.Shape CreateMultiLineBulletBox(
        long x, long y, long cx, long cy,
        string[] lines,
        int fontSize = 1600,
        string fontColor = "333333",
        string fontFamily = "맑은 고딕",
        int bulletIndent = 457200,
        int marginLeft = 457200)
    {
        var shape = new P.Shape();
        shape.NonVisualShapeProperties = new P.NonVisualShapeProperties(
            new P.NonVisualDrawingProperties { Id = GetNextShapeId(), Name = "BulletBox" },
            new P.NonVisualShapeDrawingProperties(new D.ShapeLocks { NoGrouping = true }),
            new ApplicationNonVisualDrawingProperties());

        shape.ShapeProperties = new P.ShapeProperties(
            new D.Transform2D(
                new D.Offset { X = x, Y = y },
                new D.Extents { Cx = cx, Cy = cy }),
            new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Rectangle });

        var body = new P.TextBody(
            new D.BodyProperties { Anchor = D.TextAnchoringTypeValues.Top, Wrap = D.TextWrappingValues.Square },
            new D.ListStyle());

        foreach (var line in lines)
        {
            var isSubBullet = line.StartsWith("  ");
            var cleanLine = line.TrimStart();
            var para = new D.Paragraph();

            var pProps = new D.ParagraphProperties
            {
                LeftMargin = isSubBullet ? marginLeft * 2 : marginLeft,
                Indent = -bulletIndent
            };
            pProps.Append(new D.BulletFont { Typeface = "Arial" });
            pProps.Append(new D.CharacterBullet { Char = isSubBullet ? "–" : "•" });

            var actualSize = isSubBullet ? fontSize - 100 : fontSize;
            var run = new D.Run(
                new D.RunProperties(
                    new D.SolidFill(new D.RgbColorModelHex { Val = fontColor }),
                    new D.LatinFont { Typeface = fontFamily },
                    new D.EastAsianFont { Typeface = fontFamily })
                { Language = "ko-KR", FontSize = actualSize },
                new D.Text(cleanLine));

            para.Append(pProps);
            para.Append(run);
            body.Append(para);
        }

        shape.TextBody = body;
        return shape;
    }

    public static P.Shape CreateFilledRect(long x, long y, long cx, long cy, string fillColor)
    {
        var shape = new P.Shape();
        shape.NonVisualShapeProperties = new P.NonVisualShapeProperties(
            new P.NonVisualDrawingProperties { Id = GetNextShapeId(), Name = "Rect" },
            new P.NonVisualShapeDrawingProperties(),
            new ApplicationNonVisualDrawingProperties());

        shape.ShapeProperties = new P.ShapeProperties(
            new D.Transform2D(
                new D.Offset { X = x, Y = y },
                new D.Extents { Cx = cx, Cy = cy }),
            new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Rectangle },
            new D.SolidFill(new D.RgbColorModelHex { Val = fillColor }),
            new D.Outline(new D.NoFill()));

        shape.TextBody = new P.TextBody(
            new D.BodyProperties(),
            new D.ListStyle(),
            new D.Paragraph());

        return shape;
    }

    public static P.GraphicFrame CreateTableFrame(
        long x, long y, long cx,
        string[] headers,
        string[][] rows,
        long rowHeight = 370000,
        string headerBgColor = "1C2028",
        string headerFontColor = "FFFFFF",
        string evenRowColor = "F5F6FA",
        string oddRowColor = "FFFFFF",
        int fontSize = 1400)
    {
        var colWidth = cx / headers.Length;
        var totalHeight = rowHeight * (rows.Length + 1);

        var tbl = new D.Table();
        tbl.Append(new D.TableProperties { FirstRow = true, BandRow = true });

        var tblGrid = new D.TableGrid();
        foreach (var _ in headers)
            tblGrid.Append(new D.GridColumn { Width = colWidth });
        tbl.Append(tblGrid);

        var headerRow = new D.TableRow { Height = rowHeight };
        foreach (var h in headers)
            headerRow.Append(CreateTableCell(h, fontSize, headerFontColor, true, "맑은 고딕", headerBgColor));
        tbl.Append(headerRow);

        for (int i = 0; i < rows.Length; i++)
        {
            var dataRow = new D.TableRow { Height = rowHeight };
            var bgColor = i % 2 == 0 ? evenRowColor : oddRowColor;
            foreach (var cellText in rows[i])
                dataRow.Append(CreateTableCell(cellText, fontSize, "333333", false, "맑은 고딕", bgColor));
            tbl.Append(dataRow);
        }

        var graphicFrame = new P.GraphicFrame();
        graphicFrame.NonVisualGraphicFrameProperties = new P.NonVisualGraphicFrameProperties(
            new P.NonVisualDrawingProperties { Id = GetNextShapeId(), Name = "Table" },
            new P.NonVisualGraphicFrameDrawingProperties(new D.GraphicFrameLocks { NoGrouping = true }),
            new ApplicationNonVisualDrawingProperties());

        graphicFrame.Transform = new P.Transform(
            new D.Offset { X = x, Y = y },
            new D.Extents { Cx = cx, Cy = totalHeight });

        graphicFrame.Graphic = new D.Graphic(
            new D.GraphicData(tbl) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/table" });

        return graphicFrame;
    }

    private static D.TableCell CreateTableCell(string text, int fontSize, string fontColor, bool bold, string fontFamily, string bgColor)
    {
        var cell = new D.TableCell();

        var para = new D.Paragraph(
            new D.ParagraphProperties { Alignment = D.TextAlignmentTypeValues.Center },
            new D.Run(
                new D.RunProperties(
                    new D.SolidFill(new D.RgbColorModelHex { Val = fontColor }),
                    new D.LatinFont { Typeface = fontFamily },
                    new D.EastAsianFont { Typeface = fontFamily })
                { Language = "ko-KR", FontSize = fontSize, Bold = bold },
                new D.Text(text)));

        cell.Append(new D.TextBody(
            new D.BodyProperties { Anchor = D.TextAnchoringTypeValues.Center },
            new D.ListStyle(),
            para));

        var tcProps = new D.TableCellProperties();
        tcProps.LeftMargin = 91440;
        tcProps.RightMargin = 91440;
        tcProps.TopMargin = 45720;
        tcProps.BottomMargin = 45720;

        tcProps.Append(new D.LeftBorderLineProperties(new D.NoFill()) { Width = 12700 });
        tcProps.Append(new D.RightBorderLineProperties(new D.NoFill()) { Width = 12700 });
        tcProps.Append(new D.TopBorderLineProperties(new D.NoFill()) { Width = 12700 });
        tcProps.Append(new D.BottomBorderLineProperties(
            new D.SolidFill(new D.RgbColorModelHex { Val = "E0E0E0" }),
            new D.PresetDash { Val = D.PresetLineDashValues.Solid }) { Width = 12700 });
        tcProps.Append(new D.SolidFill(new D.RgbColorModelHex { Val = bgColor }));

        cell.Append(tcProps);

        return cell;
    }

    public static void AddHeaderBar(SlidePart slidePart, string title, string bgColor = "1C2028", long barHeight = 700000)
    {
        var tree = slidePart.Slide.CommonSlideData!.ShapeTree!;
        tree.Append(CreateFilledRect(0, 0, SlideWidth, barHeight, bgColor));
        tree.Append(CreateTextBox(
            EmuCm * 2, 140000, SlideWidth - EmuCm * 4, barHeight - 200000,
            title, fontSize: 2400, fontColor: "FFFFFF", bold: true,
            anchor: D.TextAnchoringTypeValues.Center));
    }

    public static void SetSlideBackground(SlidePart slidePart, string color)
    {
        var bg = new P.Background(
            new P.BackgroundProperties(
                new D.SolidFill(new D.RgbColorModelHex { Val = color })));
        slidePart.Slide.CommonSlideData!.InsertBefore(bg, slidePart.Slide.CommonSlideData.ShapeTree);
    }

    private static D.Paragraph CreateParagraph(string text, int fontSize, string fontColor, bool bold, string fontFamily, D.TextAlignmentTypeValues align)
    {
        var runProps = new D.RunProperties(
            new D.SolidFill(new D.RgbColorModelHex { Val = fontColor }),
            new D.LatinFont { Typeface = fontFamily },
            new D.EastAsianFont { Typeface = fontFamily })
        {
            Language = "ko-KR",
            FontSize = fontSize,
            Bold = bold
        };

        return new D.Paragraph(
            new D.ParagraphProperties { Alignment = align },
            new D.Run(runProps, new D.Text(text)));
    }

    private static uint _shapeIdCounter = 2;
    private static uint GetNextShapeId() => _shapeIdCounter++;
}
