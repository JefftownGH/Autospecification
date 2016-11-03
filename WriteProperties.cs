using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inventor;
using File = System.IO.File;
using Path = System.IO.Path;
using Directory = System.IO.Directory;
using System.Diagnostics;
using System.Windows;
using Library = InventorPlugins.OftenLibrary;

namespace AutoSpecification
{
	class WriteProperties
	{
		// Properties
		private Inventor.Application inventorApp;
		private AssemblyDocument mainAssembly;
		private Component mainComponent;
		private string projectDirectory;
		private List<string> subDirectories = new List<string>();
		// Constructors
		public WriteProperties(Inventor.Application ThisApplication, AssemblyDocument inputAssembly, Component inputComponent, string inputDirectory)
		{
			 
			inventorApp = ThisApplication;
			mainAssembly = inputAssembly;
			mainComponent = inputComponent;
			projectDirectory = inputDirectory;
			try
			{
				// Get subdirectories
				GetSubDirectories();				
				// Set properties of main assembly
				PropertySet oPropSet = mainAssembly.PropertySets["Design Tracking Properties"];
				oPropSet["Part Number"].Value=mainComponent.PartNumber ;
				oPropSet["Description"].Value=mainComponent.Description;
				oPropSet = mainAssembly.PropertySets["Inventor User Defined Properties"];
				oPropSet["Заводской номер"].Value= mainComponent.FactoryNumber;
				// Iterate through assembly
				IterateAssemblyRecursive(mainAssembly.ComponentDefinition.Occurrences);
				// Replace referencies for main components (ТМ,ТС,ТП, Casing, Frame)
				ReplaceMainReferencies();
				
				mainAssembly.Save2();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}
		
		// Methods
		private void GetSubDirectories()
		{
			try
			{
				// Add general sub directories
				string subDirectory = Path.Combine(projectDirectory, "Корпус");
				if (Directory.Exists(subDirectory))
					subDirectories.Add(subDirectory);
				subDirectory = Path.Combine(projectDirectory, "Трубы");
				if (Directory.Exists(subDirectory))
					subDirectories.Add(subDirectory);
				subDirectory = Path.Combine(projectDirectory, "Рама");
				if (Directory.Exists(subDirectory))
					subDirectories.Add(subDirectory);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		private void IterateAssemblyRecursive(ComponentOccurrences occurrences)
		{
			try
			{
				foreach (ComponentOccurrence occurrence in occurrences)
				{
					
					if (occurrence.DefinitionDocumentType == DocumentTypeEnum.kAssemblyDocumentObject)
					{
						ChangeProperties(occurrence);
						IterateAssemblyRecursive((ComponentOccurrences)occurrence.SubOccurrences);
					}
					else if ((occurrence.DefinitionDocumentType == DocumentTypeEnum.kPartDocumentObject))
					{
						ChangeProperties(occurrence);
					}
				}

			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		private void ChangeProperties(ComponentOccurrence occurrence)
		{
			try
			{
				Document locDoc = (Document)occurrence.Definition.Document;
				string filePath = locDoc.FullFileName;
				if (IsComponentInSubDirectories(filePath))
				{
					// Set properties of main assembly
					PropertySet oPropSet = locDoc.PropertySets["Design Tracking Properties"];
					oPropSet["Project"].Value = mainComponent.PartNumber;
					oPropSet = locDoc.PropertySets["Inventor User Defined Properties"];
					Library.ChangeInventorProperty(oPropSet, "Заводской номер", mainComponent.FactoryNumber);
				}

			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		private bool IsComponentInSubDirectories(string filePath)
		{
			try
			{
				bool ok = false;
				foreach (string subDirectory in subDirectories)
				{
					if (filePath.IndexOf(subDirectory)>=0)
					{
						ok = true;
						break;
					}
				}
				return ok;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
				return false;
			}
		}

		private void ReplaceMainReferencies()
		{
			try
			{
				ReplaceReferences replaceReferences = new ReplaceReferences(inventorApp,
																mainAssembly,
																mainComponent);
				replaceReferences = null;
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

	}
}
