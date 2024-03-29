﻿using ApiDll;
using DbDll;
using ModelsDll;
using ModelsDll.Db;
using SpotifyAPI.Web;
using SpotifyDll;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.Channels.GetChannelInformation;

namespace WebApp.Services
{
    public class CheckChannelRewards : BackgroundService
    {
        public TwitchAPI api = new TwitchAPI();

        private readonly ILogger<CheckChannelRewards> _logger;
        private readonly Settings _settings;
        private Api _api;
        private Spotify _spotify;

        public CheckChannelRewards(ILogger<CheckChannelRewards> logger, IConfiguration configuration, Api api, Spotify spotify)
        {
            _logger = logger;
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _api = api;
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

                if (await _api.IsStreamerLive())
                {
                    using (TwitchDbContext db = new())
                    {
                        List<ChannelReward> channelRewards = db.ChannelRewards.ToList();
                        List<CustomReward> customRewards = await _api.GetChannelRewards();
                        ChannelInformation channelInfos = await _api.GetChannelInformations();
                        foreach (ChannelReward reward in channelRewards)
                        {
                            bool updateReward = false;
                            CustomReward customReward = customRewards.Where(x => x.Title == reward.Name).FirstOrDefault();
                            if (customReward == null)
                            {
                                customReward = await _api.CreateChannelReward(reward);
                                reward.TwitchId = Guid.Parse(customReward.Id);
                            }
                            if (customReward.Cost != reward.CurrentCost)
                            {
                                updateReward = true;
                            }
                            DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"));
                            if (reward.CurrentCost >= reward.BeginCost + reward.CostIncreaseAmount && reward.LastUsedDateTime < now.AddSeconds(-reward.CostDecreaseTimer))
                            {
                                reward.CurrentCost = reward.CurrentCost - reward.CostIncreaseAmount;
                                reward.LastUsedDateTime = now;
                                updateReward = true;
                            }
                            if (reward.TriggerType == "game")
                            {
                                if ((string.Equals(channelInfos.GameName, reward.TriggerValue, StringComparison.InvariantCultureIgnoreCase) && !customReward.IsEnabled))
                                {
                                    reward.IsEnabled = true;
                                    updateReward = true;
                                }
                                else if (!string.Equals(channelInfos.GameName, reward.TriggerValue, StringComparison.InvariantCultureIgnoreCase) && customReward.IsEnabled)
                                {
                                    reward.IsEnabled = false;
                                    updateReward = true;
                                }
                            }
                            else if (reward.TriggerType == "game_tag")
                            {
                                List<string> taggedGames = new List<string>();
                                if (reward.TriggerValue == "fps")
                                {
                                    taggedGames.AddRange(_settings.Tags.Fps);
                                }
                                bool isPlayingGame = taggedGames.Any(x => channelInfos.GameName.ToLower().Contains(x.ToLower()));
                                if (isPlayingGame && !customReward.IsEnabled)
                                {
                                    reward.IsEnabled = true;
                                    updateReward = true;
                                }
                                else if (!isPlayingGame && customReward.IsEnabled)
                                {
                                    reward.IsEnabled = false;
                                    updateReward = true;
                                }
                            }
                            else if (reward.TriggerType == "spotify" && reward.TriggerValue == "playing")
                            {
                                FullTrack track = await _spotify.GetCurrentSong();
                                if (track != null && !customReward.IsEnabled)
                                {
                                    reward.IsEnabled = true;
                                    updateReward = true;
                                }
                                else if (track == null && customReward.IsEnabled)
                                {
                                    reward.IsEnabled = false;
                                    updateReward = true;
                                }
                            }
                            if (updateReward)
                            {
                                await _api.UpdateChannelReward(reward);
                            }
                        }
                        db.SaveChanges();
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(_settings.CheckChannelRewardsFunction.Timer), stoppingToken);
            }
        }
    }
}