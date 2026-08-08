/* =====================================================================
   admin.js — Smart Campus Admin Panel
   Bootstrap 5.1.0 + Highcharts
   ===================================================================== */

/* ── Highcharts chart instance registry ─────────────────────────────── */
const _hcRegistry = {};

/* ── Highcharts global dark theme ───────────────────────────────────── */
function applyHighchartsTheme() {
    if (typeof Highcharts === "undefined") return;

    Highcharts.setOptions({
        chart: {
            backgroundColor: "transparent",
            style: { fontFamily: "'Inter', 'Segoe UI', sans-serif" },
            animation: { duration: 600, easing: "easeOutQuart" },
        },
        title: { style: { color: "#1f2937", fontSize: "13px", fontWeight: "700" } },
        subtitle: { style: { color: "#4b5563" } },
        legend: {
            itemStyle: { color: "#4b5563", fontWeight: "500", fontSize: "12px" },
            itemHoverStyle: { color: "#1f2937" },
        },
        tooltip: {
            backgroundColor: "#ffffff",
            borderColor: "rgba(0,0,0,0.1)",
            borderRadius: 8,
            style: { color: "#1f2937", fontSize: "12px" },
            shadow: true,
        },
        credits: { enabled: false },
        exporting: { enabled: false },
    });
}

/* ── Desktop sidebar collapse toggle ────────────────────────────────── */
function toggleDesktopSidebar() {
    const sidebar = document.getElementById("desktopSidebar");
    if (!sidebar) return;
    sidebar.classList.toggle("collapsed");
    const isCollapsed = sidebar.classList.contains("collapsed");
    localStorage.setItem("adminSidebarCollapsed", isCollapsed ? "1" : "0");
}

/* ── Restore sidebar state on first page load ────────────────────────── */
document.addEventListener("DOMContentLoaded", function () {
    const sidebar = document.getElementById("desktopSidebar");
    if (sidebar && localStorage.getItem("adminSidebarCollapsed") === "1") {
        sidebar.classList.add("collapsed");
    }
    applyHighchartsTheme();
});

/* =====================================================================
   initCharts()
   ─────────────────────────────────────────────────────────────────────
   Called by Blazor from OnAfterRenderAsync via:
       await JS.InvokeVoidAsync("initCharts");

   Highcharts uses <div> containers — zero canvas/DOM race conditions.
   We destroy any existing instance first to prevent duplicate renders.
   ===================================================================== */
function initCharts(roleDistributionData, facultyDistributionData) {
    const rolePieData =
        Array.isArray(roleDistributionData) && roleDistributionData.length
            ? roleDistributionData
                .map((item) => ({
                    name: item.name || item.Name || "",
                    y: Number(item.y ?? item.Y ?? 0),
                    color: item.color || item.Color || undefined,
                }))
                .filter((item) => item.name && Number.isFinite(item.y) && item.y > 0)
            : [];

    const rawFacultyData =
        Array.isArray(facultyDistributionData) && facultyDistributionData.length
            ? facultyDistributionData
                .map((item) => ({
                    name: item.name || item.Name || "",
                    y: Number(item.y ?? item.Y ?? 0),
                    color: item.color || item.Color || undefined,
                }))
                .filter((item) => item.name)
            : [
                { name: "Faculty of Computing", y: 28, color: "#38bdf8" },
                { name: "Faculty of Engineering", y: 23, color: "#8b5cf6" }
            ];

    const facultyChartData = rawFacultyData.map(d => ({
        name: d.name,
        y: d.y,
        color: d.color || (d.name.toLowerCase().includes("comput") ? "#38bdf8" : (d.name.toLowerCase().includes("engin") ? "#8b5cf6" : "#10b981"))
    }));

    if (typeof Highcharts === "undefined") {
        renderFallbackRolePie(rolePieData);
        return;
    }

    applyHighchartsTheme();

    /* ── Helper: safely destroy & remove from registry ─────────────── */
    function destroyHC(id) {
        if (_hcRegistry[id] && typeof _hcRegistry[id].destroy === "function") {
            try {
                _hcRegistry[id].destroy();
            } catch (e) {
                /* already destroyed */
            }
            delete _hcRegistry[id];
        }
    }

    /* ── 1. Pie Chart — Role Distribution ────────────────────────── */
    const pieEl = document.getElementById("hc-pie-chart");
    if (pieEl) {
        destroyHC("hc-pie-chart");
        _hcRegistry["hc-pie-chart"] = Highcharts.chart("hc-pie-chart", {
            chart: {
                type: "pie",
                height: 260,
                margin: [0, 0, 20, -100],
                spacing: [10, 10, 10, 10],
            },
            title: { text: null },
            plotOptions: {
                pie: {
                    innerSize: "60%" /* donut style */,
                    allowPointSelect: true,
                    cursor: "pointer",
                    borderWidth: 0,
                    dataLabels: {
                        enabled: true,
                        format: "<b>{point.name}</b><br>{point.percentage:.1f}%",
                        style: { color: "#4b5563", fontSize: "11px", fontWeight: "500" },
                        distance: 18,
                    },
                    showInLegend: true,
                },
            },
            legend: {
                layout: "vertical",
                align: "right",
                verticalAlign: "middle",
                itemMarginBottom: 8,
                symbolRadius: 2,
            },
            series: [
                {
                    name: "Users",
                    colorByPoint: true,
                    data: rolePieData,
                },
            ],
            responsive: {
                rules: [
                    {
                        condition: { maxWidth: 100 },
                        chartOptions: {
                            legend: { enabled: false },
                        },
                    },
                ],
            },
        });
    }

    /* ── 2. Column/Bar Chart — Students by Faculty & Year ──────── */
    const barEl = document.getElementById("hc-bar-chart");
    if (barEl) {
        destroyHC("hc-bar-chart");

        let categories = ["2022-2023", "2023-2024", "2024-2025", "2025-2026", "2026-2027"];
        let seriesData = [
            { name: "Faculty of Computing (FC)", color: "#38bdf8", data: [9, 8, 9, 8, 9] },
            { name: "Faculty of Engineering (FE)", color: "#8b5cf6", data: [8, 7, 8, 7, 7] }
        ];

        if (facultyDistributionData && (facultyDistributionData.categories || facultyDistributionData.Categories)) {
            const rawCats = facultyDistributionData.categories || facultyDistributionData.Categories;
            const rawSeries = facultyDistributionData.series || facultyDistributionData.Series;
            if (Array.isArray(rawCats) && rawCats.length) categories = rawCats;
            if (Array.isArray(rawSeries) && rawSeries.length) {
                seriesData = rawSeries.map(s => ({
                    name: s.name || s.Name || "",
                    color: s.color || s.Color || ((s.name || "").includes("Computing") || (s.name || "").includes("FC") ? "#38bdf8" : "#8b5cf6"),
                    data: Array.isArray(s.data) ? s.data : (Array.isArray(s.Data) ? s.Data : [])
                }));
            }
        } else if (Array.isArray(facultyDistributionData) && facultyDistributionData.length) {
            categories = facultyDistributionData.map(item => item.name || item.Name || "");
            seriesData = [
                {
                    name: "Students",
                    colorByPoint: true,
                    data: facultyDistributionData.map(item => ({
                        name: item.name || item.Name || "",
                        y: Number(item.y ?? item.Y ?? 0),
                        color: item.color || item.Color || undefined
                    }))
                }
            ];
        }

        _hcRegistry["hc-bar-chart"] = Highcharts.chart("hc-bar-chart", {
            chart: {
                type: "column",
                height: 260,
            },
            title: { text: null },
            xAxis: {
                categories: categories,
                labels: { style: { color: "#1e293b", fontSize: "12px", fontWeight: "600" } },
                lineColor: "rgba(0,0,0,0.08)",
                tickColor: "rgba(0,0,0,0.08)",
            },
            yAxis: {
                min: 0,
                title: { text: "Students", style: { color: "#64748b", fontSize: "11px" } },
                labels: { style: { color: "#4b5563", fontSize: "11px" } },
                gridLineColor: "rgba(0,0,0,0.05)",
                gridLineDashStyle: "ShortDash",
            },
            legend: {
                enabled: true,
                align: "center",
                verticalAlign: "bottom",
                layout: "horizontal",
                itemStyle: { color: "#334155", fontSize: "12px", fontWeight: "600" }
            },
            plotOptions: {
                column: {
                    borderRadius: 6,
                    borderWidth: 0,
                    groupPadding: 0.15,
                    pointPadding: 0.05,
                    dataLabels: { 
                        enabled: true,
                        style: { fontSize: "11px", fontWeight: "bold", color: "#1e293b" }
                    },
                },
            },
            series: seriesData,
        });
    }
}

window.initCharts = initCharts;

function renderFallbackRolePie(rolePieData) {
    const pieEl = document.getElementById("hc-pie-chart");
    if (!pieEl) return;

    const data =
        rolePieData && rolePieData.length
            ? rolePieData
            : [
                { name: "Admin", y: 10, color: "#1b8a5a" },
                { name: "Student", y: 70, color: "#2563eb" },
                { name: "Tutor", y: 20, color: "#7c3aed" },
            ];

    let start = 0;
    const gradientStops = data
        .map((item) => {
            const end = start + item.y;
            const stop = `${item.color} ${start}% ${end}%`;
            start = end;
            return stop;
        })
        .join(", ");

    pieEl.innerHTML = `
        <div style="min-height:260px;display:flex;align-items:center;justify-content:center;gap:24px;flex-wrap:wrap;">
            <div style="width:180px;height:180px;border-radius:50%;background:conic-gradient(${gradientStops});position:relative;flex:0 0 auto;">
                <div style="position:absolute;inset:48px;border-radius:50%;background:#ffffff;"></div>
            </div>
            <div style="display:grid;gap:10px;min-width:140px;">
                ${data
            .map(
                (item) => `
                    <div style="display:flex;align-items:center;gap:8px;color:#4b5563;font-size:12px;">
                        <span style="width:10px;height:10px;border-radius:3px;background:${item.color};display:inline-block;"></span>
                        <span>${item.name}: ${Number(item.y).toFixed(0)}%</span>
                    </div>
                `,
            )
            .join("")}
            </div>
        </div>`;
}

/* ── Legacy alias ─────────────────────────────────────────────────────── */
function toggleSidebar() {
    toggleDesktopSidebar();
}
