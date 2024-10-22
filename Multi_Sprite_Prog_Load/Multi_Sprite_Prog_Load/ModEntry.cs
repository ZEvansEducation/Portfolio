using Force.DeepCloner;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.Utilities;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Characters;
using StardewValley.Locations;
using StardewValley.Minigames;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml;

namespace Multi_Sprite_Prog_Load
{
    public class ModEntry : Mod
    {
        IModHelper modHelper;
        public List<HDTextureInfo> hdTextures = new List<HDTextureInfo>();
        HashSet<string> spritesToInvalidateDaily = new HashSet<string>();
        HashSet<string> activeNPCs = new HashSet<string>();
        HashSet<string> npcLevelChanged_needsInvalidation = new HashSet<string>();
        HashSet<string> npcFirstLoad = new HashSet<string>();
        Dictionary<string, int> oldLevel  = new Dictionary<string, int>();
        Dictionary<string, int> newLevel  = new Dictionary<string, int>();
        private static Dictionary<string, HDTextureInfo> activeSprites = new Dictionary<string, HDTextureInfo>();
        private static Dictionary<string, string> activePortraits = new Dictionary<string, string>();
        private static string directoryPath;
        private NPC npc;
        public override void Entry(IModHelper helper)
        {
            var harmony = new Harmony(this.ModManifest.UniqueID);

            foreach (MethodInfo method in typeof(ModEntry).GetMethods(BindingFlags.Static | BindingFlags.Public).Where(m => m.Name == "Draw"))
            {
                //Shotgun approach
                harmony.Patch(typeof(SpriteBatch).GetMethod("Draw", method.GetParameters().Select(p => p.ParameterType).Where(t => !t.Name.Contains("SpriteBatch")).ToArray()), new HarmonyMethod(method));
            }


            directoryPath = helper.DirectoryPath;

            string npcFolderPath = Path.Combine(Helper.DirectoryPath, "Assets", "NPC");
            string[] dirs = Directory.GetDirectories(npcFolderPath);
            foreach (string dir in dirs)
            {
                activeNPCs.Add(Path.GetFileName(dir));
                npcFirstLoad.Add(Path.GetFileName(dir));
            }
            
            //Handle daily Conditional checks
            //helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
            helper.Events.Content.AssetRequested += Content_AssetRequested;
            
        }

        private void GameLoop_SaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            setSprite();
            setPortrait();
            setBeachPortrait();
            setWinterPortrait();
            setBeachSprite();
            setWinterSprite();

            foreach (var item in spritesToInvalidateDaily)
            {
                this.Helper.GameContent.InvalidateCache(item);
            }
        }

        private void setSprite()
        {
            if(!Context.IsGameLaunched)
            {
                return; 
            }
            hdTextures.Clear();
            
            
            bool isFarmerTX = false;
            foreach (string npcName in activeNPCs)
            {
                NPC npc = Game1.getCharacterFromName(npcName);
                String targetPath = "Characters/" + npcName;
                String assetPath;
                String samplePath;
                int npcCurrentPoints = Game1.player.getFriendshipLevelForNPC(npcName);
                if (newLevel.ContainsKey(npcName))
                {
                    newLevel[npcName] = npcCurrentPoints;
                }
                else { newLevel.Add(npcName, npcCurrentPoints); }

                if (npcCurrentPoints < 2499)
                {
                    assetPath = Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName, "Base", "Level" + (levelCalculation(npcName, Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName))), "Sprites", npcName + ".png");
                    samplePath = Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName, "Base", "Level" + (levelCalculation(npcName, Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName))), "Sprites", "Sample.png");
                }
                else
                {
                    assetPath = Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName, "Marriage", "Level" + (levelCalculation(npcName, Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName))), "Sprites", npcName + ".png");
                    samplePath = Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName, "Marriage", "Level" + (levelCalculation(npcName, Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName))), "Sprites", "Sample.png");
                }
                Texture2D sampleSprite = Helper.ModContent.Load<Texture2D>(samplePath);
                Texture2D replacementSprite = Helper.ModContent.Load<Texture2D>(assetPath);
                int spriteWidth = sampleSprite.Width;
                int spriteHeight = sampleSprite.Height;
                int spriteOriginX = (int)((float)spriteWidth / 2.0);
                int spriteOriginY = (int)((float)spriteHeight * 3f / 4f);
                Sprite newSprite = new Sprite(targetPath, assetPath, spriteWidth, spriteHeight, spriteOriginX, spriteOriginY);
                spritesToInvalidateDaily.Add(targetPath);
                HDTextureInfo textureInfo = new HDTextureInfo(newSprite, replacementSprite, isFarmerTX);
                hdTextures.Add(textureInfo);

                if (activeSprites.ContainsKey(targetPath))
                {
                    activeSprites[targetPath] = textureInfo;
                }else {activeSprites.Add(targetPath, textureInfo); }

                //Console.WriteLine(npcName);
                if (oldLevel.ContainsKey(npcName))
                {
                    if (newLevel[npcName] != oldLevel[npcName]) { oldLevel[npcName] = newLevel[npcName]; }
                    else
                    {
                        //npc level hasn't changed
                    }
                }
                else { 
                    oldLevel.Add(npcName, newLevel[npcName]);
                    if (npcLevelChanged_needsInvalidation.Contains("Characters/" + npcName))
                    {
                        //npcLevelChanged_needsInvalidation.Add("Characters/" + npcName);
                    }
                    else
                    {
                        npcLevelChanged_needsInvalidation.Add("Characters/" + npcName);
                    }
                    
                }
                
            }
            
        }
        private void setPortrait()
        {
            if (!Context.IsGameLaunched)
            {
                return;
            }
            hdTextures.Clear();


            foreach (string npcName in activeNPCs)
            {
                NPC npc = Game1.getCharacterFromName(npcName);
                String targetPath = "Portraits/" + npcName;
                String assetPath;
                int npcCurrentPoints = Game1.player.getFriendshipLevelForNPC(npcName);
                if (newLevel.ContainsKey(npcName))
                {
                    newLevel[npcName] = npcCurrentPoints;
                }
                else { newLevel.Add(npcName, npcCurrentPoints); }

                if (npcCurrentPoints < 2499)
                {
                    assetPath = Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName, "Base", "Level" + (levelCalculation(npcName, Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName))), "Portraits", npcName + ".png");
                }
                else
                {
                    assetPath = Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName, "Marriage", "Level" + (levelCalculation(npcName, Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName))), "Portraits", npcName + ".png");
                }
                

                
                //Console.WriteLine(npcName);
                
                if (npcLevelChanged_needsInvalidation.Contains("Portraits/" + npcName))
                {
                    //npcLevelChanged_needsInvalidation.Add("Characters/" + npcName);
                }
                else
                {
                    npcLevelChanged_needsInvalidation.Add("Portraits/" + npcName);
                }
                if (activePortraits.ContainsKey(npcName))
                {
                    activePortraits[npcName] = assetPath;
                }
                else
                {
                    activePortraits.Add(npcName, assetPath);
                }

            }

        }
        private void setBeachPortrait()
        {
            if (!Context.IsGameLaunched)
            {
                return;
            }
            hdTextures.Clear();


            foreach (string npcName in activeNPCs)
            {
                NPC npc = Game1.getCharacterFromName(npcName);
                String targetPath = "Portraits/" + npcName + "_Beach";
                String assetPath;
                int npcCurrentPoints = Game1.player.getFriendshipLevelForNPC(npcName);
                if (newLevel.ContainsKey(npcName))
                {
                    newLevel[npcName] = npcCurrentPoints;
                }
                else { newLevel.Add(npcName, npcCurrentPoints); }

                if (npcCurrentPoints < 2499)
                {
                    assetPath = Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName, "Base", "Level" + (levelCalculation(npcName, Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName))), "Portraits", npcName + "_Beach" + ".png");
                }
                else
                {
                    assetPath = Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName, "Marriage", "Level" + (levelCalculation(npcName, Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName))), "Portraits", npcName + "_Beach" + ".png");
                }



                //Console.WriteLine(npcName);

                if (npcLevelChanged_needsInvalidation.Contains("Portraits/" + npcName + "_Beach"))
                {
                    //npcLevelChanged_needsInvalidation.Add("Characters/" + npcName);
                }
                else
                {
                    npcLevelChanged_needsInvalidation.Add("Portraits/" + npcName + "_Beach");
                }
                if (activePortraits.ContainsKey(npcName))
                {
                    activePortraits[npcName] = assetPath;
                }
                else
                {
                    activePortraits.Add(npcName, assetPath);
                }

            }

        }
        private void setWinterPortrait()
        {
            if (!Context.IsGameLaunched)
            {
                return;
            }
            hdTextures.Clear();


            foreach (string npcName in activeNPCs)
            {
                NPC npc = Game1.getCharacterFromName(npcName);
                String targetPath = "Portraits/" + npcName + "_Winter";
                String assetPath;
                int npcCurrentPoints = Game1.player.getFriendshipLevelForNPC(npcName);
                if (newLevel.ContainsKey(npcName))
                {
                    newLevel[npcName] = npcCurrentPoints;
                }
                else { newLevel.Add(npcName, npcCurrentPoints); }

                if (npcCurrentPoints < 2499)
                {
                    assetPath = Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName, "Base", "Level" + (levelCalculation(npcName, Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName))), "Portraits", npcName + "_Winter" + ".png");
                }
                else
                {
                    assetPath = Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName, "Marriage", "Level" + (levelCalculation(npcName, Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName))), "Portraits", npcName + "_Winter" + ".png");
                }



                //Console.WriteLine(npcName);

                if (npcLevelChanged_needsInvalidation.Contains("Portraits/" + npcName + "_Winter"))
                {
                    //npcLevelChanged_needsInvalidation.Add("Characters/" + npcName);
                }
                else
                {
                    npcLevelChanged_needsInvalidation.Add("Portraits/" + npcName + "_Winter");
                }
                if (activePortraits.ContainsKey(npcName))
                {
                    activePortraits[npcName] = assetPath;
                }
                else
                {
                    activePortraits.Add(npcName, assetPath);
                }

            }

        }
        private void setBeachSprite()
        {
            if (!Context.IsGameLaunched)
            {
                return;
            }
            hdTextures.Clear();


            bool isFarmerTX = false;
            foreach (string npcName in activeNPCs)
            {
                NPC npc = Game1.getCharacterFromName(npcName);
                String targetPath = "Characters/" + npcName + "_Beach";
                String assetPath;
                String samplePath;
                int npcCurrentPoints = Game1.player.getFriendshipLevelForNPC(npcName);
                if (newLevel.ContainsKey(npcName))
                {
                    newLevel[npcName] = npcCurrentPoints;
                }
                else { newLevel.Add(npcName, npcCurrentPoints); }

                if (npcCurrentPoints < 2499)
                {
                    assetPath = Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName, "Base", "Level" + (levelCalculation(npcName, Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName))), "Sprites", npcName + "_Beach" + ".png");
                    samplePath = Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName, "Base", "Level" + (levelCalculation(npcName, Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName))), "Sprites", "Sample.png");
                }
                else
                {
                    assetPath = Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName, "Marriage", "Level" + (levelCalculation(npcName, Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName))), "Sprites", npcName + "_Beach" + ".png");
                    samplePath = Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName, "Marriage", "Level" + (levelCalculation(npcName, Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName))), "Sprites", "Sample.png");
                }
                Texture2D sampleSprite = Helper.ModContent.Load<Texture2D>(samplePath);
                Texture2D replacementSprite = Helper.ModContent.Load<Texture2D>(assetPath);
                int spriteWidth = sampleSprite.Width;
                int spriteHeight = sampleSprite.Height;
                int spriteOriginX = (int)((float)spriteWidth / 2.0);
                int spriteOriginY = (int)((float)spriteHeight * 3f / 4f);
                Sprite newSprite = new Sprite(targetPath, assetPath, spriteWidth, spriteHeight, spriteOriginX, spriteOriginY);
                spritesToInvalidateDaily.Add(targetPath);
                HDTextureInfo textureInfo = new HDTextureInfo(newSprite, replacementSprite, isFarmerTX);
                hdTextures.Add(textureInfo);

                if (activeSprites.ContainsKey(targetPath))
                {
                    activeSprites[targetPath] = textureInfo;
                }
                else { activeSprites.Add(targetPath, textureInfo); }

                //Console.WriteLine(npcName);
                if (oldLevel.ContainsKey(npcName))
                {
                    if (newLevel[npcName] != oldLevel[npcName]) { oldLevel[npcName] = newLevel[npcName]; }
                    else
                    {
                        //npc level hasn't changed
                    }
                }
                else
                {
                    oldLevel.Add(npcName, newLevel[npcName]);
                    if (npcLevelChanged_needsInvalidation.Contains("Characters/" + npcName + "_Beach"))
                    {
                        //npcLevelChanged_needsInvalidation.Add("Characters/" + npcName);
                    }
                    else
                    {
                        npcLevelChanged_needsInvalidation.Add("Characters/" + npcName + "_Beach");
                    }

                }

            }

        }
        private void setWinterSprite()
        {
            if (!Context.IsGameLaunched)
            {
                return;
            }
            hdTextures.Clear();


            bool isFarmerTX = false;
            foreach (string npcName in activeNPCs)
            {
                NPC npc = Game1.getCharacterFromName(npcName);
                String targetPath = "Characters/" + npcName + "_Winter";
                String assetPath;
                String samplePath;
                int npcCurrentPoints = Game1.player.getFriendshipLevelForNPC(npcName);
                if (newLevel.ContainsKey(npcName))
                {
                    newLevel[npcName] = npcCurrentPoints;
                }
                else { newLevel.Add(npcName, npcCurrentPoints); }

                if (npcCurrentPoints < 2499)
                {
                    assetPath = Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName, "Base", "Level" + (levelCalculation(npcName, Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName))), "Sprites", npcName + "_Winter" + ".png");
                    samplePath = Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName, "Base", "Level" + (levelCalculation(npcName, Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName))), "Sprites", "Sample.png");
                }
                else
                {
                    assetPath = Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName, "Marriage", "Level" + (levelCalculation(npcName, Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName))), "Sprites", npcName + "_Winter" + ".png");
                    samplePath = Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName, "Marriage", "Level" + (levelCalculation(npcName, Path.Combine(Helper.DirectoryPath, "Assets", "NPC", npcName))), "Sprites", "Sample.png");
                }
                Texture2D sampleSprite = Helper.ModContent.Load<Texture2D>(samplePath);
                Texture2D replacementSprite = Helper.ModContent.Load<Texture2D>(assetPath);
                int spriteWidth = sampleSprite.Width;
                int spriteHeight = sampleSprite.Height;
                int spriteOriginX = (int)((float)spriteWidth / 2.0);
                int spriteOriginY = (int)((float)spriteHeight * 3f / 4f);
                Sprite newSprite = new Sprite(targetPath, assetPath, spriteWidth, spriteHeight, spriteOriginX, spriteOriginY);
                spritesToInvalidateDaily.Add(targetPath);
                HDTextureInfo textureInfo = new HDTextureInfo(newSprite, replacementSprite, isFarmerTX);
                hdTextures.Add(textureInfo);

                if (activeSprites.ContainsKey(targetPath))
                {
                    activeSprites[targetPath] = textureInfo;
                }
                else { activeSprites.Add(targetPath, textureInfo); }

                //Console.WriteLine(npcName);
                if (oldLevel.ContainsKey(npcName))
                {
                    if (newLevel[npcName] != oldLevel[npcName]) { oldLevel[npcName] = newLevel[npcName]; }
                    else
                    {
                        //npc level hasn't changed
                    }
                }
                else
                {
                    oldLevel.Add(npcName, newLevel[npcName]);
                    if (npcLevelChanged_needsInvalidation.Contains("Characters/" + npcName + "_Winter"))
                    {
                        //npcLevelChanged_needsInvalidation.Add("Characters/" + npcName);
                    }
                    else
                    {
                        npcLevelChanged_needsInvalidation.Add("Characters/" + npcName + "_Winter");
                    }

                }

            }

        }
        private int levelCalculation(string npcName, string characterPath)
        {
            int activeLevel = 0;
            int npcCurrentPoints = Game1.player.getFriendshipLevelForNPC(npcName);
            int characterBaseLevels = Directory.GetDirectories(Path.Combine(characterPath, "Base")).Length;
            int characterMarriageLevels = Directory.GetDirectories(Path.Combine(characterPath, "Marriage")).Length;
            if (npcCurrentPoints != null)
            {
                if (npcCurrentPoints < 2499)
                {
                    activeLevel = (int)(((float)npcCurrentPoints) / ((2499.0 / (float)characterBaseLevels)));
                    if (activeLevel > characterBaseLevels - 1) { activeLevel = characterBaseLevels - 1; }
                }
                else
                {
                    activeLevel = (int)(((float)npcCurrentPoints - 1249.0) /(1249.0 / (float)characterMarriageLevels));
                    if (activeLevel > characterMarriageLevels - 1) { activeLevel = characterMarriageLevels - 1; }
                }
            }
            if (activeLevel < 0)
            {
                activeLevel = 0;
            }
            return activeLevel;
        }

        private void GameLoop_DayStarted(object? sender, DayStartedEventArgs e)
        {
            setSprite();
            setPortrait();
            setBeachPortrait();
            setWinterPortrait();
            setBeachSprite();
            setWinterSprite();

            foreach (var item in spritesToInvalidateDaily)
            {
                this.Helper.GameContent.InvalidateCache(item);
            }
        }
        
        private void Content_AssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (Context.IsGameLaunched)
            {
                setSprite();
                //setPortrait();
                //setBeachPortrait();
                //setWinterPortrait();
                setBeachSprite();
                //setWinterSprite();
            }

            foreach (string npcPortrait in activePortraits.Keys) {

                if (e.NameWithoutLocale.IsEquivalentTo("Portraits/"+ npcPortrait))
                {
                    e.LoadFromModFile<Texture2D>(activePortraits[npcPortrait], AssetLoadPriority.High+1234); //give higher load priority over other portrait loads
                }
            }
            foreach (HDTextureInfo info in hdTextures)
            {

                if (e.NameWithoutLocale.IsEquivalentTo(info.Target))
                {
                    
                    e.Edit(
                    (asset) =>
                    {
                        


                        ReplacedTexture replacement;

                        if (info.Target.Contains("farmer_") && info.HDTexture is not null)
                        {
                            replacement = new ReplacedTexture(asset.AsImage().Data, info.HDTexture, info, info.SpriteWidth, info.SpriteHeight);
                            IAssetDataForImage assetImage = asset.AsImage();

                            Color[] data = new Color[info.HDTexture.Width * info.HDTexture.Height];
                            info.HDTexture.GetData(data, 0, data.Length);
                            replacement.SetData(data);

                        }

                        else
                        {
                            replacement = new ReplacedTexture(asset.AsImage().Data, info.HDTexture, info);

                        }
                        asset.AsImage().ReplaceWith(replacement);
                    });
                    

                }
            }
        }


        private static bool spriteAlreadyDrawn = false;

        public static string DirectoryPath { get => directoryPath; set => directoryPath = value; }

        public static bool DrawReplacedTexture(SpriteBatch __instance, Texture2D texture, Rectangle destination, Rectangle? sourceRectangle, Color color, Vector2 origin, Vector2 scale, float rotation = 0f, SpriteEffects effects = SpriteEffects.None, float layerDepth = 0f)
        {
            
            //This is because of the overrides calling the higher-parameter method
            //It appears Draw 4/5 are the most commonly called, so it may be unnecessary to inject into the others
            if (/*spriteAlreadyDrawn ||*/ !sourceRectangle.HasValue)
            {
                return true;
            }
            
            if (texture is ReplacedTexture a && sourceRectangle != null && sourceRectangle.Value is Rectangle r)
            {
                Rectangle updatedDestination;
                Rectangle? updatedSource;
                Vector2 updatedOrigin;


                
                if (a.HDTextureInfo.HDTexture is null)
                {

                    //Nothing to replace
                    return true;
                }
                updatedDestination = new Rectangle(destination.X, destination.Y, (int)(a.HDTextureInfo.SpriteWidth * scale.X), (int)(a.HDTextureInfo.SpriteHeight * scale.Y));
                updatedSource = new Rectangle?(new Rectangle((int)(r.X), (int)(r.Y), (int)(a.HDTextureInfo.SpriteWidth), (int)(a.HDTextureInfo.SpriteHeight)));
                updatedOrigin = new Vector2(a.HDTextureInfo.SpriteOriginX, a.HDTextureInfo.SpriteOriginY);
                //Male/Female 'Breather' sprite
                // These are textures drawn on top of the NPC's chest, and stretched, to appear as if they are breathing
                if ((r.Width == 8 && r.Height == 8) || (r.Width == 8 && r.Height == 4))
                {
                    return false;
                    
                }
                else
                {
                    //The destination is the X,Y coordinates of the Origin.
                    //Therefore, if you increase the width of the sprite, you have to make sure to update the origin, but don't need to alter the destination.
                    updatedDestination = new Rectangle(destination.X, destination.Y, (int)(a.HDTextureInfo.SpriteWidth * scale.X), (int)(a.HDTextureInfo.SpriteHeight * scale.Y));
                    if (r.X == 0)
                    {
                        updatedSource = new Rectangle?(new Rectangle((int)((r.X) * a.HDTextureInfo.SpriteWidth), (int)(r.Y), (int)(a.HDTextureInfo.SpriteWidth), (int)(a.HDTextureInfo.SpriteHeight)));
                    }
                    else if (r.X == 16)
                    {
                        updatedSource = new Rectangle?(new Rectangle((int)(a.HDTextureInfo.SpriteWidth), (int)(r.Y), (int)(a.HDTextureInfo.SpriteWidth), (int)(a.HDTextureInfo.SpriteHeight)));

                    }
                    else if (r.X == 32)
                    {
                        updatedSource = new Rectangle?(new Rectangle((int)(2 * a.HDTextureInfo.SpriteWidth), (int)(r.Y), (int)(a.HDTextureInfo.SpriteWidth), (int)(a.HDTextureInfo.SpriteHeight)));

                    }
                    else
                    {
                        updatedSource = new Rectangle?(new Rectangle((int)(3 * a.HDTextureInfo.SpriteWidth), (int)(r.Y), (int)(a.HDTextureInfo.SpriteWidth), (int)(a.HDTextureInfo.SpriteHeight)));
                    }

                    //The Origin of the sprite is in the lower center, lower 3/4s, and looks to be around where the upper-center of the shadow is drawn
                    updatedOrigin = new Vector2(a.HDTextureInfo.SpriteOriginX, a.HDTextureInfo.SpriteOriginY);

                    
                    if (r.Height == 24)
                    {
                        updatedDestination = new Rectangle(destination.X, destination.Y, (int)(a.HDTextureInfo.SpriteWidth * scale.X), (int)(a.HDTextureInfo.SpriteHeight * scale.Y));
                        updatedSource = new Rectangle?(new Rectangle(0, 0, (int)(a.HDTextureInfo.SpriteWidth), (int)(a.HDTextureInfo.SpriteHeight)));
                        if (origin.X == 8 && origin.Y == 12)
                        {
                            // Social Menu
                            updatedOrigin = new Vector2(a.HDTextureInfo.SpriteOriginX, (int)(a.HDTextureInfo.SpriteOriginY));

                        }
                        else
                        {
                            //Calendar
                            updatedOrigin = new Vector2(16, 34);
                        }
                    }
                }

                if (a.HDTextureInfo.IsFarmer)
                {
                    __instance.Draw(a, updatedDestination, updatedSource, color, rotation, updatedOrigin, effects, layerDepth);
                }
                else
                {
                    __instance.Draw(a.NewTexture, updatedDestination, updatedSource, color, rotation, updatedOrigin, effects, layerDepth);
                }
                spriteAlreadyDrawn = false;
                return false;
            }

            return true;

        }

        public static bool Draw(SpriteBatch __instance, Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, SpriteEffects effects, float layerDepth)
        {
            

            return DrawReplacedTexture(__instance, texture, destinationRectangle, sourceRectangle, color, origin, Vector2.One, rotation, effects, layerDepth);
        }
        public static bool Draw(SpriteBatch __instance, Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color)
        {

            return DrawReplacedTexture(__instance, texture, destinationRectangle, sourceRectangle, color, Vector2.Zero, Vector2.One);
        }
        public static bool Draw(SpriteBatch __instance, Texture2D texture, Rectangle destinationRectangle, Color color)
        {
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            return DrawReplacedTexture(__instance, texture, destinationRectangle, sourceRectangle, color, Vector2.Zero, Vector2.One);
        }
        public static bool Draw(SpriteBatch __instance, Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
        {
            sourceRectangle = sourceRectangle.HasValue ? sourceRectangle.Value : new Rectangle(0, 0, texture.Width, texture.Height);
            return DrawReplacedTexture(__instance, texture, new Rectangle((int)(position.X), (int)(position.Y), (int)(sourceRectangle.Value.Width * scale.X), (int)(sourceRectangle.Value.Height * scale.Y)), sourceRectangle, color, origin, scale, rotation, effects, layerDepth);
        }
        public static bool Draw(SpriteBatch __instance, Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
        {
            sourceRectangle = sourceRectangle.HasValue ? sourceRectangle.Value : new Rectangle(0, 0, texture.Width, texture.Height);
            return DrawReplacedTexture(__instance, texture, new Rectangle((int)(position.X), (int)(position.Y), (int)(sourceRectangle.Value.Width * scale), (int)(sourceRectangle.Value.Height * scale)), sourceRectangle, color, origin, new Vector2(scale), rotation, effects, layerDepth);
        }
        public static bool Draw(SpriteBatch __instance, Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color)
        {
            sourceRectangle = sourceRectangle.HasValue ? sourceRectangle.Value : new Rectangle(0, 0, texture.Width, texture.Height);
            return DrawReplacedTexture(__instance, texture, new Rectangle((int)(position.X), (int)(position.Y), (int)(sourceRectangle.Value.Width), (int)(sourceRectangle.Value.Height)), sourceRectangle, color, Vector2.Zero, Vector2.One);
        }
        public static bool Draw(SpriteBatch __instance, Texture2D texture, Vector2 position, Color color)
        {
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            return DrawReplacedTexture(__instance, texture, new Rectangle((int)(position.X), (int)(position.Y), (int)(texture.Width), (int)(texture.Height)), sourceRectangle, color, Vector2.Zero, Vector2.One);

        }
    }
}