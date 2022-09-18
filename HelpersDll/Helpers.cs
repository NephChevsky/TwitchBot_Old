using ApiDll;
using ChatDll;
using DbDll;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ModelsDll.Db;
using SpotifyDll;
using TwitchLib.Api.Core.Enums;

namespace HelpersDll
{
	public static class Helpers
	{

		public static async Task<bool> SkipSong(Spotify spotify, BasicChat chat, string displayName)
		{
			if (!(await spotify.SkipSong()))
			{
				chat.SendMessage($"{displayName} : On écoute pas de musique bouffon");
				return false;
			}
			return true;
		}

		public static async Task<int> AddSong(Spotify spotify, BasicChat chat, string song, string displayName)
		{
			int ret = await spotify.AddSong(song);
			if (ret == 1)
			{
				chat.SendMessage($"{displayName} : La musique a été ajoutée à la playlist");
			}
			else if (ret == 2)
			{
				chat.SendMessage($"{displayName} : La musique est déjà dans la playlist");
			}
			else
			{
				chat.SendMessage($"{displayName} : La musique n'a pas pu être ajoutée à la playlist");
			}
			return ret;
		}

		public static async Task<bool> RemoveSong(Spotify spotify, BasicChat chat, string displayName)
		{
			bool ret = await spotify.RemoveSong();
			if (ret)
			{
				chat.SendMessage($"{displayName} : La musique a été supprimée de la playlist");
			}
			else
			{
				chat.SendMessage($"{displayName} : La musique n'a pas pu être supprimée de la playlist");
			}
			return ret;
		}

		public static async Task ValidateRewardRedemption(Api api, string rewardType, string rewardId, string eventId)
		{
			await api.UpdateRedemptionStatus(rewardId, eventId, CustomRewardRedemptionStatus.FULFILLED);
			using (TwitchDbContext db = new())
			{
				ChannelReward dbReward = db.ChannelRewards.Where(x => x.Name == rewardType).FirstOrDefault();
				if (dbReward != null)
				{
					dbReward.CurrentCost += dbReward.CostIncreaseAmount;
					dbReward.LastUsedDateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time")); ;
					await api.UpdateChannelReward(dbReward);
					db.SaveChanges();
				}
			}
		}

		public static async Task CancelRewardRedemption(Api api, string rewardId, string eventId)
		{
			await api.UpdateRedemptionStatus(rewardId, eventId, CustomRewardRedemptionStatus.CANCELED);
		}
	}
}