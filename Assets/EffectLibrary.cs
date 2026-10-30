
using UnityEngine;
using System; 
using System.Collections; 
using System.Collections.Generic; 


public class EffectLibrary 
{
    public Dictionary<CharacterData, CharacterEffectCombo> effects = new(); 
}

public class CharacterData
{

}

[Serializable]
public class CharacterEffectCombo
{
    public CharacterData data;
    public WritingStyle nameWritingStyle; 
    public WritingStyle nameErasingStyle; 

    public WritingStyle textWritingStyle; 
    public WritingStyle textErasingStyle; 

    public WritingStyle optionsWritingStyle; 
    public WritingStyle optionsErasingStyle; 
}