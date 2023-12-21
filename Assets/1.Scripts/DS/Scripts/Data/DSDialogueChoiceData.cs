using System;
using System.Collections;
using System.Collections.Generic;
using DS.ScriptableObjects;
using UnityEngine;

namespace DS.Data
{
    [Serializable]
    public class DSDialogueChoiceData
    {
        [field:SerializeField] public string Text { get; set; }
        [field:SerializeField] public DSDialogueSO NextDialogue { get; set;  }
    }
}

