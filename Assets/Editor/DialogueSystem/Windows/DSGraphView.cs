using System;
using System.Collections.Generic;
using DS.Data.Error;
using DS.Data.Save;
using DS.Elements;
using DS.Enumerations;
using DS.Utilities;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DS.Windows
{
    public class DSGraphView : GraphView
    {
        public int NameErrorsAmount
        {
            get
            {
                return _nameErrorsAmount;
            }
            set
            {
                _nameErrorsAmount = value;
                if (_nameErrorsAmount == 0)
                {
                    _editorWindow.EnableSaving();
                }

                if (_nameErrorsAmount == 1)
                {
                    _editorWindow.DisableSaving();
                }
            }
        }
        
        private DSEditorWindow _editorWindow;
        private DSSearchWindow _searchWindow;
        private MiniMap _miniMap;

        private SerializableDictionary<string, DSGroupErrorData> _groups;
        private SerializableDictionary<string, DSNodeErrorData> _ungroupedNodes;
        private SerializableDictionary<Group, SerializableDictionary<string, DSNodeErrorData>> _groupedNodes;

        private int _nameErrorsAmount;

        public DSGraphView(DSEditorWindow editorWindow)
        {
            _editorWindow = editorWindow;
            _groups = new SerializableDictionary<string, DSGroupErrorData>();
            _ungroupedNodes = new SerializableDictionary<string, DSNodeErrorData>();
            _groupedNodes = new SerializableDictionary<Group, SerializableDictionary<string, DSNodeErrorData>>();
            
            AddManipulators();
            AddSearchWindow();
            AddMinimap();
            AddGridBackground();
            OnElementsDeleted();
            OnGroupElementsAdded();
            OnGroupElementsRemoved();
            OnGroupRenamed();
            OnGraphViewChanged();

            AddStyles();
            AddMinimapStyles();
        }

        #region Overriden Methods

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatiblePorts = new List<Port>();
            ports.ForEach(port =>
            {
                if (startPort == port || 
                    startPort.node == port.node ||
                    startPort.direction == port.direction) 
                    return;

                compatiblePorts.Add(port);
            });
            return compatiblePorts; 
        }

        #endregion

        #region Manipulators

        private void AddManipulators()
        {
            //this.AddManipulator(new ContentZoomer());
            // or you can use below
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            this.AddManipulator(CreateNodeContextualMenu("Add Node (Single Choice)", DSDialogueType.SingleChoice));
            this.AddManipulator(CreateNodeContextualMenu("Add Node (Multiple Choice)", DSDialogueType.MultipleChoice));
            this.AddManipulator(CreateGroupContextualMenu());
        }

        private IManipulator CreateGroupContextualMenu()
        {
            ContextualMenuManipulator contextualMenuManipulator = new ContextualMenuManipulator(
                menuEvent => menuEvent.menu.AppendAction("Add Group", actionEvent => CreateGroup("DialogueGroup", GetLocalMousePos(actionEvent.eventInfo.localMousePosition)))
            );
            return contextualMenuManipulator; 
        }

        private IManipulator CreateNodeContextualMenu(string actionTitle, DSDialogueType dsDialogueType)
        {
            ContextualMenuManipulator contextualMenuManipulator = new ContextualMenuManipulator(
                menuEvent => menuEvent.menu.AppendAction(actionTitle, actionEvent => AddElement(CreateNode("DialogueName", dsDialogueType, GetLocalMousePos(actionEvent.eventInfo.localMousePosition))))
            );
            return contextualMenuManipulator;
        }

        #endregion

        #region Element Creation

        public DSNode CreateNode(string nodeName, DSDialogueType dsDialogueType, Vector2 position, bool shouldDraw = true)
        {
            Type nodeType = Type.GetType($"DS.Elements.DS{dsDialogueType}Node");
            DSNode node = (DSNode)Activator.CreateInstance(nodeType);
            node.Initialize(nodeName, this, position);

            if (shouldDraw)
            {
                node.Draw();
            }
            

            AddUngroupedNode(node);
            
            return node;
        }

        public DSGroup CreateGroup(string title, Vector2 localMousePosition)
        {
            DSGroup group = new DSGroup(title, localMousePosition);
            
            AddGroup(group);
            AddElement(group);

            foreach (GraphElement element in selection)
            {
                if (!(element is DSNode))
                    continue;

                DSNode node = (DSNode)element;
                group.AddElement(node);
            }
            
            
            
            return group;
        }

        #endregion

        #region Callbacks

        private void OnElementsDeleted()
        {
            deleteSelection = (operationName, user) =>
            {
                Type groupType = typeof(DSGroup);
                Type edgeType = typeof(Edge);

                List<DSGroup> groupsToDelete = new List<DSGroup>();
                List<Edge> edgesToDelete = new List<Edge>();
                List<DSNode> nodesToDelete = new List<DSNode>();
                
                foreach (GraphElement element in selection)
                {
                    if (element is DSNode node)
                    {
                        nodesToDelete.Add(node);
                        continue;
                    }

                    if (element.GetType() == edgeType)
                    {
                        Edge edge = (Edge)element;
                        edgesToDelete.Add(edge);
                        continue;
                    }

                    if (element.GetType() != groupType)
                    {
                        continue;
                    }

                    DSGroup group = (DSGroup)element;
                    
                    groupsToDelete.Add(group);
                }

                foreach (DSGroup dsGroup in groupsToDelete)
                {
                    List<DSNode> groupNodesList = new List<DSNode>();

                    foreach (GraphElement element in dsGroup.containedElements)
                    {
                        if (!(element is DSNode))
                        {
                            continue;
                        }
                        
                        groupNodesList.Add((DSNode)element);
                    }
                    
                    dsGroup.RemoveElements(groupNodesList);
                    RemoveGroup(dsGroup);
                    RemoveElement(dsGroup);
                }

                foreach (DSNode node in nodesToDelete)
                {
                    if (node.Group != null)
                    {
                        node.Group.RemoveElement(node);
                    }
                    RemoveUngroupedNode(node);
                    node.DisconnectAllPorts();
                    RemoveElement(node);
                }
                
                DeleteElements(edgesToDelete);
            };
        }

        private void OnGroupElementsAdded()
        {
            elementsAddedToGroup = (group, elements) =>
            {
                foreach (GraphElement element in elements)
                {
                    if (!(element is DSNode))
                    {
                        continue;
                    }

                    DSGroup nodeGroup = (DSGroup)group;
                    DSNode node = (DSNode)element;
                    RemoveUngroupedNode(node);
                    AddGroupedNode(node, nodeGroup);
                }
            };
        }

        private void OnGroupElementsRemoved()
        {
            elementsRemovedFromGroup = (group, elements) =>
            {
                foreach (GraphElement element in elements)
                {
                    if (!(element is DSNode))
                    {
                        continue;
                    }

                    DSNode node = (DSNode)element;
                    RemoveGroupedNode(node, group);
                    AddUngroupedNode(node);
                }
            };
        }

        private void OnGroupRenamed()
        {
            groupTitleChanged = (group, newTitle) =>
            {
                DSGroup dsGroup = (DSGroup)@group;
                group.title = newTitle.RemoveWhitespaces().RemoveSpecialCharacters();
                
                if (string.IsNullOrEmpty(dsGroup.title))
                {
                    if (!string.IsNullOrEmpty(dsGroup.OldTitle))
                    {
                        ++NameErrorsAmount;
                    }
                }

                else
                {
                    if (string.IsNullOrEmpty(dsGroup.OldTitle))
                    {
                        --NameErrorsAmount;
                    }
                }
                
                RemoveGroup(dsGroup);
                dsGroup.OldTitle = group.title;
                AddGroup(dsGroup);
            };
        }

        private void OnGraphViewChanged()
        {
            graphViewChanged = (changes) =>
            {
                if (changes.edgesToCreate != null)
                {
                    foreach (Edge edge in changes.edgesToCreate)
                    {
                        DSNode nextNode = (DSNode)edge.input.node;

                        DSChoiceSaveData choiceData = (DSChoiceSaveData)edge.output.userData;
                        choiceData.NodeId = nextNode.ID;
                    }
                }

                if (changes.elementsToRemove != null)
                {
                    Type edgeType = typeof(Edge);

                    foreach (GraphElement element in changes.elementsToRemove)
                    {
                        if (element.GetType() != edgeType)
                            continue;

                        Edge edge = (Edge)element;

                        DSChoiceSaveData choiceData = (DSChoiceSaveData)edge.output.userData;
                        choiceData.NodeId = "";
                        
                    }
                }
                
                return changes;
            };
        }

        #endregion

        #region RepeatedElement

        private void AddGroup(DSGroup @group)
        {
            string groupName = group.title.ToLower();

            if (!_groups.ContainsKey(groupName))
            {
                DSGroupErrorData errorData = new DSGroupErrorData();
                errorData.Groups.Add(group);
                _groups.Add(groupName, errorData);
                return;
            }
            
            _groups[groupName].Groups.Add(group);
            Color errorColor = _groups[groupName].ErrorData.Color;
            group.SetErrorStyle(errorColor);

            if (_groups[groupName].Groups.Count == 2)
            {
                ++NameErrorsAmount;
                _groups[groupName].Groups[0].SetErrorStyle(errorColor);
            }
        }

        private void RemoveGroup(DSGroup @group)
        {
            string oldGroupName  = group.OldTitle.ToLower();

            List<DSGroup> groupsList = _groups[oldGroupName].Groups;
            groupsList.Remove(group);
            group.ResetStyle();

            if (groupsList.Count == 1)
            {
                --NameErrorsAmount;
                groupsList[0].ResetStyle();
                return;
            }

            if (groupsList.Count == 0)
            {
                _groups.Remove(oldGroupName);
            } 
        }

        public void AddGroupedNode(DSNode node, DSGroup group)
        {
            string nodeName = node.DialogueName.ToLower();
            node.Group = group;

            if (!_groupedNodes.ContainsKey(group))
            {
                _groupedNodes.Add(group, new SerializableDictionary<string, DSNodeErrorData>());
            }

            if (!_groupedNodes[group].ContainsKey(nodeName))
            {
                DSNodeErrorData nodeErrorData = new DSNodeErrorData();
                nodeErrorData.Nodes.Add(node);
                _groupedNodes[group].Add(nodeName, nodeErrorData);
                return;
            }
            
            _groupedNodes[group][nodeName].Nodes.Add(node);
            Color errorColor = _groupedNodes[group][nodeName].ErrorData.Color;
            node.SetErrorStyle(errorColor);

            if (_groupedNodes[group][nodeName].Nodes.Count == 2)
            {
                ++NameErrorsAmount;
                _groupedNodes[group][nodeName].Nodes[0].SetErrorStyle(errorColor);
            }
        }

        public void RemoveGroupedNode(DSNode node, Group group)
        {
            string nodeName = node.DialogueName.ToLower();
            node.Group = null; 
            
            List<DSNode> groupedNodesList = _groupedNodes[group][nodeName].Nodes;
            
            groupedNodesList.Remove(node);
            node.ResetStyle();

            if (groupedNodesList.Count == 1)
            {
                --NameErrorsAmount;
                groupedNodesList[0].ResetStyle();
            }

            if (groupedNodesList.Count == 0)
            {
                _groupedNodes[group].Remove(nodeName);

                if (_groupedNodes[group].Count == 0)
                {
                    _groupedNodes.Remove(group);
                }
            }
        }

        public void AddUngroupedNode(DSNode node)
        {
            string nodeName = node.DialogueName.ToLower();
            if (!_ungroupedNodes.ContainsKey(nodeName))
            {
                DSNodeErrorData nodeErrorData = new DSNodeErrorData();
                nodeErrorData.Nodes.Add(node);
                _ungroupedNodes.Add(nodeName, nodeErrorData);
                return;
            }

            List<DSNode> ungroupedNodesList = _ungroupedNodes[nodeName].Nodes;
            
            ungroupedNodesList.Add(node);

            Color errorColor = _ungroupedNodes[nodeName].ErrorData.Color;
            node.SetErrorStyle(errorColor);

            if (ungroupedNodesList.Count == 2)
            {
                ++NameErrorsAmount;
                ungroupedNodesList[0].SetErrorStyle(errorColor);
            }
        }

        public void RemoveUngroupedNode(DSNode node)
        {
            string nodeName = node.DialogueName.ToLower();
            List<DSNode> ungroupedNodesList = _ungroupedNodes[nodeName].Nodes;
            
            ungroupedNodesList.Remove(node);
            node.ResetStyle();

            if (ungroupedNodesList.Count == 1)
            {
                --NameErrorsAmount;
                ungroupedNodesList[0].ResetStyle();
                return; 
            }
            
            if (ungroupedNodesList.Count == 0)
            {
                _ungroupedNodes.Remove(nodeName);
            }
        }

        #endregion

        #region Element Addition

        private void AddSearchWindow()
        {
            if (_searchWindow == null)
            {
                _searchWindow = ScriptableObject.CreateInstance<DSSearchWindow>();
                _searchWindow.Initialize(this);
            }

            nodeCreationRequest = context =>
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindow);
        }

        private void AddMinimap()
        {
            _miniMap = new MiniMap()
            {
                anchored = true
            };
            
            _miniMap.SetPosition(new Rect(15, 50, 200, 180));
            
            Add(_miniMap);

            _miniMap.visible = false;
        }

        private void AddMinimapStyles()
        {
            StyleColor backgroundColor = new StyleColor(new Color32(29, 29, 30, 255));
            StyleColor borderColor = new StyleColor(new Color32(51, 51, 51, 255));

            _miniMap.style.backgroundColor = backgroundColor;
            _miniMap.style.borderTopColor = borderColor;
            _miniMap.style.borderBottomColor = borderColor;
            _miniMap.style.borderRightColor = borderColor;
            _miniMap.style.borderLeftColor = borderColor;
        }

        private void AddStyles()
        {
            this.AddStyleSheets(
                "DialogueSystem/DSGraphViewStyles.uss", 
                "DialogueSystem/DSNodeStyles.uss"
                );
        }

        private void AddGridBackground()
        {
            GridBackground gridBackground = new GridBackground();
            gridBackground.StretchToParentSize(); 
            Insert(0, gridBackground);
        }

        #endregion

        #region Utilities

        public Vector2 GetLocalMousePos(Vector2 mousePos, bool isSearchWindow = false)
        {
            Vector2 worldMousePos = mousePos;

            if (isSearchWindow)
            {
                worldMousePos -= _editorWindow.position.position ;
            }
            
            Vector2 localMousePos = contentViewContainer.WorldToLocal(worldMousePos);
            return localMousePos;
        }

        public void ClearGraph()
        {
            graphElements.ForEach(graphElements => RemoveElement(graphElements));
            
            _groups.Clear();
            _groupedNodes.Clear();
            _ungroupedNodes.Clear();

            NameErrorsAmount = 0;
        }

        public void ToggleMiniMap()
        {
            _miniMap.visible = !_miniMap.visible;
        }

        #endregion
    }
}
