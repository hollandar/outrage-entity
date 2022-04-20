namespace Outrage.Entities
{
    internal interface ILayerRef
    {
        IEnumerable<long> SetEntities { get; }
        bool IsSet(long entityId);
        void MarkSet(long entityId, bool set = true);
        void MarkSet(IEnumerable<long> entityId, bool set = true);
    }
}
