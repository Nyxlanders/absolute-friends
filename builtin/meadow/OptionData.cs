using AbsoluteFriends.Options;
using RainMeadow;

namespace AbsoluteFriends.Meadow;

internal class OptionData : OnlineResource.ResourceData
{
    private static readonly StateCache _options = new(MeadowOptions.Capture);

    public override ResourceDataState MakeState(OnlineResource resource) => new State(resource);

    internal static void ClearCache() => _options.Clear();

    internal class State : ResourceDataState
    {
        [OnlineField]
        public string options = "";

        public State()
        {
        }

        public State(OnlineResource resource)
        {
            options = _options.Value;
        }

        public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
        {
            if (!resource.isOwner)
            {
                MeadowOptions.ApplyHost(options);
            }
        }

        public override Type GetDataType() => typeof(OptionData);
    }
}
