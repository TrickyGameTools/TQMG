// Lic:
// TQMG.cs
// (c)  Jeroen Petrus Broks.
// 
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not
// distributed with this file, You can obtain one at
// http://mozilla.org/MPL/2.0/.
// Version: 19.03.19
// EndLic

#undef qdebuglog


#region Yeah, we're using this.... Any questions?
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using UseJCR6;
#endregion

namespace TrickyUnits { 

#region image
class TQMGImage{
        #region Image declarations
        readonly Class_TQMG Mama;
        readonly Texture2D[] tex;
        int hotx = 0, hoty = 0;
        int wdth = -1, hght = -1;
        bool allowvar = false;        
        #endregion


        #region Image Construction Site
        public TQMGImage(Class_TQMG parent, TJCRDIR JCR, string file)
        {
            parent.Error("Ok");
            Mama = parent;
            var bt = JCR.ReadFile(file);
            tex = new Texture2D[1];
            tex[0] = Texture2D.FromStream(Mama.gfxd, bt.GetStream());
            bt.Close();
            if (tex == null) parent.Error($"I could not load {file} from JCR resource");
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
            tex = new Texture2D[1];
            tex[0] = Texture2D.FromStream(Mama.gfxd, bt.GetStream());
            bt.Close();
            if (tex == null) parent.Error($"I could not load {file} from JCR resource");
        }

        public TQMGImage(Class_TQMG parent, int length, bool avar)
        {
            // This will be used by the font manager.
            tex = new Texture2D[length];
            allowvar = avar;
            Mama = parent;
        }

        public TQMGImage(Class_TQMG parent, TJCRDIR JCR,string bundle,int max) {
            var b = bundle.ToUpper();
            var fls = new List<string>();
            if (qstr.Suffixed(b,"/")) b += "/";
            var count = 0;
            foreach(string en in JCR.Entries.Keys) {
                if (qstr.Prefixed(en,b) && (max==0 || count < max) && (qstr.Suffixed(en,".PNG") || qstr.Suffixed(en,".JPG"))) {
                    fls.Add(en);
                    count++;
                }                
            }
            var files = fls.ToArray();
            tex = new Texture2D[count];
            for (int i=0;i<count;i++) {
                var bt = Mama.jcr.ReadFile($"{b}{files[i]}");
                tex[0] = Texture2D.FromStream(Mama.gfxd, bt.GetStream());
                bt.Close();
            }
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
                if (wdth == -1) wdth = tex[0].Width;
                return wdth;
            }
        }
        #endregion


        #region Font only stuff
        public void IRequire(int idx,string entry) {
            // This function has only been brought in to enable the font text generator to only load characters when they are actually needed.
            // Saves loading times, and also RAM you never needed. Or does it?
            try {
                if (tex[idx] != null) return;
                TQMG.Log($"IRequire({idx},\"{entry}\");");
                var bt = Mama.jcr.ReadFile(entry);                
                tex[idx] = Texture2D.FromStream(Mama.gfxd, bt.GetStream());
                bt.Close();
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


        #region Welcome to the SHOW
        Vector2 dc = new Vector2();
        public void Draw(int x,int y, int Frame = 0) {
            dc.X = x;
            dc.Y = y;
            Mama.spriteBatch.Draw(tex[Frame], dc, Mama.mColor);
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

        readonly public int Width;
        readonly public int Height;

        public TQMGText(TQMGFont parentfont, string text) {
            var x = 0;
            Height = 0;
            Ouwe = parentfont;
            for(int i = 0; i < text.Length; i++) {
                var l = new Letter();
                var c = (byte)text[i];
                int w=0, h=0;
                if (c == 32)
                    x += Height/2;
                else {
                    l.x = x;
                    Ouwe.fimg.IRequire(c, $"{Ouwe.jcrdir}{c}.png");
                    if (Ouwe.fimg.IGot(c)) {
                        Ouwe.fimg.IGetF(c, ref w, ref h);
                        l.x = x;
                        l.index = c;
                        TQMG.Log($"Char {text[i]} ({c}) listed in. >> x={x}; w={w}; h={h}");
                        x += w+1;
                        Width = x;
                        if (Height < h) Height = h;
                        FSTRING.Add(l);
                    }
                }
            }
        }

        public void Draw(int x,int y,TQMG_TextAlign align=TQMG_TextAlign.Left) {
            var M = Ouwe.Mama;
            var bx = 0;
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
        }

    }

    class TQMGFont{
        readonly public Class_TQMG Mama;
        readonly public string jcrdir;
        readonly bool ok;
        readonly public TQMGImage fimg;
        public bool crashnothave = false;
      

        public TQMGFont(Class_TQMG parent, string dir) {
            Mama = parent;
            jcrdir = dir.ToUpper();
            if (!qstr.Suffixed(jcrdir,"/")) jcrdir += "/";
            ok = false;            
            foreach(string ent in Mama.jcr.Entries.Keys) {
                ok = ok || (qstr.Prefixed(ent, jcrdir) && qstr.Suffixed(ent,".PNG"));
            }
            if (!ok) {
                Mama.Error($"JCR6 resource does not appear to have font folder \"{dir}\"!");
            }
            fimg = new TQMGImage(Mama, 256, true); // For now ASCII only... more may be supported later.
        }

        public TQMGText Text(string text) => new TQMGText(this, text);
        public void DrawText(string text, int x, int y, TQMG_TextAlign align = TQMG_TextAlign.Left) {
            var txt = Text(text);
            txt.Draw(x, y, align);
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

        public Color mColor = new Color(255, 255, 255);
        public float rotation = 0;
        //public Rectangle srcRec=null;

        public void Error(string em) {
            if (CRASH && em!="Ok") throw new System.Exception(em);
            LastError = em;
        }

        public Class_TQMG(GraphicsDeviceManager agfxm, GraphicsDevice agfxd, SpriteBatch aSB, TJCRDIR ajcr) {
            #region MKL
            MKL.Lic    ("TQMG - TQMG.cs","Mozilla Public License 2.0");
            MKL.Version("TQMG - TQMG.cs","19.03.19");
            #endregion

            #region TQMG core setup
            gfxm = agfxm;
            gfxd = agfxd;
            spriteBatch = aSB;
            jcr = ajcr;
            #endregion
        }

        public TQMGFont GetFont(string dir) {
            LastError = "Ok";
            return new TQMGFont(this, dir);
        }

        public TQMGImage GetImage(string image) => new TQMGImage(this, image);

        public int ScrWidth => gfxd.PresentationParameters.BackBufferWidth;
        public int ScrHeight => gfxd.PresentationParameters.BackBufferHeight;

    }
    #endregion

    #region Link ups....
    /* The class above is only available as a 'regular' class in case you want more than one object for whatever reason. 
     * Since I rarely expect to need this the class below just serves as the main thing.
     */
    static class TQMG {
        static Class_TQMG me;
        static public void Init(GraphicsDeviceManager agfxm, GraphicsDevice agfxd, SpriteBatch aSB, TJCRDIR ajcr) { me = new Class_TQMG( agfxm,  agfxd,  aSB, ajcr); }
        static public TQMGFont GetFont(string dir) => me.GetFont(dir);
        static public TQMGImage GetImage(string imagefile) => me.GetImage(imagefile);
        static public int ScrWidth => me.ScrHeight;
        static public int ScrHeight => me.ScrHeight;
        static public void UglyTile(TQMGImage img, int x, int y, int w, int h, int frame=0) { // Tiles, but doesn't take viewports in mind, so when textures stick out, so be it.
            for (int ix = x; ix < x + w; ix+=img.Width)
                for (int iy = y; iy < y + h; iy+=img.Height)
                    img.Draw(ix, iy,frame);
        }
        static public void Color(byte r, byte g, byte b) {
            me.mColor.R = r;
            me.mColor.G = g;
            me.mColor.B = b;
        }

        static public void Log(string msg) {
#if qdebuglog
            TeddyEdit.ProjectData.Log(msg);
#endif
        }
    }
    #endregion




}
