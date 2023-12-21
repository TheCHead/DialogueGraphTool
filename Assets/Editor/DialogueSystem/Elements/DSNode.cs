using System;
using System.Collections.Generic;
using System.Linq;
using DS.Data.Save;
using DS.Utilities;
using DS.Windows;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DS.Elements
{
    using  Enumerations;
    
    public class DSNode : Node
    {
        public string ID { get; set; }
        public string DialogueName { get; set; }
        public List<DSChoiceSaveData> Choices { get; set; }
        public string Text { get; set; }
        public DSDialogueType DSDialogueType { get; set; }
        public DSGroup Group { get; set; }

        private Color _defauldBackgroundColor;
        protected DSGraphView _graphView;

        public virtual void Initialize(string nodeName, DSGraphView graphView, Vector2 position)
        {
            ID = Guid.NewGuid().ToString();
            _graphView = graphView;
            
            DialogueName = nodeName;
            Choices = new();
            Text = "Dialogue text";
            SetPosition(new Rect(position, Vector2.zero));

            mainContainer.AddToClassList("ds-node__main-container" );
            extensionContainer.AddToClassList("ds-node__extension-container");

            _defauldBackgroundColor = new Color(29f/255f, 29f/255f, 30f/255f);
        }

        public virtual void Draw()
        {
            // Title container
            
            TextField dialogueNameTextField = DSElementUtility.CreateTextField(DialogueName, onValueChanged:callback =>
            {
                TextField target = (TextField) callback.target;
                target.value = callback.newValue.RemoveWhitespaces().RemoveSpecialCharacters();

                if (string.IsNullOrEmpty(target.value))
                {
                    if (!string.IsNullOrEmpty(DialogueName))
                    {
                        ++_graphView.NameErrorsAmount;
                    }
                }

                else
                {
                    if (string.IsNullOrEmpty(DialogueName))
                    {
                        --_graphView.NameErrorsAmount;
                    }
                }
                
                if (Group == null )
                {
                    _graphView.RemoveUngroupedNode(this );
                    DialogueName = target.value ;
                    _graphView.AddUngroupedNode(this);
                }

                else
                {
                    DSGroup curGroup = Group;
                    _graphView.RemoveGroupedNode(this, Group);
                    DialogueName = callback.newValue;
                    _graphView.AddGroupedNode(this, curGroup);
                }
            });
 
            dialogueNameTextField.AddClasses(
                "ds-node__textfield",
                "ds-node__filename-textfield",
                "ds-node__textfield__hidden"
            );

            this.titleContainer.Insert(0, dialogueNameTextField);

            // Input container
            Port inputPort = this.CreatePort("Dialogue Connection", Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            this.inputContainer.Add(inputPort);
            
            // Extension container
            VisualElement customDataContainer = new VisualElement();
            
            customDataContainer.AddToClassList("ds-node__custom-data-container");

            Foldout textFoldout = DSElementUtility.CreateFoldout("Dialogue Text");
            
            TextField dialogueTextField = DSElementUtility.CreateTextArea(Text, null, callback =>
            {
                Text = callback.newValue;
            });

            dialogueTextField.AddClasses(
                "ds-node__textfield",
                "ds-node__quote-textfield"
            );

            textFoldout.Add(dialogueTextField);
            customDataContainer.Add(textFoldout);
            this.extensionContainer.Add(customDataContainer);
        }
        
        #region OverridenMethods

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Disconnect Input Ports", action => DisconnectInputPorts());
            evt.menu.AppendAction("Disconnect Output Ports", action => DisconnectOutputPorts());

            base.BuildContextualMenu(evt);
        }

        #endregion
        

        #region Utilities

        public void DisconnectAllPorts()
        {
            DisconnectInputPorts();
            DisconnectOutputPorts();
        }

        private void DisconnectInputPorts()
        {
            DisconnectPorts(inputContainer);
        }

        private void DisconnectOutputPorts()
        {
            DisconnectPorts(outputContainer);
        }
        
        private void DisconnectPorts(VisualElement container)
        {
            foreach (Port port in container.Children())
            {
                if (!port.connected)
                    continue;
                
                _graphView.DeleteElements(port.connections);
            }
        }

        public bool IsStartingNode()
        {
            Port inputPort = (Port)inputContainer.Children().First();

            return !inputPort.connected;
        }

        public void SetErrorStyle(Color color)
        {
            mainContainer.style.backgroundColor = color;
        }

        public void ResetStyle()
        {
            mainContainer.style.backgroundColor = _defauldBackgroundColor;
        }
        #endregion
    }
}
