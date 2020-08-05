using Unity.MLAgents;

public interface IInteractable
{
    void Interact(Agent agent);
    bool NeedsTool 
    {
        get;
    }
}
