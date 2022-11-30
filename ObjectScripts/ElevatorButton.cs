using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorButton : Interactable
{
    [SerializeField] private int floor;
    [SerializeField] private bool type; // true: call button, false: floor button
    [SerializeField] private Elevator _elevator;

    private string _prompt;

    void Start()
    {
        if (type)
        {
            _prompt = "E) Call elevator";
        }
        else
        {
            if (floor == 0)
            {
                _prompt = "E) Ground floor";
            }
            else if (floor == 1)
            {
                _prompt = "E) 1st floor";
            }
            else if (floor == 2)
            {
                _prompt = "E) 2nd floor";
            }
            else if (floor == 3)
            {
                _prompt = "E) 3rd floor";
            }
            else if(floor > 3)
            {
                _prompt = "E) " + floor + "th floor";
            }
            else
            {
                _prompt = "E) Sublevel " + (floor * -1);
            }
        }
    }

    public override void Interact(GameObject actor)
    {
        _elevator.Call(floor);
    }

    public override string Prompt()
    {
        return _prompt;
    }
}
