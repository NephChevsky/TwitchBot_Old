using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelsDll;

namespace DiscordDll
{
	public class Discord
	{
		private Settings _settings;
		private readonly ILogger<Discord> _logger;
		private DiscordSocketClient _client;

		public Discord(ILogger<Discord> logger, IConfiguration configuration)
		{
			_logger = logger;
			_settings = configuration.GetSection("Settings").Get<Settings>();
			_client = new DiscordSocketClient();
			_client.LoginAsync(TokenType.Bot, _settings.DiscordFunction.AccessToken).Wait();
			_client.StartAsync().Wait();
		}

		public async Task SendMessage(ulong channelId, string message)
		{
			IMessageChannel channel = await _client.GetChannelAsync(channelId) as IMessageChannel;
			await channel.SendMessageAsync(message);
		}

		public async Task<bool> DeleteMessage(ulong channelId, string owner, string message)
		{
			IMessageChannel channel = await _client.GetChannelAsync(channelId) as IMessageChannel;
			RequestOptions options = new();
			List<IMessage> messages = (List<IMessage>) await channel.GetMessagesAsync().FlattenAsync();
			ulong id = messages.Where(x => x.Author.Username == owner && x.Content == message).Select(x => x.Id).FirstOrDefault();
			if (id != 0)
			{
				await channel.DeleteMessageAsync(id);
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}