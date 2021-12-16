using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class State
{
    protected Character character;

    public State(Character character)
    {
        this.character = character;
    }

    public abstract void Execute();

    public virtual void OnStateEnter() { }
    public virtual void OnStateExit() { }


    //protected void DisplayOnUI(UIManager.Alignment alignment)
    //{
    //    UIManager.Instance.Display(this, alignment);
    //}
}
