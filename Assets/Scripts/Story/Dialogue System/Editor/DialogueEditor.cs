using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.Linq;

public class DialogueEditor : EditorWindow
{
    Dialogue selectedDialogue = null;
    [NonSerialized] GUIStyle nodeStyle = null;
    [NonSerialized] DialogueNode draggingNode = null;
    [NonSerialized] Vector2 draggingOffset;
    [NonSerialized] DialogueNode creatingNode = null;
    [NonSerialized] DialogueNode deletingNode = null;
    [NonSerialized] DialogueNode linkingParentNode = null;
    [NonSerialized] bool draggingCanvas = false;
    [NonSerialized] Vector2 draggingCanvasOffset;

    Vector2 scrollPos;

    //Constants
    const float canvasSize = 4000;
    const float backgroundSize = 50;
    const float nodeBottomPadding = 45;
    const float maxNodeHeight = 250;
    const float iconSize = 25;


    [MenuItem("Another World Tools/Dialogue Editor")]
    public static void ShowEditorWindow()
    {
        GetWindow(typeof(DialogueEditor), false, "Dialogue Editor");
    }

    [OnOpenAsset(1)]
    public static bool OpenDialogue(int instanceID, int line)
    {
        Dialogue dialogue = EditorUtility.InstanceIDToObject(instanceID) as Dialogue;
        if (dialogue != null)
        {
            ShowEditorWindow();
            return true;
        }

        return false;
    }

    private void OnEnable()
    {
        Selection.selectionChanged += OnSelectionChanged;

        nodeStyle = new GUIStyle();
        nodeStyle.normal.textColor = Color.white;
        nodeStyle.padding = new RectOffset(20, 20, 20, 20);
        nodeStyle.border = new RectOffset(12, 12, 12, 12);
    }

    private void OnSelectionChanged()
    {
        Dialogue dialogue = Selection.activeObject as Dialogue;

        if (dialogue != null)
        {
            selectedDialogue = dialogue;
            Repaint();
        }
    }

    private void OnGUI()
    {
        if (selectedDialogue == null)
        {
            EditorGUILayout.LabelField("No Dialogue Selected");
        }
        else
        {
            ProcessEvents();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawBackground();

            foreach (DialogueNode node in selectedDialogue.GetAllNodes())
            {
                DrawConnections(node);
            }

            foreach (DialogueNode node in selectedDialogue.GetAllNodes())
            {
                DrawNode(node);
            }

            EditorGUILayout.EndScrollView();

            if (creatingNode != null)
            {
                selectedDialogue.CreateNode(creatingNode);
                creatingNode = null;
            }

            if (deletingNode != null)
            {
                selectedDialogue.DeleteNode(deletingNode);
                deletingNode = null;
            }
        }
    }



    private void DrawNode(DialogueNode node)
    {
        Rect rect;

        StoryCharacter speaker = node.GetSpeaker();

        if (selectedDialogue.GetRootNode() == node)
        {
            DrawStartNode(node);
        }
        else
        {
            if (speaker)
            {
                int i = (int)speaker.dialogueNodeColour;
                nodeStyle.normal.background = EditorGUIUtility.Load("node" + i) as Texture2D;
            }
            else
            {
                nodeStyle.normal.background = EditorGUIUtility.Load("node0") as Texture2D;
            }

            GUIStyle nameStyle = new GUIStyle();
            nameStyle.fontSize = speaker ? 17 : 14;
            nameStyle.normal.textColor = Color.white;

            GUILayout.BeginArea(node.rect, nodeStyle);
            rect = EditorGUILayout.BeginVertical(); //BEGIN VERTICAL

            if ((Event.current.type != EventType.Layout) && (Event.current.type != EventType.Used))
            {
                float clampedHeight = Mathf.Clamp(rect.height, 20, maxNodeHeight);
                node.SetSize(new Vector2(node.rect.width, clampedHeight + nodeBottomPadding));
            }

            GUILayout.BeginHorizontal();//BEGIN 1ST HORIZONTAL

            string nameToDisplay = speaker ? speaker.characterName : "PLEASE SELECT A CHARACTER";
            EditorGUILayout.LabelField(nameToDisplay, nameStyle, GUILayout.Width(180));

            GUIStyle iconStyle = new GUIStyle();
            iconStyle.fixedHeight = iconSize;
            iconStyle.fixedWidth = iconSize;

            if (speaker && node.isDialogueChoice)
            {
                Texture2D iconTex = Resources.Load("Choice") as Texture2D;
                GUILayout.Box(iconTex, iconStyle);
            }


            if (speaker && node.isThinkBubble)
            {
                Texture2D iconTex = Resources.Load("Think") as Texture2D;
                GUILayout.Box(iconTex, iconStyle);
            }

            if (speaker && node.HasConditions())
            {
                Texture2D iconTex = Resources.Load("Clipboard") as Texture2D;
                GUILayout.Box(iconTex, iconStyle);
            }

            /*if (GUILayout.Button("Resize"))
            {
                node.SetSize(new Vector2(node.rect.width, rect.height + nodeBottomPadding));
            }*/
            GUILayout.EndHorizontal();//END 1ST HORIZONTAL

            if (speaker)
            {
                //Text
                string mood = node.mood.ToString();
                EditorGUILayout.LabelField(mood, GUILayout.ExpandWidth(true));



                //Text
                GUIStyle textStyle = new GUIStyle();

                textStyle.normal.textColor = Color.white;
                textStyle.padding = new RectOffset(10, 10, 10, 10);
                textStyle.wordWrap = true;
                textStyle.fontSize = 14;


                //textStyle.normal.background = EditorGUIUtility.Load("node0") as Texture2D;

                EditorGUILayout.LabelField(node.text, textStyle, GUILayout.ExpandWidth(true));
            }

            if(speaker && node.choiceReference != ChoiceReferences.None)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Space(35);
                Texture2D iconTex = Resources.Load("Brain") as Texture2D;
                GUILayout.Box(iconTex, iconStyle);

                EditorGUILayout.LabelField(node.choiceReference.ToString(), GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
            }
        }

        LinkButtonRow(node);

        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void LinkButtonRow(DialogueNode node)
    {
        GUILayout.BeginHorizontal();

        if(selectedDialogue.GetRootNode() != node)
        {
            if (GUILayout.Button("X"))
            {
                deletingNode = node;
            }
        }

        if (linkingParentNode == null)
        {
            if (GUILayout.Button("link"))
            {
                linkingParentNode = node;
            }
        }
        else
        {
            if (linkingParentNode == node)
            {
                if (GUILayout.Button("Cancel"))
                {
                    linkingParentNode = null;
                }
            }
            else if (linkingParentNode.childNodes.Contains(node))
            {
                if (GUILayout.Button("unlink"))
                {
                    linkingParentNode.RemoveChild(node);
                    linkingParentNode = null;
                }
            }
            else
            {
                if (selectedDialogue.GetRootNode() != node)
                {
                    if (GUILayout.Button("child"))
                    {
                        linkingParentNode.AddChild(node);
                        linkingParentNode = null;
                    }
                }  
            }

        }

        if (GUILayout.Button("+"))
        {
            creatingNode = node;
        }

        GUILayout.EndHorizontal();
    }

    private void DrawStartNode(DialogueNode node)
    {
        Rect rect;

        GUIStyle startStyle = new GUIStyle(nodeStyle);
        GUIStyle textStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

        textStyle.normal.textColor = Color.black;
        startStyle.normal.background = EditorGUIUtility.Load("node4") as Texture2D;
  
        GUILayout.BeginArea(node.rect, startStyle);
        rect = EditorGUILayout.BeginVertical(); //BEGIN VERTICAL

        EditorGUILayout.LabelField("START", textStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        node.SetSize(new Vector2(250, 75));
    }

    
    private void ProcessEvents()
    {
        if (Event.current.type == EventType.MouseDown && draggingNode == null)
        {
            draggingNode = GetNodeAtPoint(Event.current.mousePosition + scrollPos);
            if (draggingNode != null)
            {
                draggingOffset = draggingNode.rect.position - Event.current.mousePosition;
                Selection.activeObject = draggingNode;
            }
            else
            {
                draggingCanvas = true;
                draggingCanvasOffset = Event.current.mousePosition + scrollPos;
                Selection.activeObject = selectedDialogue;
            }

        }
        else if (Event.current.type == EventType.MouseDrag && draggingNode != null)
        {

            draggingNode.SetPosition(Event.current.mousePosition + draggingOffset);
            Repaint();
        }
        else if (Event.current.type == EventType.MouseDrag && draggingCanvas)
        {
            scrollPos = draggingCanvasOffset - Event.current.mousePosition;
            Repaint();
        }
        else if (Event.current.type == EventType.MouseUp && draggingNode != null)
        {
            draggingNode = null;
        }
        else if (Event.current.type == EventType.MouseUp && draggingCanvas)
        {
            draggingCanvas = false;
        }


    }

    private void DrawConnections(DialogueNode node)
    {
        Vector2 parentNodeOffset = new Vector3(node.rect.width / 2, 0); //FOR HORIZONTAL TREE
        //Vector2 parentNodeOffset = new Vector3(0, node.rect.height / 2); //FOR VERTICAL TREE
        Vector3 startPosition = node.rect.center + parentNodeOffset;

        foreach (DialogueNode childNode in node.childNodes)
        {
            Vector2 childNodeOffset = new Vector3(childNode.rect.width / 2, 0); //FOR HORIZONTAL TREE
            //Vector2 childNodeOffset = new Vector3(0, childNode.rect.height / 2); //FOR VERTICAL TREE
            Vector3 endPosition = childNode.rect.center - childNodeOffset;
            Vector3 controlPointOffset = endPosition - startPosition;
            controlPointOffset.y = 0;
            controlPointOffset.x = controlPointOffset.x * 0.8f;
            Handles.DrawBezier(startPosition, endPosition, startPosition + controlPointOffset, endPosition - controlPointOffset, Color.white, null, 4f);
        }
    }




    private DialogueNode GetNodeAtPoint(Vector2 point)
    {
        DialogueNode foundNode = null;
        foreach (DialogueNode node in selectedDialogue.GetAllNodes())
        {
            if (node.rect.Contains(point))
            {
                foundNode = node;
            }

        }
        return foundNode;
    }



    private void DrawBackground()
    {
        Rect canvas = GUILayoutUtility.GetRect(canvasSize, canvasSize);
        Texture2D backgroundTex = Resources.Load("background") as Texture2D;
        Rect textCoords = new Rect(0, 0, canvasSize / backgroundSize, canvasSize / backgroundSize);
        GUI.DrawTextureWithTexCoords(canvas, backgroundTex, textCoords);
    }
}
