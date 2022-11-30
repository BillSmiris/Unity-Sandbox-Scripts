using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleporter : Interactable
{
    [SerializeField] private GameObject _endNode;

    public override void Interact(GameObject actor)
    {
        actor.GetComponent<CharacterController>().enabled = false;
        actor.transform.position = _endNode.transform.position;
        actor.GetComponent<CharacterController>().enabled = true;
    }

    public override string Prompt()
    {
        return "E) Teleport";
    }
}
