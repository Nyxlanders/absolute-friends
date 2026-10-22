using AbsoluteFriends.Utils;
using RainMeadow;

namespace AbsoluteFriends.Meadow;

internal class ValueData : OnlineEntity.EntityData
{
    public override EntityDataState MakeState(OnlineEntity onlineEntity, OnlineResource inResource) => new State(onlineEntity);

    internal class State : EntityDataState
    {
        [OnlineField]
        public string[] keys = [];

        [OnlineField]
        public int[] values = [];

        public State()
        {
        }

        public State(OnlineEntity onlineEntity)
        {
            if (onlineEntity is OnlinePhysicalObject { apo: { } apo })
            {
                (string Key, int Value)[] synced = [.. apo.SyncedValues];

                keys = [.. synced.Select(value => value.Key)];
                values = [.. synced.Select(value => value.Value)];
            }
        }

        public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
        {
            if (onlineEntity is not OnlinePhysicalObject { apo: { } apo })
            {
                return;
            }

            for (int i = 0; i < Math.Min(keys.Length, values.Length); i++)
            {
                apo.ReceiveSyncedValue(keys[i], values[i]);
            }
        }

        public override Type GetDataType() => typeof(ValueData);
    }
}
