using System;
using System.Runtime.InteropServices;
using Inventor;
using Microsoft.Win32;

namespace AutoSpecification
{
	/// <summary>
	/// This is the primary AddIn Server class that implements the ApplicationAddInServer interface
	/// that all Inventor AddIns are required to implement. The communication between Inventor and
	/// the AddIn is via the methods on this interface.
	/// </summary>
	[GuidAttribute("479138eb-d875-4658-90b1-666e5c09631e")]
	public class StandardAddInServer : Inventor.ApplicationAddInServer
	{

		// Inventor application object.
		private Inventor.Application m_inventorApplication;
		private Inventor.ApplicationEvents m_AppEvents;

		// Button definitions
		private ButtonDefinition SpecificationCommand;
		
		//private int timesWindowMaximized = 0;


		public StandardAddInServer()
		{
		
		}

		#region ApplicationAddInServer Members

		public void Activate(Inventor.ApplicationAddInSite addInSiteObject, bool firstTime)
		{
			// This method is called by Inventor when it loads the addin.
			// The AddInSiteObject provides access to the Inventor Application object.
			// The FirstTime flag indicates if the addin is loaded for the first time.

			// Initialize AddIn members.
			m_inventorApplication = addInSiteObject.Application;
			
			// Add event handlers
			m_AppEvents = m_inventorApplication.ApplicationEvents;

			// TODO: Add ApplicationAddInServer.Activate implementation.
			// e.g. event initialization, command creation etc.


			// Define the buttons on ribbons
			Inventor.UserInterfaceManager UIManager = m_inventorApplication.UserInterfaceManager;

			// Define ControlDefinition (Button on the ribbon panel)
			ControlDefinitions controlDefs = m_inventorApplication.CommandManager.ControlDefinitions;

			// SPECIFICATION BUTTON
			// Define Command
			string CommandID = "SpecificationCmd";

			try
			{
				// try to get the existing command definition
				SpecificationCommand = (Inventor.ButtonDefinition)controlDefs[CommandID];
			}

			catch
			{
				// or create it
				IPictureDisp SmallPicture = (Inventor.IPictureDisp)PictureDispConverter.ToIPictureDisp(AutoSpecification.Properties.Resources.SimpleIcon16);
				IPictureDisp LargePicture = (Inventor.IPictureDisp)PictureDispConverter.ToIPictureDisp(AutoSpecification.Properties.Resources.SimpleIcon32);

				SpecificationCommand = controlDefs.AddButtonDefinition(
					"Создание спецификаций на агрегат", CommandID,
					CommandTypesEnum.kEditMaskCmdType,
					Guid.NewGuid().ToString(),
					"Автоспецификации",
					"Создание спецификаций на агрегат",
					SmallPicture,
					LargePicture, ButtonDisplayEnum.kNoTextWithIcon);
			}
			// register the method that will be executed
			SpecificationCommand.OnExecute += new Inventor.ButtonDefinitionSink_OnExecuteEventHandler(SpecificationCommand_OnExecute);

			// add buttons to ribbon    
			if (firstTime)
			{
				UserInterfaceManager userInterfaceManager = m_inventorApplication.UserInterfaceManager;
				if (userInterfaceManager.InterfaceStyle == InterfaceStyleEnum.kRibbonInterface)
				{

					// Assembly ribbon

					// 1. Access the Assebly ribbon
					Inventor.Ribbon ribbonPart = userInterfaceManager.Ribbons["Assembly"];

					// 2. Get Assemble tab
					Inventor.RibbonTab tabSampleBlog = ribbonPart.RibbonTabs["id_TabAssemble"];

					// 3. Create panel
					Inventor.RibbonPanel pnlMyCommands = tabSampleBlog.RibbonPanels.Add("Спецификация", "id_Panel_AssemblyAutoSpecification", Guid.NewGuid().ToString());

					// 4. Add Button to Panel
					pnlMyCommands.CommandControls.AddButton(SpecificationCommand, true, false);


					//// Part Ribbon (SheetMetalTab)

					//// 1. Access the Part ribbon
					//ribbonPart = userInterfaceManager.Ribbons["Part"];

					//// 2. Get Part tab
					//tabSampleBlog = ribbonPart.RibbonTabs["id_TabSheetMetal"];

					//// 3. Create panel
					//pnlMyCommands = tabSampleBlog.RibbonPanels.Add("Макросы", "id_Panel_SheetMetalReplacePart", Guid.NewGuid().ToString());

					//// 4. Add Button to Panel
					//pnlMyCommands.CommandControls.AddButton(ReplacePartCommand, true, false);
					//pnlMyCommands.CommandControls.AddSeparator();
					//pnlMyCommands.CommandControls.AddButton(ExportDXFCommand, true, false);
					//pnlMyCommands.CommandControls.AddButton(BendTechnologyCommand, true, false);

					//// Part Ribbon (ModelTab)

					//// 2. Get Part tab
					//tabSampleBlog = ribbonPart.RibbonTabs["id_TabModel"];

					//// 3. Create panel
					//pnlMyCommands = tabSampleBlog.RibbonPanels.Add("Макросы", "id_Panel_ModelReplacePart", Guid.NewGuid().ToString());

					//// 4. Add Button to Panel
					//pnlMyCommands.CommandControls.AddButton(ReplacePartCommand, true, false);
					////pnlMyCommands.CommandControls.AddSeparator();
					////pnlMyCommands.CommandControls.AddButton(ExportDXFCommand, true, false);
					////pnlMyCommands.CommandControls.AddButton(BendTechnologyCommand, true, false);


					//// Drawing Ribbon (PlaceViewsTab)
					//// 1. Access the Part ribbon
					//ribbonPart = userInterfaceManager.Ribbons["Drawing"];

					//// 2. Get Part tab
					//tabSampleBlog = ribbonPart.RibbonTabs["id_TabPlaceViews"];

					//// 3. Create panel
					//pnlMyCommands = tabSampleBlog.RibbonPanels.Add("Макросы", "id_Panel_PlaceViewsReplacePart", Guid.NewGuid().ToString());

					//// 4. Add Button to Panel
					//pnlMyCommands.CommandControls.AddButton(ExportPDFCommand, true, false);
					//pnlMyCommands.CommandControls.AddButton(TranslateToENCommand, true, false);
					//pnlMyCommands.CommandControls.AddSeparator();
					//pnlMyCommands.CommandControls.AddButton(FramePartsListCommand, true, false);

					//// Drawing Ribbon (TabAnnotateESKD)

					//// 2. Get Part tab
					//tabSampleBlog = ribbonPart.RibbonTabs["id_TabAnnotateESKD"];

					//// 3. Create panel
					//pnlMyCommands = tabSampleBlog.RibbonPanels.Add("Макросы", "id_Panel_AnnotateESKDReplacePart", Guid.NewGuid().ToString());

					//// 4. Add Button to Panel
					//pnlMyCommands.CommandControls.AddButton(ExportPDFCommand, true, false);
					//pnlMyCommands.CommandControls.AddButton(TranslateToENCommand, true, false);
					//pnlMyCommands.CommandControls.AddSeparator();
					//pnlMyCommands.CommandControls.AddButton(FramePartsListCommand, true, false);
				}
			}

		}

		public void SpecificationCommand_OnExecute(NameValueMap context)
		{
			Preloading startPreloading = new Preloading(m_inventorApplication);
			startPreloading = null;
		}



		public void Deactivate()
		{
			// This method is called by Inventor when the AddIn is unloaded.
			// The AddIn will be unloaded either manually by the user or
			// when the Inventor session is terminated

			// TODO: Add ApplicationAddInServer.Deactivate implementation

			// Release objects.
			m_inventorApplication = null;

			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		public void ExecuteCommand(int commandID)
		{
			// Note:this method is now obsolete, you should use the 
			// ControlDefinition functionality for implementing commands.
		}

		public object Automation
		{
			// This property is provided to allow the AddIn to expose an API 
			// of its own to other programs. Typically, this  would be done by
			// implementing the AddIn's API interface in a class and returning 
			// that class object through this property.

			get
			{
				// TODO: Add ApplicationAddInServer.Automation getter implementation
				return null;
			}
		}

		#endregion

	}
}
