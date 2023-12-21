using DS.Data.Save;
using DS.Enumerations;
using DS.Utilities;
using DS.Windows;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DS.Elements
{
    public class DSSingleChoiceNode : DSNode
    {
        public override void Initialize(string nodeName, DSGraphView graphView, Vector2 position)
        {
            base.Initialize(nodeName, graphView, position);
            DSDialogueType = DSDialogueType.SingleChoice;
            DSChoiceSaveData choiceData = new DSChoiceSaveData()
            {
                Text = "Next Dialogue"
            };
            Choices.Add(choiceData);
        }

        public override void Draw()
        {
            base.Draw();
            
            // Output container
            foreach (var choice in Choices)
            {
                Port choicePort = this.CreatePort(choice.Text);
                choicePort.userData = choice;
                outputContainer.Add(choicePort);
            }
                        
            RefreshExpandedState();
        }
    }
}
