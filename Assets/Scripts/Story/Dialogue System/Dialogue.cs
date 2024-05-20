using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Story/Dialogue", order = 0)]
public class Dialogue : ScriptableObject, ISerializationCallbackReceiver
{
    //[Header("Dialogue Data")]
    //[SerializeField] bool skipParentNode = false;
    //[Header("Dialogue Conversants Data")]
    //[SerializeField] Characters player;
    //[SerializeField] List<Characters> dialogueConversants = new List<Characters>();

    [Header("Nodes Data")]
    [SerializeField] List<DialogueNode> nodes = new List<DialogueNode>();
    [SerializeField] Vector2 newNodeOffset = new Vector2(250, 0);

    //Dictionary<Characters, int> conversantColor = new Dictionary<Characters, int>();
    Dictionary<string, DialogueNode> nodeLookup = new Dictionary<string, DialogueNode>();


    private void OnValidate()
    {
        nodeLookup.Clear();
        foreach (DialogueNode node in GetAllNodes())
        {
            if(node)
                nodeLookup[node.name] = node;
        }
    }



    /*private void SetColour()
    {
        int index = 0;
        foreach (Characters speaker in dialogueConversants)
        {
            int colorToAssign = index;
            conversantColor[speaker] = colorToAssign;

            index++;

            if (index == 6)
            {
                index = 0;
            }
        }
    }*/

    //Public Getters
    public IEnumerable<DialogueNode> GetAllNodes()
    {
        return nodes;
    }

    public DialogueNode GetRootNode()
    {
        return nodes[0];
    }

    /*public IEnumerable<DialogueNode> GetAllChildren(DialogueNode parentNode)
    {
        List<DialogueNode> result = new List<DialogueNode>();

        foreach (DialogueNode node in parentNode.GetChildren())
        {
            if (nodeLookup.ContainsKey(node.name))
            {
                result.Add(nodeLookup[node.name]);
            }

        }

        return result;
    }*/

    public DialogueNode GetParentNode(DialogueNode childNode)
    {
        foreach (DialogueNode node in GetAllNodes())
        {
            if (node.childNodes.Contains(childNode))
            {
                return node;
            }
        }
        return null;
    }


    /*public IEnumerable<Characters> GetDialogueConversants()
    {
        return dialogueConversants;
    }*/

    /*public int GetConversantColour(Characters speaker)
    {
        SetColour();
        return conversantColor[speaker];
    }*/


#if UNITY_EDITOR
    public void CreateNode(DialogueNode parentNode)
    {
        DialogueNode newNode = MakeNode(parentNode);
        Undo.RegisterCreatedObjectUndo(newNode, "Created Dialogue Node");
        Undo.RecordObject(this, "Add New Dialogue Node");
        AddNode(newNode);
    }


    public void DeleteNode(DialogueNode nodeToDelete)
    {
        Undo.RecordObject(this, "Delete Dialogue Node");
        nodes.Remove(nodeToDelete);
        OnValidate();
        CleanUpDanglingChildrenReferences(nodeToDelete);
        Undo.DestroyObjectImmediate(nodeToDelete);
    }

    private void AddNode(DialogueNode newNode)
    {
        nodes.Add(newNode);
        OnValidate();
    }
    private DialogueNode MakeNode(DialogueNode parentNode)
    {
        DialogueNode newNode = CreateInstance<DialogueNode>();
        newNode.name = Guid.NewGuid().ToString();

        if (parentNode != null)
        {
            parentNode.AddChild(newNode);
            newNode.SetPosition(parentNode.rect.position + newNodeOffset);
        }
        else
        {
            newNode.SetPosition(new Vector2(100, 250));
        }

        return newNode;
    }

    private void CleanUpDanglingChildrenReferences(DialogueNode nodeToDelete)
    {
        foreach (DialogueNode node in GetAllNodes())
        {
            node.RemoveChild(nodeToDelete);
        }
    }
#endif
    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        if (nodes.Count == 0)
        {
            DialogueNode newNode = MakeNode(null);
            AddNode(newNode);
        }

        if (AssetDatabase.GetAssetPath(this) != "")
        {
            foreach (DialogueNode node in GetAllNodes())
            {
                if (AssetDatabase.GetAssetPath(node) == "")
                {
                    AssetDatabase.AddObjectToAsset(node, this);
                }
            }
        }
#endif
    }

    public void OnAfterDeserialize()
    {

    }

}
