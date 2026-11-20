import pypdf, glob, sys
fs = sorted(glob.glob(sys.argv[1] + "/webfetch-*.pdf"))
r = pypdf.PdfReader(fs[0])
print(r.pages[4].extract_text()[:900]); print("-----")
for i, p in enumerate(r.pages):
    t = p.extract_text() or ""
    if "7.8" in t or "vida" in t: print("p", i + 1, [l for l in t.split("\n") if "7.8" in l or "vida" in l][:4])
r2 = pypdf.PdfReader(fs[1]); print("----- IZA"); print(r2.pages[0].extract_text()[:1800])
