using System;
using System.Collections.Generic;

namespace Multi_Sprite_Prog_Load
{
    public class Content
    {
        public List<Sprite> Sprites { get; set; }

    }

    public class Sprite
    {
        public string Target { get; set; }
        public string FromFile { get; set; }
        public int SpriteWidth { get; set; }
        public int SpriteHeight { get; set; }
        public int SpriteOriginX { get; set; }
        public int SpriteOriginY { get; set; }

        public int WidthScale { get; set; }
        public int HeightScale { get; set; }

        public BreathType? BreathType { get; set; }

        public Sprite(string target, string fromFile, int spriteWidth, int spriteHeight, int spriteOriginX, int spriteOriginY)
        {
            Target = target;
            FromFile = fromFile;
            SpriteWidth = spriteWidth;
            SpriteHeight = spriteHeight;
            SpriteOriginX = spriteOriginX;
            SpriteOriginY = spriteOriginY;
        }
    }

    public enum BreathType
    {
        Male,
        Female,
        None
    }
}
