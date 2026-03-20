using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using System.Collections.Generic;

namespace HierarchyNavigator
{
    /// <summary>
    /// Editor tool for quickly reorganizing GameObjects in the hierarchy using keyboard shortcuts.
    /// 
    /// Shortcuts:
    /// - Ctrl+Shift+Up: Move selected GameObject(s) up in hierarchy
    /// - Ctrl+Shift+Down: Move selected GameObject(s) down in hierarchy
    /// - Ctrl+Shift+Left: Unparent selected GameObject(s) (move out)
    /// - Ctrl+Shift+Right: Parent selected GameObject(s) to sibling above (move in)
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyNavigatorTool
    {
        private const string MENU_PATH = "Tools/Hierarchy Navigator/";
        
        static HierarchyNavigatorTool()
        {
            // Register for hierarchy window GUI events to intercept shortcuts
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemGUI;
        }
        
        private static void OnHierarchyWindowItemGUI(int instanceID, Rect selectionRect)
        {
            Event e = Event.current;
            
            // Only process key events when we have selection
            if (e.type != EventType.KeyDown || Selection.gameObjects.Length == 0)
                return;
            
            // Check for Ctrl+Shift modifier
            if (!e.control || !e.shift)
                return;
            
            bool handled = false;
            
            switch (e.keyCode)
            {
                case KeyCode.UpArrow:
                    MoveUp();
                    handled = true;
                    break;
                case KeyCode.DownArrow:
                    MoveDown();
                    handled = true;
                    break;
                case KeyCode.LeftArrow:
                    if (CanMoveOut())
                    {
                        MoveOut();
                        handled = true;
                    }
                    break;
                case KeyCode.RightArrow:
                    if (CanMoveIn())
                    {
                        MoveIn();
                        handled = true;
                    }
                    break;
            }
            
            if (handled)
            {
                e.Use(); // Consume the event to prevent default behavior
            }
        }
        
        #region Move Up/Down
        
        [MenuItem(MENU_PATH + "Move Up", false, 100)]
        private static void MoveUp()
        {
            MoveSelectedObjects(-1);
        }
        
        [MenuItem(MENU_PATH + "Move Up", true)]
        private static bool MoveUpValidate()
        {
            return Selection.gameObjects.Length > 0;
        }
        
        [MenuItem(MENU_PATH + "Move Down", false, 101)]
        private static void MoveDown()
        {
            MoveSelectedObjects(1);
        }
        
        [MenuItem(MENU_PATH + "Move Down", true)]
        private static bool MoveDownValidate()
        {
            return Selection.gameObjects.Length > 0;
        }
        
        // Shortcut Manager bindings for better shortcut handling
        [Shortcut("Hierarchy Navigator/Move Up", KeyCode.UpArrow, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        private static void MoveUpShortcut()
        {
            if (!IsHierarchyFocused()) return;
            if (Selection.gameObjects.Length > 0)
                MoveSelectedObjects(-1);
        }
        
        [Shortcut("Hierarchy Navigator/Move Down", KeyCode.DownArrow, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        private static void MoveDownShortcut()
        {
            if (!IsHierarchyFocused()) return;
            if (Selection.gameObjects.Length > 0)
                MoveSelectedObjects(1);
        }
        
        [Shortcut("Hierarchy Navigator/Move Out", KeyCode.LeftArrow, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        private static void MoveOutShortcut()
        {
            if (!IsHierarchyFocused()) return;
            if (CanMoveOut())
                MoveOut();
        }
        
        [Shortcut("Hierarchy Navigator/Move In", KeyCode.RightArrow, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        private static void MoveInShortcut()
        {
            if (!IsHierarchyFocused()) return;
            if (CanMoveIn())
                MoveIn();
        }
        
        /// <summary>Returns true when the Hierarchy window is the currently focused editor window.</summary>
        private static bool IsHierarchyFocused()
        {
            var focused = EditorWindow.focusedWindow;
            return focused != null && focused.GetType().FullName.Contains("Hierarchy");
        }
        
        private static void MoveSelectedObjects(int direction)
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length == 0) return;
            
            // Sort by sibling index based on direction
            List<GameObject> sortedObjects = new List<GameObject>(selectedObjects);
            sortedObjects.Sort((a, b) =>
            {
                // If different parents, sort by parent
                if (a.transform.parent != b.transform.parent)
                    return 0;
                
                // Sort by sibling index
                int comparison = a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
                return direction > 0 ? -comparison : comparison; // Reverse for moving down
            });
            
            Undo.RecordObjects(GetTransforms(sortedObjects.ToArray()), "Move Objects in Hierarchy");
            
            foreach (GameObject obj in sortedObjects)
            {
                Transform transform = obj.transform;
                int currentIndex = transform.GetSiblingIndex();
                int newIndex = currentIndex + direction;
                
                // Get sibling count
                int siblingCount = transform.parent != null 
                    ? transform.parent.childCount 
                    : GetRootObjectCount(obj.scene);
                
                // Clamp to valid range
                newIndex = Mathf.Clamp(newIndex, 0, siblingCount - 1);
                
                if (newIndex != currentIndex)
                {
                    transform.SetSiblingIndex(newIndex);
                }
            }
            
            EditorApplication.RepaintHierarchyWindow();
        }
        
        #endregion
        
        #region Parent/Unparent (Move In/Out)
        
        [MenuItem(MENU_PATH + "Move Out (Unparent)", false, 200)]
        private static void MoveOut()
        {
            UnparentSelectedObjects();
        }
        
        [MenuItem(MENU_PATH + "Move Out (Unparent)", true)]
        private static bool MoveOutValidate()
        {
            return CanMoveOut();
        }
        
        private static bool CanMoveOut()
        {
            if (Selection.gameObjects.Length == 0) return false;
            
            // At least one object must have a parent
            foreach (GameObject obj in Selection.gameObjects)
            {
                if (obj.transform.parent != null)
                    return true;
            }
            return false;
        }
        
        [MenuItem(MENU_PATH + "Move In (Parent to Above)", false, 201)]
        private static void MoveIn()
        {
            ParentSelectedObjects();
        }
        
        [MenuItem(MENU_PATH + "Move In (Parent to Above)", true)]
        private static bool MoveInValidate()
        {
            return CanMoveIn();
        }
        
        private static bool CanMoveIn()
        {
            if (Selection.gameObjects.Length == 0) return false;

            // For multi-selection, we only need to check the top-most object
            List<GameObject> sortedSelection = GetSortedSelection();
            if (sortedSelection.Count > 0)
            {
                Transform topObject = sortedSelection[0].transform;
                Transform sibling = GetSiblingAbove(topObject);
                return sibling != null;
            }
            
            return false;
        }
        
        private static void UnparentSelectedObjects()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length == 0) return;
            
            Undo.RecordObjects(GetTransforms(selectedObjects), "Unparent Objects");
            
            foreach (GameObject obj in selectedObjects)
            {
                Transform transform = obj.transform;
                Transform parent = transform.parent;
                
                if (parent != null)
                {
                    Transform grandParent = parent.parent;
                    int parentSiblingIndex = parent.GetSiblingIndex();
                    
                    // Move to grandparent (or root) right after the current parent
                    Undo.SetTransformParent(transform, grandParent, "Unparent Object");
                    transform.SetSiblingIndex(parentSiblingIndex + 1);
                }
            }
            
            EditorApplication.RepaintHierarchyWindow();
        }
        
        private static void ParentSelectedObjects()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length == 0) return;

            // Get the sorted selection to find the top-most object
            List<GameObject> sortedObjects = GetSortedSelection();
            if (sortedObjects.Count == 0) return;

            // Find the sibling above the entire selection
            Transform newParent = GetSiblingAbove(sortedObjects[0].transform);

            if (newParent != null)
            {
                Undo.RecordObjects(GetTransforms(sortedObjects.ToArray()), "Parent Objects");

                // Parent all selected objects to the new parent
                foreach (GameObject obj in sortedObjects)
                {
                    Undo.SetTransformParent(obj.transform, newParent, "Parent Object");
                }

                // Optional: maintain original order within the new parent
                // This part can be adjusted based on desired behavior.
                // For now, they will be added at the end.
            }

            EditorApplication.RepaintHierarchyWindow();
        }
        
        private static Transform GetSiblingAbove(Transform transform)
        {
            int siblingIndex = transform.GetSiblingIndex();
            
            if (siblingIndex <= 0) return null;
            
            if (transform.parent != null)
            {
                return transform.parent.GetChild(siblingIndex - 1);
            }
            else
            {
                // Root level object
                GameObject[] rootObjects = transform.gameObject.scene.GetRootGameObjects();
                if (siblingIndex > 0 && siblingIndex < rootObjects.Length)
                {
                    return rootObjects[siblingIndex - 1].transform;
                }
            }
            
            return null;
        }
        
        #endregion
        
        #region Utility Methods
        
        private static Transform[] GetTransforms(GameObject[] gameObjects)
        {
            Transform[] transforms = new Transform[gameObjects.Length];
            for (int i = 0; i < gameObjects.Length; i++)
            {
                transforms[i] = gameObjects[i].transform;
            }
            return transforms;
        }
        
        private static int GetRootObjectCount(UnityEngine.SceneManagement.Scene scene)
        {
            return scene.rootCount;
        }

        private static List<GameObject> GetSortedSelection()
        {
            List<GameObject> sortedSelection = new List<GameObject>(Selection.gameObjects);
            sortedSelection.Sort((a, b) =>
            {
                if (a.transform.parent != b.transform.parent)
                {
                    // This case is complex to handle correctly for sorting across different parents.
                    // For now, we can assume selection is within the same parent or at root.
                    return 0;
                }
                return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
            });
            return sortedSelection;
        }
        
        #endregion
    }
}
