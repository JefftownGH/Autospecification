using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inventor;
using System.Windows;
using File = System.IO.File;
using Path = System.IO.Path;
using Directory = System.IO.Directory;
using System.Diagnostics;
using Library = InventorPlugins.OftenLibrary;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace AutoSpecification
{
	class ReplaceReferences
	{
		// Properties
		private Inventor.Application inventorApp;
		private AssemblyDocument mainAssembly;
		private Component mainComponent;
		private AssemblyDocument casingAssembly;
		//private AssemblyDocument frameAssembly;
		private Component casingComponent;
		//private Component frameComponent;
		private string projectDirectory;
		private string casingDirectory;
		private string frameDirectory;
		List<Component> componentsToReplace = new List<Component>();
		List<Component> partsToReplace = new List<Component>();
		List<Component> assembliesToReplace = new List<Component>();

		// Constructors
		public ReplaceReferences(Inventor.Application ThisApplication, 
								AssemblyDocument inputAssembly, 
								Component inputComponent,
								string inputDirectory)
		{

			inventorApp = ThisApplication;
			casingAssembly = inputAssembly;
			casingComponent = inputComponent;
			projectDirectory = inputDirectory;
			try
			{
				// Add casing sub directory
				string subDirectory = Path.Combine(projectDirectory, "Корпус");
				if (Directory.Exists(subDirectory))
				{
					casingDirectory = subDirectory;
					List<Component> LSPKits = GetLSPKits();
					// Search 
					foreach (Component component in LSPKits)
					{
						SearchSheetMetalKits(component);
					}
				}
				else
				{
					MessageBox.Show("Папка \"Корпус\" не найдена в проекте.", "Замена ссылок в комплектах деталей не будет произведена", MessageBoxButton.OK);
				}

				// Add frame sub directory
				subDirectory = Path.Combine(projectDirectory, "Рама");
				if (Directory.Exists(subDirectory))
				{
					frameDirectory = subDirectory;
					List<Component> frames = GetFrames();
					// Renumber
					foreach (Component component in frames)
					{
						RenumberFrame(component);
					}
				}
				else
				{
					MessageBox.Show("Папка \"Рама\" не найдена в проекте.", "Замена ссылок в комплектах деталей не будет произведена", MessageBoxButton.OK);
				}

				if (componentsToReplace.Count > 0)
				{
					GetReplaceCollections();
					// Sort assemblies
					assembliesToReplace = assembliesToReplace.OrderByDescending(o => o.Level).ToList();
					foreach (Component component in partsToReplace)
					{
						ReplaceComponentReferencies(component);
					}
					casingAssembly.Save2();
					foreach (Component component in assembliesToReplace)
					{
						ReplaceComponentReferencies(component);
						casingAssembly.Save2();
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		// Constructor for main components
		public ReplaceReferences(Inventor.Application ThisApplication,
								AssemblyDocument inputAssembly,
								Component inputComponent)
		{

			inventorApp = ThisApplication;
			mainAssembly = inputAssembly;
			mainComponent = inputComponent;
			try
			{
				List<Component> mainComponents =  GetMainComponents();

				if (mainComponents.Count > 0)
				{
					foreach (Component component in mainComponents)
					{
						ReplaceComponentReferencies(component);
						mainAssembly.Save2();
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		private List<Component> GetMainComponents()
		{
			List<Component> mainComponents = new List<Component>();
			try
			{
				foreach (Component component in mainComponent.Components)
				{
					if (component.AssemblyType == AssemblyTypes.Casing)
					{
						ChangePartNumber(component, "C");
						component.Level = 1;
						mainComponents.Add(component);
						foreach (Component subComponent in component.Components)
						{
							if (subComponent.CasingType == CasingTypes.Frame)
							{
								ChangePartNumber(subComponent, "F");
								component.Level = 2;
								mainComponents.Add(subComponent);
							}
						}
					}
					if (component.AssemblyType == AssemblyTypes.ТМ)
					{
						ChangePartNumber(component, "ТМ");
						component.Level = 1;
						mainComponents.Add(component);
					}
					if (component.AssemblyType == AssemblyTypes.ТС)
					{
						ChangePartNumber(component, "ТС");
						component.Level = 1;
						mainComponents.Add(component);
					}
					if (component.AssemblyType == AssemblyTypes.ТП)
					{
						ChangePartNumber(component, "ТП");
						component.Level = 1;
						mainComponents.Add(component);
					}
				}
				// Sort assemblies
				mainComponents = mainComponents.OrderByDescending(o => o.Level).ToList();
				return mainComponents;
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
				return mainComponents;
			}
		}


		private void ChangePartNumber(Component component, string add)
		{
			try
			{

				Document locDoc = (Document)inventorApp.Documents.Open(component.FullFileName, false);
				// Set properties of main assembly
				PropertySet oPropSet = locDoc.PropertySets["Design Tracking Properties"];
				component.PartNumber = add + " " + mainComponent.FactoryNumber;
				oPropSet["Part Number"].Value = component.PartNumber;
				locDoc.Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		#region SheetMetalKits

		// Search for LSP kits ("Комплект ЛСП")
		private List<Component> GetLSPKits()
		{
			List<Component> LSPKits = new List<Component>();
			try
			{
				foreach (Component component in casingComponent.Components)
				{
					if (component.CasingType == CasingTypes.ЛСП)
					{
						LSPKits.Add(component);
					}
				}
				return LSPKits;
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
				return LSPKits;
			}
		}

		private void SearchSheetMetalKits(Component component)
		{
			try
			{
				AssemblyDocument assembly = (AssemblyDocument)inventorApp.Documents.Open(component.FullFileName, false);
				// Get BOM
				BOM bom = assembly.ComponentDefinition.BOM;
				bom.StructuredViewFirstLevelOnly = false;
				bom.StructuredViewEnabled = true;
				// Set a reference to the "Structured" BOMView
				BOMView bomView = bom.BOMViews["Структурированный"];

				foreach (BOMRow row in bomView.BOMRows)
				{
					ComponentDefinition componentDefinition = row.ComponentDefinitions[1];
					if (!(componentDefinition is VirtualComponentDefinition))
					{
						if (componentDefinition is AssemblyComponentDefinition)
						{
							AssemblyDocument locAssembly = (AssemblyDocument)componentDefinition.Document;
							string filePath = locAssembly.FullFileName;
							// Check whether assembly in casing subdirectory
							if (filePath.IndexOf(casingDirectory) >= 0)
							{
								ReNumberSheetMetalKit(locAssembly, row.ChildRows);
							}
						}
					}
				}
				assembly.Close();
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}


		private void ReNumberSheetMetalKit(AssemblyDocument assembly, BOMRowsEnumerator bomRows)
		{
			try
			{
				// Get component of sheet metal kit
				Component component = GetComponent((Document)assembly);
				component.Level = 1;
				SetPartNumbersInSheetMetalKitRecursive(bomRows, component);
				componentsToReplace.Add(component);
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		private Component GetComponent(Document document)
		{
			try
			{
				Component component = new Component();
				// Get file info
				component.FullFileName = document.FullFileName;
				PropertySet oPropSet = document.PropertySets["Design Tracking Properties"];
				component.PartNumber = oPropSet["Part Number"].Value.ToString();
				component.Description = oPropSet["Description"].Value.ToString();
				oPropSet = document.PropertySets["Inventor User Defined Properties"];
				component.FactoryNumber = oPropSet["Заводской номер"].Value.ToString();
				// Define component type
				if (document.DocumentType == DocumentTypeEnum.kAssemblyDocumentObject)
				{
					component.ComponentType = ComponentTypes.Assembly;
				}
				else if (document.DocumentType == DocumentTypeEnum.kPartDocumentObject)
				{
					component.ComponentType = ComponentTypes.Part;
				}
				return component;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
				return null;
			}
		}


		private void SetPartNumbersInSheetMetalKitRecursive(BOMRowsEnumerator bomRows, Component component)
		{
			try
			{
				int AssemblyIndex = 0;
				int PartIndex = 20;

				foreach (BOMRow row in bomRows)
				{
					ComponentDefinition componentDefinition = row.ComponentDefinitions[1];
					if (!(componentDefinition is VirtualComponentDefinition))
					{
						Component subComponent;
						Document locDoc = (Document)componentDefinition.Document;
						PropertySet oPropSet = locDoc.PropertySets["Design Tracking Properties"];
						// Check whether the component in casing directory
						string filePath = locDoc.FullFileName;

						if (filePath.IndexOf(casingDirectory) >= 0)
						{
							if (componentDefinition is AssemblyComponentDefinition)
							{
								// Change Part Number
								AssemblyIndex++;
								string add = AssemblyIndex.ToString();
								if (add.Length == 1) add = "0" + add;
								oPropSet["Part Number"].Value = component.PartNumber + "." + add;
								subComponent = GetComponent(locDoc);
								// Recursive call
								SetPartNumbersInSheetMetalKitRecursive(row.ChildRows, subComponent);
								// Set component lvl	
								subComponent.Level = component.Level + 1;
								// Add component
								component.Components.Add(subComponent);
							}
							else if (componentDefinition is PartComponentDefinition)
							{
								PartIndex++;
								oPropSet["Part Number"].Value = component.PartNumber + "." + PartIndex.ToString();
								subComponent = GetComponent(locDoc);
								// Set component lvl
								subComponent.Level = component.Level + 1;
								// Add component
								component.Components.Add(subComponent);
							}
						}
					}
				}

			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		#endregion



		#region Frames

		// Search for Frames ("Рама")
		private List<Component> GetFrames()
		{
			List<Component> frames = new List<Component>();
			try
			{
				foreach (Component component in casingComponent.Components)
				{
					if (component.CasingType == CasingTypes.Frame)
					{
						frames.Add(component);
					}
				}
				return frames;
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
				return frames;
			}
		}

		private void RenumberFrame(Component component)
		{
			try
			{
				AssemblyDocument assembly = (AssemblyDocument)inventorApp.Documents.Open(component.FullFileName, false);
				// Get BOM
				BOM bom = assembly.ComponentDefinition.BOM;
				bom.StructuredViewFirstLevelOnly = false;
				bom.StructuredViewEnabled = true;
				// Set a reference to the "Structured" BOMView
				BOMView bomView = bom.BOMViews["Структурированный"];
				// Get component of Frame
				component.Level = 1;
				SetPartNumbersInFrameRecursive(bomView.BOMRows, component);
				componentsToReplace.Add(component);
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		private void SetPartNumbersInFrameRecursive(BOMRowsEnumerator bomRows, Component component)
		{
			try
			{
				int AssemblyIndex = 0;
				int PartIndex = 20;

				foreach (BOMRow row in bomRows)
				{
					ComponentDefinition componentDefinition = row.ComponentDefinitions[1];
					if (!(componentDefinition is VirtualComponentDefinition))
					{
						Component subComponent;
						Document locDoc = (Document)componentDefinition.Document;
						PropertySet oPropSet = locDoc.PropertySets["Design Tracking Properties"];
						// Check whether the component in casing directory
						string filePath = locDoc.FullFileName;

						if (filePath.IndexOf(frameDirectory) >= 0)
						{
							if (componentDefinition is AssemblyComponentDefinition)
							{
								// Change Part Number
								AssemblyIndex++;
								string add = AssemblyIndex.ToString();
								if (add.Length == 1) add = "0" + add;
								oPropSet["Part Number"].Value = component.PartNumber + "." + add;
								subComponent = GetComponent(locDoc);
								// Recursive call
								SetPartNumbersInFrameRecursive(row.ChildRows, subComponent);
								// Set component lvl	
								subComponent.Level = component.Level + 1;
								// Add component
								component.Components.Add(subComponent);
							}
							else if (componentDefinition is PartComponentDefinition)
							{
								bool needToAdd = false;
								 
								if (IsProfile(locDoc))
								{
									// Check whether the profile has unique partnumber	
									oPropSet = locDoc.PropertySets["Inventor User Defined Properties"];
									string propertyName = "HasUniquePartNumber";
									bool HasUniquePartNumber=false;
									if (Library.HasInventorProperty(oPropSet, propertyName))
									{
										HasUniquePartNumber = (bool)oPropSet[propertyName].Value;
									}
									// Profile with unique partnumber
									if (HasUniquePartNumber)
									{
										needToAdd = true;
									}
								}
								else
								{
									// Not a profile
									needToAdd = true;
								}
								// If part is unique profile or not a profile
								if (needToAdd)
								{
									PartIndex++;
									oPropSet["Part Number"].Value = component.PartNumber + "." + PartIndex.ToString();
									subComponent = GetComponent(locDoc);
									// Set component lvl
									subComponent.Level = component.Level + 1;
									// Add component
									component.Components.Add(subComponent);
								}
							}
						}
					}
				}

			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		private bool IsProfile(Document part)
		{
			try
			{
				bool ok = false;
					PropertySet oPropSet = part.PropertySets["Inventor User Defined Properties"];
					string propertyName = "ProfileType";
					if (Library.HasInventorProperty(oPropSet, propertyName)) return true;
				return ok;
			}

			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
				return false;
			}
		}


		#endregion

		#region ReplaceReferences

		private void GetReplaceCollections()
		{
			try
			{
				foreach (Component component in componentsToReplace)
				{
					assembliesToReplace.Add(component);
					DivideComponentsToReplaceRecursive(component.Components.ToList());
				}
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}


		private void DivideComponentsToReplaceRecursive(List<Component> components)
		{
			try
			{
				foreach (Component component in components)
				{
					if (component.ComponentType == ComponentTypes.Assembly)
					{
						assembliesToReplace.Add(component);
						// Recursive call
						DivideComponentsToReplaceRecursive(component.Components.ToList());
					}
					else if (component.ComponentType == ComponentTypes.Part)
					{
						partsToReplace.Add(component);
					}
				}
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}


		private void ReplaceComponentReferencies(Component component)
		{
			try
			{
				PrepareRename(component);
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		private void PrepareRename(Component component)
		{
			try
			{
				// Get file info
				string filePath = component.FullFileName;
				string fileName = Path.GetFileNameWithoutExtension(filePath);
				string extention = Path.GetExtension(filePath);
				string partNumber = component.PartNumber;
				string description = component.Description;
				string directory = Path.GetDirectoryName(filePath);

				if (description != string.Empty)
				{
					description = " " + description;
				}
				// Check whether the PartNumber+Description = filename
				string newFileName = partNumber + description;
				Library.CheckFileName(ref newFileName);
				if (fileName == newFileName)
				{
					return;
				}
				else
				{
					// Replace the references
					ReplaceCheck(System.IO.Path.Combine(directory, fileName + extention), System.IO.Path.Combine(directory, newFileName + extention));
				}

			}

			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		private void ReplaceCheck(string oldFilePath, string newFilePath)
		{
			try
			{
				// Check whether the NewFileName file exists
				if (System.IO.File.Exists(newFilePath))
				{
					MessageBox.Show("Файл " + newFilePath + " уже существует. Замена невозможна", "Замена ссылок", MessageBoxButton.OK);
				}
				else
				{
					// Ask user - change or not?   
					//InventorPlugins.ReplaceForm oForm = new InventorPlugins.ReplaceForm(oldFilePath, newFilePath);
					//oForm.ShowDialog();
					//if (oForm.DialogResult == System.Windows.Forms.DialogResult.OK)
					//{
						// Replace references
						ReplaceReferencesMethod(oldFilePath, newFilePath);
					//}
				}
			}

			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		private void ReplaceReferencesMethod(string oldFilePath, string newFilePath)
		{
			try
			{
				// Open document with specific filepath
				ApprenticeServerComponent oApprentice = new ApprenticeServerComponent();
				Document oDoc = (Document)oApprentice.Open(oldFilePath);

				// Find where file was used before renaming
				DocumentsEnumerator oDocsEnum = oDoc.ReferencingDocuments;
				
				// Save file with new name
				oDoc.SaveAs(newFilePath, false);

				// Look through referencing documents
				foreach (Document locDoc in oDocsEnum)
				{
					foreach (ReferencedFileDescriptor oRefFileDesc in locDoc.ReferencedFileDescriptors)
					{
						if (oRefFileDesc.FullFileName == oldFilePath)
						{
							// Replace the reference
							oRefFileDesc.PutLogicalFileNameUsingFull(newFilePath);
						}
					}
				}
				oApprentice = null;

				// Define file name
				string filename = System.IO.Path.GetFileName(oldFilePath);
				// Find IDW files and export PDF
				InventorPlugins.ExportPDF exportPDF = new InventorPlugins.ExportPDF(inventorApp, oDoc, oldFilePath, newFilePath);
				exportPDF = null;
				// Export to DXF
				InventorPlugins.ExportDXF exportDXF = new InventorPlugins.ExportDXF(inventorApp, oDoc, filename);
				exportDXF = null;
				// Close document
				oDoc.Close(true);
				oDoc = null;
				// Delete old files
				if (System.IO.File.Exists(oldFilePath))
				{
					System.IO.File.Delete(oldFilePath);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		#endregion
	}
}
