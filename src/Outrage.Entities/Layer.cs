namespace Outrage.Entities
{
    internal class Layer<TEntity> where TEntity : struct
    {
        int layerCount;
        int capacityStep;
        TEntity[][] layers;

        public Layer(int layerCount = 1000, int capacityStep = 1000)
        {
            this.layerCount = layerCount;
            this.capacityStep = capacityStep;
            layers = new TEntity[layerCount][];
        }

        private (long x, long y) GetIndex(long id)
        {
            var o = Math.DivRem(id, capacityStep);
            if (o.Quotient > layerCount)
                throw new CapacityException();

            return (o.Quotient, o.Remainder);
        }

        public TEntity Get(long entityId)
        {
            var ix = GetIndex(entityId);
            var layer = layers[ix.x];
            if (layer == null)
            {
                layers[ix.x] = layer = new TEntity[this.capacityStep];
            }
            return layer[ix.y];
        }
        
        public void Update(long entityId, UpdateRef<TEntity>? updateAction)
        {
            var ix = GetIndex(entityId);
            var layer = layers[ix.x];
            if (layer == null)
            {
                layers[ix.x] = layer = new TEntity[this.capacityStep];
            }
            if (updateAction != null)
                updateAction(entityId, ref layer[ix.y]);
        }
    }
}
