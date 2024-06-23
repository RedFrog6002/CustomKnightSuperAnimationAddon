using Modding;
using CustomKnight;
using System.Reflection;
using UnityEngine;
using System.Collections;

namespace CustomKnightSuperAnimationAddon;

internal class CoroutineHolder : MonoBehaviour
{
    public new void StartCoroutine(IEnumerator enumerator) => base.StartCoroutine(enumerator);
}

public class CustomKnightSuperAnimationAddon : Mod
{
    public static CustomKnightSuperAnimationAddon instance {get; private set;}

    private CoroutineHolder? coroutineHolder;
    public CustomSkinManager customManager;
    private ISelectableSkin lastSkin = null;
    private Type CustomKnightSpriteLoaderType;

    public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    public CustomKnightSuperAnimationAddon() : base("Custom Knight Super-Animation-Addon (TK2D Support)")
    {
        instance = this;
        CustomKnightSpriteLoaderType = Assembly.GetAssembly(typeof(CustomKnight.CustomKnight)).GetType("CustomKnight.SpriteLoader");
    }

    public override void Initialize()
    {
        Log("Initializing");

        customManager = new();
        UpdateSkin(SkinManager.GetCurrentSkin());
        SkinManager.OnSetSkin += OnSetSkin;

        Log("Initialized");
    }

    private CoroutineHolder GetCoroutineHolder()
    {
        if (coroutineHolder == null)
        {
            GameObject obj = new("CoroutineHolder");
            UnityEngine.Object.DontDestroyOnLoad(obj);
            coroutineHolder = obj.AddComponent<CoroutineHolder>();
        }
        return coroutineHolder;
    }

    private void OnSetSkin(object sender, EventArgs e)
    {
        Log("Setting Skin");
        ISelectableSkin skin = SkinManager.GetCurrentSkin();
        if(lastSkin?.GetId() != skin.GetId())
        {
            Log($"Skin Changed:  From: {lastSkin?.GetId()} To: {skin.GetId()}");
            UpdateSkin(skin);
        }
    }

    private void UpdateSkin(ISelectableSkin skin)
    {
        customManager.DisposeCurrent();
        GetCoroutineHolder().StartCoroutine(WaitForSetSkinComplete());
        lastSkin = skin;
    }

    private IEnumerator WaitForSetSkinComplete()
    {
        yield return new WaitUntil(() => ReflectionHelper.GetProperty<bool>(CustomKnightSpriteLoaderType, "LoadComplete"));

        customManager.UpdateSkin(lastSkin);
    }
}
