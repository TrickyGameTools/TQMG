// Lic:
// TQMG.cs
// (c)  Jeroen Petrus Broks.
// 
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not
// distributed with this file, You can obtain one at
// http://mozilla.org/MPL/2.0/.
// Version: 19.10.26
// EndLic





#undef qdebuglog

// Since some bugs came up while I was coding Kthura, this was the easiest way to go....
#undef DebugInKthura


#region Yeah, we're using this.... Any questions?
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using UseJCR6;

#if DebugInKthura
using KthuraEdit;
using KthuraEdit.Stages;
#endif

#endregion

namespace TrickyUnits { 

    class TQMG_NeedParentImage_Exception : Exception {
        new public readonly string Message = "A parent image is needed";
    }

    class TQMGPixMap {

        TQMGImage parent;
        public readonly int width, height;
        Color[] PixMap;
        int parentframe = 0;

        void NeedParent() {
            if (parent == null) throw new TQMG_NeedParentImage_Exception();
        }

        public TQMGPixMap(int w, int h) {
            var p = TQMG.NewImage(w, h);
            width = w;
            height = h;
            PixMap = p.PixMap(0);
        }

        public TQMGPixMap(TQMGImage parent,int Frame=0) {
            PixMap = parent.PixMap(Frame);
            this.parent = parent;
            width = parent.Width;
            height = parent.Height;
            parentframe = Frame;
        }

        public void Pull(int Frame=-1) {
            if (Frame < 0) Frame = parentframe;
            NeedParent();
            PixMap = parent.PixMap(Frame);            
        }

        public void Push(int Frame = -1) {
            if (Frame < 0) Frame = parentframe;
            NeedParent();
            parent.PixMap(PixMap,Frame);
        }

        public Color this[int x,int y] {
            get {
                if (x < 0 || y < 0 || x >= width || y >= height) throw new Exception($"PixMap[{x},{y}]: Out of range! ({width}x{height})");
                return PixMap[(y * width) + x];
            }
            set {
                if (x < 0 || y < 0 || x >= width || y >= height) throw new Exception($"PixMap[{x},{y}]: Out of range! ({width}x{height})");
                PixMap[(y * width) + x] = value;
            }
        }

    }

    #region image
    class TQMGImage{
        #region Image declarations
        readonly Class_TQMG Mama;
        readonly Texture2D[] tex;
        int hotx = 0, hoty = 0;
        int wdth = -1, hght = -1;
        bool allowvar = false;        
        #endregion


        #region Image Construction Set
        public TQMGImage(Class_TQMG parent, TJCRDIR JCR, string file)
        {
            parent.Error("Ok");
            Mama = parent;
            var bt = JCR.ReadFile(file);
            tex = new Texture2D[1];
            tex[0] = Texture2D.FromStream(Mama.gfxd, bt.GetStream());
            bt.Close();
            if (tex == null) { parent.Error($"I could not load {file} from JCR resource"); return; }
            var hotfile = $"{qstr.StripExt(file)}.hot";
            if (JCR.Exists(hotfile)) {
                Debug.WriteLine($"Loading hotspots: {hotfile}");
                var str = JCR.LoadString(hotfile).Trim();
                var spl = str.Split(',');
                if (spl.Length < 2) { parent.Error($"Invalid data in hotpot file {hotfile} for {file}"); return; }
                hotx = qstr.ToInt(spl[0]);
                hoty = qstr.ToInt(spl[1]);
            } else {
                Debug.WriteLine($"No hotspot file found ({hotfile})");
            }
        }

        public TQMGImage(Class_TQMG parent, string JCRFile, string file) {
            parent.Error("Ok");
            var JCR = JCR6.Dir(JCRFile);
            if (JCR == null) { parent.Error($"JCRERROR: {JCR6.JERROR}"); return; }
            var bt = JCR.ReadFile(file);
            tex = new Texture2D[1];
            tex[0] = Texture2D.FromStream(Mama.gfxd, bt.GetStream());
            bt.Close();
            if (tex == null) parent.Error($"I could not load {file} from JCR resource {JCRFile}");
        }

        public TQMGImage(Class_TQMG parent, string file)
        {
            parent.Error("Ok");
            Mama = parent;
            var bt = Mama.jcr.ReadFile(file);
            if (bt == null) throw new Exception($"new TQMGImage(<parent>,\"{file}\"): Failure -- {JCR6.JERROR}");
            tex = new Texture2D[1];
            tex[0] = Texture2D.FromStream(Mama.gfxd, bt.GetStream());
            bt.Close();
            if (tex == null) parent.Error($"I could not load {file} from JCR resource");
            var JCR = Mama.jcr;
            var hotfile = $"{qstr.StripExt(file)}.hot";
            if (JCR.Exists(hotfile)) {
                Debug.WriteLine($"Loading hotspots: {hotfile}");
                var str = JCR.LoadString(hotfile).Trim();
                var spl = str.Split(',');
                if (spl.Length < 2) { parent.Error($"Invalid data in hotpot file {hotfile} for {file}"); return; }
                hotx = qstr.ToInt(spl[0]);
                hoty = qstr.ToInt(spl[1]);
                Debug.WriteLine($"Hotspot for {file} is set to ({hotx},{hoty})");
            } else {
                Debug.WriteLine($"No hotspot file found ({hotfile})");
            }

        }

        public TQMGImage(Class_TQMG parent, int length, bool avar)
        {
            // This will be used by the font manager.
            tex = new Texture2D[length];
            allowvar = avar;
            Mama = parent;
        }

        public TQMGImage(Class_TQMG parent, TJCRDIR JCR,string bundle,int max) {
            Mama = parent;
            var b = bundle.ToUpper();
#if DebugInKthura
            DBG.Log($"Trying to load bunde: {bundle} (max = {max})");
            if (max != 0) { Console.Beep(60, 500); DBG.Log($"HEY! Max is not zero! In stead it's {max}!"); }
#endif
            var fls = new List<string>();
            if (!qstr.Suffixed(b,"/")) b += "/";
            var count = 0;
            if (JCR == null) throw new Exception("The Bundle loader received 'null' for a JCR resource!");
            foreach(string en in JCR.Entries.Keys) {
                if (qstr.Prefixed(en,b) && (max==0 || count < max) && (qstr.Suffixed(en,".PNG") || qstr.Suffixed(en,".JPG"))) {
                    fls.Add(en);
                    count++;
                }                
            }
            var files = fls.ToArray();
            tex = new Texture2D[count];
            for (int i=0;i<count;i++) {
                var bt = JCR.ReadFile($"{files[i]}");
                if (bt == null) throw new Exception($"JCR6 Error: {JCR6.JERROR}");
                tex[i] = Texture2D.FromStream(Mama.gfxd, bt.GetStream());
                bt.Close();
            }
        }

        public TQMGImage(Class_TQMG parent, QuickStream stream, bool close=true)  {
            Mama = parent;
            tex = new Texture2D[1];
            tex[0] = Texture2D.FromStream(Mama.gfxd, stream.GetStream());
            if (close) stream.Close();
        }

        public TQMGImage(Class_TQMG parent,int width, int height, int Frames = 1) {
            Mama = parent;
            tex = new Texture2D[Frames];
            for (int i = 0; i < Frames; ++i) tex[i] = new Texture2D(Mama.gfxd, width, height);
        }
        #endregion

        #region Image Format
        public int Width {
            get {
                if (wdth == -1) wdth = tex[0].Width;
                return wdth;
            }
        }

        public int Height {
            get {
                if (hght == -1) hght = tex[0].Height;
                return hght;
            }
        }

        public int FrWidth(int idx) => tex[idx].Width;

        public int Frames => tex.Length;

        #endregion

        #region Quick Hotspot
        public void HotCenter() { hotx = Width / 2; hoty = Height / 2; }
        public void HotBottomCenter() { hotx = Width / 2; hoty = Height; }
        public void HotTopCenter() { hotx = Width / 2; hoty = 0; }
        #endregion

        

        #region Font only stuff
        public void IRequire(int idx,string entry) {
            // This function has only been brought in to enable the font text generator to only load characters when they are actually needed.
            // Saves loading times, and also RAM you never needed. Or does it?
            try {
                if (tex[idx] != null) return;
                TQMG.Log($"IRequire({idx},\"{entry}\");");
                var bt = Mama.jcr.ReadFile(entry);
                if (bt == null)
                    Debug.WriteLine($"ERROR! {JCR6.JERROR} from IRequire({idx},\"{entry}\");");
                else {
                    tex[idx] = Texture2D.FromStream(Mama.gfxd, bt.GetStream());
                    bt.Close();
                }
            } catch (Exception e) {
                Mama.Error($"System error: {e.Message}");
            }
        }

        public bool IGot(int idx) {
            if (idx < 0) return false;
            if (idx >= tex.Length) return false;
            return tex[idx] != null;
        }

        public void IGetF(int idx, ref int w,ref int h) { w = tex[idx].Width; h = tex[idx].Height; }
        #endregion


        #region Save
        public void Save(int frame,string file,byte quality = 5) {
            var bto = QuickStream.WriteFile(file);
            tex[frame].SaveAsPng(bto.GetStream(),tex[frame].Width,tex[frame].Height);
            bto.Close();
        }
        public void Save(string file, byte quality = 5) => Save(0, file, quality);

        public void Save(int frame,TJCRCreate j,string entry, string Storage="Store", string Author="", string Notes="",byte quality = 5) {
            var bto = j.NewEntry(entry, Storage, Author, Notes);
            if (bto == null) throw new Exception(JCR6.JERROR);
            tex[frame].SaveAsPng(bto.GetStream, tex[frame].Width, tex[frame].Height);
            bto.Close();
        }

        public void Save(TJCRCreate j, string entry, string Storage = "Store", string Author = "", string Notes = "", byte quality = 5) => Save(0, j, entry, Storage, Author, Notes, quality);

        public void SaveBundle(TJCRCreate j, string prefix = "", string Storage = "Store", string Author = "", string Notes = "", byte quality = 5) {
            for (int i = 0; i < tex.Length; i++) Save(i, j, $"{prefix}{qstr.Right($"0000000000{i}", 9)}.png", Storage, Author, Notes, quality);
        }
        public void SaveBundle(string jcr, string prefix = "", string Storage = "Store", string Author = "", string Notes = "", byte quality = 5) {
            var j = new TJCRCreate(jcr, Storage);
            SaveBundle(j, prefix, Storage, Author, Notes, quality);
            j.Close();
        }

        #endregion

        #region Welcome to the SHOW
        Vector2 dc = new Vector2();
        Rectangle rc = new Rectangle();
        public void Draw(int x,int y, int Frame = 0) {
            try {
                dc.X = x;
                dc.Y = y;
                //TQMG.Log($"Draw({x},{y},{Frame});");
                Mama.spriteBatch.Draw(tex[Frame], dc, Mama.mColor);                
            } catch (Exception Ex) {
                TQMG.Error($"Draw({x},{y},{Frame}): {Ex.Message} in {Ex.Source}\n\n{Ex.StackTrace}");
            }
        }
        public void Draw(int x,int y, int w, int h, int Frame = 0) {
            rc.X = x;
            rc.Y = y;
            rc.Width = w;
            rc.Height = h;
            dc.X = x;
            dc.Y = y;
            Mama.spriteBatch.Draw(tex[Frame], dc,rc, Mama.mColor);
        }
        public void Draw(int ix,int iy, int x, int y, int w, int h, int Frame = 0) {
            rc.X = ix;
            rc.Y = iy;
            rc.Width = w;
            rc.Height = h;
            dc.X = x;
            dc.Y = y;
            Mama.spriteBatch.Draw(tex[Frame], dc, rc, Mama.mColor);
        }

        public void XDraw(int x, int y, int Frame = 0) {
            var vhot = new Vector2(hotx, hoty);
            var vpos = new Vector2(x, y);
            var w = (int)(Width * Mama.fScaleX);
            var h = (int)(Height * Mama.fScaleY);
            var drect = new Rectangle(x, y, w, h);
            //Matrix m;
            //Matrix.CreateScale(Mama.fScaleX, Mama.fScaleY, 1, out m);
            //Mama.spriteBatch.Draw(tex[Frame], vpos, drect, null, vhot, Mama.rotation, null, Mama.mColor);
            Mama.spriteBatch.Draw(tex[Frame], drect, null, Mama.mColor, Mama.rotation, vhot, SpriteEffects.None, 1);
        }

        public Color GetPixel(int x, int y, int Frame = 0) {
            if (x < 0 || y < 0 || x >= Width || y >= Height) throw new Exception($"GetPixel({x},{y},{Frame}): Out of range! ({Width}x{Height})");
            var Dat = new Color[tex[Frame].Width * tex[Frame].Height];
            tex[Frame].GetData<Color>(Dat);
            return Dat[y * tex[Frame].Width + x];
        }

        public void PutPixel(int x, int y, int Frame, Color Pixel) {
            if (x < 0 || y < 0 || x >= Width || y >= Height) throw new Exception($"PutPixel({x},{y},{Frame},<ColorData>): Out of range! ({Width}x{Height})");
            var Dat = new Color[tex[Frame].Width * tex[Frame].Height];
            tex[Frame].GetData<Color>(Dat);
            Dat[y * tex[Frame].Width + x] = Pixel;
            tex[Frame].SetData<Color>(Dat);
        }
        public void PutPixel(int x, int y, Color Pixel) => PutPixel(x, y, 0, Pixel);
        public void PutPixel(int x, int y, int Frame, byte r, byte g, byte b) {
            var Pixel = GetPixel(x, y,Frame);
            Pixel.R = r;
            Pixel.G = g;
            Pixel.B = b;
            PutPixel(x, y, Frame, Pixel);
        }
        public void PutPixel(int x, int y, byte r, byte g, byte b) => PutPixel(x, y, 0, r, g, b);
        public void PutPixel(int x, int y, int Frame, byte r, byte g, byte b,byte alpha) {
            var Pixel = GetPixel(x, y, Frame);
            Pixel.R = r;
            Pixel.G = g;
            Pixel.B = b;
            Pixel.A = alpha;
            PutPixel(x, y, Frame, Pixel);
        }
        public void PutPixel(int x, int y, byte r, byte g, byte b, byte a) => PutPixel(x, y, 0, r, g, b, a);

        public Color[] PixMap(int Frame = 0) {
            var ret = new Color[tex[Frame].Width * tex[Frame].Height];
            tex[Frame].GetData<Color>(ret);
            return ret;
        }

        public void PixMap(Color[] map, int Frame = 0) {
            tex[Frame].SetData<Color>(map);
        }
        #endregion

    }
    #endregion

    #region text
    enum TQMG_TextAlign { Left, Right, Center }

    class TQMGText {
        class Letter {
            public int index;
            public int x;            
        }
        readonly TQMGFont Ouwe;
        List<Letter> FSTRING = new List<Letter>();
        readonly string hastext; // debug purposes!

        readonly public int Width;
        readonly public int Height;
        readonly bool monoforce = false;
        int maxwidth = 0;

        public TQMGText(TQMGFont parentfont, string text, bool forcemono=false) {
            var x = 0;
            monoforce = forcemono;
            hastext = text;
            Height = 0;
            Ouwe = parentfont;
            TQMG.Log($"Text({text});");
            for (int i = 0; i < text.Length; i++) {
                try {
                    var l = new Letter();
                    var c = (byte)text[i];
                    int w = 0, h = 0;
                    if (c == 32)
                        x += Height / 2;
                    else if (c==13) { }
                    else if (c == 9)
                        x += x % (Height * 3);
                    else {
                        l.x = x;
                        Ouwe.fimg.IRequire(c, $"{Ouwe.jcrdir}{c}.png");
                        if (Ouwe.fimg.IGot(c)) {
                            Ouwe.fimg.IGetF(c, ref w, ref h);
                            if (forcemono) {
                                if (maxwidth < w) maxwidth = w;
                                w = maxwidth;
                            }
                            l.x = x;
                            l.index = c;
                            TQMG.Log($"Pos {i}/{text.Length}; Char {text[i]} ({c}) listed in. >> x={x}; w={w}; h={h}");
                            x += w + 1;
                            Width = x;
                            if (Height < h) Height = h;
                            FSTRING.Add(l);
                        }
                    }
                } catch (Exception Ex) {
                    TQMG.Error($"TQMGText(<parentfont>,\n{text}\n);\n{Ex.Message}");

                }
            }
            TQMG.Log($"/Text({text});");
        }

        public void Draw(int x,int y,TQMG_TextAlign align=TQMG_TextAlign.Left) {
            try {
                var M = Ouwe.Mama;
                var bx = 0;
                if (FSTRING.Count == 0) return;
                TQMG.Log($"<DrawText Content='{hastext}'>");
                switch (align) {
                    case TQMG_TextAlign.Left:
                        bx = 0;
                        break;
                    case TQMG_TextAlign.Right:
                        bx = -Width;
                        break;
                    case TQMG_TextAlign.Center:
                        bx = -(Width / 2);
                        break;
                    default:
                        Ouwe.Mama.Error("Unknown alignment code!");
                        return;
                }
                foreach (Letter L in FSTRING) {
                    Ouwe.fimg.Draw(bx + x + L.x, y, L.index);
                }
                TQMG.Log("</DrawText>");
            } catch (Exception Ex) {
                TQMG.Error($"{Ex.Message}\n\n{Ex.StackTrace}");
            }
        }

        

        /// <summary>
        /// Will draw text like normally, but stop drawing if the text goes beyond the maximum width
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">u</param>
        /// <param name="maxw">maximum width</param>
        public void DrawMax(int x,int y, int maxw) {
            foreach (Letter L in FSTRING) {
                if (L.x + Ouwe.fimg.FrWidth(L.index) < maxw)
                    Ouwe.fimg.Draw(x + L.x, y, L.index);
            }

        }

    }

    class TQMGFont{
        readonly public Class_TQMG Mama;
        readonly public string jcrdir;
        readonly bool ok;
        readonly public TQMGImage fimg;
        public bool crashnothave = false;
        internal int largestw = 0;

      

        public TQMGFont(Class_TQMG parent, string dir) {
            Mama = parent;
            jcrdir = dir.ToUpper();
            if (!qstr.Suffixed(jcrdir,"/")) jcrdir += "/";
            ok = false;      
            if (Mama.jcr==null) {
                Debug.WriteLine("HEY! JCR is null! how could this happen? Anyway expect an error upon reading this font!");
                for (int i = 0; i < 20; i++) Console.Beep();
            }
            foreach(string ent in Mama.jcr.Entries.Keys) {
                ok = ok || (qstr.Prefixed(ent, jcrdir) && qstr.Suffixed(ent,".PNG"));
            }
            if (!ok) {
                Mama.Error($"JCR6 resource does not appear to have font folder \"{dir}\"!");
            }
            fimg = new TQMGImage(Mama, 256, true); // For now ASCII only... more may be supported later.
        }

        public TQMGText Text(string text, bool forcemono=false) => new TQMGText(this, text, forcemono);
        public void DrawText(string text, int x, int y, TQMG_TextAlign align = TQMG_TextAlign.Left,bool forcemono=false) {
            var txt = Text(text,forcemono);
            txt.Draw(x, y, align);
        }

        public void DrawMax(string text, int x, int y, int maxwidth) {
            var txt = Text(text);
            txt.DrawMax(x, y, maxwidth);
        }

        public int TextWidth(string text) {
            var txt = Text(text);
            return txt.Width;
        }
        
    }

    #endregion
  
    #region core
    class Class_TQMG {
      

        readonly public GraphicsDeviceManager gfxm;
        readonly public SpriteBatch spriteBatch;
        readonly public TJCRDIR jcr;
        readonly public GraphicsDevice gfxd;
        public bool CRASH = false;
        public string LastError { get; private set; } = "Ok";
        public int ScaleX = 1000, ScaleY = 1000;
        public float fScaleX => (float)ScaleX / 1000;
        public float fScaleY => (float)ScaleY / 1000;

        public Color mColor = new Color(255, 255, 255, 255);
        public float rotation = 0;
        //public Rectangle srcRec=null;

        public void Error(string em) {
            if (CRASH && em!="Ok") throw new System.Exception(em);
            LastError = em;
        }

        readonly public Viewport OriginalViewport;
        public Class_TQMG(GraphicsDeviceManager agfxm, GraphicsDevice agfxd, SpriteBatch aSB, TJCRDIR ajcr) {
            #region MKL
            MKL.Lic    ("TQMG - TQMG.cs","Mozilla Public License 2.0");
            MKL.Version("TQMG - TQMG.cs","19.10.26");
            #endregion

            #region TQMG core setup
            gfxm = agfxm;
            gfxd = agfxd;
            spriteBatch = aSB;
            jcr = ajcr;
            OriginalViewport = gfxm.GraphicsDevice.Viewport;
            #endregion
        }

        public TQMGFont GetFont(string dir) {
            LastError = "Ok";
            return new TQMGFont(this, dir);
        }

        public TQMGImage GetImage(string image) => new TQMGImage(this, image);
        public TQMGImage GetImage(QuickStream stream, bool close = true) => new TQMGImage(this, stream, close);

        public int ScrWidth => gfxd.PresentationParameters.BackBufferWidth;
        public int ScrHeight => gfxd.PresentationParameters.BackBufferHeight;

    }
    #endregion

    #region Keyboard stuff
    /// <summary>
    /// Class containing a few quick key features, MonoGame failed to provide!
    /// I need to note, this has been taken with an standard US QWERTY keyboard in mind, so other keyboards may fail!
    /// </summary>
    static class TQMGKey {
        static KeyboardState last;
        static KeyboardState yet;
        static Dictionary<Keys, char> UnShift = new Dictionary<Keys, char>();
        static Dictionary<Keys, char> Shift = new Dictionary<Keys, char>();
        static Dictionary<Keys, byte> Lock = new Dictionary<Keys, byte>(); // 0 = nothing, 1 = Caps Lock, 2 = Num Lock

        static Array values = Enum.GetValues(typeof(Keys));

        static TQMGKey() {
            #region letters!
            Debug.WriteLine("TQMGKey: Init letters");
            UnShift[Keys.A] = 'a';
            UnShift[Keys.B] = 'b';
            UnShift[Keys.C] = 'c';
            UnShift[Keys.D] = 'd';
            UnShift[Keys.E] = 'e';
            UnShift[Keys.F] = 'f';
            UnShift[Keys.G] = 'g';
            UnShift[Keys.H] = 'h';
            UnShift[Keys.I] = 'i';
            UnShift[Keys.J] = 'J';
            UnShift[Keys.K] = 'k';
            UnShift[Keys.L] = 'l';
            UnShift[Keys.M] = 'm';
            UnShift[Keys.N] = 'n';
            UnShift[Keys.O] = 'o';
            UnShift[Keys.P] = 'p';
            UnShift[Keys.Q] = 'q';
            UnShift[Keys.R] = 'r';
            UnShift[Keys.S] = 's';
            UnShift[Keys.T] = 't';
            UnShift[Keys.U] = 'u';
            UnShift[Keys.V] = 'v';
            UnShift[Keys.W] = 'w';
            UnShift[Keys.X] = 'x';
            UnShift[Keys.Y] = 'y';
            UnShift[Keys.Z] = 'z';
            foreach(Keys k in UnShift.Keys) { // I can do this now as the letters come first. I did so for a reason :P
                Shift[k] = UnShift[k].ToString().ToUpper()[0];
                Lock[k] = 1; // Caps lock means to upper
            }
            #endregion

            #region WhiteSpace
            UnShift[Keys.Space] = ' ';
            Shift[Keys.Space] = ' ';
            UnShift[Keys.Enter] = '\n';
            Shift[Keys.Enter] = '\n';
            UnShift[Keys.Tab] = '\t';
            Shift[Keys.Tab] = '\t';
            #endregion

            #region Numberic keyboard
            Shift[Keys.NumPad0] = '0';
            Shift[Keys.NumPad1] = '1';
            Shift[Keys.NumPad2] = '2';
            Shift[Keys.NumPad3] = '3';
            Shift[Keys.NumPad4] = '4';
            Shift[Keys.NumPad5] = '5';
            Shift[Keys.NumPad6] = '6';
            Shift[Keys.NumPad7] = '7';
            Shift[Keys.NumPad8] = '8';
            Shift[Keys.NumPad9] = '9';
            Lock[Keys.NumPad0] = 2;
            Lock[Keys.NumPad1] = 2;
            Lock[Keys.NumPad2] = 2;
            Lock[Keys.NumPad3] = 2;
            Lock[Keys.NumPad4] = 2;
            Lock[Keys.NumPad5] = 2;
            Lock[Keys.NumPad6] = 2;
            Lock[Keys.NumPad7] = 2;
            Lock[Keys.NumPad8] = 2;
            Lock[Keys.NumPad9] = 2;
            #endregion

            #region TopLine Numbers
            UnShift[Keys.D1] = '1';
            UnShift[Keys.D2] = '2';
            UnShift[Keys.D3] = '3';
            UnShift[Keys.D4] = '4';
            UnShift[Keys.D5] = '5';
            UnShift[Keys.D6] = '6';
            UnShift[Keys.D7] = '7';
            UnShift[Keys.D8] = '8';
            UnShift[Keys.D9] = '9';
            UnShift[Keys.D0] = '0';
            Shift[Keys.D1] = '!';
            Shift[Keys.D2] = '@';
            Shift[Keys.D3] = '#';
            Shift[Keys.D4] = '$';
            Shift[Keys.D5] = '%';
            Shift[Keys.D6] = '^';
            Shift[Keys.D7] = '&';
            Shift[Keys.D8] = '*';
            Shift[Keys.D9] = '(';
            Shift[Keys.D0] = ')';
            #endregion
                        
            #region Special Characters 123 line
            UnShift[Keys.OemTilde] = '`';
            UnShift[Keys.OemMinus] = '-';
            UnShift[Keys.OemPlus] = '=';
            UnShift[Keys.OemPipe] = '\\';
            Shift[Keys.OemTilde] = '~';
            Shift[Keys.OemMinus] = '_';
            Shift[Keys.OemPlus] = '+';
            Shift[Keys.OemPipe] = '|';
            #endregion

            #region Special Characters QWERTY line
            UnShift[Keys.OemOpenBrackets] = '[';
            UnShift[Keys.OemCloseBrackets] = ']';
            Shift[Keys.OemOpenBrackets] = '{';
            Shift[Keys.OemCloseBrackets] = '}';
            #endregion

            #region Special Characters ASDFG line
            UnShift[Keys.OemSemicolon] = ';';
            UnShift[Keys.OemQuotes] = '\'';
            Shift[Keys.OemSemicolon] = ':';
            Shift[Keys.OemQuotes] = '"';
            #endregion

            #region Special Characters ZXCV line
            UnShift[Keys.OemComma] = ',';
            UnShift[Keys.OemPeriod] = '.';
            UnShift[Keys.OemQuestion] = '/';
            Shift[Keys.OemComma] = '<';
            Shift[Keys.OemPeriod] = '>';
            Shift[Keys.OemQuestion] = '?';
            #endregion



            #region Fill 0 to all unused locks (error prevention)... MUST ALWAYS BE THE LAST ACTION!
            foreach (Keys k in values)
                if (!Lock.ContainsKey(k)) Lock[k] = 0;
            #endregion

        }

        static public void Start(KeyboardState state) {
            last = yet;
            yet = state;
        }

        /// <summary>
        /// Returns a key pressed. Returns F19 (a key hardly in existence) when nothing has been pressed (since C# just requires to return something)
        /// </summary>
        static public Keys GetKey() {
            
            foreach(Keys k in values) {
                if (yet.IsKeyDown(k) && !last.IsKeyDown(k)) return k;
            }
            return Keys.F19;
        }

        /// <summary>
        /// Quick way to check if a key is being held or not (on the moment the last TQMGKey.Start was called, that is)
        /// </summary>
        /// <param name="k">Key code</param>
        /// <returns>True if key was indeed held</returns>
        static public bool Held(Keys k) => yet.IsKeyDown(k);

        /// <summary>
        /// Returns 'true' if a key has been hit (but not if it's been held)
        /// </summary>
        /// <param name="k">Key code</param>
        /// <returns>True if key is hit.</returns>
        static public bool Hit(Keys k) => yet.IsKeyDown(k) && !last.IsKeyDown(k);

        /// <summary>
        /// Gets character. Please note this routine can be rather slow, should only be used when typing is really needed!
        /// </summary>
        /// <returns>Returns value of character, and returns character 0 if none have been received</returns>
        public static char GetChar() {
            foreach (Keys k in values) {
                if (Hit(k)) {
                    var shift = yet.IsKeyDown(Keys.LeftShift) || yet.IsKeyDown(Keys.RightShift) || (yet.CapsLock && Lock[k] == 1) || (yet.NumLock && Lock[k] == 2);
                    if (shift && Shift.ContainsKey(k)) return Shift[k];
                    if (UnShift.ContainsKey(k)) return UnShift[k];
                }
            }
            return (char)0; 
        }
    }
    #endregion


    #region Link ups....
    /* The class above is only available as a 'regular' class in case you want more than one object for whatever reason. 
     * Since I rarely expect to need this the class below just serves as the main thing.
     */
    static class TQMG {
        static Class_TQMG me;
        public delegate void dLog(string msg); static dLog mLog = delegate { };
        static public dLog Error = delegate(string msg){ Log($"Error! {msg}"); };
        static public void Init(GraphicsDeviceManager agfxm, GraphicsDevice agfxd, SpriteBatch aSB, TJCRDIR ajcr) { me = new Class_TQMG( agfxm,  agfxd,  aSB, ajcr); }
        static public TQMGFont GetFont(string dir) => me.GetFont(dir);
        static public TQMGImage GetImage(string imagefile) => me.GetImage(imagefile);
        static public TQMGImage GetImage(QuickStream stream, bool close=true) => me.GetImage(stream,close);
        static public TQMGImage GetImage(TJCRDIR JCR, string bundle, int max) => new TQMGImage(me, JCR, bundle, max);
        static public TQMGImage GetBundle(TJCRDIR JCR, string bundle) => GetImage(JCR, bundle, 0);
        static public TQMGImage GetBundle(string JCRF,string bundle) {
            var JCR = JCR6.Dir(JCRF);
            return GetBundle(JCR, bundle);
        }
        static public TQMGImage GetBundle(string bundle) => GetBundle(me.jcr, bundle);
        static public TQMGImage NewImage(int w, int h, int f = 1) => new TQMGImage(me, w, h, f);
        static public int ScrWidth => me.ScrWidth;
        static public int ScrHeight => me.ScrHeight;

        /// <summary>
        /// Register a "Log" function for debugging purposes.
        /// </summary>
        /// <param name="l"></param>
        static public void RegLog(dLog l) { mLog = (dLog)l; }

        static public void UglyTile(TQMGImage img, int x, int y, int w, int h, int frame=0) { // Tiles, but doesn't take viewports in mind, so when textures stick out, so be it.
            for (int ix = x; ix < x + w; ix+=img.Width)
                for (int iy = y; iy < y + h; iy+=img.Height)
                    img.Draw(ix, iy,frame);
        }

        /// <summary>
        /// Simple tile
        /// </summary>
        static public void SimpleTile(TQMGImage img, int x, int y, int w, int h, int frame = 0) // Any hotspots are not yet taken into account yet.
        {
            img.Draw(x, y, w, h, frame);
        }

        static public void Tile(TQMGImage img, int ix, int iy,int x, int y, int w, int h, int frame = 0) {
            img.Draw(ix, iy, x, y, w, h, frame);
        }

        /// <summary>
        /// Sets Color
        /// </summary>
        /// <param name="r">red</param>
        /// <param name="g">green</param>
        /// <param name="b">blue</param>
        static public void Color(byte r, byte g, byte b) {
            me.mColor.R = r;
            me.mColor.G = g;
            me.mColor.B = b;            
        }

        /// <summary>
        /// Sets Color
        /// </summary>
        /// <param name="c">Full color struct (alpha value will (if set) be ignored!)</param>
        static public void Color(Color c) {
            Color(c.R, c.G, c.B);
        }

        static public Color GetColor() {
            return me.mColor;
        }

        /// <summary>
        /// Sets the alpha value. 
        /// </summary>
        /// <param name="alpha">Alpha as a number from 0 till 255</param>
        static public void SetAlpha(byte alpha) {
            me.mColor.A = alpha;
        }

        static public byte GetAlpha() => me.mColor.A;

        /// <summary>
        /// Sets the alpha value.
        /// </summary>
        /// <param name="alpha">Alpha as a float number from 0 till 1 (will be recalculated into the 0 till 255 scale)</param>
        static public void SetAlphaFloat(float alpha) {
            // Although I'm not fully sure if Kthura will be converted to C#, but if it will be, I will NEED this function!
            // The if routines are not meant to cover up bad usage (although that was just a nice side effect), but rather to catch up rounding errors. They could give unintential yet funny unwanted results.
            if (alpha < 0) {
                me.mColor.A = 0;
            } else if (alpha > 1) {
                me.mColor.A = 255;
            } else {
                me.mColor.A = (byte)(255.0 * alpha);
            }
        }

        static public Viewport originalviewport => me.OriginalViewport;
        static public void ViewPort(Viewport vp) {
            me.gfxm.GraphicsDevice.Viewport = vp;
        }

        static public void ViewPort(int x, int y, int w, int h) {
            var vp = new Viewport(x, y, w, h);
            ViewPort(vp);
        }

        static public Viewport GetViewPort => me.gfxm.GraphicsDevice.Viewport;
        static public void ViewPortFull() => ViewPort(originalviewport);



        #region Rectangle routine. Thanks to Stack overflow: https://stackoverflow.com/questions/5751732/draw-rectangle-in-xna-using-spritebatch (GONeal)!
        // Although I must say the code has been modifed for my own personal interests!
        private static Texture2D rect;
        private static Rectangle mRectangle=new Rectangle();
        public static void DrawRectangle(Rectangle coords) {
            if (rect == null) {
                rect = new Texture2D(me.gfxd, 1, 1);
                rect.SetData(new[] { Microsoft.Xna.Framework.Color.White });
            }
            me.spriteBatch.Draw(rect, coords, me.mColor);
        }
        public static void DrawRectangle(int x, int y, int w, int h) {
            mRectangle.X = x;
            mRectangle.Y = y;
            mRectangle.Width = w;
            mRectangle.Height = h;
            DrawRectangle(mRectangle);
        }

        /// <summary>
        /// Draws a rect given on the coordinates, and not on the given width and height.
        /// </summary>
        /// <param name="startx"></param>
        /// <param name="starty"></param>
        /// <param name="endx"></param>
        /// <param name="endy"></param>
        public static void DrRect(int startx, int starty, int endx, int endy) {
            var mr = new Rectangle();
            if (startx == endx) { mr.X = startx; mr.Width = 0; } else if (startx < endx) { mr.X = startx; mr.Width = endx - startx; } else { mr.X = endx; mr.Width = startx - endx; }
            if (starty == endy) { mr.Y = starty; mr.Height = 0; } else if (starty < endy) { mr.Y = starty; mr.Height = endy - starty; } else { mr.Y = endy; mr.Height = starty - endy; }
            DrawRectangle(mr);
        }

        public static void DrawLineRect(int x,int y, int w, int h) {
            DrawRectangle(x, y, w, 1); // top
            DrawRectangle(x, y + h, w, 1); // bottom
            DrawRectangle(x, y, 1, h); // left
            DrawRectangle(x + w, y, 1, h); // right
        }
        #endregion

        static public void Log(string msg) {
#if qdebuglog
            mLog(msg);
#endif
        }

        static public void RotateRAD(float rad) => me.rotation = rad;
        static public void RotateDEG(int deg) => me.rotation = (float)(deg*(Math.PI/180));
        static public void Scale(int ascale) => Scale(ascale, ascale);
        static public void Scale(int scx, int scy) { me.ScaleX = scx; me.ScaleY = scy; }
    }
    #endregion




}







