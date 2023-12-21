using DS.Data.Save;
using DS.Enumerations;
using DS.Utilities;
using DS.Windows;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DS.Elements
{
    public class DSMultipleChoiceNode : DSNode
    {
        public override void Initialize(string nodeName, DSGraphView graphView, Vector2 position)
        {
            base.Initialize(nodeName, graphView, position);
            DSDialogueType = DSDialogueType.MultipleChoice;
            DSChoiceSaveData choiceData = new DSChoiceSaveData()
            {
                Text = "New Choice"
            };
            Choices.Add(choiceData);
        }

        public override void Draw()
        {
            base.Draw();

            // Main container
            Button addChoiceButton = DSElementUtility.CreateButton("Add Choice", () =>
            {
                DSChoiceSaveData choiceData = new DSChoiceSaveData()
                {
                    Text = "New Choice"
                };
                Choices.Add(choiceData);
                Port outputPort = CreateChoicePort(choiceData);
                outputContainer.Add(outputPort);
            });
            
            addChoiceButton.AddToClassList("ds-node__button");
            
            mainContainer.Insert(1, addChoiceButton);

            // Output container
            foreach (var choice in Choices)
            {
                Port outputPort = CreateChoicePort(choice);

                outputContainer.Add(outputPort);
            }
                        
            RefreshExpandedState();
        }

        #region Element creation

        private Port CreateChoicePort(object userData)
        {
            Port choicePort = this.CreatePort();

            choicePort.userData = userData;
            DSChoiceSaveData choiceData = (DSChoiceSaveData)userData; 

            Button deleteChoiceButton = DSElementUtility.CreateButton("X", () =>
            {
                if (Choices.Count == 1)
                    return;

                if (choicePort.connected)
                {
                    _graphView.DeleteElements(choicePort.connections);
                }

                Choices.Remove(choiceData);
                _graphView.RemoveElement(choicePort);
            });

            deleteChoiceButton.AddToClassList("ds-node__button");

            TextField choiceTextField = DSElementUtility.CreateTextField(choiceData.Text, null, callback =>
            {
                choiceData.Text = callback.newValue;
            });

            choiceTextField.AddClasses(
                "ds-node__textfield",
                "ds-node__choice-textfield",
                "ds-node__textfield__hidden"
            );

            choicePort.Add(choiceTextField);
            choicePort.Add(deleteChoiceButton);
            return choicePort;
        }

        #endregion
    }
}
