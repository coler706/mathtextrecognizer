// created on 02/01/2006 at 12:55

using System;
using System.Threading;
using System.Collections.Generic;

using MathTextLibrary;
using MathTextLibrary.Bitmap;
using MathTextLibrary.Symbol;
using MathTextLibrary.Databases;
using MathTextLibrary.Databases.Caracteristic;

namespace MathTextLibrary.Controllers
{

	/// <summary>
	/// Enumeracion que indica el tipo de paso a paso con el que se
	/// van a realizar los procesos de cada controlador.
	/// </summary>
	public enum MathTextRecognizerControllerStepMode
	{
		/// <summary>
		/// Representa el modo de nodo a nod de la imagen,
		/// deteniendose en la comprobacion de las caracteristicas binarias
		/// de la imagen.
		/// </summary>
		NodeByNodeWithCaracteristicCheck,		
		
		/// <summary>
		/// Representa el modo de paso a paso por nodo segmentado de la imagen.
		/// </summary>
		NodeByNode,
		
		/// <summary>
		/// Representa el modo de ejecucion sin detenerse en pasos intermedios.
		/// </summary>
		UntilEnd
	}

	/// <summary>
	/// La clase MathTextRecognizerController realiza las funciones de control
	/// de los procesos de reconocimiento de formulas matematicas, ofreciendo una 
	/// fachada a las posibles interfaces de usuario para abstraerlas de este 
	/// cometido.
	/// </summary>
	public class MathTextRecognizerController{			
		
		//La base de datos que usaremos para reconocer los caracteres.
		private MathTextDatabase database;
		
		//El modo de ejecucion paso a paso del proceso.
		private MathTextRecognizerControllerStepMode stepByStep;		
		
		//Semaforos para garantizar la exclusion mutua, necesarios por el uso de
		//varios hilos sobre algunos recursos compartidos.
		private Mutex stepMutex;
		private Mutex resumeMutex;
		
		/// <summary>
		/// Evento usado para enviar un mensaje de informacion a la interfaz.
		/// </summary>
		public event ControllerLogMessageSentEventHandler LogMessageSent;
		
		/// <summary>
		/// Evento usado para notificar a la interfaz de que se ha terminado de
		/// realizar un proceso.
		/// </summary>
		public event ControllerProcessFinishedEventHandler RecognizeProcessFinished;
		
		/// <summary>
		/// Evento usado para notificar a la interfaz de que se ha comenzado a trabajar
		/// con un nueva pieza de la imagen.
		/// </summary>
		public event ControllerBitmapBeingRecognizedEventHandler BitmapBeingRecognized;
		
		//La imagen raiz que contiene la formula completa que deseamos reconocer.
		private MathTextBitmap startImage;
		
		//El hilo que hemos usado para implementar la ejecucion paso a paso.		
		private Thread recognizeThread;
		
		/// <summary>
		/// Constructor de la clase MathTextRecognizerController, debe ser invocado
		/// en las posibles implementaciones distintas de la interfaz de usuario del
		/// reconocedor.
		/// </summary>
		public MathTextRecognizerController(){			
			
			//Creamos una base de datos vacia en principio
			database.RecognizingStepDone+=
				new ProcessingStepDoneEventHandler(OnProcessingStepDone);				
			
			stepByStep = MathTextRecognizerControllerStepMode.UntilEnd;
			stepMutex = new Mutex();
			resumeMutex = new Mutex();	
		}
	
		/// <summary>
		/// Envolvemos el lanzamiento del evento BitmapBeingRecognized, por comodidad.
		/// </summary>
		/// <param name="bitmap">
		/// La imagen que hemos comenzado a reconocer, que sera enviada como
		/// argumentod del evento.
		/// </param>		
		protected void OnBitmapBeingRecognized(MathTextBitmap bitmap){
			if(BitmapBeingRecognized!=null){
				BitmapBeingRecognized(this,
					new ControllerBitmapBeingRecognizedEventArgs(bitmap));
			}
		}			
		
		/// <summary>
		/// Envolvemos el lanzamiento del evento LogMessageSend, por comodidad.
		/// </summary>
		/// <param name="msg">
		/// El mensaje que queremos pasar como argumento al manejador del evento.
		/// </param>		
		protected void OnLogMessageSend(string msg)
		{
			if(LogMessageSent!=null)
			{
				LogMessageSent(this,new MessageLogSentEventArgs(msg));
			}
		}
		
		/// <summary>
		/// Envolvemos el lanzamiento del evento RecognizeProcessFinished, por comodidad.
		/// </summary>		
		protected void OnRecognizeProcessFinished()
		{
			if(RecognizeProcessFinished!=null)
			{
				RecognizeProcessFinished(this,EventArgs.Empty);
			}

		}
		
		/// <summary>
		/// Manejador del evento RecognizingCaracteristicChecked de la base de datos de caracteres.
		/// </summary>
		/// <param name="sender">El objeto que envio el evento.</param>
		/// <param name="args">Los argumentos del evento.</param>
		private void OnProcessingStepDone(object sender,
		                                  ProcessingStepDoneEventArgs args)
		{
			//Lo que hacemos es notificar a la interfaz de que una determinada caracteristica binaria
			//ha tomado un valor, y que caracteres son similares.
			OnLogMessageSend(args.Process.GetType()+": "+args.Result);
			string similar="";	
			if(args.SimilarSymbols!=null){
				foreach(MathSymbol ms in args.SimilarSymbols){
					similar+="«"+ms.Text+"»,";
				}
				OnLogMessageSend("Caracteres similares: "+similar.TrimEnd(new char[]{','}));
			}
		}
		
		/// <summary>
		/// Propiedad para establecer el modo de ejecucion paso a paso del procesado.
		/// </summary>
		public MathTextRecognizerControllerStepMode StepMode{
			get{
				return stepByStep;
			}
			set{
				lock(stepMutex){
					stepByStep=value;
				}
			}
		}
		
		/// <summary>
		/// Cargamos la base de datos que vamos a utilizar para intentar 
		/// reconocer las imagenes como caracteres.
		/// </summary>
		/// <param name="path">
		/// La ruta del fichero donde esta la base de datos.
		/// </param>
		public void LoadDatabase(string path)
		{
			
			database = MathTextDatabase.Load(path);
		}
		
		/// <summary>
		/// Propiedad que permite establecer y recuperar la imagen de inicio que
		/// contiene la formula que deseamos reconocer.
		/// </summary>
		public MathTextBitmap StartImage
		{
			get
			{				
				return startImage; 			
			}
			set
			{
				startImage=value;
			}
		}
		
		/// <summary>
		/// Este es el metodo que hay que llamar para comenzar/continuar el proceso
		/// </summary>
		/// <returns>Cierto si quedan aun pasos por ejecutar, falso en caso contrario.</returns>
		public bool NextRecognizeStep()
		{
		
			bool res=true;
			if(recognizeThread==null){
				recognizeThread=new Thread(new ThreadStart(RecognizeProcess));
				recognizeThread.Priority=ThreadPriority.Highest;
				recognizeThread.Start();								
			}else{
				//Si hemos llegado a aqui, es porque el hilo esta en ejecucion y en algun
				//modo paso a paso. Si hemos querido cambiar el modo de paso a paso lo habremos
				//hecho en la interfaz mediante la propiedad que aqui ofrecemos.
				//Por tanto lo unico que tenemos que hacer es despertar el hilo.	
				lock(resumeMutex){
					if(recognizeThread.ThreadState==ThreadState.Suspended){
						recognizeThread.Resume();
						
					}else if(recognizeThread.ThreadState==ThreadState.Stopped){
						res=false;
					}
				}
			}
			return res;		
		}
		
		/// <summary>
		/// Metodo cuya ejecucion se realiza en un hilo separado.
		/// </summary>
		private void RecognizeProcess(){
		   	RecognizerTreeBuild(startImage);
		   	OnRecognizeProcessFinished();
		   	recognizeThread=null;
		}
		
		/// <summary>
		/// Con este metodo cramos un arbol de imagenes, de forma recursiva.
		/// Primero intentamos reconocer la imagen como un caracter, si no es posible,
		/// la intentamos segmentar. Si ninguno de estos procesos es posible, la imagen no
		/// pudo ser reconocida.
		/// </summary>
		/// <param name="node">La imagen que vamos a tratar de reconocer/segmentar.</param>
		private void RecognizerTreeBuild(MathTextBitmap node){			
			
			//Para proteger el paso actual de un cambio de modo intermedio
			MathTextRecognizerControllerStepMode modeAux;
			lock(stepMutex){
				modeAux=stepByStep;
			}
			
			OnBitmapBeingRecognized(node);
		
			OnLogMessageSend("Tratando la subimagen situada a partir de "+node.Position);
						
			//Si no logramos reconocer nada, es el simbolo nulo, tambien sera
			//el simbolo nulo aunque hayamos podido crearle hijos.
			MathSymbol associatedSymbol;
			
			//Lanzamos el reconocedor de caracteres.
			List<MathSymbol> associatedSymbols =database.Recognize(node);
			
			// Decidimos que símbolo de los  posiblemente devuelto usuaremos.			
			associatedSymbol = ChooseSymbol(associatedSymbols);
						
			// Asociamos el símbolo al nodo.
			node.Symbol=associatedSymbol;	
			//Si no hemos reconocido nada, pues intentaremos segmentar el caracter.
			if(associatedSymbol.SymbolType == MathSymbolType.NotRecognized)
			{			
				OnLogMessageSend("La imagen no pudo ser reconocida como un simbolo por la base de datos");
				
				node.CreateChildren();
				
				if(node.Children!=null && node.Children.Count > 1)
				{
					OnLogMessageSend("La imagen se ha segmentado correctamente");
				}
				else
				{
					OnLogMessageSend("La imagen no pudo ser segmentada, el símbolo queda sin reconocer");
				}
			}
			else
			{
				OnLogMessageSend("Símbolo reconocido por la base de datos como «"
				                 +associatedSymbol.Text+"»");
			}
			
					
			
			//Paramos aqui, lo que sigue es la llamada al procesamiento de los nodos hijos
			if(modeAux != MathTextRecognizerControllerStepMode.UntilEnd)
			{				
				recognizeThread.Suspend();
			}
			
			if(node.Children !=null && node.Children.Count>1){				
				//Si solo conseguimos un hijo, es la propia imagen, asi que nada
				foreach(MathTextBitmap child in node.Children){
					RecognizerTreeBuild(child);						
				}
			}
		}
		
	
		/// <summary>
		/// 
		/// </summary>
		/// <param name="symbols">
		/// A <see cref="List`1"/>
		/// </param>
		/// <returns>
		/// A <see cref="MathSymbol"/>
		/// </returns>
		private MathSymbol ChooseSymbol(List<MathSymbol> symbols)
		{
			if(symbols.Count == 0)
			{
				//TODO Aprender caracteres no reconocidos
				return MathSymbol.NullSymbol;
			}
			else if(symbols.Count==1)
			{
				return symbols[0];
			}
			else
			{
				//TODO Seleccion entre varios caracteres
				throw new NotImplementedException("TODO seleccion entre varios caracteres");
			}
		}
	}
}
