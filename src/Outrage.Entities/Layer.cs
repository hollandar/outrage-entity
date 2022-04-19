namespace Outrage.Entities
{
    internal class Layer<TProperty> where TProperty : struct
    {
        int layerCount;
        int capacityStep;
        TProperty[][] layers;

        /// <summary>
        /// Construct a layer
        /// </summary>
        /// <param name="layerCount">The number of allocation blocks</param>
        /// <param name="capacityStep">The size of each allocation block</param>
        public Layer(int layerCount = 1000, int capacityStep = 1000)
        {
            this.layerCount = layerCount;
            this.capacityStep = capacityStep;
            layers = new TProperty[layerCount][];
        }

        /// <summary>
        /// Given an entity id, work out what the index in the collection is
        /// </summary>
        /// <param name="id">entity id</param>
        /// <returns>(x,y) index into array set</returns>
        /// <exception cref="CapacityException">The entity ID was larger than the layers support</exception>
        private (long x, long y) GetIndex(long id)
        {
            var o = Math.DivRem(id, capacityStep);
            if (o.Quotient > layerCount)
                throw new CapacityException();

            return (o.Quotient, o.Remainder);
        }

        /// <summary>
        /// Get the value of a property from the layer
        /// </summary>
        /// <param name="entityId">the entity</param>
        /// <returns>entity property</returns>
        public TProperty Get(long entityId)
        {
            var ix = GetIndex(entityId);
            var layer = layers[ix.x];
            if (layer == null)
            {
                layers[ix.x] = layer = new TProperty[this.capacityStep];
            }
            return layer[ix.y];
        }
        
        /// <summary>
        /// Perform an update action against an entity in this layer
        /// </summary>
        /// <param name="entityId">entityId</param>
        /// <param name="updateAction">update action</param>
        public void Update(long entityId, UpdateRef<TProperty>? updateAction)
        {
            var ix = GetIndex(entityId);
            var layer = layers[ix.x];
            if (layer == null)
            {
                layers[ix.x] = layer = new TProperty[this.capacityStep];
            }
            if (updateAction != null)
                updateAction(entityId, ref layer[ix.y]);
        }
    }
}
