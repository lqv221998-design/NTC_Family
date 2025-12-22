
#region Namespaces

using System;
using System.IO;
using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Application = Autodesk.Revit.ApplicationServices.Application;

#endregion

namespace NTC.FamilyManager
{
    [Transaction(TransactionMode.Manual)]
    public class FamilyManagerCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, 
            ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // Khi chạy bằng Add-in Manager thì comment 2 dòng bên dưới để tránh lỗi
            // When running with Add-in Manager, comment the 2 lines below to avoid errors
            string dllFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            
            // Note: AssemblyLoader might need namespace update too
            AssemblyLoader.LoadAllRibbonAssemblies(dllFolder);


            // code here

            using (TransactionGroup tranGroup = new TransactionGroup(doc))
            {
              tranGroup.Start("NTC.FamilyManagerTransGr");

              MainViewModel viewModel = new MainViewModel(uidoc);
              MainWindow window = new MainWindow(viewModel);
              if (window.ShowDialog() == false) return Result.Cancelled;

              tranGroup.Assimilate();
            }

            return Result.Succeeded;

        }
    }
}
