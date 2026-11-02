
using UnityEngine;
using System; 
using System.Collections; 
using System.Collections.Generic; 


public class EffectLibrary 
{
    public Dictionary<CharacterData, CharacterWritingEffects> effects = new(); 
}

public class CharacterData
{

}

[Serializable]
public class CharacterWritingEffects
{
    public CharacterData data;
    public WritingStyle nameWritingStyle; 
    public WritingStyle nameErasingStyle; 

    public WritingStyle textWritingStyle; 
    public WritingStyle textErasingStyle; 

    public WritingStyle optionsWritingStyle; 
    public WritingStyle optionsErasingStyle; 
}

public class LinkStyle 
{
    
}