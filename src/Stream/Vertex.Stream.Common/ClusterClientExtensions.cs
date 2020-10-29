using System;
using Orleans;

namespace Vertex.Stream.Common
{
    public static class ClusterClientExtensions
    {
        public static TGrainInterface GetGrain<TGrainInterface>(IGrainFactory client, Guid primaryKey, string grainClassNamePrefix = null)
            where TGrainInterface : IGrainWithGuidKey
        {
            return client.GetGrain<TGrainInterface>(primaryKey, grainClassNamePrefix);
        }

        public static TGrainInterface GetGrain<TGrainInterface>(IGrainFactory client, long primaryKey, string grainClassNamePrefix = null)
            where TGrainInterface : IGrainWithIntegerKey
        {
            return client.GetGrain<TGrainInterface>(primaryKey, grainClassNamePrefix);
        }

        public static TGrainInterface GetGrain<TGrainInterface>(IGrainFactory client, string primaryKey, string grainClassNamePrefix = null)
            where TGrainInterface : IGrainWithStringKey
        {
            return client.GetGrain<TGrainInterface>(primaryKey, grainClassNamePrefix);
        }

        public static TGrainInterface GetGrain<TGrainInterface>(IGrainFactory client, Guid primaryKey, string keyExtension, string grainClassNamePrefix = null)
            where TGrainInterface : IGrainWithGuidCompoundKey
        {
            return client.GetGrain<TGrainInterface>(primaryKey, keyExtension, grainClassNamePrefix);
        }

        public static TGrainInterface GetGrain<TGrainInterface>(IGrainFactory client, long primaryKey, string keyExtension, string grainClassNamePrefix = null)
            where TGrainInterface : IGrainWithIntegerCompoundKey
        {
            return client.GetGrain<TGrainInterface>(primaryKey, keyExtension, grainClassNamePrefix);
        }
    }
}
