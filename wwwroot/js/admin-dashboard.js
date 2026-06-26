(function () {
    "use strict";

    /* ---------------------------------------------------------
       Sidebar toggle (mobile)
       --------------------------------------------------------- */
    var sidebar = document.getElementById("adminSidebar");
    var menuToggle = document.getElementById("menuToggle");
    var sidebarClose = document.getElementById("sidebarClose");
    var sidebarBackdrop = document.getElementById("sidebarBackdrop");

    function openSidebar() {
        if (sidebar) sidebar.classList.add("open");
        if (sidebarBackdrop) sidebarBackdrop.classList.add("visible");
    }

    function closeSidebar() {
        if (sidebar) sidebar.classList.remove("open");
        if (sidebarBackdrop) sidebarBackdrop.classList.remove("visible");
    }

    if (menuToggle) menuToggle.addEventListener("click", openSidebar);
    if (sidebarClose) sidebarClose.addEventListener("click", closeSidebar);
    if (sidebarBackdrop) sidebarBackdrop.addEventListener("click", closeSidebar);

    /* ---------------------------------------------------------
       Global search box: Ctrl/Cmd+K focuses it. The user
       directory table that this used to filter has moved to its
       own screen (Users sidebar link -> UserManager/Index), so
       there is no longer any in-page filtering to wire up here.
       --------------------------------------------------------- */
    var globalSearch = document.getElementById("globalSearch");

    document.addEventListener("keydown", function (e) {
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") {
            e.preventDefault();
            if (globalSearch) globalSearch.focus();
        }
    });

    /* ---------------------------------------------------------
       PLACEHOLDER DATA
       These two objects back the "Weekly throughput" bar chart
       and the "Workload breakdown" donut chart. Neither section
       has a backing API endpoint yet. Replace the values below
       with real data once the endpoints exist - the rendering
       code underneath does not need to change, only the data.
       --------------------------------------------------------- */
    window.__weeklyThroughput = window.__weeklyThroughput || {
        labels: ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"],
        ihm: [8, 12, 9, 14, 11, 5, 3],
        ex: [5, 7, 5, 8, 8, 4, 2]
    };

    window.__workloadBreakdown = window.__workloadBreakdown || {
        labels: ["IHM", "Ex", "Vessel", "Other"],
        values: [42, 28, 18, 12],
        colors: ["#1f9a82", "#f5b942", "#7a6ff0", "#d8dcd9"]
    };

    /* ---------------------------------------------------------
       Weekly throughput bar chart
       --------------------------------------------------------- */
    function renderWeeklyThroughputChart() {
        var canvas = document.getElementById("weeklyThroughputChart");
        if (!canvas || typeof Chart === "undefined") return;

        var data = window.__weeklyThroughput;

        new Chart(canvas.getContext("2d"), {
            type: "bar",
            data: {
                labels: data.labels,
                datasets: [
                    {
                        label: "IHM",
                        data: data.ihm,
                        backgroundColor: "#1f9a82",
                        borderRadius: 6,
                        maxBarThickness: 26
                    },
                    {
                        label: "Ex",
                        data: data.ex,
                        backgroundColor: "#f5b942",
                        borderRadius: 6,
                        maxBarThickness: 26
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { color: "#5b6b66", font: { size: 12, family: "Plus Jakarta Sans" } }
                    },
                    y: {
                        beginAtZero: true,
                        grid: { color: "#eef0ed" },
                        ticks: { color: "#5b6b66", font: { size: 11, family: "Plus Jakarta Sans" }, stepSize: 4 }
                    }
                }
            }
        });
    }

    /* ---------------------------------------------------------
       Workload breakdown donut chart
       --------------------------------------------------------- */
    function renderWorkloadDonutChart() {
        var canvas = document.getElementById("workloadDonutChart");
        if (!canvas || typeof Chart === "undefined") return;

        var data = window.__workloadBreakdown;

        new Chart(canvas.getContext("2d"), {
            type: "doughnut",
            data: {
                labels: data.labels,
                datasets: [
                    {
                        data: data.values,
                        backgroundColor: data.colors,
                        borderWidth: 0,
                        hoverOffset: 4
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: "72%",
                plugins: { legend: { display: false } }
            }
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        renderWeeklyThroughputChart();
        renderWorkloadDonutChart();
    });
})();
