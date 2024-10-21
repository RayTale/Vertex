using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vertex.Abstractions.Actor;

namespace Vertex.Runtime.Actor
{
    public class ActorBase<TPrimaryKey> : Grain, IActor<TPrimaryKey>
    {
        public ActorBase()
        {
            this.ActorType = this.GetType();
        }

        /// <summary>
        /// Gets primary key of actor
        /// Because there are multiple types, dynamic assignment in OnActivateAsync.
        /// </summary>
        public TPrimaryKey ActorId { get; private set; }

        /// <summary>
        /// Gets the real Type of the current Grain.
        /// </summary>
        protected Type ActorType { get; }

        public Task OnActivateAsync()
        {
            return this.OnActivateAsync(CancellationToken.None);
        }

        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            var type = typeof(TPrimaryKey);
            if (type == typeof(long) && this.GetPrimaryKeyLong() is TPrimaryKey longKey)
            {
                this.ActorId = longKey;
            }
            else if (type == typeof(string) && this.GetPrimaryKeyString() is TPrimaryKey stringKey)
            {
                this.ActorId = stringKey;
            }
            else if (type == typeof(Guid) && this.GetPrimaryKey() is TPrimaryKey guidKey)
            {
                this.ActorId = guidKey;
            }
            else
            {
                throw new ArgumentOutOfRangeException(typeof(TPrimaryKey).FullName);
            }

            return base.OnActivateAsync(cancellationToken);
        }
    }
}
