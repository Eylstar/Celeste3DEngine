using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> Data structure that holds all the properties for rendering text </summary>
public class TextData
{
    internal Action onDirty;
    internal Action<int> onFontSizeChanged;
    
    private string _content = "Text";
    private Vector2 _position = Vector2.Zero;
    private float _scale = 1f;
    private float _alpha = 1f;
    private Color _mainColor = Color.White;
    private bool _shadow = true;
    private Color _shadowColor = Color.Black;
    private float _shadowOffset = 4f;
    private float _rotation = 0f;
    private float _spacing = 0f;
    private TextAlignment _align = TextAlignment.Center;
    private bool _visible = true;
    private float _lineHeightMult = 1f;
    private int _fontSize = 16;
    private int _pixelsPerUnit = 300;
    
    public TextData() { }

    public TextData(TextData data)
    {
        _content = data._content;
        _position = data._position;
        _scale = data._scale;
        _alpha = data._alpha;
        _mainColor = data._mainColor;
        _shadow = data._shadow;
        _shadowColor = data._shadowColor;
        _shadowOffset = data._shadowOffset;
        _rotation = data._rotation;
        _spacing = data._spacing;
        _align = data._align;
        _visible = data._visible;
        _lineHeightMult = data._lineHeightMult;
        _fontSize = data._fontSize;
        _pixelsPerUnit = data._pixelsPerUnit;
    }
    
    
    /// <summary> Position of the text in screen coordinates or world coordinates depending on the Canvas Type </summary>
    public Vector2 position
    {
        get => _position;
        set { _position = value; onDirty?.Invoke(); }
    }

    /// <summary> Font size </summary>
    public int fontSize
    {
        get => _fontSize;
        set { _fontSize = value; onFontSizeChanged?.Invoke(value); onDirty?.Invoke(); }
    }
    
    /// <summary> Content of the text to display </summary>
    public string content
    {
        get => _content;
        set { _content = value; onDirty?.Invoke(); }
    }
    
    /// <summary> Whether to draw a shadow behind the text </summary>
    public bool shadow
    {
        get => _shadow;
        set { _shadow = value; onDirty?.Invoke(); }
    }
    
    /// <summary> Offset of the shadow in pixels </summary>
    public float shadowOffset
    {
        get => _shadowOffset;
        set { _shadowOffset = value; onDirty?.Invoke(); }
    }
    
    /// <summary> Spacing between characters in pixels </summary>
    public float spacing
    {
        get => _spacing;
        set { _spacing = value; onDirty?.Invoke(); }
    }
    
    /// <summary> Line height multiplier </summary>
    public float lineHeightMult
    {
        get => _lineHeightMult;
        set { _lineHeightMult = value; onDirty?.Invoke(); }
    }
    
    /// <summary> Main color of the text </summary>
    public Color mainColor
    {
        get => _mainColor;
        set { _mainColor = value; onDirty?.Invoke(); }
    }
    
    /// <summary> Color of the shadow </summary>
    public Color shadowColor
    {
        get => _shadowColor;
        set { _shadowColor = value; onDirty?.Invoke(); }
    }
    
    /// <summary> Alpha transparency of the text (0 = fully transparent, 1 = fully opaque) </summary>
    public float alpha
    {
        get => _alpha;
        set { _alpha = value; onDirty?.Invoke(); }
    }
    
    /// <summary> Scale of the text </summary>
    public float scale
    {
        get => _scale;
        set { _scale = value; onDirty?.Invoke(); }
    }
    
    /// <summary> Pixels per unit for world text only. Higher values make the text smaller in world space but more clear. Adjust with fontSize </summary>
    public int pixelsPerUnit
    {
        get => _pixelsPerUnit;
        set { _pixelsPerUnit = value; onDirty?.Invoke(); }
    }
    
    /// <summary> Rotation of the text in degrees </summary>
    public float rotation
    {
        get => _rotation;
        set { _rotation = value; onDirty?.Invoke(); }
    }
    
    /// <summary> Alignment of the text. Right/Center/Left </summary>
    public TextAlignment align
    {
        get => _align;
        set { _align = value; onDirty?.Invoke(); }
    }
    
    /// <summary> Whether the text is visible </summary>
    public bool visible
    {
        get => _visible;
        set { _visible = value; }
    }
}

public enum TextAlignment
{
    Left,
    Center,
    Right
}