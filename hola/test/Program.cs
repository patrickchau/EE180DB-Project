/*
 * Created by SharpDevelop.
 * User: Jorge
 * Date: 1/29/2020
 * Time: 7:11 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading;

namespace Speechrec
{
	class Function
	{
		static ManualResetEvent _completed = null;
		static void Main(string[] args)
		{
			 _completed = new ManualResetEvent(false);
			 SpeechRecognitionEngine _recognizer = new SpeechRecognitionEngine();
			 _recognizer.LoadGrammar(new Grammar(new GrammarBuilder("quick")) { Name = "testGrammar" }); // load a grammar
			 _recognizer.LoadGrammar(new Grammar(new GrammarBuilder("stop")) { Name = "testGrammar2" }); // load a grammar
			 _recognizer.LoadGrammar(new Grammar(new GrammarBuilder("paint")) { Name = "testGrammar3" }); // load a grammar
			 _recognizer.LoadGrammar(new Grammar(new GrammarBuilder("shrink")) { Name = "testGrammar4" }); // load a grammar
			 _recognizer.LoadGrammar(new Grammar(new GrammarBuilder("invincible")) { Name = "testGrammar5" }); // load a grammar
			 _recognizer.LoadGrammar(new Grammar(new GrammarBuilder("pineapple")) { Name = "exitGrammar" }); // load a "exit" grammar
			 _recognizer.SpeechRecognized += _recognizer_SpeechRecognized; 
			 _recognizer.SetInputToDefaultAudioDevice(); // set the input of the speech recognizer to the default audio device
			 _recognizer.RecognizeAsync(RecognizeMode.Multiple); // recognize speech asynchronous
			 _completed.WaitOne(); // wait until speech recognition is completed
			 _recognizer.Dispose(); // dispose the speech recognition engine
		} 
		
		static void _recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
		{
			//Console.WriteLine(e.Result.Text);
			 if (e.Result.Text == "quick") // e.Result.Text contains the recognized text
			 {
				 Console.WriteLine("go fast");
			 } 
			 if (e.Result.Text == "stop") // e.Result.Text contains the recognized text
			 {
				 Console.WriteLine("no go");
			 } 
			 if (e.Result.Text == "paint") // e.Result.Text contains the recognized text
			 {
				 Console.WriteLine("I can't see");
			 }
			 if (e.Result.Text == "shrink") // e.Result.Text contains the recognized text
			 {
				 Console.WriteLine("small boiz");
			 } 
			 if (e.Result.Text == "invincible") // e.Result.Text contains the recognized text
			 {
				 Console.WriteLine("strong boi");
			 }  
			 else if (e.Result.Text == "pineapple")
			 {
				 _completed.Set();
			 }
		}
	}
}