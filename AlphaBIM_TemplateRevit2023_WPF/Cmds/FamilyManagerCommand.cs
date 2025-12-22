
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

            try
            {
                // Khởi tạo trình nạp thư viện để tránh lỗi thiếu DLL giao diện (WPF)
                using (var loader = new AssemblyLoader())
                {
                    using (TransactionGroup tranGroup = new TransactionGroup(doc))
                    {
                        tranGroup.Start("NTC.FamilyManagerTransGr");

                        MainViewModel viewModel = new MainViewModel(uidoc);
                        MainWindow window = new MainWindow(viewModel);
                        
                        // Hiển thị giao diện
                        window.ShowDialog();

                        tranGroup.Assimilate();
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("NTC Error", "Có lỗi xảy ra: " + ex.Message);
                return Result.Failed;
            }

            return Result.Succeeded;

        }
    }
}
