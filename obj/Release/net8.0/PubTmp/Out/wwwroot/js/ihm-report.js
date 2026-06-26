(() => {

    "use strict";

    const config = window.ihmReportConfig ?? {};

    const form = document.getElementById("reportForm");

    const companyNameInput = document.getElementById("CompanyName");

    const companyIdInput = document.getElementById("CompanyId");

    const companyResults = document.getElementById("companySearchResults");

    let companySearchTimer;

    // ---------------------------------------------------------------
    // Static Aries office directory (wwwroot/data/aries-offices.json).
    // Loaded once and cached in memory; merged with the live
    // companyOptionsUrl search results so known Aries offices always
    // show up as suggestions even if the API has no matching record.
    // ---------------------------------------------------------------
    let ariesOfficesPromise = null;

    function loadAriesOffices() {

        if (!ariesOfficesPromise) {

            ariesOfficesPromise = fetch("/data/aries-offices.json")

                .then(response => response.ok ? response.json() : [])

                .catch(() => []);

        }

        return ariesOfficesPromise;

    }

    function filterAriesOffices(offices, term) {

        const needle = term.trim().toLowerCase();

        if (!needle) return [];

        return offices.filter(office => {

            const haystack = `${office.companyName} ${office.cityLocation} ${office.country}`.toLowerCase();

            return haystack.includes(needle);

        });

    }

    function mergeCompanyResults(ariesMatches, apiMatches) {

        const seen = new Set();

        const merged = [];

        [...ariesMatches, ...apiMatches].forEach(company => {

            const key = (company.companyName || "").trim().toLowerCase();

            if (!key || seen.has(key)) return;

            seen.add(key);

            merged.push(company);

        });

        return merged;

    }

    function setCompanyValue(id, value) {

        const input = document.getElementById(id);

        if (input) input.value = value ?? "";

    }

    function selectCompany(company) {

        setCompanyValue("CompanyId", company.id);

        setCompanyValue("CompanyName", company.companyName);

        setCompanyValue("CompanyAddress", company.companyAddress);

        setCompanyValue("CompanyTel", company.companyTel);

        setCompanyValue("CompanyFax", company.companyFax);

        setCompanyValue("CompanyWebsite", company.companyWebsite);

        setCompanyValue("CompanyEmail", company.companyEmail);

        setCompanyValue("CompanyLogoPath", company.companyLogoPath);

        const logoLabel = document.getElementById("CompanyLogoPath-label");

        if (logoLabel) {

            logoLabel.textContent =

                company.companyLogoPath || "No image uploaded";

        }

        companyResults.hidden = true;

    }

    companyNameInput?.addEventListener("input", () => {

        if (companyIdInput) companyIdInput.value = "";

        clearTimeout(companySearchTimer);

        companySearchTimer = setTimeout(async () => {

            const term = companyNameInput.value.trim();

            if (!term) {

                companyResults.hidden = true;

                return;

            }

            const [ariesOffices, apiCompanies] = await Promise.all([

                loadAriesOffices(),

                fetch(`${config.companyOptionsUrl}?search=${encodeURIComponent(term)}`)

                    .then(response => response.ok ? response.json() : [])

                    .catch(() => [])

            ]);

            const ariesMatches = filterAriesOffices(ariesOffices, term);

            const companies = mergeCompanyResults(ariesMatches, apiCompanies);

            companyResults.innerHTML = "";

            companies.forEach(company => {

                const option = document.createElement("button");

                option.type = "button";

                option.className = "company-search-option";

                option.innerHTML = `
<strong>${company.companyName}</strong>
<small>${company.cityLocation || ""} ${company.country || ""}</small>

            `;

                option.addEventListener("click", () => selectCompany(company));

                companyResults.appendChild(option);

            });

            companyResults.hidden = companies.length === 0;

        }, 300);

    });

    document.addEventListener("click", event => {

        if (!event.target.closest(".company-search-field")) {

            companyResults?.setAttribute("hidden", "");

        }

    });

    const token = form?.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";

    document.querySelectorAll(".section-heading").forEach(button => {

        button.addEventListener("click", () => {

            const body = button.nextElementSibling;

            const expanded = button.getAttribute("aria-expanded") === "true";

            button.setAttribute("aria-expanded", String(!expanded));

            body.classList.toggle("collapsed", expanded);

        });

    });

    const collections = {

        revisions: document.querySelector('[data-collection="revisions"][data-prefix]'),

        paint: document.querySelector('[data-collection="paint"][data-prefix]'),

        equipment: document.querySelector('[data-collection="equipment"][data-prefix]'),

        structure: document.querySelector('[data-collection="structure"][data-prefix]'),

        risk: document.querySelector('[data-collection="risk"][data-prefix]'),

        appendixPoints: document.querySelector('[data-collection="appendixPoints"][data-prefix]')

    };

    function reindexCollection(name) {

        const root = collections[name];

        if (!root) return;

        const prefix = root.dataset.prefix;

        [...root.children].forEach((row, index) => {

            row.querySelectorAll("[data-field]").forEach(input => {

                input.name = `${prefix}[${index}].${input.dataset.field}`;

                input.id = `${prefix}_${index}__${input.dataset.field}`;

                if (input.dataset.field === "SortOrder") input.value = String(index + 1);

            });

            const number = row.querySelector(".row-number");

            if (number) {

                const textNode = [...number.childNodes].find(node => node.nodeType === Node.TEXT_NODE);

                if (textNode) textNode.nodeValue = String(index + 1);

                else number.prepend(document.createTextNode(String(index + 1)));

            }

        });

    }

    function addRow(name) {

        const template = document.getElementById(`${name}-template`);

        const root = collections[name];

        if (!template || !root) return;

        root.append(template.content.cloneNode(true));

        const newRow = root.lastElementChild;

        if (name === "revisions" && newRow) {

            setRevisionDefaults(newRow);

        }

        reindexCollection(name);

        newRow?.querySelector(

            name === "revisions"

                ? '[data-field="Description"]'

                : "input, textarea"

        )?.focus();

    }

    document.addEventListener("click", event => {

        const add = event.target.closest(".add-row");

        if (add) {

            addRow(add.dataset.collection);

            return;

        }

        const remove = event.target.closest(".remove-row");

        if (remove) {

            const root = remove.closest("[data-collection][data-prefix]");

            if (!root) return;

            if (root.children.length === 1) {

                root.querySelectorAll("input:not([type=hidden]), textarea").forEach(input => input.value = "");

                return;

            }

            const name = root.dataset.collection;

            remove.closest("tr, .appendix-point")?.remove();

            reindexCollection(name);

        }

    });

    Object.keys(collections).forEach(reindexCollection);

    const revisionSourceMap = {

        Uid: "Uid",

        RevisionNo: "RevisionNo",

        RevisionDate: "DocumentDate",

        PreparedBy: "PreparedBy",

        CheckedBy: "CheckedBy",

        ApprovedBy: "ApprovedBy"

    };

    function getDocumentControlValue(fieldName) {

        return form?.elements.namedItem(fieldName)?.value ?? "";

    }

    function initialiseRevisionAutoSync() {

        collections.revisions?.querySelectorAll("tr").forEach(row => {

            Object.entries(revisionSourceMap).forEach(([revisionField, sourceField]) => {

                const input = row.querySelector(`[data-field="${revisionField}"]`);

                if (!input) return;

                // Only values currently matching document control remain automatic.

                input.dataset.autoSync =

                    input.value === getDocumentControlValue(sourceField)

                        ? "true"

                        : "false";

            });

        });

    }

    function setRevisionDefaults(row) {

        Object.entries(revisionSourceMap).forEach(([revisionField, sourceField]) => {

            const input = row.querySelector(`[data-field="${revisionField}"]`);

            if (!input) return;

            input.value = getDocumentControlValue(sourceField);

            input.dataset.autoSync = "true";

        });

    }

    function syncRevisionDefaults(sourceField) {

        const revisionField = Object.keys(revisionSourceMap)

            .find(key => revisionSourceMap[key] === sourceField);

        if (!revisionField) return;

        const value = getDocumentControlValue(sourceField);

        collections.revisions

            ?.querySelectorAll(`[data-field="${revisionField}"]`)

            .forEach(input => {

                if (input.dataset.autoSync === "true")

                    input.value = value;

            });

    }

    initialiseRevisionAutoSync();

    Object.values(revisionSourceMap).forEach(sourceField => {

        form?.elements.namedItem(sourceField)?.addEventListener("input", () => {

            syncRevisionDefaults(sourceField);

        });

        form?.elements.namedItem(sourceField)?.addEventListener("change", () => {

            syncRevisionDefaults(sourceField);

        });

    });

    collections.revisions?.addEventListener("input", event => {

        const input = event.target.closest("[data-field]");

        if (!input || !(input.dataset.field in revisionSourceMap)) return;

        // User manually edited this cell; stop automatic updates for this cell.

        input.dataset.autoSync = "false";

    });

    function reindexPeople() {

        let index = 0;

        document.querySelectorAll(".people-list .person-row").forEach(row => {

            row.querySelectorAll("[data-field]").forEach(input => {

                input.name = `OhsPeople[${index}].${input.dataset.field}`;

                if (input.dataset.field === "SortOrder") input.value = String(index + 1);

            });

            index++;

        });

    }

    document.querySelectorAll(".add-person").forEach(button => {

        button.addEventListener("click", () => {

            const list = document.querySelector(`.people-list[data-role="${button.dataset.role}"]`);

            if (!list || list.children.length >= Number(button.dataset.max)) return;

            const row = document.createElement("div");

            row.className = "person-row";

            row.innerHTML = `
<input data-field="RoleType" type="hidden" value="${button.dataset.role}">
<input data-field="SortOrder" type="hidden">
<input data-field="Name" placeholder="Full name">
<button type="button" class="remove-person"><i class="fa-solid fa-xmark"></i></button>`;

            list.append(row);

            reindexPeople();

            row.querySelector('[data-field="Name"]').focus();

        });

    });

    document.addEventListener("click", event => {

        const remove = event.target.closest(".remove-person");

        if (!remove) return;

        const list = remove.closest(".people-list");

        if (list.children.length === 1) {

            list.querySelector('[data-field="Name"]').value = "";

            return;

        }

        remove.closest(".person-row").remove();

        reindexPeople();

    });

    reindexPeople();

    async function uploadAssetFile(file, target, fileInput = null) {

        if (!file || !target) return;

        if (!file.type.startsWith("image/")) {

            alert("Please choose or paste an image file.");

            return;

        }

        const data = new FormData();

        data.append("__RequestVerificationToken", token);

        data.append("jobId", String(config.jobId));

        data.append("targetField", target);

        const extension = file.type === "image/png"

            ? ".png"

            : file.type === "image/webp"

                ? ".webp"

                : ".jpg";

        const uploadFile = file.name

            ? file

            : new File(

                [file],

                `pasted-${target}-${Date.now()}${extension}`,

                { type: file.type }

            );

        data.append("file", uploadFile);

        const label = document.getElementById(`${target}-label`);

        const zone = document.querySelector(

            `.paste-upload-zone[data-target="${target}"]`

        );

        if (label) label.textContent = "Uploading...";

        if (fileInput) fileInput.disabled = true;

        zone?.classList.add("uploading");

        try {

            const response = await fetch(config.uploadAssetUrl, {

                method: "POST",

                body: data

            });

            if (!response.ok)

                throw new Error(await response.text());

            const result = await response.json();

            const hiddenInput = document.getElementById(target);

            if (hiddenInput)

                hiddenInput.value = result.filePath ?? "";

            if (label)

                label.textContent = result.filePath ?? "Uploaded";

        } catch (error) {

            if (label)

                label.textContent = error.message || "Upload failed";

        } finally {

            if (fileInput) {

                fileInput.disabled = false;

                fileInput.value = "";

            }

            zone?.classList.remove("uploading");

        }

    }

    document.querySelectorAll(".asset-input").forEach(input => {

        input.addEventListener("change", async () => {

            if (!input.files?.length) return;

            await uploadAssetFile(

                input.files[0],

                input.dataset.target,

                input

            );

        });

    });

    document.querySelectorAll(".paste-upload-zone").forEach(zone => {

        zone.addEventListener("click", event => {

            if (!event.target.closest("input"))

                zone.focus();

        });

        zone.addEventListener("paste", async event => {

            const items = [...(event.clipboardData?.items ?? [])];

            const imageItem = items.find(item =>

                item.kind === "file" &&

                item.type.startsWith("image/")

            );

            const imageFile =

                imageItem?.getAsFile() ??

                [...(event.clipboardData?.files ?? [])]

                    .find(file => file.type.startsWith("image/"));

            if (!imageFile) {

                alert("Clipboard does not contain an image.");

                return;

            }

            event.preventDefault();

            await uploadAssetFile(

                imageFile,

                zone.dataset.target

            );

        });

    });

    function actionData(container) {

        const data = new FormData();

        data.append("__RequestVerificationToken", token);

        container.querySelectorAll("input, textarea, select").forEach(field => {

            if (!field.name || field.disabled) return;

            if (field.type === "file") {

                [...field.files].forEach(file => data.append(field.name, file));

            } else if ((field.type !== "checkbox" && field.type !== "radio") || field.checked) {

                data.append(field.name, field.value);

            }

        });

        return data;

    }

    async function saveCurrentReportBeforeReload() {

        Object.keys(collections).forEach(reindexCollection);

        reindexPeople();

        const data = new FormData(form);

        // Remove attachment-only fields from the report save request.

        data.delete("files");

        data.delete("appendixType");

        data.delete("appendixName");

        data.delete("appendixNo");

        data.delete("appendixTitle");

        const response = await fetch(config.saveUrl, {

            method: "POST",

            body: data,

            headers: {

                "X-Requested-With": "XMLHttpRequest"

            }

        });

        if (!response.ok) {

            const message = await response.text();

            throw new Error(message || "Unable to save the current report.");

        }

    }

    async function submitAppendixUpload(uploadPanel) {

        const fileInput = uploadPanel.querySelector('input[type="file"]');

        const nameInput = uploadPanel.querySelector('input[name="appendixName"]');

        if (!fileInput?.files.length) {

            alert("Please choose at least one file.");

            return;

        }

        if (uploadPanel.dataset.requireName === "true" && !nameInput?.value.trim()) {

            nameInput.focus();

            return;

        }

        const submit = uploadPanel.querySelector(".upload-submit");

        if (submit) submit.disabled = true;

        uploadPanel.classList.add("dragging");

        try {

            const response = await fetch(uploadPanel.dataset.action, {

                method: "POST",

                body: actionData(uploadPanel)

            });

            if (!response.ok) throw new Error(await response.text());

            await saveCurrentReportBeforeReload();

            window.location.reload();

        } catch (error) {

            alert(error.message || "Upload failed.");

            uploadPanel.classList.remove("dragging");

            if (submit) submit.disabled = false;

        }

    }

    document.querySelectorAll(".appendix-upload-form").forEach(uploadPanel => {

        uploadPanel.querySelector(".upload-submit")?.addEventListener("click", () => submitAppendixUpload(uploadPanel));

        const fileInput = uploadPanel.querySelector('input[type="file"]');

        if (fileInput && uploadPanel.classList.contains("upload-zone")) {

            fileInput.addEventListener("change", () => {

                if (fileInput.files.length) submitAppendixUpload(uploadPanel);

            });

        }

    });

    document.querySelectorAll(".appendix-rename").forEach(button => {

        button.addEventListener("click", async () => {

            const panel = button.closest(".appendix-title-form");

            button.disabled = true;

            try {

                const response = await fetch(panel.dataset.action, {

                    method: "POST",

                    body: actionData(panel)

                });

                if (!response.ok) throw new Error(await response.text());

                await saveCurrentReportBeforeReload();

                window.location.reload();

            } catch (error) {

                alert(error.message || "Unable to rename appendix.");

                button.disabled = false;

            }

        });

    });

    document.querySelectorAll(".attachment-delete").forEach(button => {

        button.addEventListener("click", async () => {

            if (!confirm("Delete this attachment?")) return;

            const data = new FormData();

            data.append("__RequestVerificationToken", token);

            data.append("jobId", button.dataset.jobId);

            data.append("attachmentId", button.dataset.attachmentId);

            button.disabled = true;

            try {

                const response = await fetch(button.dataset.action, { method: "POST", body: data });

                if (!response.ok) throw new Error(await response.text());

                await saveCurrentReportBeforeReload();

                window.location.reload();

            } catch (error) {

                alert(error.message || "Unable to delete attachment.");

                button.disabled = false;

            }

        });

    });

    const showAppendixForm =

        document.getElementById("showAppendixForm");

    const newAppendixForm =

        document.getElementById("newAppendixForm");

    showAppendixForm?.addEventListener("click", () => {

        showAppendixForm.hidden = true;

        newAppendixForm.hidden = false;

        newAppendixForm

            .querySelector('input[name="appendixName"]')

            ?.focus();

    });

    document

        .getElementById("cancelAppendixForm")

        ?.addEventListener("click", () => {

            newAppendixForm

                .querySelectorAll('input:not([type="hidden"])')

                .forEach(input => input.value = "");

            newAppendixForm.hidden = true;

            showAppendixForm.hidden = false;

        });

    let pdfGenerationStarted = false;

    function lockPageForPdfGeneration() {

        if (pdfGenerationStarted) return false;

        pdfGenerationStarted = true;

        document.activeElement?.blur();

        const overlay = document.createElement("div");

        overlay.id = "pdfGenerationOverlay";

        overlay.innerHTML = `
<div class="pdf-generation-message">
<i class="fa-solid fa-spinner fa-spin"></i>
<strong>Generating PDF...</strong>
<span>Please wait. Do not close or refresh this page.</span>
</div>

    `;

        document.body.appendChild(overlay);

        // Prevent keyboard input and pointer interaction.

        form?.querySelectorAll("input, textarea, select, button")

            .forEach(element => {

                element.readOnly = true;

                element.tabIndex = -1;

            });

        return true;

    }

    function updateCompletion() {

        const fields = [...document.querySelectorAll(".core-field")];

        const complete = fields.filter(field => field.value.trim().length > 0).length;

        const percent = fields.length ? Math.round((complete / fields.length) * 100) : 0;

        document.getElementById("completionValue").textContent = `${percent}%`;

        document.getElementById("formProgress").style.width = `${percent}%`;

        document.getElementById("completionRing").style.setProperty("--progress", `${percent * 3.6}deg`);

        document.getElementById("completionText").textContent =

            percent === 100 ? "Core report fields are complete" : `${fields.length - complete} core fields remaining`;

    }

    document.addEventListener("input", event => {

        if (event.target.classList.contains("core-field")) updateCompletion();

    });

    updateCompletion();

    document.getElementById("generatePdfButton")?.addEventListener("click", event => {

        event.preventDefault();

        if (!form || pdfGenerationStarted) return;

        const generateUrl =

            config.generatePdfUrl ||

            event.currentTarget.dataset.generateUrl;

        if (!generateUrl) {

            alert("The PDF generation route is unavailable. Refresh the page and try again.");

            return;

        }

        Object.keys(collections).forEach(reindexCollection);

        reindexPeople();

        if (!lockPageForPdfGeneration()) return;

        form.action = generateUrl;

        form.requestSubmit();

    });

    form?.addEventListener("submit", () => {

        Object.keys(collections).forEach(reindexCollection);

        reindexPeople();

        document.querySelectorAll(".submit-action").forEach(button => {

            button.disabled = true;

            button.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Working...';

        });

    });

    const navLinks = [...document.querySelectorAll(".section-nav a")];

    const sections = [...document.querySelectorAll(".report-section")];

    const observer = new IntersectionObserver(entries => {

        const visible = entries

            .filter(entry => entry.isIntersecting)

            .sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0];

        if (!visible) return;

        navLinks.forEach(link => link.classList.toggle("active", link.hash === `#${visible.target.id}`));

    }, { rootMargin: "-110px 0px -65% 0px", threshold: [0.05, 0.2] });

    sections.forEach(section => observer.observe(section));

    const search = document.getElementById("reportSearch");

    const results = document.getElementById("reportResults");

    let searchTimer;

    async function loadReportOptions() {

        results.innerHTML = '<div class="result-loading"><i class="fa-solid fa-spinner fa-spin"></i> Loading reports...</div>';

        try {

            const response = await fetch(`${config.jobOptionsUrl}?search=${encodeURIComponent(search.value)}`);

            if (!response.ok) throw new Error();

            const items = await response.json();

            results.innerHTML = items.length

                ? items.map(item => `
<button class="report-result template-option"

                                    type="button"

                                    data-job-id="${item.jobId}">
<i class="fa-solid fa-ship"></i>
<span><strong>${escapeHtml(item.vesselName || "Unnamed vessel")}</strong><small>${escapeHtml(item.jobNumber || "")}${item.imoNumber ? ` | IMO ${escapeHtml(item.imoNumber)}` : ""}</small></span>
<i class="fa-solid fa-chevron-right result-arrow"></i>
</button>`).join("")

                : '<div class="result-loading">No matching reports found.</div>';

        } catch {

            results.innerHTML = '<div class="result-loading">Unable to load report options.</div>';

        }

    }

    search?.addEventListener("input", () => {

        clearTimeout(searchTimer);

        searchTimer = setTimeout(loadReportOptions, 250);

    });

    document.getElementById("reportFinderModal")?.addEventListener("shown.bs.modal", () => {

        search.focus();

        loadReportOptions();

    });

    results?.addEventListener("click", async event => {

        const option = event.target.closest(".template-option");

        if (!option) return;

        if (option.dataset.jobId === String(config.currentJobId)) {

            alert("Please select another vessel - job number.");

            return;

        }

        option.disabled = true;

        try {

            const sourceJobId = option.dataset.jobId;

            const response = await fetch(

                `${config.templateFromJobUrl}?sourceJobId=${encodeURIComponent(sourceJobId)}`

            );

            if (!response.ok)

                throw new Error(await response.text());

            const templateData = await response.json();

            sessionStorage.setItem(

                `ihm-template-${config.currentJobId}`,

                JSON.stringify(templateData)

            );

            const copyData = new FormData();

            copyData.append("__RequestVerificationToken", token);

            copyData.append("jobId", String(config.currentJobId));

            copyData.append("sourceJobId", String(sourceJobId));

            const copyResponse = await fetch(config.copyAppendicesUrl, {

                method: "POST",

                body: copyData

            });

            if (!copyResponse.ok)

                throw new Error(await copyResponse.text());

            await saveCurrentReportBeforeReload();

            window.location.reload();

        } catch (error) {

            alert(error.message || "Unable to load report information.");

            option.disabled = false;

        }

    });

    const pendingTemplateKey =

        `ihm-template-${config.currentJobId}`;

    const pendingTemplate =

        sessionStorage.getItem(pendingTemplateKey);

    if (pendingTemplate) {

        sessionStorage.removeItem(pendingTemplateKey);

        try {

            applyReportTemplate(

                JSON.parse(pendingTemplate)

            );

            updateCompletion();

        } catch (error) {

            console.error(

                "Unable to restore report template.",

                error

            );

        }

    }

    function updateAssetLabels() {

        const assets = {

            CompanyLogoPath: "No image uploaded",

            ClientLogoPath: "No image uploaded",

            OhsPreparedBySignPath: "Upload signature",

            OhsApprovedBySignPath: "Upload signature"

        };

        Object.entries(assets).forEach(([fieldId, emptyText]) => {

            const field = document.getElementById(fieldId);

            const label = document.getElementById(`${fieldId}-label`);

            if (label)

                label.textContent = field?.value || emptyText;

        });

    }

    function applyReportTemplate(source) {

        // These fields belong to the current job/vessel and must not change.

        const preservedFields = new Set([

            "JobId",

            "VesselId",

            "VesselName",

            "ImoNumber",

            "OfficialNumber",

            "VesselType",

            "Flag",

            "PortOfRegistry",

            "ClassSociety",

            "CallSign",

            "Builder",

            "DateOfBuild",

            "LengthOverall",

            "LengthBetweenPerpendiculars",

            "Breadth",

            "Depth",

            "DraughtMoulded",

            "GrossTonnage",

            "NetTonnage",

            "ShipManager",

            "JobNumber",

            "ProjectName",

            "SiteOfCheck",

            "DateOfSurvey"

        ]);

        const collectionFields = new Set([

            "Revisions",

            "PaintAndCoatingSystems",

            "EquipmentAndMachinery",

            "StructureAndHull",

            "OhsPeople",

            "OhsRiskAssessmentRows",

            "AppendixOneResultPoints",

            "AppendixTwoAttachments",

            "AppendixFiveAttachments",

            "AppendixSevenAttachments",

            "CustomAppendices"

        ]);

        Object.entries(source).forEach(([jsonName, value]) => {

            const fieldName =

                jsonName.charAt(0).toUpperCase() + jsonName.slice(1);

            if (preservedFields.has(fieldName) ||

                collectionFields.has(fieldName)) {

                return;

            }

            const field = form.elements.namedItem(fieldName);

            if (!field || field instanceof RadioNodeList) return;

            if (field.type === "date" && typeof value === "string")

                field.value = value.substring(0, 10);

            else

                field.value = value ?? "";

        });

        updateAssetLabels();

        fillCollection("revisions", getProperty(source, "revisions"));

        fillCollection("paint", getProperty(source, "paintAndCoatingSystems"));

        fillCollection("structure", getProperty(source, "structureAndHull"));

        fillCollection("risk", getProperty(source, "ohsRiskAssessmentRows"));

        fillPeople(getProperty(source, "ohsPeople"));

        fillCollection("appendixPoints", getProperty(source, "appendixOneResultPoints"));

        updateCompletion();

    }

    function createPersonRow(role, name) {

        const row = document.createElement("div");

        row.className = "person-row";

        row.innerHTML = `
<input data-field="RoleType" type="hidden">
<input data-field="SortOrder" type="hidden">
<input data-field="Name" placeholder="Full name">
<button type="button" class="remove-person">
<i class="fa-solid fa-xmark"></i>
</button>`;

        row.querySelector('[data-field="RoleType"]').value = role ?? "";

        row.querySelector('[data-field="Name"]').value = name ?? "";

        return row;

    }

    function fillPeople(items) {

        document.querySelectorAll(".people-list")

            .forEach(list => list.replaceChildren());

        (items ?? []).forEach(item => {

            const role = item.roleType ?? item.RoleType;

            const name = item.name ?? item.Name ?? "";

            const list = [...document.querySelectorAll(".people-list")]

                .find(element => element.dataset.role === role);

            if (list)

                list.append(createPersonRow(role, name));

        });

        document.querySelectorAll(".people-list:empty").forEach(list => {

            list.append(createPersonRow(list.dataset.role, ""));

        });

        reindexPeople();

    }

    function fillCollection(name, items) {

        const root = collections[name];

        const template = document.getElementById(`${name}-template`);

        if (!root || !template) return;

        root.replaceChildren();

        const rows = items?.length ? items : [{}];

        rows.forEach(item => {

            root.append(template.content.cloneNode(true));

            const row = root.lastElementChild;

            row.querySelectorAll("[data-field]").forEach(field => {

                const propertyName = field.dataset.field;

                const camelName =

                    propertyName.charAt(0).toLowerCase() +

                    propertyName.slice(1);

                const value =

                    item[camelName] ??

                    item[propertyName];

                if (value === undefined || value === null) return;

                field.value =

                    field.type === "date" && typeof value === "string"

                        ? value.substring(0, 10)

                        : value;

            });

        });

        reindexCollection(name);

        if (name === "revisions") {

            initialiseRevisionAutoSync();

        }

    }

    function getProperty(object, propertyName) {

        const pascalName =

            propertyName.charAt(0).toUpperCase() +

            propertyName.slice(1);

        return object?.[propertyName] ?? object?.[pascalName];

    }

    function escapeHtml(value) {

        const div = document.createElement("div");

        div.textContent = value ?? "";

        return div.innerHTML;

    }

})();   