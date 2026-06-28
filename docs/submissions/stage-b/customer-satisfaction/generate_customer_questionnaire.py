from pathlib import Path

from bidi.algorithm import get_display
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen import canvas
from pypdf import PdfReader, PdfWriter
from pypdf.generic import NameObject, NumberObject


OUT = Path(__file__).with_name("GhostHouses_Customer_Questionnaire.pdf")
FONT_PATH = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
FONT_BOLD_PATH = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
FIELD_BORDER = colors.HexColor("#94A3B8")
FIELD_FILL = colors.HexColor("#FFFFFF")
FIELD_TEXT = colors.HexColor("#111827")


def rtl(text: str) -> str:
    return get_display(text)


def draw_rtl(c: canvas.Canvas, text: str, x: float, y: float, size: int = 11, bold: bool = False):
    c.setFont("DejaVuSans-Bold" if bold else "DejaVuSans", size)
    c.drawRightString(x, y, rtl(text))


def draw_centered_rtl(c: canvas.Canvas, text: str, x: float, y: float, size: int = 11, bold: bool = False):
    font_name = "DejaVuSans-Bold" if bold else "DejaVuSans"
    rendered = rtl(text)
    c.setFont(font_name, size)
    c.drawString(x - c.stringWidth(rendered, font_name, size) / 2, y, rendered)


def field_line(c: canvas.Canvas, label: str, name: str, y: float):
    width, _ = A4
    draw_rtl(c, label, width - 50, y + 4, 11, True)
    c.acroForm.textfield(
        name=name,
        x=55,
        y=y - 3,
        width=345,
        height=20,
        borderColor=FIELD_BORDER,
        borderWidth=0.8,
        fillColor=FIELD_FILL,
        textColor=FIELD_TEXT,
        forceBorder=True,
        fontSize=11,
    )


def rating_row(c: canvas.Canvas, label: str, name: str, y: float):
    width, _ = A4
    draw_rtl(c, label, width - 50, y + 2, 10)
    start_x = 70
    gap = 38
    for i in range(1, 6):
        c.acroForm.checkbox(
            name=f"{name}-{i}",
            x=start_x + (i - 1) * gap,
            y=y - 3,
            size=13,
            borderColor=FIELD_BORDER,
            borderWidth=0.8,
            buttonStyle="check",
            fillColor=FIELD_FILL,
            textColor=FIELD_TEXT,
            fieldFlags="",
            forceBorder=True,
        )


def apply_rtl_field_settings(path: Path):
    reader = PdfReader(path)
    writer = PdfWriter()
    writer.clone_document_from_reader(reader)

    for page in writer.pages:
        annotations = page.get("/Annots") or []
        for annotation_ref in annotations:
            annotation = annotation_ref.get_object()
            if annotation.get("/FT") == "/Tx":
                # /Q 2 is the standard PDF field quadding value for right alignment.
                annotation[NameObject("/Q")] = NumberObject(2)

    tmp = path.with_suffix(".tmp.pdf")
    with tmp.open("wb") as output:
        writer.write(output)
    tmp.replace(path)


def main():
    pdfmetrics.registerFont(TTFont("DejaVuSans", FONT_PATH))
    pdfmetrics.registerFont(TTFont("DejaVuSans-Bold", FONT_BOLD_PATH))

    c = canvas.Canvas(str(OUT), pagesize=A4)
    width, height = A4
    y = height - 50

    draw_centered_rtl(c, "שאלון שביעות רצון לקוח", width / 2, y, 20, True)
    y -= 26
    draw_centered_rtl(c, "פרויקט מבנים נטושים", width / 2, y, 13, True)
    y -= 20
    draw_centered_rtl(c, "עיריית חיפה", width / 2, y, 12)

    y -= 42
    draw_rtl(c, "פרטי המשיב", width - 50, y, 14, True)
    y -= 28
    for label, name in [
        ("שם מלא:", "respondent_name"),
        ("תפקיד:", "respondent_role"),
        ("תאריך:", "response_date"),
    ]:
        field_line(c, label, name, y)
        y -= 32

    y -= 8
    draw_rtl(c, "דירוג שביעות רצון", width - 50, y, 14, True)
    y -= 22
    draw_rtl(c, "נא לסמן דירוג מ-1 עד 5, כאשר 1 = לא מספק, ו-5 = מצוין.", width - 50, y, 10)
    y -= 28
    for i in range(1, 6):
        c.setFont("DejaVuSans-Bold", 10)
        c.drawCentredString(76 + (i - 1) * 38, y + 2, str(i))
    y -= 22
    for label, name in [
        ("שביעות רצון כללית מהמערכת", "overall"),
        ("המערכת עונה על הצרכים שהוגדרו לפרויקט", "needs_match"),
        ("ניהול מאגר המבנים ברור ונוח לשימוש", "buildings"),
        ("ייבוא, ייצוא והפקת כרטיסי מבנה עובדים בצורה שימושית", "import_export_cards"),
        ("שילוב ה-GIS והמפה מסייעים להבנת מיקום המבנים", "gis"),
        ("המערכת מוכנה להמשך הטמעה והעברה לצוות העירייה", "handoff"),
    ]:
        rating_row(c, label, name, y)
        y -= 30

    y -= 24
    draw_rtl(c, "אישור", width - 50, y, 14, True)
    y -= 28
    for label, name in [
        ("שם המאשר:", "approver_name"),
        ("חתימה:", "signature"),
        ("תאריך:", "signature_date"),
    ]:
        field_line(c, label, name, y)
        y -= 32

    c.save()
    apply_rtl_field_settings(OUT)
    print(OUT)


if __name__ == "__main__":
    main()
