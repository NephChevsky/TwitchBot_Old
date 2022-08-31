using DbDll;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelsDll;
using ModelsDll.Db;

namespace DiscordDll
{
	public class Discord
	{
		private readonly ILogger<Discord> _logger;
		private DiscordSocketClient _client;

		public Discord(ILogger<Discord> logger)
		{
			_logger = logger;
			_client = new DiscordSocketClient();

			using (TwitchDbContext db = new())
			{
				Token token = db.Tokens.Where(x => x.Name == "DiscordAccessToken").FirstOrDefault();
				if (token != null)
				{
					_client.LoginAsync(TokenType.Bot, token.Value).Wait();
					_client.StartAsync().Wait();
				}
				else
				{
					throw new Exception("Implement auth flow for discord");
				}
			}
		}

		public async Task SendMessage(ulong channelId, string message)
		{
			_logger.LogInformation($"Sending message to channel {channelId}");
			IMessageChannel channel = await _client.GetChannelAsync(channelId) as IMessageChannel;
			await channel.SendMessageAsync(message);
		}
	}
}