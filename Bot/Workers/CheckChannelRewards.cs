using ApiDll;
using ChatDll;
using DbDll;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelsDll;
using ModelsDll.Db;
using SpotifyAPI.Web;
using SpotifyDll;
using TwitchLib.Api;
using TwitchLib.Api.Core.Models.Undocumented.Chatters;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.Channels.GetChannelInformation;
using TwitchLib.Api.Helix.Models.Games;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace Bot.Workers
{
    public class CheckChannelRewards : BackgroundService
    {
        public TwitchAPI api = new TwitchAPI();

        private readonly ILogger<CheckChannelRewards> _logger;
        private readonly Settings _settings;
        private Api _api;
        private Spotify _spotify;

        public CheckChannelRewards(ILogger<CheckChannelRewards> logger, IConfiguration configuration, Spotify spotify)
        {
            _logger = logger;
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _api = new(configuration, false);
            _spotify = spotify;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_settings.CheckUptimeFunction.ComputeUptime)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                using (TwitchDbContext db = new(Guid.Empty))
                {
                    List<ChannelReward> channelRewards = db.ChannelRewards.ToList();
                    List<CustomReward> customRewards = await _api.GetChannelRewards();
                    bool createdRewards = false; 
                    channelRewards.ForEach(async channelReward =>
                    {
                        CustomReward customReward = customRewards.Where(x => x.Title == channelReward.Name).FirstOrDefault();
                        if (customReward == null)
                        {
                            await _api.CreateChannelReward(channelReward);
                            createdRewards = true;
                        }
                    });

                    if (createdRewards)
                    {
                        customRewards = await _api.GetChannelRewards();
                    }
                    
                    channelRewards.ForEach(async channelReward =>
                    {
                        if (channelReward.TriggerType == "game")
                        {
                            CustomReward customReward = customRewards.Where(x => x.Title == channelReward.Name).FirstOrDefault();
                            ChannelInformation channelInfos = await _api.GetChannelInformation();
                            if (string.Equals(channelInfos.GameName, channelReward.TriggerValue,StringComparison.InvariantCultureIgnoreCase) && !customReward.IsEnabled)
                            {
                                await _api.UpdateChannelReward(customReward.Id, channelReward, true);
							}
                            else if (!string.Equals(channelInfos.GameName, channelReward.TriggerValue, StringComparison.InvariantCultureIgnoreCase) && customReward.IsEnabled)
                            {
                                await _api.UpdateChannelReward(customReward.Id, channelReward, false);
                            }
                            else if (customReward.Cost != channelReward.CurrentCost)
                            {
                                await _api.UpdateChannelReward(customReward.Id, channelReward, customReward.IsEnabled);
                            }
                        }
                        else if (channelReward.TriggerType == "game_tag")
                        {
                            CustomReward customReward = customRewards.Where(x => x.Title == channelReward.Name).FirstOrDefault();
                            ChannelInformation channelInfos = await _api.GetChannelInformation();
                            List<string> taggedGames = new List<string>();
                            if (channelReward.TriggerValue == "fps")
                            {
                                taggedGames.AddRange(_settings.Tags.Fps);
							}
                            taggedGames.ForEach(async x => {
                                if (channelInfos.GameName.ToLower().Contains(x.ToLower()) && !customReward.IsEnabled)
                                {
                                    await _api.UpdateChannelReward(customReward.Id, channelReward, true);
                                }
                                else if (!channelInfos.GameName.ToLower().Contains(x.ToLower()) && customReward.IsEnabled)
                                {
                                    await _api.UpdateChannelReward(customReward.Id, channelReward, false);
                                }
                                else if (customReward.Cost != channelReward.CurrentCost)
                                {
                                    await _api.UpdateChannelReward(customReward.Id, channelReward, customReward.IsEnabled);
                                }
                            });
                        }
                        else if (channelReward.TriggerType == "spotify" && channelReward.TriggerValue == "playing")
                        {
                            CustomReward customReward = customRewards.Where(x => x.Title == channelReward.Name).FirstOrDefault();
                            FullTrack track = await _spotify.GetCurrentSong();
                            if (track != null && !customReward.IsEnabled)
                            {
                                await _api.UpdateChannelReward(customReward.Id, channelReward, true);
                            }
                            else if (track == null && customReward.IsEnabled)
                            {
                                await _api.UpdateChannelReward(customReward.Id, channelReward, false);
                            }
                            else if (customReward.Cost != channelReward.CurrentCost)
                            {
                                await _api.UpdateChannelReward(customReward.Id, channelReward, customReward.IsEnabled);
                            }
                        }
                    });
                }

                await Task.Delay(TimeSpan.FromSeconds(_settings.CheckChannelRewardsFunction.Timer), stoppingToken);
            }
        }
    }
}