namespace AbsoluteFriends.ChainedTogether;

internal class ChainStyle(string key, Func<bool> isAvailable, Func<Chain, ChainGraphic> create)
{
    public const string DefaultKey = "Void";

    public static readonly ChainStyle[] All =
    [
        new(DefaultKey, static () => true, static chain => new ChainGraphic(chain)),
        new("Rubicon", static () => ModManager.MSC, static chain => new RubiconGraphic(chain)),
        new("Weaver", static () => ModManager.Watcher, static chain => new WeaverGraphic(chain)),
        new("Ripple", static () => ModManager.Watcher, static chain => new RippleGraphic(chain))
    ];

    public static IEnumerable<ChainStyle> Available => All.Where(style => style.IsAvailable);

    public static ChainStyle Current => All.FirstOrDefault(style => style.Key == Config.Style.Value && style.IsAvailable) ?? All[0];

    public string Key => key;

    public bool IsAvailable => isAvailable();

    public ChainGraphic Create(Chain chain) => create(chain);
}
