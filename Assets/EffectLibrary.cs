
using UnityEngine;
using System; 
using System.Collections; 
using System.Collections.Generic; 


public class EffectLibrary 
{
    public Dictionary<CharacterData, CharacterWritingEffects> effects = new(); 
}

[Serializable]
public class CharacterWritingEffects
{
    public Style simpleTextStyle; 
    public Style linkStyle; 
    public Style optionsStyle; 
    public Style nameStyle; 
}

[Serializable]
public class Style 
{
    public WritingStyle writingStyle; 
    public IdleStyle idleStyle; 
}