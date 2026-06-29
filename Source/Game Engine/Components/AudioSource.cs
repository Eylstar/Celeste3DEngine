using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Monocle;

using NVorbis;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> A component that can play a sound effect in 3D space. </summary>
public class AudioSource : Behaviour
{
    SoundEffect effect;
    SoundEffectInstance instance;
    DynamicSoundEffectInstance dynamicInstance;
    
    /// <summary> The volume of the sound effect, from 0.0 (silent) to 1.0 (full volume) </summary>
    public float volume = 1f;
    
    /// <summary> Whether the sound effect should loop when it reaches the end </summary>
    public bool loop = false;
    
    AudioEmitter emitter = new AudioEmitter();
    
    /// <summary> Load a sound effect from the given path. The path should be inside the audio folder and without extension </summary>
    public void LoadSound(string path, string name = "")
    {
        effect = AudioCache.GetSound(path);
    }
    
    /// <summary> Play the loaded sound effect. If no sound effect is loaded, this will log a warning. </summary>
    public void Play()
    {
        if (effect == null)
        {
            Logger.Warn("AudioSource", $"No sound effect loaded for GameObject '{gameObject.name}'. Call LoadSound() before Play().");
            return;
        }
        
        instance?.Dispose();
        instance = effect.CreateInstance();
        instance.Volume = volume;
        instance.IsLooped = loop;
        emitter.Position = transform.Position;
        instance.Apply3D(Scene3D.audioListener, emitter);
        instance?.Play();
    }
    
    /// <summary> Stop the currently playing sound effect, if any. </summary>
    public void Stop()=> instance?.Stop();


    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        if (instance == null || instance.State == SoundState.Stopped) return;
        
        emitter.Position = transform.Position;
        instance.Apply3D(Scene3D.audioListener, emitter);
        instance.Volume = volume;
    }

    public override void Removed()
    {
        base.Removed();
        instance?.Stop();
        instance?.Dispose();
    }
}

internal class AudioCache
{
    internal static Dictionary<string, SoundEffect> audioCache = new();
    
    internal static SoundEffect GetSound(string path)
    {
        if (audioCache.TryGetValue(path, out SoundEffect effect)) 
            return effect;
        else
            return LoadSound(path);
    } 
        
    internal static SoundEffect LoadSound(string path)
    {
        SoundEffect effect;
        
        ModAsset asset;
        string assetPath = EnginePaths.CustomAudioPath + $"/{path}";
        
        //WAV loading
        if (Everest.Content.TryGet(assetPath + ".wav", out asset))
        {
            Logger.Info("AudioLoader", $"Loading WAV audio asset '{path}'");
            using (Stream stream = asset.Stream)
            {
                effect = SoundEffect.FromStream(stream);
            }
            audioCache[path] = effect;
            return effect;
        }
        
        //OGG loading
        else if (Everest.Content.TryGet(assetPath + ".ogg", out asset))
        {
            Logger.Info("AudioLoader", $"Loading OGG audio asset '{path}'");
            using (Stream stream = asset.Stream)
            {
                using VorbisReader reader = new VorbisReader(stream, false);
                
                int channels = reader.Channels;
                int sampleRate = reader.SampleRate;
                
                float[] floatBuffer = new float[reader.TotalSamples * channels];
                reader.ReadSamples(floatBuffer, 0, floatBuffer.Length);
                
                short[] shortBuffer = new short[floatBuffer.Length];
                for (int i = 0; i < floatBuffer.Length; i++)
                    shortBuffer[i] = (short)(floatBuffer[i] * short.MaxValue);
                
                byte[] byteBuffer = new byte[shortBuffer.Length * 2];
                Buffer.BlockCopy(shortBuffer, 0, byteBuffer, 0, byteBuffer.Length);
                
                effect = new SoundEffect(byteBuffer, sampleRate, (AudioChannels)channels);
            }
            audioCache[path] = effect;
            return effect;
        }
        else
        {
            Logger.Warn("AudioCache", $"Failed to load audio asset '{assetPath}'. Make sure the file exists and is in .wav format.");
            return null;
        }
    }
    
    internal static void UnloadAll()
    {
        foreach (var kvp in audioCache)
            kvp.Value.Dispose();
        audioCache.Clear();
    }
}