namespace CustomKnightSuperAnimationAddon.Json;

public enum DefType
{
    Simple,
    Advanced
}
public class AnimInfo
{
    public const string FileName = "Info.json";
    public const string TextureName = "Sheet.png";
    public bool useOriginalData = true;
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public tk2dSpriteAnimationClip.WrapMode LoopType = tk2dSpriteAnimationClip.WrapMode.Once;
    public int loopStart = 0;
    public float fps = 15f;
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public DefType InfoType = DefType.Simple;
}