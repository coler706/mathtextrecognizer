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

using MathTextLibrary;
using MathTextLibrary.Bitmap;
using MathTextLibrary.BitmapSegmenters;
using MathTextLibrary.Symbol;
using MathTextLibrary.Controllers;
using MathTextLibrary.Databases.Characteristic.Characteristics;

using MathTextRecognizer.Steps;
using MathTextRecognizer.Controllers;
using MathTextRecognizer.DatabaseManager;

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
		private Gtk.Window mainWindow;		
		
		[WidgetAttribute]		
		private ToolButton toolLoadImage;
		
		[WidgetAttribute]		
		private ToolButton toolLatex;
		
		[WidgetAttribute]		
		private ToolButton toolDatabase;
		
		[WidgetAttribute]
		private Expander expLog;
		
		[WidgetAttribute]
		private ImageMenuItem menuNewSession;
		
		[WidgetAttribute]
		private ImageMenuItem menuOpenDatabaseManager;
		
		[WidgetAttribute]
		private ImageMenuItem menuMakeOutput;
		
		[WidgetAttribute]
		private ImageMenuItem menuLoadImage;
		
		[WidgetAttribute]
		private HBox messageInfoHB;
		
		[WidgetAttribute]
		private ToolButton toolNewSession;
		
		[WidgetAttribute]
		private Notebook recognizingStepsNB;
		
		#endregion Glade-Widgets
		
		#region Otros atributos
		
		
		
		private bool recognizementFinished;
		
		private SegmentingAndMatchingStepWidget segmentingAndMatchingStepWidget;
		
		private LogView logView;
		
		private const string title="Reconocedor de caracteres matemáticos - ";	
		
		
		
		private DatabaseManagerDialog databaseManagerDialog;
		

		
		#endregion Otros atributos
		
		public static void Main(string[] args)
		{			
			Application.Init();
			new MainRecognizerWindow();
			
			
			
			Application.Run();
		}
		
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
			this.Initialize();			
			
			databaseManagerDialog = new DatabaseManagerDialog(this.mainWindow);
			databaseManagerDialog.DatabaseListChanged += 
				new EventHandler(OnDatabaseManagerDialogDatabaseListChanged);
			
			// Asignamos la configuracion inicial al dialogo de gestion de
			// bases de datos.			
			databaseManagerDialog.DatabaseFilesInfo = 
				Config.RecognizerConfig.Instance.DatabaseFilesInfo;
		}
		
		
#region Propiedades
		
		/// <value>
		/// Contiene el dialogo de gestion de bases de datos.
		/// </value>
		public DatabaseManagerDialog DatabaseManager
		{
			get
			{
				return databaseManagerDialog;
			}
		}
		
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
			}
		}

		/// <value>
		/// Contiene la ventana de la aplicacion.
		/// </value>
		public Gtk.Window MainWindow 
		{
			get 
			{
				return mainWindow;
			}
		}
		
#endregion Propiedades
		
#region Metodos publicos
		
		/// <summary>
		/// Método que permite borrar la zona de informacion de proceso.
		/// </summary>
		public void ClearLog()
		{			
			logView.ClearLog();			
		}
		
		/// <summary>
		/// Método usado para escribir un mensaje en la zona de información de proceso.
		/// </summary>
		/// <param name="message">El mensaje a escribir.</param>
		public void Log(string message, params object[] args)
		{			
			logView.LogLine(message, args);
		}
		
#endregion Metodos publicos
		
#region Metodos privados
		
		
		/// <summary>
		/// Para facilitar la inicializacion de los widgets.
		/// </summary>
		private void Initialize()
		{		
			mainWindow.Title = title + "Sin imagen";
			
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
			
			segmentingAndMatchingStepWidget = 
				new SegmentingAndMatchingStepWidget(this);
			
			recognizingStepsNB.AppendPage(segmentingAndMatchingStepWidget,
			                              new Label("Segmentación y "
			                                        +"reconocimiento de carácteres"));
			
			mainWindow.ShowAll();
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
				"Reconocedor de caracteres matemáticos",
				"Este programa se encarga de el reconocimiento del las "
				+ "fórmulas contenidas en imágenes,y su posterior conversión.");
		}
		
		/// <summary>
		///	Manejo del evento provocado al hacer click en el boton 
		/// "Abrir base de datos". 
		/// </summary>
		private void OnOpenDatabaseManagerClicked(object sender, EventArgs arg)
		{	
			databaseManagerDialog.Run();			
		}
		
			
		/// <summary>
		/// Manejo del evento provocado al hacer click en la opcion "Salir"
		/// del menu.
		/// </summary>
		private void OnExitClicked(object sender, EventArgs arg)
		{
			databaseManagerDialog.Destroy();
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
				LoadImage();
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
		/// Metodo que maneja el evento provocado al cerrarse el dialogo de 
		/// apertura de imagen.
		/// </summary>
		private void LoadImage()
		{
			string filename;
			
			if(ImageLoadDialog.Show(mainWindow, out filename)
				== ResponseType.Ok)
			{			
				// Cargamos la imagen desde disco
					
				segmentingAndMatchingStepWidget.SetInitialImage(filename);
				
				this.mainWindow.Title = 
					title + System.IO.Path.GetFileName(filename);
			
				recognizementFinished=false;
				toolLatex.Sensitive=false;
				menuMakeOutput.Sensitive=false;
				
				ClearLog();
				
			}
		}
		
		

		/// <summary>
		/// Metodo que se encarga de gestionar la salida de la aplicacion.
		/// </summary>
		private void OnExit()
		{
			Application.Quit();			
		}	
		
		/// <summary>
		/// Maneja el evento producido al cambiar la lista de bases de datos usadas
		/// para reconocer. 
		/// </summary>
		/// <param name="sender">
		/// A <see cref="System.Object"/>
		/// </param>
		/// <param name="args">
		/// A <see cref="EventArgs"/>
		/// </param>
		private void OnDatabaseManagerDialogDatabaseListChanged(object sender,
		                                                        EventArgs args)
		{
			messageInfoHB.Visible = 
				databaseManagerDialog.DatabaseFilesInfo.Count ==0;
		}
		
		
		
		/// <summary>
		/// Reiniciamos los valores de los widgets al estado inicial.
		/// </summary>
		private void ResetState()
		{
			toolLatex.Sensitive=true;
			
			menuLoadImage.Sensitive=true;
			menuOpenDatabaseManager.Sensitive=true;
			menuMakeOutput.Sensitive=true;
			
			toolLoadImage.Sensitive=true;
			toolDatabase.Sensitive=true;
			
			segmentingAndMatchingStepWidget.ResetState();
			
			recognizementFinished=true;
		}
	}
	
#endregion Metodos privados
}