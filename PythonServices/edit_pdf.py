import json
import sys
from io import BytesIO
from pathlib import Path
from collections import defaultdict
from pypdf import PdfReader, PdfWriter
from reportlab.pdfgen import canvas
 
 
def value(item, key, default=None):
    """Read JSON fields regardless of C# or JavaScript property casing."""
    if not isinstance(item, dict):
        return default

    if key in item:
        return item[key]

    normalized_key = key.casefold()
    for item_key, item_value in item.items():
        if str(item_key).casefold() == normalized_key:
            return item_value

    return default


def create_overlay(page_width, page_height, fields):
    packet = BytesIO()
    pdf = canvas.Canvas(packet, pagesize=(page_width, page_height))
 
    for field in fields:
        x = float(value(field, "X", 0) or 0) * page_width
        top = float(value(field, "Y", 0) or 0) * page_height
        width = max(1, float(value(field, "Width", 0) or 0) * page_width)
        height = max(1, float(value(field, "Height", 0) or 0) * page_height)
        y = page_height - top - height
 
        if bool(value(field, "CoverOriginal", True)):
            pdf.setFillColorRGB(1, 1, 1)
            pdf.rect(x, y, width, height, stroke=0, fill=1)
 
        text = str(value(field, "Text", "") or "")
        if not text:
            continue
 
        font_size = min(72, max(6, float(value(field, "FontSize", 12) or 12)))
        font_weight = str(value(field, "FontWeight", "regular") or "regular").lower()
        font_name = {
            "bold": "Times-Bold",
            "italic": "Times-Italic",
        }.get(font_weight, "Times-Roman")

        pdf.setFillColorRGB(0, 0, 0)
        pdf.setFont(font_name, font_size)
        leading = font_size * 1.2
        lines = text.replace("\r\n", "\n").replace("\r", "\n").split("\n")
        if bool(value(field, "AutoBullet", False)):
            lines = [
                line if not line.strip() else f"\u2022 {line.strip()}"
                for line in lines
            ]

        alignment = str(value(field, "Alignment", "left") or "left").lower()
        text_y = y + height - font_size

        for line in lines:
            if alignment == "center":
                text_width = pdf.stringWidth(line, font_name, font_size)
                text_x = x + max(2, (width - text_width) / 2)
            elif alignment == "right":
                text_width = pdf.stringWidth(line, font_name, font_size)
                text_x = x + max(2, width - text_width - 2)
            else:
                text_x = x + 2

            pdf.drawString(text_x, text_y, line)
            text_y -= leading
 
    pdf.save()
    packet.seek(0)
    return PdfReader(packet)
 
 
def edit_pdf(input_path, edits_path, output_path):
    reader = PdfReader(input_path)
    writer = PdfWriter()
    fields = json.loads(Path(edits_path).read_text(encoding="utf-8"))
    if not isinstance(fields, list):
        raise ValueError("PDF edits JSON must contain a list of fields.")
 
    fields_by_page = defaultdict(list)
 
    for field in fields:
        page_number = int(value(field, "Page", 0) or 0)
 
        if page_number > 0:
            fields_by_page[page_number].append(field)
 
    for page_number, original_page in enumerate(reader.pages, start=1):
        page_fields = fields_by_page.get(page_number, [])
 
        if page_fields:
            width = float(original_page.mediabox.width)
            height = float(original_page.mediabox.height)
            overlay = create_overlay(width, height, page_fields)
            original_page.merge_page(overlay.pages[0])
 
        try:
            original_page.compress_content_streams(level=9)
        except Exception:
            pass
 
        writer.add_page(original_page)
 
    Path(output_path).parent.mkdir(parents=True, exist_ok=True)
    with open(output_path, "wb") as output_file:
        writer.write(output_file)
 
 
if __name__ == "__main__":
    if len(sys.argv) != 4:
        raise SystemExit("Usage: edit_pdf.py input.pdf edits.json output.pdf")
    edit_pdf(sys.argv[1], sys.argv[2], sys.argv[3])
