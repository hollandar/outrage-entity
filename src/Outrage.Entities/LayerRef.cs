namespace Outrage.Entities
{
    class LayerRef<TProperty> : ILayerRef where TProperty : struct
    {
        private readonly int capacityStep;

        public Layer<TProperty> Layer { get; init; }
        HashSet<long> setEntities;

        /// <summary>
        /// Construct a reference to a storage object and handle set recording for the layer
        /// </summary>
        /// <param name="layerCapacity"></param>
        /// <param name="capacityStep"></param>
        public LayerRef(int layerCapacity, int capacityStep)
        {
            this.capacityStep = capacityStep;
            this.Layer = new Layer<TProperty>(layerCapacity, capacityStep);
            this.setEntities = new HashSet<long>(capacityStep);
        }

        public IEnumerable<long> SetEntities => setEntities;

        /// <summary>
        /// Mark the property in this layer as being set
        /// </summary>
        /// <param name="entityId">Entity id to mark</param>
        /// <param name="set">Mark as set (default) or unset</param>
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

        /// <summary>
        /// Mark properties in this layer as being set
        /// </summary>
        /// <param name="entityIds">Entity ids to mark</param>
        /// <param name="set">Mark as set (default) or unset</param>
        public void MarkSet(IEnumerable<long> entityIds, bool set = true)
        {
            if (set)
            {
                foreach (var entityId in entityIds)
                    this.setEntities.Add(entityId);
            }
            else
            {
                foreach (var entityId in entityIds)
                    this.setEntities.Remove(entityId);
            }
        }

        /// <summary>
        /// Test if a property is set for an entity
        /// </summary>
        /// <param name="entityId">entity id</param>
        /// <returns>true if the property is marked as set</returns>
        public bool IsSet(long entityId)
        {
            return this.setEntities.Contains(entityId);
        }
    }
}