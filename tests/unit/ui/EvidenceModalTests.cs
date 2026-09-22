#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.UI;

namespace KBTV.Tests.Unit.UI
{
    public class EvidenceModalTests : KBTVTestClass
    {
        public EvidenceModalTests(Node testScene) : base(testScene) { }

        protected override bool FailOnRecordedFailures => true;

        private EvidenceModal _modal = null!;

        [Setup]
        public void Setup()
        {
            var scene = GD.Load<PackedScene>("res://scenes/ui/EvidenceModal.tscn");
            AssertNotNull(scene);
            _modal = scene.Instantiate<EvidenceModal>();
            _modal.Initialize(null);
            TestScene.AddChild(_modal);
        }

        private static T Get<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)(field?.GetValue(target) ?? throw new InvalidOperationException($"Field {fieldName} not found"));
        }

        private static InputEventKey LetterKey(char c) =>
            new() { Pressed = true, Unicode = (uint)c };

        private static InputEventKey EnterKey() =>
            new() { Pressed = true, Keycode = Key.Enter };

        private static InputEventKey NumpadEnterKey() =>
            new() { Pressed = true, Keycode = Key.KpEnter };

        /// <summary>
        /// Force a deterministic target word (must be 5 letters).
        /// </summary>
        private void SetTarget(string word)
        {
            typeof(EvidenceModal)
                .GetField("_targetWord", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(_modal, word);
        }

        private void Type(string letters)
        {
            foreach (var c in letters)
            {
                _modal.HandleKey(LetterKey(c));
            }
        }

        private void Submit() => _modal.HandleKey(EnterKey());

        private Button FindButton(string name)
        {
            return _modal.GetNode<Button>($"ModalPanel/ContentContainer/ContentVBox/FooterHBox/{name}");
        }

        private Control Board() =>
            _modal.GetNode<Control>("ModalPanel/ContentContainer/ContentVBox/MainHBoxContainer/RightPanel/AlphabetDisplay");

        private int CountLetterButtons()
        {
            var count = 0;
            foreach (var row in Board().GetChildren())
            {
                foreach (var letter in row.GetChildren())
                {
                    if (letter is Button)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        [Test]
        public void AlphabetBoard_BuildsTwentySixButtons_AllEnabledInitially()
        {
            AssertAreEqual(26, CountLetterButtons());
        }

        [Test]
        public void TypingCorrectPassword_CompletesGameAndEnablesExtract()
        {
            var target = Get<string>(_modal, "_targetWord");

            foreach (var c in target)
            {
                _modal.HandleKey(LetterKey(c));
            }

            AssertAreEqual(target, new string(Get<char[]>(_modal, "_currentInputChars")));

            _modal.HandleKey(EnterKey());

            AssertThat(Get<bool>(_modal, "_gameCompleted"));
            var collect = FindButton("CollectButton");
            AssertFalse(collect.Disabled);
            AssertAreEqual("Download", collect.Text);
        }

        [Test]
        public void RuledOutLetter_CannotBeRetypedAndButtonDisabled()
        {
            var target = Get<string>(_modal, "_targetWord");
            var probe = PickAbsentLetter(target);

            for (int i = 0; i < 5; i++)
            {
                _modal.HandleKey(LetterKey(probe));
            }
            _modal.HandleKey(EnterKey());

            AssertFalse(Get<bool>(_modal, "_gameCompleted"));

            // Slots were cleared; typing the now-ruled-out letter must stay blocked.
            _modal.HandleKey(LetterKey(probe));
            AssertAreEqual('_', Get<char[]>(_modal, "_currentInputChars")[0]);

            // Its board button must be disabled.
            var probeButton = FindLetterButton(probe);
            AssertNotNull(probeButton!);
            AssertThat(probeButton!.Disabled, $"{probe} button should be disabled after being ruled out");
        }

        [Test]
        public void MisplacedLetter_CanBeRetypedAndButtonStaysEnabled()
        {
            var target = Get<string>(_modal, "_targetWord");
            var letter = target[0];
            var filler = PickAbsentLetter(target);

            for (int i = 0; i < 4; i++)
            {
                _modal.HandleKey(LetterKey(filler));
            }
            _modal.HandleKey(LetterKey(letter));
            _modal.HandleKey(EnterKey());

            // 'letter' is in the password at the wrong spot now (green if it was
            // last too). Either way it must not be ruled out.
            var letterButton = FindLetterButton(letter);
            AssertNotNull(letterButton!);
            AssertThat(!letterButton!.Disabled, $"{letter} should stay usable after a wrong-position guess");

            _modal.HandleKey(LetterKey(letter));
            AssertAreEqual(letter, Get<char[]>(_modal, "_currentInputChars")[0]);
        }

        /// <summary>
        /// First candidate letter guaranteed absent from the target word.
        /// </summary>
        private static char PickAbsentLetter(string target)
        {
            foreach (var c in "XYZWVJKQUPFGH")
            {
                if (!target.Contains(c))
                {
                    return c;
                }
            }
            throw new InvalidOperationException($"No absent letter for target {target}");
        }

        private Button FindLetterButton(char letter)
        {
            Button? found = null;
            foreach (var row in Board().GetChildren())
            {
                foreach (var child in row.GetChildren())
                {
                    if (child is Button button && button.Text == letter.ToString())
                    {
                        found = button;
                    }
                }
            }
            return found!;
        }

        [Test]
        public void ClickingLetterButton_TypesIntoFirstOpenSlot()
        {
            Button? first = null;
            foreach (var row in Board().GetChildren())
            {
                foreach (var child in row.GetChildren())
                {
                    if (child is Button button && !button.Disabled)
                    {
                        first = button;
                        break;
                    }
                }
                if (first != null) break;
            }

            AssertNotNull(first!);
            first!.EmitSignal(Button.SignalName.Pressed);

            var letter = first.Text[0];
            AssertAreEqual(letter, Get<char[]>(_modal, "_currentInputChars")[0]);
        }

        [Test]
        public void Abort_ClosesModalAndSignals()
        {
            var closed = false;
            _modal.ModalClosed += () => closed = true;

            _modal.Abort();

            AssertTrue(closed);
        }

        [Test]
        public void HandleKey_AfterCompletion_Ignored()
        {
            var target = Get<string>(_modal, "_targetWord");
            foreach (var c in target)
            {
                _modal.HandleKey(LetterKey(c));
            }
            _modal.HandleKey(EnterKey());

            // Game completed; further keys must not mutate the input.
            var snapshot = new string(Get<char[]>(_modal, "_currentInputChars"));
            AssertThat(_modal.HandleKey(LetterKey('X')) == false);
            AssertAreEqual(snapshot, new string(Get<char[]>(_modal, "_currentInputChars")));
        }

        [Test]
        public void GreenLetters_AutoFillNextLine_AndSolveWorks()
        {
            SetTarget("PROOF");

            // "FLOOD" scores greens at idx2/3 (O,O); F is only a wrong-position
            // match and must NOT stay filled.
            Type("FLOOD");
            Submit();

            var input = Get<char[]>(_modal, "_currentInputChars");
            var filled = Get<bool[]>(_modal, "_positionsFilled");
            AssertAreEqual('O', input[2], "green O auto-filled at slot 2");
            AssertAreEqual('O', input[3], "green O auto-filled at slot 3");
            AssertThat(filled[2] && filled[3], "green slots locked");
            AssertAreEqual('_', input[0], "wrong-position slots cleared");
            AssertAreEqual('_', input[1], "wrong-position slots cleared");
            AssertAreEqual('_', input[4], "wrong-position slots cleared");

            // Slots 0/1/4 are open; typing P,R,F yields "PROOF" thanks to the
            // carried greens.
            Type("PRF");
            AssertAreEqual("PROOF", new string(Get<char[]>(_modal, "_currentInputChars")));
            Submit();
            AssertThat(Get<bool>(_modal, "_gameCompleted"), "locked greens + typed letters solve it");
        }

        [Test]
        public void DoubleLetter_GreenAndSurplus_LetterStaysEnabled()
        {
            SetTarget("PAPER");

            // "MAPPY": greens A(1), P(2); surplus P(3) consumes the only other
            // target P(0) as a match; M/Y absent -> ruled out.
            Type("MAPPY");
            Submit();

            var filled = Get<bool[]>(_modal, "_positionsFilled");
            var input = Get<char[]>(_modal, "_currentInputChars");
            AssertThat(filled[1] && filled[2], "A and P greens locked");
            AssertAreEqual('A', input[1]);
            AssertAreEqual('P', input[2]);

            // P must stay typeable: its only remaining home (slot 0) is open.
            var pButton = FindLetterButton('P');
            AssertThat(!pButton!.Disabled, "P must remain enabled while another P may still be placed");

            Type("PER"); // P->0, E->3, R->4 -> "PAPER"
            AssertAreEqual("PAPER", new string(Get<char[]>(_modal, "_currentInputChars")));
            Submit();
            AssertTrue(Get<bool>(_modal, "_gameCompleted"));
        }

        [Test]
        public void SurplusLetter_NeverRuledOutWhileInWord()
        {
            SetTarget("SPELL");

            // "MILLS": L green at 3, surplus L at 2 matches target L(4);
            // S at 4 matches target S(0). The second L must not turn red.
            Type("MILLS");
            Submit();

            var lButton = FindLetterButton('L');
            AssertThat(!lButton!.Disabled, "L appears twice in SPELL; surplus guess copy must not rule it out");

            // Only fully-absent letters get locked out.
            AssertThat(FindLetterButton('M')!.Disabled, "M absent from SPELL -> disabled");
            AssertThat(FindLetterButton('I')!.Disabled, "I absent from SPELL -> disabled");
        }

        [Test]
        public void Greens_SurviveAdditionalWrongGuess()
        {
            SetTarget("PAPER");

            Type("MAPPY"); // greens A(1), P(2)
            Submit();

            // Slots 0/3/4 are open; "ZAPER" is wrong only at slot 0, adding
            // greens E(3) and R(4) while keeping the earlier ones.
            Type("ZER");
            AssertAreEqual("ZAPER", new string(Get<char[]>(_modal, "_currentInputChars")));
            Submit();

            var input = Get<char[]>(_modal, "_currentInputChars");
            var filled = Get<bool[]>(_modal, "_positionsFilled");
            AssertThat(filled[1] && filled[2] && filled[3] && filled[4], "old greens persist and new greens lock in");
            AssertAreEqual('_', input[0], "wrong slot cleared again");

            // 'P' already had both target copies accounted for in guess 1,
            // but it must still be typeable to place the confirmed P in slot 0.
            var pButton = FindLetterButton('P');
            AssertThat(!pButton!.Disabled, "confirmed letter must stay typeable for its other occurrence");
            Type("P");
            AssertAreEqual("PAPER", new string(Get<char[]>(_modal, "_currentInputChars")));
            Submit();
            AssertTrue(Get<bool>(_modal, "_gameCompleted"));
        }

        [Test]
        public void NumpadEnter_SubmitsGuess()
        {
            var target = Get<string>(_modal, "_targetWord");
            Type(target);
            AssertThat(_modal.HandleKey(NumpadEnterKey()));
            AssertThat(Get<bool>(_modal, "_gameCompleted"), "KpEnter must submit like Enter");
        }

        [Test]
        public void WordFile_EveryEntryValidAndUnique()
        {
            var text = FileAccess.GetFileAsString("res://assets/config/evidence_words.json");
            AssertThat(!string.IsNullOrEmpty(text), "word file must be readable");

            var parsed = Json.ParseString(text);
            AssertThat(parsed.VariantType == Variant.Type.Dictionary, "word file must be a JSON object");
            var dict = (Godot.Collections.Dictionary)parsed;
            AssertThat(dict.ContainsKey("words"), "must contain 'words'");
            var words = (Godot.Collections.Array)dict["words"];

            AssertThat(words.Count >= 400, $"curated pool too small: {words.Count}");

            var seen = new HashSet<string>();
            foreach (var w in words)
            {
                var s = w.AsString().ToUpper();
                AssertThat(s.Length == 5 && s.All(c => c >= 'A' && c <= 'Z'),
                    $"entry '{s}' is not exactly 5 A-Z letters");
                AssertThat(seen.Add(s), $"duplicate entry '{s}'");
            }
        }

        [Test]
        public void WordList_SampledSweep_EveryWordIsTypeableAndWinnable()
        {
            var field = typeof(EvidenceModal)
                .GetField("_wordList", BindingFlags.NonPublic | BindingFlags.Static)!;
            var wordList = (List<string>)field.GetValue(null)!;
            AssertThat(wordList.Count > 0, "loader must populate the word list");

            // Sample every 10th word plus a spread of double-letter words.
            var sweep = new List<string>();
            for (int i = 0; i < wordList.Count; i += 10)
            {
                sweep.Add(wordList[i]);
            }
            foreach (var w in new[] { "SPELL", "GUESS", "PAPER", "PROOF", "HAPPY", "STOOL", "ADDED", "ALIAS" })
            {
                if (wordList.Contains(w))
                {
                    sweep.Add(w);
                }
            }

            foreach (var word in sweep)
            {
                ResetGameForTarget(word);
                Type(word);
                AssertAreEqual(word, new string(Get<char[]>(_modal, "_currentInputChars")),
                    $"every target word must be typeable (blocked by a letter gate?): {word}");
                Submit();

                var won = Get<bool>(_modal, "_gameCompleted");
                AssertThat(won, $"word '{word}' must be winnable by typing it");
                var collect = FindButton("CollectButton");
                AssertThat(!collect.Disabled, $"collect button must enable after winning with '{word}'");
            }
        }

        [Test]
        public void RuledOutLetter_ButtonKeepsRedDisabledState()
        {
            var target = Get<string>(_modal, "_targetWord");
            var probe = PickAbsentLetter(target);

            for (int i = 0; i < 5; i++)
            {
                _modal.HandleKey(LetterKey(probe));
            }
            _modal.HandleKey(EnterKey());

            var button = FindLetterButton(probe);
            AssertThat(button!.Disabled, "ruled-out button is disabled");

            // Regression: disabled tiles used to render generic gray, hiding the
            // red status entirely (indistinguishable from unused letters).
            var font = button.GetThemeColor("font_disabled_color");
            var expected = new Color(0.85f * 0.9f, 0.3f * 0.9f, 0.25f * 0.9f);
            AssertThat(System.Math.Abs(font.R - expected.R) < 0.05f &&
                       System.Math.Abs(font.G - expected.G) < 0.05f &&
                       System.Math.Abs(font.B - expected.B) < 0.05f,
                $"ruled-out disabled font must be red, got {font}");

            if (button.GetThemeStylebox("disabled") is StyleBoxFlat box)
            {
                AssertThat(box.BorderColor.R > box.BorderColor.G + 0.1f,
                    $"ruled-out disabled border must be red, got {box.BorderColor}");
            }
            else
            {
                AssertTrue(false);
                GD.PrintErr($"ruled-out button missing disabled stylebox: {button.Name}");
            }
        }

        [Test]
        public void Enter_WithEmptySlots_WarnsWithoutConsumingAttempt()
        {
            Type("AAR");
            _modal.HandleKey(EnterKey());

            AssertAreEqual(0, Get<int>(_modal, "_currentAttempt"),
                "incomplete Enter must not spend an attempt");

            var content = _modal.GetNode<VBoxContainer>(
                "ModalPanel/ContentContainer/ContentVBox/MainHBoxContainer/LeftPanel/GuessHistory/GuessHistoryContent");
            var warned = content.GetChildren().OfType<RichTextLabel>()
                .Any(l => l.Text.Contains("INCOMPLETE"));
            AssertThat(warned, "Enter with empty slots must surface a visible warning");
        }

        private void ResetGameForTarget(string word)
        {
            var type = typeof(EvidenceModal);
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

            type.GetField("_gameCompleted", flags)!.SetValue(_modal, false);
            type.GetField("_currentAttempt", flags)!.SetValue(_modal, 0);
            ((List<string>)Get<object>(_modal, "_previousGuesses")!).Clear();
            type.GetField("_positionsFilled", flags)!.SetValue(_modal, new bool[5]);
            type.GetField("_currentInputChars", flags)!
                .SetValue(_modal, new char[] { '_', '_', '_', '_', '_' });
            type.GetMethod("InitializeLetterStates", flags)!.Invoke(_modal, null);
            SetTarget(word);
        }
    }
}
