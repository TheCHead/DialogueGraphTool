using DS.ScriptableObjects;
using UnityEngine;

namespace DS.Actors
{
    [CreateAssetMenu(fileName = "NewDialogueAsset", menuName = "Dialogues/NewDialogueAsset", order = 1)]
    public class DialogueAssetSO : ScriptableObject
    {
        /* Dialogue Scriptable Objects */
        [SerializeField] private DSDialogueContainerSO dialogueContainer;
        [SerializeField] private DSDialogueGroupSO dialogueGroup;
        [SerializeField] private DSDialogueSO dialogue;

        /* Filters */
        [SerializeField] private bool groupedDialogues;
        [SerializeField] private bool startingDialoguesOnly;
    
        /* Indexes */
        [SerializeField] private int selectedDialogueGroupIndex;
        [SerializeField] private int selectedDialogueIndex;

        public DSDialogueSO GetStartDialogue()
        {
            return dialogue;
        }
    }
}


