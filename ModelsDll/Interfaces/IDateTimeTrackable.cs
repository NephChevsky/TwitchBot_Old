namespace ModelsDll.Interfaces
{
    public interface IDateTimeTrackable
    {
        public DateTime CreationDateTime { get; set; }
        public DateTime LastModificationDateTime { get; set; }
    }
}
