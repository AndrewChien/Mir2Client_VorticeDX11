using Client.MirObjects;
using System;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Vortice;
using Vortice.Direct2D1.Effects;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Client.MirGraphics
{
    public static class Libraries
    {
        public static bool Loaded;
        public static int Count, Progress;

        public static readonly MLibrary
            ChrSel = new MLibrary(Settings.DataPath + "ChrSel"),
            Prguse = new MLibrary(Settings.DataPath + "Prguse"),
            Prguse2 = new MLibrary(Settings.DataPath + "Prguse2"),
            Prguse3 = new MLibrary(Settings.DataPath + "Prguse3"),
            UI_32bit = new MLibrary(Settings.DataPath + "UI_32bit"),
            StateitemEffect = new MLibrary(Settings.DataPath + "StateitemEffect"),
            BuffIcon = new MLibrary(Settings.DataPath + "BuffIcon"),
            Help = new MLibrary(Settings.DataPath + "Help"),
            MiniMap = new MLibrary(Settings.DataPath + "MMap"),
            MapLinkIcon = new MLibrary(Settings.DataPath + "MapLinkIcon"),
            Title = new MLibrary(Settings.DataPath + "Title"),
            MagIcon = new MLibrary(Settings.DataPath + "MagIcon"),
            MagIcon2 = new MLibrary(Settings.DataPath + "MagIcon2"),
            Magic = new MLibrary(Settings.DataPath + "Magic"),
            Magic2 = new MLibrary(Settings.DataPath + "Magic2"),
            Magic3 = new MLibrary(Settings.DataPath + "Magic3"),
            Magic_32bit = new MLibrary(Settings.DataPath + "Magic_32bit"),
            Effect = new MLibrary(Settings.DataPath + "Effect"),
            Effect2 = new MLibrary(Settings.DataPath + "Effect2"),
            Effect_32bit = new MLibrary(Settings.DataPath + "Effect_32bit"),
            MagicC = new MLibrary(Settings.DataPath + "MagicC"),
            GuildSkill = new MLibrary(Settings.DataPath + "GuildSkill"),
            Weather = new MLibrary(Settings.DataPath + "Weather");

        public static readonly MLibrary
            Background = new MLibrary(Settings.DataPath + "Background");


        public static readonly MLibrary
            Dragon = new MLibrary(Settings.DataPath + "Dragon");

        //Map
        public static readonly MLibrary[] MapLibs = new MLibrary[400];

        //Items
        public static readonly MLibrary
            Items = new MLibrary(Settings.DataPath + "Items"),
            StateItems = new MLibrary(Settings.DataPath + "StateItem"),
            FloorItems = new MLibrary(Settings.DataPath + "DNItems");

        //Deco
        public static readonly MLibrary
            Deco = new MLibrary(Settings.DataPath + "Deco");

        public static MLibrary[] CArmours,
                                          CWeapons,
										  CWeaponEffect,
										  CHair,
                                          CHumEffect,
                                          AArmours,
                                          AWeaponsL,
                                          AWeaponsR,
                                          AWeaponEffectL,
                                          AWeaponEffectR,
                                          AHair,
                                          AHumEffect,
                                          ARArmours,
                                          ARWeaponsEffect,
                                          ARWeaponsEffectS,
                                          ARWeapons,
                                          ARWeaponsS,
                                          ARHair,
                                          ARHumEffect,
                                          Monsters,
                                          Gates,
                                          Flags,
                                          Siege,
                                          Mounts,
                                          NPCs,
                                          Fishing,
                                          Pets,
                                          Transform,
                                          TransformMounts,
                                          TransformEffect,
                                          TransformWeaponEffect;

        static Libraries()
        {
            //Wiz/War/Tao
            InitLibrary(ref CArmours, Settings.CArmourPath, "00");
            InitLibrary(ref CHair, Settings.CHairPath, "00");
            InitLibrary(ref CWeapons, Settings.CWeaponPath, "00");
            InitLibrary(ref CWeaponEffect, Settings.CWeaponEffectPath, "00");
            InitLibrary(ref CHumEffect, Settings.CHumEffectPath, "00");

            //Assassin
            InitLibrary(ref AArmours, Settings.AArmourPath, "00");
            InitLibrary(ref AHair, Settings.AHairPath, "00");
            InitLibrary(ref AWeaponsL, Settings.AWeaponPath, "00", " L");
            InitLibrary(ref AWeaponsR, Settings.AWeaponPath, "00", " R");
            InitLibrary(ref AWeaponEffectL, Settings.AWeaponEffectPath, "00", " L");
            InitLibrary(ref AWeaponEffectR, Settings.AWeaponEffectPath, "00", " R");
            InitLibrary(ref AHumEffect, Settings.AHumEffectPath, "00");

            //Archer
            InitLibrary(ref ARArmours, Settings.ARArmourPath, "00");
            InitLibrary(ref ARHair, Settings.ARHairPath, "00");
            InitLibrary(ref ARWeapons, Settings.ARWeaponPath, "00");
            InitLibrary(ref ARWeaponsS, Settings.ARWeaponPath, "00", " S");
            InitLibrary(ref ARWeaponsEffect, Settings.ARWeaponEffectPath, "00");
            InitLibrary(ref ARWeaponsEffectS, Settings.ARWeaponEffectPath, "00", " S");
            InitLibrary(ref ARHumEffect, Settings.ARHumEffectPath, "00");

            //Other
            InitLibrary(ref Monsters, Settings.MonsterPath, "000");
            InitLibrary(ref Gates, Settings.GatePath, "00");
            InitLibrary(ref Flags, Settings.FlagPath, "00");
            InitLibrary(ref Siege, Settings.SiegePath, "00");
            InitLibrary(ref NPCs, Settings.NPCPath, "00");
            InitLibrary(ref Mounts, Settings.MountPath, "00");
            InitLibrary(ref Fishing, Settings.FishingPath, "00");
            InitLibrary(ref Pets, Settings.PetsPath, "00");
            InitLibrary(ref Transform, Settings.TransformPath, "00");
            InitLibrary(ref TransformMounts, Settings.TransformMountsPath, "00");
            InitLibrary(ref TransformEffect, Settings.TransformEffectPath, "00");
            InitLibrary(ref TransformWeaponEffect, Settings.TransformWeaponEffectPath, "00");

            #region Maplibs
            //wemade mir2 (allowed from 0-99)
            MapLibs[0] = new MLibrary(Settings.DataPath + "Map\\WemadeMir2\\Tiles");
            MapLibs[1] = new MLibrary(Settings.DataPath + "Map\\WemadeMir2\\Smtiles");
            MapLibs[2] = new MLibrary(Settings.DataPath + "Map\\WemadeMir2\\Objects");
            for (int i = 2; i < 28; i++)
            {
                MapLibs[i + 1] = new MLibrary(Settings.DataPath + "Map\\WemadeMir2\\Objects" + i.ToString());
            }
            MapLibs[90] = new MLibrary(Settings.DataPath + "Map\\WemadeMir2\\Objects_32bit");

            //shanda mir2 (allowed from 100-199)
            MapLibs[100] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\Tiles");
            for (int i = 1; i < 10; i++)
            {
                MapLibs[100 + i] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\Tiles" + (i + 1));
            }
            MapLibs[110] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\SmTiles");
            for (int i = 1; i < 10; i++)
            {
                MapLibs[110 + i] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\SmTiles" + (i + 1));
            }
            MapLibs[120] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\Objects");
            for (int i = 1; i < 31; i++)
            {
                MapLibs[120 + i] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\Objects" + (i + 1));
            }
            MapLibs[190] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\AniTiles1");
            //wemade mir3 (allowed from 200-299)
            string[] Mapstate = { "", "wood\\", "sand\\", "snow\\", "forest\\"};
            for (int i = 0; i < Mapstate.Length; i++)
            {
                MapLibs[200 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Tilesc");
                MapLibs[201 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Tiles30c");
                MapLibs[202 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Tiles5c");
                MapLibs[203 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Smtilesc");
                MapLibs[204 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Housesc");
                MapLibs[205 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Cliffsc");
                MapLibs[206 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Dungeonsc");
                MapLibs[207 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Innersc");
                MapLibs[208 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Furnituresc");
                MapLibs[209 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Wallsc");
                MapLibs[210 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "smObjectsc");
                MapLibs[211 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Animationsc");
                MapLibs[212 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Object1c");
                MapLibs[213 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Object2c");
            }
            Mapstate = new string[] { "", "wood", "sand", "snow", "forest"};
            //shanda mir3 (allowed from 300-399)
            for (int i = 0; i < Mapstate.Length; i++)
            {
                MapLibs[300 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Tilesc" + Mapstate[i]);
                MapLibs[301 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Tiles30c" + Mapstate[i]);
                MapLibs[302 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Tiles5c" + Mapstate[i]);
                MapLibs[303 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Smtilesc" + Mapstate[i]);
                MapLibs[304 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Housesc" + Mapstate[i]);
                MapLibs[305 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Cliffsc" + Mapstate[i]);
                MapLibs[306 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Dungeonsc" + Mapstate[i]);
                MapLibs[307 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Innersc" + Mapstate[i]);
                MapLibs[308 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Furnituresc" + Mapstate[i]);
                MapLibs[309 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Wallsc" + Mapstate[i]);
                MapLibs[310 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "smObjectsc" + Mapstate[i]);
                MapLibs[311 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Animationsc" + Mapstate[i]);
                MapLibs[312 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Object1c" + Mapstate[i]);
                MapLibs[313 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Object2c" + Mapstate[i]);
            }
            #endregion

            LoadLibraries();

            Thread thread = new Thread(LoadGameLibraries) { IsBackground = true };
            thread.Start();
        }

        static void InitLibrary(ref MLibrary[] library, string path, string toStringValue, string suffix = "")
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var allFiles = Directory.GetFiles(path, "*" + suffix + MLibrary.Extention, SearchOption.TopDirectoryOnly).OrderBy(x => int.Parse(Regex.Match(x, @"\d+").Value));

            var lastFile = allFiles.Count() > 0 ? Path.GetFileName(allFiles.Last()) : "0";

            var count = int.Parse(Regex.Match(lastFile, @"\d+").Value) + 1;

            library = new MLibrary[count];

            for (int i = 0; i < count; i++)
            {
                library[i] = new MLibrary(path + i.ToString(toStringValue) + suffix);
            }
        }

        static void LoadLibraries()
        {
            ChrSel.Initialize();
            Progress++;

            Prguse.Initialize();
            Progress++;

            Prguse2.Initialize();
            Progress++;

            Prguse3.Initialize();
            Progress++;

            UI_32bit.Initialize();
            Progress++;

            Title.Initialize();
            Progress++;

            StateitemEffect.Initialize();
            Progress++;
        }

        private static void LoadGameLibraries()
        {
            Count = MapLibs.Length + Monsters.Length + Gates.Length + Flags.Length + Siege.Length + NPCs.Length + CArmours.Length +
                CHair.Length + CWeapons.Length + CWeaponEffect.Length + AArmours.Length + AHair.Length + AWeaponsL.Length + AWeaponsR.Length +
                AWeaponEffectL.Length + AWeaponEffectR.Length +ARArmours.Length + ARHair.Length + ARWeapons.Length + ARWeaponsS.Length + ARWeaponsEffect.Length + ARWeaponsEffectS.Length +
                CHumEffect.Length + AHumEffect.Length + ARHumEffect.Length + Mounts.Length + Fishing.Length + Pets.Length +
                Transform.Length + TransformMounts.Length + TransformEffect.Length + TransformWeaponEffect.Length + 18;

            Dragon.Initialize();
            Progress++;

            BuffIcon.Initialize();
            Progress++;

            Help.Initialize();
            Progress++;

            MiniMap.Initialize();
            Progress++;
            MapLinkIcon.Initialize();
            Progress++;

            MagIcon.Initialize();
            Progress++;
            MagIcon2.Initialize();
            Progress++;

            Magic.Initialize();
            Progress++;
            Magic2.Initialize();
            Progress++;
            Magic3.Initialize();
            Progress++;
            Magic_32bit.Initialize();
            Progress++;
            MagicC.Initialize();
            Progress++;

            Effect.Initialize();
            Progress++;
            Effect2.Initialize();
            Progress++;
            Effect_32bit.Initialize();
            Progress++;

            Weather.Initialize();
            Progress++;

            GuildSkill.Initialize();
            Progress++;

            Background.Initialize();
            Progress++;

            Deco.Initialize();
            Progress++;

            Items.Initialize();
            Progress++;
            StateItems.Initialize();
            Progress++;
            FloorItems.Initialize();
            Progress++;

            for (int i = 0; i < MapLibs.Length; i++)
            {
                if (MapLibs[i] == null)
                    MapLibs[i] = new MLibrary("");
                else
                    MapLibs[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Monsters.Length; i++)
            {
                Monsters[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Gates.Length; i++)
            {
                Gates[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Flags.Length; i++)
            {
                Flags[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Siege.Length; i++)
            {
                Siege[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < NPCs.Length; i++)
            {
                NPCs[i].Initialize();
                Progress++;
            }


            for (int i = 0; i < CArmours.Length; i++)
            {
                CArmours[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < CHair.Length; i++)
            {
                CHair[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < CWeapons.Length; i++)
            {
                CWeapons[i].Initialize();
                Progress++;
            }

			for (int i = 0; i < CWeaponEffect.Length; i++)
			{
				CWeaponEffect[i].Initialize();
				Progress++;
			}

			for (int i = 0; i < AArmours.Length; i++)
            {
                AArmours[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < AHair.Length; i++)
            {
                AHair[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < AWeaponsL.Length; i++)
            {
                AWeaponsL[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < AWeaponsR.Length; i++)
            {
                AWeaponsR[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < AWeaponEffectL.Length; i++)
            {
                AWeaponEffectL[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < AWeaponEffectR.Length; i++)
            {
                AWeaponEffectR[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARArmours.Length; i++)
            {
                ARArmours[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARHair.Length; i++)
            {
                ARHair[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARWeapons.Length; i++)
            {
                ARWeapons[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARWeaponsS.Length; i++)
            {
                ARWeaponsS[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARWeaponsEffect.Length; i++)
            {
                ARWeaponsEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARWeaponsEffectS.Length; i++)
            {
                ARWeaponsEffectS[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < CHumEffect.Length; i++)
            {
                CHumEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < AHumEffect.Length; i++)
            {
                AHumEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARHumEffect.Length; i++)
            {
                ARHumEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Mounts.Length; i++)
            {
                Mounts[i].Initialize();
                Progress++;
            }


            for (int i = 0; i < Fishing.Length; i++)
            {
                Fishing[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Pets.Length; i++)
            {
                Pets[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Transform.Length; i++)
            {
                Transform[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < TransformEffect.Length; i++)
            {
                TransformEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < TransformWeaponEffect.Length; i++)
            {
                TransformWeaponEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < TransformMounts.Length; i++)
            {
                TransformMounts[i].Initialize();
                Progress++;
            }
            
            Loaded = true;
        }

    }

    public sealed class MLibrary
    {
        public const string Extention = ".Lib";
        public const int LibVersion = 3;

        private readonly string _fileName;

        public MImage[] _images;
        private FrameSet _frames;
        private int[] _indexList;
        private int _count;
        private bool _initialized;

        private BinaryReader _reader;
        private FileStream _fStream;

        public FrameSet Frames
        {
            get { return _frames; }
        }

        public MLibrary(string filename)
        {
            _fileName = Path.ChangeExtension(filename, Extention);
        }

        public void Initialize()
        {
            _initialized = true;

            if (!File.Exists(_fileName))
                return;

            try
            {
                _fStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read);
                _reader = new BinaryReader(_fStream);
                int currentVersion = _reader.ReadInt32();
                if (currentVersion < 2)
                {
                    System.Windows.Forms.MessageBox.Show("版本错误，lib可用版本： " + LibVersion.ToString() + " 找到的版本： " + currentVersion.ToString() + ".", _fileName, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxDefaultButton.Button1);
                    System.Windows.Forms.Application.Exit();
                    return;
                }
                _count = _reader.ReadInt32();

                int frameSeek = 0;
                if (currentVersion >= 3)
                {
                    frameSeek = _reader.ReadInt32();
                }

                _images = new MImage[_count];
                _indexList = new int[_count];

                for (int i = 0; i < _count; i++)
                    _indexList[i] = _reader.ReadInt32();

                if (currentVersion >= 3)
                {
                    _fStream.Seek(frameSeek, SeekOrigin.Begin);

                    var frameCount = _reader.ReadInt32();

                    if (frameCount > 0)
                    {
                        _frames = new FrameSet();
                        for (int i = 0; i < frameCount; i++)
                        {
                            _frames.Add((MirAction)_reader.ReadByte(), new Frame(_reader));
                        }
                    }
                }
            }
            catch (Exception)
            {
                _initialized = false;
                throw;
            }
        }

        /// <summary>
        /// 检查该索引对应的图片有无问题
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool CheckImage(int index)
        {
            if (!_initialized)
                Initialize();

            if (_images == null || index < 0 || index >= _images.Length)
                return false;

            if (_images[index] == null)
            {
                _fStream.Position = _indexList[index];
                _images[index] = new MImage(_reader);
            }
            MImage mi = _images[index];
            if (!mi.TextureValid)
            {
                if ((mi.Width == 0) || (mi.Height == 0))
                    return false;
                _fStream.Seek(_indexList[index] + 17, SeekOrigin.Begin);
                mi.CreateTexture(_reader);
            }

            return true;
        }

        public Point GetOffSet(int index)
        {
            if (!_initialized) Initialize();

            if (_images == null || index < 0 || index >= _images.Length)
                return Point.Empty;

            if (_images[index] == null)
            {
                _fStream.Seek(_indexList[index], SeekOrigin.Begin);
                _images[index] = new MImage(_reader);
            }

            return new Point(_images[index].X, _images[index].Y);
        }
        public Size GetSize(int index)
        {
            if (!_initialized) Initialize();
            if (_images == null || index < 0 || index >= _images.Length)
                return Size.Empty;

            if (_images[index] == null)
            {
                _fStream.Seek(_indexList[index], SeekOrigin.Begin);
                _images[index] = new MImage(_reader);
            }

            return new Size(_images[index].Width, _images[index].Height);
        }
        public Size GetTrueSize(int index)
        {
            if (!_initialized)
                Initialize();

            if (_images == null || index < 0 || index >= _images.Length)
                return Size.Empty;

            if (_images[index] == null)
            {
                _fStream.Position = _indexList[index];
                _images[index] = new MImage(_reader);
            }
            MImage mi = _images[index];
            if (mi.TrueSize.IsEmpty)
            {
                if (!mi.TextureValid)
                {
                    if ((mi.Width == 0) || (mi.Height == 0))
                        return Size.Empty;

                    _fStream.Seek(_indexList[index] + 17, SeekOrigin.Begin);
                    mi.CreateTexture(_reader);
                }
                return mi.GetTrueSize();
            }
            return mi.TrueSize;
        }

        /// <summary>
        /// 在指定坐标处显示制定索引号图片
        /// </summary>
        /// <param name="index"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Draw(int index, int x, int y)
        {
            //CMain.SaveError(DXManager.PrintParentMethod());//太多

            if (x >= Settings.ScreenWidth || y >= Settings.ScreenHeight)
                return;

            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            if (x + mi.Width < 0 || y + mi.Height < 0)
                return;


            DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)x, (float)y, 0.0F), Color.White);

            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }
        public void Draw(int index, Point point, Color colour, bool offSet = false)
        {
            //CMain.SaveError(DXManager.PrintParentMethod());

            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            if (offSet) point.Offset(mi.X, mi.Y);

            if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
                return;

            DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X, (float)point.Y, 0.0F), colour);

            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }

        public void Draw(int index, Point point, Color colour, bool offSet, float opacity)
        {
            //CMain.SaveError(DXManager.PrintParentMethod());

            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            if (offSet) 
                point.Offset(mi.X, mi.Y);//坐标平移量（图片向右向下偏移量）

            if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
                return;

            //debug:
            //CMain.SaveError($"MLibrary.Draw_1()：DrawOpaque：图片位置{_fileName}图片编号{index}，0-0-{mi.Width}-{mi.Height},{point.X}-{point.Y}-0,{colour},{opacity}");

            DXManager.DrawOpaque(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X, (float)point.Y, 0.0F), colour, opacity); 

            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }

        public void DrawBlend(int index, Point point, Color colour, bool offSet = false, float rate = 1)
        {
            //CMain.SaveError(DXManager.PrintParentMethod());

            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            if (offSet) point.Offset(mi.X, mi.Y);

            if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
                return;

            bool oldBlend = DXManager.Blending;
            DXManager.SetBlend(true, rate);

            DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X, (float)point.Y, 0.0F), colour);

            DXManager.SetBlend(oldBlend);
            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }
        public void Draw(int index, Rectangle section, Point point, Color colour, bool offSet)
        {
            //CMain.SaveError(DXManager.PrintParentMethod());

            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            if (offSet) point.Offset(mi.X, mi.Y);


            if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
                return;

            if (section.Right > mi.Width)
                section.Width -= section.Right - mi.Width;

            if (section.Bottom > mi.Height)
                section.Height -= section.Bottom - mi.Height;

            DXManager.Draw(mi.Image, section, new Vector3((float)point.X, (float)point.Y, 0.0F), colour);

            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }
        public void Draw(int index, Rectangle section, Point point, Color colour, float opacity)
        {
            //CMain.SaveError(DXManager.PrintParentMethod());

            if (!CheckImage(index))
                return;

            MImage mi = _images[index];


            if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
                return;

            if (section.Right > mi.Width)
                section.Width -= section.Right - mi.Width;

            if (section.Bottom > mi.Height)
                section.Height -= section.Bottom - mi.Height;

            //debug:
            //CMain.SaveError($"MLibrary.Draw_2()：DrawOpaque：图片位置{_fileName}图片编号{index}，0-0-{mi.Width}-{mi.Height},{point.X}-{point.Y}-0,{colour},{opacity}");

            DXManager.DrawOpaque(mi.Image, section, new Vector3((float)point.X, (float)point.Y, 0.0F), colour, opacity); 

            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }
        public void Draw(int index, Point point, Size size, Color colour)
        {
            //CMain.SaveError(DXManager.PrintParentMethod());

            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + size.Width < 0 || point.Y + size.Height < 0)
                return;

            float scaleX = (float)size.Width / mi.Width;
            float scaleY = (float)size.Height / mi.Height;

            //Matrix matrix = Matrix.Scaling(scaleX, scaleY, 0);
            Matrix4x4 matrix = DXManager.MatrixScaling0(scaleX, scaleY);

            //DXManager.Sprite.Transform = matrix;
            DXManager.SpriteTransform(matrix);

            DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X / scaleX, (float)point.Y / scaleY, 0.0F), Color.White);

            //DXManager.Sprite.Transform = Matrix.Identity;
            DXManager.SpriteTransform(Matrix4x4.Identity);

            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }

        public void DrawTinted(int index, Point point, Color colour, Color Tint, bool offSet = false)
        {
            //CMain.SaveError(DXManager.PrintParentMethod());

            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            if (offSet) point.Offset(mi.X, mi.Y);

            if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
                return;

            DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X, (float)point.Y, 0.0F), colour);

            if (mi.HasMask)
            {
                DXManager.Draw(mi.MaskImage, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X, (float)point.Y, 0.0F), Tint);
            }

            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }

        public void DrawUp(int index, int x, int y)
        {
            //CMain.SaveError(DXManager.PrintParentMethod());

            if (x >= Settings.ScreenWidth)
                return;

            if (!CheckImage(index))
                return;

            MImage mi = _images[index];
            y -= mi.Height;
            if (y >= Settings.ScreenHeight)
                return;
            if (x + mi.Width < 0 || y + mi.Height < 0)
                return;

            DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3(x, y, 0.0F), Color.White);

            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }
        public void DrawUpBlend(int index, Point point)
        {
            //CMain.SaveError(DXManager.PrintParentMethod());

            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            point.Y -= mi.Height;


            if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
                return;

            bool oldBlend = DXManager.Blending;
            DXManager.SetBlend(true, 1);

            DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X, (float)point.Y, 0.0F), Color.White);

            DXManager.SetBlend(oldBlend);
            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }

        public bool VisiblePixel(int index, Point point, bool accuate)
        {
            if (!CheckImage(index))
                return false;

            if (accuate)
                return _images[index].VisiblePixel(point);

            int accuracy = 2;

            for (int x = -accuracy; x <= accuracy; x++)
                for (int y = -accuracy; y <= accuracy; y++)
                    if (_images[index].VisiblePixel(new Point(point.X + x, point.Y + y)))
                        return true;

            return false;
        }
    }

    public sealed class MImage
    {
        public short Width, Height, X, Y, ShadowX, ShadowY;
        public byte Shadow;
        public int Length;

        public bool TextureValid;
        //public Texture Image;
        //lyo：Texture转换示例
        public Vortice.Direct3D11.ID3D11Texture2D Image;

        //layer 2:
        public short MaskWidth, MaskHeight, MaskX, MaskY;
        public int MaskLength;

        //public Texture MaskImage;
        //lyo：Texture转换示例
        public Vortice.Direct3D11.ID3D11Texture2D MaskImage;
        public Boolean HasMask;

        public long CleanTime;
        public Size TrueSize;

        public unsafe byte* Data;

        public MImage(BinaryReader reader)
        {
            //read layer 1
            Width = reader.ReadInt16();
            Height = reader.ReadInt16();
            X = reader.ReadInt16();
            Y = reader.ReadInt16();
            ShadowX = reader.ReadInt16();
            ShadowY = reader.ReadInt16();
            Shadow = reader.ReadByte();
            Length = reader.ReadInt32();

            //check if there's a second layer and read it
            HasMask = ((Shadow >> 7) == 1) ? true : false;
            if (HasMask)
            {
                reader.ReadBytes(Length);
                MaskWidth = reader.ReadInt16();
                MaskHeight = reader.ReadInt16();
                MaskX = reader.ReadInt16();
                MaskY = reader.ReadInt16();
                MaskLength = reader.ReadInt32();
            }
        }


        #region 所有的尝试

        private unsafe void UseUpdateSubresourceCopy(BinaryReader reader)
        {
            int w = Width;// + (4 - Width % 4) % 4;
            int h = Height;// + (4 - Height % 4) % 4;
            var textureDesc = new Vortice.Direct3D11.Texture2DDescription
            {
                Width = (uint)w,
                Height = (uint)h,
                MipLevels = 1,
                ArraySize = 1,
                Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
                SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,
                BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource,
                CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.Write,
                MiscFlags = Vortice.Direct3D11.ResourceOptionFlags.None,
            };
            Image = DXManager.Device.CreateTexture2D(textureDesc);
            var stream = DXManager.DeviceContext.Map(Image, 0, Vortice.Direct3D11.MapMode.WriteDiscard,
                Vortice.Direct3D11.MapFlags.None);

            // 2. 解压数据到托管内存
            var compressedData = reader.ReadBytes(Length);
            byte[] decompressedData;
            using (var inputStream = new MemoryStream(compressedData))
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                gzipStream.CopyTo(outputStream);
                decompressedData = outputStream.ToArray();
            }
            // 3. 将数据从托管内存复制到非托管内存
            Marshal.Copy(decompressedData, 0, stream.DataPointer, decompressedData.Length);
            // 4. 创建纹理并更新资源
            DXManager.DeviceContext.UpdateSubresource(
                dstResource: Image,
                dstSubresource: 0,
                dstBox: null,
                srcData: stream.DataPointer,
                srcRowPitch: stream.RowPitch,
                srcDepthPitch: 0);
            DXManager.DeviceContext.Unmap(Image, 0);

        }

        private unsafe void UseMashalArrayCopy(BinaryReader reader)
        {
            int w = Width;// + (4 - Width % 4) % 4;
            int h = Height;// + (4 - Height % 4) % 4;

            // 1. 解压数据到内存流
            byte[] decompressedData;
            using (var compressedStream = new MemoryStream(reader.ReadBytes(Length)))
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var decompressedStream = new MemoryStream())
            {
                gzipStream.CopyTo(decompressedStream);
                decompressedData = decompressedStream.ToArray();
            }

            // 2. 创建D3D11纹理
            var textureDesc = new Texture2DDescription
            {
                Width = (uint)w,
                Height = (uint)h,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ShaderResource,
                CPUAccessFlags = CpuAccessFlags.Write
            };

            Image = DXManager.Device.CreateTexture2D(textureDesc);

            // 3. 映射并写入纹理数据
            var stream = DXManager.DeviceContext.Map(Image, 0, MapMode.WriteDiscard, 0);
            Data = (byte*)stream.DataPointer;
            try
            {
                fixed (byte* srcPtr = decompressedData)
                {
                    Buffer.MemoryCopy(
                        source: srcPtr,
                        destination: stream.DataPointer.ToPointer(),
                        destinationSizeInBytes: (ulong)(stream.RowPitch * h),
                        sourceBytesToCopy: (ulong)decompressedData.Length);
                }
            }
            finally
            {
                DXManager.DeviceContext.Unmap(Image, 0);
            }


            DXManager.TextureList.Add(this);
            TextureValid = true;

            CleanTime = CMain.Time + Settings.CleanDelay;
        }

        private unsafe void UseOriginUngzip1(BinaryReader reader)
        {
            int w = Width;// + (4 - Width % 4) % 4;
            int h = Height;// + (4 - Height % 4) % 4;
                           //1、创建纹理
            var textureDesc = new Vortice.Direct3D11.Texture2DDescription
            {
                Width = (uint)w,
                Height = (uint)h,
                MipLevels = 1,
                ArraySize = 1,
                Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
                SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,
                BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource,
                CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.Write,
                //MiscFlags = Vortice.Direct3D11.ResourceOptionFlags.None,
            };
            Image = DXManager.Device.CreateTexture2D(textureDesc);

            //2、锁纹理
            var stream = DXManager.DeviceContext.Map(Image, 0, Vortice.Direct3D11.MapMode.WriteDiscard, 0);
            Data = (byte*)stream.DataPointer;

            //gzip解压
            byte[] textureData;
            using (MemoryStream ms = new MemoryStream())
            {
                DecompressImage(reader.ReadBytes(Length), ms);
                //sourceStream.CopyTo(ms);
                textureData = ms.ToArray();
            }

            //数组数据复制到MappedSubresource
            unsafe
            {
                byte* destPtr = (byte*)stream.DataPointer.ToPointer();
                fixed (byte* srcPtr = textureData)
                {
                    Buffer.MemoryCopy(srcPtr, destPtr, textureData.Length, textureData.Length);
                }
            }

            //// 或使用Marshal.Copy（需处理行对齐）（结果相同）
            //Marshal.Copy(textureData, 0, stream.DataPointer, textureData.Length);

            //6、解锁纹理
            DXManager.DeviceContext.Unmap(Image, 0);

            DXManager.TextureList.Add(this);
            TextureValid = true;

            CleanTime = CMain.Time + Settings.CleanDelay;
        }

        private unsafe void UseOriginUngzip2(BinaryReader reader)
        {
            //OldGzipCopy(reader);
            //return;

            int w = Width;// + (4 - Width % 4) % 4;
            int h = Height;// + (4 - Height % 4) % 4;

            //原：
            //Image = new Texture(DXManager.Device, w, h, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);
            //DataRectangle stream = Image.LockRectangle(0, LockFlags.Discard);
            //Data = (byte*)stream.Data.DataPointer;
            //DecompressImage(reader.ReadBytes(Length), stream.Data);
            //stream.Data.Dispose();
            //Image.UnlockRectangle(0);

            //1、创建纹理
            var textureDesc = new Vortice.Direct3D11.Texture2DDescription
            {
                Width = (uint)w,
                Height = (uint)h,
                MipLevels = 1,
                ArraySize = 1,
                Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
                SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,
                BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource,
                CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.Write,
                MiscFlags = Vortice.Direct3D11.ResourceOptionFlags.None,
            };
            Image = DXManager.Device.CreateTexture2D(textureDesc);

            //2、锁纹理
            var stream = DXManager.DeviceContext.Map(Image, 0, Vortice.Direct3D11.MapMode.WriteDiscard,
                Vortice.Direct3D11.MapFlags.None);

            //3、解压数据到托管内存

            byte[] decompressedData = DecompressImage(reader.ReadBytes(Length));

            //var compressedData = reader.ReadBytes(Length);
            //byte[] decompressedData;
            //using (var inputStream = new MemoryStream(compressedData))
            //using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            //using (var outputStream = new MemoryStream())
            //{
            //    gzipStream.CopyTo(outputStream);
            //    decompressedData = outputStream.ToArray();
            //}
            //4、将数据从托管内存复制到非托管内存
            Marshal.Copy(decompressedData, 0, stream.DataPointer, decompressedData.Length);
            //5、将数据从非托管内存更新到纹理
            DXManager.DeviceContext.UpdateSubresource(Image, 0, null, stream.DataPointer, stream.RowPitch, 0);

            //6、解锁纹理
            DXManager.DeviceContext.Unmap(Image, 0);

            if (HasMask)
            {
                reader.ReadBytes(12);
                w = Width;// + (4 - Width % 4) % 4;
                h = Height;// + (4 - Height % 4) % 4;

                //MaskImage = new Texture(DXManager.Device, w, h, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);
                MaskImage = DXManager.Device.CreateTexture2D(textureDesc);

                //stream = MaskImage.LockRectangle(0, LockFlags.Discard);
                stream = DXManager.DeviceContext.Map(MaskImage, 0, Vortice.Direct3D11.MapMode.WriteDiscard,
                    Vortice.Direct3D11.MapFlags.None);

                //DecompressImage(reader.ReadBytes(Length), stream);
                var compressedData1 = reader.ReadBytes(Length);
                byte[] decompressedData1;
                using (var inputStream = new MemoryStream(compressedData1))
                using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                using (var outputStream = new MemoryStream())
                {
                    gzipStream.CopyTo(outputStream);
                    decompressedData1 = outputStream.ToArray();
                }
                Marshal.Copy(decompressedData1, 0, stream.DataPointer, decompressedData1.Length);
                DXManager.DeviceContext.UpdateSubresource(MaskImage, 0, null, stream.DataPointer, stream.RowPitch, 0);

                //MaskImage.UnlockRectangle(0);
                DXManager.DeviceContext.Unmap(MaskImage, 0);
            }

            DXManager.TextureList.Add(this);
            TextureValid = true;

            CleanTime = CMain.Time + Settings.CleanDelay;
        }

        private unsafe void UseOriginUngzip3(BinaryReader reader)
        {
            //OldGzipCopy(reader);
            //return;

            int w = Width;// + (4 - Width % 4) % 4;
            int h = Height;// + (4 - Height % 4) % 4;

            //原：
            //Image = new Texture(DXManager.Device, w, h, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);
            //DataRectangle stream = Image.LockRectangle(0, LockFlags.Discard);
            //Data = (byte*)stream.Data.DataPointer;
            //DecompressImage(reader.ReadBytes(Length), stream.Data);
            //stream.Data.Dispose();
            //Image.UnlockRectangle(0);

            //1、创建纹理
            var textureDesc = new Vortice.Direct3D11.Texture2DDescription
            {
                Width = (uint)w,
                Height = (uint)h,
                MipLevels = 1,
                ArraySize = 1,
                Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
                SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,
                BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource,
                CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.Write,
                MiscFlags = Vortice.Direct3D11.ResourceOptionFlags.None,
            };
            Image = DXManager.Device.CreateTexture2D(textureDesc);

            //2、锁纹理
            var stream = DXManager.DeviceContext.Map(Image, 0, Vortice.Direct3D11.MapMode.WriteDiscard,
                Vortice.Direct3D11.MapFlags.None);

            //3、解压数据到托管内存

            byte[] decompressedData = DecompressImage(reader.ReadBytes(Length));

            //var compressedData = reader.ReadBytes(Length);
            //byte[] decompressedData;
            //using (var inputStream = new MemoryStream(compressedData))
            //using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            //using (var outputStream = new MemoryStream())
            //{
            //    gzipStream.CopyTo(outputStream);
            //    decompressedData = outputStream.ToArray();
            //}
            //4、将数据从托管内存复制到非托管内存
            Marshal.Copy(decompressedData, 0, stream.DataPointer, decompressedData.Length);
            //5、将数据从非托管内存更新到纹理
            DXManager.DeviceContext.UpdateSubresource(Image, 0, null, stream.DataPointer, stream.RowPitch, 0);

            //6、解锁纹理
            DXManager.DeviceContext.Unmap(Image, 0);

            if (HasMask)
            {
                reader.ReadBytes(12);
                w = Width;// + (4 - Width % 4) % 4;
                h = Height;// + (4 - Height % 4) % 4;

                //MaskImage = new Texture(DXManager.Device, w, h, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);
                MaskImage = DXManager.Device.CreateTexture2D(textureDesc);

                //stream = MaskImage.LockRectangle(0, LockFlags.Discard);
                stream = DXManager.DeviceContext.Map(MaskImage, 0, Vortice.Direct3D11.MapMode.WriteDiscard,
                    Vortice.Direct3D11.MapFlags.None);

                //DecompressImage(reader.ReadBytes(Length), stream);
                var compressedData1 = reader.ReadBytes(Length);
                byte[] decompressedData1;
                using (var inputStream = new MemoryStream(compressedData1))
                using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                using (var outputStream = new MemoryStream())
                {
                    gzipStream.CopyTo(outputStream);
                    decompressedData1 = outputStream.ToArray();
                }
                Marshal.Copy(decompressedData1, 0, stream.DataPointer, decompressedData1.Length);
                DXManager.DeviceContext.UpdateSubresource(MaskImage, 0, null, stream.DataPointer, stream.RowPitch, 0);

                //MaskImage.UnlockRectangle(0);
                DXManager.DeviceContext.Unmap(MaskImage, 0);
            }

            DXManager.TextureList.Add(this);
            TextureValid = true;

            CleanTime = CMain.Time + Settings.CleanDelay;
        }

        // Twiddle格式转换算法（Z形排列）
        private static int CalculateTwiddleIndex(int x, int y, int width)
        {
            int index = 0;
            for (int shift = 1; width > shift; shift <<= 1)
            {
                index |= (x & shift) << shift | (y & shift) << (shift + 1);
            }
            return index;
        }

        private unsafe void UseTwiddle(BinaryReader reader)
        {
            int w = Width;// + (4 - Width % 4) % 4;
            int h = Height;// + (4 - Height % 4) % 4;

            var format = Vortice.DXGI.Format.B8G8R8A8_UNorm;
            // 验证数据
            int bytesPerPixel = format == Format.B8G8R8A8_UNorm ? 4 : throw new NotSupportedException();


            //3、解压数据到托管内存
            var compressedData = reader.ReadBytes(Length);
            byte[] decompressedData;
            using (var inputStream = new MemoryStream(compressedData))
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                gzipStream.CopyTo(outputStream);
                decompressedData = outputStream.ToArray();
            }

            // 验证数据
            if (decompressedData.Length != w * h * bytesPerPixel)
                throw new ArgumentException("Image data size does not match texture dimensions");

            // 创建纹理描述
            var textureDesc = new Texture2DDescription
            {
                Width = (uint)w,
                Height = (uint)h,
                MipLevels = 1,
                ArraySize = 1,
                Format = format,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                CPUAccessFlags = CpuAccessFlags.None,
                MiscFlags = ResourceOptionFlags.None
            };

            // 创建临时staging纹理用于CPU写入
            var stagingDesc = textureDesc;
            stagingDesc.Usage = ResourceUsage.Staging;
            stagingDesc.BindFlags = BindFlags.None;
            stagingDesc.CPUAccessFlags = CpuAccessFlags.Write;

            var stagingTexture = DXManager.Device.CreateTexture2D(stagingDesc);
            //using (var stagingTexture = device.CreateTexture2D(stagingDesc))
            {
                // 映射纹理内存
                var mapped = DXManager.Device.ImmediateContext.Map(stagingTexture, 0, MapMode.Write, Vortice.Direct3D11.MapFlags.None);
                Data = (byte*)mapped.DataPointer;

                try
                {
                    unsafe
                    {
                        byte* pData = (byte*)mapped.DataPointer;
                        uint rowPitch = mapped.RowPitch;

                        // 按twiddle格式写入数据
                        for (int y = 0; y < h; y++)
                        {
                            for (int x = 0; x < w; x++)
                            {
                                // 计算twiddle索引
                                int twiddleIndex = CalculateTwiddleIndex(x, y, w);

                                // 计算目标内存位置
                                long destOffset = y * rowPitch + x * bytesPerPixel;

                                // 计算源数据位置
                                int srcOffset = twiddleIndex * bytesPerPixel;

                                // 复制像素数据
                                for (int i = 0; i < bytesPerPixel; i++)
                                {
                                    pData[destOffset + i] = decompressedData[srcOffset + i];
                                }
                            }
                        }
                    }
                }
                finally
                {
                    DXManager.Device.ImmediateContext.Unmap(stagingTexture, 0);
                }

                // 将数据从staging纹理复制到最终纹理
                DXManager.Device.ImmediateContext.CopyResource(stagingTexture, Image);
            }

            DXManager.TextureList.Add(this);
            TextureValid = true;

            CleanTime = CMain.Time + Settings.CleanDelay;
        }

        // 改进的Twiddle格式转换算法
        private static int CalculateTwiddleIndex(int x, int y, int width, int height)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                throw new ArgumentOutOfRangeException("Coordinates out of texture bounds");

            int index = 0;
            int mask = 1;
            while (mask < width || mask < height)
            {
                index |= (x & mask) << mask | (y & mask) << (mask + 1);
                mask <<= 1;
            }
            return Math.Min(index, width * height - 1); // 确保不超过最大索引
        }

        public unsafe void UseTwiddle2(BinaryReader reader)
        {
            var format = Vortice.DXGI.Format.B8G8R8A8_UNorm;
            uint w = (uint)Width;// + (4 - Width % 4) % 4;
            uint h = (uint)Height;// + (4 - Height % 4) % 4;
            //3、解压数据到托管内存
            var compressedData = reader.ReadBytes(Length);
            byte[] decompressedData;
            using (var inputStream = new MemoryStream(compressedData))
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                gzipStream.CopyTo(outputStream);
                decompressedData = outputStream.ToArray();
            }

            // 参数验证
            if (DXManager.Device == null) throw new ArgumentNullException(nameof(DXManager.Device));
            if (decompressedData == null) throw new ArgumentNullException(nameof(decompressedData));

            int bytesPerPixel = format == Format.B8G8R8A8_UNorm ? 4 :
                              format == Format.R8G8B8A8_UNorm ? 4 :
                              throw new NotSupportedException("Unsupported texture format");

            long expectedDataSize = w * h * bytesPerPixel;
            if (decompressedData.Length < expectedDataSize)
                throw new ArgumentException($"Image data size too small. Expected {expectedDataSize} bytes, got {decompressedData.Length}");

            // 创建或验证目标纹理
            if (Image == null)
            {
                var textureDesc = new Texture2DDescription
                {
                    //Width = w,
                    //Height = h,
                    //MipLevels = 1,
                    //ArraySize = 1,
                    //Format = format,
                    //SampleDescription = new SampleDescription(1, 0),
                    //Usage = ResourceUsage.Default,
                    //BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                    //CPUAccessFlags = CpuAccessFlags.None,
                    //MiscFlags = ResourceOptionFlags.None

                    Width = (uint)w,
                    Height = (uint)h,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
                    SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                    Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,
                    BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource,
                    CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.Write,
                    //MiscFlags = Vortice.Direct3D11.ResourceOptionFlags.None,
                };
                Image = DXManager.Device.CreateTexture2D(textureDesc);
            }
            else
            {
                var desc = Image.Description;
                if (desc.Width != w || desc.Height != h || desc.Format != format)
                    throw new ArgumentException("Destination texture dimensions/format mismatch");
            }

            // 创建staging纹理
            var stagingDesc = new Texture2DDescription
            {
                Width = w,
                Height = h,
                MipLevels = 1,
                ArraySize = 1,
                Format = format,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                //CPUAccessFlags = CpuAccessFlags.Write,
                CPUAccessFlags = CpuAccessFlags.Read,
                MiscFlags = ResourceOptionFlags.None
            };

            using (var stagingTexture = DXManager.Device.CreateTexture2D(stagingDesc))
            {
                // 映射纹理内存
                //var mapped = DXManager.Device.ImmediateContext.Map(stagingTexture, 0, MapMode.Write, Vortice.Direct3D11.MapFlags.None);
                var mapped = DXManager.Device.ImmediateContext.Map(stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
                Data = (byte*)mapped.DataPointer;

                try
                {
                    unsafe
                    {
                        byte* pData = (byte*)mapped.DataPointer;
                        uint rowPitch = mapped.RowPitch;

                        // 安全写入数据
                        for (int y = 0; y < h; y++)
                        {
                            for (int x = 0; x < w; x++)
                            {
                                int twiddleIndex = CalculateTwiddleIndex(x, y, (int)w, (int)h);
                                int srcOffset = twiddleIndex * bytesPerPixel;

                                // 边界检查
                                if (srcOffset + bytesPerPixel > decompressedData.Length)
                                    throw new IndexOutOfRangeException("Source data access out of bounds");

                                long destOffset = y * rowPitch + x * bytesPerPixel;

                                // 边界检查
                                if (destOffset + bytesPerPixel > rowPitch * h)
                                    throw new IndexOutOfRangeException("Destination texture access out of bounds");

                                // 安全复制像素数据
                                for (int i = 0; i < bytesPerPixel; i++)
                                {
                                    pData[destOffset + i] = decompressedData[srcOffset + i];
                                }
                            }
                        }
                    }
                }
                finally
                {
                    DXManager.Device.ImmediateContext.Unmap(stagingTexture, 0);
                }

                // 复制到目标纹理
                DXManager.Device.ImmediateContext.CopyResource(stagingTexture, Image);
            }

            DXManager.TextureList.Add(this);
            TextureValid = true;
            CleanTime = CMain.Time + Settings.CleanDelay;



            //检查图像完整2
            if (num2++ < count)
            {
                GrabImage.ShowImageFromGPU(DXManager.Device, Image);
            }
        }

        public unsafe void UseCppWay(BinaryReader reader)
        {
            //3、解压数据到托管内存
            var compressedData = reader.ReadBytes(Length);
            byte[] decompressedData;
            using (var inputStream = new MemoryStream(compressedData))
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                gzipStream.CopyTo(outputStream);
                decompressedData = outputStream.ToArray();
            }

            //检查图像完整1
            if (num1++ < count)
            {
                GrabImage.ShowImageFromCPU(decompressedData, Width, Height);
            }

            // 1. 创建纹理描述
            //D3D11_TEXTURE2D_DESC texDesc;
            //ZeroMemory(&texDesc, sizeof(texDesc));
            //texDesc.Width = width;
            //texDesc.Height = height;
            //texDesc.MipLevels = 1;
            //texDesc.ArraySize = 1;
            //texDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
            //texDesc.SampleDesc.Count = 1;
            //texDesc.Usage = D3D11_USAGE_DEFAULT;
            //texDesc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
            //texDesc.CPUAccessFlags = 0;
            //texDesc.MiscFlags = 0;

            var texDesc = new Vortice.Direct3D11.Texture2DDescription
            {
                Width = (uint)Width,
                Height = (uint)Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Vortice.DXGI.Format.B8G8R8A8_UNorm, // 对应A8R8G8B8格式
                SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,// Pool.Managed等效配置
                BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource,// Usage.None默认绑定
                CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.Write,
                //MiscFlags = Vortice.Direct3D11.ResourceOptionFlags.None,
            };

            //// 2. 初始化子资源数据
            //D3D11_SUBRESOURCE_DATA initData;
            //ZeroMemory(&initData, sizeof(initData));
            //initData.pSysMem = imageData.data();
            //initData.SysMemPitch = width * 4; // RGBA格式每行字节数
            //initData.SysMemSlicePitch = 0;

            //// 3. 创建纹理资源
            //HRESULT hr = device->CreateTexture2D(&texDesc, &initData, ppTexture);
            //if (FAILED(hr)) return hr;

            // 2. 准备子资源数据（关键修正点）
            var initData = new SubresourceData(Marshal.AllocHGlobal(decompressedData.Length), (uint)Width * 4, 0);
            // 3. 复制数据到非托管内存
            Marshal.Copy(decompressedData, 0, initData.DataPointer, decompressedData.Length);
            try
            {
                // 4. 创建纹理资源
                Image = DXManager.Device.CreateTexture2D(texDesc, new[] { initData });
            }
            finally
            {
                // 5. 释放非托管内存
                Marshal.FreeHGlobal(initData.DataPointer);
            }

            //// 4. 创建着色器资源视图
            //D3D11_SHADER_RESOURCE_VIEW_DESC srvDesc;
            //ZeroMemory(&srvDesc, sizeof(srvDesc));
            //srvDesc.Format = texDesc.Format;
            //srvDesc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
            //srvDesc.Texture2D.MipLevels = 1;

            //device->CreateShaderResourceView(*ppTexture, &srvDesc, ppSRV);

            //2、锁纹理
            var stream = DXManager.DeviceContext.Map(Image, 0, Vortice.Direct3D11.MapMode.WriteDiscard, 0);
            Data = (byte*)stream.DataPointer;

            //4、将数据从托管内存复制到非托管内存
            Marshal.Copy(decompressedData, 0, stream.DataPointer, decompressedData.Length);
            //5、将数据从非托管内存更新到纹理
            DXManager.DeviceContext.UpdateSubresource(Image, 0, null, stream.DataPointer, stream.RowPitch, stream.DepthPitch);

            //6、解锁纹理
            DXManager.DeviceContext.Unmap(Image, 0);

            DXManager.TextureList.Add(this);
            TextureValid = true;
            CleanTime = CMain.Time + Settings.CleanDelay;

            //检查图像完整2
            if (num2++ < count)
            {
                GrabImage.ShowImageFromGPU(DXManager.Device, Image);
            }
        }

        public unsafe void UseDirectXTexNet(BinaryReader reader)
        {
            //// 初始化DirectXTex库
            ////DirectXTexNet.Initialize();

            ////3、解压数据到托管内存
            //var compressedData = reader.ReadBytes(Length);
            //byte[] decompressedData;
            //using (var inputStream = new MemoryStream(compressedData))
            //using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            //using (var outputStream = new MemoryStream())
            //{
            //    gzipStream.CopyTo(outputStream);
            //    decompressedData = outputStream.ToArray();
            //}
            //nint ptr = Marshal.AllocHGlobal(decompressedData.Length);
            //Marshal.Copy(decompressedData, 0, ptr, decompressedData.Length);

            //try
            //{
            //    // 从内存加载图像数据
            //    var image = TexHelper.Instance.LoadFromWICMemory(ptr, decompressedData.Length,WIC_FLAGS.NONE);

            //    // 转换为RGBA32格式确保兼容性
            //    var convertedImage = image.Convert(DXGI_FORMAT.B8G8R8A8_UNORM,TEX_FILTER_FLAGS.DEFAULT,0.5f);

            //    // 获取图像元数据
            //    var metadata = convertedImage.GetMetadata();

            //    // 准备D3D11纹理描述
            //    var texDesc = new Texture2DDescription
            //    {
            //        Width = (uint)metadata.Width,
            //        Height = (uint)metadata.Height,
            //        MipLevels = 1,
            //        ArraySize = 1,
            //        Format = Vortice.DXGI.Format.B8G8R8A8_UNorm, // 对应A8R8G8B8格式
            //        SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
            //        Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,// Pool.Managed等效配置
            //        BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource,// Usage.None默认绑定
            //        CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.Write,
            //        //MiscFlags = Vortice.Direct3D11.ResourceOptionFlags.None,
            //    };

            //    // 获取图像数据指针
            //    var imageDataPtr = convertedImage.GetPixels();

            //    // 创建D3D11纹理
            //    Image = DXManager.Device.CreateTexture2D(texDesc, new[] { new SubresourceData(imageDataPtr, (uint)(metadata.Width * 4), 0) });
            //}
            //catch (COMException ex) when (ex.HResult == 0x88982F50)
            //{
            //    // 处理WIC组件缺失错误
            //    Console.WriteLine("错误：缺少必要的WIC组件。请安装Windows Imaging Component编解码器包。");
            //    // 可在此处添加自动下载或安装组件的逻辑
            //    return;
            //}
            //finally
            //{
            //    // 清理DirectXTex资源
            //    //DirectXTex.Shutdown();
            //}

            //DXManager.TextureList.Add(this);
            //TextureValid = true;
            //CleanTime = CMain.Time + Settings.CleanDelay;

            ////检查图像完整2
            //if (num2++ < count)
            //{
            //    GrabImage.ShowImageFromGPU(DXManager.Device, Image);
            //}
        }

        // 1. 修正纹理创建函数
        public unsafe void CreateTextureFromBytes(BinaryReader reader)
        {
            //3、解压数据到托管内存
            var compressedData = reader.ReadBytes(Length);
            byte[] decompressedData;
            using (var inputStream = new MemoryStream(compressedData))
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                gzipStream.CopyTo(outputStream);
                decompressedData = outputStream.ToArray();
            }

            //检查图像完整1
            if (num1++ < count)
            {
                GrabImage.ShowImageFromCPU(decompressedData, Width, Height);
            }

            // 验证输入数据
            if (decompressedData == null || decompressedData.Length == 0)
                throw new ArgumentException("Invalid image data");

            if (decompressedData.Length < Width * Height * 4)
                throw new ArgumentException("Image data size does not match dimensions");

            // 配置纹理描述 (关键修正点1)
            var texDesc = new Vortice.Direct3D11.Texture2DDescription
            {
                Width = (uint)Width,
                Height = (uint)Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Vortice.DXGI.Format.B8G8R8A8_UNorm, // 对应A8R8G8B8格式
                SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,// Pool.Managed等效配置
                BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource,// Usage.None默认绑定
                CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.Write,
                //MiscFlags = Vortice.Direct3D11.ResourceOptionFlags.None,
            };

            // 准备子资源数据 (关键修正点2)
            var initData = new SubresourceData(Marshal.AllocHGlobal(decompressedData.Length), (uint)Width * 4, 0);

            try
            {
                // 复制数据到非托管内存
                Marshal.Copy(decompressedData, 0, initData.DataPointer, decompressedData.Length);

                // 创建纹理资源
                Image = DXManager.Device.CreateTexture2D(texDesc, new[] { initData });
            }
            finally
            {
                Marshal.FreeHGlobal(initData.DataPointer);
            }

            DXManager.TextureList.Add(this);
            TextureValid = true;
            CleanTime = CMain.Time + Settings.CleanDelay;

            //检查图像完整2
            if (num2++ < count)
            {
                GrabImage.ShowImageFromGPU(DXManager.Device, Image);
            }

            //2、锁纹理
            var stream = DXManager.DeviceContext.Map(Image, 0, Vortice.Direct3D11.MapMode.WriteDiscard, 0);
            Data = (byte*)stream.DataPointer;
            DXManager.DeviceContext.Unmap(Image, 0);
        }

        #endregion

        public unsafe void CreateTexture(BinaryReader reader)
        {
            //原：
            //Image = new Texture(DXManager.Device, w, h, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);
            //DataRectangle stream = Image.LockRectangle(0, LockFlags.Discard);
            //Data = (byte*)stream.Data.DataPointer;
            //DecompressImage(reader.ReadBytes(Length), stream.Data);
            //stream.Data.Dispose();
            //Image.UnlockRectangle(0);

            var decompressedData = DecompressData(reader.ReadBytes(Length));
            nint datapoint = 0;
            Image = DXManager.CreateTextureFromBytes(decompressedData, (uint)Width, (uint)Height, ref datapoint);
            //Data = (byte*)datapoint;

            //var stream = DXManager.DeviceContext.Map(Image, 0, Vortice.Direct3D11.MapMode.WriteDiscard, 0);
            //Data = (byte*)stream.DataPointer;
            //DXManager.DeviceContext.Unmap(Image, 0);

            if (HasMask)
            {
                reader.ReadBytes(12);

                //MaskImage = new Texture(DXManager.Device, w, h, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);
                //stream = MaskImage.LockRectangle(0, LockFlags.Discard);
                //DecompressImage(reader.ReadBytes(Length), stream);

                decompressedData = DecompressData(reader.ReadBytes(Length));
                MaskImage = DXManager.CreateTextureFromBytes(decompressedData, (uint)Width, (uint)Height, ref datapoint);
            }

            DXManager.TextureList.Add(this);
            TextureValid = true;
            CleanTime = CMain.Time + Settings.CleanDelay;
        }
        static int num1 = 0, num2 = 0, count = 10;

        public unsafe void DisposeTexture()
        {
            DXManager.TextureList.Remove(this);

            if (Image != null)
            {
                Image.Dispose();
            }

            if (MaskImage != null)
            {
                MaskImage.Dispose();
            }

            TextureValid = false;
            Image = null;
            MaskImage = null;
            Data = null;
        }

        public unsafe bool VisiblePixel(Point p)
        {
            if (p.X < 0 || p.Y < 0 || p.X >= Width || p.Y >= Height)
                return false;

            int w = Width;

            bool result = false;
            if (Data != null)
            {
                int x = p.X;
                int y = p.Y;
                
                int index = (y * (w << 2)) + (x << 2) + 3;
                
                byte col = Data[index];

                if (col == 0) 
                    return false;
                else return true;
            }
            return result;
        }

        public Size GetTrueSize()
        {
            if (TrueSize != Size.Empty) return TrueSize;

            int l = 0, t = 0, r = Width, b = Height;

            bool visible = false;
            for (int x = 0; x < r; x++)
            {
                for (int y = 0; y < b; y++)
                {
                    if (!VisiblePixel(new Point(x, y))) continue;

                    visible = true;
                    break;
                }

                if (!visible) continue;

                l = x;
                break;
            }

            visible = false;
            for (int y = 0; y < b; y++)
            {
                for (int x = l; x < r; x++)
                {
                    if (!VisiblePixel(new Point(x, y))) continue;

                    visible = true;
                    break;

                }
                if (!visible) continue;

                t = y;
                break;
            }

            visible = false;
            for (int x = r - 1; x >= l; x--)
            {
                for (int y = 0; y < b; y++)
                {
                    if (!VisiblePixel(new Point(x, y))) continue;

                    visible = true;
                    break;
                }

                if (!visible) continue;

                r = x + 1;
                break;
            }

            visible = false;
            for (int y = b - 1; y >= t; y--)
            {
                for (int x = l; x < r; x++)
                {
                    if (!VisiblePixel(new Point(x, y))) continue;

                    visible = true;
                    break;

                }
                if (!visible) continue;

                b = y + 1;
                break;
            }

            TrueSize = Rectangle.FromLTRB(l, t, r, b).Size;

            return TrueSize;
        }

        private static byte[] DecompressImage(byte[] image)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(image), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        private static void DecompressImage(byte[] data, Stream destination)
        {
            using (var stream = new GZipStream(new MemoryStream(data), CompressionMode.Decompress))
            {
                stream.CopyTo(destination);
            }
        }

        private static byte[] DecompressData(byte[] compressedData)
        {
            byte[] decompressedData;
            using (var inputStream = new MemoryStream(compressedData))
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                gzipStream.CopyTo(outputStream);
                decompressedData = outputStream.ToArray();
            }
            return decompressedData;
        }
    }
}
