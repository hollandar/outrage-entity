namespace Outrage.Entities
{
    class LayerRef<TEntity> : ILayerRef where TEntity : struct
    {
        private readonly int capacityStep;

        public Layer<TEntity> Layer { get; init; }
        HashSet<long> setEntities;

        public LayerRef(int layerCapacity, int capacityStep)
        {
            this.capacityStep = capacityStep;
            this.Layer = new Layer<TEntity>(layerCapacity, capacityStep);
            this.setEntities = new HashSet<long>(capacityStep);
        }

        public IEnumerable<long> SetEntities => setEntities.AsEnumerable();

        public void MarkSet(long entityId, bool set = true)
        {
            if (set)
            {
                this.setEntities.Add(entityId);
            }
            else
            {
                this.setEntities.Remove(entityId);
            }
        }

        public bool IsSet(long entityId)
        {
            return this.setEntities.Contains(entityId);
        }
    }
}