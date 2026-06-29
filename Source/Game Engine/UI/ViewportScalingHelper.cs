using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

sealed class ViewportScalingHelper
{
    public static void GetUiTransform(out float scale, out Vector2 offset)
    {
        const float refW = 1920f;
        const float refH = 1080f;

        Viewport vp = Engine.Viewport;

        float sx = vp.Width / refW;
        float sy = vp.Height / refH;

        scale = Math.Min(sx, sy);

        float uiW = refW * scale;
        float uiH = refH * scale;

        offset = new Vector2(
            (vp.Width - uiW) * 0.5f,
            (vp.Height - uiH) * 0.5f
        );
    }
}