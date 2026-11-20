import pypdf, glob, sys
fs = sorted(glob.glob(sys.argv[1] + "/webfetch-*.pdf"))
r = pypdf.PdfReader(fs[1])
t = r.pages[2].extract_text()
print(t[:1700])
