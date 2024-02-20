using System.Security.Policy;
using UnityEngine;
using CustomKnightSuperAnimationAddon.Json;

namespace CustomKnightSuperAnimationAddon;

public class Tk2dAnimationSkinable
{
    public string name;
    public string goName;
    public CustomSkinManager customSkinManager;

    public bool isSkinned = false;

    public FolderInfo folderInfo;
    public Dictionary<string, Texture2D> textures = new();
    public Dictionary<string, AnimInfo> animInfos = new();
    public Dictionary<string, SimpleInfo> simpleInfos = new();
    public Dictionary<string, AdvancedInfo> advancedInfos = new();

    public void Log(object o) => CustomKnightSuperAnimationAddon.instance.Log(o);

    public Tk2dAnimationSkinable(string name)
    {
        this.name = name;
    }

    public void Reset()
    {
        Log($"Skinable {name} - Updating Skin");

        textures.Clear();
        animInfos.Clear();
        simpleInfos.Clear();
        advancedInfos.Clear();

        isSkinned = customSkinManager.current.Exists($"{name}/{FolderInfo.FileName}");
        Log($"Skinable {name}: isSkinned = {isSkinned}");
        
        if (!isSkinned)
            return;

        folderInfo = customSkinManager.ReadFromJson<FolderInfo>($"{name}/{FolderInfo.FileName}");

        foreach (string anim in folderInfo.Animations)
        {
            if (!customSkinManager.current.Exists($"{name}/{anim}/{AnimInfo.TextureName}"))
                continue;

            Texture2D texture = customSkinManager.current.GetTexture($"{name}/{anim}/{AnimInfo.TextureName}");

            if (!customSkinManager.current.Exists($"{name}/{anim}/{AnimInfo.FileName}"))
                continue;

            AnimInfo animInfo = customSkinManager.ReadFromJson<AnimInfo>($"{name}/{anim}/{AnimInfo.FileName}");

            if (animInfo.InfoType == DefType.Simple)
            {
                if (!customSkinManager.current.Exists($"{name}/{anim}/{SimpleInfo.FileName}"))
                    continue;

                simpleInfos[anim] = customSkinManager.ReadFromJson<SimpleInfo>($"{name}/{anim}/{SimpleInfo.FileName}");
            }
            else
            {
                if (!customSkinManager.current.Exists($"{name}/{anim}/{SimpleInfo.FileName}"))
                    continue;

                advancedInfos[anim] = customSkinManager.ReadFromJson<AdvancedInfo>($"{name}/{anim}/{AdvancedInfo.FileName}");
            }
            textures[anim] = texture;
            animInfos[anim] = animInfo;
        }
    }
}
