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
using Library = InventorPlugins.OftenLibrary;

namespace AutoSpecification
{

	public partial class SpecificationForm : Window
	{
		// Properties
		private Inventor.Application inventorApp;
		public Specification specification { get; set; }
		//private bool isFirstTimeMain = true;
		//private bool isFirstTimeCasing = true;
		// Constructors
		public SpecificationForm(Inventor.Application ThisApplication, Component inputComponent)
		{
			inventorApp = ThisApplication;
			specification = new Specification(inventorApp, inputComponent);
			specification.Author = Properties.Settings.Default.Author;
			specification.CheckedBy = Properties.Settings.Default.CheckedBy;
			InitializeComponent();
			//DataGridMain.DataContext = mainComponent.Components;
		}


		// Events
		private void AssemblyType_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var comboBox = sender as ComboBox;
			var selectedItem = this.DataGridMain.CurrentItem;
			specification.CheckCasing();
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
			WriteProperties writeProperties = new WriteProperties(inventorApp, specification.MainComponent, specification.projectDirectory);
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
																		specification.CasingComponent,
																		specification.projectDirectory);
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
