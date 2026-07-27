(() => {
    "use strict";

    const search = document.getElementById("reportSearch");
    const rows = [...document.querySelectorAll(".report-row")];
    const typeButtons = [...document.querySelectorAll(".type-filters [data-type]")];
    const categoryButtons = [...document.querySelectorAll(".category-card[data-category]")];
    const panels = [...document.querySelectorAll(".reports-section[data-panel]")];
    const empty = document.getElementById("reportsEmpty");
    const hasCategoryFilters = categoryButtons.length > 0;
    const hasTypeFilters = typeButtons.length > 0;
    let activeCategory = "auras";
    let activeType = "";

    function availableTypesForCategory(category) {
        if (category === "ihm") return ["", "ihm"];
        if (category === "exsurvey") return ["", "exsurvey"];
        return ["", "towage", "seafastening", "loadout"];
    }

    function syncTypeButtons() {
        if (!hasTypeFilters) return;

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

        panels.forEach(panel => {
            panel.classList.toggle("active", !hasCategoryFilters || panel.dataset.panel === activeCategory);
        });
        syncTypeButtons();

        rows.forEach(row => {
            const rowType = row.dataset.type ?? "";
            const rowCategory = rowType === "ihm"
                ? "ihm"
                : rowType === "exsurvey"
                    ? "exsurvey"
                    : "auras";
            const matchesCategory = !hasCategoryFilters || rowCategory === activeCategory;
            const matchesType = !hasTypeFilters || !activeType || rowType === activeType;
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

    if (hasCategoryFilters) {
        if (!categoryButtons.some(button => button.classList.contains("active"))) {
            categoryButtons[0].classList.add("active");
        }
        activeCategory = categoryButtons.find(button => button.classList.contains("active"))?.dataset.category
            ?? categoryButtons[0]?.dataset.category
            ?? "auras";
        const requestedCategory = new URLSearchParams(window.location.search).get("category");
        if (requestedCategory && categoryButtons.some(button => button.dataset.category === requestedCategory)) {
            activeCategory = requestedCategory;
            categoryButtons.forEach(button => {
                button.classList.toggle("active", button.dataset.category === requestedCategory);
            });
        }
    }

    async function readJson(response) {
        const text = await response.text();
        if (!text) return null;
        try {
            return JSON.parse(text);
        } catch {
            return { message: text };
        }
    }

    function messageFrom(payload, fallback) {
        return payload?.message || payload?.Message || fallback;
    }

    function escapeHtml(value) {
        return String(value ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }

    function clientValue(client, key) {
        return client?.[key] ?? client?.[key.charAt(0).toUpperCase() + key.slice(1)] ?? "";
    }

    function clientLabel(client) {
        const name = clientValue(client, "fullName") || clientValue(client, "email") || "Client";
        const email = clientValue(client, "email");
        return `${name}${email ? ` (${email})` : ""}`;
    }

    function assignmentClientName(item) {
        return item?.clientName
            || item?.ClientName
            || item?.clientEmail
            || item?.ClientEmail
            || item?.clientUser?.fullName
            || item?.ClientUser?.FullName
            || item?.clientUser?.email
            || item?.ClientUser?.Email
            || "Client";
    }

    function renderClientOptions(select, clients, term = "") {
        if (!select) return;

        const needle = term.trim().toLowerCase();
        const filtered = clients.filter(client => {
            const text = `${clientValue(client, "fullName")} ${clientValue(client, "email")}`.toLowerCase();
            return !needle || text.includes(needle);
        });

        if (clients.length === 0) {
            select.innerHTML = '<option value="">No approved Client users found</option>';
            select.disabled = true;
            return;
        }

        select.disabled = false;
        select.innerHTML = '<option value="">Select Client</option>' + filtered.map(client => {
            const id = clientValue(client, "id");
            return `<option value="${escapeHtml(id)}">${escapeHtml(clientLabel(client))}</option>`;
        }).join("");

        if (filtered.length === 0) {
            select.innerHTML += '<option value="" disabled>No Client matches search</option>';
        }
    }

    async function loadClients(projectType, term = "") {
        let url = `/Home/ReportClients?projectType=${encodeURIComponent(projectType)}`;
        if (term.trim()) {
            url += `&search=${encodeURIComponent(term.trim())}`;
        }

        const response = await fetch(url, {
            headers: { "Accept": "application/json" }
        });
        const payload = await readJson(response);
        if (!response.ok) throw new Error(messageFrom(payload, "Unable to load Client users."));
        return Array.isArray(payload) ? payload : [];
    }

    async function loadAssignments(type, id) {
        const response = await fetch(`/Home/ReportAssignments?type=${encodeURIComponent(type)}&id=${encodeURIComponent(id)}`, {
            headers: { "Accept": "application/json" }
        });
        const payload = await readJson(response);
        if (!response.ok) throw new Error(messageFrom(payload, "Unable to load report assignments."));
        return Array.isArray(payload) ? payload : [];
    }

    function renderAssignments(container, assignments) {
        if (assignments.length === 0) {
            container.innerHTML = '<span class="assignment-empty">No clients assigned</span>';
            return;
        }

        container.innerHTML = assignments.map(item => {
            const name = assignmentClientName(item);
            const id = item.id || item.Id;
            return `<span class="assigned-client">
                ${escapeHtml(name)}
                <button type="button" class="revoke-assignment" data-assignment-id="${escapeHtml(id)}">Remove</button>
            </span>`;
        }).join("");
    }

    async function refreshSendBox(box, clientsCache) {
        const type = box.dataset.reportType;
        const id = box.dataset.reportId;
        const projectType = box.dataset.projectType;
        const select = box.querySelector(".client-select");
        const clientSearch = box.querySelector(".client-search");
        const assigned = box.querySelector(".assigned-clients");

        try {
            const clientTerm = clientSearch?.value ?? "";
            const cacheKey = `${projectType}|${clientTerm.trim().toLowerCase()}`;
            const clients = clientsCache[cacheKey] ?? await loadClients(projectType, clientTerm);
            clientsCache[cacheKey] = clients;
            box._clients = clients;
            renderClientOptions(select, clients);

            const assignments = await loadAssignments(type, id);
            renderAssignments(assigned, assignments);
        } catch (error) {
            if (select) {
                select.innerHTML = '<option value="">Unable to load Client users</option>';
                select.disabled = true;
            }
            assigned.textContent = error.message || "Unable to load assignments.";
        }
    }

    const sendBoxes = [...document.querySelectorAll(".report-send")];
    const clientsCache = {};
    sendBoxes.forEach(box => refreshSendBox(box, clientsCache));

    document.addEventListener("input", event => {
        const searchInput = event.target.closest(".client-search");
        if (!searchInput) return;

        const box = searchInput.closest(".report-send");
        const select = box?.querySelector(".client-select");
        renderClientOptions(select, box?._clients ?? [], searchInput.value);

        clearTimeout(searchInput._clientSearchTimer);
        searchInput._clientSearchTimer = setTimeout(() => {
            if (box) refreshSendBox(box, clientsCache);
        }, 300);
    });

    document.addEventListener("click", async event => {
        const sendButton = event.target.closest(".send-report-btn");
        if (sendButton) {
            const box = sendButton.closest(".report-send");
            const select = box?.querySelector(".client-select");
            const clientUserId = Number(select?.value || 0);
            if (!box || !clientUserId) {
                alert("Please select a Client user.");
                return;
            }

            sendButton.disabled = true;
            sendButton.textContent = "Sending...";
            try {
                const response = await fetch("/Home/AssignReportToClient", {
                    method: "POST",
                    headers: {
                        "Accept": "application/json",
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify({
                        reportType: box.dataset.reportType,
                        reportId: Number(box.dataset.reportId),
                        surveyJobId: Number(box.dataset.reportId),
                        clientUserId
                    })
                });
                const payload = await readJson(response);
                if (!response.ok) throw new Error(messageFrom(payload, "Unable to send report."));
                await refreshSendBox(box, clientsCache);
                alert(messageFrom(payload, "Report assigned successfully."));
            } catch (error) {
                alert(error.message || "Unable to send report.");
            } finally {
                sendButton.disabled = false;
                sendButton.textContent = "Send";
            }
            return;
        }

        const revokeButton = event.target.closest(".revoke-assignment");
        if (revokeButton) {
            const assignmentId = revokeButton.dataset.assignmentId;
            const box = revokeButton.closest(".report-send");
            if (!assignmentId || !box) return;
            revokeButton.disabled = true;
            try {
                const response = await fetch(`/Home/RevokeReportAssignment?assignmentId=${encodeURIComponent(assignmentId)}`, {
                    method: "POST",
                    headers: { "Accept": "application/json" }
                });
                const payload = await readJson(response);
                if (!response.ok) throw new Error(messageFrom(payload, "Unable to remove assignment."));
                await refreshSendBox(box, clientsCache);
            } catch (error) {
                alert(error.message || "Unable to remove assignment.");
                revokeButton.disabled = false;
            }
        }
    });

    // ---------- Send-to-client MODAL (IHM action-icon flow) ----------
    const sendModal = document.getElementById("sendReportModal");
    if (sendModal) {
        const modalSearch = sendModal.querySelector(".client-search");
        const modalSelect = sendModal.querySelector(".client-select");
        const modalSendBtn = sendModal.querySelector(".send-modal-send");
        const modalAssigned = sendModal.querySelector(".assigned-clients");

        let modalContext = null;

        function closeSendModal() {
            sendModal.hidden = true;
            modalContext = null;
            modalSearch.value = "";
        }

        async function refreshModal() {
            if (!modalContext) return;
            try {
                const clients = await loadClients(modalContext.projectType, modalSearch.value);
                renderClientOptions(modalSelect, clients);

                const assignments = await loadAssignments(modalContext.reportType, modalContext.reportId);
                renderAssignments(modalAssigned, assignments);
            } catch (error) {
                modalSelect.innerHTML = '<option value="">Unable to load Client users</option>';
                modalAssigned.textContent = error.message || "Unable to load assignments.";
            }
        }

        async function openSendModal(btn) {
            modalContext = {
                reportType: btn.dataset.reportType,
                reportId: btn.dataset.reportId,
                projectType: btn.dataset.projectType
            };
            sendModal.hidden = false;
            modalSearch.value = "";
            modalSelect.innerHTML = '<option value="">Loading...</option>';
            modalAssigned.textContent = "Loading assigned clients...";
            await refreshModal();
        }

        document.addEventListener("click", async event => {
            const trigger = event.target.closest(".send-icon-btn");
            if (trigger) {
                await openSendModal(trigger);
                return;
            }

            if (event.target === sendModal || event.target.closest(".send-modal-close")) {
                closeSendModal();
                return;
            }

            if (event.target.closest(".send-modal-send")) {
                const clientUserId = Number(modalSelect.value || 0);
                if (!modalContext || !clientUserId) {
                    alert("Please select a Client user.");
                    return;
                }

                modalSendBtn.disabled = true;
                modalSendBtn.textContent = "Sending...";
                try {
                    const response = await fetch("/Home/AssignReportToClient", {
                        method: "POST",
                        headers: { "Accept": "application/json", "Content-Type": "application/json" },
                        body: JSON.stringify({
                            reportType: modalContext.reportType,
                            reportId: Number(modalContext.reportId),
                            surveyJobId: Number(modalContext.reportId),
                            clientUserId
                        })
                    });
                    const payload = await readJson(response);
                    if (!response.ok) throw new Error(messageFrom(payload, "Unable to send report."));
                    await refreshModal();
                    alert(messageFrom(payload, "Report assigned successfully."));
                } catch (error) {
                    alert(error.message || "Unable to send report.");
                } finally {
                    modalSendBtn.disabled = false;
                    modalSendBtn.textContent = "Send";
                }
                return;
            }

            const revokeBtn = event.target.closest(".revoke-assignment");
            if (revokeBtn && sendModal.contains(revokeBtn)) {
                const assignmentId = revokeBtn.dataset.assignmentId;
                if (!assignmentId) return;
                revokeBtn.disabled = true;
                try {
                    const response = await fetch(`/Home/RevokeReportAssignment?assignmentId=${encodeURIComponent(assignmentId)}`, {
                        method: "POST",
                        headers: { "Accept": "application/json" }
                    });
                    const payload = await readJson(response);
                    if (!response.ok) throw new Error(messageFrom(payload, "Unable to remove assignment."));
                    await refreshModal();
                } catch (error) {
                    alert(error.message || "Unable to remove assignment.");
                    revokeBtn.disabled = false;
                }
            }
        });

        modalSearch.addEventListener("input", () => {
            clearTimeout(modalSearch._timer);
            modalSearch._timer = setTimeout(refreshModal, 300);
        });

        document.addEventListener("keydown", event => {
            if (event.key === "Escape" && !sendModal.hidden) closeSendModal();
        });
    }

    // ---------- Delete report ----------
    document.addEventListener("click", async event => {
        const deleteBtn = event.target.closest(".delete-report-btn");
        if (!deleteBtn) return;

        const type = deleteBtn.dataset.reportType;
        const id = deleteBtn.dataset.reportId;
        if (!confirm("Delete this report? This cannot be undone.")) return;

        deleteBtn.disabled = true;
        try {
            const response = await fetch(`/Home/DeleteSavedReport?type=${encodeURIComponent(type)}&id=${encodeURIComponent(id)}`, {
                method: "POST",
                headers: { "Accept": "application/json" }
            });
            const payload = await readJson(response);
            if (!response.ok) throw new Error(messageFrom(payload, "Unable to delete report."));
            deleteBtn.closest(".report-row")?.remove();
        } catch (error) {
            alert(error.message || "Unable to delete report.");
            deleteBtn.disabled = false;
        }
    });

    // ---------- Prepared On -> local timezone ----------
    document.querySelectorAll(".prepared-on[data-utc]").forEach(el => {
        const utc = el.getAttribute("data-utc");
        if (!utc) return;
        const d = new Date(utc);
        if (isNaN(d)) return;
        el.textContent = d.toLocaleString(undefined, {
            day: "2-digit", month: "short", year: "numeric",
            hour: "2-digit", minute: "2-digit", hour12: true
        });
    });

    filterReports();
})();
