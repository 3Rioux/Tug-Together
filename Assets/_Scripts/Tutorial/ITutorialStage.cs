// ITutorialStage.cs
using System;

public interface ITutorialStage
{
    /// <summary>Called by the controller to start this stage</summary>
    void ActivateStage();

    /// <summary>Called by the controller (or self) to hide this stage</summary>
    void DeactivateStage();

    /// <summary>Fired when the stage is done and the controller should move on</summary>
    event Action StageCompleted;
}