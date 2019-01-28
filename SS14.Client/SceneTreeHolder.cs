﻿using SS14.Client.Interfaces;
using System;
using SS14.Client.Graphics;

namespace SS14.Client
{
    internal class SceneTreeHolder : ISceneTreeHolder
    {
        public Godot.CanvasLayer BelowWorldScreenSpace { get; private set; }
        public Godot.SceneTree SceneTree { get; private set; }
        public Godot.Node2D WorldRoot { get; private set; }

        public void Initialize(Godot.SceneTree tree)
        {
            SceneTree = tree ?? throw new ArgumentNullException(nameof(tree));

            BelowWorldScreenSpace = new Godot.CanvasLayer
            {
                Name = "ScreenSubWorld",
                Layer = CanvasLayers.LAYER_SCREEN_BELOW_WORLD
            };
            SceneTree.GetRoot().AddChild(BelowWorldScreenSpace);

            WorldRoot = new Godot.Node2D
            {
                Name = "WorldRoot"
            };
            SceneTree.GetRoot().AddChild(WorldRoot);
        }
    }
}
