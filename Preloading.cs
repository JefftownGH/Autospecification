using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inventor;
using System.Windows;
using Library = InventorPlugins.OftenLibrary;

namespace AutoSpecification
{
	class Preloading
	{

		// Properties
		private Inventor.Application inventorApp;
		private Document oDoc;
		private Component mainComponent = new Component();
		// Constructors
		public Preloading(Inventor.Application ThisApplication)
		{
			inventorApp = ThisApplication;
			// Get active document
			oDoc = inventorApp.ActiveDocument;

			// Check whether the Document is assembly
			if (oDoc.DocumentType == DocumentTypeEnum.kAssemblyDocumentObject)
			{
				// Get component Data
				GetComponentData();
				// Call user form
				SpecificationForm oForm = new SpecificationForm(inventorApp, mainComponent);
				if (oForm.ShowDialog() == true)
				{

				}
				oForm = null;
			}
			else
			{
				MessageBox.Show("Откройте 3d-модель компонента", System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}

			//oDoc.Save2();
			//oDoc.Close();
			//oDoc = null;
		}

		// Methods

		private void GetComponentData()
		{
			try
			{
				// Get file info
				mainComponent.FullFileName = oDoc.FullFileName;
				PropertySet oPropSet = oDoc.PropertySets["Design Tracking Properties"];
				mainComponent.PartNumber = oPropSet["Part Number"].Value.ToString();
				mainComponent.Description = oPropSet["Description"].Value.ToString();
				oPropSet = oDoc.PropertySets["Inventor User Defined Properties"];
				mainComponent.FactoryNumber = oPropSet["Заводской номер"].Value.ToString();
				// Get quantity of units
				string propertyName = "Количество агрегатов";
				if (!Library.HasInventorProperty(oPropSet,propertyName))
				{
					Library.ChangeInventorProperty(oPropSet, propertyName, "1");
				}
				mainComponent.Quantity = oPropSet[propertyName].Value.ToString();
				mainComponent.AssemblyType = AssemblyTypes.Common;
				mainComponent.ComponentType = ComponentTypes.Assembly;

			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}


	}
}
