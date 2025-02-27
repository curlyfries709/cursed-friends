using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class CustomTraversalProvider : ITraversalProvider
{
    public CustomTraversalProvider() { }
    public CustomTraversalProvider(CharacterGridUnit characterTraversingPath)
    {
        this.characterTraversingPath = characterTraversingPath;
    }

    CharacterGridUnit characterTraversingPath = null;

    public void ResetWithNewCharacter(CharacterGridUnit newCharacter)
    {
        ResetData();
        characterTraversingPath = newCharacter;
    }
    
    private void ResetData()
    {
        characterTraversingPath = null;
    }

    //ITraversalProvide OVERRIDES
    public bool filterDiagonalGridConnections
    {
        get
        {
            return true;
        }
    }
    public bool CanTraverse(Path path, GraphNode node)
    {
        GridPosition gridPosition = LevelGrid.Instance.gridSystem.GetGridPosition((Vector3)node.position);
        //If node is occupied by another Character Unit, allow it to be traversed. 

        //TODO: Update to include hazards. If immune to hazard traverse, else cannot traverse. Though if positive hazard, traverse.
        bool isOccupiedByACharacterUnit = LevelGrid.Instance.IsGridPositionOccupiedByCharacterUnit(gridPosition, true);

        if (isOccupiedByACharacterUnit)
        {
            return true;
        }

        return DefaultITraversalProvider.CanTraverse(path, node);
    }

    
}
