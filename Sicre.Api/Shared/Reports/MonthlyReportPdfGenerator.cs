using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sicre.Api.Features.Analytics.DTOs;

namespace Sicre.Api.Shared.Reports;

public class MonthlyReportPdfGenerator
{
    private static class P
    {
        public const string Brand = "#1d3e81";

        // Status badge colors — subtle tints only
        public const string GreenText = "#15803d";
        public const string GreenBg = "#f0fdf4";
        public const string OrangeText = "#c2410c";
        public const string OrangeBg = "#fff7ed";
        public const string RedText = "#b91c1c";
        public const string RedBg = "#fef2f2";
        public const string YellowText = "#854d0e";
        public const string YellowBg = "#fffbeb";

        // Muted bar-segment colors
        public const string BarGreen = "#86efac";
        public const string BarOrange = "#fdba74";
        public const string BarRed = "#fca5a5";
        public const string BarYellow = "#fde047";

        public const string TextMain = "#1e293b";
        public const string TextMuted = "#64748b";
        public const string TextLight = "#94a3b8";
        public const string TableHeaderBg = "#f1f5f9";
        public const string RowAlt = "#fafafa";
        public const string Border = "#e2e8f0";
        public const string White = "#ffffff";
    }

    private static (string text, string bg) RateColors(double rate) =>
        rate >= 75
            ? (P.GreenText, P.GreenBg)
            : rate >= 50
                ? (P.YellowText, P.YellowBg)
                : (P.RedText, P.RedBg);

    public byte[] Generate(MonthlyReportData data) =>
        Document
            .Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(P.TextMain));

                    page.Header().Element(c => Header(c, data));
                    page.Content()
                        .PaddingHorizontal(1.8f, Unit.Centimetre)
                        .PaddingTop(20)
                        .Element(c => Content(c, data));
                    page.Footer()
                        .PaddingHorizontal(1.8f, Unit.Centimetre)
                        .PaddingBottom(1.2f, Unit.Centimetre)
                        .Element(Footer);
                });
            })
            .GeneratePdf();

    // ─── Page Header ────────────────────────────────────────────────────

    private static void Header(IContainer c, MonthlyReportData data)
    {
        c.Background(P.White)
            .BorderBottom(1)
            .BorderColor(P.Border)
            .PaddingHorizontal(1.8f, Unit.Centimetre)
            .PaddingVertical(14)
            .Row(row =>
            {
                // Logo Llanogas
                row.ConstantItem(110).AlignMiddle().Element(logo =>
                {
                    if (data.LogoLlanogas != null)
                        try { logo.MaxHeight(38).Image(data.LogoLlanogas); } catch { }
                });

                // Title block
                row.RelativeItem()
                    .AlignMiddle()
                    .Column(col =>
                    {
                        col.Item()
                            .AlignCenter()
                            .DefaultTextStyle(ts => ts.FontSize(13).Bold().FontColor(P.Brand))
                            .Text("INFORME MENSUAL DE CUMPLIMIENTO");

                        col.Item()
                            .PaddingTop(3)
                            .AlignCenter()
                            .DefaultTextStyle(ts => ts.FontSize(9.5f).FontColor(P.TextMuted))
                            .Text(data.PeriodLabel.ToUpperInvariant());

                        col.Item()
                            .PaddingTop(2)
                            .AlignCenter()
                            .DefaultTextStyle(ts => ts.FontSize(7.5f).FontColor(P.TextLight))
                            .Text(data.GeneratedAt);
                    });

                // Logo Cusianagas
                row.ConstantItem(110).AlignMiddle().AlignRight().Element(logo =>
                {
                    if (data.LogoCusianagas != null)
                        try { logo.MaxHeight(38).Image(data.LogoCusianagas); } catch { }
                });
            });
    }

    // ─── Page Content ───────────────────────────────────────────────────

    private static void Content(IContainer c, MonthlyReportData data)
    {
        c.Column(col =>
        {
            col.Item().Element(c2 => KpiSummary(c2, data.StateDistribution));

            if (data.Trend.Count > 0)
                col.Item().PaddingTop(22).Element(c2 => TrendSection(c2, data.Trend));

            if (data.ByEntity.Count > 0)
                col.Item().PaddingTop(22).Element(c2 => EntitySection(c2, data.ByEntity));

            if (data.ByBranch.Count > 0)
                col.Item().PaddingTop(22).Element(c2 => BranchSection(c2, data.ByBranch));

            if (data.ByResponsible.Count > 0)
                col.Item()
                    .PaddingTop(22)
                    .Element(c2 => ResponsibleSection(c2, data.ByResponsible));

            col.Item()
                .PaddingTop(28)
                .DefaultTextStyle(ts => ts.FontSize(7.5f).Italic().FontColor(P.TextLight))
                .Text(
                    "Este informe fue generado automáticamente por SICRE · "
                        + "Los datos corresponden a las instancias registradas para el período indicado."
                );
        });
    }

    // ─── KPI Summary ────────────────────────────────────────────────────

    private static void KpiSummary(IContainer c, StateDistributionDto dist)
    {
        c.Column(col =>
        {
            col.Item().Element(SectionTitle("Resumen Ejecutivo"));

            col.Item()
                .PaddingTop(12)
                .Row(row =>
                {
                    KpiCard(row, "TOTAL", dist.Total, P.Brand, dist.Total);
                    KpiCard(row, "A TIEMPO", dist.OnTime, P.BarGreen, dist.Total);
                    KpiCard(row, "TARDE", dist.Late, P.BarOrange, dist.Total);
                    KpiCard(row, "NO REPORTADO", dist.Overdue, P.BarRed, dist.Total);
                    KpiCard(row, "PENDIENTE", dist.Pending, P.BarYellow, dist.Total);
                });

            if (dist.Total > 0)
            {
                col.Item()
                    .PaddingTop(10)
                    .Height(5)
                    .Row(r =>
                    {
                        if (dist.OnTime > 0)
                            r.RelativeItem(dist.OnTime).Background(P.BarGreen);
                        if (dist.Late > 0)
                            r.RelativeItem(dist.Late).Background(P.BarOrange);
                        if (dist.Overdue > 0)
                            r.RelativeItem(dist.Overdue).Background(P.BarRed);
                        if (dist.Pending > 0)
                            r.RelativeItem(dist.Pending).Background(P.BarYellow);
                    });

                col.Item()
                    .PaddingTop(5)
                    .Row(r =>
                    {
                        void Legend(string color, string label) =>
                            r.AutoItem()
                                .PaddingRight(14)
                                .Row(inner =>
                                {
                                    inner
                                        .ConstantItem(8)
                                        .AlignMiddle()
                                        .Height(8)
                                        .Width(8)
                                        .Background(color);
                                    inner
                                        .AutoItem()
                                        .PaddingLeft(4)
                                        .DefaultTextStyle(ts =>
                                            ts.FontSize(7.5f).FontColor(P.TextMuted)
                                        )
                                        .Text(label);
                                });

                        Legend(P.BarGreen, "A Tiempo");
                        Legend(P.BarOrange, "Tarde");
                        Legend(P.BarRed, "No Reportado");
                        Legend(P.BarYellow, "Pendiente");
                    });
            }
        });
    }

    private static void KpiCard(
        RowDescriptor row,
        string label,
        int value,
        string accentColor,
        int total
    )
    {
        var pct = total > 0 ? value * 100.0 / total : 0.0;

        row.RelativeItem()
            .Padding(3)
            .Border(1)
            .BorderColor(P.Border)
            .Column(col =>
            {
                col.Item().Height(3).Background(accentColor);
                col.Item()
                    .Background(P.White)
                    .PaddingHorizontal(10)
                    .PaddingTop(10)
                    .PaddingBottom(12)
                    .Column(card =>
                    {
                        card.Item()
                            .DefaultTextStyle(ts => ts.FontSize(22).Bold().FontColor(P.TextMain))
                            .Text(value.ToString());

                        card.Item()
                            .PaddingTop(2)
                            .DefaultTextStyle(ts => ts.FontSize(7f).Bold().FontColor(P.TextMuted))
                            .Text(label);

                        card.Item()
                            .PaddingTop(4)
                            .DefaultTextStyle(ts => ts.FontSize(7f).FontColor(P.TextLight))
                            .Text($"{pct:F1}%");
                    });
            });
    }

    // ─── Trend Table ────────────────────────────────────────────────────

    private static void TrendSection(IContainer c, List<ComplianceTrendDto> trend)
    {
        c.Column(col =>
        {
            col.Item().Element(SectionTitle("Tendencia de Cumplimiento — Últimos 12 Meses"));
            col.Item()
                .PaddingTop(10)
                .Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2.2f);
                        cols.RelativeColumn(1f);
                        cols.RelativeColumn(1f);
                        cols.RelativeColumn(1f);
                        cols.RelativeColumn(1f);
                        cols.RelativeColumn(1f);
                        cols.RelativeColumn(1.3f);
                    });

                    table.Header(h =>
                        TableHeader(
                            h,
                            "Mes",
                            "Total",
                            "A Tiempo",
                            "Tarde",
                            "No Report.",
                            "Pendiente",
                            "Cumplimiento"
                        )
                    );

                    bool alt = false;
                    foreach (var t in trend)
                    {
                        var bg = alt ? P.RowAlt : P.White;
                        alt = !alt;
                        var (rateText, rateBg) = RateColors(t.OnTimePercentage);

                        DataCell(table.Cell(), bg, t.Month);
                        DataCell(table.Cell(), bg, t.Total.ToString(), center: true);
                        DataCell(table.Cell(), bg, t.OnTime.ToString(), center: true);
                        DataCell(table.Cell(), bg, t.Late.ToString(), center: true);
                        DataCell(table.Cell(), bg, t.Overdue.ToString(), center: true);
                        DataCell(table.Cell(), bg, t.Pending.ToString(), center: true);
                        RateBadgeCell(table.Cell(), bg, $"{t.OnTimePercentage:F1}%", rateText, rateBg);
                    }
                });
        });
    }

    // ─── Entity Table ────────────────────────────────────────────────────

    private static void EntitySection(IContainer c, List<EntityComplianceDto> items)
    {
        c.Column(col =>
        {
            col.Item().Element(SectionTitle("Cumplimiento por Entidad de Control"));
            col.Item()
                .PaddingTop(10)
                .Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(22);
                        cols.RelativeColumn(3f);
                        cols.RelativeColumn(1f);
                        cols.RelativeColumn(1f);
                        cols.RelativeColumn(1f);
                        cols.RelativeColumn(1f);
                        cols.RelativeColumn(1f);
                        cols.RelativeColumn(1.3f);
                    });

                    table.Header(h =>
                        TableHeader(
                            h,
                            "#",
                            "Entidad de Control",
                            "Total",
                            "A Tiempo",
                            "Tarde",
                            "No Report.",
                            "Pendiente",
                            "Cumplimiento"
                        )
                    );

                    bool alt = false;
                    int i = 1;
                    foreach (var item in items.OrderByDescending(x => x.OnTimeRate))
                    {
                        var bg = alt ? P.RowAlt : P.White;
                        alt = !alt;
                        var (rateText, rateBg) = RateColors(item.OnTimeRate);

                        DataCell(table.Cell(), bg, i++.ToString(), center: true);
                        DataCell(table.Cell(), bg, item.EntityName, bold: true);
                        DataCell(table.Cell(), bg, item.Total.ToString(), center: true);
                        DataCell(table.Cell(), bg, item.OnTime.ToString(), center: true);
                        DataCell(table.Cell(), bg, item.Late.ToString(), center: true);
                        DataCell(table.Cell(), bg, item.Overdue.ToString(), center: true);
                        DataCell(table.Cell(), bg, item.Pending.ToString(), center: true);
                        RateBadgeCell(table.Cell(), bg, $"{item.OnTimeRate:F1}%", rateText, rateBg);
                    }
                });
        });
    }

    // ─── Branch Table ────────────────────────────────────────────────────

    private static void BranchSection(IContainer c, List<BranchComplianceDto> items)
    {
        c.Column(col =>
        {
            col.Item().Element(SectionTitle("Cumplimiento por Sede"));
            col.Item()
                .PaddingTop(10)
                .Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(22);
                        cols.RelativeColumn(3f);
                        cols.RelativeColumn(1f);
                        cols.RelativeColumn(1f);
                        cols.RelativeColumn(1f);
                        cols.RelativeColumn(1f);
                        cols.RelativeColumn(1.3f);
                    });

                    table.Header(h =>
                        TableHeader(
                            h,
                            "#",
                            "Sede",
                            "Total",
                            "A Tiempo",
                            "Tarde",
                            "No Report.",
                            "Cumplimiento"
                        )
                    );

                    bool alt = false;
                    int i = 1;
                    foreach (var item in items.OrderByDescending(x => x.OnTimeRate))
                    {
                        var bg = alt ? P.RowAlt : P.White;
                        alt = !alt;
                        var (rateText, rateBg) = RateColors(item.OnTimeRate);

                        DataCell(table.Cell(), bg, i++.ToString(), center: true);
                        DataCell(table.Cell(), bg, item.BranchName, bold: true);
                        DataCell(table.Cell(), bg, item.Total.ToString(), center: true);
                        DataCell(table.Cell(), bg, item.OnTime.ToString(), center: true);
                        DataCell(table.Cell(), bg, item.Late.ToString(), center: true);
                        DataCell(table.Cell(), bg, item.Overdue.ToString(), center: true);
                        RateBadgeCell(table.Cell(), bg, $"{item.OnTimeRate:F1}%", rateText, rateBg);
                    }
                });
        });
    }

    // ─── Responsible Table ───────────────────────────────────────────────

    private static void ResponsibleSection(IContainer c, List<ResponsibleComplianceDto> items)
    {
        var top = items.OrderByDescending(x => x.OnTimeRate).Take(20).ToList();
        var title =
            items.Count > 20
                ? $"Cumplimiento por Responsable — Top 20 de {items.Count}"
                : "Cumplimiento por Responsable";

        c.Column(col =>
        {
            col.Item().Element(SectionTitle(title));
            col.Item()
                .PaddingTop(10)
                .Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(22);
                        cols.RelativeColumn(3.5f);
                        cols.RelativeColumn(1f);
                        cols.RelativeColumn(1f);
                        cols.RelativeColumn(1f);
                        cols.RelativeColumn(1f);
                        cols.RelativeColumn(1.3f);
                    });

                    table.Header(h =>
                        TableHeader(
                            h,
                            "#",
                            "Responsable",
                            "Total",
                            "A Tiempo",
                            "Tarde",
                            "No Report.",
                            "Cumplimiento"
                        )
                    );

                    bool alt = false;
                    int i = 1;
                    foreach (var item in top)
                    {
                        var bg = alt ? P.RowAlt : P.White;
                        alt = !alt;
                        var (rateText, rateBg) = RateColors(item.OnTimeRate);

                        DataCell(table.Cell(), bg, i++.ToString(), center: true);
                        DataCell(table.Cell(), bg, item.ResponsibleName, bold: true);
                        DataCell(table.Cell(), bg, item.Total.ToString(), center: true);
                        DataCell(table.Cell(), bg, item.OnTime.ToString(), center: true);
                        DataCell(table.Cell(), bg, item.Late.ToString(), center: true);
                        DataCell(table.Cell(), bg, item.Overdue.ToString(), center: true);
                        RateBadgeCell(table.Cell(), bg, $"{item.OnTimeRate:F1}%", rateText, rateBg);
                    }
                });
        });
    }

    // ─── Page Footer ────────────────────────────────────────────────────

    private static void Footer(IContainer c)
    {
        c.BorderTop(1)
            .BorderColor(P.Border)
            .PaddingTop(8)
            .Row(row =>
            {
                row.RelativeItem()
                    .DefaultTextStyle(ts => ts.FontSize(7.5f).FontColor(P.TextLight))
                    .Text("SICRE · Sistema de Consolidación y Reporte a Entidades");

                row.ConstantItem(70)
                    .AlignRight()
                    .Text(t =>
                    {
                        t.DefaultTextStyle(ts => ts.FontSize(7.5f).FontColor(P.TextLight));
                        t.Span("Página ");
                        t.CurrentPageNumber();
                        t.Span(" de ");
                        t.TotalPages();
                    });
            });
    }

    // ─── Shared helpers ──────────────────────────────────────────────────

    private static Action<IContainer> SectionTitle(string title) =>
        c =>
            c.Row(row =>
            {
                row.ConstantItem(3).Background(P.Brand);
                row.RelativeItem()
                    .PaddingLeft(8)
                    .PaddingVertical(5)
                    .DefaultTextStyle(ts => ts.FontSize(10f).Bold().FontColor(P.Brand))
                    .Text(title);
            });

    private static void TableHeader(TableCellDescriptor h, params string[] columns)
    {
        foreach (var col in columns)
        {
            h.Cell()
                .Background(P.TableHeaderBg)
                .BorderBottom(2)
                .BorderColor(P.Brand)
                .PaddingHorizontal(6)
                .PaddingVertical(7)
                .DefaultTextStyle(ts => ts.FontSize(7.5f).SemiBold().FontColor(P.TextMain))
                .Text(col);
        }
    }

    private static void DataCell(
        IContainer cell,
        string bg,
        string text,
        bool center = false,
        bool bold = false
    )
    {
        var c = cell.Background(bg)
            .BorderBottom(1)
            .BorderColor(P.Border)
            .PaddingHorizontal(6)
            .PaddingVertical(5.5f);

        if (center)
            c = c.AlignCenter();

        c.DefaultTextStyle(ts =>
            {
                ts = ts.FontSize(8.5f).FontColor(P.TextMain);
                if (bold)
                    ts = ts.SemiBold();
                return ts;
            })
            .Text(text);
    }

    private static void RateBadgeCell(
        IContainer cell,
        string rowBg,
        string text,
        string textColor,
        string badgeBg
    )
    {
        cell.Background(rowBg)
            .BorderBottom(1)
            .BorderColor(P.Border)
            .PaddingHorizontal(4)
            .PaddingVertical(4)
            .AlignCenter()
            .AlignMiddle()
            .Background(rowBg)
            .Element(inner =>
                inner
                    .Background(badgeBg)
                    .PaddingHorizontal(6)
                    .PaddingVertical(2)
                    .DefaultTextStyle(ts => ts.FontSize(8f).SemiBold().FontColor(textColor))
                    .Text(text)
            );
    }
}
