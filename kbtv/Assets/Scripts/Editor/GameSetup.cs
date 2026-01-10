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

        // Load topics for matching
        Topic ufoTopic = AssetDatabase.LoadAssetAtPath<Topic>("Assets/Data/Topics/UFOs.asset");
        Topic cryptidsTopic = AssetDatabase.LoadAssetAtPath<Topic>("Assets/Data/Topics/Cryptids.asset");
        Topic ghostsTopic = AssetDatabase.LoadAssetAtPath<Topic>("Assets/Data/Topics/GhostsAndHauntings.asset");
        Topic conspiraciesTopic = AssetDatabase.LoadAssetAtPath<Topic>("Assets/Data/Topics/GovernmentConspiracies.asset");

        // Create UFO caller templates for each legitimacy level
        templates.Add(GetOrCreateCallerTemplate("UFO_Fake", ufoTopic, CallerLegitimacy.Fake, PopulateUfoFakeDialogue));
        templates.Add(GetOrCreateCallerTemplate("UFO_Questionable", ufoTopic, CallerLegitimacy.Questionable, PopulateUfoQuestionableDialogue));
        templates.Add(GetOrCreateCallerTemplate("UFO_Credible", ufoTopic, CallerLegitimacy.Credible, PopulateUfoCredibleDialogue));
        templates.Add(GetOrCreateCallerTemplate("UFO_Compelling", ufoTopic, CallerLegitimacy.Compelling, PopulateUfoCompellingDialogue));

        // Create Cryptids caller templates for each legitimacy level
        templates.Add(GetOrCreateCallerTemplate("Cryptids_Fake", cryptidsTopic, CallerLegitimacy.Fake, PopulateCryptidsFakeDialogue));
        templates.Add(GetOrCreateCallerTemplate("Cryptids_Questionable", cryptidsTopic, CallerLegitimacy.Questionable, PopulateCryptidsQuestionableDialogue));
        templates.Add(GetOrCreateCallerTemplate("Cryptids_Credible", cryptidsTopic, CallerLegitimacy.Credible, PopulateCryptidsCredibleDialogue));
        templates.Add(GetOrCreateCallerTemplate("Cryptids_Compelling", cryptidsTopic, CallerLegitimacy.Compelling, PopulateCryptidsCompellingDialogue));

        // Create Ghosts caller templates for each legitimacy level
        templates.Add(GetOrCreateCallerTemplate("Ghosts_Fake", ghostsTopic, CallerLegitimacy.Fake, PopulateGhostsFakeDialogue));
        templates.Add(GetOrCreateCallerTemplate("Ghosts_Questionable", ghostsTopic, CallerLegitimacy.Questionable, PopulateGhostsQuestionableDialogue));
        templates.Add(GetOrCreateCallerTemplate("Ghosts_Credible", ghostsTopic, CallerLegitimacy.Credible, PopulateGhostsCredibleDialogue));
        templates.Add(GetOrCreateCallerTemplate("Ghosts_Compelling", ghostsTopic, CallerLegitimacy.Compelling, PopulateGhostsCompellingDialogue));

        // Create Conspiracies caller templates for each legitimacy level
        templates.Add(GetOrCreateCallerTemplate("Conspiracies_Fake", conspiraciesTopic, CallerLegitimacy.Fake, PopulateConspiraciesFakeDialogue));
        templates.Add(GetOrCreateCallerTemplate("Conspiracies_Questionable", conspiraciesTopic, CallerLegitimacy.Questionable, PopulateConspiraciesQuestionableDialogue));
        templates.Add(GetOrCreateCallerTemplate("Conspiracies_Credible", conspiraciesTopic, CallerLegitimacy.Credible, PopulateConspiraciesCredibleDialogue));
        templates.Add(GetOrCreateCallerTemplate("Conspiracies_Compelling", conspiraciesTopic, CallerLegitimacy.Compelling, PopulateConspiraciesCompellingDialogue));

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

    // ========== CRYPTIDS DIALOGUE ==========

    private static void PopulateCryptidsFakeDialogue(CallerDialogueTemplate template)
    {
        template.IntroLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Yo Vern, I saw Bigfoot in my backyard last night!", Tone = DialogueTone.Excited, Weight = 1f },
            new DialogueTemplate { Text = "Dude, there's like, a creature living in the woods behind my house.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "So my buddy told me to call because I definitely saw something hairy.", Tone = DialogueTone.Nervous, Weight = 1f }
        };

        template.DetailLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "It was huge, like eight feet tall. Or maybe six. It was dark.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "It made this noise, like a howl but also kind of like a scream?", Tone = DialogueTone.Excited, Weight = 1f },
            new DialogueTemplate { Text = "I saw tracks too but my dog ran through them before I could get a picture.", Tone = DialogueTone.Nervous, Weight = 1f }
        };

        template.DefenseLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "It wasn't a bear, man. Bears don't walk on two legs... do they?", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "I know what I saw! Probably.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "My cousin saw it too but he's not here right now.", Tone = DialogueTone.Nervous, Weight = 1f }
        };

        template.AcceptanceLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Yeah, exactly! That's what I was thinking.", Tone = DialogueTone.Excited, Weight = 1f },
            new DialogueTemplate { Text = "See? Vern gets it.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Uh, yeah, what you said.", Tone = DialogueTone.Confused, Weight = 1f }
        };

        template.ConclusionLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Thanks Vern. I'm gonna set up a trail cam.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Alright, peace. Keep it weird.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Cool, later.", Tone = DialogueTone.Neutral, Weight = 1f }
        };
    }

    private static void PopulateCryptidsQuestionableDialogue(CallerDialogueTemplate template)
    {
        template.IntroLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I think I might have seen something in the woods last week...", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "I'm not sure what it was, but it definitely wasn't a normal animal.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "This is gonna sound weird, but I heard something I can't explain.", Tone = DialogueTone.Nervous, Weight = 1f }
        };

        template.DetailLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "It was moving through the trees, maybe on two legs? Hard to tell in the dark.", Tone = DialogueTone.Confused, Weight = 1f },
            new DialogueTemplate { Text = "The smell was awful. Like rotting meat mixed with sulfur.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I found these tracks, but they could be anything I guess.", Tone = DialogueTone.Confused, Weight = 1f }
        };

        template.DefenseLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I'm not saying it was Bigfoot, but it wasn't a bear either.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Look, I've lived here thirty years. I know what's in these woods.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "I know how it sounds, but I had to tell someone.", Tone = DialogueTone.Nervous, Weight = 1f }
        };

        template.AcceptanceLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "That's what my neighbor said too.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Maybe I should look into it more.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I guess that makes sense.", Tone = DialogueTone.Confused, Weight = 1f }
        };

        template.ConclusionLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Thanks for not laughing at me, Vern.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I just needed to get that off my chest.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "I'll call back if I see it again.", Tone = DialogueTone.Neutral, Weight = 1f }
        };
    }

    private static void PopulateCryptidsCredibleDialogue(CallerDialogueTemplate template)
    {
        template.IntroLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Vern, I'm a wildlife biologist, and I saw something last month that doesn't fit any known species.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I've been tracking unusual sightings in my area for three years now. The pattern is undeniable.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I was doing fieldwork near the river when I encountered something I can't explain.", Tone = DialogueTone.Excited, Weight = 1f }
        };

        template.DetailLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Bipedal, approximately seven feet tall, covered in dark hair. I observed it for nearly two minutes before it moved into the tree line.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "The footprints measured eighteen inches. The stride length suggests something weighing over four hundred pounds.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I have hair samples. Sent them to a lab - they came back 'unknown primate.' No match in any database.", Tone = DialogueTone.Excited, Weight = 1f }
        };

        template.DefenseLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I've studied bears, elk, moose - this wasn't any of those. The anatomy was all wrong.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I have photographs, plaster casts of the prints, and GPS coordinates. This is documented.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Three other researchers have had similar encounters in the same area. We can't all be mistaken.", Tone = DialogueTone.Neutral, Weight = 1f }
        };

        template.AcceptanceLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Thank you. The scientific community won't touch this, but someone needs to.", Tone = DialogueTone.Excited, Weight = 1f },
            new DialogueTemplate { Text = "That's exactly why I called you. Mainstream media won't cover it.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I knew your listeners would understand.", Tone = DialogueTone.Excited, Weight = 1f }
        };

        template.ConclusionLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I'll send you the lab results, Vern. People need to see this.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Keep shining a light on these things. Science should be open-minded.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I'm heading back out there next week. I'll report anything new.", Tone = DialogueTone.Excited, Weight = 1f }
        };
    }

    private static void PopulateCryptidsCompellingDialogue(CallerDialogueTemplate template)
    {
        template.IntroLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Vern, I was a park ranger for twenty-five years. What I'm about to tell you ended my career.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "I have footage, Vern. Clear footage. And I've been threatened to keep quiet about it.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "The government knows these creatures exist. I've seen the internal memos.", Tone = DialogueTone.Conspiratorial, Weight = 1f }
        };

        template.DetailLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "We found a body. Not human, not any known animal. Command came in, locked down the area, took everything. Told us it never happened.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "I tracked a family group for six months. Multiple individuals, different sizes. They have a social structure, Vern. They're intelligent.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "The creature looked at me. Not like an animal looks at you - it understood. It knew I was there to observe, and it let me.", Tone = DialogueTone.Neutral, Weight = 1f }
        };

        template.DefenseLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "They took my pension, Vern. You think I'd give that up for a hoax?", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "I have copies of everything. Hidden. They can discredit me, but they can't erase the evidence.", Tone = DialogueTone.Conspiratorial, Weight = 1f },
            new DialogueTemplate { Text = "Two other rangers saw it with me. One recanted after the visit from the suits. The other disappeared.", Tone = DialogueTone.Dramatic, Weight = 1f }
        };

        template.AcceptanceLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "That's why I called you. You're the only one who'll let me tell this story.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "The cover-up goes deep, Vern. Forest Service, Fish and Wildlife - they all know.", Tone = DialogueTone.Conspiratorial, Weight = 1f },
            new DialogueTemplate { Text = "Finally. Someone who takes this seriously.", Tone = DialogueTone.Neutral, Weight = 1f }
        };

        template.ConclusionLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Get the word out, Vern. Before they bury this forever.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "I've said what I needed to say. Watch your back.", Tone = DialogueTone.Conspiratorial, Weight = 1f },
            new DialogueTemplate { Text = "Keep fighting the good fight. The truth matters.", Tone = DialogueTone.Neutral, Weight = 1f }
        };
    }

    // ========== GHOSTS DIALOGUE ==========

    private static void PopulateGhostsFakeDialogue(CallerDialogueTemplate template)
    {
        template.IntroLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Vern, my house is totally haunted!", Tone = DialogueTone.Excited, Weight = 1f },
            new DialogueTemplate { Text = "Dude, I saw a ghost last night. For real.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "So like, things keep moving around in my apartment...", Tone = DialogueTone.Nervous, Weight = 1f }
        };
        template.DetailLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "The TV turned on by itself. Twice! Well, once might have been the cat.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I felt this cold spot. Super creepy. Might've been the AC though.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "I heard footsteps upstairs but nobody was there. Probably.", Tone = DialogueTone.Excited, Weight = 1f }
        };
        template.DefenseLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I'm not making this up! Weird stuff happens all the time.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "My roommate heard it too. He's just not here to confirm.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "Why would I lie about being haunted?", Tone = DialogueTone.Nervous, Weight = 1f }
        };
        template.AcceptanceLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Yeah! See? I knew it was real.", Tone = DialogueTone.Excited, Weight = 1f },
            new DialogueTemplate { Text = "That's what I figured too.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Uh, yeah, exactly.", Tone = DialogueTone.Confused, Weight = 1f }
        };
        template.ConclusionLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Thanks Vern. I'm gonna get a ouija board.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Cool, peace out.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Later, dude.", Tone = DialogueTone.Neutral, Weight = 1f }
        };
    }

    private static void PopulateGhostsQuestionableDialogue(CallerDialogueTemplate template)
    {
        template.IntroLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I think there might be something in my grandmother's old house...", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "I'm not sure if I believe in ghosts, but something strange is happening.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "Ever since we moved in, things have felt... off.", Tone = DialogueTone.Nervous, Weight = 1f }
        };
        template.DetailLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Doors close on their own. Could be drafts, but it feels intentional.", Tone = DialogueTone.Confused, Weight = 1f },
            new DialogueTemplate { Text = "I hear whispers at night. Might be pipes, might be something else.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "My dog stares at the corner and growls. Animals sense things, right?", Tone = DialogueTone.Confused, Weight = 1f }
        };
        template.DefenseLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I've checked the house for drafts, for animals. Nothing explains it.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I'm not a believer, Vern. But I can't explain what I'm experiencing.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "Multiple family members have noticed it. We can't all be imagining things.", Tone = DialogueTone.Nervous, Weight = 1f }
        };
        template.AcceptanceLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "That's a relief to hear. I thought I was losing my mind.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Maybe I should look into the house's history.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I hadn't thought of it that way.", Tone = DialogueTone.Confused, Weight = 1f }
        };
        template.ConclusionLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Thanks for listening, Vern. I needed to tell someone.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I'll keep you updated if anything else happens.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "Appreciate it. Take care.", Tone = DialogueTone.Neutral, Weight = 1f }
        };
    }

    private static void PopulateGhostsCredibleDialogue(CallerDialogueTemplate template)
    {
        template.IntroLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Vern, I've been a paranormal investigator for fifteen years. This case is different.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I'm a skeptic by nature, but what I documented last month changed my mind.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I work at a historic hotel. The things I've witnessed there are undeniable.", Tone = DialogueTone.Excited, Weight = 1f }
        };
        template.DetailLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "EMF readings spiked to levels that shouldn't be possible. No electrical source anywhere nearby.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I captured a full-bodied apparition on thermal imaging. It maintained form for thirty seconds.", Tone = DialogueTone.Excited, Weight = 1f },
            new DialogueTemplate { Text = "EVP sessions produced clear responses. Direct answers to specific questions. Not random noise.", Tone = DialogueTone.Neutral, Weight = 1f }
        };
        template.DefenseLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I've ruled out every conventional explanation. Checked the wiring, the pipes, everything.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Three independent investigators verified my findings. Same results.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I have hours of footage. This isn't pareidolia or wishful thinking.", Tone = DialogueTone.Neutral, Weight = 1f }
        };
        template.AcceptanceLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Thank you. Most people dismiss this without even looking at the evidence.", Tone = DialogueTone.Excited, Weight = 1f },
            new DialogueTemplate { Text = "That means a lot. This work isn't easy when no one takes you seriously.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Exactly. The evidence speaks for itself.", Tone = DialogueTone.Excited, Weight = 1f }
        };
        template.ConclusionLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I'll send you the footage, Vern. Your listeners should see this.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Keep doing what you do. We need more open minds out there.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I'll be back with more findings. This investigation is ongoing.", Tone = DialogueTone.Excited, Weight = 1f }
        };
    }

    private static void PopulateGhostsCompellingDialogue(CallerDialogueTemplate template)
    {
        template.IntroLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Vern, I'm a retired mortician. I've worked with the dead my whole life. They're not always gone.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "I have recordings of the deceased communicating. Names, dates, information I couldn't have known.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "What I'm about to tell you got me fired from my hospital job. They said I was scaring patients.", Tone = DialogueTone.Conspiratorial, Weight = 1f }
        };
        template.DetailLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "A woman appeared at the foot of bed 4 every night at 3 AM. Same woman. She died in that room in 1952. I found her photo.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "The spirit told me where to find a letter hidden in the walls. It was exactly where she said. Dated 1923.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "Multiple patients reported seeing the same figure. Patients who never spoke to each other. Same description every time.", Tone = DialogueTone.Neutral, Weight = 1f }
        };
        template.DefenseLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I didn't believe it myself until I found physical proof. You can't fake a letter from 1923.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "The hospital covered it up. They didn't want bad press. But the staff knows. They all know.", Tone = DialogueTone.Conspiratorial, Weight = 1f },
            new DialogueTemplate { Text = "I've got nothing to gain from this. I lost my job. My family thinks I'm crazy. But I know what I experienced.", Tone = DialogueTone.Dramatic, Weight = 1f }
        };
        template.AcceptanceLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "The veil between worlds is thin, Vern. Most people just refuse to see it.", Tone = DialogueTone.Conspiratorial, Weight = 1f },
            new DialogueTemplate { Text = "Finally, someone who understands. The dead have messages for us.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "You get it. That's why I called your show.", Tone = DialogueTone.Neutral, Weight = 1f }
        };
        template.ConclusionLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Listen to the dead, Vern. They have so much to tell us.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "I've said my piece. The truth will come out eventually.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Keep the lines open. They're trying to reach us.", Tone = DialogueTone.Conspiratorial, Weight = 1f }
        };
    }

    // ========== CONSPIRACIES DIALOGUE ==========

    private static void PopulateConspiraciesFakeDialogue(CallerDialogueTemplate template)
    {
        template.IntroLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Vern, the government is putting stuff in the water, man!", Tone = DialogueTone.Excited, Weight = 1f },
            new DialogueTemplate { Text = "Dude, I figured out what they're really doing. It's all connected.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "So I read this thing online and it blew my mind...", Tone = DialogueTone.Nervous, Weight = 1f }
        };
        template.DetailLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "It's all on the internet if you know where to look. They can't hide it.", Tone = DialogueTone.Excited, Weight = 1f },
            new DialogueTemplate { Text = "My cousin works for the government and he says weird stuff happens all the time.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "You ever notice how things just... happen? That's not a coincidence.", Tone = DialogueTone.Nervous, Weight = 1f }
        };
        template.DefenseLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Just because I can't prove it doesn't mean it's not true!", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "That's what they want you to think, man.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "Open your eyes! It's all right there!", Tone = DialogueTone.Excited, Weight = 1f }
        };
        template.AcceptanceLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "See? Vern gets it. He knows.", Tone = DialogueTone.Excited, Weight = 1f },
            new DialogueTemplate { Text = "Yeah, exactly what I was saying.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Uh, yeah, that too.", Tone = DialogueTone.Confused, Weight = 1f }
        };
        template.ConclusionLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Stay woke, Vern. They're watching.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Peace out. Question everything.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Later. Keep fighting the system.", Tone = DialogueTone.Neutral, Weight = 1f }
        };
    }

    private static void PopulateConspiraciesQuestionableDialogue(CallerDialogueTemplate template)
    {
        template.IntroLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I've been noticing some things that don't add up, Vern...", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "I'm not usually one for conspiracy theories, but hear me out.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "Something happened at work and now I'm not sure what to believe.", Tone = DialogueTone.Nervous, Weight = 1f }
        };
        template.DetailLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "The official story just doesn't match what I saw. Small details, but they matter.", Tone = DialogueTone.Confused, Weight = 1f },
            new DialogueTemplate { Text = "I found some documents that were supposed to be destroyed. Why would they do that?", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "People who ask questions keep getting transferred. That can't be a coincidence.", Tone = DialogueTone.Confused, Weight = 1f }
        };
        template.DefenseLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I'm not saying it's a conspiracy, but something's being hidden.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I just want answers. Is that too much to ask?", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "Maybe I'm paranoid, but the pieces fit too well.", Tone = DialogueTone.Nervous, Weight = 1f }
        };
        template.AcceptanceLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "That's what I was thinking too.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Maybe I should dig deeper.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I hadn't considered that angle.", Tone = DialogueTone.Confused, Weight = 1f }
        };
        template.ConclusionLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Thanks for not dismissing me, Vern.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I'll keep looking into it. Carefully.", Tone = DialogueTone.Nervous, Weight = 1f },
            new DialogueTemplate { Text = "I just needed to tell someone.", Tone = DialogueTone.Nervous, Weight = 1f }
        };
    }

    private static void PopulateConspiraciesCredibleDialogue(CallerDialogueTemplate template)
    {
        template.IntroLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Vern, I'm a former government contractor. What I know keeps me up at night.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I've spent two years investigating this. The paper trail is undeniable.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I'm a journalist, and the story I uncovered will never see print. Too dangerous.", Tone = DialogueTone.Neutral, Weight = 1f }
        };
        template.DetailLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "The funding came from black budget accounts. No oversight. No records. Billions, Vern.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I have emails between officials coordinating the cover-up. Dates, names, everything.", Tone = DialogueTone.Excited, Weight = 1f },
            new DialogueTemplate { Text = "Three whistleblowers came forward. All three had accidents within six months. All three.", Tone = DialogueTone.Neutral, Weight = 1f }
        };
        template.DefenseLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I've verified every document. Cross-referenced with public records. This is solid.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "My sources are unimpeachable. Career officials who couldn't stay silent anymore.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I'm risking everything by coming forward. That should tell you something.", Tone = DialogueTone.Neutral, Weight = 1f }
        };
        template.AcceptanceLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Thank you. The mainstream media won't touch this.", Tone = DialogueTone.Excited, Weight = 1f },
            new DialogueTemplate { Text = "That's why alternative media matters. You ask the real questions.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "I knew your audience would understand.", Tone = DialogueTone.Excited, Weight = 1f }
        };
        template.ConclusionLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I'll send you the documents. Let your listeners decide.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "Keep asking questions, Vern. It's the only way.", Tone = DialogueTone.Neutral, Weight = 1f },
            new DialogueTemplate { Text = "The truth is out there. We just have to find it.", Tone = DialogueTone.Excited, Weight = 1f }
        };
    }

    private static void PopulateConspiraciesCompellingDialogue(CallerDialogueTemplate template)
    {
        template.IntroLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Vern, I worked in intelligence for twenty years. I'm breaking my oath tonight.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "I have classified documents. Real ones. What they show changes everything.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "They tried to silence me twice. I'm only alive because I took precautions.", Tone = DialogueTone.Conspiratorial, Weight = 1f }
        };
        template.DetailLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "The program has been running since 1954. Every administration knows. They all play along.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "I've seen the files. Names you'd recognize. Operations that would topple governments.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "The money trail leads to the highest levels. We're talking trillions, Vern. Hidden in plain sight.", Tone = DialogueTone.Conspiratorial, Weight = 1f }
        };
        template.DefenseLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "I've got dead man's switches in place. If something happens to me, everything goes public.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "They've already tried to discredit me. Check my record - spotless until I started asking questions.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "I have nothing to gain and everything to lose. Why would I fabricate this?", Tone = DialogueTone.Neutral, Weight = 1f }
        };
        template.AcceptanceLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "You're one of the few who'll let me speak. They control the rest.", Tone = DialogueTone.Conspiratorial, Weight = 1f },
            new DialogueTemplate { Text = "The system is rigged, Vern. But people are waking up.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "Thank you. The truth needs a platform.", Tone = DialogueTone.Neutral, Weight = 1f }
        };
        template.ConclusionLines = new DialogueTemplate[]
        {
            new DialogueTemplate { Text = "Stay vigilant, Vern. Trust no one. Especially not the ones in power.", Tone = DialogueTone.Conspiratorial, Weight = 1f },
            new DialogueTemplate { Text = "The revolution starts with information. Keep spreading it.", Tone = DialogueTone.Dramatic, Weight = 1f },
            new DialogueTemplate { Text = "Watch your back. They're always watching.", Tone = DialogueTone.Conspiratorial, Weight = 1f }
        };
    }
}
