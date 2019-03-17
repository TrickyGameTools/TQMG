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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using UseJCR6;

namespace TrickyUnits
{

#region image
class TQMGImage{
}
#endregion

#region text
class TQMGFont{
}

    #endregion




    #region core
    class Class_TQMG {

        readonly GraphicsDeviceManager gfxm;
        readonly SpriteBatch spriteBatch;
        readonly TJCRDIR ajcr;
        readonly GraphicsDevice gfxd;

        public Class_TQMG(GraphicsDeviceManager agfxm, GraphicsDevice agfxd, SpriteBatch aSB, TJCRDIR ajcr) {
            #region MKL
            MKL.Lic("TQMG - TQMG.cs", "Mozilla Public License 2.0");
            MKL.Version("TQMG - TQMG.cs", "19.03.17");
            #endregion

            #region TQMG core setup
            gfxm = agfxm;
            gfxd = agfxd;
            spriteBatch = aSB;
            #endregion
        }

    }

    #endregion




}
