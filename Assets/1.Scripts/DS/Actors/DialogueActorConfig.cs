using System;
using System.Collections.Generic;
using DS.ScriptableObjects;
using UnityEngine;

namespace DS.Actors
{
    [CreateAssetMenu(fileName = "NewActorConfig", menuName = "Dialogues/NewActorConfig", order = 0)]
    public class DialogueActorConfig : ScriptableObject
    {
        public List<AudioClip> TalkAudioClips;
        public float TalkAudioPeriod = 0.2f;
        public Color TextColor;
        public List<StateDialogue> StateDialogues;
        
        private Dictionary<WorldStates, DialogueAssetSO> _stateToDialogueMap = new ();

        private void OnEnable()
        {
            Debug.Log("DialogueConfigLoaded");
            foreach (StateDialogue stateDialogue in StateDialogues)
            {
                _stateToDialogueMap[stateDialogue.WorldState] = stateDialogue.DialogueAsset;
            }
        }

        public DialogueAssetSO GetStartDialogueAsset(WorldStates state)
        {
            //return StateDialogues.First(x => x.WorldState == state).Dialogue.GetStartDialogue();
            return _stateToDialogueMap[state];
        }
    }

    [Serializable]
    public class StateDialogue
    {
        public WorldStates WorldState;
        public DialogueAssetSO DialogueAsset;
    }
}

public enum WorldStates
{
    state0,
    state1,
    state2
}

