using ApiDll;
using DbDll;
using ModelsDll.Db;
using TwitchLib.Api.Core.Enums;

namespace HelpersDll
{
	public static class Helpers
	{
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