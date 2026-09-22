#nullable enable

using System;
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

            AssertTrue(Get<bool>(_modal, "_gameCompleted"));
            var collect = FindButton("CollectButton");
            AssertFalse(collect.Disabled);
            AssertAreEqual("Extract Evidence", collect.Text);
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
    }
}
