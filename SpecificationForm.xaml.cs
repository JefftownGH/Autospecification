using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Inventor;
using File = System.IO.File;
using Path = System.IO.Path;
using Directory = System.IO.Directory;

namespace AutoSpecification
{

	public partial class SpecificationForm : Window
	{
		// Properties
		private string projectDirectory { get; set; }
		private Inventor.Application inventorApp;
		private AssemblyDocument mainAssembly;
		private AssemblyDocument casingAssembly;
		public Component mainComponent { get; set; }
		public Component casingComponent { get; set; }
		public Specification specification { get; set; }
		//private bool isFirstTimeMain = true;
		//private bool isFirstTimeCasing = true;
		// Constructors
		public SpecificationForm(Inventor.Application ThisApplication, Component inputComponent)
		{
			inventorApp = ThisApplication;
			projectDirectory = System.IO.Path.GetDirectoryName(inventorApp.DesignProjectManager.ActiveDesignProject.FullFileName);
			mainComponent = inputComponent;
			// Search for components
			mainAssembly = (AssemblyDocument)inventorApp.Documents.Open(mainComponent.FullFileName);
			specification = new Specification(inventorApp, mainComponent);
			specification.Author = Properties.Settings.Default.Author;
			specification.CheckedBy = Properties.Settings.Default.CheckedBy;
			SearchComponents();
			InitializeComponent();
			//DataGridMain.DataContext = mainComponent.Components;
		}

		// Methods
		private void SearchComponents()
		{
			try
			{
				foreach (ComponentOccurrence occurrence in mainAssembly.ComponentDefinition.Occurrences)
				{
					if (occurrence.DefinitionDocumentType == DocumentTypeEnum.kAssemblyDocumentObject)
					{
						AssemblyDocument assembly = (AssemblyDocument)occurrence.Definition.Document;
						Component component = GetAssemblyComponent(assembly);
						mainComponent.Components.Add(component);
					}
				}

			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		private Component GetAssemblyComponent(AssemblyDocument assembly)
		{
			try
			{
				Component component = new Component();
				// Get file info
				component.FullFileName = assembly.FullFileName;
				PropertySet oPropSet = assembly.PropertySets["Design Tracking Properties"];
				component.PartNumber = oPropSet["Part Number"].Value.ToString();
				component.Description = oPropSet["Description"].Value.ToString();
				oPropSet = assembly.PropertySets["Inventor User Defined Properties"];
				component.FactoryNumber = oPropSet["Заводской номер"].Value.ToString();
				component.ComponentType = ComponentTypes.Assembly;
				// Define assembly type				
				string fileName = Path.GetFileName(component.FullFileName);
				component.AssemblyType = AssemblyTypes.Common;
				if (fileName.IndexOf("Корпус") >= 0)
				{
					component.AssemblyType = AssemblyTypes.Casing;
					casingComponent = component;
					SearchCasingComponents();
				}
				if (fileName.IndexOf("Трубы медь") >= 0)
				{
					component.AssemblyType = AssemblyTypes.ТМ;
				}
				if (fileName.IndexOf("Трубы сталь") >= 0)
				{
					component.AssemblyType = AssemblyTypes.ТС;
				}
				if (fileName.IndexOf("Трубы пластик") >= 0)
				{
					component.AssemblyType = AssemblyTypes.ТП;
				}
				return component;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
				return null;
			}
		}

		private void SearchCasingComponents()
		{
			try
			{
				casingAssembly = (AssemblyDocument)inventorApp.Documents.Open(casingComponent.FullFileName,false);
				foreach (ComponentOccurrence occurrence in casingAssembly.ComponentDefinition.Occurrences)
				{
					AddCasingComponentRecursive(occurrence, casingComponent);
				}

				casingAssembly.Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		private void AddCasingComponentRecursive(ComponentOccurrence occurrence, Component component)
		{
			try
			{
				if (occurrence.DefinitionDocumentType == DocumentTypeEnum.kAssemblyDocumentObject)
				{
					AssemblyDocument assembly = (AssemblyDocument)occurrence.Definition.Document;
					Component subComponent = GetCasingComponent(assembly);
					component.Components.Add(subComponent);
					// Recursive call
					//foreach (ComponentOccurrence subOccurrence in assembly.ComponentDefinition.Occurrences)
					//{
					//	AddCasingComponentRecursive(subOccurrence, subComponent);
					//}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		private Component GetCasingComponent(AssemblyDocument assembly)
		{
			try
			{
				Component component = new Component();
				// Get file info
				component.FullFileName = assembly.FullFileName;
				PropertySet oPropSet = assembly.PropertySets["Design Tracking Properties"];
				component.PartNumber = oPropSet["Part Number"].Value.ToString();
				component.Description = oPropSet["Description"].Value.ToString();
				oPropSet = assembly.PropertySets["Inventor User Defined Properties"];
				component.FactoryNumber = oPropSet["Заводской номер"].Value.ToString();
				component.ComponentType = ComponentTypes.Assembly;
				// Define assembly type				
				string fileName = Path.GetFileName(component.FullFileName);
				component.CasingType = CasingTypes.Common;
				if (fileName.IndexOf("Комплект ЛСП") >= 0)
				{
					component.CasingType = CasingTypes.ЛСП;
				}
				if (fileName.IndexOf("Рама") >= 0)
				{
					component.CasingType = CasingTypes.Frame;
				}
				return component;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
				return null;
			}
		}

		// Events
		private void AssemblyType_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var comboBox = sender as ComboBox;
			var selectedItem = this.DataGridMain.CurrentItem;

		}


		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//FitContent(DataGridMain);
			//FitContent(Casing_dataGrid);
		}

		private void DataGridMain_Loaded(object sender, RoutedEventArgs e)
		{
			DataGrid dg = sender as DataGrid;
			FitContent((DataGrid)sender);
		}

		private void Casing_dataGrid_Loaded(object sender, RoutedEventArgs e)
		{
			//isFirstTimeCasing = false;
			DataGrid dg = sender as DataGrid;
			FitContent((DataGrid)sender);
		}

		private void Casing_dataGrid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//DataGrid dg = sender as DataGrid;
			//FitContent((DataGrid)sender);
		}

		private void DataGridMain_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//DataGrid dg = sender as DataGrid;
			//FitContent((DataGrid)sender);
		}

		private void FitContent(DataGrid dataGrid)
		{
			int columnsCount = dataGrid.Columns.Count;
			double columnsWidth = 0;
			for (int i = 0; i < columnsCount; i++)
			{
				dataGrid.Columns[i].Width = new DataGridLength(1.0, DataGridLengthUnitType.Auto);
				if (i != columnsCount - 1)
				{
					columnsWidth += dataGrid.Columns[i].ActualWidth;
				}
			}
			dataGrid.Columns[columnsCount - 1].Width = dataGrid.ActualWidth - columnsWidth;
		}

		#region Buttons

		// WriteProperties
		private void WriteProperties_button_Click(object sender, RoutedEventArgs e)
		{
			WriteProperties writeProperties = new WriteProperties(inventorApp, mainAssembly, mainComponent, projectDirectory);
			writeProperties = null;
		}


		// Specifications

		private void AllSpecifications_button_Click(object sender, RoutedEventArgs e)
		{
			specification.CreateAll();
		}

		private void Specification_button_Click(object sender, RoutedEventArgs e)
		{
			specification.Create(SPTypes.СП);
		}

		private void TM_button_Click(object sender, RoutedEventArgs e)
		{
			specification.Create(SPTypes.ТМ);

		}

		private void TS_button_Click(object sender, RoutedEventArgs e)
		{
			specification.Create(SPTypes.ТС);

		}

		private void TP_button_Click(object sender, RoutedEventArgs e)
		{
			specification.Create(SPTypes.ТП);

		}

		private void ReplaceReference_button_Click(object sender, RoutedEventArgs e)
		{
			ReplaceReferences replaceReferences = new ReplaceReferences(inventorApp, 
																		casingAssembly, 
																		casingComponent,
																		projectDirectory);
			replaceReferences = null;
		}

		private void CasingSP_button_Click(object sender, RoutedEventArgs e)
		{
			specification.Create(SPTypes.Корпус);
		}

		private void OrderList_button_Click(object sender, RoutedEventArgs e)
		{

		}

		private void FrameSP_button_Click(object sender, RoutedEventArgs e)
		{

		}

		private void CuttingList_button_Click(object sender, RoutedEventArgs e)
		{

		}

		// Cancel button
		private void Cancel_button_Click(object sender, RoutedEventArgs e)
		{

		}
		#endregion 









	}
}
