"""Compara el Google Doc actual (JSON de la API) con el documento generado, para ver qué corrigió el equipo."""
import difflib
import json
import re
import sys
from pathlib import Path

import docx

SRC = sys.argv[1]
J = json.load(open(SRC, encoding="utf8"))
tab = J["content"]["tabs"][0]
body = tab["documentTab"]["body"]["content"]


def par_text(p):
    return "".join(e.get("textRun", {}).get("content", "") for e in p.get("elements", [])).replace("\x0b", " ").strip()


def walk(content):
    out = []
    for el in content:
        if "paragraph" in el:
            t = par_text(el["paragraph"])
            if t:
                out.append(t)
        elif "table" in el:
            for row in el["table"]["tableRows"]:
                cells = []
                for c in row["tableCells"]:
                    cells.append(" ".join(walk(c["content"])))
                out.append(" | ".join(cells))
    return out


actual = walk(body)

d = docx.Document(str(Path(__file__).parent / "NeuralHub_Primer_Incremento.docx"))
mio = []
from docx.oxml.ns import qn
for el in d.element.body.iterchildren():
    if el.tag == qn("w:p"):
        t = docx.text.paragraph.Paragraph(el, d).text.strip()
        if t:
            mio.append(t)
    elif el.tag == qn("w:tbl"):
        tb = docx.table.Table(el, d)
        for r in tb.rows:
            seen, cells = [], []
            for c in r.cells:
                if c._tc in seen:
                    continue
                seen.append(c._tc)
                cells.append(" ".join(p.text.strip() for p in c.paragraphs if p.text.strip()))
            mio.append(" | ".join(cells))


def norm(s):
    return re.sub(r"\s+", " ", s).strip()


A = [norm(x) for x in actual]
M = [norm(x) for x in mio]
Path(Path(__file__).parent / "doc_actual.txt").write_text("\n".join(A), encoding="utf8")
sm = difflib.SequenceMatcher(None, M, A, autojunk=False)
for tag, i1, i2, j1, j2 in sm.get_opcodes():
    if tag == "equal":
        continue
    print(f"--- {tag} (mío {i1}-{i2} / actual {j1}-{j2})")
    for x in M[i1:i2]:
        print("  - " + x[:400])
    for x in A[j1:j2]:
        print("  + " + x[:400])
print("TOTAL líneas mío", len(M), "actual", len(A))
