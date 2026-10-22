/*

    1. On clicking on text: 
        - if not finished writing and not in options, skips and finishes writing it. 
        - if finished and not in options, goes to next part; 
        - this does not apply at all if in options - reacts to option button 

*/ 

public class DialogueManager 
{
    public TextManager textManager; 

    public void StartDialogue(FullTextAsset textAsset)
    {
        currentAsset = textAsset; 
    }

#region current 

    public FullTextAsset currentAsset; 
    public TextPart currentPart = null; 
    public bool options => currentPart is ConditionTable; 

#endregion

#region Text State 

    public bool finishedWriting; 

#endregion

#region Interaction

    public List<Button> buttons = new(); 
    public void OnConditionTableEnter(ConditionTable part)
    {
        /// deactivates and removes all on click callbacks 
        /// actiavtes buttons based on the amount of ConditionEntries. 
        /// gives them callbacks that lead to the part from that ConditionEntry 
    }

    public void OnTextClick()
    {

    }

#endregion

}