/*
 * Created by SharpDevelop.
 * User: Ire
 * Date: 02/01/2006
 * Time: 23:06 
 */
 
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Gtk;
using Gdk;
using Glade;

using MathTextCustomWidgets;
using MathTextCustomWidgets.Widgets.Logger;
using MathTextCustomWidgets.Widgets.ImageArea;
using MathTextCustomWidgets.Dialogs;
using MathTextCustomWidgets.Dialogs.SymbolLabel;

using MathTextLibrary;
using MathTextLibrary.Bitmap;
using MathTextLibrary.BitmapSegmenters;
using MathTextLibrary.Symbol;
using MathTextLibrary.Controllers;
using MathTextLibrary.Databases.Characteristic.Characteristics;

using MathTextRecognizer.Stages;
using MathTextRecognizer.Controllers;
using MathTextRecognizer.DatabaseManager;
using MathTextRecognizer.LexicalRulesManager;
using MathTextRecognizer.SyntacticalRulesManager;

namespace MathTextRecognizer
{
	/// <summary>
	/// Esta clase representa la ventana principal de la aplicacion de
	/// reconocimiento de formulas.
	/// </summary>
	public class MainRecognizerWindow
	{
#region Glade-Widgets
		[WidgetAttribute]
		private Gtk.Window mainWindow = null;		
		
		[WidgetAttribute]		
		private ToolButton toolLoadImage = null;
				
		[WidgetAttribute]		
		private ToolButton toolDatabase = null;
		
		[WidgetAttribute]
		private Expander expLog = null;
		
		[WidgetAttribute]
		private ImageMenuItem menuNewSession = null;
		
		[WidgetAttribute]
		private ImageMenuItem menuOpenDatabaseManager = null;
		
		[WidgetAttribute]
		private ImageMenuItem menuLoadImage = null;
		
		[WidgetAttribute]
		private HBox messageInfoHB = null;
		
		[WidgetAttribute]
		private ToolButton toolNewSession = null;
		
		[WidgetAttribute]
		private Notebook recognizingStepsNB = null;
		
		[WidgetAttribute]
		private Label stageNameLabel = null;
		
		[WidgetAttribute]
		private Label messageInfoLabel = null;
		
#endregion Glade-Widgets

#region Fields
		
		private bool recognizementFinished;
		
		private OCRStageWidget ocrWidget;		
		private TokenizingStageWidget  tokenizingWidget;		
		private ParsingStageWidget parsingWidget;
			
		private LogView logView;
		
		private const string title="Reconocedor de texto matemático";		
		
		
		private string imageFile;
		
#endregion Fields
		
#region Main method
		
		public static void Main(string[] args)
		{			
			Application.Init();
			new MainRecognizerWindow();
			Application.Run();
		}
#endregion Main method
		
#region Constructors
		/// <summary>
		/// El constructor de <code>MainWindow</code>.
		/// </summary>
		public MainRecognizerWindow()
		{
			Glade.XML gxml = new Glade.XML (null, 
			                                "mathtextrecognizer.glade",
			                                "mainWindow",
			                                null);
			gxml.Autoconnect (this);			
			this.InitializeWidgets();			
			
			
			Config.RecognizerConfig.Instance.Changed+= new EventHandler(OnConfigChanged);	
			
			
			
			
			this.mainWindow.Icon = 
				ImageResources.LoadPixbuf("mathtextrecognizer48");
			
			OnConfigChanged(this, EventArgs.Empty);
			
			
		}
		
#endregion Constructors
		
#region Properties
		

		/// <value>
		/// Contiene el estado de expansión del visor del log.
		/// </value>
		public bool LogAreaExpanded
		{
			get
			{
				return expLog.Expanded;
			}
			set
			{
				expLog.Expanded = value;
				logView.Follow = value;
			}
		}

		/// <value>
		/// Contiene la ventana de la aplicacion.
		/// </value>
		public Gtk.Window Window 
		{
			get 
			{
				return mainWindow;
			}
		}
		
		/// <value>
		/// Contains a boolean value indicating if some elements of the gui
		/// which shouldn't be clicked while a process is running are sensitive.
		/// </value>
		public bool ProcessItemsSensitive
		{
			get
			{
				return toolDatabase.Sensitive;
			}
			set
			{
				toolDatabase.Sensitive =value;
				toolLoadImage.Sensitive =value;
				
				menuLoadImage.Sensitive =value;
				menuOpenDatabaseManager.Sensitive =value;
			}
		}
		
		/// <value>
		/// Contains the widget used to represent and control the 
		/// segmentation and matching process.
		/// </value>
		public OCRStageWidget OCRWidget
		{
			get
			{
				return ocrWidget;				
			}
		}
		
		/// <value>
		/// Contains the widget used to show and control the sintactical
		/// analisys process.
		/// </value>
		public TokenizingStageWidget TokenizingWidget
		{
			get
			{
				return tokenizingWidget;
			}			
		}

		/// <value>
		/// Contains the widget used to show the syntactical analysis.
		/// </value>
		public ParsingStageWidget FormulaMatchingWidget 
		{
			get 
			{
				return parsingWidget;
			}
		}

		/// <value>
		/// Contains the image file path to be recognized.
		/// </value>
		public string ImageFile 
		{
			get 
			{
				return imageFile;
			}
		}

		
#endregion Properties
		
#region Public methods
		
		/// <summary>
		/// Clears the log view
		/// </summary>
		public void ClearLog()
		{			
			logView.ClearLog();			
		}
		
		/// <summary>
		/// Writes a message in the log view.
		/// </summary>
		/// <param name="message">
		/// The message.
		/// </param>
		public void Log(string message, params object[] args)
		{			
			logView.LogLine(message, args);
		}
		
		public void CreateOCRWidget()
		{
			// We add the OCR widget.
			ocrWidget = new OCRStageWidget(this);
			
			
			recognizingStepsNB.AppendPage(ocrWidget,
			                              new Label(OCRStageWidget.WidgetLabel));
			
			ocrWidget.ShowAll();
			
			
		}
		
		public void CreateUnassistedWidget()
		{
			UnassistedStageWidget widget = new UnassistedStageWidget(this);
			
			recognizingStepsNB.AppendPage(widget,
			                              new Label(UnassistedStageWidget.WidgetLabel));
			
			widget.ShowAll();
		}
		
		public void CreateTokenizingWidget()
		{
			// We add the tokenizer widget.
			tokenizingWidget = new TokenizingStageWidget(this);
			recognizingStepsNB.AppendPage(tokenizingWidget,
			                              new Label(TokenizingStageWidget.WidgetLabel));
			tokenizingWidget.ShowAll();
		}
		
		public void CreateParsingWidget()
		{
			parsingWidget = new ParsingStageWidget(this);
			recognizingStepsNB.AppendPage(parsingWidget,
			                              new Label(ParsingStageWidget.WidgetLabel));
			parsingWidget.ShowAll();
		}
		
		public void CreateBlackboardWidget()
		{
			Widget stage = new BlackboardStageWidget(this);
			recognizingStepsNB.AppendPage(stage,
			                              new Label(BlackboardStageWidget.WidgetLabel));
			stage.ShowAll();
		}
		
		
		/// <summary>
		/// Metodo que maneja el evento provocado al cerrarse el dialogo de 
		/// apertura de imagen.
		/// </summary>
		public bool LoadImage()
		{
			string filename;
			
			if(ImageLoadDialog.Show(mainWindow, out filename)
				== ResponseType.Ok)
			{			
				this.imageFile = filename;
		
				recognizementFinished=false;
				ClearLog();
				ResetState();
				return true;
			}
			
			return false;
		}
		
	
		
#endregion Public methods
		
#region Non-public methods
		
	
		/// <summary>
		/// Para facilitar la inicializacion de los widgets.
		/// </summary>
		private void InitializeWidgets()
		{		
			mainWindow.Title = title;
			
			// Ponemos iconos personalizados en los botones
			menuLoadImage.Image = ImageResources.LoadImage("insert-image16");
			toolLoadImage.IconWidget = ImageResources.LoadImage("insert-image22");
			
			menuOpenDatabaseManager.Image = ImageResources.LoadImage("database16");
			toolDatabase.IconWidget = ImageResources.LoadImage("database22");
			
			toolNewSession.IconWidget = ImageResources.LoadImage("window-new22");
			menuNewSession.Image = ImageResources.LoadImage("window-new16");
			
			// Creamos el cuadro de registro.
			logView = new LogView();
			expLog.Add(logView);	
			
			while(recognizingStepsNB.NPages > 0)
			{
				recognizingStepsNB.RemovePage(0);
			}
			
			recognizingStepsNB.AppendPage(new InitialStageWidget(this),
			                              new Label(InitialStageWidget.WidgetLabel));
			
			mainWindow.ShowAll();
			
			recognizingStepsNB.Page = 0;
		}
		
		
		/// <summary>
		/// Metodo que maneja el evento provocado al cerrar la ventana.
		/// </summary>
		/// <param name="sender">El objeto que provoca el evento.</param>
		/// <param name="arg">Los argumentos del evento.</param>
		private void OnMainWindowDeleteEvent(object sender,DeleteEventArgs arg)
		{
			OnExit();
		}		
		
		/// <summary>
		/// Manejo del evento provocado al hacer clic sobre la opcion de menu
		/// "Acerca de".
		/// </summary>
		private void OnMenuAboutClicked(object sender, EventArgs arg)
		{
			AppInfoDialog.Show(
				mainWindow,
				"Reconocedor de texto matemático",
				"Este programa se encarga de el reconocimiento de fórmulas contenidas en imágenes y su conversión a un formato de texto plano especificado a traves de reglas.",
			     "mathtextrecognizer");
		}
		
		/// <summary>
		///	Manejo del evento provocado al hacer click en el boton 
		/// "Abrir base de datos". 
		/// </summary>
		private void OnOpenDatabaseManagerClicked(object sender, EventArgs arg)
		{	
			DatabaseManagerDialog dialog = 
				new DatabaseManagerDialog(this.Window);
			dialog.Show();			
			dialog.Destroy();
		}
		
			
		/// <summary>
		/// Manejo del evento provocado al hacer click en la opcion "Salir"
		/// del menu.
		/// </summary>
		private void OnExitClicked(object sender, EventArgs arg)
		{
			OnExit();
		}
		
		/// <summary>
		/// Manejo del evento provocado al hacer click en el boton 
		/// "Cargar imagen".
		/// </summary>
		private void OnLoadImageClicked(object sender, EventArgs arg)
		{
			
			ResponseType res = ResponseType.Yes;
			
			if(recognizementFinished)
			{
				 res = ConfirmDialog.Show(
					mainWindow,
					"Si cargas una nueva imágen perderás el reconocimiento realizado.\n"+
					"¿Deseas continuar?");
			}
			
			if(res==ResponseType.Yes)
			{			
				(recognizingStepsNB.Children[1] as RecognizingStageWidget).SetInitialData();
			}
		}
		
			
		/// <summary>
		/// Manejo del evento provocado al hacer click en el botón
		/// "Nueva sesion".
		/// </summary>
		private void OnNewSessionClicked(object sender, EventArgs arg)
		{			
			System.Diagnostics.Process.Start(System.Environment.CommandLine);			
		}
		
		/// <summary>
		/// Handles the change in the notebook holding the widgets for the
		/// recognizing stages, so we can chow its name in a label.
		/// </summary>
		/// <param name="sender">
		/// A <see cref="System.Object"/>
		/// </param>
		/// <param name="arg">
		/// A <see cref="ChangeCurrentPageArgs"/>
		/// </param>
		private void OnRecognizingStepsNBSwitchPage(object sender,
		                                            SwitchPageArgs arg)
		{
			
			Widget page = 
				recognizingStepsNB.Children[(int)arg.PageNum];
			stageNameLabel.Markup = 
				String.Format("<b><i>{0}</i></b>",
				              recognizingStepsNB.GetTabLabelText(page));
			
			bool hasLoadImage = 
				(page.GetType() == typeof(OCRStageWidget)					
				|| page.GetType() == typeof(UnassistedStageWidget));
			
			toolLoadImage.Sensitive = hasLoadImage;
			this.menuLoadImage.Sensitive = hasLoadImage;
		}
		
		/// <summary>
		/// Handles the click on the simbol list editor menu item.
		/// </summary>
		/// <param name="sender">
		/// A <see cref="System.Object"/>
		/// </param>
		/// <param name="arg">
		/// A <see cref="EventArgs"/>
		/// </param>
		private void OnSymbolListEditorItem(object sender, EventArgs arg)
		{
			SymbolLabelDialog dialog = 
				new SymbolLabelDialog(mainWindow);
			
			dialog.Show();
			dialog.Destroy();
		}
	
		
	
		
		

		/// <summary>
		/// Metodo que se encarga de gestionar la salida de la aplicacion.
		/// </summary>
		private void OnExit()
		{
			foreach (RecognizingStageWidget widget in recognizingStepsNB) 
			{
				widget.Abort();
			}
			Application.Quit();			
		}	
		
		/// <summary>
		/// Handles the event produced when the config changes.
		/// </summary>
		/// <param name="sender">
		/// A <see cref="System.Object"/>
		/// </param>
		/// <param name="args">
		/// A <see cref="EventArgs"/>
		/// </param>
		private void OnConfigChanged(object sender, EventArgs args)
		{
			if(Config.RecognizerConfig.Instance.DatabaseFilesInfo.Count ==0)
			{
				messageInfoHB.Visible = true;
				messageInfoLabel.Text = "No hay bases de datos  de caracteres para reconocer, añada una en el gestor de bases de datos.";
			}			
			else if(Config.RecognizerConfig.Instance.LexicalRules.Count == 0)
			{
				messageInfoHB.Visible = true;
				messageInfoLabel.Text = "No hay reglas léxicas definidas, añada una en el gestor de reglas léxicas.";
			}
			else if(Config.RecognizerConfig.Instance.SyntacticalRules.Count == 0)
			{
				messageInfoHB.Visible = true;
				messageInfoLabel.Text = "No hay reglas sintácticas definidas, añada una en el gestor de reglas sintácticas.";
			}
			else
			{
				messageInfoHB.Visible = false;
			}
			
				
		}
		
		
		
		/// <summary>
		/// Reiniciamos los valores de los widgets al estado inicial.
		/// </summary>
		private void ResetState()
		{
			
			menuOpenDatabaseManager.Sensitive=true;
			
			toolDatabase.Sensitive=true;
			
			// We reset the state of the stage widgets,
			// so the information generated for previous seasons is discarded.
			
			while(recognizingStepsNB.NPages > 2)
			{
				recognizingStepsNB.RemovePage(2);
			}
			
			
			
			recognizementFinished=true;
		}
		
		/// <summary>
		/// Opens the lexical rules manager when the appropiate menu item is
		/// clicked.
		/// </summary>
		/// <param name="sender">
		/// A <see cref="System.Object"/>
		/// </param>
		/// <param name="args">
		/// A <see cref="EventArgs"/>
		/// </param>
		private void OnLexicalManagerItemClicked(object sender, EventArgs args)
		{
			LexicalRulesManagerDialog dialog = 
				new LexicalRulesManagerDialog(this.Window);
			
			dialog.Show();
			
			dialog.Destroy();
		}
		
		/// <summary>
		/// Shows the syntactical rules manager.
		/// </summary>
		/// <param name="sender">
		/// A <see cref="System.Object"/>
		/// </param>
		/// <param name="args">
		/// A <see cref="EventArgs"/>
		/// </param>
		private void OnSyntacticalManagerItemActivate(object sender, 
		                                              EventArgs args)
		{
			SyntacticalRulesManagerDialog dialog = 
				new SyntacticalRulesManagerDialog(this.Window);
			dialog.Show();
			
			dialog.Destroy();
		}
		
		/// <summary>
		/// Shows the output settings dialog.
		/// </summary>
		/// <param name="sender">
		/// A <see cref="System.Object"/>
		/// </param>
		/// <param name="args">
		/// A <see cref="EventArgs"/>
		/// </param>
		private void OnOutputSettingsItemActivate(object sender, EventArgs args)
		{
			Output.OutputSettingsDialog dialog = 
				new Output.OutputSettingsDialog(this.Window);
			
			dialog.Show();
			dialog.Destroy();
		}
	}
	
#endregion Non-public methods
}
