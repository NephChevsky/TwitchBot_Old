using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelsDll;
using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace SpeechDll
{
	public class Speech
	{
		private Settings _settings;
		private static ILogger<Speech> _logger;

		private SpeechSynthesizer _reader;
		private SpeechRecognitionEngine _recognizer;

		public Speech(IConfiguration configuration, ILogger<Speech> logger)
		{
			_settings = configuration.GetSection("Settings").Get<Settings>();
			_logger = logger;

			_reader = new();
			_reader.SelectVoice("Microsoft Hortense Desktop");


		}

		public MemoryStream TextToSpeech(string text)
		{
			MemoryStream speech = new();
			_reader.SetOutputToWaveStream(speech);
			_reader.Speak(text);
			return speech;
		}

		public void StartSpeechToText(EventHandler<SpeechRecognizedEventArgs> method)
		{
			_recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("fr-FR"));
			_recognizer.LoadGrammar(new DictationGrammar());
			_recognizer.SpeechRecognized += method;
			_recognizer.SetInputToDefaultAudioDevice();
			_recognizer.RecognizeAsync(RecognizeMode.Multiple);
		}

		public void StopSpeechToText()
		{
			_recognizer.Dispose();
			_recognizer = null;
		}
	}
}