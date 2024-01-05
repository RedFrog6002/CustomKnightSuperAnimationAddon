namespace CustomKnightSuperAnimationAddon.Json;

public class AdvancedInfo
{
    public const string FileName = "Advanced.json";
    public List<AdvancedFrameInfo> frames = new();
}

public class AdvancedFrameInfo
{
    [Newtonsoft.Json.JsonConverter(typeof(Modding.Converters.Vector2Converter))]
    public UnityEngine.Vector2 position = new UnityEngine.Vector2(0f, 0f);
    [Newtonsoft.Json.JsonConverter(typeof(Modding.Converters.Vector2Converter))]
    public UnityEngine.Vector2 size = new UnityEngine.Vector2(0f, 0f);
    [Newtonsoft.Json.JsonConverter(typeof(Modding.Converters.Vector2Converter))]
    public UnityEngine.Vector2 anchor = new UnityEngine.Vector2(0f, 0f);

    public bool useOriginalEventData = true;

    public bool triggerEvent = false;
    public string eventInfo = "";
    public int eventInt = 0;
    public float eventFloat = 0;
}