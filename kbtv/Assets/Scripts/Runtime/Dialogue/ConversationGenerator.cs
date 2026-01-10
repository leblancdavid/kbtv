using System.Collections.Generic;
using UnityEngine;
using KBTV.Callers;
using KBTV.Data;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Generates procedural conversations between Vern and callers.
    /// Uses templates and Vern's current stats to create dynamic dialogue.
    /// </summary>
    public class ConversationGenerator
    {
        private readonly List<CallerDialogueTemplate> _callerTemplates;
        private readonly VernDialogueTemplate _vernTemplate;
        private readonly VernStats _vernStats;

        // Fallback dialogue for when no templates match
        private static readonly string[] FALLBACK_CALLER_INTRO = new[]
        {
            "Hi Vern, thanks for taking my call.",
            "Vern, you're not gonna believe this...",
            "First time caller, long time listener here.",
            "I've been waiting to tell someone about this."
        };

        private static readonly string[] FALLBACK_CALLER_DETAIL = new[]
        {
            "It happened just last week, clear as day.",
            "I've got photos, but they didn't come out right.",
            "My neighbor saw it too, ask anyone around here.",
            "I know how it sounds, but I'm telling you it's real."
        };

        private static readonly string[] FALLBACK_CALLER_DEFENSE = new[]
        {
            "I know what I saw, Vern.",
            "You can doubt me all you want, but I was there.",
            "This isn't a joke, I'm serious.",
            "Why would I make this up?"
        };

        private static readonly string[] FALLBACK_CALLER_CONCLUSION = new[]
        {
            "Thanks for letting me share this, Vern.",
            "Just wanted to get that off my chest.",
            "Keep doing what you do, Vern.",
            "I'll keep you updated if anything else happens."
        };

        private static readonly string[] FALLBACK_VERN_INTRO = new[]
        {
            "We've got {callerName} on the line from {location}. What's on your mind tonight?",
            "Next up, {callerName} calling in. Go ahead, caller.",
            "{callerName}, you're on the air. What have you got for us?",
            "Let's hear from {callerName} in {location}. You're live."
        };

        private static readonly string[] FALLBACK_VERN_PROBE = new[]
        {
            "Interesting. Tell me more about that.",
            "And when exactly did this happen?",
            "Hold on, let's slow down. Walk me through this.",
            "What made you decide to call in about this tonight?"
        };

        private static readonly string[] FALLBACK_VERN_SKEPTICAL = new[]
        {
            "Now wait a minute, that sounds a bit far-fetched.",
            "I gotta be honest with you, I'm having trouble buying this.",
            "You sure you weren't just seeing things?",
            "Caller, I've heard a lot of stories, and this one's... something."
        };

        private static readonly string[] FALLBACK_VERN_BELIEVING = new[]
        {
            "Now THIS is what I'm talking about, folks.",
            "See, this is why I do this show.",
            "I believe you. I really do.",
            "That's fascinating. Really fascinating."
        };

        private static readonly string[] FALLBACK_VERN_SIGNOFF = new[]
        {
            "Thanks for calling in tonight.",
            "Appreciate the call, stay safe out there.",
            "Keep your eyes on the skies, caller.",
            "Thanks for sharing that with us."
        };

        private static readonly string[] FALLBACK_VERN_EXTRA_PROBE = new[]
        {
            "Don't stop now. What happened next?",
            "Keep going, I'm listening.",
            "And then what?",
            "There's more to this story, isn't there?"
        };

        private static readonly string[] FALLBACK_VERN_ENGAGING = new[]
        {
            "Well now, this is quite a story. Go on.",
            "Okay, okay, you've got my attention.",
            "This is entertaining if nothing else.",
            "Alright, let's see where this goes."
        };

        private static readonly string[] FALLBACK_VERN_CUTOFF = new[]
        {
            "Alright, we're gonna stop you right there. Thanks for calling.",
            "Yeah, I don't think so. Next caller.",
            "We're done here. Moving on.",
            "That's all the time we have for you tonight."
        };

        private static readonly string[] FALLBACK_CALLER_EXTRA_DETAIL = new[]
        {
            "And that's not even the strangest part...",
            "There's more. You need to hear this.",
            "I haven't even told you about what happened after.",
            "But wait, there's something else."
        };

        private static readonly string[] FALLBACK_CALLER_EXTRA_DEFENSE = new[]
        {
            "I can prove it. I have evidence.",
            "Other people have seen the same thing.",
            "This isn't just my story - it's documented.",
            "I knew you'd want proof. I've got it."
        };

        public ConversationGenerator(
            List<CallerDialogueTemplate> callerTemplates,
            VernDialogueTemplate vernTemplate,
            VernStats vernStats)
        {
            _callerTemplates = callerTemplates ?? new List<CallerDialogueTemplate>();
            _vernTemplate = vernTemplate;
            _vernStats = vernStats;
        }

        /// <summary>
        /// Generate a conversation for the given caller.
        /// Conversation length varies based on caller legitimacy and Vern's mood.
        /// </summary>
        public Conversation Generate(Caller caller, Topic currentTopic = null)
        {
            var conversation = new Conversation(caller);

            // Find matching caller template
            CallerDialogueTemplate callerTemplate = FindMatchingTemplate(caller, currentTopic);

            // Determine conversation length (can be affected by Vern's mood for fake callers)
            ConversationLength length = DetermineConversationLength(callerTemplate, caller);

            // Determine Vern's response stance based on stats and caller legitimacy
            VernResponseType challengeResponse = DetermineVernChallengeType(caller, length);

            // Build conversation based on length
            string topicName = currentTopic?.DisplayName ?? caller.ClaimedTopic;

            switch (length)
            {
                case ConversationLength.Short:
                    BuildShortConversation(conversation, callerTemplate, caller, topicName, challengeResponse);
                    break;
                case ConversationLength.Standard:
                    BuildStandardConversation(conversation, callerTemplate, caller, topicName, challengeResponse);
                    break;
                case ConversationLength.Extended:
                    BuildExtendedConversation(conversation, callerTemplate, caller, topicName, challengeResponse);
                    break;
                case ConversationLength.Long:
                    BuildLongConversation(conversation, callerTemplate, caller, topicName, challengeResponse);
                    break;
                default:
                    BuildStandardConversation(conversation, callerTemplate, caller, topicName, challengeResponse);
                    break;
            }

            return conversation;
        }

        /// <summary>
        /// Determine conversation length based on template and Vern's mood.
        /// Fake callers may get extended if Vern is in a good mood and wants to play along.
        /// </summary>
        private ConversationLength DetermineConversationLength(CallerDialogueTemplate template, Caller caller)
        {
            ConversationLength baseLength = template?.Length ?? ConversationLength.Standard;

            // Fake callers can have variable length based on Vern's mood
            if (caller.Legitimacy == CallerLegitimacy.Fake && _vernStats?.Mood != null)
            {
                float mood = _vernStats.Mood.Normalized;
                float patience = _vernStats.Patience.Normalized;

                // Good mood + high patience = might play along (Standard length)
                if (mood > 0.6f && patience > 0.5f)
                {
                    return ConversationLength.Standard;
                }
                // Otherwise cut them short
                return ConversationLength.Short;
            }

            return baseLength;
        }

        /// <summary>
        /// Build a short conversation (6 lines) - used for fake callers being cut off.
        /// Structure: Intro(2) → Probe(2) → Resolution(2)
        /// </summary>
        private void BuildShortConversation(
            Conversation conversation,
            CallerDialogueTemplate callerTemplate,
            Caller caller,
            string topicName,
            VernResponseType challengeResponse)
        {
            // === INTRO PHASE ===
            AddVernLine(conversation, VernResponseType.Introduction, caller, topicName, ConversationPhase.Intro);
            AddCallerLine(conversation, callerTemplate, CallerPhase.Intro, caller, topicName, ConversationPhase.Intro);

            // === PROBE PHASE ===
            AddVernLine(conversation, VernResponseType.Probing, caller, topicName, ConversationPhase.Probe);
            AddCallerLine(conversation, callerTemplate, CallerPhase.Detail, caller, topicName, ConversationPhase.Probe);

            // === RESOLUTION PHASE (skip challenge, go straight to wrap-up) ===
            // Use CutOff or Dismissive based on the challenge response
            VernResponseType signOffType = (challengeResponse == VernResponseType.Engaging) 
                ? VernResponseType.SignOff 
                : VernResponseType.CutOff;
            AddVernLine(conversation, signOffType, caller, topicName, ConversationPhase.Resolution);
            AddCallerLine(conversation, callerTemplate, CallerPhase.Conclusion, caller, topicName, ConversationPhase.Resolution);
        }

        /// <summary>
        /// Build a standard conversation (8 lines) - used for questionable callers.
        /// Structure: Intro(2) → Probe(2) → Challenge(2) → Resolution(2)
        /// </summary>
        private void BuildStandardConversation(
            Conversation conversation,
            CallerDialogueTemplate callerTemplate,
            Caller caller,
            string topicName,
            VernResponseType challengeResponse)
        {
            // === INTRO PHASE ===
            AddVernLine(conversation, VernResponseType.Introduction, caller, topicName, ConversationPhase.Intro);
            AddCallerLine(conversation, callerTemplate, CallerPhase.Intro, caller, topicName, ConversationPhase.Intro);

            // === PROBE PHASE ===
            AddVernLine(conversation, VernResponseType.Probing, caller, topicName, ConversationPhase.Probe);
            AddCallerLine(conversation, callerTemplate, CallerPhase.Detail, caller, topicName, ConversationPhase.Probe);

            // === CHALLENGE PHASE ===
            AddVernLine(conversation, challengeResponse, caller, topicName, ConversationPhase.Challenge);
            AddChallengeCallerResponse(conversation, callerTemplate, caller, topicName, challengeResponse);

            // === RESOLUTION PHASE ===
            AddVernLine(conversation, VernResponseType.SignOff, caller, topicName, ConversationPhase.Resolution);
            AddCallerLine(conversation, callerTemplate, CallerPhase.Conclusion, caller, topicName, ConversationPhase.Resolution);
        }

        /// <summary>
        /// Build an extended conversation (10 lines) - used for credible callers.
        /// Structure: Intro(2) → Probe(2) → ExtraProbe(2) → Challenge(2) → Resolution(2)
        /// </summary>
        private void BuildExtendedConversation(
            Conversation conversation,
            CallerDialogueTemplate callerTemplate,
            Caller caller,
            string topicName,
            VernResponseType challengeResponse)
        {
            // === INTRO PHASE ===
            AddVernLine(conversation, VernResponseType.Introduction, caller, topicName, ConversationPhase.Intro);
            AddCallerLine(conversation, callerTemplate, CallerPhase.Intro, caller, topicName, ConversationPhase.Intro);

            // === PROBE PHASE ===
            AddVernLine(conversation, VernResponseType.Probing, caller, topicName, ConversationPhase.Probe);
            AddCallerLine(conversation, callerTemplate, CallerPhase.Detail, caller, topicName, ConversationPhase.Probe);

            // === EXTRA PROBE PHASE ===
            AddVernLine(conversation, VernResponseType.ExtraProbing, caller, topicName, ConversationPhase.Probe);
            AddCallerLine(conversation, callerTemplate, CallerPhase.ExtraDetail, caller, topicName, ConversationPhase.Probe);

            // === CHALLENGE PHASE ===
            AddVernLine(conversation, challengeResponse, caller, topicName, ConversationPhase.Challenge);
            AddChallengeCallerResponse(conversation, callerTemplate, caller, topicName, challengeResponse);

            // === RESOLUTION PHASE ===
            AddVernLine(conversation, VernResponseType.SignOff, caller, topicName, ConversationPhase.Resolution);
            AddCallerLine(conversation, callerTemplate, CallerPhase.Conclusion, caller, topicName, ConversationPhase.Resolution);
        }

        /// <summary>
        /// Build a long conversation (12 lines) - used for compelling callers.
        /// Structure: Intro(2) → Probe(2) → ExtraProbe(2) → Challenge(2) → ExtraChallenge(2) → Resolution(2)
        /// </summary>
        private void BuildLongConversation(
            Conversation conversation,
            CallerDialogueTemplate callerTemplate,
            Caller caller,
            string topicName,
            VernResponseType challengeResponse)
        {
            // === INTRO PHASE ===
            AddVernLine(conversation, VernResponseType.Introduction, caller, topicName, ConversationPhase.Intro);
            AddCallerLine(conversation, callerTemplate, CallerPhase.Intro, caller, topicName, ConversationPhase.Intro);

            // === PROBE PHASE ===
            AddVernLine(conversation, VernResponseType.Probing, caller, topicName, ConversationPhase.Probe);
            AddCallerLine(conversation, callerTemplate, CallerPhase.Detail, caller, topicName, ConversationPhase.Probe);

            // === EXTRA PROBE PHASE ===
            AddVernLine(conversation, VernResponseType.ExtraProbing, caller, topicName, ConversationPhase.Probe);
            AddCallerLine(conversation, callerTemplate, CallerPhase.ExtraDetail, caller, topicName, ConversationPhase.Probe);

            // === CHALLENGE PHASE ===
            AddVernLine(conversation, challengeResponse, caller, topicName, ConversationPhase.Challenge);
            AddChallengeCallerResponse(conversation, callerTemplate, caller, topicName, challengeResponse);

            // === EXTRA CHALLENGE PHASE (additional defense/elaboration) ===
            // Vern follows up with more probing after the initial challenge
            AddVernLine(conversation, VernResponseType.ExtraProbing, caller, topicName, ConversationPhase.Challenge);
            AddCallerLine(conversation, callerTemplate, CallerPhase.ExtraDefense, caller, topicName, ConversationPhase.Challenge);

            // === RESOLUTION PHASE ===
            AddVernLine(conversation, VernResponseType.SignOff, caller, topicName, ConversationPhase.Resolution);
            AddCallerLine(conversation, callerTemplate, CallerPhase.Conclusion, caller, topicName, ConversationPhase.Resolution);
        }

        /// <summary>
        /// Add the caller's response during the challenge phase based on Vern's stance.
        /// </summary>
        private void AddChallengeCallerResponse(
            Conversation conversation,
            CallerDialogueTemplate callerTemplate,
            Caller caller,
            string topicName,
            VernResponseType challengeResponse)
        {
            // Caller response depends on Vern's stance
            if (challengeResponse == VernResponseType.Skeptical ||
                challengeResponse == VernResponseType.Dismissive ||
                challengeResponse == VernResponseType.Annoyed ||
                challengeResponse == VernResponseType.CutOff)
            {
                AddCallerLine(conversation, callerTemplate, CallerPhase.Defense, caller, topicName, ConversationPhase.Challenge);
            }
            else
            {
                AddCallerLine(conversation, callerTemplate, CallerPhase.Acceptance, caller, topicName, ConversationPhase.Challenge);
            }
        }

        private enum CallerPhase { Intro, Detail, ExtraDetail, Defense, ExtraDefense, Acceptance, Conclusion }

        private CallerDialogueTemplate FindMatchingTemplate(Caller caller, Topic topic)
        {
            CallerDialogueTemplate bestMatch = null;
            int bestPriority = int.MinValue;

            foreach (var template in _callerTemplates)
            {
                if (template.Matches(topic, caller.Legitimacy))
                {
                    if (template.Priority > bestPriority)
                    {
                        bestPriority = template.Priority;
                        bestMatch = template;
                    }
                }
            }

            return bestMatch;
        }

        private VernResponseType DetermineVernChallengeType(Caller caller, ConversationLength length)
        {
            // For fake callers with standard length, Vern is playing along (Engaging)
            if (caller.Legitimacy == CallerLegitimacy.Fake && length == ConversationLength.Standard)
            {
                return VernResponseType.Engaging;
            }

            // If no stats available, use simple legitimacy-based logic
            if (_vernStats?.Energy == null)
            {
                return caller.Legitimacy switch
                {
                    CallerLegitimacy.Fake => VernResponseType.Dismissive,
                    CallerLegitimacy.Questionable => VernResponseType.Skeptical,
                    CallerLegitimacy.Credible => VernResponseType.Believing,
                    CallerLegitimacy.Compelling => VernResponseType.Believing,
                    _ => VernResponseType.Skeptical
                };
            }

            float energy = _vernStats.Energy.Normalized;
            float mood = _vernStats.Mood.Normalized;
            float belief = _vernStats.Belief.Normalized;
            float patience = _vernStats.Patience.Normalized;

            // Low energy = tired responses
            if (energy < 0.2f)
            {
                return VernResponseType.Tired;
            }

            // Low patience + low mood = annoyed
            if (patience < 0.3f && mood < 0.4f)
            {
                return VernResponseType.Annoyed;
            }

            // High belief + credible/compelling caller = believing
            if (belief > 0.5f && caller.Legitimacy >= CallerLegitimacy.Credible)
            {
                return VernResponseType.Believing;
            }

            // Low belief or fake caller = skeptical/dismissive
            if (belief < 0.3f || caller.Legitimacy == CallerLegitimacy.Fake)
            {
                return caller.Legitimacy == CallerLegitimacy.Fake 
                    ? VernResponseType.Dismissive 
                    : VernResponseType.Skeptical;
            }

            // Default: legitimacy-based
            return caller.Legitimacy switch
            {
                CallerLegitimacy.Fake => VernResponseType.Dismissive,
                CallerLegitimacy.Questionable => VernResponseType.Skeptical,
                CallerLegitimacy.Credible => mood > 0.5f ? VernResponseType.Believing : VernResponseType.Skeptical,
                CallerLegitimacy.Compelling => VernResponseType.Believing,
                _ => VernResponseType.Skeptical
            };
        }

        private void AddVernLine(
            Conversation conversation,
            VernResponseType responseType,
            Caller caller,
            string topicName,
            ConversationPhase phase)
        {
            string text;
            DialogueTone tone;

            // Try to get from template first
            DialogueTemplate template = _vernTemplate?.GetResponse(responseType);

            if (template != null)
            {
                text = template.ApplySubstitutions(caller, topicName);
                tone = template.Tone;
            }
            else
            {
                // Use fallback
                text = GetFallbackVernLine(responseType, caller, topicName);
                tone = GetToneForResponseType(responseType);
            }

            conversation.AddLine(Speaker.Vern, text, tone, phase);
        }

        private void AddCallerLine(
            Conversation conversation,
            CallerDialogueTemplate template,
            CallerPhase callerPhase,
            Caller caller,
            string topicName,
            ConversationPhase convPhase)
        {
            string text;
            DialogueTone tone;

            DialogueTemplate lineTemplate = callerPhase switch
            {
                CallerPhase.Intro => template?.GetIntroLine(),
                CallerPhase.Detail => template?.GetDetailLine(),
                CallerPhase.ExtraDetail => template?.GetExtraDetailLine(),
                CallerPhase.Defense => template?.GetDefenseLine(),
                CallerPhase.ExtraDefense => template?.GetExtraDefenseLine(),
                CallerPhase.Acceptance => template?.GetAcceptanceLine(),
                CallerPhase.Conclusion => template?.GetConclusionLine(),
                _ => null
            };

            if (lineTemplate != null)
            {
                text = lineTemplate.ApplySubstitutions(caller, topicName);
                tone = lineTemplate.Tone;
            }
            else
            {
                // Use fallback
                text = GetFallbackCallerLine(callerPhase, caller);
                tone = GetToneForLegitimacy(caller.Legitimacy);
            }

            conversation.AddLine(Speaker.Caller, text, tone, convPhase);
        }

        private string GetFallbackVernLine(VernResponseType type, Caller caller, string topicName)
        {
            string[] lines = type switch
            {
                VernResponseType.Introduction => FALLBACK_VERN_INTRO,
                VernResponseType.Probing => FALLBACK_VERN_PROBE,
                VernResponseType.ExtraProbing => FALLBACK_VERN_EXTRA_PROBE,
                VernResponseType.Skeptical => FALLBACK_VERN_SKEPTICAL,
                VernResponseType.Dismissive => FALLBACK_VERN_SKEPTICAL,
                VernResponseType.Believing => FALLBACK_VERN_BELIEVING,
                VernResponseType.Tired => FALLBACK_VERN_PROBE,
                VernResponseType.Annoyed => FALLBACK_VERN_SKEPTICAL,
                VernResponseType.Engaging => FALLBACK_VERN_ENGAGING,
                VernResponseType.CutOff => FALLBACK_VERN_CUTOFF,
                VernResponseType.SignOff => FALLBACK_VERN_SIGNOFF,
                _ => FALLBACK_VERN_PROBE
            };

            string line = lines[Random.Range(0, lines.Length)];
            
            // Apply substitutions
            line = line.Replace("{callerName}", caller.Name ?? "caller");
            line = line.Replace("{location}", caller.Location ?? "out there");
            line = line.Replace("{topic}", topicName ?? "the paranormal");

            return line;
        }

        private string GetFallbackCallerLine(CallerPhase phase, Caller caller)
        {
            string[] lines = phase switch
            {
                CallerPhase.Intro => FALLBACK_CALLER_INTRO,
                CallerPhase.Detail => FALLBACK_CALLER_DETAIL,
                CallerPhase.ExtraDetail => FALLBACK_CALLER_EXTRA_DETAIL,
                CallerPhase.Defense => FALLBACK_CALLER_DEFENSE,
                CallerPhase.ExtraDefense => FALLBACK_CALLER_EXTRA_DEFENSE,
                CallerPhase.Acceptance => FALLBACK_CALLER_DETAIL,
                CallerPhase.Conclusion => FALLBACK_CALLER_CONCLUSION,
                _ => FALLBACK_CALLER_INTRO
            };

            return lines[Random.Range(0, lines.Length)];
        }

        private DialogueTone GetToneForResponseType(VernResponseType type)
        {
            return type switch
            {
                VernResponseType.Introduction => DialogueTone.Neutral,
                VernResponseType.Probing => DialogueTone.Neutral,
                VernResponseType.ExtraProbing => DialogueTone.Excited,
                VernResponseType.Skeptical => DialogueTone.Skeptical,
                VernResponseType.Dismissive => DialogueTone.Dismissive,
                VernResponseType.Believing => DialogueTone.Believing,
                VernResponseType.Tired => DialogueTone.Neutral,
                VernResponseType.Annoyed => DialogueTone.Annoyed,
                VernResponseType.Engaging => DialogueTone.Neutral,
                VernResponseType.CutOff => DialogueTone.Dismissive,
                VernResponseType.SignOff => DialogueTone.Neutral,
                _ => DialogueTone.Neutral
            };
        }

        private DialogueTone GetToneForLegitimacy(CallerLegitimacy legitimacy)
        {
            return legitimacy switch
            {
                CallerLegitimacy.Fake => DialogueTone.Neutral, // Trying to sound normal
                CallerLegitimacy.Questionable => DialogueTone.Nervous,
                CallerLegitimacy.Credible => DialogueTone.Excited,
                CallerLegitimacy.Compelling => DialogueTone.Dramatic,
                _ => DialogueTone.Neutral
            };
        }
    }
}
