using Modding;
using CustomKnight;
using System.Reflection;
using UnityEngine;
using System.Collections;

namespace CustomKnightSuperAnimationAddon;

public class CustomKnightSuperAnimationAddon : Mod
{
    public static CustomKnightSuperAnimationAddon instance {get; private set;}

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
        SkinManager.OnSetSkin += OnSetSkin;
        CustomKnight.CustomKnight.OnReady += OnSetSkin;

        Log("Initialized");
    }

    private void OnSetSkin(object sender, EventArgs e)
    {
        Log("Setting Skin");
        ISelectableSkin skin = SkinManager.GetCurrentSkin();
        if(lastSkin?.GetId() != skin.GetId())
        {
            Log($"Skin Changed:  From: {lastSkin?.GetId()} To: {skin.GetId()}");
            customManager.DisposeCurrent();
            GameManager.instance.StartCoroutine(WaitForSetSkinComplete());
            lastSkin = skin;
        }
    }

    private IEnumerator WaitForSetSkinComplete()
    {
        yield return new WaitUntil(() => ReflectionHelper.GetProperty<bool>(CustomKnightSpriteLoaderType, "LoadComplete"));

        customManager.UpdateSkin(lastSkin);
    }
}
