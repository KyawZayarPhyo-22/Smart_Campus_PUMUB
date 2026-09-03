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
function initCharts(roleDistributionData, facultyDistributionData, lang) {
    const isMyanmar = (lang === "my" || lang === "mm");

    const roleColors = {
        "Admin": "#06b6d4",
        "ADMIN": "#06b6d4",
        "Super Admin": "#6366f1",
        "SUPER ADMIN": "#6366f1",
        "Student": "#10b981",
        "STUDENT": "#10b981",
        "Tutor": "#8b5cf6",
        "TUTOR": "#8b5cf6"
    };

    const roleOrder = {
        "Super Admin": 1,
        "SUPER ADMIN": 1,
        "Admin": 2,
        "ADMIN": 2,
        "Tutor": 3,
        "TUTOR": 3,
        "Student": 4,
        "STUDENT": 4
    };

    const rolePieData =
        Array.isArray(roleDistributionData) && roleDistributionData.length
            ? roleDistributionData
                .map((item) => {
                    const name = item.name || item.Name || "";
                    return {
                        name: name,
                        y: Number(item.y ?? item.Y ?? 0),
                        color: item.color || item.Color || roleColors[name] || "#3b82f6",
                    };
                })
                .filter((item) => item.name && Number.isFinite(item.y) && item.y > 0)
                .sort((a, b) => (roleOrder[a.name] || 99) - (roleOrder[b.name] || 99))
            : [
                { name: "Super Admin", y: 2, color: "#6366f1" },
                { name: "Admin", y: 5, color: "#06b6d4" },
                { name: "Tutor", y: 21, color: "#8b5cf6" },
                { name: "Student", y: 1, color: "#10b981" }
            ];

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

    /* ── 1. Pie / Donut Chart — Role Distribution ────────────────────────── */
    const pieEl = document.getElementById("hc-pie-chart");
    if (pieEl) {
        destroyHC("hc-pie-chart");
        _hcRegistry["hc-pie-chart"] = Highcharts.chart("hc-pie-chart", {
            chart: {
                type: "pie",
                height: 260,
                spacing: [10, 10, 10, 10],
            },
            title: { text: null },
            tooltip: {
                backgroundColor: "#ffffff",
                borderColor: "#e2e8f0",
                borderRadius: 10,
                shadow: true,
                style: { color: "#0f172a", fontSize: "12px" },
                pointFormat: isMyanmar 
                    ? "<b>{point.name}</b>: {point.y} ဦး (<b>{point.percentage:.1f}%</b>)"
                    : "<b>{point.name}</b>: {point.y} users (<b>{point.percentage:.1f}%</b>)"
            },
            plotOptions: {
                pie: {
                    innerSize: "62%" /* modern donut style */,
                    allowPointSelect: true,
                    cursor: "pointer",
                    borderWidth: 2,
                    borderColor: "#ffffff",
                    dataLabels: {
                        enabled: false, /* Clean donut chart - prevents text overlapping legend */
                    },
                    showInLegend: true,
                },
            },
            legend: {
                layout: "vertical",
                align: "right",
                verticalAlign: "middle",
                itemMarginBottom: 8,
                symbolRadius: 4,
                itemStyle: { color: "#334155", fontSize: "12px", fontWeight: "600" },
                itemHoverStyle: { color: "#0f172a" },
                labelFormat: "{name} ({percentage:.1f}%)",
            },
            series: [
                {
                    name: isMyanmar ? "အခန်းကဏ္ဍများ" : "Roles",
                    colorByPoint: true,
                    data: rolePieData,
                },
            ],
            responsive: {
                rules: [
                    {
                        condition: { maxWidth: 350 },
                        chartOptions: {
                            legend: {
                                layout: "horizontal",
                                align: "center",
                                verticalAlign: "bottom",
                            },
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
            { name: isMyanmar ? "ကွန်ပျူတာမဟာဌာန (FC)" : "Faculty of Computing (FC)", color: "#38bdf8", data: [0, 0, 0, 0, 1] },
            { name: isMyanmar ? "အင်ဂျင်နီယာမဟာဌာန (FE)" : "Faculty of Engineering (FE)", color: "#8b5cf6", data: [0, 0, 0, 0, 0] }
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
                    name: isMyanmar ? "ကျောင်းသားများ" : "Students",
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
                spacing: [10, 10, 10, 10],
            },
            title: { text: null },
            tooltip: {
                shared: true,
                backgroundColor: "#ffffff",
                borderColor: "#e2e8f0",
                borderRadius: 10,
                shadow: true,
                style: { color: "#0f172a", fontSize: "12px" },
                pointFormat: isMyanmar
                    ? '<span style="color:{series.color}">\u25CF</span> {series.name}: <b>{point.y} ဦး</b><br/>'
                    : '<span style="color:{series.color}">\u25CF</span> {series.name}: <b>{point.y} students</b><br/>'
            },
            xAxis: {
                categories: categories,
                labels: { style: { color: "#475569", fontSize: "11px", fontWeight: "600" } },
                lineColor: "rgba(0,0,0,0.08)",
                tickColor: "rgba(0,0,0,0.08)",
            },
            yAxis: {
                min: 0,
                title: { text: isMyanmar ? "ကျောင်းသားဦးရေ" : "Students", style: { color: "#64748b", fontSize: "11px" } },
                labels: { style: { color: "#64748b", fontSize: "11px" } },
                gridLineColor: "rgba(0,0,0,0.05)",
                gridLineDashStyle: "ShortDash",
            },
            legend: {
                enabled: true,
                align: "center",
                verticalAlign: "bottom",
                layout: "horizontal",
                itemStyle: { color: "#334155", fontSize: "12px", fontWeight: "600" },
                itemHoverStyle: { color: "#0f172a" }
            },
            plotOptions: {
                column: {
                    borderRadius: 6,
                    borderWidth: 0,
                    groupPadding: 0.18,
                    pointPadding: 0.05,
                    dataLabels: { 
                        enabled: true,
                        style: { fontSize: "11px", fontWeight: "700", color: "#1e293b", textOutline: "none" }
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

/* ── Grade Mark Input Enforcer (0 - 100 Range & No Leading Zeros) ────── */
document.addEventListener("input", function (e) {
    if (e.target && e.target.classList.contains("mark-input")) {
        let val = e.target.value;
        if (val === "" || val === null || val === undefined) return;

        // Keep only digits and decimal point
        val = val.replace(/[^0-9.]/g, "");

        // Only allow one decimal point
        const parts = val.split(".");
        if (parts.length > 2) {
            val = parts[0] + "." + parts.slice(1).join("");
        }

        // Prevent leading zeros like "0000" -> "0", "08" -> "8", "005" -> "5" (while keeping "0.5")
        if (val.length > 1 && val.startsWith("0") && !val.startsWith("0.")) {
            val = String(parseFloat(val) || 0);
        }

        // Clamp number strictly to max 100 and min 0
        const num = parseFloat(val);
        if (!isNaN(num)) {
            if (num > 100) {
                val = "100";
            } else if (num < 0) {
                val = "0";
            }
        }

        if (e.target.value !== val) {
            e.target.value = val;
        }
    }
}, true);

document.addEventListener("keydown", function (e) {
    if (e.target && e.target.classList.contains("mark-input")) {
        if (e.key === "-" || e.key === "+" || e.key === "e" || e.key === "E") {
            e.preventDefault();
        }
    }
}, true);

/* ── Academic Report Printing Helper ────────────────────────────────── */
window.printAcademicReport = function (targetId) {
    if (!targetId || targetId === "all") {
        const allSheets = document.querySelectorAll(".grade-sheet-container");
        allSheets.forEach(s => s.classList.add("print-section-active"));
        window.print();
        allSheets.forEach(s => s.classList.remove("print-section-active"));
    } else {
        const el = document.getElementById(targetId);
        if (el) {
            el.classList.add("print-section-active");
            window.print();
            el.classList.remove("print-section-active");
        } else {
            window.print();
        }
    }
};


