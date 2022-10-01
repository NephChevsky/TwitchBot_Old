using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelsDll;
using System.Speech.Synthesis;

namespace SpeechDll
{
	public class Speech
	{
		private Settings _settings;
		private Secret _secret;
		private static ILogger<Speech> _logger;

		private SpeechSynthesizer _reader;

		public Speech(IConfiguration configuration, ILogger<Speech> logger)
		{
			_settings = configuration.GetSection("Settings").Get<Settings>();
			_secret = configuration.GetSection("Secret").Get<Secret>();
			_logger = logger;

			_reader = new();
			//_reader.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult, 0, new System.Globalization.CultureInfo("fr-FR"));
		}

		public MemoryStream TextToSpeech(string text)
		{
			MemoryStream speech = new();
			_reader.SetOutputToWaveStream(speech);
			_reader.Speak(text);
			return speech;
		}
	}
}