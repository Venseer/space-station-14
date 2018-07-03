﻿using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using System.Collections.Generic;
using SS14.Shared.Utility;

namespace SS14.Client.Map
{
    /// <summary>
    ///     Special TileDefinitionManager that makes a Godot TileSet for usage by TileMaps.
    /// </summary>
    public class ClientTileDefinitionManager : TileDefinitionManager, IClientTileDefinitionManager
    {
        [Dependency]
        readonly IResourceCache resourceCache;

        public Godot.TileSet TileSet { get; private set; } = new Godot.TileSet();

        private Dictionary<ushort, TextureResource> Textures = new Dictionary<ushort, TextureResource>();

        public override ushort Register(ITileDefinition tileDef)
        {
            var ret = base.Register(tileDef);

            TileSet.CreateTile(ret);
            if (!string.IsNullOrEmpty(tileDef.SpriteName))
            {
                var texture = resourceCache.GetResource<TextureResource>(new ResourcePath("/Textures/Tiles/") / $@"{tileDef.SpriteName}.png");
                TileSet.TileSetTexture(ret, texture.Texture.GodotTexture);
                Textures[ret] = texture;
            }

            return ret;
        }
    }
}
