using AbsoluteFriends.Manager;
using AbsoluteFriends.Utils;
using RainMeadow;

namespace AbsoluteFriends.Meadow;

internal class ManagerData : OnlineResource.ResourceData
{
    private static readonly StateCache _manager = new(() => RainWorldUtils.CurrentGame is { } game ? FriendManager.ExportHostState(game) : "");

    public override ResourceDataState MakeState(OnlineResource resource) => new State(resource);

    internal static void ClearCache() => _manager.Clear();

    internal class State : ResourceDataState
    {
        [OnlineField]
        public string manager = "";

        public State()
        {
        }

        public State(OnlineResource resource)
        {
            manager = _manager.Value;
        }

        public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
        {
            if (!resource.isOwner)
            {
                FriendManager.ApplyHostState(manager);
            }
        }

        public override Type GetDataType() => typeof(ManagerData);
    }
}
