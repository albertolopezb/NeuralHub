import pypdf, glob, re, sys
for f in sorted(glob.glob(sys.argv[1] + "/webfetch-*.pdf")):
    r = pypdf.PdfReader(f)
    print("==", f[-14:], len(r.pages), "pag", (r.metadata or {}).get("/Title"))
    for i, p in enumerate(r.pages):
        t = p.extract_text() or ""
        for m in re.finditer(r"[^\n]{0,100}(7\.8|esperanza de vida|4 de cada 10|cuatro de cada diez)[^\n]{0,100}", t):
            print(" p", i + 1, ":", m.group(0).strip()[:220])
            break
