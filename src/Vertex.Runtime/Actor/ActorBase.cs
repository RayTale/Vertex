using Orleans;
using System;
using System.Threading.Tasks;
using Vertex.Abstractions.Actor;

namespace Vertex.Runtime.Actor
{
    public class ActorBase<PrimaryKey> : Grain, IActor<PrimaryKey>
    {
        public ActorBase()
        {
            ActorType = this.GetType();
        }
        /// <summary>
        /// Primary key of actor
        /// Because there are multiple types, dynamic assignment in OnActivateAsync
        /// </summary>
        public PrimaryKey ActorId { get; private set; }
        /// <summary>
        /// The real Type of the current Grain
        /// </summary>
        protected Type ActorType { get; }
        public override Task OnActivateAsync()
        {
            var type = typeof(PrimaryKey);
            if (type == typeof(long) && this.GetPrimaryKeyLong() is PrimaryKey longKey)
                ActorId = longKey;
            else if (type == typeof(string) && this.GetPrimaryKeyString() is PrimaryKey stringKey)
                ActorId = stringKey;
            else if (type == typeof(Guid) && this.GetPrimaryKey() is PrimaryKey guidKey)
                ActorId = guidKey;
            else
                throw new ArgumentOutOfRangeException(typeof(PrimaryKey).FullName);
            return base.OnActivateAsync();
        }
    }
}
