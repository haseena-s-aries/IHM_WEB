import * as pdfjsLib from
    "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/4.10.38/pdf.min.mjs";

pdfjsLib.GlobalWorkerOptions.workerSrc =
    "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/4.10.38/pdf.worker.min.mjs";

const config = window.pdfEditorConfig;
const stage = document.getElementById("pdfStage");
const pagesHost = document.getElementById("pdfPages");
const status = document.getElementById("saveStatus");

let pdf = null;
let fields = [];
let selectedId = null;
let selectedPdfText = null;
let currentPage = 1;
let tool = "select";
let generatedPreviewMode = false;
const pageViews = new Map();
const clamp = (value, min, max) => Math.min(max, Math.max(min, value));
function newId() {
    if (window.crypto && typeof window.crypto.randomUUID === "function") {
        return window.crypto.randomUUID();
    }

    return `id-${Date.now()}-${Math.random().toString(36).slice(2, 11)}`;
}



async function initialize() {
    status.textContent = "Loading PDF...";

    try {
        await loadPdf();
        await loadFields();
        await renderAllPages();

        updateCurrentPage();
        renderFieldList();
        status.textContent = "Ready";
    } catch (error) {
        console.error(error);
        status.textContent = "PDF load failed";
        pagesHost.innerHTML =
            `<div class="pdf-load-error"><strong>Unable to display PDF.</strong><span>${escapeHtml(error.message)}</span></div>`;
    }
}

async function loadPdf(pdfBytes = null) {
    let bytes = pdfBytes;

    if (!bytes) {
        const separator = config.pdfUrl.includes("?") ? "&" : "?";
        const response = await fetch(
            `${config.pdfUrl}${separator}v=${Date.now()}`,
            { credentials: "same-origin", cache: "no-store" }
        );

        if (!response.ok)
            throw new Error(await responseMessage(response, "Saved report PDF was not found."));

        const contentType = response.headers.get("content-type") || "";
        if (!contentType.toLowerCase().includes("application/pdf"))
            throw new Error("The server did not return a PDF. Your session may have expired.");

        bytes = await response.arrayBuffer();
    }

    pdf = await pdfjsLib.getDocument({
        data: new Uint8Array(bytes)
    }).promise;

    document.getElementById("pageCount").textContent = pdf.numPages;
}

function escapeHtml(value) {
    const element = document.createElement("div");
    element.textContent = value || "";
    return element.innerHTML;
}

async function responseMessage(response, fallback) {
    const text = (await response.text()).trim();
    if (!text || text.startsWith("<!DOCTYPE") || text.startsWith("<html"))
        return `${fallback} (HTTP ${response.status})`;
    return text;
}

async function loadFields() {
    const response = await fetch(
        `${config.loadUrl}?type=${encodeURIComponent(config.type)}&id=${config.id}`
    );

    if (!response.ok) {
        fields = [];
        return;
    }

    const documentData = await response.json();

    fields = Array.isArray(documentData.fields)
        ? documentData.fields
        : [];
}

async function renderAllPages() {
    pagesHost.replaceChildren();
    pageViews.clear();

    const availableWidth = Math.max(600, stage.clientWidth - 100);

    for (let pageNumber = 1; pageNumber <= pdf.numPages; pageNumber++) {
        const page = await pdf.getPage(pageNumber);
        const baseViewport = page.getViewport({ scale: 1 });
        const scale = Math.min(1.5, availableWidth / baseViewport.width);
        const viewport = page.getViewport({ scale });

        const shell = document.createElement("div");
        shell.className = "pdf-page-shell";
        shell.dataset.page = pageNumber;

        const badge = document.createElement("span");
        badge.className = "page-badge";
        badge.textContent = `Page ${pageNumber}`;

        const canvas = document.createElement("canvas");
        canvas.className = "pdf-page-canvas";
        canvas.width = viewport.width;
        canvas.height = viewport.height;

        const textLayer = document.createElement("div");
        textLayer.className = "page-text-layer";

        const overlayLayer = document.createElement("div");
        overlayLayer.className = "page-overlay-layer";

        shell.style.width = `${viewport.width}px`;
        shell.style.height = `${viewport.height}px`;

        shell.append(canvas, textLayer, overlayLayer, badge);
        pagesHost.appendChild(shell);

        const view = {
            pageNumber,
            shell,
            canvas,
            textLayer,
            overlayLayer
        };

        pageViews.set(pageNumber, view);

        await page.render({
            canvasContext: canvas.getContext("2d"),
            viewport
        }).promise;

        await renderTextLayer(page, viewport, view);
        renderFieldsForPage(pageNumber);
        updateRemovedPageStyle(pageNumber);
    }
}

async function renderTextLayer(page, viewport, view) {
    const textContent = await page.getTextContent();

    for (const item of textContent.items) {
        const transform = pdfjsLib.Util.transform(
            viewport.transform,
            item.transform
        );

        const span = document.createElement("span");
        span.textContent = item.str;
        span.style.left = `${transform[4]}px`;
        span.style.top = `${transform[5] - Math.abs(transform[3])}px`;
        span.style.fontSize = `${Math.abs(transform[3])}px`;
        span.dataset.pdfText = "true";

        span.addEventListener("click", event => {
            if (tool !== "select") return;

            event.stopPropagation();
            selectPdfText(span, view);
        });

        view.textLayer.appendChild(span);
    }
}

function selectPdfText(span, view) {
    document.querySelectorAll(".pdf-text-selected")
        .forEach(x => x.classList.remove("pdf-text-selected"));

    span.classList.add("pdf-text-selected");

    selectedPdfText = { span, view };
    selectedId = null;
    currentPage = view.pageNumber;

    document.getElementById("pageNumber").textContent = currentPage;
    document.getElementById("fontSize").value =
        Math.max(6, Math.round(parseFloat(span.style.fontSize) || 12));

    status.textContent = `Selected: ${span.textContent}`;
}

function fieldFromSelectedText(text) {
    if (!selectedPdfText) {
        alert("Select PDF text first.");
        return null;
    }

    const { span, view } = selectedPdfText;
    const rect = span.getBoundingClientRect();
    const shellRect = view.shell.getBoundingClientRect();

    return {
        id: newId(),
        editType: "replace_text",
        page: view.pageNumber,
        x: clamp((rect.left - shellRect.left) / view.canvas.width, 0, 1),
        y: clamp((rect.top - shellRect.top) / view.canvas.height, 0, 1),
        width: clamp(
            Math.max(rect.width + 20, 120) / view.canvas.width,
            0.08,
            1
        ),
        height: clamp(
            Math.max(
                rect.height + 10,
                (Number(document.getElementById("fontSize").value) || 12) * 2
            ) / view.canvas.height,
            0.035,
            1
        ),
        text,
        fontSize: Number(document.getElementById("fontSize").value) || 12,
        fontWeight: document.getElementById("fontWeight").value,
        alignment: document.getElementById("textAlignment").value,
        autoBullet: document.getElementById("autoBullet").checked,
        coverOriginal: true
    };
}

function editSelectedPdfText() {
    const originalText =
        selectedPdfText?.span.textContent?.trim() || "";

    const field = fieldFromSelectedText(originalText);

    if (!field) return;
    generatedPreviewMode = false;
    fields.push(field);
    selectedId = field.id;

    selectedPdfText.span.classList.remove("pdf-text-selected");
    selectedPdfText = null;

    renderFieldsForPage(field.page);
    renderFieldList();

    const editBox = pageViews
        .get(field.page)
        ?.overlayLayer
        .querySelector(`[data-id="${field.id}"]`);

    const content = editBox?.querySelector(".field-content");

    if (content) {
        content.focus();

        const range = document.createRange();
        range.selectNodeContents(content);

        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);
    }
}

function deleteSelected() {
    if (selectedId) {
        generatedPreviewMode = false;
        const field = fields.find(x => x.id === selectedId);
        fields = fields.filter(x => x.id !== selectedId);
        selectedId = null;

        if (field) renderFieldsForPage(field.page);
        renderFieldList();
        return;
    }

    const field = fieldFromSelectedText("");
    if (!field) return;

    fields.push(field);
    selectedPdfText.span.classList.remove("pdf-text-selected");
    selectedPdfText = null;

    renderFieldsForPage(field.page);
    renderFieldList();
}

function addField() {
    generatedPreviewMode = false;
    const field = {
        id: newId(),
        editType: "text",
        page: currentPage,
        x: 0.12,
        y: 0.12,
        width: 0.28,
        height: 0.05,
        text: "Enter text",
        fontSize: Number(document.getElementById("fontSize").value) || 12,
        fontWeight: document.getElementById("fontWeight").value,
        alignment: document.getElementById("textAlignment").value,
        autoBullet: document.getElementById("autoBullet").checked,
        coverOriginal: document.getElementById("coverOriginal").checked
    };

    fields.push(field);
    selectedId = field.id;

    renderFieldsForPage(field.page);
    renderFieldList();
}

function renderFieldsForPage(pageNumber) {
    const view = pageViews.get(pageNumber);
    if (!view) return;

    view.overlayLayer.replaceChildren();

    fields
        .filter(x => x.page === pageNumber && x.editType !== "remove_page")
        .filter(x => !generatedPreviewMode || x.id === selectedId)
        .forEach(field => renderField(view, field));
}

function renderField(view, field) {
    const element = document.createElement("div");
    element.className =
        `edit-field${field.id === selectedId ? " selected" : ""}`;
    element.dataset.id = field.id;

    element.style.left = `${field.x * view.canvas.width}px`;
    element.style.top = `${field.y * view.canvas.height}px`;
    element.style.width = `${field.width * view.canvas.width}px`;
    element.style.height = `${field.height * view.canvas.height}px`;
    element.style.fontSize = `${field.fontSize}px`;
    element.style.fontWeight =
        field.fontWeight === "bold" ? "bold" : "normal";
    element.style.fontStyle =
        field.fontWeight === "italic" ? "italic" : "normal";
    element.style.textAlign = field.alignment || "left";

    const content = document.createElement("div");
    content.className = "field-content";
    content.contentEditable = "true";
    content.textContent = field.text || "";

    const moveHandle = document.createElement("button");
    moveHandle.type = "button";
    moveHandle.className = "move-handle";
    moveHandle.textContent = "+";
    moveHandle.title = "Drag to move";

    const resizeHandle = document.createElement("span");
    resizeHandle.className = "resize-handle";
    resizeHandle.title = "Drag to resize";

    element.append(content, moveHandle, resizeHandle);
    view.overlayLayer.appendChild(element);

    element.addEventListener("click", event => {
        event.stopPropagation();
        selectedId = field.id;
        currentPage = field.page;
        updateToolbar(field);
    });

    content.addEventListener("input", () => {
        field.text = content.textContent || "";
        renderFieldList();
    });

    content.addEventListener("blur", () => {
        syncFieldBounds(element, content, field, view);
    });

    bindMoveHandle(moveHandle, element, content, field, view);
    bindResizeHandle(resizeHandle, element, content, field, view);
}

function bindMoveHandle(handle, element, content, field, view) {
    handle.addEventListener("pointerdown", event => {
        event.preventDefault();
        event.stopPropagation();

        const startX = event.clientX;
        const startY = event.clientY;
        const originalX = element.offsetLeft;
        const originalY = element.offsetTop;

        const move = moveEvent => {
            element.style.left = `${clamp(
                originalX + moveEvent.clientX - startX,
                0,
                view.canvas.width - element.offsetWidth
            )}px`;

            element.style.top = `${clamp(
                originalY + moveEvent.clientY - startY,
                0,
                view.canvas.height - element.offsetHeight
            )}px`;
        };

        const up = () => {
            syncFieldBounds(element, content, field, view);
            window.removeEventListener("pointermove", move);
            window.removeEventListener("pointerup", up);
        };

        window.addEventListener("pointermove", move);
        window.addEventListener("pointerup", up);
    });
}

function bindResizeHandle(handle, element, content, field, view) {
    handle.addEventListener("pointerdown", event => {
        event.preventDefault();
        event.stopPropagation();

        const startX = event.clientX;
        const startY = event.clientY;
        const originalWidth = element.offsetWidth;
        const originalHeight = element.offsetHeight;

        const move = moveEvent => {
            element.style.width = `${clamp(
                originalWidth + moveEvent.clientX - startX,
                55,
                view.canvas.width - element.offsetLeft
            )}px`;

            element.style.height = `${clamp(
                originalHeight + moveEvent.clientY - startY,
                24,
                view.canvas.height - element.offsetTop
            )}px`;
        };

        const up = () => {
            syncFieldBounds(element, content, field, view);
            window.removeEventListener("pointermove", move);
            window.removeEventListener("pointerup", up);
        };

        window.addEventListener("pointermove", move);
        window.addEventListener("pointerup", up);
    });
}

function syncFieldBounds(element, content, field, view) {
    field.x = element.offsetLeft / view.canvas.width;
    field.y = element.offsetTop / view.canvas.height;
    field.width = element.offsetWidth / view.canvas.width;
    field.height = element.offsetHeight / view.canvas.height;
    field.text = content.textContent || "";
}

function updateToolbar(field) {
    document.getElementById("fontSize").value = field.fontSize;
    document.getElementById("fontWeight").value =
        field.fontWeight || "regular";
    document.getElementById("autoBullet").checked =
        Boolean(field.autoBullet);
    document.getElementById("coverOriginal").checked =
        Boolean(field.coverOriginal);
    document.getElementById("textAlignment").value =
        field.alignment || "left";
    document.getElementById("pageNumber").textContent = field.page;
}

function toggleRemoveCurrentPage() {
    const existing = fields.find(x =>
        x.page === currentPage && x.editType === "remove_page"
    );

    if (existing) {
        fields = fields.filter(x => x.id !== existing.id);
    } else {
        fields.push({
            id: newId(),
            editType: "remove_page",
            page: currentPage,
            x: 0,
            y: 0,
            width: 0,
            height: 0,
            text: "",
            fontSize: 12,
            fontWeight: "regular",
            autoBullet: false,
            coverOriginal: false
        });
    }

    updateRemovedPageStyle(currentPage);
    renderFieldList();
}

function updateRemovedPageStyle(pageNumber) {
    const view = pageViews.get(pageNumber);
    if (!view) return;

    const removed = fields.some(x =>
        x.page === pageNumber && x.editType === "remove_page"
    );

    view.shell.classList.toggle("page-marked-removed", removed);

    if (pageNumber === currentPage) {
        const removePageButton = document.getElementById("removePage");

        if (removePageButton) {
            removePageButton.textContent =
                removed ? "Restore current page" : "Remove current page";
        }
    }
}

function renderFieldList() {
    const list = document.getElementById("fieldList");
    list.replaceChildren();

    for (const field of fields) {
        const item = document.createElement("div");
        item.className = "field-list-item";

        const button = document.createElement("button");

        button.textContent = field.editType === "remove_page"
            ? `Page ${field.page}: Remove page`
            : `Page ${field.page}: ${field.text || "Empty field"}`;

        const removeButton = document.createElement("button");
        removeButton.type = "button";
        removeButton.className = "field-remove-button";
        removeButton.textContent = "Remove";
        removeButton.onclick = event => {
            event.stopPropagation();
            fields = fields.filter(x => x.id !== field.id);

            if (selectedId === field.id) {
                selectedId = null;
            }

            renderFieldsForPage(field.page);
            updateRemovedPageStyle(field.page);
            renderFieldList();
        };

        button.onclick = () => {
            currentPage = field.page;
            selectedId = field.id;

            pageViews.get(field.page)?.shell.scrollIntoView({
                behavior: "smooth",
                block: "start"
            });

            updateCurrentPage();
            renderAllVisibleFields();
        };

        item.append(button, removeButton);
        list.appendChild(item);
    }
}
function renderAllVisibleFields() {
    for (const pageNumber of pageViews.keys()) {
        renderFieldsForPage(pageNumber);
    }
}
function updateCurrentPage() {
    const stageRect = stage.getBoundingClientRect();
    let closestPage = currentPage;
    let closestDistance = Number.MAX_VALUE;

    for (const [pageNumber, view] of pageViews) {
        const rect = view.shell.getBoundingClientRect();
        const distance = Math.abs(rect.top - stageRect.top - 20);

        if (distance < closestDistance) {
            closestDistance = distance;
            closestPage = pageNumber;
        }
    }

    currentPage = closestPage;
    document.getElementById("pageNumber").textContent = currentPage;
    updateRemovedPageStyle(currentPage);
}

function payload() {
    return {
        reportType: config.type,
        reportId: config.id,
        fields
    };
}

async function save() {
    status.textContent = "Saving...";

    const response = await fetch(config.saveUrl, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "same-origin",
        body: JSON.stringify(payload())
    });

    if (response.ok) {
        status.textContent = "Saved";
        alert("PDF edits saved successfully.");
    } else {
        status.textContent = "Save failed";
        alert(await response.text());
    }
}

function setEditorBusy(isBusy) {
    document.body.classList.toggle("editor-busy", isBusy);

    document.querySelectorAll(
        ".editor-toolbar button, .editor-toolbar input, .editor-toolbar select"
    ).forEach(control => {
        control.disabled = isBusy;
    });

    document.querySelectorAll(".field-content").forEach(field => {
        field.contentEditable = isBusy ? "false" : "true";
    });

    document.querySelectorAll(
        ".move-handle, .resize-handle, .page-text-layer"
    ).forEach(element => {
        element.style.pointerEvents = isBusy ? "none" : "";
    });
}

async function generate() {
    status.textContent = "Saving edits...";
    setEditorBusy(true);

    try {
        // Save latest contentEditable values.
        document.querySelectorAll(".edit-field").forEach(element => {
            const field = fields.find(x => x.id === element.dataset.id);
            const view = field ? pageViews.get(field.page) : null;
            const content = element.querySelector(".field-content");

            if (field && view && content) {
                syncFieldBounds(element, content, field, view);
            }
        });

        const saveResponse = await fetch(config.saveUrl, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            credentials: "same-origin",
            body: JSON.stringify(payload())
        });

        if (!saveResponse.ok)
            throw new Error(await responseMessage(saveResponse, "Unable to save PDF edits."));

        status.textContent = "Generating PDF...";

        const response = await fetch(config.generateUrl, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            credentials: "same-origin",
            body: JSON.stringify(payload())
        });

        if (!response.ok)
            throw new Error(await responseMessage(response, "Unable to generate edited PDF."));

        status.textContent = "Opening edited PDF...";
        const generatedBlob = await response.blob();
        const generatedBytes = await generatedBlob.arrayBuffer();

        selectedId = null;
        selectedPdfText = null;
        generatedPreviewMode = true;

        await loadPdf(generatedBytes);
        await renderAllPages();
        renderFieldList();
        updateCurrentPage();
        renderAllVisibleFields();

        status.textContent = "PDF updated";
        alert("PDF generated successfully.");
    } catch (error) {
        console.error(error);
        status.textContent = "Generation failed";
        alert(`PDF generation failed: ${error.message}`);
    } finally {
        setEditorBusy(false);
    }
}
document.getElementById("selectTool").onclick = () => {
    tool = "select";
    document.body.classList.remove("add-text-mode");
};

document.getElementById("addTextTool").onclick = () => {
    tool = "write";
    addField();
};

document.getElementById("editSelectedText").onclick =
    editSelectedPdfText;

document.getElementById("deleteField").onclick =
    deleteSelected;

document.getElementById("removePage")?.addEventListener(
    "click",
    toggleRemoveCurrentPage
);

document.getElementById("saveEdits").onclick =
    save;

document.getElementById("generatePdf").onclick =
    generate;

document.getElementById("fontSize").onchange = event => {
    const field = fields.find(x => x.id === selectedId);
    if (!field) return;

    field.fontSize = Number(event.target.value) || 12;
    renderFieldsForPage(field.page);
};


document.getElementById("fontWeight").onchange = event => {
    const field = fields.find(x => x.id === selectedId);
    if (!field) return;

    field.fontWeight = event.target.value;
    renderFieldsForPage(field.page);
};
document.getElementById("textAlignment").onchange = event => {
    const field = fields.find(x => x.id === selectedId);
    if (!field) return;

    field.alignment = event.target.value;
    renderFieldsForPage(field.page);
};
document.getElementById("autoBullet").onchange = event => {
    const field = fields.find(x => x.id === selectedId);
    if (field) field.autoBullet = event.target.checked;
};

document.getElementById("coverOriginal").onchange = event => {
    const field = fields.find(x => x.id === selectedId);
    if (field) field.coverOriginal = event.target.checked;
};

stage.addEventListener("scroll", updateCurrentPage, { passive: true });

initialize().catch(error => {
    console.error(error);
    status.textContent = "PDF failed to load";
    alert(`PDF editor could not load: ${error.message}`);
});
