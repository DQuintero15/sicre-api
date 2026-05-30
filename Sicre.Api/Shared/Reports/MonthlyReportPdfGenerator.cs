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
        public const string BrandBg = "#dbeafe";

        public const string GreenText = "#15803d";
        public const string GreenBg = "#dcfce7";
        public const string Green = "#22c55e";

        public const string OrangeText = "#c2410c";
        public const string OrangeBg = "#ffedd5";
        public const string Orange = "#f97316";

        public const string RedText = "#b91c1c";
        public const string RedBg = "#fee2e2";
        public const string Red = "#ef4444";

        public const string YellowText = "#854d0e";
        public const string YellowBg = "#fef9c3";
        public const string Yellow = "#eab308";

        public const string TextMain = "#0f172a";
        public const string TextMuted = "#64748b";
        public const string HeaderBg = "#f1f5f9";
        public const string RowAlt = "#f8fafc";
        public const string Border = "#e2e8f0";
        public const string White = "#ffffff";
    }

    private static (string text, string bg, string accent) RateColors(double rate) =>
        rate >= 75 ? (P.GreenText, P.GreenBg, P.Green)
        : rate >= 50 ? (P.YellowText, P.YellowBg, P.Yellow)
        : (P.RedText, P.RedBg, P.Red);

    public byte[] Generate(MonthlyReportData data)
    {
        return Document
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
    }

    // ─── Page Header ────────────────────────────────────────────────────

    private static void Header(IContainer c, MonthlyReportData data)
    {
        c.Background(P.Brand)
            .PaddingHorizontal(1.8f, Unit.Centimetre)
            .PaddingVertical(16)
            .Row(row =>
            {
                // Logo Llanogas
                var leftLogo = row.ConstantItem(130).AlignMiddle();
                if (data.LogoLlanogas != null)
                {
                    try
                    {
                        leftLogo.MaxHeight(40).Image(data.LogoLlanogas);
                    }
                    catch
                    { /* logo unavailable */
                    }
                }

                // Title block
                row.RelativeItem()
                    .AlignMiddle()
                    .Column(col =>
                    {
                        col.Item()
                            .AlignCenter()
                            .DefaultTextStyle(ts => ts.FontSize(14).Bold().FontColor(P.White))
                            .Text("INFORME MENSUAL DE CUMPLIMIENTO");

                        col.Item()
                            .PaddingTop(3)
                            .AlignCenter()
                            .DefaultTextStyle(ts => ts.FontSize(10).FontColor("#93c5fd"))
                            .Text(data.PeriodLabel.ToUpperInvariant());

                        col.Item()
                            .PaddingTop(3)
                            .AlignCenter()
                            .DefaultTextStyle(ts => ts.FontSize(7.5f).FontColor("#bfdbfe"))
                            .Text(data.GeneratedAt);
                    });

                // Logo Cusianagas
                var rightLogo = row.ConstantItem(100).AlignMiddle().AlignRight();
                if (data.LogoCusianagas != null)
                {
                    try
                    {
                        rightLogo.MaxHeight(36).Image(data.LogoCusianagas);
                    }
                    catch
                    { /* logo unavailable */
                    }
                }
            });
    }

    // ─── Page Content ───────────────────────────────────────────────────

    private static void Content(IContainer c, MonthlyReportData data)
    {
        c.Column(col =>
        {
            // Resumen Ejecutivo
            col.Item().Element(c => KpiSummary(c, data.StateDistribution));

            // Tendencia de Cumplimiento
            if (data.Trend.Count > 0)
                col.Item().PaddingTop(22).Element(c => TrendSection(c, data.Trend));

            // Por Entidad de Control
            if (data.ByEntity.Count > 0)
                col.Item().PaddingTop(22).Element(c => EntitySection(c, data.ByEntity));

            // Por Sede
            if (data.ByBranch.Count > 0)
                col.Item().PaddingTop(22).Element(c => BranchSection(c, data.ByBranch));

            // Por Responsable
            if (data.ByResponsible.Count > 0)
                col.Item().PaddingTop(22).Element(c => ResponsibleSection(c, data.ByResponsible));

            // Nota de cierre
            col.Item()
                .PaddingTop(28)
                .DefaultTextStyle(ts => ts.FontSize(7.5f).Italic().FontColor(P.TextMuted))
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
                    KpiCard(row, "TOTAL", dist.Total, P.Brand, P.BrandBg, dist.Total);
                    KpiCard(row, "A TIEMPO", dist.OnTime, P.GreenText, P.GreenBg, dist.Total);
                    KpiCard(row, "TARDE", dist.Late, P.OrangeText, P.OrangeBg, dist.Total);
                    KpiCard(row, "NO REPORTADO", dist.Overdue, P.RedText, P.RedBg, dist.Total);
                    KpiCard(row, "PENDIENTE", dist.Pending, P.YellowText, P.YellowBg, dist.Total);
                });

            // Visual breakdown bar
            if (dist.Total > 0)
            {
                col.Item()
                    .PaddingTop(10)
                    .Height(7)
                    .Row(r =>
                    {
                        if (dist.OnTime > 0)
                            r.RelativeItem(dist.OnTime).Background(P.Green);
                        if (dist.Late > 0)
                            r.RelativeItem(dist.Late).Background(P.Orange);
                        if (dist.Overdue > 0)
                            r.RelativeItem(dist.Overdue).Background(P.Red);
                        if (dist.Pending > 0)
                            r.RelativeItem(dist.Pending).Background(P.Yellow);
                    });

                col.Item()
                    .PaddingTop(4)
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

                        Legend(P.Green, "A Tiempo");
                        Legend(P.Orange, "Tarde");
                        Legend(P.Red, "No Reportado");
                        Legend(P.Yellow, "Pendiente");
                    });
            }
        });
    }

    private static void KpiCard(
        RowDescriptor row,
        string label,
        int value,
        string color,
        string bg,
        int total
    )
    {
        var pct = total > 0 ? value * 100.0 / total : 0.0;

        row.RelativeItem()
            .Padding(3)
            .Column(col =>
            {
                col.Item().Height(4).Background(color);
                col.Item()
                    .Background(bg)
                    .Border(1)
                    .BorderColor(P.Border)
                    .PaddingHorizontal(10)
                    .PaddingTop(10)
                    .PaddingBottom(12)
                    .Column(card =>
                    {
                        card.Item()
                            .DefaultTextStyle(ts => ts.FontSize(22).Bold().FontColor(color))
                            .Text(value.ToString());

                        card.Item()
                            .PaddingTop(2)
                            .DefaultTextStyle(ts => ts.FontSize(7.5f).Bold().FontColor(color))
                            .Text(label);

                        card.Item()
                            .PaddingTop(4)
                            .DefaultTextStyle(ts => ts.FontSize(7.5f).FontColor(P.TextMuted))
                            .Text($"{pct:F1}% del total");
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
                        cols.RelativeColumn(2.2f); // mes
                        cols.RelativeColumn(1f); // total
                        cols.RelativeColumn(1f); // a tiempo
                        cols.RelativeColumn(1f); // tarde
                        cols.RelativeColumn(1f); // no rep
                        cols.RelativeColumn(1f); // pendiente
                        cols.RelativeColumn(1.3f); // % cumpl
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
                        var (rateText, _, _) = RateColors(t.OnTimePercentage);

                        DataCell(table.Cell(), bg, t.Month);
                        DataCell(table.Cell(), bg, t.Total.ToString(), center: true);
                        DataCell(
                            table.Cell(),
                            bg,
                            t.OnTime.ToString(),
                            center: true,
                            color: P.GreenText
                        );
                        DataCell(
                            table.Cell(),
                            bg,
                            t.Late.ToString(),
                            center: true,
                            color: t.Late > 0 ? P.OrangeText : null
                        );
                        DataCell(
                            table.Cell(),
                            bg,
                            t.Overdue.ToString(),
                            center: true,
                            color: t.Overdue > 0 ? P.RedText : null
                        );
                        DataCell(
                            table.Cell(),
                            bg,
                            t.Pending.ToString(),
                            center: true,
                            color: t.Pending > 0 ? P.YellowText : null
                        );
                        DataCell(
                            table.Cell(),
                            bg,
                            $"{t.OnTimePercentage:F1}%",
                            center: true,
                            color: rateText,
                            bold: true
                        );
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
                        cols.ConstantColumn(22); // #
                        cols.RelativeColumn(3f); // entidad
                        cols.RelativeColumn(1f); // total
                        cols.RelativeColumn(1f); // a tiempo
                        cols.RelativeColumn(1f); // tarde
                        cols.RelativeColumn(1f); // no rep
                        cols.RelativeColumn(1f); // pendiente
                        cols.RelativeColumn(1.3f); // % cumpl
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
                        var (rateText, _, _) = RateColors(item.OnTimeRate);

                        DataCell(table.Cell(), bg, i++.ToString(), center: true);
                        DataCell(table.Cell(), bg, item.EntityName, bold: true);
                        DataCell(table.Cell(), bg, item.Total.ToString(), center: true);
                        DataCell(
                            table.Cell(),
                            bg,
                            item.OnTime.ToString(),
                            center: true,
                            color: P.GreenText
                        );
                        DataCell(
                            table.Cell(),
                            bg,
                            item.Late.ToString(),
                            center: true,
                            color: item.Late > 0 ? P.OrangeText : null
                        );
                        DataCell(
                            table.Cell(),
                            bg,
                            item.Overdue.ToString(),
                            center: true,
                            color: item.Overdue > 0 ? P.RedText : null
                        );
                        DataCell(
                            table.Cell(),
                            bg,
                            item.Pending.ToString(),
                            center: true,
                            color: item.Pending > 0 ? P.YellowText : null
                        );
                        DataCell(
                            table.Cell(),
                            bg,
                            $"{item.OnTimeRate:F1}%",
                            center: true,
                            color: rateText,
                            bold: true
                        );
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
                        var (rateText, _, _) = RateColors(item.OnTimeRate);

                        DataCell(table.Cell(), bg, i++.ToString(), center: true);
                        DataCell(table.Cell(), bg, item.BranchName, bold: true);
                        DataCell(table.Cell(), bg, item.Total.ToString(), center: true);
                        DataCell(
                            table.Cell(),
                            bg,
                            item.OnTime.ToString(),
                            center: true,
                            color: P.GreenText
                        );
                        DataCell(
                            table.Cell(),
                            bg,
                            item.Late.ToString(),
                            center: true,
                            color: item.Late > 0 ? P.OrangeText : null
                        );
                        DataCell(
                            table.Cell(),
                            bg,
                            item.Overdue.ToString(),
                            center: true,
                            color: item.Overdue > 0 ? P.RedText : null
                        );
                        DataCell(
                            table.Cell(),
                            bg,
                            $"{item.OnTimeRate:F1}%",
                            center: true,
                            color: rateText,
                            bold: true
                        );
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
                        var (rateText, _, _) = RateColors(item.OnTimeRate);

                        DataCell(table.Cell(), bg, i++.ToString(), center: true);
                        DataCell(table.Cell(), bg, item.ResponsibleName, bold: true);
                        DataCell(table.Cell(), bg, item.Total.ToString(), center: true);
                        DataCell(
                            table.Cell(),
                            bg,
                            item.OnTime.ToString(),
                            center: true,
                            color: P.GreenText
                        );
                        DataCell(
                            table.Cell(),
                            bg,
                            item.Late.ToString(),
                            center: true,
                            color: item.Late > 0 ? P.OrangeText : null
                        );
                        DataCell(
                            table.Cell(),
                            bg,
                            item.Overdue.ToString(),
                            center: true,
                            color: item.Overdue > 0 ? P.RedText : null
                        );
                        DataCell(
                            table.Cell(),
                            bg,
                            $"{item.OnTimeRate:F1}%",
                            center: true,
                            color: rateText,
                            bold: true
                        );
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
                    .DefaultTextStyle(ts => ts.FontSize(7.5f).FontColor(P.TextMuted))
                    .Text("SICRE · Sistema de Consolidación y Reporte a Entidades");

                row.ConstantItem(70)
                    .AlignRight()
                    .Text(t =>
                    {
                        t.DefaultTextStyle(ts => ts.FontSize(7.5f).FontColor(P.TextMuted));
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
                row.ConstantItem(4).Background(P.Brand);
                row.RelativeItem()
                    .PaddingLeft(8)
                    .PaddingVertical(6)
                    .DefaultTextStyle(ts => ts.FontSize(10.5f).Bold().FontColor(P.Brand))
                    .Text(title);
            });

    private static void TableHeader(TableCellDescriptor h, params string[] columns)
    {
        foreach (var col in columns)
        {
            h.Cell()
                .Background(P.Brand)
                .PaddingHorizontal(6)
                .PaddingVertical(7)
                .DefaultTextStyle(ts => ts.FontSize(7.5f).Bold().FontColor(P.White))
                .Text(col);
        }
    }

    private static void DataCell(
        IContainer cell,
        string bg,
        string text,
        bool center = false,
        string? color = null,
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
                ts = ts.FontSize(8.5f);
                if (color != null)
                    ts = ts.FontColor(color);
                if (bold)
                    ts = ts.SemiBold();
                return ts;
            })
            .Text(text);
    }
}
