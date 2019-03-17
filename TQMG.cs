// Lic:
// TQMG.cs
// (c)  Jeroen Petrus Broks.
// 
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not
// distributed with this file, You can obtain one at
// http://mozilla.org/MPL/2.0/.
// Version: 19.03.17
// EndLic

#region Yeah, we're using this.... Any questions?
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using UseJCR6;
#endregion

namespace TrickyUnits { 

#region image
class TQMGImage{
        Class_TQMG Mama;
}
#endregion

#region text
class TQMGFont{
        readonly Class_TQMG Mama;
        readonly string jcrdir;
        readonly bool ok;

        public TQMGFont(Class_TQMG parent, string dir) {
            Mama = parent;
            jcrdir = dir.ToUpper();
            ok = false;            
            foreach(string ent in Mama.jcr.Entries.Keys) {
                ok = ok || qstr.Prefixed($"{ent}/", jcrdir);
            }
            if (!ok) {
                Mama.Error($"JCR6 resource does not appear to have font folder \"{dir}\"!");
            }
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

        public void Error(string em) {
            if (CRASH && em!="Ok") throw new System.Exception(em);
            LastError = em;
        }

        public Class_TQMG(GraphicsDeviceManager agfxm, GraphicsDevice agfxd, SpriteBatch aSB, TJCRDIR ajcr) {
            #region MKL
            MKL.Lic("TQMG - TQMG.cs", "Mozilla Public License 2.0");
            MKL.Version("TQMG - TQMG.cs", "19.03.17");
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


    }

    #endregion




}
