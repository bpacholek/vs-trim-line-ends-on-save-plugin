using System.Linq;
using System.Collections.Generic;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System;
using Microsoft.VisualStudio.TextManager.Interop;

using System.Windows.Forms;
using System.ComponentModel;

namespace idct.trimOnSave
{
    internal class FormatDocumentOnBeforeSave : IVsRunningDocTableEvents3
    {
        private DTE _dte;
        private IVsTextManager _txtMngr;
        private SettingsPage _settingsPage;
        private RunningDocumentTable _runningDocumentTable;

        public FormatDocumentOnBeforeSave(DTE dte, RunningDocumentTable runningDocumentTable, IVsTextManager txtMngr, SettingsPage settingsPage)
        {
            _runningDocumentTable = runningDocumentTable;
            _txtMngr = txtMngr;
            _dte = dte;
            _settingsPage = settingsPage;
        }

        struct scrollData
        {
            public int piMinUnit;
            public int piMaxUnit;
            public int piVisibleUnits;
            public int piFirstVisible;
        }

        struct breakPointData
        {
            public string functionName;
            public int fileLine;
            public int fileColumn;
            public string condition;
            public dbgBreakpointConditionType conditionType;
            public dbgHitCountType hitCountType;
            public string language;
            public int hitCount;
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

            //preserving breakpoints
            List<breakPointData> breakPointsPreserved = new List<breakPointData>();
            foreach (Breakpoint breakPoint in _dte.Debugger.Breakpoints)
            {
                if(breakPoint.File == _dte.ActiveDocument.FullName)
                {
                    breakPointData newBreakPoint = new breakPointData();

                    newBreakPoint.fileColumn = breakPoint.FileColumn;
                    newBreakPoint.fileLine = breakPoint.FileLine;
                    newBreakPoint.language = breakPoint.Language;
                    newBreakPoint.condition = breakPoint.Condition;
                    newBreakPoint.functionName = breakPoint.FunctionName;
                    newBreakPoint.conditionType = breakPoint.ConditionType;

                    newBreakPoint.hitCount = breakPoint.CurrentHits;
                    newBreakPoint.hitCountType = breakPoint.HitCountType;

                    breakPointsPreserved.Add(newBreakPoint);
                }
            }



            //preserving scroll position
            scrollData horizontal = new scrollData();
            scrollData vertical = new scrollData();
            textViewCurrent.GetScrollInfo(0,out horizontal.piMinUnit, out horizontal.piMaxUnit, out horizontal.piVisibleUnits, out horizontal.piFirstVisible);
            textViewCurrent.GetScrollInfo(1,out vertical.piMinUnit, out vertical.piMaxUnit, out vertical.piVisibleUnits, out vertical.piFirstVisible);

            //reading
            string text = GetDocumentText(document);

            //getting options
            string endLineSymbol = "";
            switch(_settingsPage.newLine)
            {
                case SettingsPage.NewLineSymbol.Current:
                    endLineSymbol = "$1";
                    break;
                case SettingsPage.NewLineSymbol.Windows:
                    endLineSymbol = "\r\n";
                    break;
                case SettingsPage.NewLineSymbol.Unix:
                    endLineSymbol = "\n";
                    break;
                case SettingsPage.NewLineSymbol.VisualStudio:
                    endLineSymbol = Environment.NewLine;
                    break;
                default:
                    endLineSymbol = Environment.NewLine;
                    break;
            }

            text = Regex.Replace(text, "[ \t]+?(\r\n|\n|\r)", endLineSymbol, RegexOptions.Compiled);

            //fixed to avoid adding a new line
            text = Regex.Replace(text, "[ \t]+?($)", "$1", RegexOptions.Compiled|RegexOptions.Multiline);

            //replace EOLs based on the setting
            text = Regex.Replace(text, "(\r\n|\n|\r)", endLineSymbol, RegexOptions.Compiled);

            //setting
            SetDocumentText(document, text);

            //we need to change the column position as it is possible that there were spaces before the trim operation
            StringReader reader = new StringReader(text);
            int currentLine = 0;
            bool breaked = false;
            while(reader.Peek() != -1)
            {
                string currentLineText = reader.ReadLine();
                if (currentLine < line)
                {
                    ++currentLine;
                }
                else
                {
                    if (column > currentLineText.Length)
                    {
                        column = currentLineText.Length;
                    }
                    breaked = true;
                    break;
                }
            }

            //special case: empty last line
            if(breaked == false)
            {
                column = 0;
            }

            reader.Close();
            reader = null;

            //restoring breakpoints
            foreach (breakPointData breakPoint in breakPointsPreserved)
            {
                _dte.Debugger.Breakpoints.Add("", Path.GetFileName(_dte.ActiveDocument.FullName), breakPoint.fileLine, breakPoint.fileColumn, breakPoint.condition, breakPoint.conditionType, breakPoint.language,"",1,"",breakPoint.hitCount,breakPoint.hitCountType);
            }

            //restoring cursor position
            textViewCurrent.SetCaretPos(line, column);

            //restoring scroll
            textViewCurrent.SetScrollPosition(0,horizontal.piFirstVisible);
            textViewCurrent.SetScrollPosition(1,vertical.piFirstVisible);

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
