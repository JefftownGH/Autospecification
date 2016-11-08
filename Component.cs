using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;
using System.ComponentModel;
using Library = InventorPlugins.OftenLibrary;
using Inventor;

namespace AutoSpecification
{
	public enum AssemblyTypes { Common, Casing, ТМ, ТС, ТП }
	public enum CasingTypes { Common, Frame, ЛСП }
	public enum ComponentTypes { Assembly, Part }

	public class Component : INotifyPropertyChanged
	{
		// Default constructor
		public Component(Inventor.Application ThisApplication)
		{
			inventorApp = ThisApplication;
		}

		// Properties
		private Inventor.Application inventorApp;
		private string partNumber;
		public string PartNumber
		{
			get { return this.partNumber; }
			set
			{
				this.partNumber = value;
				// Call OnPropertyChanged whevener the property is updated
				OnPropertyChanged("PartNumber");
			}
		}
		private string description;
		public string Description
		{
			get { return this.description; }
			set
			{
				this.description = value;
				// Call OnPropertyChanged whevener the property is updated
				OnPropertyChanged("Description");
			}
		}
		private string factoryNumber;
		public string FactoryNumber
		{
			get { return this.factoryNumber; }
			set
			{
				this.factoryNumber = value;
				// Call OnPropertyChanged whevener the property is updated
				OnPropertyChanged("FactoryNumber");
			}
		}
		public string Quantity { get; set; }
		public string FullFileName { get; set; }
		public int Level { get; set; }
		
		private ObservableCollection<Component> components = new ObservableCollection<Component>();
		//public ComponentList Components = new ComponentList();
		public ObservableCollection<Component> Components
		{
			get
			{
				return this.components;
			}
			set
			{
				components = value;
			}
		}
		private AssemblyTypes assemblyType;
		public AssemblyTypes AssemblyType
		{
			get { return this.assemblyType; }
			set
			{
				this.assemblyType = value;
				
				// Call OnPropertyChanged whevener the property is updated
				OnPropertyChanged("AssemblyType");
			}
		}
		private CasingTypes casingType;
		public CasingTypes CasingType
		{
			get { return this.casingType; }
			set
			{
				this.casingType = value;

				// Call OnPropertyChanged whevener the property is updated
				OnPropertyChanged("CasingType");
			}
		}
		public ComponentTypes ComponentType { get; set; }
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
			if ((name=="AssemblyType")||(name=="CasingType"))
			{
				// Change inventor property
				Document document = inventorApp.Documents.Open(this.FullFileName, false);
				PropertySet oPropSet = document.PropertySets["Inventor User Defined Properties"];
				string propertyName = "Тип сборки";
				switch (name)
				{
					case "AssemblyType":
						Library.ChangeInventorProperty(oPropSet, propertyName, this.assemblyType.ToString());
						break;
					case "CasingType":
						Library.ChangeInventorProperty(oPropSet, propertyName, this.CasingType.ToString());
						break;
					default:
						break;
				}
			}
		}
	}

	//// List of components
	//public class ComponentList : ObservableCollection<Component>
	//{

	//}

}
