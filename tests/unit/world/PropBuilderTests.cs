using Chickensoft.GoDotTest;
using Godot;

namespace KBTV.Tests.Unit.World
{
    public class PropBuilderTests : KBTVTestClass
    {
        public PropBuilderTests(Node testScene) : base(testScene) { }

        private static Texture2D CreateOpaqueTexture(int width, int height)
        {
            var image = Image.Create(width, height, false, Image.Format.Rgba8);
            image.Fill(new Color(0.5f, 0.5f, 0.5f, 1f));
            return ImageTexture.CreateFromImage(image);
        }

        private static Texture2D CreateCheckerTexture(int width, int height)
        {
            var image = Image.Create(width, height, false, Image.Format.Rgba8);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool opaque = (x + y) % 2 == 0;
                    image.SetPixel(x, y, opaque ? new Color(1, 1, 1, 1) : new Color(0, 0, 0, 0));
                }
            }
            return ImageTexture.CreateFromImage(image);
        }

        private static Texture2D CreateTransparentTexture(int width, int height)
        {
            var image = Image.Create(width, height, false, Image.Format.Rgba8);
            image.Fill(new Color(0, 0, 0, 0));
            return ImageTexture.CreateFromImage(image);
        }

        private static Texture2D CreateOpaqueRectangleTexture(int width, int height, int rectX, int rectY, int rectW, int rectH)
        {
            var image = Image.Create(width, height, false, Image.Format.Rgba8);
            image.Fill(new Color(0, 0, 0, 0));
            for (int y = rectY; y < rectY + rectH; y++)
            {
                for (int x = rectX; x < rectX + rectW; x++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                        image.SetPixel(x, y, new Color(1, 1, 1, 1));
                }
            }
            return ImageTexture.CreateFromImage(image);
        }

        [Test]
        public void GetBaseFootprint_NullTexture_ReturnsZeroRect()
        {
            var rect = PropBuilder.GetBaseFootprint(null, 16);

            AssertThat(rect.Size == Vector2.Zero, "Expected zero rect for null texture");
            AssertThat(rect.Position == Vector2.Zero, "Expected zero position for null texture");
        }

        [Test]
        public void GetBaseFootprint_FullyTransparentTexture_ReturnsZeroRect()
        {
            var texture = CreateTransparentTexture(16, 16);

            var rect = PropBuilder.GetBaseFootprint(texture, 16);

            AssertThat(rect.Size == Vector2.Zero, "Expected zero rect for transparent texture");
        }

        [Test]
        public void GetBaseFootprint_AllOpaqueTexture_ReturnsFullScannedBounds()
        {
            var texture = CreateOpaqueTexture(16, 16);

            var rect = PropBuilder.GetBaseFootprint(texture, 16);

            AssertAreEqual(0f, rect.Position.X, "X position should be 0");
            AssertAreEqual(0f, rect.Position.Y, "Y position should be 0");
            AssertAreEqual(16f, rect.Size.X, "Width should match texture width");
            AssertAreEqual(16f, rect.Size.Y, "Height should match texture height");
        }

        [Test]
        public void GetBaseFootprint_OpaqueInBottomOnly_ReturnsTightBoundsInBand()
        {
            var texture = CreateOpaqueRectangleTexture(32, 32, 4, 24, 24, 8);

            var rect = PropBuilder.GetBaseFootprint(texture, 16);

            AssertAreEqual(4f, rect.Position.X, "X position should be 4");
            AssertAreEqual(24f, rect.Position.Y, "Y position should be 24");
            AssertAreEqual(24f, rect.Size.X, "Width should be 24");
            AssertAreEqual(8f, rect.Size.Y, "Height should be 8");
        }

        [Test]
        public void GetBaseFootprint_FloorScanHeightLargerThanTexture_ClampsToTextureHeight()
        {
            var texture = CreateOpaqueTexture(8, 8);

            var rect = PropBuilder.GetBaseFootprint(texture, 100);

            AssertAreEqual(8f, rect.Size.X, "Width should be 8");
            AssertAreEqual(8f, rect.Size.Y, "Height should be 8");
        }

        [Test]
        public void GetBaseFootprint_FloorScanHeightOne_OnlyScansBottomRow()
        {
            var texture = CreateOpaqueRectangleTexture(32, 32, 0, 0, 32, 31);
            var image = ((ImageTexture)texture).GetImage();
            image.SetPixel(10, 31, new Color(1, 1, 1, 1));
            image.SetPixel(20, 31, new Color(1, 1, 1, 1));
            var updatedTexture = ImageTexture.CreateFromImage(image);

            var rect = PropBuilder.GetBaseFootprint(updatedTexture, 1);

            AssertAreEqual(10f, rect.Position.X, $"X position should be 10, got {rect.Position.X}");
            AssertAreEqual(31f, rect.Position.Y, $"Y position should be 31 (bottom row), got {rect.Position.Y}");
            AssertAreEqual(11f, rect.Size.X, $"Width should be 11, got {rect.Size.X}");
            AssertAreEqual(1f, rect.Size.Y, $"Height should be 1, got {rect.Size.Y}");
        }

        [Test]
        public void GetBaseFootprint_TriangleShape_ReturnsTightTriangle()
        {
            var image = Image.Create(8, 8, false, Image.Format.Rgba8);
            image.Fill(new Color(0, 0, 0, 0));
            for (int y = 0; y < 8; y++)
            {
                int rowWidth = y + 1;
                for (int x = 0; x < rowWidth; x++)
                {
                    image.SetPixel(x, y, new Color(1, 1, 1, 1));
                }
            }
            var texture = ImageTexture.CreateFromImage(image);

            var rect = PropBuilder.GetBaseFootprint(texture, 8);

            AssertAreEqual(0f, rect.Position.X, "X position should be 0");
            AssertAreEqual(0f, rect.Position.Y, "Y position should be 0");
            AssertAreEqual(8f, rect.Size.X, "Width should be 8");
            AssertAreEqual(8f, rect.Size.Y, "Height should be 8");
        }

        [Test]
        public void GetBaseFootprint_AlphaThreshold_FiltersLowAlpha()
        {
            var image = Image.Create(8, 8, false, Image.Format.Rgba8);
            image.Fill(new Color(0, 0, 0, 0.3f));
            for (int x = 0; x < 8; x++)
                image.SetPixel(x, 7, new Color(0, 0, 0, 1f));
            var texture = ImageTexture.CreateFromImage(image);

            var rectDefault = PropBuilder.GetBaseFootprint(texture, 8, 128);
            var rectLow = PropBuilder.GetBaseFootprint(texture, 8, 50);

            AssertAreEqual(1f, rectDefault.Size.Y, $"Default threshold (128) should only catch the alpha-1.0 bottom row, got size {rectDefault.Size}");
            AssertAreEqual(8f, rectDefault.Size.X, "Default threshold should include the full bottom row width");

            AssertAreEqual(8f, rectLow.Size.Y, $"Low threshold (50) should catch 0.3 alpha rows (76.5 >= 50), got size {rectLow.Size}");
            AssertAreEqual(8f, rectLow.Size.X, "Low threshold should include all 8 columns");
        }

        [Test]
        public void ImageFootprintToSpriteLocal_ShiftsOriginToBottomAnchor()
        {
            var imageRect = new Rect2(4, 24, 24, 8);
            var textureSize = new Vector2(32, 32);

            var local = PropBuilder.ImageFootprintToSpriteLocal(imageRect, textureSize);

            AssertAreEqual(-12f, local.Position.X, $"X should be 4 - 32/2 = -12, got {local.Position.X}");
            AssertAreEqual(-8f, local.Position.Y, $"Y should be 24 - 32 = -8, got {local.Position.Y}");
            AssertAreEqual(24f, local.Size.X, "Width unchanged");
            AssertAreEqual(8f, local.Size.Y, "Height unchanged");
        }

        [Test]
        public void ImageFootprintToSpriteLocal_FullTexture_OriginAtTopLeft()
        {
            var imageRect = new Rect2(0, 0, 32, 32);
            var textureSize = new Vector2(32, 32);

            var local = PropBuilder.ImageFootprintToSpriteLocal(imageRect, textureSize);

            AssertAreEqual(-16f, local.Position.X, $"X should be -16, got {local.Position.X}");
            AssertAreEqual(-32f, local.Position.Y, $"Y should be -32, got {local.Position.Y}");
            AssertAreEqual(32f, local.Size.X, "Width unchanged");
            AssertAreEqual(32f, local.Size.Y, "Height unchanged");
        }

        [Test]
        public void FootprintToCollisionCenter_SpeakerStandBase_PlacesBottomEdgeAtFloor()
        {
            var footprint = new Rect2(11, 52, 13, 12);
            var textureSize = new Vector2(32, 64);

            var center = PropBuilder.FootprintToCollisionCenter(footprint, textureSize);

            AssertAreEqual(1.5f, center.X, $"X center should be 11 + 6.5 - 16 = 1.5, got {center.X}");
            AssertAreEqual(-6f, center.Y, $"Y center should be 52 + 6 - 64 = -6 (bottom at floor Y=0), got {center.Y}");

            var bottomEdge = center.Y + footprint.Size.Y * 0.5f;
            AssertAreEqual(0f, bottomEdge, $"Bottom edge should be at root Y=0 (the floor), got {bottomEdge}");
        }

        [Test]
        public void FootprintToCollisionCenter_AudioCabinetBase_PlacesBottomEdgeAtFloor()
        {
            var footprint = new Rect2(2, 8, 28, 48);
            var textureSize = new Vector2(32, 56);

            var center = PropBuilder.FootprintToCollisionCenter(footprint, textureSize);

            AssertAreEqual(0f, center.X, $"X center should be 2 + 14 - 16 = 0, got {center.X}");
            AssertAreEqual(-24f, center.Y, $"Y center should be 8 + 24 - 56 = -24, got {center.Y}");

            var bottomEdge = center.Y + footprint.Size.Y * 0.5f;
            AssertAreEqual(0f, bottomEdge, $"Bottom edge should be at root Y=0 (the floor), got {bottomEdge}");
        }

        [Test]
        public void FootprintToCollisionCenter_StorageShelfBase_PlacesBottomEdgeAtFloor()
        {
            var footprint = new Rect2(13, 60, 38, 4);
            var textureSize = new Vector2(64, 64);

            var center = PropBuilder.FootprintToCollisionCenter(footprint, textureSize);

            AssertAreEqual(0f, center.X, $"X center should be 13 + 19 - 32 = 0, got {center.X}");
            AssertAreEqual(-2f, center.Y, $"Y center should be 60 + 2 - 64 = -2 (bottom at floor), got {center.Y}");

            var bottomEdge = center.Y + footprint.Size.Y * 0.5f;
            AssertAreEqual(0f, bottomEdge, $"Bottom edge should be at root Y=0 (the floor), got {bottomEdge}");
        }

        [Test]
        public void FootprintToCollisionCenter_RoundTableSurface_CentersHorizontally()
        {
            var footprint = new Rect2(15, 32, 50, 16);
            var textureSize = new Vector2(80, 48);

            var center = PropBuilder.FootprintToCollisionCenter(footprint, textureSize);

            AssertAreEqual(0f, center.X, $"X center should be 15 + 25 - 40 = 0, got {center.X}");
            AssertAreEqual(-8f, center.Y, $"Y center should be 32 + 8 - 48 = -8 (bottom at floor), got {center.Y}");

            var bottomEdge = center.Y + footprint.Size.Y * 0.5f;
            AssertAreEqual(0f, bottomEdge, $"Bottom edge should be at the floor, got {bottomEdge}");
        }

        [Test]
        public void FootprintToCollisionCenter_AsymmetricFootprint_ShiftsHorizontally()
        {
            var footprint = new Rect2(20, 0, 16, 64);
            var textureSize = new Vector2(64, 64);

            var center = PropBuilder.FootprintToCollisionCenter(footprint, textureSize);

            AssertAreEqual(-4f, center.X, $"X center should be 20 + 8 - 32 = -4, got {center.X}");
            AssertAreEqual(-32f, center.Y, $"Y center should be 0 + 32 - 64 = -32, got {center.Y}");
        }

        [Test]
        public void FootprintToCollisionCenter_BottomEdgeFormula_MatchesMinYPlusHeightMinusTextureHeight()
        {
            var footprint = new Rect2(0, 20, 8, 12);
            var textureSize = new Vector2(32, 32);

            var center = PropBuilder.FootprintToCollisionCenter(footprint, textureSize);
            var bottomEdge = center.Y + footprint.Size.Y * 0.5f;
            var expected = footprint.Position.Y + footprint.Size.Y - textureSize.Y;

            AssertAreEqual(expected, bottomEdge, $"Bottom edge ({bottomEdge}) should equal minY+h-H ({expected})");
        }
    }
}
