using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DS.ScriptableObjects;
using DS.Utilities;
using UnityEditor;
using UnityEngine;

namespace DS.Inspectors
{
    [CustomEditor(typeof(DSDialogue))]
    public class DSInspector : Editor
    {
        /* Dialogue Scriptable Objects*/
        private SerializedProperty _dialogueContainerProperty;
        private SerializedProperty _dialogueGroupProperty;
        private SerializedProperty _dialogueProperty;
        
        
        /* Filters */
        private SerializedProperty _groupedDialoguesProperty;
        private SerializedProperty _startingDialoguesOnlyProperty;
        
        /* Indexes */
        private SerializedProperty _selectedDialogueGroupIndexProperty;
        private SerializedProperty _selectedDialogueIndexProperty;
        
        
        private void OnEnable()
        {
            _dialogueContainerProperty = serializedObject.FindProperty("dialogueContainer");
            _dialogueGroupProperty = serializedObject.FindProperty("dialogueGroup");
            _dialogueProperty = serializedObject.FindProperty("dialogue");

            _groupedDialoguesProperty = serializedObject.FindProperty("groupedDialogues");
            _startingDialoguesOnlyProperty = serializedObject.FindProperty("startingDialoguesOnly");

            _selectedDialogueGroupIndexProperty = serializedObject.FindProperty("selectedDialogueGroupIndex");
            _selectedDialogueIndexProperty = serializedObject.FindProperty("selectedDialogueIndex");
        }

        public override void OnInspectorGUI()
        {
            // Update to display current state of class instance data
            serializedObject.Update();
            
            DrawDialogueContainerArea();

            DSDialogueContainerSO dialogueContainer = 
                (DSDialogueContainerSO)_dialogueContainerProperty.objectReferenceValue;

            if (dialogueContainer == null)
            {
                StopDrawing("Select a Dialogue Container to continue");
                return;
            }
            
            DrawFiltersArea();

            bool startingDialoguesOnly = _startingDialoguesOnlyProperty.boolValue;

            List<string> dialogueNames;

            string dialogueFolderPath = $"Assets/DialogueSystem/Dialogues/{dialogueContainer.FileName}";

            string dialogueInfoMessage;
            
            if (_groupedDialoguesProperty.boolValue)
            {
                List<string> dialogueGroupNames = dialogueContainer.GetDialogueGroupNames();

                if (dialogueGroupNames.Count == 0)
                {
                    StopDrawing("There are no Dialogue Groups in selected Dialogue Container");
                    return;
                }
                DrawDialogueGroupArea(dialogueContainer, dialogueGroupNames);

                DSDialogueGroupSO dialogueGroup = (DSDialogueGroupSO)_dialogueGroupProperty.objectReferenceValue;
                dialogueNames = dialogueContainer.GetGroupedDialogueNames(dialogueGroup, startingDialoguesOnly);
                dialogueFolderPath += $"/Groups/{dialogueGroup.GroupName}/Dialogues";
                dialogueInfoMessage = "There are no" + (startingDialoguesOnly ? " Starting" : "") + " Dialogues in selected Dialogue Group";
            }
            
            else
            {
                dialogueNames = dialogueContainer.GetUngroupedDialogueNames(startingDialoguesOnly);
                dialogueFolderPath += "/Global/Dialogues";
                dialogueInfoMessage = "There are no " + (startingDialoguesOnly ? " Starting" : "") + " Ungrouped Dialogues in selected Dialogue Container";
            }

            if (dialogueNames.Count == 0)
            {
                StopDrawing(dialogueInfoMessage);
                return;
            }
            
            DrawDialogueArea(dialogueNames, dialogueFolderPath);

            // Apply properties' changes to class instance
            serializedObject.ApplyModifiedProperties();
        }


        #region Draw Methods

        private void DrawDialogueContainerArea()
        {
            DSInspectorUtility.DrawHeader("Dialogue Container");
            _dialogueContainerProperty.DrawPropertyField();
            DSInspectorUtility.DrawSpace();
        }

        private void DrawFiltersArea()
        {
            DSInspectorUtility.DrawHeader("Filters");
            _groupedDialoguesProperty.DrawPropertyField();
            _startingDialoguesOnlyProperty.DrawPropertyField();
            DSInspectorUtility.DrawSpace();
        }

        private void DrawDialogueGroupArea(DSDialogueContainerSO dialogueContainer, List<string> dialogueGroupNames)
        {
            DSInspectorUtility.DrawHeader("Dialogue Group");

            int oldSelectedDialogueGroupIndex = _selectedDialogueGroupIndexProperty.intValue;
            DSDialogueGroupSO oldDialogueGroup = (DSDialogueGroupSO)_dialogueGroupProperty.objectReferenceValue;
            bool isOldDialogueGroupNull = oldDialogueGroup == null;
            string oldDialogueGroupName = isOldDialogueGroupNull ? "" : oldDialogueGroup.GroupName;
            
            UpdateIndexOnOptionsUpdate(
                dialogueGroupNames, 
                _selectedDialogueGroupIndexProperty, 
                oldSelectedDialogueGroupIndex, 
                oldDialogueGroupName, 
                isOldDialogueGroupNull);

            _selectedDialogueGroupIndexProperty.intValue = 
                DSInspectorUtility.DrawPopup("Dialogue Group", _selectedDialogueGroupIndexProperty, dialogueGroupNames.ToArray());

            string selectedDialogueGroupName = dialogueGroupNames[_selectedDialogueGroupIndexProperty.intValue];
            
            DSDialogueGroupSO dialogueGroup = DSIOUtility.LoadAsset<DSDialogueGroupSO>(
                $"Assets/DialogueSystem/Dialogues/{dialogueContainer.FileName}/Groups/{selectedDialogueGroupName}", 
                selectedDialogueGroupName);
            _dialogueGroupProperty.objectReferenceValue = dialogueGroup;
            
            DSInspectorUtility.DrawDisabledFields(() => _dialogueGroupProperty.DrawPropertyField());
            DSInspectorUtility.DrawSpace();
        }

        private void DrawDialogueArea(List<string> dialogueNames, string dialogueFolderPath)
        {
            DSInspectorUtility.DrawHeader("Dialogue");

            int oldSelectedDialogueIndex = _selectedDialogueIndexProperty.intValue;
            DSDialogueSO oldDialogue = (DSDialogueSO)_dialogueProperty.objectReferenceValue;
            bool isOldDialogueNull = oldDialogue == null;
            string oldDialogueName = isOldDialogueNull ? "" : oldDialogue.DialogueName;
            
            UpdateIndexOnOptionsUpdate(dialogueNames, _selectedDialogueIndexProperty, oldSelectedDialogueIndex, oldDialogueName, isOldDialogueNull);
            
            _selectedDialogueIndexProperty.intValue = 
                DSInspectorUtility.DrawPopup("Dialogue", 
                    _selectedDialogueIndexProperty, 
                    dialogueNames.ToArray());

            string selectedDialogueName = dialogueNames[_selectedDialogueIndexProperty.intValue];

            DSDialogueSO selectedDialogue =
                DSIOUtility.LoadAsset<DSDialogueSO>(dialogueFolderPath, selectedDialogueName);

            _dialogueProperty.objectReferenceValue = selectedDialogue;
            
            DSInspectorUtility.DrawDisabledFields(() => _dialogueProperty.DrawPropertyField());
        }

        private void StopDrawing(string reason, MessageType messageType = MessageType.Info)
        {
            DSInspectorUtility.DrawHelpBox(reason, messageType);
            DSInspectorUtility.DrawSpace();
            DSInspectorUtility.DrawHelpBox("You need to select a Dialogue for this component to work properly at Runtime", MessageType.Warning);
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Index Methods

        private void UpdateIndexOnOptionsUpdate(
            List<string> optionNames, 
            SerializedProperty indexProperty,
            int oldSelectedPropertyIndex,
            string oldPropertyName,
            bool isOldPropertyNull)
        {
            if (isOldPropertyNull)
            {
                indexProperty.intValue = 0;
                return;
            }

            bool isOldIndexOutOfBounds = oldSelectedPropertyIndex > optionNames.Count - 1;
            bool isOldNameDifferent = isOldIndexOutOfBounds || oldPropertyName != optionNames[indexProperty.intValue];

            if (isOldNameDifferent)
            {
                if (optionNames.Contains(oldPropertyName))
                {
                    indexProperty.intValue = optionNames.IndexOf(oldPropertyName);
                }
                else
                {
                    indexProperty.intValue = 0;
                }
            }
        }

        #endregion
    }
}