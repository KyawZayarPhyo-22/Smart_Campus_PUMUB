/* =====================================================================
   admin.js — Smart Campus Admin Panel
   Bootstrap 5.1.0 + Highcharts
   ===================================================================== */

/* ── Highcharts chart instance registry ─────────────────────────────── */
const _hcRegistry = {};

/* ── Highcharts global dark theme ───────────────────────────────────── */
function applyHighchartsTheme() {
    if (typeof Highcharts === 'undefined') return;

    Highcharts.setOptions({
        chart: {
            backgroundColor: 'transparent',
            style: { fontFamily: "'Inter', 'Segoe UI', sans-serif" },
            animation: { duration: 600, easing: 'easeOutQuart' }
        },
        title:    { style: { color: '#f0f6fc', fontSize: '13px', fontWeight: '700' } },
        subtitle: { style: { color: '#8b949e' } },
        legend: {
            itemStyle:      { color: '#8b949e', fontWeight: '500', fontSize: '12px' },
            itemHoverStyle: { color: '#f0f6fc' }
        },
        tooltip: {
            backgroundColor: '#1c2333',
            borderColor:     'rgba(255,255,255,0.1)',
            borderRadius:    8,
            style:           { color: '#f0f6fc', fontSize: '12px' },
            shadow:          true
        },
        credits: { enabled: false },
        exporting: { enabled: false }
    });
}

/* ── Desktop sidebar collapse toggle ────────────────────────────────── */
function toggleDesktopSidebar() {
    const sidebar = document.getElementById('desktopSidebar');
    if (!sidebar) return;
    sidebar.classList.toggle('collapsed');
    const isCollapsed = sidebar.classList.contains('collapsed');
    localStorage.setItem('adminSidebarCollapsed', isCollapsed ? '1' : '0');
}

/* ── Restore sidebar state on first page load ────────────────────────── */
document.addEventListener('DOMContentLoaded', function () {
    const sidebar = document.getElementById('desktopSidebar');
    if (sidebar && localStorage.getItem('adminSidebarCollapsed') === '1') {
        sidebar.classList.add('collapsed');
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
function initCharts() {
    if (typeof Highcharts === 'undefined') return;

    applyHighchartsTheme();

    /* ── Helper: safely destroy & remove from registry ─────────────── */
    function destroyHC(id) {
        if (_hcRegistry[id] && typeof _hcRegistry[id].destroy === 'function') {
            try { _hcRegistry[id].destroy(); } catch (e) { /* already destroyed */ }
            delete _hcRegistry[id];
        }
    }

    /* ── 1. Pie Chart — Students Enrollment by Major ─────────────── */
    const pieEl = document.getElementById('hc-pie-chart');
    if (pieEl) {
        destroyHC('hc-pie-chart');
        _hcRegistry['hc-pie-chart'] = Highcharts.chart('hc-pie-chart', {
            chart: {
                type: 'pie',
                height: 260,
                margin: [0, 0, 0, 0],
                spacing: [10, 10, 10, 10]
            },
            title: { text: null },
            plotOptions: {
                pie: {
                    innerSize: '60%',         /* donut style */
                    allowPointSelect: true,
                    cursor: 'pointer',
                    borderWidth: 0,
                    dataLabels: {
                        enabled: true,
                        format: '<b>{point.name}</b><br>{point.percentage:.1f}%',
                        style: { color: '#8b949e', fontSize: '11px', fontWeight: '500' },
                        distance: 18
                    },
                    showInLegend: true
                }
            },
            legend: {
                layout: 'vertical',
                align: 'right',
                verticalAlign: 'middle',
                itemMarginBottom: 8,
                symbolRadius: 4
            },
            series: [{
                name: 'Students',
                colorByPoint: true,
                data: [
                    { name: 'Civil Eng.',     y: 1500, color: '#22d3ee' },
                    { name: 'Mechanical',     y: 1200, color: '#8b5cf6' },
                    { name: 'IT',             y: 1020, color: '#10b981' },
                    { name: 'Electrical',     y:  800, color: '#3b82f6' }
                ]
            }],
            responsive: {
                rules: [{
                    condition: { maxWidth: 400 },
                    chartOptions: {
                        legend: { enabled: false }
                    }
                }]
            }
        });
    }

    /* ── 2. Column/Bar Chart — Library Books & Tutors by Dept ──────── */
    const barEl = document.getElementById('hc-bar-chart');
    if (barEl) {
        destroyHC('hc-bar-chart');
        _hcRegistry['hc-bar-chart'] = Highcharts.chart('hc-bar-chart', {
            chart: {
                type: 'column',
                height: 260
            },
            title: { text: null },
            xAxis: {
                categories: ['Civil', 'Mech', 'IT', 'Math', 'Science'],
                labels: { style: { color: '#6e7681', fontSize: '12px' } },
                lineColor:  'rgba(255,255,255,0.05)',
                tickColor:  'rgba(255,255,255,0.05)'
            },
            yAxis: {
                min: 0,
                title: { text: null },
                labels: { style: { color: '#6e7681', fontSize: '11px' } },
                gridLineColor: 'rgba(255,255,255,0.05)',
                gridLineDashStyle: 'ShortDash'
            },
            legend: {
                symbolRadius: 4,
                itemMarginRight: 16
            },
            plotOptions: {
                column: {
                    borderRadius: 5,
                    borderWidth: 0,
                    groupPadding: 0.1,
                    pointPadding: 0.05,
                    dataLabels: { enabled: false }
                }
            },
            series: [
                {
                    name: 'Library PDF Books',
                    color: '#22d3ee',
                    data: [300, 250, 400, 150, 140]
                },
                {
                    name: 'Tutors',
                    color: '#8b5cf6',
                    data: [80, 75, 90, 40, 30]
                }
            ]
        });
    }
}

/* ── Legacy alias ─────────────────────────────────────────────────────── */
function toggleSidebar() { toggleDesktopSidebar(); }
