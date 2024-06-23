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

    // Keyed by texture name.
    public Dictionary<string, List<tk2dSpriteCollectionData>> customSpriteCollectionDatas = new();
    public Dictionary<string, tk2dSpriteAnimation> customSpriteAnimations = new();

    // Keyed by game object name, with possible duplicates.
    public Dictionary<string, (tk2dSpriteAnimator, tk2dSpriteAnimation, Tk2dAnimationSkinable)> foundReferences = new();

    public List<Tk2dAnimationSkinable> animationSkinables = [
        new("Knight", ["Knight"]),
        new("VoidSpells", ["Q Mega"]),  // TODO: Other void spells
    ];

    public void Log(object o) => CustomKnightSuperAnimationAddon.instance.Log(o);

    public CustomSkinManager()
    {
        Log("Initializing CustomSkinManager");
        On.tk2dSpriteAnimator.Start += OnSpriteAnimatorStart;
        foreach (var skinable in animationSkinables)
        {
            skinable.customSkinManager = this;
            foreach (var goName in skinable.goNames) { foundReferences.Add(goName, (null, null, skinable)); }
        }
        SearchActive();
        Log("Initialized CustomSkinManager");
        //animationSkinables = SkinManager.Skinables.Values.Where(skinable => skinable is Skinable_Tk2d || skinable is Skinable_Tk2ds).Select(skinable => new Tk2dAnimationSkinnable(skinable.name, this)).ToList();
    }

    public void UpdateSkin(ISelectableSkin current)
    {
        Log("CustomSkinManager - Updating Skin");
        this.current = current;

        DisposeCurrent();
        
        foreach (var pair in foundReferences.Values)
        {
            Log($"CustomSkinManager - Updating {pair.Item3}");
            pair.Item3.Reset();
            if (pair.Item1 && pair.Item3.isSkinned)
                pair.Item1.Library = CreateSpriteAnimationFor(pair.Item3, pair.Item2);
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
        On.tk2dSpriteAnimator.Start -= OnSpriteAnimatorStart;

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

        customSpriteAnimations[skinable.name] = newAnim;
        customSpriteCollectionDatas[skinable.name] = datas;

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

    private void OnSpriteAnimatorStart(On.tk2dSpriteAnimator.orig_Start orig, tk2dSpriteAnimator self)
    {
        orig(self);
        if (foundReferences.TryGetValue(self.name, out var reference) && !reference.Item1)
        {
            reference.Item1 = self;
            reference.Item2 = self.Library;
            foundReferences[self.name] = reference;
            if (reference.Item3.isSkinned)
                self.Library = CreateSpriteAnimationFor(reference.Item3, self.Library);
        }
    }

    public void SearchActive()
    {
        foreach (tk2dSpriteAnimator animator in UObject.FindObjectsOfType<tk2dSpriteAnimator>())
        {
            if (foundReferences.TryGetValue(animator.name, out var reference) && !reference.Item1)
            {
                reference.Item1 = animator;
                reference.Item2 = animator.Library;
                foundReferences[animator.name] = reference;
            }
        }
    }
}