using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Data;
using Inventor;
using File = System.IO.File;
using Path = System.IO.Path;
using Directory = System.IO.Directory;
using Excel = Microsoft.Office.Interop.Excel;
using ExcelClass = InventorPlugins.Excel_Class;
using Library = InventorPlugins.OftenLibrary;
using Shell32;

namespace AutoSpecification
{
	public enum SPTypes { СП, ТМ, ТС, ТП, Корпус}

	public class Specification : INotifyPropertyChanged
	{

		// Default constructor
		public Specification(Inventor.Application ThisApplication, Component inputComponent)
		{

			try
			{
				mainComponent = inputComponent;
				inventorApp = ThisApplication;
				projectDirectory = System.IO.Path.GetDirectoryName(inventorApp.DesignProjectManager.ActiveDesignProject.FullFileName);
				Quantity = mainComponent.Quantity;
				if (!SearchVPFile())
				{
					System.Windows.MessageBox.Show("Создайте ярлык с ссылкой на ведомость покупных в папке с проектом.", "Отсутствует ярлык (ВП)", MessageBoxButton.OK);
				}
				SearchSPFile();
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}
		// Properties
		public string FileName { get; set; }
		public string FilePath { get; set; }
		public string VPfilePath { get; set; }
		private string author;
		private DataTable table;
		private DataTable VPtable;
		public string Author
		{
			get { return this.author; }
			set
			{
				this.author = value;
				Properties.Settings.Default.Author = value;
				Properties.Settings.Default.Save();
				// Call OnPropertyChanged whevener the property is updated
				OnPropertyChanged("Author");
			}
		}
		private string checkedBy;
		public string CheckedBy
		{
			get { return this.checkedBy; }
			set
			{
				this.checkedBy = value;
				Properties.Settings.Default.CheckedBy = value;
				Properties.Settings.Default.Save();
				// Call OnPropertyChanged whevener the property is updated
				OnPropertyChanged("CheckedBy");
			}
		}

		private string quantity;
		public string Quantity
		{
			get { return this.quantity; }
			set
			{
				this.quantity = value;
				// Call OnPropertyChanged whevener the property is updated
				OnPropertyChanged("Quantity");
			}
		}

		private string projectDirectory;
		private Inventor.Application inventorApp;
		private Component mainComponent;

		// Private Methods

		private bool SearchVPFile()
		{
			try
			{
				bool ok = false;
				List<string> foundFiles = Directory.GetFiles(projectDirectory, "ВП*.lnk").ToList();
				if (foundFiles.Count > 0)
				{
					VPfilePath = foundFiles[0];
					VPfilePath = GetLnkTarget(VPfilePath);
					ok = true;
				}
				return ok;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
				return false;
			}
		}

		public static string GetLnkTarget(string lnkPath)
		{
			var shl = new Shell32.Shell();         // Move this to class scope
			lnkPath = Path.GetFullPath(lnkPath);
			var dir = shl.NameSpace(Path.GetDirectoryName(lnkPath));
			var itm = dir.Items().Item(Path.GetFileName(lnkPath));
			var lnk = (Shell32.ShellLinkObject)itm.GetLink;
			return lnk.Target.Path;
		}

		private void SearchSPFile()
		{
			try
			{
				// Search for SP file in project directory
				List<string> foundFiles = Directory.GetFiles(projectDirectory, "СП " + mainComponent.PartNumber + "*.*")
											.Where(file => file.ToLower().EndsWith("xls") || file.ToLower().EndsWith("xlsx"))
											.ToList();

				if (foundFiles.Count > 0)
				{
					FilePath = foundFiles[0];
				}
				else
				{
					// Set file path
					FilePath = Path.Combine(projectDirectory, "СП " + mainComponent.PartNumber + ".xlsx");

					// Create SP file
					Excel.Application excelApp = new Excel.Application();
					if (excelApp == null)
					{
						Console.WriteLine("EXCEL could not be started. Check that your office installation and project references are correct.");
						return;
					}
					excelApp.Visible = false;
					Excel.Workbook excelWorkBook = excelApp.Workbooks.Add(Microsoft.Office.Interop.Excel.XlWBATemplate.xlWBATWorksheet);
					excelWorkBook.SaveAs(FilePath);
					excelApp.Quit();
					ExcelClass.KillExcelProcess(excelApp);
				}
				FileName = Path.GetFileName(FilePath);
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}


		// Declare event
		public event PropertyChangedEventHandler PropertyChanged = delegate { };
		// Create the OnPropertyChanged method to raise the event
		protected void OnPropertyChanged(string name)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(name));
			}
			if (name == "Quantity") 
			{
				// Save quantity to model
				AssemblyDocument assembly = (AssemblyDocument)inventorApp.Documents.Open(mainComponent.FullFileName);
				PropertySet oPropSet = assembly.PropertySets["Inventor User Defined Properties"];
				// Set quantity of units
				string propertyName = "Количество агрегатов";
				Library.ChangeInventorProperty(oPropSet, propertyName, this.Quantity);
			}
		}

		#region Create specifications
		// CreateAll method
		public void CreateAll()
		{
			try
			{
				// Search for another components
				bool[] isComponentsExist = { false, false, false, false };
				foreach (Component component in mainComponent.Components)
				{
					if (component.AssemblyType == AssemblyTypes.Casing)
					{
						isComponentsExist[3] = true;
					}
					if (component.AssemblyType == AssemblyTypes.ТП)
					{
						isComponentsExist[2]=true;
					}
					if (component.AssemblyType == AssemblyTypes.ТС)
					{
						isComponentsExist[1]=true;
					}
					if (component.AssemblyType==AssemblyTypes.ТМ)
					{
						isComponentsExist[0]=true;
					}
				}
				// Create SP for casing
				if (isComponentsExist[3])
				{
					Create(SPTypes.Корпус);
				}
				// Create SP for plastic tubes
				if (isComponentsExist[2])
				{
					Create(SPTypes.ТП);
				}
				// Create SP for steel tubes
				if (isComponentsExist[1])
				{
					Create(SPTypes.ТС);
				}
				// Create SP for copper tubes
				if (isComponentsExist[0])
				{
					Create(SPTypes.ТМ);
				}
				// Create main SP
				Create(SPTypes.СП);
		
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		// Create Method
		public void Create(SPTypes SPType)
		{
			try
			{
				// Declare specification component
				Component specComponent = null;

				// Check whether the component exist
				if (!IsComponentExist(SPType, out specComponent))
				{
					System.Windows.MessageBox.Show("Компонент для данного типа спецификации отсутствует в модели.", "Компонент отсутствует в модели", MessageBoxButton.OK);
					return;
				}
				// Open Workbook
				Excel.Application excelApp;
				Excel.Workbook workBook = ExcelClass.OpenExcelWorkBook(FilePath, out excelApp, true);
				// Get appropriate work sheet
				Excel.Worksheet workSheet = GetWorkSheet(workBook, SPType);
				// Get specification header
				string specHeader = GetSpecificationHeader(SPType);
				// Format worksheet columns
				FormatWorkSheet(workSheet, specHeader);
				// Load Data																														 
				table = GetDataTable(specComponent);
				// Refine and Merge table rows
				RefineAndMerge();
				// Sort table
				SortData(SPType);
				// Write data to Excel sheet
				WriteData(workSheet);
				// Match VP and SP data
				MatchData(workSheet, excelApp);

				// Close Excel
				ExcelClass.CloseExcel(workBook, excelApp);
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}


		private bool IsComponentExist(SPTypes SPType, out Component component)
		{
			component = null;
			try
			{
				bool ok = false;
				switch (SPType)
				{
					case SPTypes.СП:
						component = mainComponent;
						ok = true;
						break;
					case SPTypes.ТМ:
						foreach (Component subComponent in mainComponent.Components)
						{
							if (subComponent.AssemblyType == AssemblyTypes.ТМ)
							{
								component = subComponent;
								ok = true;
							}
						}
						break;
					case SPTypes.ТС:
						foreach (Component subComponent in mainComponent.Components)
						{
							if (subComponent.AssemblyType == AssemblyTypes.ТС)
							{
								component = subComponent;
								ok = true;
							}
						}
						break;
					case SPTypes.ТП:
						foreach (Component subComponent in mainComponent.Components)
						{
							if (subComponent.AssemblyType == AssemblyTypes.ТП)
							{
								component = subComponent;
								ok = true;
							}
						}
						break;
					case SPTypes.Корпус:
						foreach (Component subComponent in mainComponent.Components)
						{
							if (subComponent.AssemblyType == AssemblyTypes.Casing)
							{
								component = subComponent;
								ok = true;
							}
						}
						break;
					default:
						break;
				}
				return ok;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
				return false;
			}
		}

		private Excel.Worksheet GetWorkSheet(Excel.Workbook workBook, SPTypes SPType)
		{
			Excel.Worksheet workSheet = null;
			try
			{
				string workSheetName = string.Empty;
				switch (SPType)
				{
					case SPTypes.СП:
						workSheetName = "Спецификация";
						break;
					case SPTypes.ТМ:
						workSheetName = "Спецификация-медь";
						break;
					case SPTypes.ТС:
						workSheetName = "Спецификация-сталь";
						break;
					case SPTypes.ТП:
						workSheetName = "Спецификация-пластик";
						break;
					case SPTypes.Корпус:
						workSheetName = "Спецификация-корпус";
						break;
					default:
						break;
				}
				if (workSheetName != string.Empty)
				{
					foreach (Excel.Worksheet workSheet2 in workBook.Sheets)
					{
						if (workSheet2.Name == workSheetName)
						{
							workSheet = workSheet2;
						}
					}
					// Check whether the sheet exist
					if (workSheet == null)
					{
						// Create new sheet
						workSheet = workBook.ActiveSheet;
						if (workSheet.Name != "Лист1")
						{
							// Add new sheet
							workSheet = workBook.Sheets.Add();
						}
						// Rename sheet
						workSheet.Name = workSheetName;
					}
				}
				else
				{
					return null;
				}
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
			return workSheet;
		}

		private string GetSpecificationHeader(SPTypes SPType)
		{
			string header = string.Empty;
			try
			{
				header += mainComponent.Description + " ";
				header += mainComponent.PartNumber;
				header += "\n";
				switch (SPType)
				{
					case SPTypes.СП:
						header += "Спецификация ";
						break;
					case SPTypes.ТМ:
						header += "Спецификация Трубы медные ";
						break;
					case SPTypes.ТС:
						header += "Спецификация Трубы стальные ";
						break;
					case SPTypes.ТП:
						header += "Спецификация Трубы пластиковые ";
						break;
					case SPTypes.Корпус:
						header += "Спецификация Корпус ";
						break;
					default:
						break;
				}

				// Check whether Quantity is numeric
				int quantity;
				bool result = Int32.TryParse(this.Quantity, out quantity);
				// Define factory numbers
				if (result)
				{
					if (this.Quantity != "1")
					{
						// get
						double factoryNumber;
						result = Double.TryParse(mainComponent.FactoryNumber, out factoryNumber);
						if (result)
						{
							string lastFactoryNumber = (factoryNumber + quantity).ToString();
							lastFactoryNumber = lastFactoryNumber.Substring(lastFactoryNumber.Length - 4);
							header += "Заводские номера № " + mainComponent.FactoryNumber +
									 "–" + lastFactoryNumber;
						}
						else
						{
							header += "Заводской номер № " + mainComponent.FactoryNumber;
						}
					}
					else
					{
						header += "Заводской номер № " + mainComponent.FactoryNumber;
					}
				}
				else
				{
					header += "Заводской номер № " + mainComponent.FactoryNumber;
				}
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
			return header;
		}


		private void FormatWorkSheet(Excel.Worksheet workSheet, string header)
		{
			try
			{
				// Clear data
				int lastRow = workSheet.Cells[65536, 2].End[Excel.XlDirection.xlUp].Row;
				workSheet.Range["a1", workSheet.Cells[lastRow, 10]].Clear();
				// First column
				Excel.Range range = (Excel.Range)workSheet.Cells[2, 1];
				range.ColumnWidth = 8;
				range.Value = "№";
				range.EntireColumn.VerticalAlignment = Excel.Constants.xlTop;
				range.EntireColumn.HorizontalAlignment = Excel.Constants.xlCenter;
				range.EntireColumn.WrapText = true;
				// Second column
				range = (Excel.Range)workSheet.Cells[1, 2];
				range.ColumnWidth = 14;
				range.Value = DateTime.Today;
				workSheet.Cells[2, 2].Value = "Поз";
				range.EntireColumn.VerticalAlignment = Excel.Constants.xlTop;
				range.EntireColumn.HorizontalAlignment = Excel.Constants.xlCenter;
				// Third column
				range = (Excel.Range)workSheet.Cells[1, 3];
				range.ColumnWidth = 64;
				range.EntireColumn.VerticalAlignment = Excel.Constants.xlTop;
				range.EntireColumn.HorizontalAlignment = Excel.Constants.xlLeft;
				range.EntireColumn.WrapText = true;
				// Set header of specification
				range.HorizontalAlignment = Excel.Constants.xlCenter;
				range.Font.Bold = true;
				range.Value = header;
				range = (Excel.Range)workSheet.Cells[2, 3];
				range.Value = "Наименование";
				range.HorizontalAlignment = Excel.Constants.xlCenter;
				// Forth column
				range = (Excel.Range)workSheet.Cells[2, 4];
				range.ColumnWidth = 6;
				range.EntireColumn.VerticalAlignment = Excel.Constants.xlTop;
				range.EntireColumn.HorizontalAlignment = Excel.Constants.xlCenter;
				range.Value = "Кол-во";
				workSheet.PageSetup.Zoom = false;
				workSheet.PageSetup.FitToPagesWide = 1;
				//workSheet.PageSetup.FitToPagesTall = 0;

			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		private DataTable GetDataTable(Component specComponent)
		{
			DataTable resultTable = new DataTable();
			// Format table
			resultTable.Columns.Add("VPNumber", typeof(string)); // Code from purchase roll
			resultTable.Columns.Add("PartNumber", typeof(string));
			resultTable.Columns.Add("Description", typeof(string));
			resultTable.Columns.Add("Quantity", typeof(string));
			resultTable.Columns.Add("IsConsumable", typeof(bool));
			resultTable.Columns.Add("UnitOfMeasure", typeof(string));
			try
			{
				AssemblyDocument assembly = (AssemblyDocument)inventorApp.Documents.Open(specComponent.FullFileName, false);
				// Get BOM
				BOM bom = assembly.ComponentDefinition.BOM;
				bom.StructuredViewFirstLevelOnly = false;
				bom.StructuredViewEnabled = true;
				// Get merge settings
				bool mergeEnabled = false;
				string[] mergeExcludeList = new string[] { "жопа" };
				bom.GetPartNumberMergeSettings(out mergeEnabled, out mergeExcludeList);
				// Set merge settings to false temporarily
				bom.SetPartNumberMergeSettings(false, mergeExcludeList);
				// Set a reference to the "Structured" BOMView
				BOMView bomView = bom.BOMViews["Структурированный"];


				foreach (BOMRow BOMrow in bomView.BOMRows)
				{
					DataRow row = resultTable.NewRow();
					ComponentDefinition componentDefinition = BOMrow.ComponentDefinitions[1];
					Document locDoc = (Document)componentDefinition.Document;
					PropertySet oPropSet = locDoc.PropertySets["Design Tracking Properties"];
					row["PartNumber"] = oPropSet["Part Number"].Value.ToString();
					row["Description"] = oPropSet["Description"].Value.ToString();
					oPropSet = locDoc.PropertySets["Inventor User Defined Properties"];
					if (Library.HasInventorProperty(oPropSet, "Расходник"))
					{
						row["IsConsumable"] = oPropSet["Расходник"].Value;
					}
					else
					{
						row["IsConsumable"] = false;
					}
					row["Quantity"] = BOMrow.TotalQuantity;
					// Add row
					resultTable.Rows.Add(row);
				}

				// Restore BOM merge settings
				bom.SetPartNumberMergeSettings(mergeEnabled, mergeExcludeList);
				if (specComponent.FullFileName != mainComponent.FullFileName)
				{
					assembly.Close();
				}
				return resultTable;
			}

			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
				return null;
			}

		}

		private void RefineAndMerge()
		{
			try
			{
				// Refine consumables and remove units of measure
				foreach (DataRow row in table.Rows)
				{
					// Add "R " to consumables
					string partNumber = row["PartNumber"].ToString();
					if (Convert.ToBoolean(row["IsConsumable"]))
					{
						if (partNumber.IndexOf("R ") != 0)
						{
							row["PartNumber"] = "R " + partNumber;
						}
					}
					// Check quantity units of measure
					string strQuantity = row["Quantity"].ToString();
					int spaceIndex = strQuantity.IndexOf(" ");
					// Get units of measure
					string unitOfMeasure = string.Empty;
					if (spaceIndex >= 0)
					{
						unitOfMeasure = strQuantity.Substring(spaceIndex + 1, strQuantity.Length - spaceIndex - 1);
						strQuantity = strQuantity.Substring(0, spaceIndex);
						// Get quantities
						double quantity;
						bool result = Double.TryParse(strQuantity, out quantity);
						// Change units of measure
						if (unitOfMeasure == "мм")
						{
							quantity /= 1000;
							unitOfMeasure = "м";
						}
						row["UnitOfMeasure"] = unitOfMeasure;
						row["Quantity"] = quantity.ToString();
					}
					else
					{
						row["UnitOfMeasure"] = "шт";
					}
				}

				// Merge rows
				for (int i = table.Rows.Count - 1; i > 0; i--)
				{
					for (int j = 0; j < i; j++)
					{
						if (table.Rows[i]["PartNumber"].ToString() == table.Rows[j]["PartNumber"].ToString())
						{
							// Get str quantities
							string strQuantity1 = table.Rows[j]["Quantity"].ToString();
							string strQuantity2 = table.Rows[i]["Quantity"].ToString();
							// Get units of measure
							string unitOfMeasure1 = table.Rows[j]["UnitOfMeasure"].ToString();
							string unitOfMeasure2 = table.Rows[i]["UnitOfMeasure"].ToString();
							// Get quantities
							double quantity1;
							double quantity2;
							bool result1 = Double.TryParse(strQuantity1, out quantity1);
							bool result2 = Double.TryParse(strQuantity2, out quantity2);
							// Sum quantities
							if (unitOfMeasure1 != unitOfMeasure2)
							{
								strQuantity1 = "Ошибка!";
							}
							else
							{
								if ((result1) && (result2))
								{
									quantity1 += quantity2;
									strQuantity1 = quantity1.ToString();
								}
								else
								{
									strQuantity1 = "Ошибка!";
								}
							}
							// Set new value
							table.Rows[j]["Quantity"] = strQuantity1;
							// Delete row
							table.Rows.RemoveAt(i);
							// Exit from nested loop
							break;
						}
					}
				}
				// Round double quantities
				foreach (DataRow row in table.Rows)
				{
					// Check quantity units of measure
					string strQuantity = row["Quantity"].ToString();
					// Get quantities
					double quantity;
					bool result = Double.TryParse(strQuantity, out quantity);
					// Change units of measure
					if (result)
					{
						quantity = Math.Ceiling(quantity * 100) / 100;
						row["Quantity"] = quantity.ToString();
					}
				}
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}


		private void SortData(SPTypes SPType)
		{
			try
			{
				// Sort table
				table = table.AsEnumerable().OrderBy(c => c[1]).CopyToDataTable();
				// Search for main components
				DataTable mainTable = null;
				DataTable commonTable = null;
				if ((SPType == SPTypes.СП)||(SPType == SPTypes.Корпус))
				{
					// Copy table
					commonTable = table.Copy();
					mainTable = table.Copy();
					for (int i = table.Rows.Count - 1; i >= 0; i--)
					{
						string partNumber = table.Rows[i]["PartNumber"].ToString();
						if (partNumber.IndexOf(mainComponent.FactoryNumber) > 0)
						{
							commonTable.Rows.RemoveAt(i);
						}
						else
						{
							mainTable.Rows.RemoveAt(i);
						}
					}
					table.Clear();
					// merge tables
					mainTable.Merge(commonTable);
					table = mainTable.Copy();
					// Clear tables
					mainTable = null;
					commonTable = null;
				}


			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}


		private void WriteData(Excel.Worksheet workSheet)
		{
			try
			{
				int rowIndex = 3;
				foreach (DataRow row in table.Rows)
				{
					workSheet.Cells[rowIndex, 2].Value = row["PartNumber"];
					workSheet.Cells[rowIndex, 3].Value = row["Description"];
					workSheet.Cells[rowIndex, 4].Value = row["Quantity"];
					if (row["Quantity"].ToString() == "Ошибка!")
					{
						// Color cell
						workSheet.Cells[rowIndex, 4].Interior.Color = Microsoft.Office.Interop.Excel.XlRgbColor.rgbRosyBrown;
					}
					else
					{
						workSheet.Cells[rowIndex, 4].Interior.ColorIndex = 0;
					}
					rowIndex++;
				}

				workSheet.Cells[rowIndex + 1, 2].Value = "Разработал";
				workSheet.Cells[rowIndex + 1, 3].Value = this.Author;
				workSheet.Cells[rowIndex + 2, 2].Value = "Проверил";
				workSheet.Cells[rowIndex + 2, 3].Value = this.CheckedBy;
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}


		private void MatchData(Excel.Worksheet workSheet, Excel.Application excelApp)
		{
			try
			{
				// Open purchased roll
				// Open Workbook
				Excel.Workbook VPworkBook = excelApp.Workbooks.Open(VPfilePath);
				// Get appropriate work sheet
				Excel.Worksheet VPworkSheet = VPworkBook.Sheets["Сортированная"];
				// Get data from VP
				VPtable = GetVPTable(VPworkSheet);
				// Compare VP and SP
				CompareVP(workSheet);

				// Close workbook
				VPworkBook.Close();
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		private DataTable GetVPTable(Excel.Worksheet workSheet)
		{
			DataTable resultTable = new DataTable();
			// Format table
			resultTable.Columns.Add("VPNumber", typeof(string)); // Code from purchase roll
			resultTable.Columns.Add("PartNumber", typeof(string));
			resultTable.Columns.Add("Description", typeof(string));
			resultTable.Columns.Add("Quantity", typeof(string));
			try
			{
				// Get last row index
				int lastRow = workSheet.Cells[65536, 2].End[Excel.XlDirection.xlUp].Row;
				string str = workSheet.Cells[lastRow, 2].Value;
				lastRow--;
				while (str != null)
				{
					str = workSheet.Cells[lastRow, 2].Value;
					lastRow--;
				}
				// Iterate through purchased roll
				for (int i = 3; i <= lastRow; i++)
				{
					DataRow row = resultTable.NewRow();
					if (workSheet.Cells[i, 1].Value != null)
					{
						row["VPNumber"] = workSheet.Cells[i, 1].Value.ToString();
					}
					row["PartNumber"] = workSheet.Cells[i, 2].Value.ToString();
					row["Description"] = workSheet.Cells[i, 3].Value.ToString();
					row["Quantity"] = workSheet.Cells[i, 4].Value.ToString();
					resultTable.Rows.Add(row);
				}

				return resultTable;
			}

			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
				return null;
			}
		}


		private void CompareVP(Excel.Worksheet workSheet)
		{
			try
			{
				// Get list of partNumbers
				List<string> VPpartNumbers = VPtable.AsEnumerable().Select(x => x[1].ToString()).ToList();

				int startIndex = 3;
				for (int i = 0; i < table.Rows.Count; i++)
				{
					int index = startIndex + i;
					string partNumber = workSheet.Cells[index, 2].Value.ToString();
					if (VPpartNumbers.Contains(partNumber))
					{
						int VPindex = VPpartNumbers.FindIndex(x => x == partNumber);
						// Copy items to SP
						workSheet.Cells[startIndex + i, 1].Value = VPtable.Rows[VPindex]["VPNumber"];
						// Get number of items
						List<string> items = VPtable.Rows[VPindex]["VPNumber"].ToString().Split(',').ToList();
						// Get Quantity
						double quantity;
						bool result = Double.TryParse(workSheet.Cells[index, 4].Value.ToString(), out quantity);
						if (result)
						{
							if (items.Count != quantity)
							{
								// Color font
								workSheet.Cells[startIndex + i, 1].Font.Color = Microsoft.Office.Interop.Excel.XlRgbColor.rgbRosyBrown;
							}
						}
					}
					else
					{
						// Color cell
						workSheet.Cells[startIndex + i, 1].Value = "Нет в ВП";
						workSheet.Cells[startIndex + i, 1].Interior.Color = Microsoft.Office.Interop.Excel.XlRgbColor.rgbRosyBrown;
						workSheet.Cells[startIndex + i, 1].Font.Color = Microsoft.Office.Interop.Excel.XlRgbColor.rgbWhite;
					}
				}
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(ex.Message, System.Reflection.MethodBase.GetCurrentMethod().Name, MessageBoxButton.OK);
			}
		}

		#endregion
	}
}
