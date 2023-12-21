using System;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements; 
using DS.Utilities;
using UnityEditor.UIElements;

namespace DS.Windows
{
    public class DSEditorWindow : EditorWindow
    {
        private DSGraphView _graphView;
        private readonly  string _defaultFileName = "DialogueFileName";
        private static TextField _fileNameTextField;
        private Button _saveButton;
        private Button _miniMapButton;
        
        [MenuItem("Window/DialogueSystem/Dialogue Graph")]
        public static void ShowExample()
        {
            GetWindow<DSEditorWindow>("Dialogue Graph");
        }

        private void CreateGUI()
        {
            AddGraphView();
            AddToolbar();
            AddStyles();
        }

        #region Element Addition

        private void AddGraphView()
        {
            _graphView = new DSGraphView(this);
            _graphView.StretchToParentSize(); 
            rootVisualElement.Add(_graphView);
        }

        private void AddToolbar()
        {
            Toolbar toolbar = new Toolbar();

            _fileNameTextField  = DSElementUtility.CreateTextField(_defaultFileName, "File Name:", callback =>
            {
                _fileNameTextField.value = callback.newValue.RemoveWhitespaces().RemoveSpecialCharacters();
            });
            
            _saveButton = DSElementUtility.CreateButton("Save", ()=> SaveGraph());
            
            Button loadButton = DSElementUtility.CreateButton("Load", () => LoadGraph());
            _miniMapButton = DSElementUtility.CreateButton("Minimap", () => ToggleMiniMap());
            Button clearButton = DSElementUtility.CreateButton("Clear", () => ClearGraph());
            Button resetButton = DSElementUtility.CreateButton("Reset", () => ResetGraph());
            

            toolbar.Add(_fileNameTextField);
            toolbar.Add(_saveButton);
            toolbar.Add(loadButton);
            toolbar.Add(_miniMapButton);

            VisualElement space = new VisualElement();
            space.style.minWidth = 50;
            toolbar.Add(space);
            
            toolbar.Add(clearButton);
            toolbar.Add(resetButton);
            
            toolbar.AddStyleSheets("DialogueSystem/DSToolbarStyles.uss");
            
            rootVisualElement.Add(toolbar);
        }

        private void AddStyles()
        {
            rootVisualElement.AddStyleSheets("DialogueSystem/DSVariables.uss");
        }

        #endregion

        #region Toolbar Actions

        private void SaveGraph()
        {
            if (string.IsNullOrEmpty(_fileNameTextField.value))
            {
                EditorUtility.DisplayDialog(
                    "Invalid file name.",
                    "Please ensure the file name is valid.",
                    "Ok"
                    );

                return;
            }
            
            DSIOUtility.Initialize(_graphView, _fileNameTextField.value);
            DSIOUtility.Save();
        }

        private void LoadGraph()
        {
            string filePath = EditorUtility.OpenFilePanel("Dialogue Graphs", "Assets/Editor/DialogueSystem/Graphs", "asset");
            if (string.IsNullOrEmpty(filePath))
                return;
            
            ClearGraph();
            DSIOUtility.Initialize(_graphView, Path.GetFileNameWithoutExtension(filePath));
            DSIOUtility.Load();
        }

        private void ToggleMiniMap()
        {
            _graphView.ToggleMiniMap();
            _miniMapButton.ToggleInClassList("ds-toolbar__button__selected");
        }

        private void ClearGraph()
        {
            _graphView.ClearGraph();
        }

        private void ResetGraph()
        {
            ClearGraph();
            UpdateFileName(_defaultFileName);
        }

        #endregion
        
        #region Utility

        public static void UpdateFileName(string newFileName)
        {
            _fileNameTextField.value = newFileName;
        }
        
        public void EnableSaving()
        {
            _saveButton.SetEnabled(true);
        }

        public void DisableSaving()
        {
            _saveButton.SetEnabled(false);
        }
        
        #endregion
    }
}