(() => {
    "use strict";
    const search = document.getElementById("reportSearch");
    const cards = [...document.querySelectorAll(".report-card")];
    const buttons = [...document.querySelectorAll("[data-type]")];
    const empty = document.getElementById("reportsEmpty");
    let activeType = "";

    function filterReports() {
        const term = search?.value.trim().toLowerCase() ?? "";
        let visible = 0;
        cards.forEach(card => {
            card.hidden = !((!activeType || card.dataset.type === activeType) && (!term || card.dataset.search.includes(term)));
            if (!card.hidden) visible++;
        });
        empty?.classList.toggle("show", visible === 0);
    }

    search?.addEventListener("input", filterReports);
    buttons.forEach(button => button.addEventListener("click", () => {
        activeType = button.dataset.type ?? "";
        buttons.forEach(item => item.classList.toggle("active", item === button));
        filterReports();
    }));
})();
