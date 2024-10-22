using Microsoft.Xna.Framework.Graphics;
using static StardewValley.Minigames.CraneGame;


namespace Multi_Sprite_Prog_Load
{
    public class HDTextureInfo
    {
        public string Target { get; set; }
        public Texture2D? HDTexture { get; set; }
        public Sprite sprite { get; set; }
        public string sampleSpritePath { get; set; }
        public int SpriteWidth { get; set; }
        public int SpriteHeight { get; set; }

        public int SpriteOriginX { get; set; }
        public int SpriteOriginY { get; set; }


        public int WidthScale { get; set; }
        public int HeightScale { get; set; }

        public bool DisableBreath { get; set; }
        public bool IsFarmer { get; set; }


        public HDTextureInfo(Sprite sprite, Texture2D? hdTexture, bool isFarmer = false, string sampleSpritePath = "empty")
        {

            this.sprite = sprite;

            Target = sprite.Target;
            HDTexture = hdTexture;

            WidthScale = sprite.WidthScale;
            HeightScale = sprite.HeightScale;

            SpriteWidth = sprite.SpriteWidth;
            SpriteHeight = sprite.SpriteHeight;
            SpriteOriginX = SpriteWidth / 2;
            SpriteOriginY = (int)(SpriteHeight * 3 / 4);

            DisableBreath = true;

            this.sampleSpritePath = sampleSpritePath;
        }
    }
}
