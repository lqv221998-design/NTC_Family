using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace NTC.FamilyManager.Base
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
                        // Success, we can also prompt to place it
                        // TaskDialog.Show("Success", $"Loaded family: {family.Name}");
                        
                        // Optional: Activate placement
                        // uiDoc.PostRequestForPlacement(family.GetFamilySymbolIds().First().ToElement(doc) as FamilySymbol);
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
