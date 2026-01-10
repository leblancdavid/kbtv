using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;
using KBTV;
using KBTV.Data;
using KBTV.Callers;
using KBTV.Dialogue;

/// <summary>
/// Editor utility to set up the game scene with all required components and assets.
/// Run via menu: KBTV > Setup Game Scene
/// </summary>
public static class GameSetup
{
    private const string DATA_PATH = "Assets/Data";
    private const string VERN_STATS_PATH = "Assets/Data/DefaultVernStats.asset";
    private const string DIALOGUE_PATH = "Assets/Data/Dialogue";
    private const string VERN_DIALOGUE_PATH = "Assets/Data/Dialogue/VernDialogue.asset";

    [MenuItem("KBTV/Setup Game Scene")]
    public static void SetupGameScene()
    {
        Debug.Log("GameSetup: Starting scene setup...");

        // Ensure Data folder exists
        EnsureDirectoryExists(DATA_PATH);

        // Create or find VernStats
        VernStats vernStats = GetOrCreateVernStats();

        // Create dialogue templates
        VernDialogueTemplate vernDialogue = GetOrCreateVernDialogue();
        List<CallerDialogueTemplate> callerDialogues = GetOrCreateCallerDialogues();

        // Find all topics
        Topic[] topics = FindAllAssets<Topic>("Assets/Data/Topics");

        // Find caller modifiers
        StatModifier goodCaller = AssetDatabase.LoadAssetAtPath<StatModifier>("Assets/Data/Events/GoodCaller.asset");
        StatModifier badCaller = AssetDatabase.LoadAssetAtPath<StatModifier>("Assets/Data/Events/BadCaller.asset");
        StatModifier greatCaller = AssetDatabase.LoadAssetAtPath<StatModifier>("Assets/Data/Events/GreatCaller.asset");

        // Find all items
        StatModifier[] items = FindAllAssets<StatModifier>("Assets/Data/Items");

        // Find or create GameBootstrap in scene
        GameBootstrap bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
        if (bootstrap == null)
        {
            GameObject bootstrapObj = new GameObject("GameBootstrap");
            bootstrap = bootstrapObj.AddComponent<GameBootstrap>();
            Debug.Log("GameSetup: Created GameBootstrap GameObject");
        }

        // Configure GameBootstrap via SerializedObject (proper way to set serialized fields)
        SerializedObject serializedBootstrap = new SerializedObject(bootstrap);

        // Set VernStats
        serializedBootstrap.FindProperty("_vernStatsAsset").objectReferenceValue = vernStats;

        // Set tonight's topic (first available, prefer UFOs)
        Topic tonightsTopic = AssetDatabase.LoadAssetAtPath<Topic>("Assets/Data/Topics/UFOs.asset");
        if (tonightsTopic == null && topics.Length > 0)
            tonightsTopic = topics[0];
        serializedBootstrap.FindProperty("_tonightsTopic").objectReferenceValue = tonightsTopic;

        // Set available topics
        SerializedProperty topicsArray = serializedBootstrap.FindProperty("_availableTopics");
        topicsArray.arraySize = topics.Length;
        for (int i = 0; i < topics.Length; i++)
        {
            topicsArray.GetArrayElementAtIndex(i).objectReferenceValue = topics[i];
        }

        // Set caller modifiers
        serializedBootstrap.FindProperty("_goodCallerModifier").objectReferenceValue = goodCaller;
        serializedBootstrap.FindProperty("_badCallerModifier").objectReferenceValue = badCaller;
        serializedBootstrap.FindProperty("_greatCallerModifier").objectReferenceValue = greatCaller;

        // Set available items
        SerializedProperty itemsArray = serializedBootstrap.FindProperty("_availableItems");
        itemsArray.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            itemsArray.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        // Set dialogue templates
        serializedBootstrap.FindProperty("_vernDialogue").objectReferenceValue = vernDialogue;

        SerializedProperty callerDialoguesArray = serializedBootstrap.FindProperty("_callerDialogues");
        callerDialoguesArray.arraySize = callerDialogues.Count;
        for (int i = 0; i < callerDialogues.Count; i++)
        {
            callerDialoguesArray.GetArrayElementAtIndex(i).objectReferenceValue = callerDialogues[i];
        }

        // Enable UI
        serializedBootstrap.FindProperty("_enableLiveShowUI").boolValue = true;
        serializedBootstrap.FindProperty("_enableDebugUI").boolValue = false;
        serializedBootstrap.FindProperty("_enableAudio").boolValue = true;

        // Disable auto-start so PreShow is shown
        serializedBootstrap.FindProperty("_autoStartLiveShow").boolValue = false;

        // Set reasonable show duration for testing (2 minutes)
        serializedBootstrap.FindProperty("_showDuration").floatValue = 120f;

        // Apply changes
        serializedBootstrap.ApplyModifiedProperties();

        // Mark scene dirty so it can be saved
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"GameSetup: Complete! Configured with:");
        Debug.Log($"  - VernStats: {vernStats.name}");
        Debug.Log($"  - Tonight's Topic: {(tonightsTopic != null ? tonightsTopic.name : "None")}");
        Debug.Log($"  - Available Topics: {topics.Length}");
        Debug.Log($"  - Items: {items.Length}");
        Debug.Log($"  - Vern Dialogue: {(vernDialogue != null ? vernDialogue.name : "None")}");
        Debug.Log($"  - Caller Dialogues: {callerDialogues.Count}");
        Debug.Log($"  - Show Duration: 120 seconds");
        Debug.Log("GameSetup: Press Play to start the game!");

        // Select the bootstrap object so user can see it
        Selection.activeGameObject = bootstrap.gameObject;
    }

    [MenuItem("KBTV/Create Missing Assets")]
    public static void CreateMissingAssets()
    {
        EnsureDirectoryExists(DATA_PATH);
        GetOrCreateVernStats();
        Debug.Log("GameSetup: All required assets created!");
    }

    private static VernStats GetOrCreateVernStats()
    {
        // Try to load existing
        VernStats vernStats = AssetDatabase.LoadAssetAtPath<VernStats>(VERN_STATS_PATH);
        
        if (vernStats == null)
        {
            // Create new VernStats asset
            vernStats = ScriptableObject.CreateInstance<VernStats>();
            
            AssetDatabase.CreateAsset(vernStats, VERN_STATS_PATH);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"GameSetup: Created VernStats at {VERN_STATS_PATH}");
        }
        else
        {
            Debug.Log($"GameSetup: Found existing VernStats at {VERN_STATS_PATH}");
        }

        return vernStats;
    }

    private static T[] FindAllAssets<T>(string folder) where T : Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
        T[] assets = new T[guids.Length];
        
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            assets[i] = AssetDatabase.LoadAssetAtPath<T>(path);
        }
        
        return assets;
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path);
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    private static VernDialogueTemplate GetOrCreateVernDialogue()
    {
        EnsureDirectoryExists(DIALOGUE_PATH);

        // Try to load existing
        VernDialogueTemplate vernDialogue = AssetDatabase.LoadAssetAtPath<VernDialogueTemplate>(VERN_DIALOGUE_PATH);

        if (vernDialogue == null)
        {
            // Create new VernDialogue asset
            vernDialogue = ScriptableObject.CreateInstance<VernDialogueTemplate>();

            // Populate with dialogue content from CONVERSATION_DESIGN.md
            PopulateVernDialogue(vernDialogue);

            AssetDatabase.CreateAsset(vernDialogue, VERN_DIALOGUE_PATH);
            AssetDatabase.SaveAssets();

            Debug.Log($"GameSetup: Created VernDialogue at {VERN_DIALOGUE_PATH}");
        }
        else
        {
            Debug.Log($"GameSetup: Found existing VernDialogue at {VERN_DIALOGUE_PATH}");
        }

        return vernDialogue;
    }

    private static void PopulateVernDialogue(VernDialogueTemplate template)
    {
        // Introduction Lines
        template.IntroductionLines = new DialogueTemplate[]
        {
            new DialogueTemplate
            {
                Text = "We've got {callerName} on the line from {location}. What've you got for us tonight?",
                Tone = DialogueTone.Neutral,
                Weight = 1f
            },
            new DialogueTemplate
            {
                Text = "{callerName}, you're on the air. Go ahead.",
                Tone = DialogueTone.Neutral,
                Weight = 1f
            },
            new DialogueTemplate
            {
                Text = "Next up, {callerName} calling in from {location}. Talk to me.",
                Tone = DialogueTone.Neutral,
                Weight = 1f
            }
        };

        // Probing Lines
        template.ProbingLines = new DialogueTemplate[]
        {
            new DialogueTemplate
            {
                Text = "Alright, walk me through this. What exactly did you see?",
                Tone = DialogueTone.Neutral,
                Weight = 1f
            },
            new DialogueTemplate
            {
                Text = "And when did this happen? Give me the details.",
                Tone = DialogueTone.Neutral,
                Weight = 1f
            },
            new DialogueTemplate
            {
                Text = "Hold on, slow down. Start from the beginning.",
                Tone = DialogueTone.Neutral,
                Weight = 1f
            }
        };

        // Skeptical Lines
        template.SkepticalLines = new DialogueTemplate[]
        {
            new DialogueTemplate
            {
                Text = "Now hold on a second, that could've been anything up there.",
                Tone = DialogueTone.Skeptical,
                Weight = 1f
            },
            new DialogueTemplate
            {
                Text = "I gotta be honest with you, I'm not buying it.",
                Tone = DialogueTone.Skeptical,
                Weight = 1f
            },
            new DialogueTemplate
            {
                Text = "You sure it wasn't just a plane? A satellite?",
                Tone = DialogueTone.Skeptical,
                Weight = 1f
            }
        };

        // Dismissive Lines
        template.DismissiveLines = new DialogueTemplate[]
        {
            new DialogueTemplate
            {
                Text = "Yeah, okay. Thanks for calling in.",
                Tone = DialogueTone.Dismissive,
                Weight = 1f
            },
            new DialogueTemplate
            {
                Text = "Right. We're gonna move on to the next caller.",
                Tone = DialogueTone.Dismissive,
                Weight = 1f
            },
            new DialogueTemplate
            {
                Text = "Uh-huh. Appreciate it.",
                Tone = DialogueTone.Dismissive,
                Weight = 1f
            }
        };

        // Believing Lines
        template.BelievingLines = new DialogueTemplate[]
        {
            new DialogueTemplate
            {
                Text = "See, THIS is what I'm talking about, folks.",
                Tone = DialogueTone.Excited,
                Weight = 1f
            },
            new DialogueTemplate
            {
                Text = "Now that's fascinating. I believe you.",
                Tone = DialogueTone.Believing,
                Weight = 1f
            },
            new DialogueTemplate
            {
                Text = "This is exactly the kind of call I live for.",
                Tone = DialogueTone.Excited,
                Weight = 1f
            }
        };

        // Tired Lines
        template.TiredLines = new DialogueTemplate[]
        {
            new DialogueTemplate
            {
                Text = "Mm-hmm. Go on.",
                Tone = DialogueTone.Neutral,
                Weight = 1f
            },
            new DialogueTemplate
            {
                Text = "Alright. What else?",
                Tone = DialogueTone.Neutral,
                Weight = 1f
            },
            new DialogueTemplate
            {
                Text = "Yeah... okay...",
                Tone = DialogueTone.Neutral,
                Weight = 1f
            }
        };

        // Annoyed Lines
        template.AnnoyedLines = new DialogueTemplate[]
        {
            new DialogueTemplate
            {
                Text = "Get to the point, caller.",
                Tone = DialogueTone.Annoyed,
                Weight = 1f
            },
            new DialogueTemplate
            {
                Text = "We don't have all night here.",
                Tone = DialogueTone.Annoyed,
                Weight = 1f
            },
            new DialogueTemplate
            {
                Text = "Is there a point to this story?",
                Tone = DialogueTone.Annoyed,
                Weight = 1f
            }
        };

        // SignOff Lines
        template.SignOffLines = new DialogueTemplate[]
        {
            new DialogueTemplate
            {
                Text = "Appreciate the call. Keep watching the skies.",
                Tone = DialogueTone.Neutral,
                Weight = 1f
            },
            new DialogueTemplate
            {
                Text = "Thanks for sharing that with us tonight.",
                Tone = DialogueTone.Neutral,
                Weight = 1f
            },
            new DialogueTemplate
            {
                Text = "Stay vigilant out there, {callerName}.",
                Tone = DialogueTone.Neutral,
                Weight = 1f
            }
        };
    }

    private static List<CallerDialogueTemplate> GetOrCreateCallerDialogues()
    {
        EnsureDirectoryExists(DIALOGUE_PATH);

        var templates = new List<CallerDialogueTemplate>();

        // Load UFO topic for matching
        Topic ufoTopic = AssetDatabase.LoadAssetAtPath<Topic>("Assets/Data/Topics/UFOs.asset");

        // Create UFO caller templates for each legitimacy level
        templates.Add(GetOrCreateCallerTemplate("UFO_Fake", ufoTopic, CallerLegitimacy.Fake, PopulateUfoFakeDialogue));
        templates.Add(GetOrCreateCallerTemplate("UFO_Questionable", ufoTopic, CallerLegitimacy.Questionable, PopulateUfoQuestionableDialogue));
        templates.Add(GetOrCreateCallerTemplate("UFO_Credible", ufoTopic, CallerLegitimacy.Credible, PopulateUfoCredibleDialogue));
        templates.Add(GetOrCreateCallerTemplate("UFO_Compelling", ufoTopic, CallerLegitimacy.Compelling, PopulateUfoCompellingDialogue));

        // Remove any null entries (in case topic wasn't found)
        templates.RemoveAll(t => t == null);

        Debug.Log($"GameSetup: {templates.Count} CallerDialogue templates ready");
        return templates;
    }

    private static CallerDialogueTemplate GetOrCreateCallerTemplate(
        string assetName,
        Topic topic,
        CallerLegitimacy legitimacy,
        System.Action<CallerDialogueTemplate> populateAction)
    {
        string assetPath = $"{DIALOGUE_PATH}/{assetName}.asset";

        CallerDialogueTemplate template = AssetDatabase.LoadAssetAtPath<CallerDialogueTemplate>(assetPath);

        if (template == null)
        {
            template = ScriptableObject.CreateInstance<CallerDialogueTemplate>();
            template.Topic = topic;
            template.Legitimacy = legitimacy;
            template.Priority = 0;

            populateAction(template);

            AssetDatabase.CreateAsset(template, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"GameSetup: Created {assetName} at {assetPath}");
        }

        return template;
    }

    private static void PopulateUfoFakeDialogue(CallerDialogueTemplate template)
    {
        template.IntroLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Yeah so I totally saw a UFO last night, it was crazy.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Dude, Vern, you're not gonna believe this...", Tone = DialogueTone.Excited, Weight = 1f },
            new DialogueTemplate { Text = "Uh, hi, first time caller... I saw some lights?", Tone = DialogueTone.Nervous, Weight = 1f }
        };

        template.DetailLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "It was like... really bright. And flying around.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "It did a bunch of loops and stuff. Very alien-like.", Tone = DialogueTone.Excited, Weight = 1f },
            new DialogueTemplate { Text = "I didn't get a video but trust me it was wild.", Tone = DialogueTone.Nervous, Weight = 1f }
        };

        template.DefenseLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I mean, I know what I saw, man.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "Why would I lie about this?", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "My buddy saw it too, he's just not here right now.", Tone = DialogueTone.Nervous, Weight = 1f }
        };

        template.AcceptanceLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Uh, yeah, exactly what I was thinking.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "See? Someone gets it.", Tone = DialogueTone.Excited, Weight = 1f },
            new DialogueTemplate { Text = "That's... yeah, that.", Tone = DialogueTone.Confused, Weight = 1f }
        };

        template.ConclusionLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Cool, thanks Vern.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Peace out, keep it real.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Alright, later.", Tone = DialogueTone.Neutral, Weight = 1f }
        };
    }

    private static void PopulateUfoQuestionableDialogue(CallerDialogueTemplate template)
    {
        template.IntroLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I think I might have seen something strange in the sky...", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "I'm not sure what it was, but I had to call.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "This might sound crazy, but hear me out...", Tone = DialogueTone.Nervous, Weight = 1f }
        };

        template.DetailLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "It could have been a plane, but the lights were blinking weird.", Tone = DialogueTone.Confused, Weight = 1f },
            new DialogueTemplate { Text = "It hovered for a bit, then just... moved off.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I was half asleep, but I'm pretty sure I saw something.", Tone = DialogueTone.Confused, Weight = 1f }
        };

        template.DefenseLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I'm not saying it was aliens, but it wasn't normal.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Look, I'm just telling you what I saw.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "I know how this sounds, okay?", Tone = DialogueTone.Nervous, Weight = 1f }
        };

        template.AcceptanceLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "That's what I thought too.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Yeah, maybe you're right.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I hadn't considered that.", Tone = DialogueTone.Confused, Weight = 1f }
        };

        template.ConclusionLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Thanks for hearing me out, Vern.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I just needed to tell someone.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "Okay, thanks. I feel better now.", Tone = DialogueTone.Neutral, Weight = 1f }
        };
    }

    private static void PopulateUfoCredibleDialogue(CallerDialogueTemplate template)
    {
        template.IntroLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Vern, I've been listening for years and I finally have my own sighting to report.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I never thought I'd be making this call, but here I am.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "So I was out stargazing Tuesday night when something appeared that wasn't on any chart.", Tone = DialogueTone.Excited, Weight = 1f }
        };

        template.DetailLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Three amber lights in a triangle formation, completely silent, hovering for about two minutes before shooting off.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "It moved against the wind, no sound, no blinking lights like a plane would have.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I timed it - ninety seconds of hovering, then acceleration that would flatten any pilot.", Tone = DialogueTone.Excited, Weight = 1f }
        };

        template.DefenseLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I've worked at the airport for fifteen years. I know aircraft. This wasn't one.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I'm an amateur astronomer. I know what satellites look like. This was different.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "My whole family saw it. We're not all crazy, Vern.", Tone = DialogueTone.Neutral, Weight = 1f }
        };

        template.AcceptanceLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I knew you'd understand. Nobody else believes me.", Tone = DialogueTone.Excited, Weight = 1f },
            new DialogueTemplate { Text = "Thank god. My wife thinks I've lost it.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "That's exactly what I was hoping to hear.", Tone = DialogueTone.Excited, Weight = 1f }
        };

        template.ConclusionLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Thanks Vern. I'll send you the photos if I can get them developed.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Keep doing what you're doing. People need to hear this stuff.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I'll keep watching and call back if it happens again.", Tone = DialogueTone.Excited, Weight = 1f }
        };
    }

    private static void PopulateUfoCompellingDialogue(CallerDialogueTemplate template)
    {
        template.IntroLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Vern, I'm a retired Air Force pilot, and what I saw last week has kept me up every night since.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "I have documentation, Vern. Radar logs. Witness statements. This is real.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "I was told never to speak of this. But after thirty years, I can't stay silent anymore.", Tone = DialogueTone.Conspiratorial, Weight = 1f }
        };

        template.DetailLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Radar contact at 40,000 feet, no transponder, performed maneuvers that would kill any human pilot. Command told us to forget it happened.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "We tracked it for eighteen minutes. It descended from 80,000 feet to sea level in under four seconds. Nothing we have can do that.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "The craft was disc-shaped, metallic, approximately 40 feet in diameter. I was close enough to see the seams.", Tone = DialogueTone.Neutral, Weight = 1f }
        };

        template.DefenseLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "They can discredit me all they want. I have my flight logs, I have witnesses. This happened.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "I've got nothing to gain from this and everything to lose. Why would I make it up?", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "You think I want to be called crazy? I'm risking my pension for this.", Tone = DialogueTone.Dramatic, Weight = 1f }
        };

        template.AcceptanceLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "That's exactly right. They don't want us talking about this.", Tone = DialogueTone.Conspiratorial, Weight = 1f },
            new DialogueTemplate { Text = "Finally, someone who understands what's at stake here.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "You're one of the few who gets it, Vern.", Tone = DialogueTone.Neutral, Weight = 1f }
        };

        template.ConclusionLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Keep doing what you're doing, Vern. The truth needs to get out.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "I've said my piece. Do with it what you will.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Stay safe, Vern. They're listening.", Tone = DialogueTone.Conspiratorial, Weight = 1f }
        };
    }
}
