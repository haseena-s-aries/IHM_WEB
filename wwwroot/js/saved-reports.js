(() => {
    "use strict";

    const search = document.getElementById("reportSearch");
    const rows = [...document.querySelectorAll(".report-row")];
    const typeButtons = [...document.querySelectorAll(".type-filters [data-type]")];
    const categoryButtons = [...document.querySelectorAll(".category-card[data-category]")];
    const panels = [...document.querySelectorAll(".reports-section[data-panel]")];
    const empty = document.getElementById("reportsEmpty");
    let activeCategory = "auras";
    let activeType = "";

    function availableTypesForCategory(category) {
        return category === "ihm"
            ? ["", "ihm"]
            : ["", "towage", "seafastening", "loadout"];
    }

    function syncTypeButtons() {
        const allowed = availableTypesForCategory(activeCategory);
        if (!allowed.includes(activeType)) {
            activeType = "";
        }

        typeButtons.forEach(button => {
            const type = button.dataset.type ?? "";
            button.hidden = !allowed.includes(type);
            button.classList.toggle("active", type === activeType);
        });
    }

    function filterReports() {
        const term = search?.value.trim().toLowerCase() ?? "";
        let visible = 0;

        panels.forEach(panel => panel.classList.toggle("active", panel.dataset.panel === activeCategory));
        syncTypeButtons();

        rows.forEach(row => {
            const rowType = row.dataset.type ?? "";
            const rowCategory = rowType === "ihm" ? "ihm" : "auras";
            const matchesCategory = rowCategory === activeCategory;
            const matchesType = !activeType || rowType === activeType;
            const matchesSearch = !term || (row.dataset.search ?? "").includes(term);
            row.hidden = !(matchesCategory && matchesType && matchesSearch);
            if (!row.hidden) visible++;
        });

        empty?.classList.toggle("show", visible === 0);
    }

    search?.addEventListener("input", filterReports);

    categoryButtons.forEach(button => button.addEventListener("click", () => {
        activeCategory = button.dataset.category ?? "auras";
        categoryButtons.forEach(item => item.classList.toggle("active", item === button));
        filterReports();
    }));

    typeButtons.forEach(button => button.addEventListener("click", () => {
        activeType = button.dataset.type ?? "";
        filterReports();
    }));

    filterReports();
})();
