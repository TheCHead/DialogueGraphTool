using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DS.Data;
using DS.Data.Save;
using DS.Elements;
using DS.Enumerations;
using DS.ScriptableObjects;
using DS.Windows;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DS.Utilities
{
    public static class DSIOUtility
    {
        private static DSGraphView _graphView;
        private static string _graphFileName;
        private static string _containerFolderPath;
        private static List<DSGroup> _groups;
        private static List<DSNode> _nodes;

        private static Dictionary<string, DSDialogueGroupSO> _createdDialogueGroups;
        private static Dictionary<string, DSDialogueSO> _createdDialogues;

        private static Dictionary<string, DSGroup> _loadedGroups;
        private static Dictionary<string, DSNode> _loadedNodes;

        public static void Initialize(DSGraphView graphView, string graphName)
        {
            _graphView = graphView;
            _graphFileName = graphName;
            _containerFolderPath = $"Assets/DialogueSystem/Dialogues/{_graphFileName}";
            _groups = new List<DSGroup>();
            _nodes = new List<DSNode>();
            _createdDialogueGroups = new Dictionary<string, DSDialogueGroupSO>();
            _createdDialogues = new Dictionary<string, DSDialogueSO>();
            _loadedGroups = new Dictionary<string, DSGroup>();
            _loadedNodes = new Dictionary<string, DSNode>();
        }
        
        #region SaveMethods

        public static void Save()
        {
            CreateStaticFolders();
            GetElementsFromGraphView();
            // Editor use
            DSGraphSaveDataSO graphData = CreateAsset<DSGraphSaveDataSO>
                ("Assets/Editor/DialogueSystem/Graphs", $"{_graphFileName}Graph");
            graphData.Initialize(_graphFileName);

            // Runtime use
            DSDialogueContainerSO dialogueContainerData = CreateAsset<DSDialogueContainerSO>
                (_containerFolderPath, _graphFileName);
            dialogueContainerData.Initialize(_graphFileName);
            
            SaveGroups(graphData, dialogueContainerData);
            SaveNodes(graphData, dialogueContainerData);
            
            SaveAsset(graphData);
            SaveAsset(dialogueContainerData);
        }

        #region Nodes

        private static void SaveNodes(DSGraphSaveDataSO graphData, DSDialogueContainerSO dialogueContainerData)
        {
            SerializableDictionary<string, List<string>> groupedNodeNames =
                new SerializableDictionary<string, List<string>>();
            List<string> ungroupedNodeNames = new List<string>();
            
            foreach (DSNode node in _nodes)
            {
                SaveNodeToGraph(node, graphData);
                SaveNodeToScriptableObject(node, dialogueContainerData);

                if (node.Group != null)
                {
                    groupedNodeNames.AddItem(node.Group.title, node.DialogueName);
                    continue;
                }
                
                ungroupedNodeNames.Add(node.DialogueName);
            }

            UpdateDialogueChoicesConnections();

            UpdateOldGroupedNodes(groupedNodeNames, graphData);
            UpdateOldUngroupedNodes(ungroupedNodeNames, graphData);
        }

        private static void UpdateDialogueChoicesConnections()
        {
            foreach (DSNode node in _nodes)
            {
                DSDialogueSO dialogue = _createdDialogues[node.ID];

                for (int i = 0; i < node.Choices.Count; i++)
                {
                    DSChoiceSaveData nodeChoice = node.Choices[i];

                    if (string.IsNullOrEmpty(nodeChoice.NodeId))
                        continue;
                    dialogue.Choices[i].NextDialogue = _createdDialogues[nodeChoice.NodeId];
                    
                    SaveAsset(dialogue);
                }
            }
        }

        private static void SaveNodeToGraph(DSNode node, DSGraphSaveDataSO graphData)
        {
            // Cloning list of choices so ScriptableObject would not change every time we edit node choice in graph (as choice data is of reference type)
            List<DSChoiceSaveData> choices = CloneNodeChoices(node.Choices);
            
            DSNodeSaveData nodeData = new DSNodeSaveData
            {
                ID = node.ID,
                Name = node.DialogueName,
                Text = node.Text,
                Choices = choices,
                GroupId = node.Group?.ID,
                DialogueType = node.DSDialogueType,
                Position = node.GetPosition().position
            };
            
            graphData.Nodes.Add(nodeData);
        }

        private static void SaveNodeToScriptableObject(DSNode node, DSDialogueContainerSO dialogueContainerData)
        {
            DSDialogueSO dialogueData;

            if (node.Group != null)
            {
                dialogueData = CreateAsset<DSDialogueSO>($"{_containerFolderPath}/Groups/{node.Group.title}/Dialogues",
                    node.DialogueName);
                dialogueContainerData.DialogueGroups.AddItem(_createdDialogueGroups[node.Group.ID], dialogueData);
            }
            else
            {
                dialogueData = CreateAsset<DSDialogueSO>($"{_containerFolderPath}/Global/Dialogues", node.DialogueName);
                dialogueContainerData.UngroupedDialogues.Add(dialogueData);
            }
            
            dialogueData.Initialize(
                node.DialogueName, 
                node.Text, 
                ConvertNodeChoicesToDialogueChoices(node.Choices), 
                node.DSDialogueType, 
                node.IsStartingNode()
                );
            
            _createdDialogues.Add(node.ID, dialogueData);
            SaveAsset(dialogueData);
        }

        private static List<DSDialogueChoiceData> ConvertNodeChoicesToDialogueChoices(List<DSChoiceSaveData> nodeChoices)
        {
            List<DSDialogueChoiceData> dialogueChoices = new List<DSDialogueChoiceData>();

            foreach (DSChoiceSaveData nodeChoice in nodeChoices)
            {
                DSDialogueChoiceData choiceData = new DSDialogueChoiceData
                {
                    Text = nodeChoice.Text
                };
                dialogueChoices.Add(choiceData);
            }

            return dialogueChoices;
        }

        private static void UpdateOldGroupedNodes(SerializableDictionary<string, List<string>> currentGroupedNodeNames, DSGraphSaveDataSO graphData)
        {
            if (graphData.OldGroupedNodeNames != null && graphData.OldGroupedNodeNames.Count != 0)
            {
                foreach (KeyValuePair<string,List<string>> oldGroupedNode in graphData.OldGroupedNodeNames)
                {
                    List<string> nodesToRemove = new List<string>();

                    if (currentGroupedNodeNames.ContainsKey(oldGroupedNode.Key))
                    {
                        nodesToRemove = oldGroupedNode.Value.Except(currentGroupedNodeNames[oldGroupedNode.Key])
                            .ToList();
                    }

                    foreach (string nodeToRemove in nodesToRemove)
                    {
                        RemoveAsset($"{_containerFolderPath}/Groups/{oldGroupedNode.Key}/Dialogues", nodesToRemove);
                    }
                }
            }

            graphData.OldGroupedNodeNames = new SerializableDictionary<string, List<string>>(currentGroupedNodeNames);
        }

        private static void UpdateOldUngroupedNodes(List<string> currentUngroupedNodeNames, DSGraphSaveDataSO graphData)
        {
            if (graphData.OldUngroupedNodeNames != null && graphData.OldUngroupedNodeNames.Count != 0)
            {
                List<string> nodesToRemove = graphData.OldGroupNames.Except(currentUngroupedNodeNames).ToList();

                foreach (string nodeToRemove in nodesToRemove)
                {
                    RemoveAsset($"{_containerFolderPath}/Global/Dialogue", nodesToRemove);
                }
            }

            graphData.OldUngroupedNodeNames = currentUngroupedNodeNames;
        }

        #endregion

        #region Groups

        private static void SaveGroups(DSGraphSaveDataSO graphData, DSDialogueContainerSO dialogueContainerData)
        {
            List<string> groupNames = new List<string>();
            
            foreach (DSGroup group in _groups)
            {
                SaveGroupToGraph(group, graphData);
                SaveGroupToScriptableObject(group, dialogueContainerData);
                groupNames.Add(group.title);
            }

            UpdateOldGroups(groupNames, graphData);
        }

        private static void SaveGroupToGraph(DSGroup @group, DSGraphSaveDataSO graphData)
        {
            DSGroupSaveData groupData = new DSGroupSaveData()
            {
                ID = group.ID,
                Name = group.title,
                Position = group.GetPosition().position
            };
            
            graphData.Groups.Add(groupData);
        }

        private static void SaveGroupToScriptableObject(DSGroup @group, DSDialogueContainerSO dialogueContainerData)
        {
            string groupName = group.title;
            
            CreateFolder($"{_containerFolderPath}/Groups", groupName);
            CreateFolder($"{_containerFolderPath}/Groups/{groupName}", "Dialogues");

            DSDialogueGroupSO dialogueGroupData = CreateAsset<DSDialogueGroupSO>($"{_containerFolderPath}/Groups/{groupName}", groupName);
            dialogueGroupData.Initialize(groupName);
            _createdDialogueGroups.Add(group.ID, dialogueGroupData);
            dialogueContainerData.DialogueGroups.Add(dialogueGroupData, new List<DSDialogueSO>());

            SaveAsset(dialogueGroupData);
        }

        private static void UpdateOldGroups(List<string> currentGroupNames, DSGraphSaveDataSO graphData)
        {
            if (graphData.OldGroupNames != null && graphData.OldGroupNames.Count != 0)
            {
                List<string> groupsToRemove = graphData.OldGroupNames.Except(currentGroupNames).ToList();

                foreach (string groupToRemove in groupsToRemove)
                {
                    RemoveFolder($"{_containerFolderPath}/Groups/{groupsToRemove}");
                }
            }

            graphData.OldGroupNames = new List<string>(currentGroupNames);
        }

        #endregion

        #endregion

        #region LoadMethods

        public static void Load()
        {
            DSGraphSaveDataSO dsGraphData =
                LoadAsset<DSGraphSaveDataSO>("Assets/Editor/DialogueSystem/Graphs", _graphFileName);

            if (dsGraphData == null)
            {
                EditorUtility.DisplayDialog(
                    "Couldn't load the file!",
                    "The file at the following path could not be found:\n\n" +
                    $"Assets/Editor/DialogueSystem/Graphs/{_graphFileName}\n\n",
                    "Ok"
                );

                return;
            }
            
            DSEditorWindow.UpdateFileName(dsGraphData.FileName);

            LoadGroups(dsGraphData.Groups);
            LoadNodes(dsGraphData.Nodes);
            LoadNodesConnections();
        }

        private static void LoadGroups(List<DSGroupSaveData> groups)
        {
            foreach (DSGroupSaveData groupData in groups)
            {
                DSGroup group = _graphView.CreateGroup(groupData.Name, groupData.Position);
                group.ID = groupData.ID;
                _loadedGroups.Add(group.ID, group);
            }
        }

        private static void LoadNodes(List<DSNodeSaveData> nodes)
        {
            foreach (DSNodeSaveData nodeData in nodes)
            {
                List<DSChoiceSaveData> choices = CloneNodeChoices(nodeData.Choices);
                DSNode node = _graphView.CreateNode(nodeData.Name, nodeData.DialogueType, nodeData.Position, false);
                node.ID = nodeData.ID;
                node.Choices = choices;
                node.Text = nodeData.Text;
                
                node.Draw();
                
                _graphView.AddElement(node);
                
                _loadedNodes.Add(node.ID, node);

                if (string.IsNullOrEmpty(nodeData.GroupId))
                    continue;

                DSGroup group = _loadedGroups[nodeData.GroupId];
                node.Group = group;
                group.AddElement(node);
            }
        }

        private static void LoadNodesConnections()
        {
            foreach (KeyValuePair<string, DSNode> loadedNode in _loadedNodes)
            {
                foreach (Port choicePort in loadedNode.Value.outputContainer.Children())
                {
                    DSChoiceSaveData choiceData = (DSChoiceSaveData)choicePort.userData;

                    if (string.IsNullOrEmpty(choiceData.NodeId))
                        continue;

                    DSNode nextNode = _loadedNodes[choiceData.NodeId];

                    Port nextNodeInputPort = (Port)nextNode.inputContainer.Children().First();

                    Edge edge = choicePort.ConnectTo(nextNodeInputPort);
                    _graphView.AddElement(edge);

                    loadedNode.Value.RefreshPorts();
                }
            }
        }

        #endregion

        #region Fetch Methods

        private static void GetElementsFromGraphView()
        {
            Type groupType = typeof(DSGroup);

            _graphView.graphElements.ForEach(graphElement =>
            {
                if (graphElement is DSNode node)
                {
                    _nodes.Add(node);
                    return;
                }

                if (graphElement.GetType() == groupType)
                {
                    DSGroup group = (DSGroup)graphElement;
                    _groups.Add(group);
                    return;
                }
            });
        }

        #endregion

        #region Creation Methods

        private static void CreateStaticFolders()
        {
            CreateFolder("Assets/Editor/DialogueSystem", "Graphs");
            CreateFolder("Assets","DialogueSystem");
            CreateFolder("Assets/DialogueSystem", "Dialogues");
            CreateFolder("Assets/DialogueSystem/Dialogues", _graphFileName);
            CreateFolder(_containerFolderPath, "Global");
            CreateFolder(_containerFolderPath, "Groups");
            CreateFolder($"{_containerFolderPath}/Global", "Dialogues");
        }

        #endregion

        #region Utility Methods

        public static void CreateFolder(string path, string folderName)
        {
            if (AssetDatabase.IsValidFolder($"{path}/{folderName}"))
            {
                return;
            }

            AssetDatabase.CreateFolder(path, folderName);
        }

        public static void RemoveFolder(string fullPath)
        {
            FileUtil.DeleteFileOrDirectory($"{fullPath}.meta");
            FileUtil.DeleteFileOrDirectory($"{fullPath}/");
        }

        public static T CreateAsset<T>(string path, string assetName) where T : ScriptableObject
        {
            string fullPath = $"{path}/{assetName}.asset";
            T asset = LoadAsset<T>(path, assetName);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, fullPath);
            }
            
            return asset;
        }

        public static T LoadAsset<T>(string path, string assetName) where T : ScriptableObject
        {
            string fullPath = $"{path}/{assetName}.asset";
            return AssetDatabase.LoadAssetAtPath<T>(fullPath);
        }

        public static void RemoveAsset(string path, List<string> assetName)
        {
            AssetDatabase.DeleteAsset($"{path}/{assetName}.asset");
        }

        public static void SaveAsset(UnityEngine.Object assetObj)
        {
            EditorUtility.SetDirty(assetObj);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static List<DSChoiceSaveData> CloneNodeChoices(List<DSChoiceSaveData> nodeChoices)
        {
            List<DSChoiceSaveData> choices = new List<DSChoiceSaveData>();

            foreach (DSChoiceSaveData choice in nodeChoices)
            {
                DSChoiceSaveData choiceData = new DSChoiceSaveData
                {
                    Text = choice.Text,
                    NodeId = choice.NodeId
                };

                choices.Add(choiceData);
            }

            return choices;
        }

        #endregion
    }
}

