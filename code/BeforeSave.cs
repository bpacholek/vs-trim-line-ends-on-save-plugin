using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System;
using Microsoft.VisualStudio.TextManager.Interop;

namespace idct.trimOnSave
{
    internal class FormatDocumentOnBeforeSave : IVsRunningDocTableEvents3
    {
        private DTE _dte;
        private IVsTextManager _txtMngr;
        private RunningDocumentTable _runningDocumentTable;

        public FormatDocumentOnBeforeSave(DTE dte, RunningDocumentTable runningDocumentTable, IVsTextManager txtMngr)
        {
            _runningDocumentTable = runningDocumentTable;
            _txtMngr = txtMngr;
            _dte = dte;
        }

        public int OnBeforeSave(uint docCookie)
        {
            var document = FindDocument(docCookie);

            //if no document - do nothing
            if (document == null)
                return VSConstants.S_OK;

            //preserving cursor position
            IVsTextView textViewCurrent;
            _txtMngr.GetActiveView(1, null, out textViewCurrent);
            int line = 0;
            int column = 0;
            textViewCurrent.GetCaretPos(out line, out column);

            //reading
            string text = GetDocumentText(document);

            text = Regex.Replace(text, "[ \t]+?(\r\n|\n|\r|$)", Environment.NewLine, RegexOptions.Compiled);

            //setting
            SetDocumentText(document, text);

            //restoring cursor position
            textViewCurrent.SetCaretPos(line, column);

            return VSConstants.S_OK;
        }

        private Document FindDocument(uint docCookie)
        {
            var documentInfo = _runningDocumentTable.GetDocumentInfo(docCookie);
            var documentPath = documentInfo.Moniker;

            return _dte.Documents.Cast<Document>().FirstOrDefault(doc => doc.FullName == documentPath);
        }

        private static string GetDocumentText(Document document)
        {
            var textDocument = (TextDocument)document.Object("TextDocument");
            EditPoint editPoint = textDocument.StartPoint.CreateEditPoint();
            var content = editPoint.GetText(textDocument.EndPoint);
            return content;
        }

        private static void SetDocumentText(Document document, string content)
        {
            var textDocument = (TextDocument)document.Object("TextDocument");
            EditPoint editPoint = textDocument.StartPoint.CreateEditPoint();
            EditPoint endPoint = textDocument.EndPoint.CreateEditPoint();
            editPoint.ReplaceText(endPoint, content, 0);
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRdtLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRdtLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterSave(uint docCookie)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents3.OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld,
            string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            return VSConstants.S_OK;
        }


        int IVsRunningDocTableEvents2.OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld,
            string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            return VSConstants.S_OK;
        }
    }
}
