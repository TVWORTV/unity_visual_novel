using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
#region Events

    public Action onDialogueStart, onDialogueEnd;
    
    public Action<TextPart> onPartEnter, onPartExit;

    public Action<OptionsTable> onOptionsShown;
    public Action<OptionsTable, Option> onOptionChosen;

    public Action<ConditionTable> onConditionTableEnter; 
    public Action<ConditionEntry> onConditionEntryShown; 

#endregion

#region Assignments 

    public Writer writer;

#endregion

}
