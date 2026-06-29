using System.Collections.Generic;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Cil;


namespace Celeste.Mod.Celeste3DEngine;

internal class RenderingHooks
{
    internal static Renderer3D activeRenderer = null;
    
    internal static void AddRenderer(Renderer3D renderer) => activeRenderer = renderer;

	

	public static void IL_Level_Render(ILContext il)
	{
		ILCursor cursor = new(il);
		IL_Level_Render_3D_HDBG(cursor);
		cursor.Index = 0;
		IL_Level_Render_3D(cursor);
		cursor.Index = 0;
		IL_Level_Render_3D_Foreground(cursor);
	}
    
    
    public static void IL_Level_Render_3D_HDBG(ILCursor c) 
    {
	    if (c.TryGotoNextBestFit(MoveType.Before,
		        instr => instr.MatchLdnull(),
		        instr => instr.MatchCallvirt<GraphicsDevice>(nameof(GraphicsDevice.SetRenderTarget))
	        )
	        && c.TryGotoNextBestFit(MoveType.Before, instr => instr.MatchCallvirt<SpriteBatch>(nameof(SpriteBatch.Begin)))) 
	    {
		    c.EmitLdarg0();
		    c.EmitDelegate(Render3DHDBG);
	    }
	    else
		    Logger.Log(LogLevel.Error, "Celeste3DEngine", "Failed to insert Render 3D HD hook");
    }
    
    public static void IL_Level_Render_3D(ILCursor c) 
    {
	    if (c.TryGotoNextBestFit(MoveType.Before,instr => instr.MatchCall(typeof(Distort), nameof(Level.Render))))
	    {
		    c.EmitLdarg0();
		    c.EmitDelegate(Render3D);	
	    }
	    else
		    Logger.Log(LogLevel.Error, "Celeste3DEngine", "Failed to instert Render 3D hook");
    }
    
    public static void IL_Level_Render_3D_Foreground(ILCursor c)
    {
	    if (c.TryGotoNextBestFit(MoveType.Before,
		        instr => instr.MatchLdnull(),
		        instr => instr.MatchCallvirt<GraphicsDevice>(nameof(GraphicsDevice.SetRenderTarget))))
	    {
		    c.EmitLdarg0();
		    c.EmitDelegate(Render3DForeground);
	    }
	    else
		    Logger.Log(LogLevel.Error, "Celeste3DEngine", "Failed to insert Render 3D Foreground hook");
    }
    
    static void Render3DHDBG(Level level) 
    {
	    if (activeRenderer != null)
	    {
		    level.BackgroundColor = Color.Transparent;
		    activeRenderer.RenderHDBG();
	    }
    }
    
    static void Render3D(Level level) 
    {
	    if (activeRenderer != null)
		    activeRenderer.RenderWorld();
    }
    
    static void Render3DForeground(Level level) 
	{
	    if (activeRenderer != null && !activeRenderer.isHDMode)
		    activeRenderer.RenderForeground();
	}
    
    
	public static void ON_HUD_RenderHD(On.Celeste.Level.orig_Render orig, Level self)
	{
		orig(self);
		if (activeRenderer != null)
		{
			if (activeRenderer.isHDMode)
				activeRenderer.RenderForeground();
			
			activeRenderer.RenderOverlayCanvases();
		}
	}
}