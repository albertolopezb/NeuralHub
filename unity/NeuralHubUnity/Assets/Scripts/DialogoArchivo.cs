using System;
using System.Runtime.InteropServices;

namespace NeuralHub
{
    /// <summary>Ventana de Windows para elegir el documento del pitch (en el editor usa la de Unity).</summary>
    public static class DialogoArchivo
    {
#if UNITY_EDITOR
        public static string Elegir()
        {
            return UnityEditor.EditorUtility.OpenFilePanel("Elige tu pitch", "", "docx,pdf,txt,md");
        }
#elif UNITY_STANDALONE_WIN
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        class OpenFileName
        {
            public int lStructSize; public IntPtr hwndOwner; public IntPtr hInstance; public string lpstrFilter;
            public string lpstrCustomFilter; public int nMaxCustFilter; public int nFilterIndex; public IntPtr lpstrFile;
            public int nMaxFile; public IntPtr lpstrFileTitle; public int nMaxFileTitle; public string lpstrInitialDir;
            public string lpstrTitle; public int Flags; public short nFileOffset; public short nFileExtension; public string lpstrDefExt;
            public IntPtr lCustData; public IntPtr lpfnHook; public string lpTemplateName; public IntPtr pvReserved; public int dwReserved; public int FlagsEx;
        }

        [DllImport("comdlg32.dll", EntryPoint = "GetOpenFileNameW", CharSet = CharSet.Unicode)]
        static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        [DllImport("user32.dll")]
        static extern IntPtr GetActiveWindow();

        public static string Elegir()
        {
            const int max = 2048;
            var bufer = Marshal.AllocHGlobal(max * 2);
            try
            {
                Marshal.WriteInt16(bufer, 0);
                var o = new OpenFileName
                {
                    hwndOwner = GetActiveWindow(), lpstrFilter = "Pitch (docx, pdf, txt, md)\0*.docx;*.pdf;*.txt;*.md\0\0",
                    lpstrFile = bufer, nMaxFile = max, lpstrTitle = "Elige tu pitch",
                    Flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008,   // explorador, debe existir, no cambiar de carpeta
                };
                o.lStructSize = Marshal.SizeOf(o);
                return GetOpenFileName(o) ? Marshal.PtrToStringUni(bufer) : null;
            }
            finally { Marshal.FreeHGlobal(bufer); }
        }
#else
        public static string Elegir() { return null; }
#endif
    }
}
