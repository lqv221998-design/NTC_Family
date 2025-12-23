using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace NTC.FamilyManager.Infrastructure.Revit
{
    public class RevitRequestHandler : IExternalEventHandler
    {
        public string FamilyPath { get; set; }

        public void Execute(UIApplication app)
        {
            if (string.IsNullOrEmpty(FamilyPath)) return;

            UIDocument uiDoc = app.ActiveUIDocument;
            Document doc = uiDoc.Document;

            using (Transaction trans = new Transaction(doc, "Load Family from Manager"))
            {
                trans.Start();
                try
                {
                    bool loaded = doc.LoadFamily(FamilyPath, out Family family);
                    if (loaded)
                    {
                        // Family loaded successfully
                    }
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    trans.RollBack();
                    TaskDialog.Show("Error", $"Could not load family: {ex.Message}");
                }
            }
        }

        public string GetName()
        {
            return "NTC Family Manager Handler";
        }
    }
}
