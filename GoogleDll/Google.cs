using Google.Cloud.TextToSpeech.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelsDll;

namespace GoogleDll
{
	public class Google
	{
		private Settings _settings;
		private readonly ILogger<Google> _logger;
        private TextToSpeechClient _ttsClient;

        public Google(ILogger<Google> logger, IConfiguration configuration)
		{
			_logger = logger;
			_settings = configuration.GetSection("Settings").Get<Settings>();

            string credentialPath = _settings.GoogleFunction.CredentialPath;
            if (!File.Exists(credentialPath))
            {
                credentialPath = Path.Combine("D:\\Dev\\Twitch", credentialPath);
			}
            _ttsClient = new TextToSpeechClientBuilder { CredentialsPath = credentialPath }.Build();

        }

		public MemoryStream ConvertToSpeech(string text)
		{
            var input = new SynthesisInput
            {
                Text = text
            };

            var voiceSelection = new VoiceSelectionParams
            {
                LanguageCode = "fr-FR",
                Name = "fr-FR-Wavenet-E"
            };

            var audioConfig = new AudioConfig
            {
                AudioEncoding = AudioEncoding.Mp3
            };

            var response = _ttsClient.SynthesizeSpeech(input, voiceSelection, audioConfig);

            MemoryStream stream = new MemoryStream();
            response.AudioContent.WriteTo(stream);

            return stream;
        }
	}
}