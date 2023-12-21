using System.Collections.Generic;
using DS.Elements;
using DS.Enumerations;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DS.Windows
{
    public class DSSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private DSGraphView _graphView;
        private Texture2D _indentIcon;
        
        public void Initialize(DSGraphView dsGraphView)
        {
            _graphView = dsGraphView;
            _indentIcon = new Texture2D(1, 1);
            _indentIcon.SetPixel(0, 0, Color.clear);
            _indentIcon.Apply();
        }
        
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> searchTreeEntries = new List<SearchTreeEntry>()
            {
                 new SearchTreeGroupEntry(new GUIContent("Create Element")),
                 new SearchTreeGroupEntry(new GUIContent("Dialogue Node"), 1),
                 new SearchTreeEntry(new GUIContent("Single Choice", _indentIcon))
                 {
                     level = 2,
                     userData = DSDialogueType.SingleChoice
                 },
                 new SearchTreeEntry(new GUIContent("Multiple Choice", _indentIcon))
                 {
                     level = 2,
                     userData = DSDialogueType.MultipleChoice
                 },
                 new SearchTreeGroupEntry(new GUIContent("Dialogue Group"), 1),
                 new SearchTreeEntry(new GUIContent("Single Group", _indentIcon))
                 {
                     level = 2,
                     userData = new Group()
                 }
            };

            return searchTreeEntries;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            Vector2 localMousePos = _graphView.GetLocalMousePos(context.screenMousePosition, true);
            
            switch (SearchTreeEntry.userData)
            {
                case DSDialogueType.SingleChoice:
                {
                    DSSingleChoiceNode singleChoiceNode =
                        (DSSingleChoiceNode)_graphView.CreateNode("DialogueName", DSDialogueType.SingleChoice, localMousePos);
                    _graphView.AddElement(singleChoiceNode);
                    return true;
                }
                case DSDialogueType.MultipleChoice:
                {
                    DSMultipleChoiceNode multipleChoiceNode =
                        (DSMultipleChoiceNode)_graphView.CreateNode("DialogueName", DSDialogueType.MultipleChoice, localMousePos);
                    _graphView.AddElement(multipleChoiceNode);
                    return true;
                }
                case Group _:
                {
                    _graphView.CreateGroup("New Group", localMousePos);
                    return true;
                }
                default:
                    return false;
            }
        }
    }
}
