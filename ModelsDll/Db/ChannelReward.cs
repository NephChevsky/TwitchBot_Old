using ModelsDll.Interfaces;

namespace ModelsDll.Db
{
	public class ChannelReward : ISoftDeleteable
	{
		public Guid Id { get; set; }
		public Guid TwitchId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public bool IsEnabled { get; set; }
		public string BackgroundColor { get; set; }
		public bool UserText { get; set; }
		public int BeginCost { get; set; }
		public int CurrentCost { get; set; }
		public int CostIncreaseAmount { get; set; }
		public int CostDecreaseTimer { get; set; }
		public bool SkipRewardQueue { get; set; }
		public int RedemptionCooldownTime { get; set; }
		public int RedemptionPerStream { get; set; }
		public int RedemptionPerUserPerStream { get; set; }
		public string TriggerType { get; set; }
		public string TriggerValue { get; set; }
		public DateTime LastUsedDateTime { get; set; }

		// ISoftDeleteable
		public bool Deleted { get; set; }
	}
}
