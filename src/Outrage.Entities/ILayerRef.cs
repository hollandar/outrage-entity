namespace Outrage.Entities
{
    internal interface ILayer
    {
        IEnumerable<long> SetEntities { get; }
        bool IsSet(long entityId);
        void MarkUnset(IEnumerable<long> entityId);
        void MarkUnset(long entityId);
        IEnumerable<long> QuerySet();
    }
}
