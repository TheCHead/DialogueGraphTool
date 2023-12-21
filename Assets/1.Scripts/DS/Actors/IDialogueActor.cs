using System;
using DS.Actors;
using UnityEngine;
using UnityEngine.Events;

namespace DS.Actors
{
    public interface IDialogueActor
    {
        public DialogueActorConfig Config { get; }
        public UnityEvent OnStartDialogue { get; }
        public UnityEvent OnEndDialogue { get; }
        
        public void StartDialogue(DialogueAssetSO dialogue = null);
        public void EndDialogue();
        public Transform GetTransform();
    }
}
