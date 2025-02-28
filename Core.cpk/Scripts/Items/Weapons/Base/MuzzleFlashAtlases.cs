﻿namespace AtomicTorch.CBND.CoreMod.Items.Weapons
{
    using AtomicTorch.CBND.GameApi.Resources;

    public static class MuzzleFlashAtlases
    {
        /// <summary>
        /// Used for artillery cannot equipped on a mech.
        /// </summary>
        public static readonly TextureAtlasResource AtlasArtillery
            = new("FX/MuzzleFlash/AtlasArtillery",
                columns: 6,
                rows: 2,
                isTransparent: true);

        /// <summary>
        /// Used for blue laser weapons.
        /// </summary>
        public static readonly TextureAtlasResource AtlasLaserBlue
            = new("FX/MuzzleFlash/AtlasLaserBlue",
                columns: 6,
                rows: 2,
                isTransparent: true);

        /// <summary>
        /// Used for red laser weapons.
        /// </summary>
        public static readonly TextureAtlasResource AtlasLaserRed
            = new("FX/MuzzleFlash/AtlasLaserRed",
                columns: 6,
                rows: 2,
                isTransparent: true);

        /// <summary>
        /// Used for modern military firearms such as machine pistols and rifles.
        /// </summary>
        public static readonly TextureAtlasResource AtlasNoSmokeLarge
            = new("FX/MuzzleFlash/AtlasNoSmokeLarge",
                columns: 6,
                rows: 2,
                isTransparent: true);

        /// <summary>
        /// Used for modern small arms such as handgun.
        /// </summary>
        public static readonly TextureAtlasResource AtlasNoSmokeSmall
            = new("FX/MuzzleFlash/AtlasNoSmokeSmall",
                columns: 6,
                rows: 2,
                isTransparent: true);

        /// <summary>
        /// Used for modern firearms with wider bore diameter, such as shotguns.
        /// </summary>
        public static readonly TextureAtlasResource AtlasNoSmokeWide
            = new("FX/MuzzleFlash/AtlasNoSmokeWide",
                columns: 6,
                rows: 2,
                isTransparent: true);

        /// <summary>
        /// Used for plasma weapons.
        /// </summary>
        public static readonly TextureAtlasResource AtlasPlasma
            = new("FX/MuzzleFlash/AtlasPlasma",
                columns: 6,
                rows: 2,
                isTransparent: true);

        /// <summary>
        /// Used for primitive rifles such as musket.
        /// </summary>
        public static readonly TextureAtlasResource AtlasSmokeLarge
            = new("FX/MuzzleFlash/AtlasSmokeLarge",
                columns: 6,
                rows: 2,
                isTransparent: true);

        /// <summary>
        /// Used for primitive pistols such as flintlock pistol.
        /// </summary>
        public static readonly TextureAtlasResource AtlasSmokeSmall1
            = new("FX/MuzzleFlash/AtlasSmokeSmall1",
                columns: 6,
                rows: 2,
                isTransparent: true);

        /// <summary>
        /// Used for black powder pistols such as luger and revolver.
        /// </summary>
        public static readonly TextureAtlasResource AtlasSmokeSmall2
            = new("FX/MuzzleFlash/AtlasSmokeSmall2",
                columns: 6,
                rows: 2,
                isTransparent: true);
    }
}