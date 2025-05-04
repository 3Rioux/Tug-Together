using System;

public interface ITutorialStage
{
    /// <summary>
    /// Invoked by the TutorialController when this stage should begin
    /// </summary>
    void ActivateStage();

    /// <summary>
    /// Invoked by the TutorialController when this stage should be cleaned up
    /// </summary>
    void DeactivateStage();

    /// <summary>
    /// Fired by the stage script when it has detected completion (all players pressed keys, etc.)
    /// </summary>
    event Action StageCompleted;
}