using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Windows.Forms;
using System.ComponentModel;

namespace idct.trimOnSave
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.guidVSPackage3PkgString)]
    [ProvideOptionPage(typeof(SettingsPage),
    "Text Editor", "Trim on save", 0, 0, true)]
    [ProvideAutoLoad("{f1536ef8-92ec-443c-9ed7-fdadf150da82}")] //To set the UI context to autoload a VSPackage
    public sealed class trimOnSavePackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require
        /// any Visual Studio service because at this point the package object is created but
        /// not sited yet inside Visual Studio environment. The place to do all the other
        /// initialization is the Initialize method.
        /// </summary>
        public trimOnSavePackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            var dte = (DTE)GetService(typeof(DTE));

            var txtMgr = (IVsTextManager)GetService(typeof(SVsTextManager));
            var runningDocumentTable = new RunningDocumentTable(this);
            SettingsPage options = (SettingsPage)GetDialogPage(typeof(SettingsPage));
            var plugin = new FormatDocumentOnBeforeSave(dte, runningDocumentTable, txtMgr, options);
            runningDocumentTable.Advise(plugin);
            base.Initialize();

        }

        #endregion

    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("1D9ECCF3-5D2F-4112-9B25-264596873DC9")]
    public class SettingsPage : DialogPage
    {
        public enum NewLineSymbol {
            Windows=0,
            Unix=1,
            VisualStudio=2,
            Current=3
        };
        private NewLineSymbol newLineSymbol_Value = NewLineSymbol.VisualStudio;

        private int[] myNumbers;

        [Category("On Save")]
        public NewLineSymbol newLine
        {
            get { return newLineSymbol_Value; }
            set { newLineSymbol_Value = value; }
        }
    }
}
