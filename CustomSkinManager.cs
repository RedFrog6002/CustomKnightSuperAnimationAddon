using Modding;
using CustomKnight;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UObject = UnityEngine.Object;
using FrogCore.Ext;
using FrogCore;
using Newtonsoft.Json;
using System.ComponentModel.Design.Serialization;
using System.Text;
using CustomKnightSuperAnimationAddon.Json;

namespace CustomKnightSuperAnimationAddon;

public class CustomSkinManager
{
    public ISelectableSkin current;
    public bool populated = false;

    public Dictionary<string, List<tk2dSpriteCollectionData>> customSpriteCollectionDatas = new();
    public Dictionary<string, tk2dSpriteAnimation> customSpriteAnimations = new();
    public Dictionary<int, (tk2dSpriteAnimator, tk2dSpriteAnimation, Tk2dAnimationSkinable)> foundReferences = new();

    public Dictionary<string, Tk2dAnimationSkinable> animationSkinables = new() {
        ["Knight"] = new Tk2dAnimationSkinable("Knight"),
        ["Geo"] = new Tk2dAnimationSkinable("Geo"),
        ["Prompts Cln"] = new Tk2dAnimationSkinable("Prompts Cln"),
        ["HUD Cln"] = new Tk2dAnimationSkinable("HUD Cln")
    };

    public void Log(object o) => CustomKnightSuperAnimationAddon.instance.Log(o); 

    public CustomSkinManager()
    {
        Log("Initializing CustomSkinManager");
        On.tk2dSpriteAnimator.OnEnable += OnSpriteAnimatorOnEnable;
        foreach (var skinable in animationSkinables.Values)
        {
            skinable.customSkinManager = this;
        }
        Log("Initialized CustomSkinManager");
        //animationSkinables = SkinManager.Skinables.Values.Where(skinable => skinable is Skinable_Tk2d || skinable is Skinable_Tk2ds).Select(skinable => new Tk2dAnimationSkinnable(skinable.name, this)).ToList();
    }
    private Tk2dAnimationSkinable GetOrCreateSkinable(string name)
    {
        if(!animationSkinables.TryGetValue(name, out var result))
        {
            result = new(name)
            {
                customSkinManager = this
            };
            result.Reset();
            animationSkinables[name] = result;
        }
        return result;
    }
    public void UpdateSkin(ISelectableSkin current)
    {
        Log("CustomSkinManager - Updating Skin");
        this.current = current;

        DisposeCurrent();

        var skinRoot = Path.GetDirectoryName(current.getSwapperPath());
        foreach(var part in Directory.GetDirectories(skinRoot))
        {
            var skin = GetOrCreateSkinable(Path.GetFileName(part));
            skin.Reset();
        }

        foreach(var v in UObject.FindObjectsOfType<tk2dSpriteAnimator>())
        {
            NewSpriteAnimator(v);
        }

        populated = true;
    }

    public void DisposeCurrent()
    {
        DestroyCollections();
        DestroyAnimations();
        RestoreOriginalReferences();
        customSpriteCollectionDatas.Clear();
        customSpriteAnimations.Clear();

        populated = false;
    }

    public void Destroy()
    {
        On.tk2dSpriteAnimator.OnEnable -= OnSpriteAnimatorOnEnable;
        DisposeCurrent();
    }

    public T ReadFromJson<T>(string path)
    {
        if (current.Exists(path))
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(Encoding.Default.GetString(current.GetFile(path)));
            }
            catch (Exception e)
            {
                Log(e);
            }
        }
        return default;
    }

    public tk2dSpriteAnimation CreateSpriteAnimationFor(Tk2dAnimationSkinable skinable, tk2dSpriteAnimation orig)
    {
        Log($"CustomSkinManager - Creating {skinable.name}");

        tk2dSpriteAnimation newAnim = FrogCore.Utils.CloneTk2dAnimation(orig, "Custom " + orig.name);
        newAnim.gameObject.DontDestroyOnLoad();
        List<tk2dSpriteCollectionData> datas = new();
        
        foreach (string anim in skinable.folderInfo.Animations)
        {
            AnimInfo animInfo = skinable.animInfos[anim];
            Texture2D texture = skinable.textures[anim];

            tk2dSpriteAnimationClip origClip = newAnim.GetClipByName(anim);
            tk2dSpriteAnimationClip newClip = new() {name = anim};
            
            tk2dSpriteCollectionData data = new GameObject("Custom " + orig.name + " - " + anim).AddComponent<tk2dSpriteCollectionData>();
            data.gameObject.DontDestroyOnLoad();
            
            newAnim.clips = newAnim.clips.Where(clip => clip != origClip).Append(newClip).ToArray();
            datas.Add(data);

            if (animInfo.useOriginalData)
            {
                newClip.fps = origClip.fps;
                newClip.loopStart = origClip.loopStart;
                newClip.wrapMode = origClip.wrapMode;
            }
            else
            {
                newClip.fps = animInfo.fps;
                newClip.loopStart = animInfo.loopStart;
                newClip.wrapMode = animInfo.LoopType;
            }

            if (animInfo.InfoType == DefType.Simple)
            {
                SimpleInfo simpleInfo = skinable.simpleInfos[anim];
                
                newClip.frames = new tk2dSpriteAnimationFrame[simpleInfo.frames];
                string[] names = new string[simpleInfo.frames];
                Rect[] rects = new Rect[simpleInfo.frames];
                Vector2[] anchors = new Vector2[simpleInfo.frames];

                float width = texture.width / simpleInfo.columns;
                float height = texture.height / simpleInfo.rows;

                int i = 0;
                for (int y = 0; y < simpleInfo.rows; y++)
                {
                    if (i >= simpleInfo.frames)
                        break;
                    for (int x = 0; x < simpleInfo.columns; x++)
                    {
                        tk2dSpriteAnimationFrame frame = new tk2dSpriteAnimationFrame();
                        if (i < origClip.frames.Length)
                            frame.CopyTriggerFrom(origClip.frames[i]);
                        frame.spriteCollection = data;
                        frame.spriteId = i;
                        newClip.frames[i] = frame;
                        names[i] = "Frame" + i;
                        rects[i] = new Rect(x * width, y * height, width, height);
                        anchors[i] = simpleInfo.anchor;
                        i++;
                        if (i >= simpleInfo.frames)
                            break;
                    }
                }

                Utils.CreateFromTexture(data.gameObject, texture, tk2dSpriteCollectionSize.PixelsPerMeter(64f), new Vector2(texture.width, texture.height), names, rects, null, anchors, new bool[simpleInfo.frames]);
            }
            else
            {
                AdvancedInfo advancedInfo = skinable.advancedInfos[anim];
                
                newClip.frames = new tk2dSpriteAnimationFrame[advancedInfo.frames.Count];
                string[] names = new string[advancedInfo.frames.Count];
                Rect[] rects = new Rect[advancedInfo.frames.Count];
                Vector2[] anchors = new Vector2[advancedInfo.frames.Count];

                for (int i = 0; i < advancedInfo.frames.Count; i++)
                {
                    AdvancedFrameInfo frameInfo = advancedInfo.frames[i];
                    tk2dSpriteAnimationFrame frame = new tk2dSpriteAnimationFrame();
                    if (frameInfo.useOriginalEventData && i < origClip.frames.Length)
                        frame.CopyTriggerFrom(origClip.frames[i]);
                    else
                    {
                        frame.triggerEvent = frameInfo.triggerEvent;
                        frame.eventInfo = frameInfo.eventInfo;
                        frame.eventInt = frameInfo.eventInt;
                        frame.eventFloat = frameInfo.eventFloat;
                    }
                    frame.spriteCollection = data;
                    frame.spriteId = i;
                    newClip.frames[i] = frame;
                    names[i] = "Frame" + i;
                    rects[i] = new Rect(frameInfo.position, frameInfo.size);
                    anchors[i] = frameInfo.anchor;
                }

                Utils.CreateFromTexture(data.gameObject, texture, tk2dSpriteCollectionSize.PixelsPerMeter(64f), new Vector2(texture.width, texture.height), names, rects, null, anchors, new bool[advancedInfo.frames.Count]);
            }
        }

        customSpriteAnimations[skinable.goName] = newAnim;
        customSpriteCollectionDatas[skinable.goName] = datas;

        Log($"CustomSkinManager - Finished Creating {skinable.name}");

        return newAnim;
    }

    public void DestroyCollections()
    {
        foreach (var data in customSpriteCollectionDatas.SelectMany(pair => pair.Value)) {GameObject.Destroy(data.gameObject);}
    }

    public void DestroyAnimations()
    {
        foreach (var anim in customSpriteAnimations.Values) {GameObject.Destroy(anim.gameObject);}
    }

    public void RestoreOriginalReferences()
    {
        foreach (var pair in foundReferences.Values) { if (pair.Item1) pair.Item1.Library = pair.Item2; }
    }

    private void NewSpriteAnimator(tk2dSpriteAnimator anim)
    {
        if(!foundReferences.TryGetValue(anim.GetInstanceID(), out var reference))
        {
            reference.Item1 = anim;
            reference.Item2 = anim.Library;
            if (anim.Library == null) return;
            reference.Item3 = GetOrCreateSkinable(anim.Library.name);
            foundReferences[anim.GetInstanceID()] = reference;
        }
        if (reference.Item3.isSkinned)
        {
            if (!customSpriteAnimations.TryGetValue(reference.Item2.name, out var animOverride))
            {
                animOverride = CreateSpriteAnimationFor(reference.Item3, reference.Item2);
            }
            anim.Library = animOverride;
        }
    }

    private void OnSpriteAnimatorOnEnable(On.tk2dSpriteAnimator.orig_OnEnable orig, tk2dSpriteAnimator self)
    {
        orig(self);
        NewSpriteAnimator(self);
    }
}