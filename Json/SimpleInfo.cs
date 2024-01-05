namespace CustomKnightSuperAnimationAddon.Json;

public class SimpleInfo
{
    public const string FileName = "Simple.json";
    public int columns = 1;
    public int rows = 1;
    public int frames = 1;
    [Newtonsoft.Json.JsonConverter(typeof(Modding.Converters.Vector2Converter))]
    public UnityEngine.Vector2 anchor = new UnityEngine.Vector2(0f, 0f);
}