import sys
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment
from openpyxl.utils import get_column_letter
from gestion import *

wb = Workbook()
hdr = PatternFill("solid", fgColor="00165C"); hf = Font(bold=True, color="FFFFFF")
def hoja(nombre, cols, filas, anchos):
    ws = wb.create_sheet(nombre)
    ws.append(cols)
    for c in ws[1]: c.fill = hdr; c.font = hf; c.alignment = Alignment(wrap_text=True, vertical="center")
    for f in filas: ws.append(list(f))
    for i, w in enumerate(anchos, 1): ws.column_dimensions[get_column_letter(i)].width = w
    for row in ws.iter_rows(min_row=2):
        for c in row: c.alignment = Alignment(wrap_text=True, vertical="top")
    ws.freeze_panes = "A2"
    return ws
wb.remove(wb.active)
hoja("Product backlog", ["ID", "Historia de usuario", "Usuario", "Prioridad", "Puntos", "Sprint", "Estado", "Criterios de aceptación"],
     [(h[0], h[1], h[2], h[3], h[4], h[5], h[6], " | ".join(CRITERIOS[h[0]])) for h in HISTORIAS], [8, 70, 14, 10, 8, 10, 18, 90])
hoja("Sprint backlog", ["ID", "Tarea", "Categoría", "Responsable", "Inicio", "Fin", "Estado", "Historia"], TAREAS, [7, 70, 34, 24, 10, 10, 12, 20])
hoja("Calendario", ["Sprint", "Inicio", "Cierre", "Enfoque"], SPRINTS, [12, 18, 18, 80])
hoja("Bitácora entrega 1", ["Actividad", "Responsable", "Fecha", "Nota"], BITACORA_E1, [70, 36, 22, 40])
hoja("Bitácora entrega 2", ["Actividad", "Responsable", "Fecha"], BITACORA_E2, [90, 40, 18])
filas = []
for m in MIEMBROS:
    n = [sum(1 for t in TAREAS if t[3] == m and t[2] == c) for c in (D, B, U)]
    filas.append((m, *n, sum(n)))
hoja("Participación", ["Integrante", "Documentación y planeación", "Programación multiagente (back-end)", "Programación en Unity (front-end)", "Total de tareas"], filas, [28, 22, 30, 28, 14])
wb.save("NeuralHub_Gestion.xlsx")
print(filas)
