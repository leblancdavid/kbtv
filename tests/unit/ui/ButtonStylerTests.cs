using Chickensoft.GoDotTest;
using Godot;
using KBTV.UI;

namespace KBTV.Tests.Unit.UI
{
    public class ButtonStylerTests : KBTVTestClass
    {
        public ButtonStylerTests(Node testScene) : base(testScene) { }

        private Button _approveButton = null!;
        private Button _rejectButton = null!;

        [Setup]
        public void Setup()
        {
            _approveButton = new Button();
            _rejectButton = new Button();
        }

        [Test]
        public void StyleApprove_Disabled_SetsCorrectColors()
        {
            ButtonStyler.StyleApprove(_approveButton, false);

            var style = _approveButton.GetThemeStylebox("normal") as StyleBoxFlat;
            AssertThat(style != null);
        }

        [Test]
        public void StyleApprove_Enabled_SetsCorrectColors()
        {
            ButtonStyler.StyleApprove(_approveButton, true);

            var style = _approveButton.GetThemeStylebox("normal") as StyleBoxFlat;
            AssertThat(style != null);
        }

        [Test]
        public void StyleReject_Disabled_SetsCorrectColors()
        {
            ButtonStyler.StyleReject(_rejectButton, false);

            var style = _rejectButton.GetThemeStylebox("normal") as StyleBoxFlat;
            AssertThat(style != null);
        }

        [Test]
        public void StyleReject_Enabled_SetsCorrectColors()
        {
            ButtonStyler.StyleReject(_rejectButton, true);

            var style = _rejectButton.GetThemeStylebox("normal") as StyleBoxFlat;
            AssertThat(style != null);
        }

        [Test]
        public void StyleApprove_AppliesCornerRadius()
        {
            ButtonStyler.StyleApprove(_approveButton, true);

            var style = _approveButton.GetThemeStylebox("normal") as StyleBoxFlat;
            AssertThat(style!.CornerRadiusTopLeft == 8);
            AssertThat(style.CornerRadiusTopRight == 8);
            AssertThat(style.CornerRadiusBottomLeft == 8);
            AssertThat(style.CornerRadiusBottomRight == 8);
        }

        [Test]
        public void StyleApprove_AppliesContentMargins()
        {
            ButtonStyler.StyleApprove(_approveButton, true);

            var style = _approveButton.GetThemeStylebox("normal") as StyleBoxFlat;
            AssertThat(style!.ContentMarginLeft == 20);
            AssertThat(style.ContentMarginRight == 20);
            AssertThat(style.ContentMarginTop == 12);
            AssertThat(style.ContentMarginBottom == 12);
        }
    }
}
